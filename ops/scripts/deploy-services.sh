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
#   analysis   market analysis API (NF-Hotel/notebook-market-analysis), internal listener only
#   benchmark  the benchmark service (API of notebook-benchmark): the web application starts runs there, it runs
#              the models in ollama and delivers the result to the api; internal networks only, no published port
#   nginx      (starts only when web is running and the analysis API is healthy) the reverse proxy: TLS on 443 and a redirect on 80 for web, loopback port for the rest;
#              a development certificate is made when deploy/nginx/tls has none (MIL-017)
#   tunnel     (only when deploy/cloudflared/cloudflared.env exists) Cloudflare Tunnel, the public entrance
#              (MIL-040, ADR-0028): an outbound connection to Cloudflare, no published port
#   dbrelay    (only when deploy/postgres-tailnet/postgres-tailnet.env exists) the database relay of the team
#              (MIL-048, ADR-0034): publishes 5432 on the tailnet address only, forwards to postgres
#   alloy      (only when deploy/alloy/alloy.env exists) sends the logs and the host metrics
#              to the monitoring (ADR-0020); left out with a note until the monitoring runs
# Each service reads its git-ignored settings file next to its docker-compose.yml
# (postgres.env, ollama.env, nfhotel-api.env, nfhotel-web.env, market-analysis.env, benchmark.env, nginx.env);
# copy the matching *.example file first. The Docker networks are created if
# missing: nfhotel-proxy, nfhotel-llm, and nfhotel-db and nfhotel-api with --internal (no route out).
#
# Usage: deploy-services.sh [--no-build] [--pull] [--down] [service...]
#   service     postgres, ollama, api, web, analysis, benchmark, nginx, tunnel, dbrelay, alloy (default: all)
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
order=(postgres ollama api web analysis benchmark nginx tunnel dbrelay alloy)

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
    postgres | ollama | api | web | analysis | benchmark | nginx | tunnel | dbrelay | alloy) services+=("$arg") ;;
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
    tunnel) echo "$root/deploy/cloudflared" ;;
    dbrelay) echo "$root/deploy/postgres-tailnet" ;;
    alloy) echo "$root/deploy/alloy" ;;
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
    tunnel) echo "cloudflared.env" ;;
    dbrelay) echo "postgres-tailnet.env" ;;
    alloy) echo "alloy.env" ;;
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

# Alloy runs as its own user and reads the journal through the host's systemd-journal group
# (deploy/alloy/docker-compose.yml). Every compose call on that file needs the number.
if [[ " ${ordered[*]} " == *" alloy "* && -f "$(dir_of alloy)/alloy.env" ]]; then
  export NFHOTEL_JOURNAL_GID
  NFHOTEL_JOURNAL_GID="$(getent group systemd-journal | cut -d: -f3)" || NFHOTEL_JOURNAL_GID=""
  [[ "$NFHOTEL_JOURNAL_GID" =~ ^[0-9]+$ ]] || die "the host has no group systemd-journal; Alloy cannot read the journal"
fi

if ((down)); then
  for s in "${ordered[@]}"; do
    [[ "$s" == "alloy" && ! -f "$(dir_of alloy)/alloy.env" ]] && continue
    [[ "$s" == "tunnel" && ! -f "$(dir_of tunnel)/cloudflared.env" ]] && continue
    [[ "$s" == "dbrelay" && ! -f "$(dir_of dbrelay)/postgres-tailnet.env" ]] && continue
    echo "==> stopping $s"
    (cd "$(dir_of "$s")" && docker compose --env-file "$(env_of "$s")" down)
  done
  echo "Done."
  exit 0
fi

