#!/usr/bin/env bash
# Deploy the NFHotel containers from deploy/ with Docker Compose, on the machine
# whose Docker daemon you are pointed at. deploy.sh runs this inside the Proxmox
# VM; you can also run it on any Docker host.
#
# Services, in deploy order (the proxy last, so its upstreams already exist):
#   postgres   PostgreSQL 16, internal network only, no published port
#   ollama     model server for analysis and benchmark, network nfhotel-llm, no
#              published port
#   api        database API: the only service with the database login and the passport
#              key; internal networks nfhotel-api and nfhotel-db, no published port
#   web        NFHotel web application, the only service the proxy serves publicly; it
#              holds only an API address and a token
#   analysis   market analysis API and notebook, internal listener only
#   benchmark  the benchmark service (API of notebook-benchmark): the web application starts runs there, it runs
#              the models in ollama and delivers the result to the api; internal networks only, no published port
#   nginx      (starts only when web is running and the analysis API is healthy) the reverse proxy: TLS on 443 and a redirect on 80 for web, loopback port for the rest;
#              a development certificate is made when deploy/nginx/tls has none (MIL-017)
# Each service reads its git-ignored settings file next to its docker-compose.yml
# (postgres.env, ollama.env, nfhotel-api.env, nfhotel-web.env, market-analysis.env, benchmark.env, nginx.env);
# copy the matching *.example file first. The Docker networks are created if
# missing: nfhotel-proxy, nfhotel-llm, and nfhotel-db and nfhotel-api with --internal (no route out).
#
# Usage: deploy-services.sh [--no-build] [--pull] [--down] [service...]
#   service     postgres, ollama, api, web, analysis, benchmark, nginx (default: all seven)
#   --no-build  start without rebuilding the images
#   --pull      git pull the repository (and submodules) before building
#   --down      stop and remove the named services instead of deploying (volumes,
#               and so the database, are kept)
# Exit:  0 deployed; 1 a check or compose step failed; 2 usage error
# Needs: bash 4+, docker with the compose plugin, git (for --pull)

set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
proxy_network="${NFHOTEL_PROXY_NETWORK:-nfhotel-proxy}"
db_network="${NFHOTEL_DB_NETWORK:-nfhotel-db}"
api_network="${NFHOTEL_API_NETWORK:-nfhotel-api}"
llm_network="${NFHOTEL_LLM_NETWORK:-nfhotel-llm}"
order=(postgres ollama api web analysis benchmark nginx)

build=1
pull=0
down=0
services=()

die() {
  echo "error: $*" >&2
  exit 1
}

usage() {
  sed -n '2,/^set -euo/p' "${BASH_SOURCE[0]}" | sed '$d' | sed 's/^# \{0,1\}//'
}

for arg in "$@"; do
  case "$arg" in
    --no-build) build=0 ;;
    --pull) pull=1 ;;
    --down) down=1 ;;
    -h | --help)
      usage
      exit 0
      ;;
    postgres | ollama | api | web | analysis | benchmark | nginx) services+=("$arg") ;;
    *)
      echo "error: unknown argument: $arg" >&2
      usage >&2
      exit 2
      ;;
  esac
done

if [[ ${#services[@]} -eq 0 ]]; then
  services=("${order[@]}")
fi

dir_of() {
  case "$1" in
    postgres) echo "$root/deploy/postgres" ;;
    ollama) echo "$root/deploy/ollama" ;;
    api) echo "$root/deploy/nfhotel-api" ;;
    web) echo "$root/deploy/nfhotel-web" ;;
    analysis) echo "$root/deploy/market-analysis" ;;
    benchmark) echo "$root/deploy/benchmark" ;;
    nginx) echo "$root/deploy/nginx" ;;
  esac
}

env_of() {
  case "$1" in
    postgres) echo "postgres.env" ;;
    ollama) echo "ollama.env" ;;
    api) echo "nfhotel-api.env" ;;
    web) echo "nfhotel-web.env" ;;
    analysis) echo "market-analysis.env" ;;
    benchmark) echo "benchmark.env" ;;
    nginx) echo "nginx.env" ;;
  esac
}

# Run in deploy order, whatever order the arguments came in.
ordered=()
for s in "${order[@]}"; do
  for wanted in "${services[@]}"; do
    [[ "$wanted" == "$s" ]] && ordered+=("$s")
  done
