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
    echo "DB_ROOT_PASSWORD must be set (in the environment, or in a local .env file: DB_ROOT_PASSWORD=...), or set DB_CONTAINER to dump from inside a container." >&2
    exit 1
fi

OUT_DIR="${1:-backups}"
mkdir -p "$OUT_DIR"
TIMESTAMP="$(date -u +%Y%m%dT%H%M%SZ)"
OUT_FILE="$OUT_DIR/merchforge-$TIMESTAMP.sql"

echo "Backing up $DB_NAME database (${DB_CONTAINER:-$DB_HOST:$DB_PORT}) to $OUT_FILE ..."

# --single-transaction: a consistent snapshot of InnoDB tables without locking
# them for the duration of the dump - safe to run against a live database.
if [[ -n "${DB_CONTAINER:-}" ]]; then
    docker exec "$DB_CONTAINER" sh -c \
        'MYSQL_PWD="$MARIADB_ROOT_PASSWORD" exec mysqldump -uroot --single-transaction --routines --triggers "$1"' \
        _ "$DB_NAME" > "$OUT_FILE"
else
    mysqldump \
        -h "$DB_HOST" -P "$DB_PORT" -u "$DB_USER" -p"$DB_ROOT_PASSWORD" \
        --single-transaction \
        --routines \
        --triggers \
        "$DB_NAME" > "$OUT_FILE"
fi

# A dump that "succeeded" with nothing in it would pass every check above, and
# restoring from one of those is how a backup turns out to be worthless.
if [[ ! -s "$OUT_FILE" ]]; then
    echo "Backup file is empty - refusing to report success." >&2
    exit 1
fi

echo "Backup written to $OUT_FILE ($(du -h "$OUT_FILE" | cut -f1))."
