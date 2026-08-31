# Build context is this directory (MerchForge.api/) - the project has no
# ProjectReferences to sibling projects, so it doesn't need the rest of the
# solution to build.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, on just the project file, so dependency restore is cached
# across builds where only application code changed.
COPY MerchForge.api.csproj ./
RUN dotnet restore MerchForge.api.csproj

COPY . .
RUN dotnet publish MerchForge.api.csproj -c Release -o /app/publish --no-restore

# ---- runtime image: the app itself ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

# Every secret/connection value is supplied at runtime via environment
# variables (Jwt__SecretKey, ConnectionStrings__DefaultConnection, ...) - see
# appsettings.Production.json.example - never baked into the image.
ENTRYPOINT ["dotnet", "MerchForge.api.dll"]

# ---- migrator image: applies EF Core migrations, nothing else ----
# Deliberately a separate image/entrypoint from the app above, not something
# MerchForge.api.dll ever does on its own startup - migrations only ever run
# as this explicit, standalone step (see docker-compose.yml's "migrate"
# service and scripts/deploy-migrate.sh), never automatically when the API
# container starts or restarts.
FROM build AS migrator
WORKDIR /src
RUN dotnet tool install --global dotnet-ef --version 9.*
ENV PATH="$PATH:/root/.dotnet/tools"
ENTRYPOINT ["dotnet", "ef", "database", "update", "--project", "MerchForge.api.csproj"]
