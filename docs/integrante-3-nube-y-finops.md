# Integrante 3 — Arquitecto Cloud y FinOps

Material de apoyo para la exposición (min 6-10 de la teoría), la demo del despliegue y la
presentación del presupuesto. Precios consultados el **2026-11-10**; las cifras marcadas con ≈ son
estimaciones o provienen de fuentes de terceros y deben confirmarse en la página oficial antes de
presentar (ver «Fuentes»).

## 1. Qué desplegamos y dónde

```
Navegador ──HTTPS──► [ Render: servicio "web" ]  nginx + Angular (imagen Docker)
                          │  /api/*  (proxy inverso, variable API_UPSTREAM)
                          ▼
                     [ Render: servicio "api" ]  ASP.NET Core 10 (imagen Docker, puerto 8080, /health)
                          │  ConnectionStrings__DefaultConnection
                          ▼
                     [ Somee: SQL Server ]  base externa con acceso remoto
```

- Render no ofrece SQL Server administrado (solo PostgreSQL y Redis; otras bases corren como
  servicio propio con disco persistente). El proyecto usa SQL Server y EF Core, así que la base
  vive en Somee. Alternativa descartada por ahora: migrar a PostgreSQL (cambia el proveedor de EF y
  las migraciones, y afecta al resto del equipo).
- Las dos imágenes Docker son las mismas que se usan en `docker compose` local: lo único que cambia
  entre entornos son las variables de entorno.

## 2. Comparación de opciones de hospedaje

| Familia | Qué es | Quién opera qué |
|---|---|---|
| **Nube administrada (PaaS)** | La plataforma ejecuta tu aplicación (Azure App Service, Render, etc.). | El proveedor: SO, parches, balanceo, TLS. Tú: código y configuración. |
| **Contenedores administrados** | Entregas una imagen Docker y la plataforma la ejecuta y la escala (Render con Docker, Azure Container Apps, Cloud Run). | El proveedor: el host y el orquestador. Tú: la imagen. |
| **Servidor propio (VPS)** | Una máquina virtual (o física) donde instalas Docker y operas todo. | Tú: SO, parches, firewall, TLS, respaldos, monitoreo. |

En la práctica Render y Azure App Service ejecutan nuestras imágenes Docker, por lo que las
alternativas presupuestadas son:

- **A. Render (contenedores Docker) + SQL Server en Somee** — lo que está desplegado.
- **B. Azure App Service (Linux, contenedores) + Azure SQL Database.**
- **C. Servidor propio: VPS DigitalOcean con `docker compose` (API + web + SQL Server).**

### Criterios y puntaje (1 = malo, 5 = muy bueno)

| Criterio | Peso | A. Render + Somee | B. Azure | C. VPS propio |
|---|---|---|---|---|
| Costo mensual | 25 | 5 | 3 | 3 |
| Esfuerzo de operación | 20 | 5 | 4 | 1 |
| Compatibilidad con SQL Server | 20 | 3 | 5 | 3 |
| Escalado | 15 | 3 | 5 | 2 |
| Despliegue y rollback | 10 | 4 | 4 | 3 |
| Disponibilidad / riesgo | 10 | 2 | 4 | 2 |
| **Total ponderado (de 100)** | 100 | **78** | **82** | **47** |

Cálculo: suma de (peso × puntaje) ÷ 5. Ejemplo A: (25·5 + 20·5 + 20·3 + 15·3 + 10·4 + 10·2) ÷ 5 = 390 ÷ 5 = 78.

Por qué esos puntajes:

- **Costo:** Render con base externa gratis o barata es la más económica; Azure y el VPS cuestan más
  a escala pequeña (sección 4).
- **Operación:** en el VPS hay que parchear, respaldar y monitorear a mano; en Render casi todo
  viene incluido.
- **SQL Server:** Azure SQL es SQL Server administrado (respaldos y SLA incluidos). En Somee la base
  es externa y compartida; en el VPS corre en un contenedor que hay que respaldar y licenciar.
- **Disponibilidad:** el plan gratuito de Render duerme el servicio y Somee es un punto único de
  falla ajeno a Render.

## 3. Operación

| Tema | A. Render | B. Azure | C. VPS |
|---|---|---|---|
| **Costos** | Por instancia y por mes; plan gratuito con límites; el exceso de ancho de banda se cobra. | Por plan de App Service y por nivel de base de datos; muchos recursos se facturan aunque estén ociosos. | Precio fijo por servidor; todo lo demás es tiempo de operación. |
| **Monitoreo** | Logs y métricas en el panel; health check en `/health`. | Azure Monitor y Application Insights; alertas configurables. | Hay que instalar y mantener herramientas propias (por ejemplo Uptime Kuma, Prometheus). |
| **Escalado** | Vertical: cambiar el tipo de instancia. Horizontal: más instancias (servicios de pago, sin disco adjunto). | Vertical y horizontal; autoescalado en planes superiores. | Manual: cambiar el tamaño del droplet o montar más servidores y un balanceador. |
| **Rollback** | Volver a un despliegue anterior desde el panel, o redesplegar un commit previo. | Redesplegar la imagen anterior; los *deployment slots* permiten intercambiar versiones (planes Standard o superiores). | Volver a levantar `docker compose` con la etiqueta de imagen anterior. |

