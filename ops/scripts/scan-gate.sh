#!/usr/bin/env bash
# The container scan gate of DSA-001 section 8 (MIL-022 task 1): read a Trivy JSON
# report and fail on any critical vulnerability, or any high one with a fix available.
# Trivy has already left out what security/trivyignore.yaml accepts.
#
# With --check-ignore it checks that ignore file instead: every entry has an id, a
# statement (the reason) and an expired_at date at most 30 days after <today>, so an
# accepted risk always runs out and comes back to the gate.
#
# Usage: scan-gate.sh <trivy report.json>
#        scan-gate.sh --check-ignore <trivyignore.yaml> [<today as YYYY-MM-DD>]
# Exit:  0 the gate passes; 1 it fails (the findings are printed); 2 usage error
# Needs: bash 4+, jq, GNU date

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=scripts/lib/common.sh
. "$ROOT/scripts/lib/common.sh"

readonly MAX_DAYS=30

# check_ignore <file> <today>: the entries of a Trivy YAML ignore file, one
# "- id:" per entry, each with its own statement and expired_at.
check_ignore() {
  local file="$1" today="$2" bad=0 id statement expired days today_s
  [[ -f "$file" ]] || die "no ignore file $file"
  today_s="$(date -u -d "$today" +%s)" || die "'$today' is not a date"
  while IFS='|' read -r id statement expired; do
    if [[ -z "$statement" ]]; then
      echo "FAIL  $id has no statement (the reason it is accepted)"
      bad=1
    fi
    if [[ ! "$expired" =~ ^[0-9]{4}-[0-9]{2}-[0-9]{2}$ ]]; then
      echo "FAIL  $id has no expired_at date"
      bad=1
      continue
    fi
    days=$((($(date -u -d "$expired" +%s) - today_s) / 86400))
    if ((days > MAX_DAYS)); then
      echo "FAIL  $id expires in $days days, more than $MAX_DAYS"
      bad=1
    elif ((days < 0)); then
      echo "NOTE  $id expired on $expired; Trivy no longer ignores it"
    fi
  done < <(awk '
    function flush() { if (id != "") print id "|" st "|" ex; id = ""; st = ""; ex = "" }
    /^[[:space:]]*- id:/ { flush(); sub(/.*- id:[[:space:]]*/, ""); id = $0; next }
    /^[[:space:]]+statement:/ { sub(/.*statement:[[:space:]]*/, ""); st = $0; next }
    /^[[:space:]]+expired_at:/ { sub(/.*expired_at:[[:space:]]*/, ""); ex = $0; next }
    END { flush() }' "$file")
  ((bad == 0)) || exit 1
  echo "==> $file: every accepted finding has a reason and expires within $MAX_DAYS days"
}

case "${1:-}" in
  --check-ignore)
    [[ $# -ge 2 ]] || {
      usage >&2
      exit 2
    }
    check_ignore "$2" "${3:-$(date -u +%F)}"
    exit 0
    ;;
  -h | --help)
    usage
    exit 0
    ;;
  "")
    usage >&2
    exit 2
    ;;
esac

report="$1"
[[ -f "$report" ]] || die "no Trivy report $report"
image="$(jq -r '.ArtifactName // "the image"' "$report")" || die "$report is not a Trivy JSON report"
findings="$(jq -r '
  [.Results[]?.Vulnerabilities[]?
   | select(.Severity == "CRITICAL" or (.Severity == "HIGH" and (.FixedVersion // "") != ""))]
  | unique_by(.VulnerabilityID + " " + .PkgName)
  | .[] | "\(.Severity)  \(.VulnerabilityID)  \(.PkgName) \(.InstalledVersion) -> \(.FixedVersion // "" | if . == "" then "no fix" else . end)"' "$report")"
if [[ -n "$findings" ]]; then
  echo "FAIL  $image has findings that block the release (DSA-001 section 8):"
  while IFS= read -r line; do echo "      $line"; done <<<"$findings"
  echo "      Fix them, or accept one in security/trivyignore.yaml with a reason and an expiry."
  exit 1
fi
echo "PASS  $image: no critical finding and no high finding with a fix"
