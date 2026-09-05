#!/usr/bin/env bash
# Restores the database from a backup produced by backup-db.sh. Destructive:
# this replaces every table in the target database with what's in the backup
# file. Confirms before running unless --force is passed (for use from an
# automated/CI context where the confirmation prompt can't be answered).
#
# Targets any directly reachable MySQL/MariaDB instance (requires the `mysql`
# client on PATH). Supply connection details via the environment or a local
# .env file (gitignored) - only DB_ROOT_PASSWORD is required, the rest default
# to a typical local dev setup:
#   DB_HOST=127.0.0.1 DB_PORT=3306 DB_USER=root DB_NAME=merchforge
#
# Set DB_CONTAINER to run the client inside that Docker container instead of over
# TCP. That is how a containerised deployment reaches a database publishing no port
# to its host - which is the case in docker-compose.prod.yml, where only nginx is
# exposed. The password is then read from the container's own environment rather
# than passed in, so it never appears in a host process listing.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."

if [[ -f .env ]]; then
    set -a; source .env; set +a
fi

DB_HOST="${DB_HOST:-127.0.0.1}"
DB_PORT="${DB_PORT:-3306}"
DB_USER="${DB_USER:-root}"
DB_NAME="${DB_NAME:-merchforge}"

if [[ -z "${DB_CONTAINER:-}" && -z "${DB_ROOT_PASSWORD:-}" ]]; then
    echo "DB_ROOT_PASSWORD must be set (in the environment, or in a local .env file: DB_ROOT_PASSWORD=...), or set DB_CONTAINER to restore into a container." >&2
    exit 1
fi

# What the messages below name, so the confirmation prompt says which database is
# about to be overwritten in either mode.
TARGET="${DB_CONTAINER:-$DB_HOST:$DB_PORT}"

FORCE=false
ARGS=()
for arg in "$@"; do
    if [[ "$arg" == "--force" ]]; then
        FORCE=true
    else
        ARGS+=("$arg")
    fi
done

BACKUP_FILE="${ARGS[0]:-}"

if [[ -z "$BACKUP_FILE" || ! -f "$BACKUP_FILE" ]]; then
    echo "Usage: scripts/restore-db.sh <path-to-backup.sql> [--force]" >&2
    exit 1
fi

if [[ "$FORCE" != true ]]; then
    read -r -p "This will overwrite the current $DB_NAME database ($TARGET) with $BACKUP_FILE. Type 'yes' to continue: " CONFIRM
    if [[ "$CONFIRM" != "yes" ]]; then
        echo "Aborted."
        exit 1
    fi
fi

echo "Restoring $DB_NAME database ($TARGET) from $BACKUP_FILE ..."

if [[ -n "${DB_CONTAINER:-}" ]]; then
    docker exec -i "$DB_CONTAINER" sh -c \
        'MYSQL_PWD="$MARIADB_ROOT_PASSWORD" exec mysql -uroot "$1"' \
        _ "$DB_NAME" < "$BACKUP_FILE"
else
    mysql -h "$DB_HOST" -P "$DB_PORT" -u "$DB_USER" -p"$DB_ROOT_PASSWORD" "$DB_NAME" < "$BACKUP_FILE"
fi

echo "Restore complete."
