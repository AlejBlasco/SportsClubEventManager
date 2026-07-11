#!/usr/bin/env bash
#
# Extracts a single version section from CHANGELOG.md (Keep a Changelog
# format) and prints its content on stdout, without the "## [X.Y.Z] - date"
# header itself.
#
# Used by .github/workflows/release.yml to build the body of a GitHub
# Release from CHANGELOG.md instead of GitHub's auto-generated notes.
#
# Usage:
#   extract-changelog-section.sh <version-without-v> <path-to-changelog>
#
# Exit codes:
#   0 -> header found and the section has real content (at least one
#        non-blank line between the header and the next section/footer)
#   1 -> header "## [X.Y.Z] - ..." not found in CHANGELOG.md
#   2 -> header found, but the section has no real content (blank lines only)

set -euo pipefail

VERSION="${1:-}"
CHANGELOG_PATH="${2:-}"

if [[ -z "$VERSION" || -z "$CHANGELOG_PATH" ]]; then
  echo "::error::Usage: extract-changelog-section.sh <version-without-v> <path-to-changelog>" >&2
  exit 1
fi

if [[ ! -f "$CHANGELOG_PATH" ]]; then
  echo "::error::Changelog file not found: $CHANGELOG_PATH" >&2
  exit 1
fi

# Escape regex metacharacters in VERSION (dots, mainly) so a literal version
# like "0.2.0" is matched exactly, not loosely (e.g. against "0X2X0").
ESCAPED_VERSION="$(printf '%s' "$VERSION" | sed 's/[.[\*^$]/\\&/g')"
HEADER_PATTERN="^## \[${ESCAPED_VERSION}\]"

if ! grep -qE "$HEADER_PATTERN" "$CHANGELOG_PATH"; then
  echo "::error::Section '## [${VERSION}]' not found in ${CHANGELOG_PATH}." >&2
  exit 1
fi

# Print everything strictly between the matched header and whichever comes
# first: the next section header ("## [...]") or the reference-style link
# definitions block at the foot of the file ("[x.y.z]: https://...", as used
# today at the bottom of CHANGELOG.md). This keeps those footer links out of
# the body when the matched section is the last one in the file.
SECTION="$(awk -v pattern="$HEADER_PATTERN" '
  BEGIN { capturing = 0 }
  $0 ~ pattern { capturing = 1; next }
  capturing && ($0 ~ /^## \[/ || $0 ~ /^\[[^]]+\]:/) { capturing = 0 }
  capturing { print }
' "$CHANGELOG_PATH")"

# Trim leading/trailing blank lines while keeping internal formatting intact.
TRIMMED="$(printf '%s\n' "$SECTION" | sed -e '/./,$!d' -e ':a' -e '/^\n*$/{$d;N;ba' -e '}')"

if [[ -z "$(printf '%s' "$TRIMMED" | tr -d '[:space:]')" ]]; then
  echo "::error::Section '## [${VERSION}]' was found in ${CHANGELOG_PATH} but has no real content (empty section)." >&2
  exit 2
fi

printf '%s\n' "$TRIMMED"
exit 0
