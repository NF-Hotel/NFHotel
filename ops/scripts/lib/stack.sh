#!/usr/bin/env bash
# Shared steps of the NF Hotel stacks that run straight in the local Docker, with no VM:
# the throwaway stack of the end-to-end tests (scripts/e2e-stack.sh, MIL-027) and the light
# development mode (scripts/dev-lite.sh, ADR-0035). PostgreSQL 16, the API, the web
# application and nginx run from the compose files of deploy/, in the deployed shape: TLS at
# nginx, the API and the database on internal networks, secrets as files (ADR-0024). The mail of
# the API goes through the forwarder of deploy/mail-relay, as on the VM, to the mail catcher
# Mailpit of deploy/mailpit instead of a relay (MIL-088, ADR-0053 decision 7). Source it after
# lib/common.sh; do not run it.
#
# Usage: . "$ROOT/scripts/lib/stack.sh"; stack_init <name>; then the stack_* functions
# Needs: bash 4+, docker with the compose plugin, openssl, git (for the commit of the build)

# The folders of deploy/ in start order: the mail catcher and its forwarder before the API.
readonly STACK_FOLDERS=(postgres mailpit mail-relay nfhotel-api nfhotel-web nginx)
STACK_OPS="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

# stack_init <name>: the prefix of the compose projects and networks.
stack_init() {
  [[ "$1" =~ ^[a-z0-9][a-z0-9-]*$ ]] || die "the name '$1' must be lower case letters, digits and -"
  STACK_NAME="$1"
  STACK_PROXY_NET="$1-proxy"
  STACK_API_NET="$1-api"
  STACK_DB_NET="$1-db"
  STACK_PYTHON_NET="$1-python"
  STACK_MAIL_NET="$1-mail"
  STACK_MAIL_OUT_NET="$1-mail-out"
}

# stack_native <path>: a native path for the Docker daemon (Git Bash on Windows has cygpath).
stack_native() {
  if command -v cygpath >/dev/null 2>&1; then cygpath -m "$1"; else echo "$1"; fi
}

# stack_secret [bytes]: a random secret in hex, free of ; and quotes (see the API compose file).
stack_secret() { openssl rand -hex "${1:-24}"; }

# stack_compose <folder of deploy/> <compose arguments...>: compose for one service of the stack.
stack_compose() {
  local folder="$1"
  shift
  NFHOTEL_PROXY_NETWORK="$STACK_PROXY_NET" NFHOTEL_API_NETWORK="$STACK_API_NET" NFHOTEL_DB_NETWORK="$STACK_DB_NET" \
    NFHOTEL_PYTHON_NETWORK="$STACK_PYTHON_NET" NFHOTEL_MAIL_NETWORK="$STACK_MAIL_NET" \
    NFHOTEL_MAIL_OUT_NETWORK="$STACK_MAIL_OUT_NET" \
    docker compose --project-name "$STACK_NAME-$folder" --file "$(stack_native "$STACK_OPS/deploy/$folder/docker-compose.yml")" "$@"
}

