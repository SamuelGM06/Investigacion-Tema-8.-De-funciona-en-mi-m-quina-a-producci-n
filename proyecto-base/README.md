# TodoApi — proyecto base

Solución del Primer Proyecto Programado: la API de tareas desarrollada en clase, con las seis
funcionalidades del enunciado.

1. Dueño de la tarea.
2. Estados con transiciones válidas.
3. Fecha límite y vencimiento.
4. Estadísticas.
5. Notificación de tareas vencidas.
6. Revisión automática en segundo plano.

El proyecto mantiene la organización vista en clase: los controladores usan directamente el
`DbContext` y reciben las entidades en el body. Este es el punto de partida para las sesiones de
arquitectura, donde se reorganiza paso a paso.

## Requisitos

- SDK de .NET 10.
- SQL Server (el del laboratorio, uno local o un contenedor de Docker).
- La herramienta de Entity Framework: `dotnet tool install --global dotnet-ef`.

## Puesta en marcha

1. Configurar la cadena de conexión y la llave JWT con User Secrets (ver «Configuración y secretos»).
   - `LabConnection` es la que usa `Program.cs`, con usuario `sa`.
   - `HomeConnection` usa la autenticación de Windows.
   - Las dos crean una base nueva llamada `TodoApiArquitecturaDB`, así que no hay choque con la
     base usada en clase.
2. Crear la base de datos desde la carpeta `TodoApi/`:

   ```bash
   dotnet ef database update
   ```

3. Ejecutar:

   ```bash
   dotnet run
   ```

   La API queda en `http://localhost:5289`.

## Endpoints

| Método | Ruta | Qué hace |
|---|---|---|
| POST | `/api/auth/register` | Registra un usuario (`userName`, `password`) |
| POST | `/api/auth/login` | Devuelve un token JWT |
| GET | `/api/todoitems` | Lista las tareas del usuario; filtros `status`, `categoryId`, `overdue=true` |
| GET | `/api/todoitems/{id}` | Consulta una tarea propia |
| POST | `/api/todoitems` | Crea una tarea (siempre empieza en `Pendiente`) |
| PUT | `/api/todoitems/{id}` | Edita título, descripción, fecha límite y categoría |
| PATCH | `/api/todoitems/{id}/status` | Cambia el estado: `{ "newStatus": "EnProgreso", "force": false }` |
| DELETE | `/api/todoitems/{id}` | Elimina una tarea propia |
| GET | `/api/todoitems/stats` | Total, cantidad por estado, vencidas y promedio de días hasta completar |
| POST | `/api/todoitems/notificar-vencidas` | Notifica las tareas vencidas del usuario que no se habían notificado |
| GET, POST, PUT, DELETE | `/api/categories` | Categorías; no se puede eliminar una que tenga tareas (409) |

Todas las rutas, salvo las de `auth`, necesitan el encabezado `Authorization: Bearer <token>`.
Una tarea de otro usuario responde 403.

El archivo `TodoApi/TodoApi.http` trae todas las solicitudes en orden, listas para la extensión
REST Client de VS Code. También funcionan desde Postman.

## Detalles de funcionamiento

- **Fechas:** se guardan y se comparan en UTC. Conviene enviar la fecha límite con zona horaria,
  por ejemplo `2026-12-01T18:00:00Z`.
- **Transiciones de estado:** las únicas válidas son Pendiente → EnProgreso, Pendiente → Cancelada,
  EnProgreso → Completada y EnProgreso → Cancelada. Completar una tarea vencida exige
  `"force": true`.
- **Notificación:** escribe una advertencia en el log, que aparece en la consola. Una tarea no se
  vuelve a notificar, salvo que cambie su fecha límite y vuelva a vencer.
- **Revisión en segundo plano:** corre cada `OverdueCheck:IntervalMinutes` minutos (1 por defecto,
  en `appsettings.json`) y revisa las tareas de todos los usuarios.

## Estructura

```
TodoApi/
├── Controllers/   AuthController, CategoriesController, TodoItemsController
├── Data/          TodoDbContext y migraciones
├── Models/        TodoItem, TodoStatus, Category, Credentials, ChangeStatusRequest
├── Services/      INotifier, LogNotifier, OverdueNotificationService,
│                  OverdueCheckBackgroundService
├── Program.cs
└── appsettings.json
```

## Ejecutar con Docker (recomendado)

Solo se necesita Docker Desktop. Desde la carpeta `proyecto-base/`:

```bash
cp .env.example .env        # editar MSSQL_SA_PASSWORD y JWT_KEY
docker compose up --build
```

| Servicio | URL | Descripción |
|---|---|---|
| web | http://localhost:4200 | Angular servido por nginx; `/api` se reenvía a la API |
| api | http://localhost:8080 | API; `GET /health` devuelve `Healthy` si la base responde |
| db  | (interno) | SQL Server 2022 con volumen persistente `sqldata` |

Las migraciones se aplican solas al arrancar la API. Para borrar todo, incluidos los datos:
`docker compose down -v`.

## Configuración y secretos

Ningún secreto vive en el repositorio. `appsettings.json` deja vacíos los valores sensibles y la API
**no arranca** si faltan:

| Clave | Variable de entorno | Secreto |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` | Sí |
| `Jwt:Key` | `Jwt__Key` | Sí |
| `Cors:AllowedOrigins` | `Cors__AllowedOrigins__0` | No |
| `Database:MigrateOnStartup` | `Database__MigrateOnStartup` | No |

Para trabajar sin Docker (`dotnet run`) se usan User Secrets:

```bash
cd TodoApi
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=TodoApiArquitecturaDB;User Id=sa;Password=...;TrustServerCertificate=True"
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 32)"
```

En GitHub Actions y en el hospedaje los mismos valores se definen como *secrets* / variables de
entorno del servicio. Más detalle en `../docs/integrante-1-contenedores-y-secretos.md`.
