#!/usr/bin/env bash
# Start the application the way it is deployed, on this machine, so a developer can see a change.
#
# ADR-0003: the services run as Docker containers in one virtual machine on Proxmox VE. For development the
# Proxmox VE node is a local Docker container (ops/deploy/proxmox, the image of MIL-003), so the chain is
#   this machine -> Proxmox VE container -> VM (Debian, Docker) -> postgres, ollama, api, web, analysis, benchmark, nginx
# The script does the whole chain and then reuses it, so the next run only ships your working tree to the
# VM and rebuilds the services you name (ops/scripts/deploy.sh does the VM and the services):
#   1. check that Docker can use /dev/kvm (on WSL2 start a distribution that loads the KVM modules first)
#   2. make a missing pve.env, deploy.env and service settings file from its .example, with fresh secrets;
#      an existing file is never changed
#   3. start the Proxmox VE container (created once, then only started) and set its root password
#   4. ops/scripts/deploy.sh --local: create the VM when it is missing, copy this working tree into it, deploy
#   5. publish the site on this machine: https://localhost:8443/ (a development certificate); the mobile app
#      (MobileApp repository) reaches the sign-in and the rooms of the API at https://localhost:8443/mobile-api/
#      (from the Android emulator: https://10.0.2.2:8443/mobile-api/), ADR-0015
#   6. create one back office account per role (Manager, Receptionist, Housekeeping) and print the user names and
#      passwords at the end. This is for development only: the passwords are printed only when
#      ASPNETCORE_ENVIRONMENT=Development in nfhotel-api.env, and never in a release deployment.
#      The generated passwords are kept in ops/deploy/nfhotel-api/dev-staff.env (git-ignored).
#
# Usage: dev-up.sh [--check] [--all] [--seed] [--open] [--down] [--destroy] [web|api|analysis|benchmark|nginx ...]
#   --check    only test that this machine can run it (Docker, KVM, tools, ports, disk, memory) and say how to fix
#              each problem; starts nothing. The same test runs at the start of every other run.
#   web, api   rebuild and restart only these (default: web and api, the code of this repository)
#   analysis   rebuild the market analysis service from MARKET_ANALYSIS_REF in ops/deploy/market-analysis/market-analysis.env
#   benchmark  rebuild the benchmark service from BENCHMARK_REF in ops/deploy/benchmark/benchmark.env
#   nginx      recreate the proxy, to read a changed template or the mobile-app route (ADR-0015)
#   --all      rebuild and restart every service of the seven
#   --seed     load db/seed_dev.sql (sample rooms, guests and bookings) into the database
#   --open     open the site in the default browser when it is ready
#   --down     stop the seven services in the VM; the database volume, the VM and Proxmox are kept
#   --destroy  remove the Proxmox VE container, with the VM and the database in it (asks nothing: development only)
# Env:   DEV_SITE_PORT port of the site on this machine (default 8443); DEV_PVE_PORT Proxmox web UI (default 8006);
#        DEV_MODELS the Ollama models to pull, space separated (default "llama3.2:3b phi3.5"; empty pulls none)
# Exit:  0 the site answers; 1 a step failed; 2 usage error
# Needs: bash 4+, docker, curl, ssh, tar. The first run downloads the Proxmox image and the Debian image and
# installs Docker in the VM: allow ten minutes or more. Later runs take seconds to a minute.
# Do not use on a shared host: dockurr/proxmox is a third-party image that runs privileged.

set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# The operations of the application, in this repository since ADR-0016.
ops="$root/ops"
export MSYS_NO_PATHCONV=1

site_port="${DEV_SITE_PORT:-8443}"
pve_port="${DEV_PVE_PORT:-8006}"
image="dockurr/proxmox:latest"
default_container="nfhotel-pve-proxmox-1"
core=(postgres ollama api web analysis benchmark nginx)
rebuild=()
seed=0
open_site=0
check_only=0
down=0
destroy=0

