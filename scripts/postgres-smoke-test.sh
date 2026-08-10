#!/usr/bin/env bash
set -euo pipefail

PORT=5081
BASE="http://127.0.0.1:${PORT}"
LOG_FILE="${RUNNER_TEMP:-/tmp}/fairymu-api-postgres.log"

cleanup() {
  if [[ -n "${API_PID:-}" ]] && kill -0 "$API_PID" 2>/dev/null; then
    kill "$API_PID" || true
    wait "$API_PID" 2>/dev/null || true
  fi
}
trap cleanup EXIT

ASPNETCORE_ENVIRONMENT=Development \
ASPNETCORE_URLS="${BASE}" \
FairyMU__Persistence__Mode=Postgres \
FairyMU__Persistence__InitializeDatabase=true \
ConnectionStrings__FairyMU="Host=127.0.0.1;Port=5432;Database=fairymu_ci;Username=fairymu;Password=fairymu_ci_password" \
dotnet run --project FairyMU.Api.csproj --configuration Release --no-build >"$LOG_FILE" 2>&1 &
API_PID=$!

echo "Waiting for PostgreSQL-backed FairyMU API..."
for i in {1..45}; do
  if curl --fail --silent "${BASE}/api/status" >/tmp/fairymu-pg-status.json 2>/dev/null; then
    break
  fi
  if ! kill -0 "$API_PID" 2>/dev/null; then
    echo "API exited unexpectedly:"
    cat "$LOG_FILE"
    exit 1
  fi
  sleep 1
done

python3 - <<'PY'
import json
d=json.load(open("/tmp/fairymu-pg-status.json"))
assert d["status"] == "Online", d
assert d["persistence"].lower() == "postgres", d
print("PASS: PostgreSQL mode is active")
PY

SUFFIX="${GITHUB_RUN_ID:-local}${GITHUB_RUN_ATTEMPT:-1}"
USERNAME="PG${SUFFIX}"
USERNAME="${USERNAME:0:16}"
EMAIL="pg-${SUFFIX}@example.com"
PASSWORD="PG-Test-Password-123!"

CODE=$(curl --silent --output /tmp/fairymu-pg-register.json --write-out "%{http_code}" \
  -X POST "${BASE}/api/register" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"${USERNAME}\",\"email\":\"${EMAIL}\",\"password\":\"${PASSWORD}\"}")

[[ "$CODE" == "201" ]] || { echo "Register failed ($CODE)"; cat /tmp/fairymu-pg-register.json; exit 1; }
echo "PASS: PostgreSQL registration"

LOGIN_CODE=$(curl --silent --output /tmp/fairymu-pg-login.json --write-out "%{http_code}" \
  -X POST "${BASE}/api/login" \
  -H "Content-Type: application/json" \
  -d "{\"usernameOrEmail\":\"${USERNAME}\",\"password\":\"${PASSWORD}\"}")

[[ "$LOGIN_CODE" == "200" ]] || { echo "Login failed ($LOGIN_CODE)"; cat /tmp/fairymu-pg-login.json; exit 1; }

TOKEN=$(python3 - <<'PY'
import json
d=json.load(open("/tmp/fairymu-pg-login.json"))
assert d.get("accessToken"), d
print(d["accessToken"])
PY
)
echo "PASS: PostgreSQL login"

ACCOUNT_CODE=$(curl --silent --output /tmp/fairymu-pg-account.json --write-out "%{http_code}" \
  "${BASE}/api/account" \
  -H "Authorization: Bearer ${TOKEN}")

[[ "$ACCOUNT_CODE" == "200" ]] || { echo "Account failed ($ACCOUNT_CODE)"; cat /tmp/fairymu-pg-account.json; exit 1; }

python3 - <<'PY'
import json
d=json.load(open("/tmp/fairymu-pg-account.json"))
assert d["username"].startswith("PG"), d
print("PASS: PostgreSQL account read")
PY

echo "All PostgreSQL smoke tests passed."