Estrategia de rollback para este proyecto:

1. Cada despliegue sale de un commit identificable en `main` (el pipeline solo despliega si
   compilan y pasan las pruebas).
2. Para volver atrás se redespliega el commit anterior (en Render, desde el historial de despliegues).
3. Las migraciones de base de datos deben ser compatibles hacia atrás: agregar columnas es seguro;
   borrar o renombrar exige dos pasos. El código se revierte fácil, los datos no.

## 4. Presupuesto mensual (USD)

### Supuestos (explícitos para poder discutirlos)

| Supuesto | 100 usuarios | 10,000 usuarios |
|---|---|---|
| Usuarios concurrentes en hora pico (5 %) | 5 | 500 |
| Frecuencia por usuario concurrente | 1 petición cada 10 s | 1 petición cada 10 s |
| Pico de peticiones | 0.5 por segundo | 50 por segundo |
| Datos (50 tareas × 1 KB por usuario) | 5 MB | 500 MB |
| Transferencia (10 sesiones/mes × 0.7 MB por usuario) | 0.7 GB | 70 GB |

Son supuestos de trabajo, no mediciones. Se justifican porque la API es un CRUD sencillo y la base
de datos, no el CPU, suele ser el cuello de botella.

### A. Render + SQL Server en Somee

| Concepto | 100 usuarios | 10,000 usuarios |
|---|---|---|
| Servicio `api` | Starter: $7.00 | Standard (2 GB RAM, 1 CPU): $25.00 |
| Servicio `web` (nginx) | Starter: $7.00 | Starter: $7.00 |
| Base de datos en Somee | $0.00 (plan gratuito actual, confirmar) | ≈ $7.95 (plan de pago) |
| Ancho de banda (peor caso, sin cupo incluido, $0.15/GB) | 0.7 GB × 0.15 = $0.11 | 70 GB × 0.15 = $10.50 |
| **Total** | **$14.11** | **$50.45** |

Con el plan gratuito de Render (API y front en Free) el costo para 100 usuarios sería $0, pero los
servicios se duermen tras 15 minutos sin tráfico y tardan cerca de un minuto en despertar; sirve para la
demo, no para producción.

### B. Azure App Service + Azure SQL Database

| Concepto | 100 usuarios | 10,000 usuarios |
|---|---|---|
| App Service Linux (hospeda `api` y `web`) | B1 (1 núcleo, 1.75 GB): $13.14 | B2 (2 núcleos, 3.5 GB): ≈ $26.28 |
| Azure SQL Database | Basic (5 DTU, 2 GB): ≈ $5.00 | Standard S1 (20 DTU): ≈ $30.00 |
| Ancho de banda | $0 (dentro del cupo gratuito, verificar) | $0 (dentro del cupo gratuito, verificar) |
| **Total** | **$18.14** | **$56.28** |

Notas: Azure SQL tiene una oferta gratuita (100,000 segundos de vCore al mes y 32 GB por base), con
lo que el total para 100 usuarios bajaría a $13.14. El precio de B2 se estima como el doble de B1.

### C. Servidor propio (VPS DigitalOcean + docker compose)

| Concepto | 100 usuarios | 10,000 usuarios |
|---|---|---|
| Droplet | 4 GB RAM, 2 vCPU: $24.00 | 8 GB RAM, 4 vCPU: $48.00 |
| Respaldos (≈ 20 % del droplet, confirmar) | $4.80 | $9.60 |
| Ancho de banda | Incluido en el plan | Incluido en el plan |
| **Total** | **$28.80** | **$57.60** |

Un droplet de 2 GB no alcanza ni para 100 usuarios: SQL Server en contenedor requiere al menos
2 GB de RAM por sí solo, y la API y nginx comparten la misma máquina.

**Licencia:** `docker-compose.yml` usa `MSSQL_PID=Developer`, que según la licencia de Microsoft es
solo para desarrollo y pruebas. Producción en un VPS exigiría la edición Express (gratuita pero con
límites de tamaño y memoria) o pagar una licencia. Es un costo oculto de esta alternativa (confirmar
con Microsoft).

### Comparación

| | A. Render + Somee | B. Azure | C. VPS |
|---|---|---|---|
| 100 usuarios | $14.11 | $18.14 | $28.80 |
| 10,000 usuarios | $50.45 | $56.28 | $57.60 |
| Costo por usuario (100) | $0.141 | $0.181 | $0.288 |
| Costo por usuario (10,000) | $0.0050 | $0.0056 | $0.0058 |
| Horas de operación al mes (estimación) | 1 | 2 | 6 |
| Total con tiempo valorado a $10/h (100 usuarios) | $24.11 | $38.14 | $88.80 |
| Total con tiempo valorado a $10/h (10,000 usuarios) | $60.45 | $76.28 | $117.60 |

Las horas de operación y la tarifa de $10/h son ilustrativas: sirven para mostrar que el precio de
lista no es el costo total. La diferencia entre las alternativas se reduce a escala, pero el
servidor propio nunca deja de ser el más caro cuando se cuenta el tiempo.

