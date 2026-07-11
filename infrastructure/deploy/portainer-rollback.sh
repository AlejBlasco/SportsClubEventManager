#!/usr/bin/env bash
#
# Rolls back the homelab production stack to a previously deployed image tag
# by calling the Portainer REST API directly (Business Edition), instead of
# requiring a human to edit the stack's environment variables in the
# Portainer UI.
#
# Portainer's GitOps redeploy webhook (used by cd.yml's normal "deploy" job)
# only re-pulls images using the stack's *current* configuration - it has no
# way to change APP_VERSION, so it cannot be reused for a rollback to a
# specific older tag. This script instead:
#   1. Authenticates against the Portainer API with an API key
#      (X-API-Key header).
#   2. Locates the production stack by name (GET /api/stacks).
#   3. Updates the stack's APP_VERSION environment variable to the
#      requested tag and forces pullImage=true (PUT /api/stacks/{id}),
#      which also triggers the redeploy - no further action is needed.
#
# The image for the requested tag must already exist in GHCR (pushed by a
# previous, successful run of cd.yml's build-and-push job) - this script
# never builds or pushes anything, it only repoints the stack at an image
# that is already there.
#
# Usage:
#   portainer-rollback.sh <sha-hash>
#
# Requires PORTAINER_API_URL and PORTAINER_API_KEY in the environment, and
# optionally PORTAINER_STACK_NAME (defaults to
# "sportsclubeventmanager-prod" - see infrastructure/deploy/DEPLOYMENT_RUNBOOK.md
# if the real stack name in Portainer differs).

set -euo pipefail

TARGET_VERSION="${1:-}"
PORTAINER_API_URL="${PORTAINER_API_URL:-}"
PORTAINER_API_KEY="${PORTAINER_API_KEY:-}"
PORTAINER_STACK_NAME="${PORTAINER_STACK_NAME:-sportsclubeventmanager-prod}"

if [[ -z "$TARGET_VERSION" ]]; then
  echo "::error::Usage: portainer-rollback.sh <sha-hash>" >&2
  exit 1
fi

if [[ -z "$PORTAINER_API_URL" || -z "$PORTAINER_API_KEY" ]]; then
  echo "::error::PORTAINER_API_URL and PORTAINER_API_KEY must be set" >&2
  exit 1
fi

API_BASE="${PORTAINER_API_URL%/}/api"

echo "Locating stack '${PORTAINER_STACK_NAME}' in Portainer..."
stacks_json="$(curl -f -sS -H "X-API-Key: ${PORTAINER_API_KEY}" "${API_BASE}/stacks")"

stack_id="$(echo "$stacks_json" | jq -r --arg name "$PORTAINER_STACK_NAME" '.[] | select(.Name == $name) | .Id' | head -n1)"
endpoint_id="$(echo "$stacks_json" | jq -r --arg name "$PORTAINER_STACK_NAME" '.[] | select(.Name == $name) | .EndpointId' | head -n1)"

if [[ -z "$stack_id" || "$stack_id" == "null" ]]; then
  echo "::error::Stack '${PORTAINER_STACK_NAME}' not found in Portainer (checked ${API_BASE}/stacks). Set PORTAINER_STACK_NAME if the real stack name differs." >&2
  exit 1
fi

echo "Found stack '${PORTAINER_STACK_NAME}' (id=${stack_id}, endpointId=${endpoint_id})."

echo "Fetching current stack configuration..."
stack_details_json="$(curl -f -sS -H "X-API-Key: ${PORTAINER_API_KEY}" "${API_BASE}/stacks/${stack_id}")"
stack_file_content="$(curl -f -sS -H "X-API-Key: ${PORTAINER_API_KEY}" "${API_BASE}/stacks/${stack_id}/file" | jq -r '.StackFileContent')"

# Merge the requested APP_VERSION into the stack's existing environment
# variables, replacing any previous APP_VERSION entry so the rest of the
# stack's configured env (secrets references, ports, etc.) is preserved.
updated_env_json="$(echo "$stack_details_json" | jq -c --arg version "$TARGET_VERSION" '
  ((.Env // []) | map(select(.name != "APP_VERSION"))) + [{"name": "APP_VERSION", "value": $version}]
')"

echo "Updating stack '${PORTAINER_STACK_NAME}' to APP_VERSION=${TARGET_VERSION} and forcing a re-pull..."
update_payload="$(jq -n \
  --arg stackFileContent "$stack_file_content" \
  --argjson env "$updated_env_json" \
  '{stackFileContent: $stackFileContent, env: $env, prune: false, pullImage: true}')"

response_body="$(mktemp)"
http_status="$(curl -sS -o "$response_body" -w '%{http_code}' \
  -X PUT \
  -H "X-API-Key: ${PORTAINER_API_KEY}" \
  -H "Content-Type: application/json" \
  "${API_BASE}/stacks/${stack_id}?endpointId=${endpoint_id}" \
  -d "$update_payload")"

if [[ "$http_status" != "200" ]]; then
  echo "::error::Portainer API returned HTTP ${http_status} while updating stack '${PORTAINER_STACK_NAME}'" >&2
  cat "$response_body" >&2 || true
  rm -f "$response_body"
  exit 1
fi

rm -f "$response_body"
echo "Stack '${PORTAINER_STACK_NAME}' updated to APP_VERSION=${TARGET_VERSION}. Portainer is redeploying it now."
