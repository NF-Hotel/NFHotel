#!/usr/bin/env bash
# Check that the logs of the throwaway stack hold no personal data of the guest that the
# end-to-end tests booked as (MIL-036 task 4, the log policy of the wiki page Incidents).
# The logs go to Loki and are kept 14 days (ADR-0020), so a name, an e-mail address or a phone
# number in them is personal data kept without a purpose. Run after the end-to-end tests,
# before the stack is removed. The tokens of the verification of a booking by email, the
# links that carry them, and the emails that each test of the verification makes for itself
# must not be in a log either (MIL-088 criterion 6).
#
# Usage: check-log-privacy.sh [--name <prefix>] [--values-file <file>] [--pattern <regex>]...
#          [<value>...]
#   --name         prefix of the stack's containers (default nfhotel-e2e)
#   --values-file  a file with one value per line, such as the tokens that the tests wrote; a
#                  value there may start with -, and an empty line is skipped
#   --pattern      an extended regular expression (grep -E) for values that the tests made up
#                  as they ran, such as their emails e2e-<guid>@example.com; a log line that
#                  matches it is found. It may be given more than once
#   <value>        a piece of personal data that the tests entered, for example an e-mail address
# Env:   NFHOTEL_DOCKER (default docker; for the tests)
# Exit:  0 no value is in any log; 1 a value was found (the container is named, not the line)
#        or no container was found; 2 usage error
# Needs: bash 4+, docker

set -euo pipefail

docker="${NFHOTEL_DOCKER:-docker}"
prefix="nfhotel-e2e"
values=()
patterns=()

usage() {
  echo "usage: check-log-privacy.sh [--name <prefix>] [--values-file <file>] [--pattern <regex>]... [<value>...]" >&2
  exit 2
}

while (($#)); do
  case "$1" in
    --name)
      (($# >= 2)) || usage
      prefix="$2"
      shift 2
      ;;
    --values-file)
      if (($# < 2)) || [[ ! -f "$2" ]]; then usage; fi
      while IFS= read -r value || [[ -n "$value" ]]; do
        value="${value%$'\r'}"
        [[ -z "$value" ]] || values+=("$value")
      done <"$2"
      shift 2
      ;;
    --pattern)
      if (($# < 2)) || [[ -z "$2" ]]; then usage; fi
      patterns+=("$2")
      shift 2
      ;;
    -*) usage ;;
    *)
      values+=("$1")
      shift
      ;;
  esac
done
((${#values[@]} + ${#patterns[@]})) || usage

mapfile -t containers < <("$docker" ps --all --format '{{.Names}}' | grep "^$prefix" || true)
((${#containers[@]})) || {
  echo "error: no container of the stack $prefix" >&2
  exit 1
}

found=0
for container in "${containers[@]}"; do
  logs="$("$docker" logs "$container" 2>&1 || true)"
  # The line itself is not printed: it holds the personal data.
  for value in ${values[@]+"${values[@]}"}; do
    if [[ "$logs" == *"$value"* ]]; then
      echo "FOUND personal data in the log of $container (one of the test values)"
      found=1
    fi
  done
  for pattern in ${patterns[@]+"${patterns[@]}"}; do
    if grep -Eq -- "$pattern" <<<"$logs"; then
      echo "FOUND personal data in the log of $container (a match of $pattern)"
      found=1
    fi
  done
done
((found == 0)) && echo "no personal data of the test guest in the logs of ${#containers[@]} containers"
exit "$found"
