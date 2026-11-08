# syntax=docker/dockerfile:1
# =============================================================================
# Imagen de la API (ASP.NET Core 10) — build multi-etapa
#   Etapa 1 (build):   usa el SDK completo (~800 MB) para restaurar y publicar.
#   Etapa 2 (runtime): solo el runtime de ASP.NET (~220 MB) + los binarios.
# Resultado: imagen pequeña, sin código fuente ni herramientas de compilación.
# =============================================================================

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Se copia primero solo el .csproj para aprovechar la caché de capas:
# si no cambian las dependencias, `dotnet restore` no se vuelve a ejecutar.
COPY TodoApi.csproj ./
RUN dotnet restore TodoApi.csproj

COPY . ./
RUN dotnet publish TodoApi.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Configuración NO secreta: se puede fijar en la imagen.
# Los secretos (ConnectionStrings__DefaultConnection, Jwt__Key) se inyectan
# al ejecutar el contenedor, nunca se escriben aquí.
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    HttpsRedirection__Enabled=false \
    DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app/publish ./

# Las imágenes oficiales de .NET traen el usuario sin privilegios "app".
USER app
EXPOSE 8080

ENTRYPOINT ["dotnet", "TodoApi.dll"]