die() {
  echo "error: $*" >&2
  exit 1
}

usage() {
  sed -n '2,/^set -euo/p' "${BASH_SOURCE[0]}" | sed '$d' | sed 's/^# \{0,1\}//'
}

for arg in "$@"; do
  case "$arg" in
    --check) check_only=1 ;;
    --all) rebuild=(api web) ;;
    --seed) seed=1 ;;
    --open) open_site=1 ;;
    --down) down=1 ;;
    --destroy) destroy=1 ;;
    -h | --help)
      usage
      exit 0
      ;;
    web | api | analysis | benchmark | nginx) rebuild+=("$arg") ;;
    *)
      echo "error: unknown argument: $arg" >&2
      usage >&2
      exit 2
      ;;
  esac
done
if [[ ${#rebuild[@]} -eq 0 ]]; then
  rebuild=(api web)
fi

# Tests that this machine can run the chain below, and says how to fix each problem. Every line starts with ok,
# warning or PROBLEM; one PROBLEM fails the check. Nothing is started or changed.
preflight() {
  local problems=0 name tool kib mem_mib need_mib vm_mib running_container

  report() { # status text
    printf '  %-8s %s\n' "$1" "$2"
    [[ "$1" == "PROBLEM" ]] && problems=$((problems + 1))
    return 0
  }

  echo "Checking this machine for dev-up.sh"
  if ((BASH_VERSINFO[0] < 4)); then
    report PROBLEM "bash ${BASH_VERSION} is too old; install bash 4 or later (on Windows: Git for Windows)"
  else
    report ok "bash ${BASH_VERSION%%(*}"
  fi

  for tool in curl ssh tar git; do
    if command -v "$tool" >/dev/null 2>&1; then
      report ok "$tool"
    else
      report PROBLEM "$tool is not on PATH (on Windows: install Git for Windows and use Git Bash)"
    fi
  done

  if ! command -v docker >/dev/null 2>&1; then
    report PROBLEM "docker is not on PATH; install Docker Desktop (Windows, macOS) or Docker Engine (Linux)"
  elif ! docker info >/dev/null 2>&1; then
    report PROBLEM "the Docker daemon does not answer; start Docker Desktop, wait until it says running, and try again"
  else
    report ok "docker $(docker version --format '{{.Server.Version}}' 2>/dev/null)"
    if docker compose version >/dev/null 2>&1; then
      report ok "docker compose"
    else
      report PROBLEM "the docker compose plugin is missing; update Docker Desktop, or install docker-compose-plugin"
    fi

    if docker run --rm --privileged alpine sh -c 'test -e /dev/kvm' >/dev/null 2>&1; then
      report ok "/dev/kvm is available to Docker (Proxmox VE needs it)"
    else
      report PROBLEM "/dev/kvm is not available to Docker. On Windows with WSL2: turn on virtualization in the BIOS, open a WSL distribution that loads the KVM modules (for example: wsl -d Debian), and try again. On Linux: install and load kvm for your CPU"
    fi

    kib="$(docker info --format '{{.MemTotal}}' 2>/dev/null || echo 0)"
    mem_mib=$((kib / 1024 / 1024))
    vm_mib="$(value_of "$ops/deploy.env" VM_MEMORY 2>/dev/null || true)"
    [[ "$vm_mib" =~ ^[0-9]+$ ]] || vm_mib=12288
    need_mib=$((vm_mib + 2048))
    if ((mem_mib >= need_mib)); then
      report ok "Docker has ${mem_mib} MiB of memory (the VM uses ${vm_mib} MiB)"
    else
      report warning "Docker has ${mem_mib} MiB of memory and about ${need_mib} MiB are needed; on Windows raise the memory in %UserProfile%\\.wslconfig (memory=16GB) and run: wsl --shutdown"
    fi
  fi

  # The ports are free, unless the container of an earlier run owns them.
  running_container="$(value_of "$ops/deploy.env" PVE_CONTAINER 2>/dev/null || true)"
  if [[ -z "$running_container" || "$(docker inspect -f '{{.State.Running}}' "$running_container" 2>/dev/null || true)" != "true" ]]; then
    for name in "$site_port" "$pve_port"; do
      if (exec 3<>"/dev/tcp/127.0.0.1/${name}") 2>/dev/null; then
        report PROBLEM "port ${name} on this machine is in use; stop what uses it, or set DEV_SITE_PORT or DEV_PVE_PORT"
      else
        report ok "port ${name} is free"
      fi
    done
  else
    report ok "ports ${site_port} and ${pve_port} belong to the running Proxmox container"
  fi

  kib="$(df -Pk "$root" 2>/dev/null | awk 'NR==2 {print $4}')"
  if [[ -n "$kib" ]] && ((kib / 1024 / 1024 < 40)); then
    report warning "only $((kib / 1024 / 1024)) GiB free on the disk of this folder; the first run needs about 40 GiB for the images, the VM and the models"
  else
    report ok "disk space"
  fi

  if ((problems > 0)); then
    echo "$problems problem(s). Fix them and run: bash scripts/dev-up.sh --check"
    return 1
  fi
  echo "This machine can run dev-up.sh."
}

# A random secret: base64 of the given number of bytes, without the characters / + = that need quoting.
secret() {
  if command -v openssl >/dev/null 2>&1; then
    openssl rand -base64 "$1" | tr -d '\n/+='
  else
    head -c "$1" /dev/urandom | base64 | tr -d '\n/+='
  fi
}

# The value of KEY in a settings file (no shell evaluation).
value_of() {
  sed -n "s/^$2=//p" "$1" | tr -d '\r' | head -n 1
}

# Copy a settings file from its example when it is missing; returns 0 when it was made.
make_env() {
  local dir="$1" name="$2"
  [[ -f "$dir/$name" ]] && return 1
  [[ -f "$dir/$name.example" ]] || die "$dir/$name.example is missing"
  cp "$dir/$name.example" "$dir/$name"
  echo "==> made $dir/$name from the example"
}

preflight || exit 1
if ((check_only)); then
  exit 0
fi

pve_env="$ops/pve.env"
deploy_env="$ops/deploy.env"

# move_old_settings: before ADR-0016 the git-ignored settings files were in the root and in
# deploy/ of this checkout. Copy each one that is not in ops/ yet, once, so an existing
# development VM keeps its passwords. The old files are left where they are.
move_old_settings() {
  local old new
  for old in "$root/pve.env" "$root/deploy.env"; do
    new="$ops/$(basename "$old")"
    [[ -f "$old" && ! -f "$new" ]] && cp -p "$old" "$new" && echo "==> copied $old to $new"
  done
  [[ -d "$root/deploy" ]] || return 0
  while IFS= read -r old; do
    new="$ops/${old#"$root"/}"
    [[ -f "$new" ]] && continue
    mkdir -p "$(dirname "$new")" && cp -p "$old" "$new" && echo "==> copied $old to $new"
  done < <(find "$root/deploy" -type f \( -name "*.env" \))
}
move_old_settings

if make_env "$ops" pve.env; then
  sed -i "s#^PVE_PASSWORD=.*#PVE_PASSWORD=$(secret 18)#" "$pve_env"
fi
if make_env "$ops" deploy.env; then
  # Development: the Proxmox node is the local container, its storage is "local", and the script
  # makes its own ssh key for the VM user.
  sed -i \
    -e "s#^PVE_SSH=.*#PVE_SSH=#" \
    -e "s#^PVE_CONTAINER=.*#PVE_CONTAINER=${default_container}#" \
    -e "s#^VM_STORAGE=.*#VM_STORAGE=local#" \
    -e "s#^SSH_KEY_FILE=.*#SSH_KEY_FILE=#" \
    "$deploy_env"
fi

container="$(value_of "$deploy_env" PVE_CONTAINER)"
[[ -n "$container" ]] || die "deploy.env has no PVE_CONTAINER; this script runs Proxmox VE in a local container (set PVE_SSH only for a real node, and use scripts/deploy.sh)"
[[ -z "$(value_of "$deploy_env" PVE_SSH)" ]] || die "deploy.env sets both PVE_SSH and PVE_CONTAINER; keep only PVE_CONTAINER for development"
vm_name="$(value_of "$deploy_env" VM_NAME)"
vm_name="${vm_name:-nfhotel-services}"

if ((destroy)); then
  docker rm -f -v "$container" >/dev/null 2>&1 || true
  echo "Removed the Proxmox VE container $container, with its VM and database."
  exit 0
fi

pg_dir="$ops/deploy/postgres"
api_dir="$ops/deploy/nfhotel-api"
web_dir="$ops/deploy/nfhotel-web"
nginx_dir="$ops/deploy/nginx"

new_api=0
[[ -f "$api_dir/nfhotel-api.env" ]] || new_api=1
new_web=0
[[ -f "$web_dir/nfhotel-web.env" ]] || new_web=1
if make_env "$pg_dir" postgres.env; then
  sed -i "s#^POSTGRES_PASSWORD=.*#POSTGRES_PASSWORD=$(secret 18)#" "$pg_dir/postgres.env"
fi
make_env "$api_dir" nfhotel-api.env || true
make_env "$web_dir" nfhotel-web.env || true
make_env "$nginx_dir" nginx.env || true
# The route of the mobile app (ADR-0015, MIL-029): /mobile-api on the site address, to the sign-in and the rooms of the
# API. It is added to an existing nginx.env too, because it is a development setting; a release deployment never runs
# this script, so it has no such route.
if ! grep -q '^COMPOSE_FILE=' "$nginx_dir/nginx.env"; then
  printf '\nCOMPOSE_FILE=docker-compose.yml:docker-compose.dev.yml\n' >> "$nginx_dir/nginx.env"
fi
make_env "$ops/deploy/ollama" ollama.env || true
# The market analysis service (NF-Hotel/notebook-market-analysis, built from a pinned commit). An older
# market-analysis.env of the frozen service has no API_TOKEN, so it is kept under another name and the new example
# takes its place. The key of the service is made once; the web application sends the same value as HolidayApi__ApiKey.
analysis_dir="$ops/deploy/market-analysis"
if [[ -f "$analysis_dir/market-analysis.env" ]] && ! grep -q '^API_TOKEN=' "$analysis_dir/market-analysis.env"; then
  mv "$analysis_dir/market-analysis.env" "$analysis_dir/market-analysis.env.old-frozen"
  echo "==> kept the old $analysis_dir/market-analysis.env as market-analysis.env.old-frozen"
fi
make_env "$analysis_dir" market-analysis.env || true
if [[ "$(value_of "$analysis_dir/market-analysis.env" API_TOKEN)" == "change-me" ]]; then
  sed -i "s#^API_TOKEN=.*#API_TOKEN=$(secret 32)#" "$analysis_dir/market-analysis.env"
fi
if [[ -z "$(value_of "$web_dir/nfhotel-web.env" HolidayApi__ApiKey)" ]]; then
  sed -i "s#^HolidayApi__ApiKey=.*#HolidayApi__ApiKey=$(value_of "$analysis_dir/market-analysis.env" API_TOKEN)#" "$web_dir/nfhotel-web.env"
fi
# The benchmark service: an older benchmark.env from the notebook has none of the NFBENCH_ settings, so it is
# kept under another name and the new example takes its place. The token of the service is made once; the web
# service gets the same value from this file (ops/scripts/deploy-services.sh).
bench_dir="$ops/deploy/benchmark"
if [[ -f "$bench_dir/benchmark.env" ]] && ! grep -q '^NFBENCH_WEB_URL=' "$bench_dir/benchmark.env"; then
  mv "$bench_dir/benchmark.env" "$bench_dir/benchmark.env.old-notebook"
  echo "==> kept the old $bench_dir/benchmark.env as benchmark.env.old-notebook"
fi
make_env "$bench_dir" benchmark.env || true
if [[ "$(value_of "$bench_dir/benchmark.env" NFBENCH_API_TOKEN)" == "<"* ]]; then
  sed -i "s#^NFBENCH_API_TOKEN=.*#NFBENCH_API_TOKEN=$(secret 32)#" "$bench_dir/benchmark.env"
fi
passport_key="$(openssl rand -base64 32 2>/dev/null || head -c 32 /dev/urandom | base64)"
passport_key="$(printf '%s' "$passport_key" | tr -d '\n')"
if ((new_api)); then
  sed -i \
    -e "s#^Encryption__PassportKey=.*#Encryption__PassportKey=${passport_key}#" \
    -e "s#^Api__Token=.*#Api__Token=$(secret 32)#" \
    -e "s#^Staff__Token__SigningKey=.*#Staff__Token__SigningKey=$(secret 48)#" \
    -e "s#^Staff__InitialManager__Password=.*#Staff__InitialManager__Password=$(secret 18)#" \
    -e "s#^Staff__InitialManager__MustChangePassword=.*#Staff__InitialManager__MustChangePassword=false#" \
    "$api_dir/nfhotel-api.env"
fi
if ((new_web)); then
  sed -i "s#^Encryption__PassportKey=.*#Encryption__PassportKey=${passport_key}#" "$web_dir/nfhotel-web.env"
fi

# Run a command inside the Proxmox VE container.
pve() { docker exec -i "$container" "$@"; }

# Run a command in the VM, through the Proxmox VE container (the VM address is only reachable from there).
vm_ip=""
vm() {
  pve ssh -i /root/.ssh/nfhotel_dev -o StrictHostKeyChecking=accept-new -o BatchMode=yes -o ConnectTimeout=10 \
    "nfhotel@${vm_ip}" "$@"
}

find_vm_ip() {
  local vmid
  vmid="$(pve sh -c "qm list | awk -v n='$vm_name' '\$2==n {print \$1}'" 2>/dev/null | head -n 1 || true)"
  [[ -n "$vmid" ]] || return 1
  pve sh -c "qm guest cmd $vmid network-get-interfaces 2>/dev/null" |
    grep -o '"ip-address" *: *"[0-9.]*"' | grep -o '[0-9]*\.[0-9]*\.[0-9]*\.[0-9]*' |
    grep -v '^127\.' | head -n 1
}

if ((down)); then
  [[ "$(docker inspect -f '{{.State.Running}}' "$container" 2>/dev/null || true)" == "true" ]] ||
    die "the Proxmox VE container $container is not running; nothing to stop"
  exec bash "$ops/scripts/deploy.sh" --skip-vm --down "${core[@]}"
fi

# 1. KVM.
docker run --rm --privileged alpine sh -c 'test -e /dev/kvm' >/dev/null 2>&1 ||
  die "/dev/kvm is not available to Docker; on WSL2 start a distribution that loads the KVM modules (for example: wsl -d Debian) and retry"

# 2. The Proxmox VE container: created once, then only started.
created=0
state="$(docker inspect -f '{{.State.Running}}' "$container" 2>/dev/null || echo missing)"
case "$state" in
  true) ;;
  false)
    echo "==> starting the Proxmox VE container $container"
    docker start "$container" >/dev/null
    ;;
  *)
    echo "==> creating the Proxmox VE container $container (the image is large: the first start takes a while)"
    docker run -d --name "$container" --privileged \
      -e "PVE_PASSWORD=$(value_of "$pve_env" PVE_PASSWORD)" \
      -p "127.0.0.1:${pve_port}:8006" -p "127.0.0.1:${site_port}:8443" \
      -v /lib/modules:/lib/modules:ro "$image" >/dev/null || die "could not start $image"
    created=1
    ;;
