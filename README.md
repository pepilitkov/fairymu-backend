# FairyMU Backend v1.1

> GitHub Actions CI: build → API smoke tests → publish artifact.

Backend scaffold за FairyMU, подготвен предварително без нужда от admin права на служебния компютър.

## Stack
- ASP.NET Core Minimal API
- .NET 10
- Built-in ASP.NET Core Identity password hashing
- Built-in ASP.NET Core rate limiting
- Explicit CORS allowlist
- In-memory accounts and sessions for v1 development
- API contract in `openapi.yaml`

## Endpoints

Public:
- `GET /api/status`
- `GET /api/online`
- `GET /api/rankings`
- `GET /api/guilds`
- `GET /api/events`

Authentication:
- `POST /api/register`
- `POST /api/login`
- `POST /api/logout`

Authenticated:
- `GET /api/account`
- `GET /api/characters`

## Important: v1 limitations
This v1 intentionally DOES NOT write to OpenMU/PostgreSQL yet.

Accounts and login sessions are in RAM and disappear when the process restarts.
The opaque bearer token is a temporary v1 implementation.

Before public launch we will:
1. replace `InMemoryAccountStore` with PostgreSQL/OpenMU integration;
2. replace temporary sessions with production authentication/session storage;
3. connect rankings, guilds, events and online count to actual OpenMU data;
4. configure HTTPS/reverse proxy on the VPS;
5. use server-side secrets/environment variables only.

## Security already included
- Passwords are never stored as plaintext.
- Passwords are hashed with ASP.NET Core Identity `PasswordHasher`.
- Registration and login have a stricter rate limit.
- CORS is limited to configured frontend origins.
- Request models use validation attributes.
- No DB password/API secret is committed.
- Protected endpoints require a valid bearer session token.

## Configuration
Edit `appsettings.Production.example.json` when deploying.
Do not commit real secrets.

Current frontend origin:
`https://pepilitkov.github.io`

## Later VPS commands
When .NET 10 SDK is installed on the VPS:

```bash
dotnet restore
dotnet build -c Release
dotnet run
```

For production, we will publish with:

```bash
dotnet publish -c Release -o ./publish
```

## OpenMU integration point
The backend is intentionally separated from the game-data adapter.
`DemoGameDataService` will later be replaced by an OpenMU/PostgreSQL-backed service.

## GitHub Actions CI

Workflow: `.github/workflows/backend-ci.yml`

При push към `main` или pull request GitHub автоматично:

1. checkout-ва repository-то;
2. инсталира .NET 10 чрез `actions/setup-dotnet`;
3. изпълнява `dotnet restore`;
4. изпълнява Release build;
5. стартира FairyMU API локално на GitHub runner-а;
6. smoke test-ва:
   - `GET /api/status`
   - `POST /api/register`
   - `POST /api/login`
   - `GET /api/account`
   - `GET /api/characters`
   - грешна парола → HTTP 401
7. изпълнява `dotnet publish`;
8. качва готовия `publish/` като GitHub Actions artifact.

### Как да видиш резултата
Repository → **Actions** → **FairyMU Backend CI** → последния run.

Зелено ✅ = build + основният API flow са минали.
Червено ❌ = отвори failed step и виж точната грешка.

### Artifact
При успешен run в долната част на workflow run-а ще има artifact с име:
`fairymu-backend-<commit-sha>`

Той е готовият publish output за бъдещия VPS.
