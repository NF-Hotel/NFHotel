#!/usr/bin/env bash
# Keeps the reference pages of MIL-049 true: every workflow in .github/workflows and every
# composite action in .github/actions is named on docs/wiki/Workflows.md, every service of
# ops/deploy/*/docker-compose*.yml on docs/wiki/Containers-and-Images.md, and every alert rule
# of ops/deploy/monitoring on docs/wiki/Alerts.md. docs/wiki/Technology-Choices.md (MIL-050)
# must name every workflow, and every third-party image of the compose files and Dockerfiles
# in its image index. Containers-and-Images.md must also name the tag that each compose file
# and Dockerfile pins (MIL-065), so a pin that moves without the page fails here; a copy of
# the page with one tag changed shows that the check catches a stale tag. A name counts when
# the page has it in backticks, so a word in a sentence does not hide a missing entry.
#
# Run it from anywhere in the repository (it needs .github, docs and ops, not only ops):
#   bash ops/tests/test-wiki-coverage.sh
# Exit:  0 every name is on its page; 1 a name is missing or a source has no names
# Needs: bash, awk, find, grep, mktemp, sed

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

# has_name <file> <name>: the file has the name in backticks.
has_name() {
  grep -qF "\`$2\`" "$1"
}

# check_names <page> <kind> <name>...: each name must be on the page in backticks.
check_names() {
  local page="$1" kind="$2"
  shift 2
  [[ "$#" -gt 0 ]] && ok "$kind: found $# to check" || ko "$kind: found none, so the source moved"
  local name
  for name in "$@"; do
    has_name "$WIKI/$page" "$name" && ok "$kind $name is on $page" || ko "$kind $name is missing from $page"
  done
}

# third_party_refs: the image of every `image:` line of the compose files (also the folded
# `image: >-` form) and of every FROM of the Dockerfiles, at any depth under ops/deploy (the
# socket proxy has its own folder in the Alloy stack), with its tag and without its digest; the
# images of NF Hotel itself (set through a variable, an anchor or a :local tag) are left out.
third_party_refs() {
  {
    awk '/^[[:space:]]+image:[[:space:]]*>-[[:space:]]*$/ { getline; print $1; next }
         /^[[:space:]]+image:/ { print $2 }' "$ROOT"/ops/deploy/*/docker-compose*.yml
    find "$ROOT/Dockerfile" "$ROOT/Dockerfile.api" "$ROOT/ops/deploy" -type f -name 'Dockerfile*' \
      -exec awk '/^FROM / { print $2 }' {} +
  } | tr -d "\"'" | grep -vE '^[$*]|:local$' | sed -E 's/@sha256:.*//'
}

# third_party_images: each third-party image once, without its tag.
third_party_images() {
  third_party_refs | sed -E 's/:[^/]*$//' | sort -u
}

# third_party_tags: each third-party image once with the tag it is pinned to; an image pinned by
# its digest alone has no tag to name.
third_party_tags() {
  third_party_refs | grep -E ':[^/:]+$' | sort -u
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

check_names Technology-Choices.md workflow "${workflows[@]}"

mapfile -t images < <(third_party_images)
check_names Technology-Choices.md "third-party image" "${images[@]}"

mapfile -t tags < <(third_party_tags)
check_names Containers-and-Images.md "pinned image tag" "${tags[@]}"

# The tag check must catch a stale tag. A copy of the page in which one tag that the page names
# gets a suffix must miss that tag: a match without the closing backtick would still find it.
probe=""
for tag in "${tags[@]}"; do
  has_name "$WIKI/Containers-and-Images.md" "$tag" && probe="$tag" && break
done
if [[ -z "$probe" ]]; then
  ko "stale tag: Containers-and-Images.md names no pinned tag, so there is none to change"
else
  stale_page="$(mktemp)"
  trap 'rm -f "$stale_page"' EXIT
  awk -v old="\`$probe\`" -v new="\`$probe-stale\`" '
    { while ((i = index($0, old)) > 0) $0 = substr($0, 1, i - 1) new substr($0, i + length(old)); print }' \
    "$WIKI/Containers-and-Images.md" >"$stale_page"
  ! has_name "$stale_page" "$probe" && has_name "$stale_page" "$probe-stale" &&
    ok "stale tag: a copy of Containers-and-Images.md with $probe-stale fails the tag check" ||
    ko "stale tag: a copy of Containers-and-Images.md with $probe-stale passes the tag check"
fi

echo "$pass passed, $fail failed"
[[ "$fail" -eq 0 ]]
