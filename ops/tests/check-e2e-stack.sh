#!/usr/bin/env bash
# Check the throwaway stack of scripts/e2e-stack.sh (MIL-027 task 1 of NFHotel) from a
# client on its Docker network: the public pages answer through nginx over TLS, the back
# office sends a visitor to the login, port 80 redirects to HTTPS, no container of the
# stack publishes a host port, the API and PostgreSQL are not on the proxy network, and
# the three test accounts exist.
#
# Usage: check-e2e-stack.sh <stack.env> [<name>]
# Exit:  0 every check passed; 1 a check failed; 2 usage error
# Needs: bash 4+, docker

set -uo pipefail

[[ $# -ge 1 && -f "$1" ]] || {
  echo "usage: check-e2e-stack.sh <stack.env> [<name>]" >&2
  exit 2
}
stack_env="$1"
name="${2:-nfhotel-e2e}"
network="$(sed -n 's/^E2E_NETWORK=//p' "$stack_env")"
readonly CURL="curlimages/curl:8.10.1"
failures=0

check() {
  if [[ "$3" == "$2" ]]; then
    echo "PASS  $1"
  else
    echo "FAIL  $1 (expected '$2', got '$3')"
    failures=$((failures + 1))
  fi
}
# get <format> <url>: one curl call from a container on the stack's network.
get() {
  MSYS_NO_PATHCONV=1 docker run --rm --network "$network" "$CURL" -sk -o /dev/null -w "$1" "$2" || true
}

check "the home page answers over TLS" 200 "$(get '%{http_code}' https://nginx/)"
check "the booking page answers over TLS" 200 "$(get '%{http_code}' https://nginx/book)"
check "the login page answers over TLS" 200 "$(get '%{http_code}' https://nginx/admin/login)"
check "the back office sends a visitor to the login" "https://nginx/admin/login?returnUrl=%2Fadmin" \
  "$(get '%{redirect_url}' https://nginx/admin)"
check "port 80 redirects to HTTPS" 301 "$(get '%{http_code}' http://nginx/)"

ports=""
for id in $(docker ps --quiet --filter "label=com.docker.compose.project" | xargs -r docker inspect \
  --format '{{index .Config.Labels "com.docker.compose.project"}} {{.Id}}' | awk -v p="$name-" 'index($1, p) == 1 {print $2}'); do
  ports+="$(docker port "$id" 2>/dev/null)"
done
check "no container of the stack publishes a host port" "" "$ports"
on_proxy="$(docker network inspect "$network" --format '{{range .Containers}}{{.Name}} {{end}}')"
check "the API is not on the proxy network" "" "$(tr ' ' '\n' <<<"$on_proxy" | grep -E 'nfhotel-api|postgres' || true)"
for role in MANAGER RECEPTIONIST HOUSEKEEPING; do
  check "stack.env holds the $role account" yes \
    "$(grep -qE "^E2E_${role}_USER=.+" "$stack_env" && grep -qE "^E2E_${role}_PASSWORD=.{32}$" "$stack_env" && echo yes)"
done

((failures == 0)) || exit 1
echo "==> the end-to-end stack is as expected"
