#!/usr/bin/env bash
# Dumps the database to a timestamped .sql file. See docs/backup-and-recovery.md
# for the policy this implements (frequency, retention, storage) - this script
# is the mechanism, not the schedule; a real deployment runs it on a timer
# (cron/CI schedule/managed-backup feature) and ships the output somewhere
# durable off the database host, neither of which this script does itself.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."

if [[ -f .env ]]; then
    set -a; source .env; set +a
fi

if [[ -z "${DB_ROOT_PASSWORD:-}" ]]; then
    echo "DB_ROOT_PASSWORD must be set (copy .env.example to .env)." >&2
    exit 1
fi

OUT_DIR="${1:-backups}"
mkdir -p "$OUT_DIR"
TIMESTAMP="$(date -u +%Y%m%dT%H%M%SZ)"
OUT_FILE="$OUT_DIR/merchforge-$TIMESTAMP.sql"

echo "Backing up merchforge database to $OUT_FILE ..."

# --single-transaction: a consistent snapshot of InnoDB tables without locking
# them for the duration of the dump - safe to run against a live database.
docker compose exec -T mysql mysqldump \
    -uroot -p"$DB_ROOT_PASSWORD" \
    --single-transaction \
    --routines \
    --triggers \
    merchforge > "$OUT_FILE"

echo "Backup written to $OUT_FILE ($(du -h "$OUT_FILE" | cut -f1))."
