# FairyMU Backend v2 — PostgreSQL Ready

FairyMU Backend v2 adds a real PostgreSQL persistence path while keeping the no-admin workflow.

## What changed

- EF Core 10
- Npgsql EF Core provider 10.0.3
- `IAccountStore` abstraction
- in-memory store remains available
- PostgreSQL `EfAccountStore`
- separate `fairymu_portal.portal_accounts` table
- GitHub Actions PostgreSQL service
- automatic PostgreSQL smoke tests
- OpenMU adapter boundary prepared

## Modes

Default:

```json
"Persistence": { "Mode": "Memory" }
```

PostgreSQL:

```text
FairyMU__Persistence__Mode=Postgres
ConnectionStrings__FairyMU=Host=...;Database=...;Username=...;Password=...
```

For CI only, `InitializeDatabase=true` uses `EnsureCreated()` to bootstrap the temporary database.

For production we will move to controlled EF migrations before launch.

## Security

Do not commit a real production connection string.
Use environment variables / deployment secrets on the VPS.

Passwords remain hashed through ASP.NET Core Identity PasswordHasher.

## OpenMU

OpenMU integration is deliberately not faked in v2. OpenMU currently uses EF Core + PostgreSQL, but the final adapter must be built against the exact server version/schema that we deploy.

## GitHub CI success target

The workflow must pass:

- Build Release
- In-memory smoke tests
- PostgreSQL smoke tests
- Publish backend
- Upload artifact