done
if ((down)); then
  reversed=()
  for ((i = ${#ordered[@]} - 1; i >= 0; i--)); do reversed+=("${ordered[i]}"); done
  ordered=("${reversed[@]}")
fi

command -v docker >/dev/null || die "docker is not installed or not on PATH"
docker compose version >/dev/null 2>&1 || die "the docker compose plugin is missing"
docker info >/dev/null 2>&1 || die "cannot reach the Docker daemon (is it running, is DOCKER_HOST right?)"

if ((down)); then
  for s in "${ordered[@]}"; do
    echo "==> stopping $s"
    (cd "$(dir_of "$s")" && docker compose --env-file "$(env_of "$s")" down)
  done
  echo "Done."
  exit 0
fi

# The default AI model of the market analysis (ADR-0014) is chosen in the back office and stored in the database
# of the web application. Write it as `model` in the [llm] section of the analysis settings before the analysis
# service starts, so that the service uses it. Does nothing when the database is not running, no default is set,
# or the name has a character that a model name does not have.
analysis_changed=0
apply_default_model() {
  local toml="$1" model
  model="$(docker exec nfhotel-postgres-postgres-1 sh -c \
    'psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -t -A -c "select model from ai_default where purpose = '"'"'market_analysis'"'"'"' 2>/dev/null |
    tr -d '\r' | head -n 1 || true)"
  if [[ -z "$model" ]]; then
    return 0
  fi
  if [[ ! "$model" =~ ^[A-Za-z0-9._:/-]+$ ]]; then
    echo "warning: the default model '$model' has an unusual character; $toml is not changed" >&2
    return 0
  fi
  # The analysis service uses a model only when the name is exactly one that Ollama lists, tag included
  # (phi3.5:latest, not phi3.5), so find the listed name; a default that is not installed is not written.
  local installed resolved
  installed="$(docker exec ollama-ollama-1 ollama list 2>/dev/null || true)"
  if [[ -z "$installed" ]]; then
    echo "warning: Ollama is not running, so the name of the default model $model is not checked" >&2
  elif resolved="$(printf '%s
' "$installed" | bash "$root/scripts/resolve-model-name.sh" "$model")"; then
    model="$resolved"
  else
    echo "warning: the default model $model is not installed in Ollama (see: ollama list); $toml is not changed" >&2
    return 0
  fi
  echo "==> the default AI model of the market analysis is $model"
  awk -v model="$model" '
    /^\[/ { in_llm = ($0 == "[llm]") }
    in_llm && /^model[[:space:]]*=/ { print "model = \"" model "\""; next }
    { print }
  ' "$toml" >"$toml.tmp"
  if ! cmp -s "$toml" "$toml.tmp"; then
    # Written in place: the container mounts this file, and a new file would leave it looking at the old one.
    cat "$toml.tmp" >"$toml"
    analysis_changed=1
  fi
  rm -f "$toml.tmp"
}

# Check every settings file before touching anything.
for s in "${ordered[@]}"; do
  d="$(dir_of "$s")"
  e="$(env_of "$s")"
  [[ -f "$d/$e" ]] || die "$d/$e is missing; copy $e.example to $e and fill it in"
  if [[ "$s" == "analysis" && ! -f "$d/market-analysis.toml" ]]; then
    die "$d/market-analysis.toml is missing; copy market-analysis.toml.example to it"
  fi
done

# The API builds its connection string from postgres.env, and the web service takes the API
# token from nfhotel-api.env (docker-compose.yml of each), so each secret is written in one
# file only. Export those values for compose. They are read, never printed. When the deploy
# command passes NFHOTEL_API_CONNECTION_STRING (the login of nfhotel_app, ADR-0011) in the
# environment, compose uses it instead of the superuser (deploy/nfhotel-api).
for s in "${ordered[@]}"; do
  if [[ "$s" == "api" ]]; then
    pg_env="$(dir_of postgres)/$(env_of postgres)"
    [[ -f "$pg_env" ]] || die "$pg_env is missing; the API takes the database login from it"
    # Split each line at the first = only: read with IFS== drops a trailing = (base64).
    while IFS= read -r line || [[ -n "$line" ]]; do
      key="${line%%=*}"
      value="${line#*=}"
      case "$key" in
        POSTGRES_USER | POSTGRES_PASSWORD | POSTGRES_DB) export "$key=${value%$'\r'}" ;;
      esac
    done <"$pg_env"
  fi
  if [[ "$s" == "web" || "$s" == "benchmark" ]]; then
    api_env="$(dir_of api)/$(env_of api)"
    [[ -f "$api_env" ]] || die "$api_env is missing; the $s service takes the API token from it"
    while IFS= read -r line || [[ -n "$line" ]]; do
      key="${line%%=*}"
      value="${line#*=}"
      [[ "$key" == "Api__Token" ]] && export "API_TOKEN=${value%$'\r'}"
    done <"$api_env"
  fi
  if [[ "$s" == "web" ]]; then
    # The web application calls the benchmark service with the token of that service; an empty value
    # leaves the Start page saying that the service is not set up.
    bench_env="$(dir_of benchmark)/$(env_of benchmark)"
    if [[ -f "$bench_env" ]]; then
      while IFS= read -r line || [[ -n "$line" ]]; do
        key="${line%%=*}"
        value="${line#*=}"
        [[ "$key" == "NFBENCH_API_TOKEN" ]] && export "BENCHMARK_API_TOKEN=${value%$'\r'}"
      done <"$bench_env"
    fi
  fi
done

if ((pull)); then
  echo "==> git pull"
  git -C "$root" pull --ff-only
  git -C "$root" submodule update --init --recursive
fi

if ! docker network inspect "$proxy_network" >/dev/null 2>&1; then
  echo "==> creating network $proxy_network"
  docker network create "$proxy_network" >/dev/null
fi
if ! docker network inspect "$llm_network" >/dev/null 2>&1; then
  echo "==> creating network $llm_network"
  docker network create "$llm_network" >/dev/null
fi
if ! docker network inspect "$db_network" >/dev/null 2>&1; then
  echo "==> creating internal network $db_network"
  docker network create --internal "$db_network" >/dev/null
fi
if ! docker network inspect "$api_network" >/dev/null 2>&1; then
  echo "==> creating internal network $api_network"
  docker network create --internal "$api_network" >/dev/null
fi

# The services are separate Compose projects, so Compose cannot order them. Check the order here:
# web needs a healthy database and API, the proxy needs a running web service. Waits up to 60 seconds,
# because the service may have been started a moment ago.
require() {
  local service="$1" wanted="$2" container="$3" state
  for _ in $(seq 1 30); do
    state="$(docker inspect -f "{{.State.Running}} {{if .State.Health}}{{.State.Health.Status}}{{end}}" "$container" 2>/dev/null || true)"
    case "$wanted" in
      healthy) [[ "$state" == "true healthy" ]] && return 0 ;;
      running) [[ "$state" == true* ]] && return 0 ;;
    esac
    sleep 2
  done
  die "$service needs $container to be $wanted; deploy it first (postgres, api, web, analysis, nginx, in that order)"
}

