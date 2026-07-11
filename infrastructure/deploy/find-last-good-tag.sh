#!/usr/bin/env bash
#
# Finds the sha-<hash> image tag of the last successful deployment to the
# "homelab-production" GitHub Environment, strictly before a given commit
# sha.
#
# Used by:
#   - cd.yml's post-deploy-smoke-test job, to print rollback guidance in the
#     job summary when the smoke test fails.
#   - optionally, a human operator running it locally to get a candidate
#     value for rollback.yml's "version" input.
#
# Usage:
#   find-last-good-tag.sh <CURRENT_SHA>
#
# Requires `gh` authenticated (GH_TOKEN/GITHUB_TOKEN in the environment)
# with read access to the repository's Deployments API, and `jq`.

set -euo pipefail

REPO="AlejBlasco/SportsClubEventManager"
CURRENT_SHA="${1:-}"

if [[ -z "$CURRENT_SHA" ]]; then
  echo "::error::Usage: find-last-good-tag.sh <CURRENT_SHA>" >&2
  exit 1
fi

echo "Looking up deployments for environment 'homelab-production'..." >&2

deployments_json="$(gh api "repos/${REPO}/deployments?environment=homelab-production&per_page=100")"
deployment_count="$(echo "$deployments_json" | jq 'length')"

# Deployments are returned newest first. Walk them, skip the current commit,
# and keep the first one whose latest status is "success".
last_good_sha=""

for ((i = 0; i < deployment_count; i++)); do
  deployment_id="$(echo "$deployments_json" | jq -r ".[$i].id")"
  deployment_sha="$(echo "$deployments_json" | jq -r ".[$i].sha")"

  if [[ "$deployment_sha" == "$CURRENT_SHA" ]]; then
    continue
  fi

  latest_status="$(gh api "repos/${REPO}/deployments/${deployment_id}/statuses?per_page=1" | jq -r '.[0].state // empty')"

  if [[ "$latest_status" == "success" ]]; then
    last_good_sha="$deployment_sha"
    break
  fi
done

if [[ -z "$last_good_sha" ]]; then
  echo "::error::No previous successful deployment found for environment 'homelab-production' before ${CURRENT_SHA}" >&2
  exit 1
fi

short_sha="${last_good_sha:0:7}"
echo "sha-${short_sha}"