# The default AI model of the market analysis (ADR-0014) is chosen in the back office and stored in the database
# of the web application. Write it as OLLAMA_MODEL in the analysis settings (market-analysis.env) before the analysis
# service starts, so that the service uses it. Does nothing when the database is not running, no default is set,
# or the name has a character that a model name does not have.
analysis_changed=0
apply_default_model() {
  local envfile="$1" model
  model="$(docker exec nfhotel-postgres-postgres-1 sh -c \
    'psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -t -A -c "select model from ai_default where purpose = '"'"'market_analysis'"'"'"' 2>/dev/null |
    tr -d '\r' | head -n 1 || true)"
  if [[ -z "$model" ]]; then
    return 0
  fi
  if [[ ! "$model" =~ ^[A-Za-z0-9._:/-]+$ ]]; then
    echo "warning: the default model '$model' has an unusual character; $envfile is not changed" >&2
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
    echo "warning: the default model $model is not installed in Ollama (see: ollama list); $envfile is not changed" >&2
    return 0
  fi
  echo "==> the default AI model of the market analysis is $model"
  if grep -q '^OLLAMA_MODEL=' "$envfile"; then
    sed "s|^OLLAMA_MODEL=.*|OLLAMA_MODEL=$model|" "$envfile" >"$envfile.tmp"
  else
    {
      cat "$envfile"
      printf 'OLLAMA_MODEL=%s\n' "$model"
    } >"$envfile.tmp"
  fi
  if ! cmp -s "$envfile" "$envfile.tmp"; then
    # Written in place, so the owner and the mode of the file stay as they are.
    cat "$envfile.tmp" >"$envfile"
    analysis_changed=1
  fi
  rm -f "$envfile.tmp"
}

# Alloy is optional: it runs once the monitoring of ADR-0020 exists and alloy.env names it.
alloy_skipped=0
if [[ " ${ordered[*]} " == *" alloy "* && ! -f "$(dir_of alloy)/alloy.env" ]]; then
  echo "note: $(dir_of alloy)/alloy.env is missing; alloy is left out until the monitoring runs (ADR-0020)"
  alloy_skipped=1
fi
# The tunnel is optional too: without cloudflared.env the site stays on the tailnet only (MIL-040).
tunnel_skipped=0
if [[ " ${ordered[*]} " == *" tunnel "* && ! -f "$(dir_of tunnel)/cloudflared.env" ]]; then
  echo "note: $(dir_of tunnel)/cloudflared.env is missing; the tunnel is left out and the site stays on the tailnet (ADR-0028)"
  tunnel_skipped=1
fi
# The database relay of the team is optional: without postgres-tailnet.env no port is published (ADR-0034).
dbrelay_skipped=0
if [[ " ${ordered[*]} " == *" dbrelay "* && ! -f "$(dir_of dbrelay)/postgres-tailnet.env" ]]; then
  echo "note: $(dir_of dbrelay)/postgres-tailnet.env is missing; the database relay is left out (ADR-0034)"
  dbrelay_skipped=1
fi

# Check every settings file before touching anything.
for s in "${ordered[@]}"; do
  d="$(dir_of "$s")"
  e="$(env_of "$s")"
  [[ "$s" == "alloy" ]] && ((alloy_skipped)) && continue
  [[ "$s" == "tunnel" ]] && ((tunnel_skipped)) && continue
  [[ "$s" == "dbrelay" ]] && ((dbrelay_skipped)) && continue
  [[ -f "$d/$e" ]] || die "$d/$e is missing; copy $e.example to $e and fill it in"
done