for s in "${ordered[@]}"; do
  d="$(dir_of "$s")"
  e="$(env_of "$s")"
  if [[ "$s" == "web" ]]; then
    require web healthy nfhotel-postgres-postgres-1
    require web healthy nfhotel-api-nfhotel-api-1
  fi
  if [[ "$s" == "nginx" ]]; then
    require nginx running nfhotel-web-nfhotel-web-1
    require nginx healthy market-analysis-market-analysis-api-1
    # The proxy needs a certificate to start. A pair put there by the owner is never replaced.
    names="$(sed -n 's/^NGINX_SERVER_NAME=//p' "$d/$e" | tr -d '\r') $(hostname -I 2>/dev/null || true)"
    # shellcheck disable=SC2086 # the names are split on purpose
    bash "$root/scripts/make-dev-certificate.sh" "$d/tls" $names || die "no certificate for the proxy"
  fi
  if [[ "$s" == "analysis" ]]; then
    apply_default_model "$d/market-analysis.toml"
  fi
  echo "==> deploying $s"
  args=(--env-file "$e" up -d --remove-orphans)
  if ((build)) && [[ "$s" =~ ^(api|web|analysis|benchmark)$ ]]; then
    args+=(--build)
  fi
  # --wait returns once every service with a healthcheck is healthy.
  if [[ "$s" != "nginx" ]]; then
    args+=(--wait --wait-timeout 300)
  else
    # The proxy keeps nothing. Its templates are rendered into the container when it starts, so a container that is
    # kept would still hold a file of a template that was removed; always start it fresh.
    args+=(--force-recreate)
  fi
  (cd "$d" && docker compose "${args[@]}") || die "deploying $s failed"
  if [[ "$s" == "analysis" ]] && ((analysis_changed)); then
    # The service reads its settings when it starts, so a changed default model needs a restart.
    echo "==> restarting analysis to use the new model"
    (cd "$d" && docker compose --env-file "$e" restart) || die "restarting $s failed"
  fi
  if [[ "$s" == "ollama" ]]; then
    models="$(sed -n 's/^OLLAMA_PULL_MODELS=//p' "$d/$e" | tr -d '\r')"
    for model in $models; do
      echo "==> pulling model $model"
      (cd "$d" && docker compose --env-file "$e" exec -T ollama ollama pull "$model") ||
        die "pulling $model failed"
    done
  fi
done

echo
echo "==> status"
for s in "${ordered[@]}"; do
  (cd "$(dir_of "$s")" && docker compose --env-file "$(env_of "$s")" ps)
done
echo "Deployed: ${ordered[*]}"
