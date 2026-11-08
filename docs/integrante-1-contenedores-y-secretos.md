# Integrante 1 — Contenedores y configuración/secretos

Material de apoyo para la exposición (min 0-3 de la teoría) y la demo.

## 1. Contenedores: qué problema resuelven

**El problema: «funciona en mi máquina».** Una aplicación depende de mucho más que su código:
versión del runtime (.NET 10), del sistema operativo, librerías nativas, variables de entorno,
la base de datos y su versión. En este mismo proyecto lo vimos: el front apuntaba al puerto 5039
y la API corría en 5289, y la cadena de conexión dependía de que cada quien tuviera su SQL Server
(`LabConnection` vs `HomeConnection`). Cada máquina era un entorno distinto.

**La solución: empaquetar la app con su entorno.** Una *imagen* contiene el sistema de archivos
mínimo, el runtime y la aplicación ya compilada. Un *contenedor* es una instancia en ejecución de
esa imagen. La misma imagen corre igual en la laptop, en GitHub Actions y en Render/Azure.

### Cómo funcionan (más allá de la definición)

| Concepto | Qué es en realidad |
|---|---|
| Aislamiento | El kernel de Linux da a cada proceso su propia vista del sistema con **namespaces** (PID, red, montajes, usuarios). El proceso cree que está solo. |
| Límites de recursos | **cgroups** limitan CPU y memoria por contenedor. |
| Imagen por capas | Cada instrucción del Dockerfile crea una capa de solo lectura (sistema de archivos *union/overlay*). Las capas se reutilizan y cachean. |
| Contenedor vs VM | La VM virtualiza hardware y arranca un SO completo (GB, minutos). El contenedor comparte el kernel del anfitrión (MB, segundos). En Windows/Mac, Docker Desktop corre una VM Linux liviana (WSL2) por debajo. |
| Inmutabilidad | La imagen no cambia; los datos que deben persistir van en **volúmenes** (`sqldata` en nuestro compose). |

### Decisiones tomadas en nuestro producto

- **Build multi-etapa** (`TodoApi/Dockerfile`, `ToDoApp/Dockerfile`): se compila con el SDK
  (~800 MB) y se publica sobre la imagen de runtime (~220 MB). La imagen final no tiene código
  fuente ni compiladores → más pequeña y con menos superficie de ataque.
- **Orden de capas para la caché**: primero se copia el `.csproj`/`package.json` y se restauran
  dependencias; después el código. Cambiar una línea de C# no vuelve a descargar los paquetes.
- **Usuario sin privilegios** (`USER app`): si alguien explota la API, no es root dentro del contenedor.
- **Front con nginx como proxy inverso**: el navegador llama a `/api` en el mismo origen y nginx lo
  reenvía al contenedor `api`. Así no hay que compilar la URL de la API dentro del JavaScript ni
  abrir CORS.
- **docker compose** orquesta 3 servicios (`db`, `api`, `web`) en una red privada; la API encuentra
  a la base por el nombre `db`. El *healthcheck* de SQL Server + `depends_on: service_healthy`
  evita que la API arranque antes de que la base acepte conexiones.
- **Migraciones al iniciar** (`Database__MigrateOnStartup=true`): el contenedor de SQL arranca vacío.
  Es cómodo en demo; en un sistema grande se prefiere aplicarlas como un paso del pipeline.

## 2. Configuración y secretos

### Qué encontramos en el proyecto base

`appsettings.json` tenía versionados la contraseña de `sa`, una cadena de conexión a una base real
en somee.com con usuario y contraseña, y la llave que firma los JWT. Cualquiera con acceso al repo
podía conectarse a la base o **fabricar tokens válidos** para cualquier usuario.

> Importante: borrarlos del archivo no los borra del **historial de git**. Esas credenciales deben
> considerarse comprometidas y **rotarse** (cambiar la contraseña de la base y generar una llave JWT
> nueva). Limpiar el historial (git filter-repo / BFG) es opcional; rotar es obligatorio.

### Principios aplicados (12-Factor App, factor III «Config»)

