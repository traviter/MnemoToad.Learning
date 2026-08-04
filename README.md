# MnemoToad.Learning

Backend service for the learning/study domain of MnemoToad — flashcards and spaced-repetition
review (starting with Leitner boxes), expanding to other study modes over time. ASP.NET Core Web
API backed by Postgres.

This is one of several services that make up the MnemoToad platform. The **Knowledge** service
(knowledge-graph data: node types, knowledge nodes, relationships) and a consumer-facing
**Application** service (web + mobile) live in their own separate repos and are not part of this
one. See [traviter/MnemoToad.Knowledge](https://github.com/traviter/MnemoToad.Knowledge) for the
sibling repo this one's structure/conventions were scaffolded from.

**Status**: skeleton only — no domain model yet. The flashcard/Leitner-box entity design is a
follow-up piece of work; this repo currently has the project structure, shared plumbing (DB context
abstraction, test infrastructure, CI pipeline), and a `/health` endpoint, mirroring
`MnemoToad.Knowledge`'s conventions so the domain work can start directly.

## Stack

Same as `MnemoToad.Knowledge`: **.NET 10** ASP.NET Core Web API (controllers, no minimal APIs),
**EF Core**/Npgsql against **PostgreSQL**, **DbUp** for schema migrations, **NUnit**+**Moq** for
.NET tests, **Karate** (Java/Maven) for HTTP-level integration tests. See that repo's `CLAUDE.md`
for the full set of conventions this one follows (repository pattern with no service layer,
DataAnnotations validation, constraint-violation translation, three-layer test structure, etc.).

## Solution layout

| Project | Purpose |
|---|---|
| `MnemoToad.Learning.Api` | ASP.NET Core Web API — controllers, contracts, DI setup |
| `MnemoToad.Learning.Data` | `AppDbContext`/`IAppDbContext` (no entities yet) |
| `MnemoToad.Learning.DbMigrator` | Standalone DbUp console app that applies SQL migrations |
| `MnemoToad.Learning.Tests` | NUnit/Moq tests + shared test infrastructure |
| `MnemoToad.Learning.Karate` | Java/Maven HTTP integration tests (separate toolchain) |

## Running locally

This service runs its API **side by side** with `MnemoToad.Knowledge`'s (different ports, so both
can be up at once), but shares the **same local Postgres container** as a separate database —
mirroring the shared-server-per-service-database topology used in Azure:

| | Knowledge | Learning |
|---|---|---|
| API (http/https) | 5065 / 7117 | 5075 / 7127 |
| Postgres (host port) | 5432 (shared) | 5432 (shared) |
| Postgres database | `mnemotoad_knowledge` | `mnemotoad_learning` |
| DB roles | `mnemotoad_knowledge_admin` / `mnemotoad_knowledge_app` | `mnemotoad_learning_admin` / `mnemotoad_learning_app` |

`MnemoToad.Learning.DbMigrator/docker-compose.yml` is identical to `MnemoToad.Knowledge`'s
equivalent (same `name:`/`container_name:`) so it works the same from either repo and controls the
same container — no need to run it from both. The container doesn't auto-create any database —
each service creates its own via its own bootstrap script.

1. Start Postgres (if not already running from the Knowledge repo):
   `docker compose -f MnemoToad.Learning.DbMigrator/docker-compose.yml up -d`
2. First time only: create the `mnemotoad_learning` database and its roles by running
   `MnemoToad.Learning.DbMigrator/Bootstrap/CreateDatabaseAndRoles.sql` against the container
   (e.g. `psql -h localhost -U postgres -f MnemoToad.Learning.DbMigrator/Bootstrap/CreateDatabaseAndRoles.sql`,
   password `localdevpassword`).
3. Your own `appsettings.Development.json` already exists locally in `MnemoToad.Learning.Api/` and
   `MnemoToad.Learning.DbMigrator/` (gitignored, not shared) pointing at `localhost:5432` /
   `mnemotoad_learning` with the roles above.
4. Apply migrations: `dotnet run --project MnemoToad.Learning.DbMigrator`
5. Run the API: `dotnet run --project MnemoToad.Learning.Api`

## Testing

- `dotnet test MnemoToad.Learning.Tests/MnemoToad.Learning.Tests.csproj`
- Karate (from `MnemoToad.Learning.Karate/`): `mvn test -Dkarate.env=dev -Dkarate.tags=@Smoke`
