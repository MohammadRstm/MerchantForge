#!/usr/bin/env bash
# Dumps the database to a timestamped .sql file. See docs/backup-and-recovery.md
# for the policy this implements (frequency, retention, storage) - this script
# is the mechanism, not the schedule; a real deployment runs it on a timer
# (cron/CI schedule/managed-backup feature) and ships the output somewhere
# durable off the database host, neither of which this script does itself.
#
# Targets any directly reachable MySQL/MariaDB instance (requires the
# `mysqldump` client on PATH). Supply connection details via the environment
# or a local .env file (gitignored) - only DB_ROOT_PASSWORD is required, the
# rest default to a typical local dev setup:
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

OUT_DIR="${1:-backups}"
mkdir -p "$OUT_DIR"
TIMESTAMP="$(date -u +%Y%m%dT%H%M%SZ)"
OUT_FILE="$OUT_DIR/merchforge-$TIMESTAMP.sql"

echo "Backing up $DB_NAME database ($DB_HOST:$DB_PORT) to $OUT_FILE ..."

# --single-transaction: a consistent snapshot of InnoDB tables without locking
# them for the duration of the dump - safe to run against a live database.
mysqldump \
    -h "$DB_HOST" -P "$DB_PORT" -u "$DB_USER" -p"$DB_ROOT_PASSWORD" \
    --single-transaction \
    --routines \
    --triggers \
    "$DB_NAME" > "$OUT_FILE"

echo "Backup written to $OUT_FILE ($(du -h "$OUT_FILE" | cut -f1))."
