#!/usr/bin/env bash
# Keeps the reference pages of MIL-049 true: every workflow in .github/workflows and every
# composite action in .github/actions is named on docs/wiki/Workflows.md, every service of
# ops/deploy/*/docker-compose*.yml on docs/wiki/Containers-and-Images.md, and every alert rule
# of ops/deploy/monitoring on docs/wiki/Alerts.md. A name counts when the page has it in
# backticks, so a word in a sentence does not hide a missing entry.
#
# Run it from anywhere in the repository (it needs .github, docs and ops, not only ops):
#   bash ops/tests/test-wiki-coverage.sh
# Exit:  0 every name is on its page; 1 a name is missing or a source has no names
# Needs: bash, awk, grep

# shellcheck disable=SC2015 # ok and ko always return 0, so `test && ok || ko` runs
# exactly one of them; the short form keeps each check on one line.
# No -e: every check runs and is counted, and the exit code says whether one failed.
set -uo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
WIKI="$ROOT/docs/wiki"
pass=0
fail=0
ok() {
  echo "PASS  $1"
  pass=$((pass + 1))
}
ko() {
  echo "FAIL  $1"
  fail=$((fail + 1))
}

# check_names <page> <kind> <name>...: each name must be on the page in backticks.
check_names() {
  local page="$1" kind="$2"
  shift 2
  [[ "$#" -gt 0 ]] && ok "$kind: found $# to check" || ko "$kind: found none, so the source moved"
  local name
  for name in "$@"; do
    grep -qF "\`$name\`" "$WIKI/$page" && ok "$kind $name is on $page" || ko "$kind $name is missing from $page"
  done
}

# compose_services <file>: the keys directly under the top-level `services:`.
compose_services() {
  awk '/^services:/ { s = 1; next }
       /^[^ #]/ { s = 0 }
       s && /^  [A-Za-z0-9_.-]+:[[:space:]]*$/ { sub(/^  /, ""); sub(/:.*/, ""); print }' "$1"
}

mapfile -t workflows < <(find "$ROOT/.github/workflows" -maxdepth 1 -type f \( -name '*.yaml' -o -name '*.yml' \) -printf '%f\n' | sort)
check_names Workflows.md workflow "${workflows[@]}"

mapfile -t actions < <(find "$ROOT/.github/actions" -mindepth 1 -maxdepth 1 -type d -printf '%f\n' | sort)
check_names Workflows.md "composite action" "${actions[@]}"

mapfile -t services < <(for f in "$ROOT"/ops/deploy/*/docker-compose*.yml; do compose_services "$f"; done | sort -u)
check_names Containers-and-Images.md "compose service" "${services[@]}"

mapfile -t alerts < <(cat "$ROOT"/ops/deploy/monitoring/prometheus/rules/*.yml "$ROOT"/ops/deploy/monitoring/loki/rules/*/*.yml |
  awk '/^[[:space:]]*- alert:/ { print $3 }' | sort -u)
check_names Alerts.md "alert rule" "${alerts[@]}"

echo "$pass passed, $fail failed"
[[ "$fail" -eq 0 ]]