esac

echo "==> waiting for the Proxmox API"
ready=0
for _ in $(seq 1 90); do
  code="$(pve curl -sk -o /dev/null -w '%{http_code}' https://127.0.0.1:8006/api2/json/version 2>/dev/null || true)"
  if [[ "$code" == "401" || "$code" == "200" ]]; then
    ready=1
    break
  fi
  sleep 5
done
((ready)) || die "the Proxmox API did not answer in time; see: docker logs $container"

# The root password is set through standard input, so it is never on a command line.
printf 'root:%s\n' "$(value_of "$pve_env" PVE_PASSWORD)" | pve chpasswd || die "could not set the root password in $container"

# 3. The VM and the services. The first run (or a run after --down) deploys all seven; later runs only
#    the services to rebuild.
# The services to deploy: the ones to rebuild, and every service that is not running in the VM yet (the whole
# list on the first run, or when the site does not answer).
if ((created)) || [[ "$(curl -sk -o /dev/null -w '%{http_code}' "https://localhost:${site_port}/" || true)" != "200" ]]; then
  todo=("${core[@]}")
else
  vm_ip="$(find_vm_ip)" || die "could not find the address of the VM"
  running="$(vm "docker ps --format '{{.Names}}'" 2>/dev/null || true)"
  todo=()
  for s in "${core[@]}"; do
    case "$s" in
      postgres) name="nfhotel-postgres-postgres-1" ;;
      ollama) name="ollama-ollama-1" ;;
      api) name="nfhotel-api-nfhotel-api-1" ;;
      web) name="nfhotel-web-nfhotel-web-1" ;;
      analysis) name="market-analysis-market-analysis-api-1" ;;
      benchmark) name="benchmark-benchmark-1" ;;
      nginx) name="nfhotel-nginx-nginx-1" ;;
    esac
    wanted=0
    for r in "${rebuild[@]}"; do [[ "$r" == "$s" ]] && wanted=1; done
    if ((wanted)) || ! grep -qx "$name" <<<"$running"; then
      todo+=("$s")
    fi
  done
