# FairyMU Backend v2 architecture

## Current layers

Frontend (GitHub Pages)
→ HTTPS REST API
→ `IAccountStore`
→ either:
- `InMemoryAccountStore` for easy local/demo use
- `EfAccountStore` + PostgreSQL for persistent portal accounts

Game data:
→ `DemoGameDataService` today
→ OpenMU adapter later

## Important separation

`fairymu_portal.portal_accounts` is a FairyMU portal table.
It is intentionally NOT presented as an OpenMU account table.

We should not guess OpenMU's live persistence schema and write directly into unknown tables.

When the actual OpenMU server is provisioned, we will inspect the exact version/schema and implement:
- OpenMU account provisioning adapter
- real characters adapter
- rankings adapter
- online player adapter
- guild adapter
- event schedule adapter

## CI

GitHub Actions starts an ephemeral PostgreSQL 17 service.
The CI verifies:
- project restores/builds
- in-memory API flow
- PostgreSQL API flow
- register/login/account persistence
- publish artifact

No PostgreSQL installation is required on the user's computer.