1. **Separar configuración de código.** La misma imagen debe servir para local, pruebas y producción;
   lo que cambia entre entornos (cadena de conexión, llaves, orígenes CORS) se inyecta desde afuera.
2. **Distinguir configuración de secreto.** `Jwt:Issuer` o `OverdueCheck:IntervalMinutes` pueden
   estar en `appsettings.json`. La contraseña de la base y la llave JWT, nunca.
3. **Fail fast.** `Program.cs` se niega a arrancar si falta `ConnectionStrings:DefaultConnection` o
   `Jwt:Key`, en vez de usar un valor por defecto inseguro.
4. **Mínimo privilegio y rotación.** Cada entorno con sus propias credenciales; si se filtra la de
   desarrollo, producción no se ve afectada.

### Dónde vive cada secreto según el entorno

| Entorno | Mecanismo | Cómo se define |
|---|---|---|
| Desarrollo con `dotnet run` | **User Secrets** (archivo fuera del repo, en el perfil del usuario) | `dotnet user-secrets set "Jwt:Key" "..."` |
| Local con Docker | Archivo **`.env`** (en `.gitignore`), leído por compose | `cp .env.example .env` |
| GitHub Actions | **GitHub Secrets** (cifrados, se enmascaran en los logs) | Settings → Secrets and variables → Actions |
| Producción (Render/Azure) | **Variables de entorno** del servicio | Panel del hospedaje |
| Sistemas más grandes | **Gestor de secretos** (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault) con rotación y auditoría | — |

ASP.NET Core une todas estas fuentes con un orden de prioridad: `appsettings.json` <
`appsettings.{Environment}.json` < User Secrets (solo Development) < variables de entorno. Una
variable `Jwt__Key` (doble guion bajo = `:`) sobrescribe `Jwt:Key` sin tocar código.

### Errores comunes que evitamos

- Escribir secretos en el `Dockerfile` con `ENV`: quedan grabados en una capa de la imagen y
  cualquiera que la descargue puede leerlos con `docker history`.
- Copiar `.env` dentro de la imagen: lo impide `.dockerignore`.
- Imprimir la configuración completa en los logs.
- Variables de entorno: son prácticas pero visibles con `docker inspect`; por eso en sistemas
  grandes se usa un gestor de secretos o *Docker secrets* montados como archivos.

## 3. Guion de la demo (Integrante 1)

1. **Estructura de contenedores**: mostrar `docker-compose.yml`, los dos `Dockerfile` y
   `docker compose ps` con los tres servicios sanos. Abrir `http://localhost:8080/health`.
2. **No hay secretos en el repo**:
   ```bash
   git grep -n -i -E "password|pwd=|Jwt.*Key\" *: *\"[^\"]" -- . ':!*.md' ':!.env.example'
   ```
   No debe devolver nada con valores reales. Mostrar `appsettings.json` con los campos vacíos y
   `.env.example` como plantilla. Mostrar que la API no arranca sin `Jwt__Key`:
   ```bash
   docker run --rm todoapp-api:local
   ```
   (sale con «Falta la configuración 'ConnectionStrings:DefaultConnection'...»).
3. **Cambio pequeño**: modificar un endpoint (por ejemplo, agregar un campo al JSON de
   `/api/todoitems/stats`), `git commit` y `git push` → el Integrante 2 sigue en GitHub Actions.
4. **Falla intencional**: romper una aserción en las pruebas, `git push` → el pipeline queda en rojo
   y no se despliega.

## 4. Preguntas probables en la defensa

- *¿Por qué no basta con borrar la contraseña del archivo?* Sigue en el historial; hay que rotarla.
- *¿Diferencia entre imagen y contenedor?* Plantilla inmutable vs. proceso en ejecución.
- *¿Por qué multi-etapa?* Imagen más pequeña, sin SDK ni código fuente.
- *¿Qué pasa con los datos si borro el contenedor de SQL?* Se conservan en el volumen `sqldata`
  (salvo `docker compose down -v`).
- *¿Por qué la API escucha en HTTP y no HTTPS?* El TLS lo termina el proxy/hospedaje; dentro de la
  red privada de contenedores no hace falta.
