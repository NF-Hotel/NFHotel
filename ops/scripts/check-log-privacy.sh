#!/usr/bin/env bash
# Check that the logs of the throwaway stack hold no personal data of the guest that the
# end-to-end tests booked as (MIL-036 task 4, the log policy of the wiki page Incidents).
# The logs go to Loki and are kept 14 days (ADR-0020), so a name, an e-mail address or a phone
# number in them is personal data kept without a purpose. Run after the end-to-end tests,
# before the stack is removed.
#
# Usage: check-log-privacy.sh [--name <prefix>] <value>...
#   --name   prefix of the stack's containers (default nfhotel-e2e)
#   <value>  a piece of personal data that the tests entered, for example an e-mail address
# Env:   NFHOTEL_DOCKER (default docker; for the tests)
# Exit:  0 no value is in any log; 1 a value was found (the container is named, not the line)
#        or no container was found; 2 usage error
# Needs: bash 4+, docker

set -euo pipefail

docker="${NFHOTEL_DOCKER:-docker}"
prefix="nfhotel-e2e"
values=()

usage() {
  echo "usage: check-log-privacy.sh [--name <prefix>] <value>..." >&2
  exit 2
}

while (($#)); do
  case "$1" in
    --name)
      (($# >= 2)) || usage
      prefix="$2"
      shift 2
      ;;
    -*) usage ;;
    *)
      values+=("$1")
      shift
      ;;
  esac
done
((${#values[@]})) || usage

mapfile -t containers < <("$docker" ps --all --format '{{.Names}}' | grep "^$prefix" || true)
((${#containers[@]})) || {
  echo "error: no container of the stack $prefix" >&2
  exit 1
}

found=0
for container in "${containers[@]}"; do
  logs="$("$docker" logs "$container" 2>&1 || true)"
  for value in "${values[@]}"; do
    # The line itself is not printed: it holds the personal data.
    if [[ "$logs" == *"$value"* ]]; then
      echo "FOUND personal data in the log of $container (one of the test values)"
      found=1
    fi
  done
done
((found == 0)) && echo "no personal data of the test guest in the logs of ${#containers[@]} containers"
exit "$found"
