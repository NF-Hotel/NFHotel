#!/usr/bin/env bash
# Test of the Docker networks of the services (MIL-061, ADR-0039). Static part: every compose
# file that scripts/deploy-services.sh deploys puts each service on exactly the networks of the
# wiki page Network Isolation; only nginx and the database relay publish a host port, nginx the
# ports 80 and 443 only; the scripts create the python network as an internal network; and
# nothing under ops/ still carries the internal listener of nginx (8081). Live part: stand-ins
# named as the services and attached to the networks the compose files give them answer from the
# python network and not from the proxy network, where nginx stands; the API answers from the api
# network, the one network nginx and the API share.
#
# Usage: test-service-networks.sh
# It starts the containers t-net-* and the networks t-net-proxy, t-net-api, t-net-python,
# t-net-llm and t-net-db, and removes them at the end.
# Exit:  0 every check passed; 1 a check failed
# Needs: bash 4+, docker with the compose plugin, jq

set -uo pipefail

OPS="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
readonly CURL=curlimages/curl:8.10.1 ECHO=hashicorp/http-echo:1.0
pass=0
fail=0
check() {
  if [[ "$2" == "$3" ]]; then
    echo "PASS  $1 ($3)"
    pass=$((pass + 1))
  else
    echo "FAIL  $1 (got '$3', expected '$2')"
    fail=$((fail + 1))
  fi
}

# The table of the wiki page Network Isolation: compose folder/service, then the network keys of
# the compose file in alphabetical order. A new service gets a row here and on the wiki page in
# the same pull request (the page, "Rules for a new service").
declare -A EXPECTED=(
  ["nginx/nginx"]="api proxy"
  ["nfhotel-web/nfhotel-web"]="api proxy python"
  ["nfhotel-api/nfhotel-api"]="api db python"
  ["market-analysis/market-analysis-api"]="llm python"
  ["benchmark/benchmark"]="llm python"
  ["benchmark/results-owner"]=""
  ["ollama/ollama"]="llm"
  ["ollama/models-owner"]=""
  ["postgres/postgres"]="db"
  ["postgres-tailnet/relay"]="db published"
  ["cloudflared/cloudflared"]="proxy"
  ["alloy/alloy"]="apps egress internal"
  ["alloy/socket-proxy"]="internal"
)
# The host ports a service may publish, as the ports inside the container.
declare -A PORTS=(
  ["nginx/nginx"]="80 443"
  ["postgres-tailnet/relay"]="5432"
)
# What each compose file needs to be read: the example file in place of the git-ignored
# settings file, and the values the deploy passes.
declare -A SETTINGS=(
  ["nginx"]="NGINX_ENV_FILE=nginx.env.example"
  ["nfhotel-web"]="NFHOTEL_WEB_ENV_FILE=nfhotel-web.env.example"
  ["nfhotel-api"]="NFHOTEL_API_ENV_FILE=nfhotel-api.env.example"
  ["market-analysis"]="MARKET_ANALYSIS_ENV_FILE=market-analysis.env.example"
  ["benchmark"]="BENCHMARK_ENV_FILE=benchmark.env.example API_TOKEN=test-token"
  ["ollama"]="OLLAMA_ENV_FILE=ollama.env.example"
  ["postgres"]="POSTGRES_ENV_FILE=postgres.env.example"
  ["postgres-tailnet"]="NFHOTEL_TAILNET_IP=100.64.0.1"
  ["cloudflared"]=""
  ["alloy"]="ALLOY_ENV_FILE=alloy.env.example NFHOTEL_JOURNAL_GID=999"
)
readonly FOLDERS=(nginx nfhotel-web nfhotel-api market-analysis benchmark ollama postgres postgres-tailnet cloudflared alloy)

# config <folder>: the compose file of the folder as JSON, read with its example settings.
config() {
  # shellcheck disable=SC2086 # the settings are several assignments, split on purpose
  (cd "$OPS/deploy/$1" && env ${SETTINGS[$1]} docker compose -f docker-compose.yml config --format json 2>/dev/null)
}

