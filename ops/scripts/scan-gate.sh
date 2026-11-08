#!/usr/bin/env bash
# The container scan gate of DSA-001 section 8 (MIL-022 task 1): read a Trivy JSON
# report and fail on any critical vulnerability, any high one with a fix available, and
# any secret found in the image (a key, a token or a password in a file of a layer), so
# no image with a secret reaches the registry, which may be public. Trivy has already
# left out what security/trivyignore.yaml accepts.
#
# With --check-ignore it checks that ignore file instead: every entry has an id, a
# statement (the reason) and an expired_at date at most 30 days after <today>, so an
# accepted risk always runs out and comes back to the gate. Before that, the file must be in
# the format of its header, the YAML that Trivy reads: a value that YAML cannot read (a plain
# value with ": ", or one that starts with an indicator), a key that Trivy does not read or a
# key twice fails the check with its line number (MIL-066 task 5).
#
# With --fixable it prints the report with only the vulnerabilities that have a fixed version,
# and every secret: what code scanning shows, because someone can act on it (ADR-0032). The
# full report stays the record of the release.
#
# Usage: scan-gate.sh <trivy report.json>
#        scan-gate.sh --check-ignore <trivyignore.yaml> [<today as YYYY-MM-DD>]
#        scan-gate.sh --fixable <trivy report.json>
# Exit:  0 the gate passes; 1 it fails (the findings are printed); 2 usage error
# Needs: bash 4+, jq, GNU date

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=ops/scripts/lib/common.sh
. "$ROOT/scripts/lib/common.sh"

readonly MAX_DAYS=30

# The YAML of the ignore file, as its header describes it (MIL-066 task 5): a value in double or
# single quotes, or a plain value that does not start with a YAML indicator and has no ": ".
readonly DQ='^"(([^"\\]|\\.)*)"( +#.*)? *$'
readonly SQ="^'(([^']|'')*)'( +#.*)? *\$"
readonly INDICATOR='^[][?:,{}#&*!|>%@`-]'
readonly FLOW='^ *\[ *("([^"\\]|\\.)*" *(, *"([^"\\]|\\.)*" *)*)?\] *(#.*)?$'

