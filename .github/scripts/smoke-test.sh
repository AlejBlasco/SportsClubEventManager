#!/usr/bin/env bash
#
# Smoke-tests a locally built Docker image for the "api" or "web" service.
#
# The application containers in this repo cannot start without a reachable
# SQL Server connection string (AddInfrastructure throws, and
# MigrateDatabaseAsync() runs before the host starts listening), and the api
# additionally validates a handful of Authentication:* settings on start
# (ValidateOnStart()). This script therefore spins up an ephemeral SQL Server
# container (and, for the "web" leg, an auxiliary "api" container using the
# latest image already published to GHCR) on a temporary Docker network,
# starts the image under test against them with disposable placeholder
# configuration, waits for Docker's own HEALTHCHECK (already targeting
# /health/live, see docker/Dockerfile.api and docker/Dockerfile.web) to
# report "healthy", and then verifies the container over HTTP:
#
#   - GET /health/live  -> blocking: fails the script if it is not 200.
#   - GET /health        -> informational only: the status code is logged,
#                            it never fails the script (see design doc,
#                            issue #44, for the rationale).
#
# Usage:
#   smoke-test.sh <SERVICE_NAME=api|web> <IMAGE_REF>
#
# Both arguments can also be supplied via environment variables of the same
# name (SERVICE_NAME, IMAGE_REF) if preferred by the caller.
#
# Everything created by this script (containers, network) is removed by a
# trap on EXIT, so cleanup runs even if an earlier step fails.

set -euo pipefail

SERVICE_NAME="${1:-${SERVICE_NAME:-}}"
IMAGE_REF="${2:-${IMAGE_REF:-}}"

if [[ -z "$SERVICE_NAME" || -z "$IMAGE_REF" ]]; then
  echo "::error::Usage: smoke-test.sh <SERVICE_NAME=api|web> <IMAGE_REF>" >&2
  exit 1
fi

if [[ "$SERVICE_NAME" != "api" && "$SERVICE_NAME" != "web" ]]; then
  echo "::error::SERVICE_NAME must be 'api' or 'web', got '$SERVICE_NAME'" >&2
  exit 1
fi

RUN_ID="${GITHUB_RUN_ID:-local}-${SERVICE_NAME}"
NETWORK_NAME="ci-smoke-${RUN_ID}"
SQL_CONTAINER="ci-sqlserver-${RUN_ID}"
API_AUX_CONTAINER="ci-api-aux-${RUN_ID}"
APP_CONTAINER="ci-${SERVICE_NAME}-${RUN_ID}"
HOST_PORT=8080

# SA_PASSWORD: disposable value generated for the lifetime of this container
# only, plus a fixed suffix to reliably satisfy SQL Server's password
# complexity policy (upper/lower/digit/special character).
SA_PASSWORD="$(openssl rand -base64 24)Aa1!"

# JWT secret: only its length (>=32) is validated on api startup, it is
# never used to authenticate anything real in this smoke test.
JWT_SECRET="$(openssl rand -base64 32)"

# Admin password: required by the SeedAdministratorUser migration, which
# throws on startup if AdminUser:Password is unset - disposable value, never
# used to log in to anything real in this smoke test.
ADMIN_PASSWORD="$(openssl rand -base64 24)Aa1!"

cleanup() {
  echo "Cleaning up smoke test resources for ${SERVICE_NAME}..."
  docker rm -f "$APP_CONTAINER" "$API_AUX_CONTAINER" "$SQL_CONTAINER" >/dev/null 2>&1 || true
  docker network rm "$NETWORK_NAME" >/dev/null 2>&1 || true
}
trap cleanup EXIT

# Polls `docker inspect` for a container's HEALTHCHECK status until it
# reports "healthy" or the timeout (in seconds) elapses.
wait_for_healthy() {
  local container="$1"
  local timeout_seconds="$2"
  local elapsed=0
  local status

  while true; do
    status="$(docker inspect --format='{{.State.Health.Status}}' "$container" 2>/dev/null || echo "unknown")"
    if [[ "$status" == "healthy" ]]; then
      echo "Container ${container} is healthy."
      return 0
    fi

    if (( elapsed >= timeout_seconds )); then
      echo "::error::Container ${container} did not become healthy within ${timeout_seconds}s (last status: ${status})"
      docker logs "$container" || true
      return 1
    fi

    sleep 2
    elapsed=$((elapsed + 2))
  done
}