## 5. Recomendación justificada

**Para el estado actual del proyecto (cientos de usuarios, demo y entrega académica): alternativa A,
Render con la base en Somee.**

- Es la más barata ($14.11 frente a $18.14 y $28.80 con 100 usuarios) y la que menos operación
  exige.
- Reutiliza las mismas imágenes Docker que ya funcionan con `docker compose`, sin cambiar código.
- El despliegue se dispara desde el pipeline con un *Deploy Hook* y el rollback se hace desde el panel.
- Pierde solo 4 puntos frente a Azure en la comparación ponderada (78 frente a 82).

**Cuándo cambiar a B (Azure App Service + Azure SQL):**

- Si se necesita una base SQL Server con respaldos y SLA, en lugar de depender de un host externo.
- Si el tráfico se acerca a los miles de usuarios (a 10,000 usuarios la diferencia de costo es
  de unos $6 al mes y ganamos disponibilidad y escalado).
- Si se requiere autoescalado o intercambio de versiones sin caída.

**C (servidor propio)** solo tendría sentido si el equipo necesita control total o hay un requisito de
no usar nube; para este proyecto es la más cara en tiempo y tiene el problema de licencia de SQL Server.

## 6. Cómo está desplegado hoy

1. Servicio `api` en Render: tipo Web Service con runtime Docker, carpeta raíz
   `proyecto-base/TodoApi`, puerto 8080 (variable `PORT=8080`) y *health check* en `/health`.
2. Servicio `web` en Render: carpeta raíz `proyecto-base/ToDoApp`; nginx reenvía `/api` a la API con la
   variable `API_UPSTREAM` (la URL pública de la API).
3. Variables de entorno de la API (los valores solo existen en el panel de Render):
   `ConnectionStrings__DefaultConnection`, `Jwt__Key`, `Database__MigrateOnStartup`.
4. Despliegue automático: el workflow `.github/workflows/ci.yml` compila, ejecuta las pruebas y
   construye las imágenes. Solo si todo pasa y el push es a `main`, llama a los *Deploy Hooks* de
   Render (guardados como GitHub Secrets `RENDER_DEPLOY_HOOK_API` y `RENDER_DEPLOY_HOOK_WEB`).
   El *Auto-Deploy* de Render debe estar desactivado para que no despliegue por su cuenta.
5. Si las pruebas fallan, el trabajo `desplegar` no se ejecuta: el cambio no llega a producción.

## 7. Riesgos conocidos

- El plan gratuito de Render se duerme tras 15 minutos sin tráfico; las horas gratuitas (750 al mes)
  se comparten entre todos los servicios gratuitos del workspace. Abrir los servicios unos minutos
  antes de presentar.
- La base en Somee es un host externo: si cae o cambia sus condiciones, la API deja de funcionar.
  Confirmar el plan contratado y sus límites de espacio e inactividad.
- Las credenciales que estuvieron en el historial de git se consideran comprometidas: la contraseña
  de la base se rotó y la llave JWT de producción debe ser distinta de la anterior. Los valores
  vigentes solo viven en el panel de Render.

## 8. Preguntas probables en la defensa

- *¿Por qué no usaron la base gratuita de Render?* Es PostgreSQL; el proyecto usa SQL Server y
  cambiarlo implica reescribir las migraciones.
- *¿Qué pasa si el despliegue sale mal?* Se redespliega el commit anterior desde el historial de Render;
  las migraciones deben ser compatibles hacia atrás.
- *¿Cuánto cuesta con 10,000 usuarios?* Unos $50 al mes en Render + Somee y unos $56 en Azure, según los
  supuestos de la sección 4.
- *¿Por qué no un servidor propio?* Es lo más caro en tiempo de operación y la licencia Developer de
  SQL Server no permite producción.
- *¿Cómo evitan que el pipeline despliegue código roto?* El despliegue depende del trabajo de
  compilación y pruebas; si una prueba falla, el trabajo de despliegue no corre.
- *¿Dónde están los secretos?* En el panel de Render y en los GitHub Secrets; nunca en el repositorio.

## Fuentes (consultadas el 2026-11-10)

- Render: <https://render.com/docs/free>, <https://render.com/docs/faq>, <https://render.com/pricing.md>,
  <https://render.com/docs/disks>
- Precios de Render por instancia y ancho de banda (tercero):
  <https://livemy.app/blog/render-pricing>
- Azure App Service: <https://azure.microsoft.com/en-us/pricing/details/app-service/linux/>
  (B1 ≈ $13.14 según <https://learn.microsoft.com/en-us/answers/a/12322935>)
- Azure SQL Database: <https://azure.microsoft.com/updates/public-preview-azure-sql-database-free-offer/>
  y tarifas DTU de terceros en <https://www.flexera.com/blog/finops/azure-sql-database-pricing/>
- DigitalOcean: <https://www.digitalocean.com/pricing/droplets>, y tabla de terceros en
  <https://1vps-o5g.pages.dev/review-digitalocean>
- Somee (planes y acceso remoto, tercero): <https://www.whtop.com/compare/exclusivehosting.net,somee>