# No secret goes into the environment of a container (MIL-035, ADR-0024): for PostgreSQL, the
# API and the web application, scripts/write-secret-files.sh splits the settings into
# secrets/settings.env and one file per secret, which compose mounts at /run/secrets. The web
# application takes the API token from nfhotel-api.env and the benchmark token from benchmark.env,
# so each secret is written in one settings file only. When the deploy command passes
# NFHOTEL_API_CONNECTION_STRING (the login of nfhotel_app, ADR-0011), the API logs in with it
# instead of the superuser of postgres.env. The values are read, never printed.
secret_files() {
  # $1: the service of this script; $2: the service of write-secret-files.sh, when it has another name.
  bash "$root/scripts/write-secret-files.sh" "${2:-$1}" --out "$(dir_of "$1")/secrets" \
    --postgres "$(dir_of postgres)/$(env_of postgres)" --api "$(dir_of api)/$(env_of api)" \
    --web "$(dir_of web)/$(env_of web)" --benchmark "$(dir_of benchmark)/$(env_of benchmark)" \
    --cloudflared "$(dir_of tunnel)/$(env_of tunnel)" >/dev/null
}
for s in "${ordered[@]}"; do
  case "$s" in
    postgres | api | web) secret_files "$s" || die "cannot write the secret files of $s" ;;
    tunnel) ((tunnel_skipped)) || secret_files tunnel cloudflared || die "cannot write the secret file of the tunnel" ;;
  esac
  if [[ "$s" == "benchmark" ]]; then
    # The benchmark service is built from its own repository and reads the API token from its
    # environment (deploy/benchmark).
    api_env="$(dir_of api)/$(env_of api)"
    [[ -f "$api_env" ]] || die "$api_env is missing; the benchmark service takes the API token from it"
    while IFS= read -r line || [[ -n "$line" ]]; do
      key="${line%%=*}"
      value="${line#*=}"
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

