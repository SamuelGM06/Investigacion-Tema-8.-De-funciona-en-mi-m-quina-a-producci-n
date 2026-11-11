# Bitácora de decisiones y fallos

Registro del equipo para la diapositiva final de la demo (minuto 18-20, a cargo del Integrante 3).
Cada integrante agrega sus entradas; las del Integrante 3 ya están escritas y las secciones de los
Integrantes 1 y 2 quedan para que las completen.

Formato: **qué pasó → por qué → qué hicimos → qué aprendimos.**

## Integrante 3 — Despliegue en la nube y costos

### Decisiones

| # | Decisión | Por qué | Alternativas descartadas |
|---|---|---|---|
| D1 | Hospedar en **Render** con imágenes Docker. | Reutiliza las mismas imágenes de `docker compose`; despliegue automático tras pasar los checks de CI; plan gratuito para la demo. | Azure App Service (más caro a escala pequeña), VPS propio (más trabajo de operación y licencia de SQL Server). |
| D2 | Base de datos **SQL Server en Somee**. | Render no ofrece SQL Server administrado y no queríamos cambiar el código ni las migraciones. | Migrar a PostgreSQL (reescribe el proveedor de EF y las migraciones); SQL Server en contenedor de Render (servicio de pago, unos $25/mes). |
| D3 | Secretos solo en el panel de Render (y en GitHub Secrets si el pipeline los necesita). | Principio de configuración del Integrante 1: nada de secretos en el repo. | Variables en `appsettings.json` (ya habían quedado en el historial de git). |
| D4 | *Auto-Deploy* de Render en **After CI Checks Pass**. | Que un cambio solo llegue a producción si compilan y pasan las pruebas, sin secrets ni pasos extra en el workflow. | Auto-Deploy en cada push (se saltaría las pruebas); *Deploy Hook* llamado desde el workflow (más visible en Actions, pero requiere secrets). |

### Fallos y aprendizajes

| # | Qué pasó | Causa | Qué hicimos | Aprendizaje |
|---|---|---|---|---|
| F1 | Al abrir la URL de la API en el navegador salió **404**. | La API no tiene ruta en `/`; solo `/health` y `/api/...`. | Comprobamos `/health` y un login real con Postman. | Un 404 en la raíz no siempre es un error; hay que probar los endpoints que existen. |
| F2 | El nginx del front apuntaba a `http://api:8080`. | Ese nombre solo existe dentro de `docker compose`, no en Render. | Parametrizamos nginx con las variables `API_UPSTREAM` y `PORT` (plantilla procesada al arrancar el contenedor). | Lo que depende del entorno debe ser configuración, no estar fijo en la imagen. |
| F3 | Render espera un puerto distinto al que escucha la API. | La imagen escucha en 8080. | Definimos `PORT=8080` en el servicio. | Revisar el contrato de puerto de cada plataforma. |
| F4 | Las credenciales de la base y la llave JWT habían estado en el historial de git. | Estaban versionadas en `appsettings.json`. | Se rotó la contraseña de la base en Somee; la llave JWT de producción es distinta de la anterior y vive solo en Render. | Borrar un secreto del archivo no lo borra del historial: hay que rotarlo. |
| F5 | Una llave de ejemplo se pegó en la conversación con la IA. | Descuido al pedir una validación. | Se pidió generar otra y ponerla solo en el panel. | Un secreto que se comparte, aunque sea en un chat, deja de ser secreto. |
| F6 | No teníamos Docker instalado en una de las máquinas. | Docker Desktop no estaba instalado. | Se probó el build directamente en Render y en el pipeline. | Para la demo y la defensa conviene tener Docker disponible localmente. |
| F7 | El `README.md` quedó con marcadores de conflicto de merge en `main`. | Un merge sin resolver se subió al repositorio. | Se corrigió y se subió de nuevo. | Revisar `git status` y buscar `<<<<<<<` antes de hacer push. |
| F8 | La carpeta `docs/` desapareció al volver a clonar el repositorio. | El archivo del Integrante 1 estaba solo en una copia local y no se había subido a `main`. | Se restauró desde la copia leída y se subió al repositorio. | Lo que no está en el remoto no existe: hacer push a menudo. |

### Pendientes y riesgos abiertos

- El checkbox de completar tarea en el front llama a una ruta `PATCH .../toggle` que la API no tiene,
  y el front lee `isCompleted` mientras la API devuelve `Status`. Corregir antes de mostrar el
  checkbox en vivo.
- El plan gratuito de Render duerme los servicios tras 15 minutos sin tráfico: abrir ambas URLs unos
  minutos antes de presentar.
- Confirmar el plan contratado en Somee y sus límites.

## Integrante 1 — Contenedores y secretos

_Pendiente de completar por el Integrante 1 (decisiones y fallos de Dockerfile, compose y manejo de secretos)._

## Integrante 2 — CI/CD

_Pendiente de completar por el Integrante 2 (decisiones y fallos del pipeline y las pruebas)._

## Uso de inteligencia artificial

Usamos Claude (Anthropic) como asistente. Declaración de qué hizo y qué verificó el equipo:

| Para qué se usó la IA | Qué verificó el equipo por su cuenta |
|---|---|
| Revisar el repositorio y detectar problemas (conflicto en el README, rutas del front que no existen en la API, identidad de git). | Se confirmó cada hallazgo mirando los archivos y el historial. |
| Comparar opciones de hospedaje y buscar precios. | Los precios de terceros están marcados con ≈ y se confirman en las páginas oficiales antes de presentar. |
| Redactar la configuración de nginx con variables de entorno y el paso de despliegue del pipeline. | El despliegue real en Render y el login contra la API en producción se probaron en vivo. |
| Redactar los borradores de la documentación del Integrante 3 y de esta bitácora. | Los supuestos del presupuesto son discutibles y se explican en el documento. |

Errores de la IA que se corrigieron durante el trabajo: se afirmó que el *health check* de Render
mantendría el servicio despierto, sin haberlo verificado; después se corrigió y se recomendó abrir
los servicios antes de presentar.
