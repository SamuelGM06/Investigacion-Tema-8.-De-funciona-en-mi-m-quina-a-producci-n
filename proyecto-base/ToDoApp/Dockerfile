# syntax=docker/dockerfile:1
# =============================================================================
# Imagen del front-end (Angular) — build multi-etapa
#   Etapa 1 (build): Node compila la aplicación a archivos estáticos.
#   Etapa 2 (serve): nginx sirve esos archivos (sin Node en producción).
# =============================================================================

FROM node:24-alpine AS build
WORKDIR /app

# Dependencias primero para aprovechar la caché de capas
COPY package.json package-lock.json ./
RUN npm ci

COPY . ./
RUN npx ng build --configuration production

FROM nginx:1.27-alpine AS serve
# nginx procesa /etc/nginx/templates/*.template al arrancar y sustituye las variables
# de entorno (${PORT}, ${API_UPSTREAM}); el resultado queda en conf.d/default.conf.
COPY nginx.conf /etc/nginx/templates/default.conf.template
# Valores por defecto para docker compose; en Render se sobrescriben desde el panel.
ENV PORT=80 \
    API_UPSTREAM=http://api:8080
# El builder @angular/build:application deja los archivos en dist/<proyecto>/browser
COPY --from=build /app/dist/ToDoApp/browser /usr/share/nginx/html
EXPOSE 80