fi
echo "==> deploying: ${todo[*]}"
log="$(mktemp)"
bash "$ops/scripts/deploy.sh" --local "${todo[@]}" | tee "${log}"
vm_ip="$(sed -n 's/^Done\. VM [0-9]* at \([0-9.]*\)\.$/\1/p' "${log}" | tail -n 1)"
[[ -n "$vm_ip" ]] || vm_ip="$(find_vm_ip)" || die "could not find the address of the VM"

# Two small models (phi3.5 is the one for a small system), so that the Start page lists something and a run can finish. Pull more by hand:
#   docker exec ollama-ollama-1 ollama pull <model>   (in the VM)
dev_models="${DEV_MODELS:-llama3.2:3b phi3.5}"
installed="$(vm "docker exec ollama-ollama-1 ollama list" 2>/dev/null || true)"
for dev_model in $dev_models; do
  if ! grep -q "^${dev_model}" <<<"$installed"; then
    echo "==> pulling the model ${dev_model} into Ollama (a few GB, the first time only)"
    vm "docker exec ollama-ollama-1 ollama pull ${dev_model}" >/dev/null || die "could not pull the model ${dev_model}"
  fi
done

# The proxy reads its snippets and templates at start; make it read the copy that was just shipped.
vm "docker exec nfhotel-nginx-nginx-1 nginx -s reload" >/dev/null 2>&1 || true

