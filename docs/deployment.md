# Docker & local deployment

A local/staging stack, not a chosen production topology - no hosting provider
has been picked yet (see the roadmap's Phase 9). This documents what exists:
how to build and run the whole app in containers, and how the pieces fit
together.

## What's here

- `MerchForge.api/Dockerfile` - multi-stage: `build` (SDK, restores and
  publishes), `runtime` (aspnet runtime image, what actually ships), and
  `migrator` (same SDK build, but its entrypoint is `dotnet ef database
  update` instead of the app - see [Migrations](#migrations)).
- `MerchForgeClient/Dockerfile` - multi-stage: `build` (Node, `npm run
  build`), `runtime` (nginx serving the static output, with SPA-fallback
  routing so client-side routes don't 404 on a hard refresh).
- `docker-compose.yml` (at the `MerchForge` solution root) - MySQL/MariaDB,
  Mailpit (a local SMTP catcher - see [Email](#email)), the migration
  one-off, the API, and the dashboard frontend.
- `scripts/deploy-migrate.sh`, `scripts/backup-db.sh`,
  `scripts/restore-db.sh`, `scripts/seed-demo-business.sh`.

## Quick start

```bash
cd MerchForge                      # the backend solution root
cp .env.example .env                # fill in DB_ROOT_PASSWORD / JWT_SECRET_KEY
docker compose up -d --build
scripts/seed-demo-business.sh       # creates a SuperAdmin + a demo owner/business
```

Then:
- Dashboard: http://localhost:8081
- API: http://localhost:8080 (health check at `/health`)
- Sent emails (invitation links, etc.): http://localhost:8025

## Migrations

The API **never** applies migrations on its own startup - `Program.cs` has no
`Database.Migrate()`/`EnsureCreated()` call, deliberately. Schema changes only
ever happen via the explicit `migrator` build target
(`scripts/deploy-migrate.sh`, or docker-compose's `migrate` service, which
`api` waits on before starting). This is a deliberate safety choice: an
unreviewed migration should never run automatically as a side effect of a
routine container restart.

## The frontend's cross-repo build context

`docker-compose.yml`'s `frontend` service builds from
`../../../Desktop/MerchForgeClient` - the two repos are expected to be cloned
side by side on the same machine, which is how this project's local setup
already works. This is a local/staging convenience, not something a real
deployment pipeline would do; a real CI/CD setup would build and push the
frontend as its own image from its own repo, and this compose file would pull
that image instead of building it inline.

## Email

The backend's `EmailService` sends real SMTP (`MailKit`). Locally, there's no
real SMTP account to use, so `docker-compose.yml` points `Email:Host`/`Port`
at [Mailpit](https://github.com/axllent/mailpit) instead - every email the app
sends (invitations, notifications) lands there instead of a real inbox, with a
web UI at http://localhost:8025 to read them.

## TLS / cookies

This stack is plain HTTP end to end - no reverse proxy or TLS termination in
front of it. Refresh-token cookies default to `Secure: true` (see
`appsettings.json`), which a browser silently drops over plain HTTP, breaking
login entirely - so `docker-compose.yml` overrides `RefreshToken__Secure` and
`CustomerRefreshToken__Secure` to `false` for this stack specifically. A real
deployment must sit behind HTTPS and must **not** carry that override forward.

## Backups

See [backup-and-recovery.md](backup-and-recovery.md).
