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
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."

if [[ -f .env ]]; then
    set -a; source .env; set +a
fi

DB_HOST="${DB_HOST:-127.0.0.1}"
DB_PORT="${DB_PORT:-3306}"
DB_USER="${DB_USER:-root}"
DB_NAME="${DB_NAME:-merchforge}"

if [[ -z "${DB_ROOT_PASSWORD:-}" ]]; then
    echo "DB_ROOT_PASSWORD must be set (in the environment, or in a local .env file: DB_ROOT_PASSWORD=...)." >&2
    exit 1
fi

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
    read -r -p "This will overwrite the current $DB_NAME database ($DB_HOST:$DB_PORT) with $BACKUP_FILE. Type 'yes' to continue: " CONFIRM
    if [[ "$CONFIRM" != "yes" ]]; then
        echo "Aborted."
        exit 1
    fi
fi

echo "Restoring $DB_NAME database ($DB_HOST:$DB_PORT) from $BACKUP_FILE ..."
mysql -h "$DB_HOST" -P "$DB_PORT" -u "$DB_USER" -p"$DB_ROOT_PASSWORD" "$DB_NAME" < "$BACKUP_FILE"

echo "Restore complete."
