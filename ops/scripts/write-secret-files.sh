#!/usr/bin/env bash
# Split the settings of one service into a settings file without secrets and one file per
# secret (MIL-035 tasks 2 and 3, ADR-0024). Compose passes the settings file as env_file and
# mounts each secret file at /run/secrets/<name>, where the API and the web application read it
# with AddKeyPerFile and PostgreSQL with POSTGRES_PASSWORD_FILE. So no secret is in the
# environment of a container, where docker inspect, the socket proxy of Alloy and any process
# of the container could read it (RC-185).
#
# The settings files stay the one place where an operator writes a secret; this script is run
# by every deploy (scripts/deploy-services.sh) and by the throwaway stacks, so a rotated value
# reaches the containers at the next deploy. This file holds the one list of the secrets.
#
# Usage: write-secret-files.sh <postgres|api|web|cloudflared> --out <folder> [--postgres <postgres.env>]
#          [--api <nfhotel-api.env>] [--web <nfhotel-web.env>] [--benchmark <benchmark.env>]
#          [--cloudflared <cloudflared.env>]
#        write-secret-files.sh --list <postgres|api|web|cloudflared>
#   postgres  needs --postgres
#   api       needs --api, and --postgres unless NFHOTEL_API_CONNECTION_STRING is set (the
#             login of nfhotel_app, ADR-0011, passed by scripts/deploy-release.sh)
#   web       needs --web and --api (the API token); --benchmark gives the benchmark token
#   cloudflared  needs --cloudflared (the tunnel token of MIL-040, ADR-0028)
#   --out     written: <folder>/settings.env (the settings without the secrets) and
#             <folder>/<name> for each secret; the folder is 0700, so only its owner (the
#             deploying user) and root read it on the host. The files are 0444 because the
#             container user (app, postgres) differs from the owner and Compose without Swarm
#             cannot change the owner of a bind-mounted secret; inside the container only the
#             one file is mounted. A secret that is not set is written empty, which the
#             applications read as not set.
#   --list    print the names of the secrets of the service, one per line
# Env:   NFHOTEL_ENVIRONMENT  when set, the settings of the API and the web application get
#        ASPNETCORE_ENVIRONMENT with this value instead of their own (scripts/deploy-release.sh sets
#        Production, MIL-040), so the VM never runs a release as Development
# Exit:  0 written; 1 a settings file is missing or a value has a ; or quote in the connection
#        string; 2 usage error
# Needs: bash 4+

set -euo pipefail

# The secrets of each service, by the name the container reads them under.
declare -A SECRETS=(
  [postgres]="POSTGRES_PASSWORD"
  [api]="ConnectionStrings__HotelDatabase Encryption__PassportKey Api__Token Staff__Token__SigningKey Staff__InitialManager__Password MarketAnalysisService__ApiKey"
  [web]="Encryption__PassportKey Api__Token HolidayApi__ApiKey BenchmarkService__Token"
  [cloudflared]="TUNNEL_TOKEN"
)

die() {
  echo "error: $*" >&2
  exit 1
}

usage() {
  echo "usage: write-secret-files.sh <postgres|api|web|cloudflared> --out <folder> [--postgres <file>] [--api <file>] [--web <file>] [--benchmark <file>] [--cloudflared <file>]" >&2
  echo "       write-secret-files.sh --list <postgres|api|web|cloudflared>" >&2
  exit 2
}

# setting <file> <key>: print the value of key in a settings file (empty when it is not set).
# Split at the first = only, so a base64 value that ends with = stays whole.
setting() {
  local line key
  while IFS= read -r line || [[ -n "$line" ]]; do
    line="${line%$'\r'}"
    key="${line%%=*}"
    if [[ "$key" == "$2" && "$line" == *=* ]]; then
      printf '%s' "${line#*=}"
      return 0
    fi
  done <"$1"
}

if [[ "${1:-}" == "--list" ]]; then
  [[ -n "${2:-}" && -n "${SECRETS[${2}]:-}" ]] || usage
  tr ' ' '\n' <<<"${SECRETS[$2]}"
  exit 0
fi

service="${1:-}"
[[ -n "$service" && -n "${SECRETS[$service]:-}" ]] || usage
shift
out="" postgres_env="" api_env="" web_env="" benchmark_env="" cloudflared_env=""
while (($#)); do
  (($# >= 2)) || usage
  case "$1" in
    --out) out="$2" ;;
    --postgres) postgres_env="$2" ;;
    --api) api_env="$2" ;;
    --web) web_env="$2" ;;
    --benchmark) benchmark_env="$2" ;;
    --cloudflared) cloudflared_env="$2" ;;
    *) usage ;;
  esac
  shift 2
