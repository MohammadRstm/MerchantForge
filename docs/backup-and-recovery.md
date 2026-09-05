# Backup & recovery

There was no backup/recovery story of any kind before this document - this is
the answer the production-readiness roadmap's Phase 8 required before a real
deploy.

## Mechanism

`scripts/backup-db.sh` runs `mysqldump --single-transaction` against the
database, producing a single plain-SQL file. `--single-transaction` takes a
consistent InnoDB snapshot without locking tables, so it's safe to run against
a live database. `scripts/restore-db.sh` replays a dump file back into the
database (with a confirmation prompt, since it's destructive to whatever is
currently there).

Both scripts target any directly reachable MySQL/MariaDB instance via the
`mysqldump`/`mysql` CLI clients, connecting over `DB_HOST`/`DB_PORT` (default
`127.0.0.1:3306`, overridable via the environment or a local `.env` file).

Setting `DB_CONTAINER` instead runs the client **inside** that Docker container.
That is how production is reached: `docker-compose.prod.yml` publishes only
nginx, so the database has no host port to connect to at all, and without this
neither script could run there. It also means the password is read from the
container's own environment rather than passed in, so it never appears in the
host's process listing:

```bash
DB_CONTAINER=merchforge-db-prod scripts/backup-db.sh backups
DB_CONTAINER=merchforge-db-prod scripts/restore-db.sh backups/merchforge-<timestamp>.sql
```
Whichever hosting provider is eventually chosen may also offer its own managed
backup/point-in-time-recovery feature - if so, prefer that for production and
keep these scripts as the documented, tested fallback / what local development
actually uses.

## Frequency

Not yet running on a schedule anywhere - there is no CI/CD or hosting
environment to schedule it in yet (see the roadmap's Phase 9, deferred
pending a hosting decision). Once one exists, this should run:

- **Daily**, at minimum, via whatever the hosting environment's job scheduler
  is (a cron container, a scheduled CI job, or the provider's managed backup
  feature if used instead of this script).
- **Immediately before every migration.** This one is done: the *Deploy to
  production* workflow calls `scripts/backup-db.sh` and then applies the
  migration script, in that order, and `set -e` means a failed backup stops the
  deploy with the old stack still serving. A schema change is exactly the kind
  of event a backup should exist on both sides of.

  (An earlier version of this document named a `scripts/deploy-migrate.sh` that
  was never written. The deploy workflow is where that lives now.)

## Retention

The deploy workflow keeps the ten most recent dumps on the host and deletes the
rest - enough to reach back past a bad deploy, and bounded so the directory
cannot grow without limit. That is a deploy-time safety net, not a retention
policy; the policy below is still undecided.

Not yet decided pending the hosting choice, since retention cost/policy is
usually tied to whatever object storage or managed-backup feature that
provider offers. Recommended starting point once one is chosen: 7 daily
backups + 4 weekly backups, deleting anything older - cheap, and enough to
recover from "we noticed a data problem within the last month," which is the
overwhelmingly common case.

## Storage location

Still unresolved, and now it matters: these dumps contain real customer data.
`scripts/backup-db.sh` writes to a local `backups/` directory (gitignored) by
default, which is enough to develop and test the mechanism but is not a real
backup location (a backup that lives on the same
disk as the database it's backing up doesn't protect against the failure
modes backups exist for). Once a hosting provider is chosen, `backups/`
should be replaced with genuinely off-host, durable storage (object storage
in a different region/account than the database, at minimum).

## Verifying a backup can actually be restored

A backup nobody has ever restored is a hope, not a backup. The drill:

```bash
# 1. Take a backup of the current database.
scripts/backup-db.sh

# 2. Deliberately destroy data - simulating what a backup exists to recover
#    from. Do this against a database you can afford to lose, obviously.
mysql -h "$DB_HOST" -P "$DB_PORT" -uroot -p"$DB_ROOT_PASSWORD" \
    -e "DROP DATABASE merchforge; CREATE DATABASE merchforge;"

# 3. Restore from the backup just taken.
scripts/restore-db.sh backups/merchforge-<timestamp>.sql

# 4. Confirm the data is actually back (e.g. log in as a known seeded account
#    and confirm the business still exists).
```

This drill has **not** yet actually been executed end to end - only the
scripts' syntax has been checked (`bash -n`), and the mechanism (`mysqldump`/
`mysql`) is standard enough to trust in isolation, but that is not the same as
a proven restore. It should be run for real against whatever database this
ends up deployed against before that deployment is trusted for production, and
re-run any time the backup/restore scripts change - a backup process that
silently stopped working is worse than no backup process, since it looks fine
until the moment it's needed.
