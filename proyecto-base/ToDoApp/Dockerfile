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
COPY nginx.conf /etc/nginx/conf.d/default.conf
# El builder @angular/build:application deja los archivos en dist/<proyecto>/browser
COPY --from=build /app/dist/ToDoApp/browser /usr/share/nginx/html
EXPOSE 80