echo "== the networks and the ports of every service, from the compose files"
seen=" "
for folder in "${FOLDERS[@]}"; do
  json="$(config "$folder")"
  check "deploy/$folder/docker-compose.yml is read" 0 "$?"
  # The fields are joined with |, not tabs: a tab is whitespace to read, so an empty field would vanish.
  while IFS='|' read -r service networks ports mode; do
    [[ -n "$service" ]] || continue
    key="$folder/$service"
    seen+="$key "
    [[ "$mode" == "none" ]] && networks=""
    # No colon: a service with no network (network_mode none) has an empty row in the table, not a missing one.
    check "$key is on the networks of the table" "${EXPECTED[$key]-<not in the table>}" "$networks"
    check "$key publishes only what the table allows" "${PORTS[$key]:-}" "$ports"
  done < <(jq -r '.services | to_entries[] | [.key, ((.value.networks // {}) | keys | join(" ")),
    ([(.value.ports // [])[].target | tostring] | join(" ")), (.value.network_mode // "")] | join("|")' <<<"$json" | tr -d '\r')
done
for key in "${!EXPECTED[@]}"; do
  [[ "$seen" == *" $key "* ]] || check "$key is still in a compose file" yes no
done

echo "== the scripts make the python network as an internal network"
check "deploy-services.sh creates the python network --internal" 1 \
  "$(grep -cF "docker network create --internal \"\$python_network\"" "$OPS/scripts/deploy-services.sh")"
check "stack.sh names the python network of a stack" 1 "$(grep -cF "STACK_PYTHON_NET=\"\$1-python\"" "$OPS/scripts/lib/stack.sh")"

echo "== nothing under ops/ carries the internal listener of nginx"
check "no nginx template listens on 8081" "" "$(grep -rl 'listen 8081' "$OPS/deploy/nginx" || true)"
check "no NGINX_INTERNAL_PORT under ops/" "" \
  "$(grep -rl 'NGINX_INTERNAL_PORT' "$OPS" --exclude-dir=.dev-lite --exclude-dir=output --exclude=test-service-networks.sh || true)"
check "no ANALYSIS_* variable of nginx under ops/" "" \
  "$(grep -rlE 'ANALYSIS_(API_)?(SERVER_NAME|UPSTREAM_HOST|UPSTREAM_PORT)' "$OPS" --exclude-dir=.dev-lite --exclude-dir=output --exclude=test-service-networks.sh || true)"

echo "== the isolation, live, with stand-ins on the networks of the compose files"
cleanup() {
  docker rm -f t-net-analysis t-net-benchmark t-net-api >/dev/null 2>&1
  docker network rm t-net-proxy t-net-api t-net-python t-net-llm t-net-db >/dev/null 2>&1
}
trap cleanup EXIT
cleanup
docker network create t-net-proxy >/dev/null
docker network create --internal t-net-api >/dev/null
docker network create --internal t-net-python >/dev/null
docker network create t-net-llm >/dev/null
docker network create --internal t-net-db >/dev/null
# networks_of <folder> <service>: the network keys of the service, from its compose file.
networks_of() { jq -r --arg s "$2" '.services[$s].networks // {} | keys | join(" ")' <<<"$(config "$1")" | tr -d '\r'; }
# standin <container> <service name> <port> <network keys...>: an http-echo that answers as the
# service, on the throwaway networks that match the keys of its compose file.
standin() {
  local name="$1" alias="$2" port="$3" first="$4" net
  shift 4
  docker run -d --name "$name" --network "t-net-$first" --network-alias "$alias" "$ECHO" -listen=":$port" -text="$alias" >/dev/null
  for net in "$@"; do docker network connect --alias "$alias" "t-net-$net" "$name"; done
}
# shellcheck disable=SC2046 # the network keys are split on purpose
standin t-net-analysis market-analysis-api 8000 $(networks_of market-analysis market-analysis-api)
# shellcheck disable=SC2046
standin t-net-benchmark benchmark 8000 $(networks_of benchmark benchmark)
# shellcheck disable=SC2046
standin t-net-api nfhotel-api 8080 $(networks_of nfhotel-api nfhotel-api)
# reach <network key> <url>: the HTTP status a client on that network gets; 000 when the name does
# not resolve there.
reach() { docker run --rm --network "t-net-$1" "$CURL" -s -o /dev/null -w '%{http_code}' --max-time 5 "$2" || true; }
check "the proxy network (nginx) does not reach the market analysis" 000 "$(reach proxy http://market-analysis-api:8000/)"
check "the proxy network (nginx) does not reach the benchmark" 000 "$(reach proxy http://benchmark:8000/)"
check "the proxy network (nginx) does not reach the API" 000 "$(reach proxy http://nfhotel-api:8080/)"
check "the api network reaches the API (the two mobile routes)" 200 "$(reach api http://nfhotel-api:8080/)"
check "the api network does not reach the market analysis" 000 "$(reach api http://market-analysis-api:8000/)"
check "the api network does not reach the benchmark" 000 "$(reach api http://benchmark:8000/)"
check "the python network reaches the market analysis" 200 "$(reach python http://market-analysis-api:8000/)"
check "the python network reaches the benchmark" 200 "$(reach python http://benchmark:8000/)"
check "the python network reaches the API (the results of the benchmark)" 200 "$(reach python http://nfhotel-api:8080/)"
check "the llm network (Ollama) does not reach the API" 000 "$(reach llm http://nfhotel-api:8080/)"

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