# scalar_of <text>: the YAML value of a scalar written as <text>, without a trailing comment, in
# $scalar; fails with the reason in $why when YAML cannot read <text> as one value.
scalar_of() {
  local text="${1#"${1%%[![:space:]]*}"}"
  scalar="" why=""
  if [[ "$text" == \"* ]]; then
    [[ "$text" =~ $DQ ]] || why="a double-quoted value that does not end with its closing quote"
    scalar="${BASH_REMATCH[1]:-}"
  elif [[ "$text" == \'* ]]; then
    [[ "$text" =~ $SQ ]] || why="a single-quoted value that does not end with its closing quote"
    scalar="${BASH_REMATCH[1]:-}"
  else
    [[ "$text" == "#"* ]] || scalar="${text%% #*}"
    scalar="${scalar%"${scalar##*[![:space:]]}"}"
    if [[ "$scalar" =~ $INDICATOR ]]; then
      why="a plain value that starts with ${scalar:0:1}, which YAML reads as syntax (quote the value)"
    elif [[ "$scalar" == *": "* || "$scalar" == *: ]]; then
      why="a plain value with \": \" in it, which YAML cannot read (quote the value)"
    fi
  fi
  [[ -z "$why" ]]
}

# read_entries <file>: check that the file is in the format of its header, the YAML that Trivy
# reads, and fill ids, statements and expiries with one element per entry. Prints FAIL with the
# line number for each line outside it: a value that YAML cannot read, a key that Trivy does not
# read (such as path for paths, which would make Trivy ignore the finding in every image), or a
# section or a key twice. Fails when there is one.
read_entries() {
  local file="$1" n=0 bad=0 line key value scalar why section="" sections=" " keys="" list=0 i=-1
  local re_section='^(vulnerabilities|misconfigurations|secrets|licenses):[[:space:]]*(#.*)?$'
  local re_entry='^  - id:( (.*))?$' re_key='^    ([A-Za-z_]+):( (.*))?$' re_item='^      - (.*)$'
  ids=() statements=() expiries=()
  while IFS= read -r line || [[ -n "$line" ]]; do
    n=$((n + 1))
    line="${line%$'\r'}"
    [[ "$line" =~ ^[[:space:]]*(#.*)?$ ]] && continue
    why=""
    if [[ "$line" =~ $re_section ]]; then
      key="${BASH_REMATCH[1]}"
      [[ "$sections" != *" $key "* ]] || why="the section $key a second time"
      sections+="$key " section="$key" keys="" list=0
    elif [[ "$line" =~ $re_entry ]]; then
      if [[ -z "$section" ]]; then
        why="an entry before its section, such as vulnerabilities:"
      else
        scalar_of "${BASH_REMATCH[2]:-}" && [[ -z "$scalar" ]] && why="an entry without an id"
        i=$((i + 1))
        ids[i]="$scalar" statements[i]="" expiries[i]="" keys=" id " list=0
      fi
    elif [[ "$line" =~ $re_key ]]; then
      key="${BASH_REMATCH[1]}" value="${BASH_REMATCH[3]:-}" list=0
      if [[ -z "$keys" ]]; then
        why="the key $key outside an entry"
      elif [[ " statement expired_at paths purls " != *" $key "* ]]; then
        why="the key $key, which Trivy does not read (id, paths, purls, statement, expired_at)"
      elif [[ "$keys" == *" $key "* ]]; then
        why="the key $key a second time in the entry ${ids[i]}"
      else
        keys+="$key "
        case "$key" in
          paths | purls)
            if [[ "$value" =~ ^[[:space:]]*(#.*)?$ ]]; then
              list=1
            elif [[ ! "$value" =~ $FLOW ]]; then
              why="$key in [ ] that are not double-quoted items (or write one item per line)"
            fi
            ;;
          statement) scalar_of "$value" && statements[i]="$scalar" ;;
          expired_at) scalar_of "$value" && expiries[i]="$scalar" ;;
        esac
      fi
    elif [[ "$line" =~ $re_item ]]; then
      if ((list == 0)); then
        why="a list item outside paths or purls"
      elif scalar_of "${BASH_REMATCH[1]}"; then
        [[ -n "$scalar" ]] || why="an empty item of the list"
      fi
    else
      why="a line outside the format of the file (see its header)"
    fi
    if [[ -n "$why" ]]; then
      echo "FAIL  line $n: $why"
      bad=1
    fi
  done <"$file"
  ((bad == 0))
}

# check_ignore <file> <today>: first the YAML (read_entries), because a file that Trivy cannot
# parse stops the scan of every release after the merge; then each entry, with its own
# statement and expired_at.
check_ignore() {
  local file="$1" today="$2" bad=0 i id statement expired days today_s
  local -a ids statements expiries
  [[ -f "$file" ]] || die "no ignore file $file"
  today_s="$(date -u -d "$today" +%s)" || die "'$today' is not a date"
  read_entries "$file" || {
    echo "FAIL  $file is not in the format that Trivy reads, as its header describes; the entries were not checked"
    exit 1
  }
  for i in "${!ids[@]}"; do
    id="${ids[i]}" statement="${statements[i]}" expired="${expiries[i]}"
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
  done
  ((bad == 0)) || exit 1
  echo "==> $file: every accepted finding has a reason and expires within $MAX_DAYS days"
}

case "${1:-}" in
  --fixable)
    [[ $# -eq 2 && -f "$2" ]] || {
      usage >&2
      exit 2
    }
    jq '.Results |= ((. // []) | map(if .Vulnerabilities then
          .Vulnerabilities |= map(select((.FixedVersion // "") != "")) else . end))' "$2"
    exit 0
    ;;
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
secrets="$(jq -r '
  [.Results[]? | .Target as $t | .Secrets[]? | "SECRET  \(.RuleID) (\(.Severity)) in \($t)"]
  | unique | .[]' "$report")"
if [[ -n "$secrets" ]]; then
  echo "FAIL  $image holds secrets (DSA-001 section 8); remove them from the image and rotate them:"
  while IFS= read -r line; do echo "      $line"; done <<<"$secrets"
  exit 1
fi
if [[ -n "$findings" ]]; then
  echo "FAIL  $image has findings that block the release (DSA-001 section 8):"
  while IFS= read -r line; do echo "      $line"; done <<<"$findings"
  echo "      Fix them, or accept one in security/trivyignore.yaml with a reason and an expiry."
  exit 1
fi
echo "PASS  $image: no secret, no critical finding and no high finding with a fix"