analysis_skipped=0
for s in "${ordered[@]}"; do
  d="$(dir_of "$s")"
  e="$(env_of "$s")"
  [[ "$s" == "alloy" ]] && ((alloy_skipped)) && continue
  [[ "$s" == "tunnel" ]] && ((tunnel_skipped)) && continue
  [[ "$s" == "dbrelay" ]] && ((dbrelay_skipped)) && continue
  # The analysis image is built from a pinned commit of NF-Hotel/notebook-market-analysis, which
  # needs the internet; a host that cannot reach it leaves the service out, with the reason, and the
  # rest of the deployment goes on. A release (--no-build) always names its digests.
  if [[ "$s" == "analysis" ]] && ((build)); then
    if ! (cd "$d" && docker compose --env-file "$e" build market-analysis-api) >&2; then
      echo "warning: cannot build the analysis image from NF-Hotel/notebook-market-analysis (GitHub and PyPI must be reachable);" >&2
      echo "         analysis is left out." >&2
      analysis_skipped=1
      continue
    fi
  fi
  if [[ "$s" == "web" ]]; then
    require web healthy nfhotel-postgres-postgres-1
    require web healthy nfhotel-api-nfhotel-api-1
  fi
  if [[ "$s" == "nginx" ]]; then
    require nginx running nfhotel-web-nfhotel-web-1
    ((analysis_skipped)) || require nginx healthy market-analysis-market-analysis-api-1
    # The proxy needs a certificate to start. A pair put there by the owner is never replaced.
    names="$(sed -n 's/^NGINX_SERVER_NAME=//p' "$d/$e" | tr -d '\r') $(hostname -I 2>/dev/null || true)"
    # shellcheck disable=SC2086 # the names are split on purpose
    bash "$root/scripts/make-dev-certificate.sh" "$d/tls" $names || die "no certificate for the proxy"
  fi
  if [[ "$s" == "analysis" ]]; then
    apply_default_model "$d/$e"
  fi
  echo "==> deploying $s"
  args=(--env-file "$e" up -d --remove-orphans)
  if ((build)) && [[ "$s" =~ ^(api|web|analysis|benchmark)$ ]]; then
    args+=(--build)
  fi
  # --wait returns once every service with a healthcheck is healthy; the proxy has one since
  # MIL-023 task 2, so the smoke test that follows finds it listening.
  args+=(--wait --wait-timeout 300)
  if [[ "$s" == "nginx" ]]; then
    # The proxy keeps nothing, and its templates are rendered into the container when it starts. A hash of the
    # templates, the snippets and the settings is part of its environment, so compose recreates it exactly when one
    # of them changed, and a deploy that changes only an image keeps the running proxy (MIL-021 criterion 5). A new
    # certificate is loaded by nfhotel-cert-renew with a reload.
    export NGINX_CONFIG_HASH
    NGINX_CONFIG_HASH="$(cd "$d" && find templates snippets -type f -print0 | sort -z | xargs -0 sha256sum |
      cat - "$e" | sha256sum | cut -c1-16)" || die "cannot hash the configuration of the proxy"
  fi
  if [[ "$s" == "api" || "$s" == "web" || "$s" == "tunnel" ]]; then
    # Compose does not see a changed secret file; this hash of them makes it recreate the
    # container after a rotation (ADR-0024).
    export NFHOTEL_SETTINGS_HASH
    NFHOTEL_SETTINGS_HASH="$(cat "$d"/secrets/* | sha256sum | cut -c1-16)" || die "cannot hash the secret files of $s"
  fi
  if [[ "$s" == "tunnel" ]]; then
    # The public entrance must not take the hotel down: when the tunnel does not turn healthy (a wrong token,
    # Cloudflare unreachable), the deploy warns and goes on, the site stays on the tailnet, and PublicSiteDown
    # alerts (MIL-040).
    if ! (cd "$d" && docker compose "${args[@]}"); then
      echo "warning: the tunnel did not turn healthy; the release stays on the tailnet, and the monitoring alerts PublicSiteDown" >&2
      (cd "$d" && docker compose --env-file "$e" logs --tail 20 cloudflared) >&2 || true
    fi
    continue
  fi
  if [[ "$s" == "dbrelay" ]]; then
    # The relay serves the team only: when it does not turn healthy (the tailnet address is not up yet),
    # the deploy warns and goes on, and the hotel keeps running (ADR-0034).
    if ! (cd "$d" && docker compose "${args[@]}"); then
      echo "warning: the database relay did not turn healthy; the team cannot reach the database until the next deploy" >&2
      (cd "$d" && docker compose --env-file "$e" logs --tail 20 relay) >&2 || true
    fi
    continue
  fi
  if [[ "$s" == "alloy" ]]; then
    # The monitoring agent must not take the hotel down: when it does not turn healthy, the
    # deploy warns and goes on, and NoMetricsFromNfHotel alerts (ADR-0020).
    if ! (cd "$d" && docker compose "${args[@]}"); then
      echo "warning: alloy did not turn healthy; the release stays, and the monitoring alerts NoMetricsFromNfHotel" >&2
      (cd "$d" && docker compose --env-file "$e" logs --tail 20 alloy) >&2 || true
    fi
    continue
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
  [[ "$s" == "analysis" ]] && ((analysis_skipped)) && continue
  [[ "$s" == "alloy" ]] && ((alloy_skipped)) && continue
  [[ "$s" == "tunnel" ]] && ((tunnel_skipped)) && continue
  [[ "$s" == "dbrelay" ]] && ((dbrelay_skipped)) && continue
  (cd "$(dir_of "$s")" && docker compose --env-file "$(env_of "$s")" ps)
done
deployed=()
for s in "${ordered[@]}"; do
  [[ "$s" == "analysis" ]] && ((analysis_skipped)) && continue
  [[ "$s" == "alloy" ]] && ((alloy_skipped)) && continue
  [[ "$s" == "tunnel" ]] && ((tunnel_skipped)) && continue
  [[ "$s" == "dbrelay" ]] && ((dbrelay_skipped)) && continue
  deployed+=("$s")
done
echo "Deployed: ${deployed[*]}$( ((analysis_skipped)) && echo ' (analysis left out: see the warning above)')$( ((alloy_skipped)) && echo ' (alloy left out: no alloy.env)')$( ((tunnel_skipped)) && echo ' (tunnel left out: no cloudflared.env)')$( ((dbrelay_skipped)) && echo ' (dbrelay left out: no postgres-tailnet.env)')"
