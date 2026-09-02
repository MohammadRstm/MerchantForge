# Build stage - has the full SDK (compilers, NuGet cache), never shipped in the final image.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, from just the project file, so this layer is cached across
# builds unless a package reference actually changes.
COPY MerchForge.api.csproj .
RUN dotnet restore MerchForge.api.csproj

COPY . .
RUN dotnet publish MerchForge.api.csproj -c Release -o /app/publish --no-restore

# Runtime stage - aspnet's runtime-only image, a fraction of the SDK image's size.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# curl is not present in this base image (verified by exec'ing into a built
# container) -- it exists solely so HEALTHCHECK below has something to call;
# nothing in the app itself needs it.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# wwwroot is created here (not left to Program.cs's own Directory.CreateDirectory)
# so it's owned by appuser before the app ever runs as that non-root user -- and
# it's a mount point for a volume in docker-compose.prod.yml, so uploaded product/
# template images survive container recreation.
RUN useradd --system --create-home appuser \
    && mkdir -p /app/wwwroot \
    && chown -R appuser:appuser /app

COPY --from=build --chown=appuser:appuser /app/publish .

USER appuser

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=15s --timeout=5s --start-period=20s --retries=5 \
    CMD curl -f http://127.0.0.1:8080/health || exit 1

ENTRYPOINT ["dotnet", "MerchForge.api.dll"]