done
[[ -n "$out" ]] || usage

need() {
  [[ -n "$2" ]] || die "$service needs --$1"
  [[ -f "$2" ]] || die "$2 is missing; copy its .example and fill it in"
}

declare -A values=()
case "$service" in
  postgres)
    need postgres "$postgres_env"
    own="$postgres_env"
    values[POSTGRES_PASSWORD]="$(setting "$postgres_env" POSTGRES_PASSWORD)"
    ;;
  api)
    need api "$api_env"
    own="$api_env"
    if [[ -n "${NFHOTEL_API_CONNECTION_STRING:-}" ]]; then
      values[ConnectionStrings__HotelDatabase]="$NFHOTEL_API_CONNECTION_STRING"
    else
      # Before the roles of ADR-0011 exist, the API logs in as the superuser of postgres.env.
      need postgres "$postgres_env"
      password="$(setting "$postgres_env" POSTGRES_PASSWORD)"
      [[ "$password" != *[\;\"\']* ]] || die "the password in $postgres_env has a ; or a quote (use openssl rand -hex 24)"
      values[ConnectionStrings__HotelDatabase]="Host=postgres;Port=5432;Database=$(setting "$postgres_env" POSTGRES_DB);Username=$(setting "$postgres_env" POSTGRES_USER);Password=$password"
    fi
    for name in Encryption__PassportKey Api__Token Staff__Token__SigningKey Staff__InitialManager__Password MarketAnalysisService__ApiKey; do
      values[$name]="$(setting "$api_env" "$name")"
    done
    ;;
  web)
    need web "$web_env"
    need api "$api_env"
    own="$web_env"
    for name in Encryption__PassportKey HolidayApi__ApiKey BenchmarkService__Token; do
      values[$name]="$(setting "$web_env" "$name")"
    done
    # The API token and the benchmark token are written in the settings of their own service only.
    values[Api__Token]="$(setting "$api_env" Api__Token)"
    if [[ -n "$benchmark_env" && -f "$benchmark_env" ]]; then
      values[BenchmarkService__Token]="$(setting "$benchmark_env" NFBENCH_API_TOKEN)"
    fi
    ;;
  cloudflared)
    need cloudflared "$cloudflared_env"
    own="$cloudflared_env"
    values[TUNNEL_TOKEN]="$(setting "$cloudflared_env" TUNNEL_TOKEN)"
    [[ -n "${values[TUNNEL_TOKEN]}" ]] || die "TUNNEL_TOKEN in $cloudflared_env is empty"
    ;;
esac

mkdir -p "$out"
chmod 0700 "$out"
umask 077
for name in ${SECRETS[$service]}; do
  # Replace the file whole: an editor or a half-written file never reaches a starting container.
  printf '%s' "${values[$name]}" >"$out/.$name.new"
  chmod 0444 "$out/.$name.new"
  mv -f "$out/.$name.new" "$out/$name"
done

# The settings without the secrets: each line whose key is a secret is left out.
pattern="^($(tr ' ' '|' <<<"${SECRETS[$service]}"))="
grep -Ev "$pattern" "$own" | tr -d '\r' >"$out/.settings.env.new" || true
# The deploy of a release decides the environment, not the settings file (MIL-040).
if [[ -n "${NFHOTEL_ENVIRONMENT:-}" && ("$service" == api || "$service" == web) ]]; then
  [[ "$NFHOTEL_ENVIRONMENT" =~ ^[A-Za-z]+$ ]] || die "NFHOTEL_ENVIRONMENT '$NFHOTEL_ENVIRONMENT' is not a name"
  grep -v "^ASPNETCORE_ENVIRONMENT=" "$out/.settings.env.new" >"$out/.settings.env.tmp" || true
  echo "ASPNETCORE_ENVIRONMENT=$NFHOTEL_ENVIRONMENT" >>"$out/.settings.env.tmp"
  mv -f "$out/.settings.env.tmp" "$out/.settings.env.new"
fi
mv -f "$out/.settings.env.new" "$out/settings.env"
echo "wrote the settings and $(wc -w <<<"${SECRETS[$service]}") secret files of $service to $out"
