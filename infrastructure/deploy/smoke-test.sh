#!/usr/bin/env bash
#
# Polls the deployed homelab instance's health endpoints to confirm a
# deployment (or rollback) actually left the application in a working state,
# beyond "the Portainer webhook/API call returned 200".
#
# Checks, in order:
#   1. GET $HOMELAB_WEB_URL/health/live  - the Web process is up and serving
#      requests.
#   2. GET $HOMELAB_WEB_URL/health/ready - Web's own dependencies (its
#      database) AND, transitively, Api's availability (see
#      ApiAvailabilityHealthCheck, issue #41) are healthy. Hitting only Web
#      from outside is enough to also verify Api, without exposing Api's own
#      health endpoint publicly.
#
# Unlike the pre-publish smoke test in .github/scripts/smoke-test.sh (which
# treats /health as purely informational because it runs against an
# ephemeral, cold-starting SQL Server container), both checks here are
# blocking: the homelab's SQL Server is already running before a redeploy,
# so there is no database cold-start flakiness to account for - a failure
# here means the deployment is genuinely broken.
#
# Both checks retry up to MAX_ATTEMPTS times, POLL_INTERVAL_SECONDS apart
# (default: 15s x 6 attempts =~ 90s per check), to tolerate the few seconds
# it takes Docker to recreate a container after a Portainer redeploy.
#
# This script is intentionally free of any "on failure, compute the rollback
# tag" logic - that belongs to the caller (cd.yml's post-deploy-smoke-test
# job), since rollback.yml's post-rollback-smoke-test job reuses this same
# script but must NOT attempt a "rollback of the rollback" on failure (see
# design doc, issue #45, Risks & Open Decisions).
#
# Usage:
#   smoke-test.sh
#
# Requires HOMELAB_WEB_URL in the environment (public URL of Web, with or
# without a trailing slash).

set -euo pipefail

HOMELAB_WEB_URL="${HOMELAB_WEB_URL:-}"
POLL_INTERVAL_SECONDS="${POLL_INTERVAL_SECONDS:-15}"
MAX_ATTEMPTS="${MAX_ATTEMPTS:-6}"

if [[ -z "$HOMELAB_WEB_URL" ]]; then
  echo "::error::HOMELAB_WEB_URL is not set" >&2
  exit 1
fi

# Polls a single health endpoint until it returns HTTP 200 or MAX_ATTEMPTS is
# reached.
poll_endpoint() {
  local path="$1"
  local url="${HOMELAB_WEB_URL%/}${path}"
  local attempt

  for ((attempt = 1; attempt <= MAX_ATTEMPTS; attempt++)); do
    echo "Checking GET ${url} (attempt ${attempt}/${MAX_ATTEMPTS})..."
    if curl -f -sS "$url" >/dev/null; then
      echo "${path} OK."
      return 0
    fi

    if (( attempt < MAX_ATTEMPTS )); then
      sleep "$POLL_INTERVAL_SECONDS"
    fi
  done

  echo "::error::${path} did not return 200 after ${MAX_ATTEMPTS} attempts (~$((MAX_ATTEMPTS * POLL_INTERVAL_SECONDS))s)" >&2
  return 1
}

poll_endpoint "/health/live"
poll_endpoint "/health/ready"

echo "Smoke test passed against ${HOMELAB_WEB_URL}."