if ((seed)); then
  echo "==> loading db/seed_dev.sql"
  pg_user="$(value_of "$pg_dir/postgres.env" POSTGRES_USER)"
  pg_db="$(value_of "$pg_dir/postgres.env" POSTGRES_DB)"
  vm "cd /opt/nfhotel/ops/deploy/postgres && docker compose --env-file postgres.env exec -T postgres psql -v ON_ERROR_STOP=1 -U ${pg_user} -d ${pg_db}" \
    <"$root/db/seed_dev.sql" >/dev/null || die "the seed script failed (it may already have been loaded)"
fi

# 4. One account per role, for development only. The Manager is the first account that the API makes from
#    nfhotel-api.env; the other two are made with the create-staff command. The passwords are written to a
#    git-ignored file, so the next run prints the same ones.
show_accounts=0
if [[ "$(value_of "$api_dir/nfhotel-api.env" ASPNETCORE_ENVIRONMENT)" == "Development" ]]; then
  show_accounts=1
  staff_file="$api_dir/dev-staff.env"
  touch "$staff_file"
  for entry in receptionist:Receptionist housekeeping:Housekeeping; do
    user="${entry%%:*}"
    role="${entry##*:}"
    if [[ -z "$(value_of "$staff_file" "$user")" ]]; then
      printf '%s=%s
