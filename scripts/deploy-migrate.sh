#!/usr/bin/env bash
# Applies pending EF Core migrations - the one deliberate, explicit way this
# app's schema is ever changed. Never run automatically by the API itself (see
# MerchForge.api/Dockerfile's "migrator" stage and its own comment) - this
# script, or the equivalent "migrate" service in docker-compose.yml, is the
# only thing that calls it.
#
# Usage:
#   scripts/deploy-migrate.sh                 # uses the local docker-compose stack
#   scripts/deploy-migrate.sh --host           # runs `dotnet ef` directly against
#                                               # whatever ConnectionStrings__DefaultConnection
#                                               # is already set in the environment -
#                                               # for a real deploy target, run this from
#                                               # your CI/CD pipeline with that variable set,
#                                               # not from a developer's machine.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."

if [[ "${1:-}" == "--host" ]]; then
    if [[ -z "${ConnectionStrings__DefaultConnection:-}" ]]; then
        echo "ConnectionStrings__DefaultConnection must be set in the environment for --host mode." >&2
        exit 1
    fi
    echo "Applying migrations directly against the configured connection string..."
    dotnet ef database update --project MerchForge.api/MerchForge.api.csproj
else
    echo "Applying migrations via the local docker-compose stack (mysql -> migrate)..."
    docker compose up -d mysql
    docker compose run --rm migrate
fi

echo "Migrations applied."
