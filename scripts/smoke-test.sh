#!/usr/bin/env bash
set -euo pipefail

PORT=5080
BASE="http://127.0.0.1:${PORT}"
LOG_FILE="${RUNNER_TEMP:-/tmp}/fairymu-api.log"

cleanup() {
  if [[ -n "${API_PID:-}" ]] && kill -0 "$API_PID" 2>/dev/null; then
    kill "$API_PID" || true
    wait "$API_PID" 2>/dev/null || true
  fi
}
trap cleanup EXIT

echo "Starting FairyMU API on ${BASE}..."
ASPNETCORE_ENVIRONMENT=Development \
ASPNETCORE_URLS="${BASE}" \
dotnet run --project FairyMU.Api.csproj --configuration Release --no-build >"$LOG_FILE" 2>&1 &
API_PID=$!

echo "Waiting for API..."
for i in {1..30}; do
  if curl --fail --silent "${BASE}/api/status" >/tmp/fairymu-status.json 2>/dev/null; then
    break
  fi

  if ! kill -0 "$API_PID" 2>/dev/null; then
    echo "API process exited unexpectedly."
    cat "$LOG_FILE"
    exit 1
  fi

  sleep 1
done

curl --fail --silent "${BASE}/api/status" >/tmp/fairymu-status.json
python3 - <<'PY'
import json
d=json.load(open("/tmp/fairymu-status.json"))
assert d["status"] == "Online", d
assert d["server"] == "FairyMU", d
print("PASS: /api/status")
PY

SUFFIX="${GITHUB_RUN_ID:-local}${GITHUB_RUN_ATTEMPT:-1}"
USERNAME="CIPlayer${SUFFIX}"
USERNAME="${USERNAME:0:16}"
EMAIL="ci-${SUFFIX}@example.com"
PASSWORD="CI-Test-Password-123!"

echo "Testing registration..."
REGISTER_CODE=$(curl --silent --output /tmp/fairymu-register.json --write-out "%{http_code}" \
  -X POST "${BASE}/api/register" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"${USERNAME}\",\"email\":\"${EMAIL}\",\"password\":\"${PASSWORD}\"}")

if [[ "$REGISTER_CODE" != "201" ]]; then
  echo "Expected HTTP 201 from register, got ${REGISTER_CODE}"
  cat /tmp/fairymu-register.json
  exit 1
fi
echo "PASS: /api/register"

echo "Testing login..."
LOGIN_CODE=$(curl --silent --output /tmp/fairymu-login.json --write-out "%{http_code}" \
  -X POST "${BASE}/api/login" \
  -H "Content-Type: application/json" \
  -d "{\"usernameOrEmail\":\"${USERNAME}\",\"password\":\"${PASSWORD}\"}")

if [[ "$LOGIN_CODE" != "200" ]]; then
  echo "Expected HTTP 200 from login, got ${LOGIN_CODE}"
  cat /tmp/fairymu-login.json
  exit 1
fi

TOKEN=$(python3 - <<'PY'
import json
d=json.load(open("/tmp/fairymu-login.json"))
token=d.get("accessToken")
assert token and len(token) >= 32, d
print(token)
PY
)
echo "PASS: /api/login"

echo "Testing authenticated account endpoint..."
ACCOUNT_CODE=$(curl --silent --output /tmp/fairymu-account.json --write-out "%{http_code}" \
  "${BASE}/api/account" \
  -H "Authorization: Bearer ${TOKEN}")

if [[ "$ACCOUNT_CODE" != "200" ]]; then
  echo "Expected HTTP 200 from account, got ${ACCOUNT_CODE}"
  cat /tmp/fairymu-account.json
  exit 1
fi

python3 - <<'PY'
import json
d=json.load(open("/tmp/fairymu-account.json"))
assert d["username"].startswith("CIPlayer"), d
assert "email" in d, d
print("PASS: /api/account")
PY

echo "Testing authenticated characters endpoint..."
CHAR_CODE=$(curl --silent --output /tmp/fairymu-characters.json --write-out "%{http_code}" \
  "${BASE}/api/characters" \
  -H "Authorization: Bearer ${TOKEN}")

if [[ "$CHAR_CODE" != "200" ]]; then
  echo "Expected HTTP 200 from characters, got ${CHAR_CODE}"
  cat /tmp/fairymu-characters.json
  exit 1
fi

python3 - <<'PY'
import json
d=json.load(open("/tmp/fairymu-characters.json"))
assert isinstance(d, list), d
assert len(d) >= 1, d
assert {"name","class","level","resets"}.issubset(d[0]), d[0]
print("PASS: /api/characters")
PY

echo "Testing invalid login returns 401..."
BAD_CODE=$(curl --silent --output /tmp/fairymu-bad-login.json --write-out "%{http_code}" \
  -X POST "${BASE}/api/login" \
  -H "Content-Type: application/json" \
  -d "{\"usernameOrEmail\":\"${USERNAME}\",\"password\":\"WrongPassword!\"}")

if [[ "$BAD_CODE" != "401" ]]; then
  echo "Expected HTTP 401 from bad login, got ${BAD_CODE}"
  cat /tmp/fairymu-bad-login.json
  exit 1
fi
echo "PASS: invalid credentials rejected"

echo "All FairyMU API smoke tests passed."