# stack_down [--volumes]: remove the containers and networks; --volumes removes the volumes (the database) too.
# It goes by the labels compose puts on what it makes, not through `compose down`: compose before 5 refuses down
# when the settings files of a compose file are missing, as they are in a new shell.
stack_down() {
  local folder label i
  local -a ids
  for ((i = ${#STACK_FOLDERS[@]} - 1; i >= 0; i--)); do
    folder="${STACK_FOLDERS[$i]}"
    label="label=com.docker.compose.project=$STACK_NAME-$folder"
    mapfile -t ids < <(docker ps -aq --filter "$label")
    ((${#ids[@]} == 0)) || docker rm -f "${ids[@]}" >/dev/null
    if [[ "${1:-}" == "--volumes" ]]; then
      mapfile -t ids < <(docker volume ls -q --filter "$label")
      ((${#ids[@]} == 0)) || docker volume rm -f "${ids[@]}" >/dev/null
    fi
  done
  docker network rm "$STACK_PROXY_NET" "$STACK_API_NET" "$STACK_DB_NET" "$STACK_PYTHON_NET" "$STACK_MAIL_NET" \
    "$STACK_MAIL_OUT_NET" >/dev/null 2>&1 || true
}

# stack_networks: create the networks that are missing; the API, database, python and mail networks are internal.
# The python network carries the calls to the Python services (ADR-0039), which the stacks do not run; the
# compose files of the web application and the API name it, so it must exist. The way out of the forwarder of the
# mail has a route out, as on the VM, and the catcher waits there for it. The proxy network is made with its
# subnet named: Docker picks a free subnet for it, and the network is made again with that subnet, because the
# Docker of the GitHub runners gives a container a fixed address (docker run --ip) only on a network whose subnet was
# named, and check-e2e-stack.sh gives its client one (MIL-087).
stack_networks() {
  local net subnet
  for net in "$STACK_PROXY_NET" "$STACK_API_NET" "$STACK_DB_NET" "$STACK_PYTHON_NET" "$STACK_MAIL_NET" "$STACK_MAIL_OUT_NET"; do
    docker network inspect "$net" >/dev/null 2>&1 && continue
    if [[ "$net" == "$STACK_PROXY_NET" ]]; then
      docker network create "$net" >/dev/null
      subnet="$(docker network inspect "$net" --format '{{range .IPAM.Config}}{{.Subnet}} {{end}}' |
        tr ' ' '\n' | grep -m 1 -E '^[0-9.]+/[0-9]+$')" || die "Docker gave the network $net no IPv4 subnet"
      docker network rm "$net" >/dev/null
      docker network create --subnet "$subnet" "$net" >/dev/null
    elif [[ "$net" == "$STACK_MAIL_OUT_NET" ]]; then
      docker network create "$net" >/dev/null
    else
      docker network create --internal "$net" >/dev/null
    fi
  done
}

# stack_write_settings <settings folder> <manager user name> <manager password> <must change: true|false>:
# write postgres.env, nfhotel-api.env, nfhotel-web.env, nginx.env and the TLS pair, with secrets made for
# this stack. The folder is the owner's only.
stack_write_settings() {
  local settings="$1" manager="$2" password="$3" must_change="$4"
  mkdir -p "$settings"
  chmod 700 "$settings"
  (
    umask 077
    cat >"$settings/postgres.env" <<EOF
POSTGRES_USER=nfhotel
POSTGRES_PASSWORD=$(stack_secret)
POSTGRES_DB=nfhotel
EOF
    cat >"$settings/nfhotel-api.env" <<EOF
ASPNETCORE_ENVIRONMENT=Production
Encryption__PassportKey=$(openssl rand -base64 32)
HotelTime__TimeZoneId=Europe/Copenhagen
Api__Token=$(stack_secret)
Staff__Token__SigningKey=$(stack_secret 32)
Staff__InitialManager__UserName=$manager
Staff__InitialManager__Password=$password
Staff__InitialManager__MustChangePassword=$must_change
Database__MigrateOnStart=true
EOF
    cat >"$settings/nfhotel-web.env" <<EOF
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
Tls__Require=true
HotelTime__TimeZoneId=Europe/Copenhagen
EOF
    cat >"$settings/nginx.env" <<EOF
NGINX_SERVER_NAME=_
NGINX_REDIRECT_PORT=
NGINX_HSTS=
UPSTREAM_HOST=nfhotel-web
UPSTREAM_PORT=8080
EOF
  )
  bash "$STACK_OPS/scripts/make-dev-certificate.sh" "$settings/tls" nginx >/dev/null ||
    die "cannot make the development certificate"
}

# stack_setting <settings file> <key>: the value of the key on its last line; empty when it has none.
stack_setting() { sed -n "s/^$2=//p" "$1" | tr -d '\r' | tail -n 1; }

# stack_set_settings <settings file> <key=value>...: give each key its value, on a line at the end of the file in
# place of the lines it had. The file is written over, not replaced, so it keeps its mode.
stack_set_settings() {
  local file="$1" pair keys=""
  shift
  for pair in "$@"; do keys+="${keys:+|}${pair%%=*}"; done
  (
    umask 077
    {
      grep -Ev "^($keys)=" "$file" || true
      printf '%s\n' "$@"
    } >"$file.new"
  )
  cat "$file.new" >"$file"
  rm -f "$file.new"
}

# stack_write_mail_settings <settings folder> <site address> <verification: true|false>: the mail of the stack
# (MIL-088, ADR-0053 decision 7). The certificate of the catcher is made once, for the host name mailpit, in
# <settings folder>/mail-tls, and nfhotel-api.env gets the settings of the mail: the relay is the catcher, reached
# through the forwarder with TLS from the first byte as on the VM, and the API trusts the one certificate of the
# catcher by its SHA-256 thumbprint. The links of the mails point at the site address. The password of the account
# and BookingVerification__Enabled are set when they are missing and kept otherwise, so settings that are kept
# between runs keep them; the other values are set at every run.
stack_write_mail_settings() {
  local settings="$1" site="$2" verification="$3" file="$1/nfhotel-api.env" thumbprint password
  bash "$STACK_OPS/scripts/make-dev-certificate.sh" "$settings/mail-tls" mailpit >/dev/null ||
    die "cannot make the certificate of the mail catcher"
  # The SHA-256 of the DER form of the certificate in 64 lower-case hex digits. The certificate goes in on standard
  # input, so a native openssl of Windows needs no path.
  thumbprint="$(openssl x509 -noout -fingerprint -sha256 <"$settings/mail-tls/fullchain.pem" | tr -d '\r' |
    sed 's/^.*=//' | tr -d ':' | tr 'A-F' 'a-f')"
  [[ "$thumbprint" =~ ^[0-9a-f]{64}$ ]] || die "cannot read the thumbprint of the certificate of the mail catcher"
  password="$(stack_setting "$file" Smtp__Password)"
  [[ -n "$password" ]] || password="$(stack_secret)"
  [[ -n "$(stack_setting "$file" BookingVerification__Enabled)" ]] ||
    stack_set_settings "$file" "BookingVerification__Enabled=$verification"
  stack_set_settings "$file" Smtp__Host=mailpit Smtp__Port=1025 Smtp__Security=SslOnConnect Smtp__UserName=stack \
    "Smtp__Password=$password" Smtp__FromAddress=booking@example.com "Smtp__FromName=NF Hotel" \
    Smtp__Forwarder=mail-relay:1465 "Smtp__CertificateThumbprint=$thumbprint" "BookingVerification__SiteAddress=$site"
}

# stack_env <settings folder> [<app dir>]: export what the compose files of the stack read: the settings and secret
# files, the images of this stack, the certificate of the catcher, and the host and the port of the relay for the
# forwarder, from nfhotel-api.env as on the VM; with <app dir>, also the checkout to build from and its commit.
stack_env() {
  local native_settings
  native_settings="$(stack_native "$1")"
  export POSTGRES_ENV_FILE="$native_settings/postgres/settings.env" POSTGRES_SECRETS="$native_settings/postgres"
  export NFHOTEL_API_ENV_FILE="$native_settings/api/settings.env" NFHOTEL_API_SECRETS="$native_settings/api"
  export NFHOTEL_WEB_ENV_FILE="$native_settings/web/settings.env" NFHOTEL_WEB_SECRETS="$native_settings/web"
  export NFHOTEL_WEB_IMAGE="$STACK_NAME-web:local" NFHOTEL_API_IMAGE="$STACK_NAME-api:local"
  export NFHOTEL_NGINX_IMAGE="$STACK_NAME-nginx:local" MAILPIT_TLS_DIR="$native_settings/mail-tls"
  export MAIL_RELAY_HOST MAIL_RELAY_PORT
  MAIL_RELAY_HOST="$(stack_setting "$1/nfhotel-api.env" Smtp__Host)"
  MAIL_RELAY_PORT="$(stack_setting "$1/nfhotel-api.env" Smtp__Port)"
  if [[ -n "${2:-}" ]]; then
    export NFHOTEL_APP_DIR NFHOTEL_APP_SHA
    NFHOTEL_APP_DIR="$(stack_native "$(cd "$2" && pwd)")"
    NFHOTEL_APP_SHA="$(git -C "$2" rev-parse HEAD 2>/dev/null || echo unknown)"
  fi
}

# stack_start <settings folder> <app dir> [<nginx compose arguments...>]: split the settings into secret
# files, build the API and the web application from <app dir> and the proxy from deploy/nginx
# (ADR-0040), and start the services, each waiting for its health check: PostgreSQL, the mail catcher
# and its forwarder (stack_write_mail_settings first), the API, the web application and nginx. The
# arguments after <app dir> go to compose for nginx (extra --file overrides, as native paths:
# stack_native).
# Env:   STACK_MAILPIT_FILE  a compose file that overrides the one of the catcher (the light mode
#        publishes its web interface on the loopback)
stack_start() {
  local settings="$1" app_dir="$2" service native_settings
  shift 2
  [[ -f "$app_dir/Dockerfile" && -f "$app_dir/Dockerfile.api" ]] ||
    die "$app_dir is not an NF-Hotel/NFHotel checkout with Dockerfile and Dockerfile.api"
  docker compose version >/dev/null 2>&1 || die "the docker compose plugin is missing"
  for service in postgres api web; do
    bash "$STACK_OPS/scripts/write-secret-files.sh" "$service" --out "$settings/$service" \
      --postgres "$settings/postgres.env" --api "$settings/nfhotel-api.env" --web "$settings/nfhotel-web.env" >/dev/null ||
      die "cannot write the secret files of $service"
  done
  native_settings="$(stack_native "$settings")"
  stack_env "$settings" "$app_dir"

  stack_networks
  echo "==> starting PostgreSQL"
  stack_compose postgres up --detach --wait --wait-timeout 120 >/dev/null ||
    die "PostgreSQL did not become healthy"
  echo "==> starting the mail catcher and the forwarder of the mail"
  stack_compose mailpit ${STACK_MAILPIT_FILE:+--file "$(stack_native "$STACK_MAILPIT_FILE")"} \
    up --detach --wait --wait-timeout 120 >/dev/null || die "the mail catcher did not become healthy"
  stack_compose mail-relay up --detach --wait --wait-timeout 120 >/dev/null ||
    die "the forwarder of the mail did not become healthy"
  echo "==> building and starting the API (migrations at start)"
  stack_compose nfhotel-api up --detach --build --wait --wait-timeout 240 ||
    die "the API did not become healthy"
  echo "==> building and starting the web application"
  stack_compose nfhotel-web up --detach --build --wait --wait-timeout 240 ||
    die "the web application did not become healthy"
  echo "==> building and starting nginx (TLS with a development certificate)"
  NGINX_ENV_FILE="$native_settings/nginx.env" NGINX_TLS_DIR="$native_settings/tls" \
    stack_compose nginx "$@" up --detach --build --wait --wait-timeout 60 >/dev/null ||
    die "nginx did not start"
}

# stack_create_staff <user name> <role> <password>: make a staff account with the create-staff command of
# the API image; the password is in its environment only, never on a command line. It fails when the
# account exists already.
stack_create_staff() {
  STAFF_PASSWORD="$3" stack_compose nfhotel-api run --rm --no-deps -e STAFF_PASSWORD nfhotel-api create-staff "$1" "$2" >/dev/null
}

# stack_check_secret_env: fail when a secret is in the environment of a container of the stack (MIL-035).
stack_check_secret_env() {
  bash "$STACK_OPS/scripts/check-secret-env.sh" --name "$STACK_NAME-" || die "a secret is in the environment of a container"
}
