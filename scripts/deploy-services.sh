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
#   benchmark  LLM benchmark notebook, internal listener only
#   nginx      the reverse proxy: TLS on 443 and a redirect on 80 for web, loopback port for the rest;
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
# file only. Export those values for compose. They are read, never printed.
for s in "${ordered[@]}"; do
  if [[ "$s" == "api" ]]; then
    pg_env="$(dir_of postgres)/$(env_of postgres)"
    [[ -f "$pg_env" ]] || die "$pg_env is missing; the API takes the database login from it"
    while IFS='=' read -r key value; do
      case "$key" in
        POSTGRES_USER | POSTGRES_PASSWORD | POSTGRES_DB) export "$key=${value%$'\r'}" ;;
      esac
    done <"$pg_env"
  fi
  if [[ "$s" == "web" ]]; then
    api_env="$(dir_of api)/$(env_of api)"
    [[ -f "$api_env" ]] || die "$api_env is missing; the web service takes the API token from it"
    while IFS='=' read -r key value; do
      [[ "$key" == "Api__Token" ]] && export "API_TOKEN=${value%$'\r'}"
    done <"$api_env"
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

for s in "${ordered[@]}"; do
  d="$(dir_of "$s")"
  e="$(env_of "$s")"
  if [[ "$s" == "nginx" ]]; then
    # The proxy needs a certificate to start. A pair put there by the owner is never replaced.
    names="$(sed -n 's/^NGINX_SERVER_NAME=//p' "$d/$e" | tr -d '\r') $(hostname -I 2>/dev/null || true)"
    # shellcheck disable=SC2086 # the names are split on purpose
    bash "$root/scripts/make-dev-certificate.sh" "$d/tls" $names || die "no certificate for the proxy"
  fi
  echo "==> deploying $s"
  args=(--env-file "$e" up -d --remove-orphans)
  if ((build)) && [[ "$s" =~ ^(api|web|analysis|benchmark)$ ]]; then
    args+=(--build)
  fi
  # --wait returns once every service with a healthcheck is healthy.
  if [[ "$s" != "nginx" ]]; then
    args+=(--wait --wait-timeout 300)
  fi
  (cd "$d" && docker compose "${args[@]}") || die "deploying $s failed"
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