' "$user" "$(secret 18)" >>"$staff_file"
    fi
    # The password goes in through standard input, so it is on no command line.
    printf '%s
' "$(value_of "$staff_file" "$user")" |
      vm "docker exec -i nfhotel-api-nfhotel-api-1 sh -c 'IFS= read -r STAFF_PASSWORD; export STAFF_PASSWORD; exec dotnet NFHotel.Api.dll create-staff ${user} ${role}'"         >/dev/null 2>&1 || true
  done
fi

# 5. Publish the site on this machine: a tunnel inside the Proxmox VE container, from the port that
#    docker published to the proxy of the VM.
pve sh -c "pkill -f 'ssh.*-L 0.0.0.0:8443' || true" >/dev/null 2>&1 || true
docker exec -d "$container" ssh -i /root/.ssh/nfhotel_dev -o StrictHostKeyChecking=accept-new -o BatchMode=yes \
  -o ServerAliveInterval=30 -N -L "0.0.0.0:8443:127.0.0.1:443" "nfhotel@${vm_ip}"

url="https://localhost:${site_port}/"
echo "==> waiting for $url"
ready=0
for _ in $(seq 1 30); do
  if [[ "$(curl -sk -o /dev/null -w '%{http_code}' "$url" || true)" == "200" ]]; then
    ready=1
    break
  fi
  sleep 2
