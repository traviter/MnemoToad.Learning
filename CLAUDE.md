# MnemoToad.Learning — Project Conventions

## Relationship to MnemoToad.Knowledge

This repo was scaffolded from [traviter/MnemoToad.Knowledge](https://github.com/traviter/MnemoToad.Knowledge),
a sibling repo in the same MnemoToad platform (separate repo per service, not a monorepo — see
that repo's CLAUDE.md "Solution layout" intro for the platform-level reasoning). **Treat
`MnemoToad.Knowledge`'s CLAUDE.md as the pattern reference for conventions not yet re-documented
here** — API patterns (no service layer, repository pattern, `IAppDbContext`), database conventions
(DbUp, snake_case naming, two Postgres roles, constraint-violation translation), testing structure
(NUnit+Moq, three layers: controller/repository/system tests), and CI/CD pipeline structure
(`pipeline.yml` as sole orchestrator, `workflow_call`+`workflow_dispatch` subordinate files) all
apply here the same way unless this file says otherwise. Once this repo has its own domain-specific
decisions worth recording, add them here rather than leaving everything as "same as Knowledge."

## Current status (as of scaffold)

No domain model yet — no entities, no repositories, no controllers beyond `/health`. The
flashcard/Leitner-box entity design is deliberately a separate piece of work, not done as part of
scaffolding. `IAppDbContext`/`AppDbContext` exist but expose no `DbSet<T>` properties yet; add them
the same way `MnemoToad.Knowledge.Data`'s `AppDbContext` does once entities exist.

## Local dev — runs side by side with Knowledge, shares its Postgres container

API ports differ from Knowledge so both processes can run at once locally: `5075`/`7127` here vs.
Knowledge's `5065`/`7117`.

**Postgres is shared, not duplicated** — `MnemoToad.Learning.DbMigrator/docker-compose.yml` (placed
under `DbMigrator`, not the repo root, since it's Postgres-only infra and `DbMigrator` owns
database/schema concerns) is byte-identical to `MnemoToad.Knowledge`'s equivalent (same top-level
`name: mnemotoad`, same `container_name: mnemotoad-postgres`, same host port `5432`), specifically
so `docker compose -f <path>/docker-compose.yml up -d` from either repo resolves to the *same*
Compose project and container rather than erroring on a `container_name` conflict or spinning up a
second Postgres instance. This mirrors the shared-Azure-Postgres-server topology (see
[[project_postgres_role_naming]]) — one server, one database per service, distinct roles per
service. If either repo's `docker-compose.yml` ever needs to change, change both identically or the
"run it from either repo" property breaks.

The container doesn't pre-seed any database (no `POSTGRES_DB` env var) — `mnemotoad_learning` and
its roles don't exist until `MnemoToad.Learning.DbMigrator/Bootstrap/CreateDatabaseAndRoles.sql` is
run once against the container (see that file; same script used for the real Azure server, just
pointed at `localhost:5432` instead).

If a third service ever needs to run alongside these two locally, continue the pattern: same shared
`docker-compose.yml`/container (each in its own `DbMigrator/` project), its own database + role
pair via its own bootstrap script, its own API port.

## Azure / CI

The Azure App Service exists as of 2026-08-04: `mnemotoad-learning`, hostname
`mnemotoad-learning-dng0c0chbcfufwg8.westus3-01.azurewebsites.net`. `AZURE_WEBAPP_NAME` in
`deploy-api.yml`/`restart-api.yml` and `MnemoToad.Learning.Karate`'s `karate-config.js` `azure`
baseUrl are both set to these real values now. **The exact app name (`mnemotoad-learning`) was
inferred from the hostname pattern** (`<app-name>-<hash>.<region>-01.azurewebsites.net`, same
pattern Knowledge's `Mnemotoad` → `mnemotoad-fybvhxgdapesd2d3...` already established), not
confirmed against the Azure Portal/`az webapp list` directly — `az webapp list --query
"[?name=='...']"` does an exact, case-sensitive match, so if the real resource name has different
casing (e.g. `MnemotoadLearning` instead of `mnemotoad-learning`), `deploy-api.yml`/`restart-api.yml`
will fail to resolve a hostname/resource group and need correcting.

No deploy has happened yet — the App Service exists but is empty, so `/health` won't respond
successfully until `pipeline.yml`/`deploy-api.yml` actually runs. GitHub secrets
(`DB_ADMIN_CONNECTION_STRING`, `DB_CONNECTION_STRING`, `AZURE_CLIENT_ID`/`AZURE_TENANT_ID`/
`AZURE_SUBSCRIPTION_ID`) and the Postgres roles/database (see [[project_postgres_role_naming]]) are
still the user's setup work, not done from this side.

## Database

`MnemoToad.Learning.DbMigrator/Scripts/001_GrantAppUserPrivileges.sql` is the first (and so far
only) DbUp script — grants table privileges to `mnemotoad_learning_app`, mirroring the shape
Knowledge's `004_GrantAppUserPrivileges.sql` originally established, though Knowledge has since
moved its own version of this grant to `001_GrantKnowledgeAppUserPrivileges.sql` too (see
Knowledge's CLAUDE.md — retired script numbers can be reused for new, unrelated scripts, since
DbUp's journal tracks by filename, not number; don't read "start at 001" as meaning numbers are
sacred once used, just that nothing else claimed this slot yet on this side).
Append-only/never-renumber-an-applied-script discipline still applies from here on (see Knowledge's
CLAUDE.md "Database" section for the full rule). DB/role bootstrap (`CREATE DATABASE`,
`CREATE ROLE`) is a manual `psql` step per environment, same as Knowledge — not a DbUp script (see
`Bootstrap/CreateDatabaseAndRoles.sql`).
