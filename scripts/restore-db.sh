#!/usr/bin/env bash
# Restores the database from a backup produced by backup-db.sh. Destructive:
# this replaces every table in the target database with what's in the backup
# file. Confirms before running unless --force is passed (for use from an
# automated/CI context where the confirmation prompt can't be answered).
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."

if [[ -f .env ]]; then
    set -a; source .env; set +a
fi

if [[ -z "${DB_ROOT_PASSWORD:-}" ]]; then
    echo "DB_ROOT_PASSWORD must be set (copy .env.example to .env)." >&2
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
    read -r -p "This will overwrite the current merchforge database with $BACKUP_FILE. Type 'yes' to continue: " CONFIRM
    if [[ "$CONFIRM" != "yes" ]]; then
        echo "Aborted."
        exit 1
    fi
fi

echo "Restoring merchforge database from $BACKUP_FILE ..."
docker compose exec -T mysql mysql -uroot -p"$DB_ROOT_PASSWORD" merchforge < "$BACKUP_FILE"

echo "Restore complete."