done
((ready)) || die "the site did not answer 200 at $url; look in the VM: docker ps, and the logs of the web service"

echo
echo "Ready: $url (development certificate: accept the browser warning)"
echo "Proxmox VE: https://localhost:${pve_port}/ (user root; the password is PVE_PASSWORD in pve.env)"
if ((show_accounts)); then
  echo
  echo "Back office accounts (development only), sign in at ${url}admin/login:"
  printf '  %-14s %-13s %s
' "user name" "role" "password"
  printf '  %-14s %-13s %s
' "$(value_of "$api_dir/nfhotel-api.env" Staff__InitialManager__UserName)" "Manager"     "$(value_of "$api_dir/nfhotel-api.env" Staff__InitialManager__Password)"
  printf '  %-14s %-13s %s
' "receptionist" "Receptionist" "$(value_of "$api_dir/dev-staff.env" receptionist)"
  printf '  %-14s %-13s %s
' "housekeeping" "Housekeeping" "$(value_of "$api_dir/dev-staff.env" housekeeping)"
else
  echo "Back office accounts are not shown: ASPNETCORE_ENVIRONMENT is not Development in nfhotel-api.env."
fi

if ((open_site)); then
  if command -v xdg-open >/dev/null 2>&1; then
    xdg-open "$url" >/dev/null 2>&1 || true
  elif command -v cmd.exe >/dev/null 2>&1; then
    cmd.exe /c start "" "$url" >/dev/null 2>&1 || true
  elif command -v open >/dev/null 2>&1; then
    open "$url" || true
  fi
fi