echo "Creating temporary Docker network ${NETWORK_NAME}..."
docker network create "$NETWORK_NAME" >/dev/null

echo "Starting ephemeral SQL Server container ${SQL_CONTAINER}..."
docker run -d \
  --name "$SQL_CONTAINER" \
  --network "$NETWORK_NAME" \
  -e ACCEPT_EULA=Y \
  -e MSSQL_PID=Developer \
  -e SA_PASSWORD="$SA_PASSWORD" \
  --health-cmd='/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -C -Q "SELECT 1" || exit 1' \
  --health-interval=5s \
  --health-timeout=5s \
  --health-retries=12 \
  --health-start-period=10s \
  mcr.microsoft.com/mssql/server:2022-latest >/dev/null

wait_for_healthy "$SQL_CONTAINER" 60

CONNECTION_STRING="Server=${SQL_CONTAINER},1433;Database=SportsClubEventManagerCi;User Id=sa;Password=${SA_PASSWORD};TrustServerCertificate=True;"

if [[ "$SERVICE_NAME" == "web" ]]; then
  # The web smoke test needs a running api dependency. It always uses the
  # latest image already published to GHCR (never the api image freshly
  # built in the same PR, which lives in a separate matrix leg and is not
  # transferred between jobs) - see design doc, issue #44, resolved risk.
  echo "Starting auxiliary api container ${API_AUX_CONTAINER} (ghcr.io/alejblasco/sportsclubeventsmanager-api:latest)..."
  docker run -d \
    --name "$API_AUX_CONTAINER" \
    --network "$NETWORK_NAME" \
    -e ASPNETCORE_ENVIRONMENT=Development \
    -e ConnectionStrings__DefaultConnection="$CONNECTION_STRING" \
    -e Authentication__JwtSettings__SecretKey="$JWT_SECRET" \
    -e Authentication__JwtSettings__Issuer="ci-smoke-test" \
    -e Authentication__JwtSettings__Audience="ci-smoke-test" \
    -e Authentication__Google__ClientId="ci-smoke-test" \
    -e Authentication__Google__ClientSecret="ci-smoke-test" \
    -e AdminUser__Password="$ADMIN_PASSWORD" \
    ghcr.io/alejblasco/sportsclubeventsmanager-api:latest >/dev/null

  wait_for_healthy "$API_AUX_CONTAINER" 90
fi

echo "Starting container under test ${APP_CONTAINER} (${IMAGE_REF})..."
APP_ENV_ARGS=(
  -e ASPNETCORE_ENVIRONMENT=Development
  -e ConnectionStrings__DefaultConnection="$CONNECTION_STRING"
)

if [[ "$SERVICE_NAME" == "api" ]]; then
  APP_ENV_ARGS+=(
    -e Authentication__JwtSettings__SecretKey="$JWT_SECRET"
    -e Authentication__JwtSettings__Issuer="ci-smoke-test"
    -e Authentication__JwtSettings__Audience="ci-smoke-test"
    -e Authentication__Google__ClientId="ci-smoke-test"
    -e Authentication__Google__ClientSecret="ci-smoke-test"
    -e AdminUser__Password="$ADMIN_PASSWORD"
  )
else
  APP_ENV_ARGS+=(
    -e ApiSettings__BaseUrl="http://${API_AUX_CONTAINER}:8080"
  )
fi

docker run -d \
  --name "$APP_CONTAINER" \
  --network "$NETWORK_NAME" \
  -p "${HOST_PORT}:8080" \
  "${APP_ENV_ARGS[@]}" \
  "$IMAGE_REF" >/dev/null

wait_for_healthy "$APP_CONTAINER" 90

echo "Checking GET /health/live (blocking)..."
if ! curl -f -sS "http://localhost:${HOST_PORT}/health/live" >/dev/null; then
  echo "::error::/health/live did not return 200 for ${SERVICE_NAME}"
  exit 1
fi
echo "/health/live OK for ${SERVICE_NAME}."

echo "Checking GET /health (informational only)..."
health_status_code="$(curl -s -o /dev/null -w '%{http_code}' "http://localhost:${HOST_PORT}/health" || echo "000")"
echo "Informational: /health returned HTTP ${health_status_code} for ${SERVICE_NAME} (does not affect the job result)."

echo "Smoke test passed for ${SERVICE_NAME}."
