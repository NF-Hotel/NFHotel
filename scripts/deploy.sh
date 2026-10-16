#!/usr/bin/env bash
# Deploy the whole NFHotel server: one virtual machine on Proxmox VE (12 GiB
# memory by default) running every service as a Docker container (ADR-0003).
#
# Steps, each safe to repeat:
#   1. find the VM by name on the Proxmox node, or create it (Debian + Docker,
#      scripts/create-docker-vm.sh) and start it
#   2. find the VM's address, wait for ssh, cloud-init and Docker
#   3. clone or update the repository inside the VM and copy over the git-ignored
#      settings files (deploy/*/*.env and market-analysis.toml)
#   4. run scripts/deploy-services.sh in the VM:
#        postgres   internal network only, no published port
#        ollama     local model server for analysis and benchmark, not published
#        api        the database API: the only service with the database login and the key
#        web        NFHotel, the only service served to the internet; it calls the API
#        analysis   market analysis, loopback of the VM only
#        benchmark  LLM benchmark, loopback of the VM only
#        nginx      reverse proxy in front of them
#
# Development: a Proxmox VE in a local Docker container (PVE_CONTAINER=<name> in
# deploy.env instead of PVE_SSH); the VM is then reached through that container.
#
# Settings: deploy.env in the repository root (copy deploy.env.example). The
# per-service settings files live next to each docker-compose.yml in deploy/
# (copy the *.example files). Nothing secret is stored in the repository.
#
# Usage: deploy.sh [--skip-vm] [--local] [--dry-run] [--no-build] [--down] [service...]
#   service     postgres, ollama, api, web, analysis, benchmark, nginx (default: all seven)
#   --skip-vm   do not create or start the VM; deploy to the existing one
#   --local     ship this working tree to the VM (tracked and untracked files, not the
#               ignored ones) instead of cloning the repository; for unpushed work
#   --dry-run   print what step 1 would run on the node and stop
#   --no-build  start without rebuilding the images
#   --down      stop and remove the named services (data volumes are kept)
# Reach the internal services from your machine with an ssh tunnel, for example
#   ssh -L 8081:127.0.0.1:8081 nfhotel@<vm address>
#   then open analysis.localhost:8081 or benchmark.localhost:8081 in a browser
# Exit:  0 deployed; 1 a step failed; 2 usage error
# Needs: bash 4+, ssh, tar, and key login to the Proxmox node and to the VM user

set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
export MSYS_NO_PATHCONV=1

skip_vm=0
local_code=0
dry_run=0
passthrough=()

die() {
  echo "error: $*" >&2
  exit 1
}

usage() {
  sed -n '2,/^set -euo/p' "${BASH_SOURCE[0]}" | sed '$d' | sed 's/^# \{0,1\}//'
}

for arg in "$@"; do
  case "$arg" in
    --skip-vm) skip_vm=1 ;;
    --local) local_code=1 ;;
    --dry-run) dry_run=1 ;;
    --no-build | --down | postgres | ollama | api | web | analysis | benchmark | nginx)
      passthrough+=("$arg")
      ;;
    -h | --help)
      usage
      exit 0
      ;;
    *)
      echo "error: unknown argument: $arg" >&2
      usage >&2
      exit 2
      ;;
  esac
done

settings="$root/deploy.env"
[[ -f "$settings" ]] || die "$settings is missing; copy deploy.env.example to deploy.env and fill it in"
set -a
# shellcheck disable=SC1090
source <(tr -d '\r' <"$settings")
set +a

PVE_SSH="${PVE_SSH:-}"
PVE_CONTAINER="${PVE_CONTAINER:-}"
[[ -n "$PVE_SSH" || -n "$PVE_CONTAINER" ]] || die "set PVE_SSH (or PVE_CONTAINER) in deploy.env"
[[ -z "$PVE_SSH" || -z "$PVE_CONTAINER" ]] || die "set only one of PVE_SSH and PVE_CONTAINER"
VM_NAME="${VM_NAME:-nfhotel-services}"
VM_ID="${VM_ID:-}"
VM_CORES="${VM_CORES:-4}"
VM_MEMORY="${VM_MEMORY:-12288}"
VM_DISK="${VM_DISK:-60}"
VM_STORAGE="${VM_STORAGE:-local-lvm}"
VM_SNIPPETS="${VM_SNIPPETS:-local}"
VM_BRIDGE="${VM_BRIDGE:-vmbr0}"
VM_IP="${VM_IP:-}"
VM_USER="nfhotel"
REPO_URL="${REPO_URL:-https://github.com/NF-Hotel/NFHotel.git}"
REPO_REF="${REPO_REF:-main}"
# With PVE_CONTAINER (a Proxmox VE in a local Docker container) the VM address is
# only reachable from inside that container, so ssh to the VM goes through it,
# with a dedicated key pair that this script creates and copies into the container.
dev_key="$HOME/.ssh/nfhotel_dev"
# Native Windows tools (ssh-keygen, docker cp) need a Windows path; Git Bash has cygpath.
if command -v cygpath >/dev/null 2>&1; then dev_key="$(cygpath -m "$dev_key")"; fi
if [[ -n "$PVE_CONTAINER" ]]; then
  SSH_KEY_FILE="${SSH_KEY_FILE:-$dev_key.pub}"
else
  SSH_KEY_FILE="${SSH_KEY_FILE:-$HOME/.ssh/id_ed25519.pub}"
fi
SSH_KEY_FILE="${SSH_KEY_FILE/#\~/$HOME}"
remote_dir="/opt/nfhotel"

ssh_opts=(-o StrictHostKeyChecking=accept-new -o BatchMode=yes -o ConnectTimeout=10)
# The remote command is meant to expand on this side.
# shellcheck disable=SC2029
node() {
  if [[ -n "$PVE_CONTAINER" ]]; then
    docker exec -i "$PVE_CONTAINER" sh -c "$*"
  else
    ssh "${ssh_opts[@]}" "$PVE_SSH" "$@"
  fi
}
# shellcheck disable=SC2029
vm() {
  if [[ -n "$PVE_CONTAINER" ]]; then
    docker exec -i "$PVE_CONTAINER" ssh -i /root/.ssh/nfhotel_dev "${ssh_opts[@]}" "$VM_USER@$VM_IP" "$@"
  else
    ssh "${ssh_opts[@]}" "$VM_USER@$VM_IP" "$@"
  fi
}

if [[ -n "$PVE_CONTAINER" ]]; then
  if [[ "$SSH_KEY_FILE" == "$dev_key.pub" && ! -f "$dev_key" ]]; then
    echo "==> creating the development key $dev_key"
    ssh-keygen -q -t ed25519 -N "" -C nfhotel-dev -f "$dev_key"
  fi
  docker exec "$PVE_CONTAINER" mkdir -p /root/.ssh
  docker cp "$dev_key" "$PVE_CONTAINER:/root/.ssh/nfhotel_dev"
  docker exec "$PVE_CONTAINER" chmod 600 /root/.ssh/nfhotel_dev
fi

create_args=(
  --name "$VM_NAME" --cores "$VM_CORES" --memory "$VM_MEMORY"
  --disk "$VM_DISK" --storage "$VM_STORAGE" --snippets "$VM_SNIPPETS"
  --bridge "$VM_BRIDGE" --ssh-key-file "$SSH_KEY_FILE" --start
)
if [[ -n "$PVE_CONTAINER" ]]; then
  create_args+=(--container "$PVE_CONTAINER")
else
  create_args+=(--ssh "$PVE_SSH")
fi
[[ -n "$VM_ID" ]] && create_args+=(--vmid "$VM_ID")

if ((dry_run)); then
  echo "==> dry run: step 1 on ${PVE_CONTAINER:-$PVE_SSH}"
  bash "$root/scripts/create-docker-vm.sh" "${create_args[@]}" --dry-run
  exit 0
fi

# 1. The VM.
if ((! skip_vm)); then
  if [[ -z "$VM_ID" ]]; then
    VM_ID="$(node "qm list | awk -v n='$VM_NAME' '\$2==n {print \$1}'" | head -n 1)"
  fi
  if [[ -n "$VM_ID" ]] && node "qm status $VM_ID" >/dev/null 2>&1; then
    echo "==> VM $VM_ID ($VM_NAME) exists"
    current="$(node "qm config $VM_ID | sed -n 's/^memory: //p'")"
    if [[ "$current" != "$VM_MEMORY" ]]; then
      echo "warning: VM $VM_ID has ${current} MiB, deploy.env asks for ${VM_MEMORY} MiB." >&2
      echo "         Change it with: qm set $VM_ID --memory $VM_MEMORY, then restart the VM." >&2
    fi
    if [[ "$(node "qm status $VM_ID")" != *running* ]]; then
      echo "==> starting VM $VM_ID"
      node "qm start $VM_ID"
    fi
  else
    echo "==> creating VM $VM_NAME (${VM_MEMORY} MiB)"
    # The script prints the VM id as its last line; qm prints progress before it.
    VM_ID="$(bash "$root/scripts/create-docker-vm.sh" "${create_args[@]}" | tail -n 1)" ||
      die "creating the VM failed"
  fi
fi
[[ -n "$VM_ID" ]] || die "no VM found; run without --skip-vm to create it"

# 2. The address, then ssh, cloud-init and Docker.
if [[ -z "$VM_IP" ]]; then
  echo "==> waiting for the address of VM $VM_ID (guest agent)"
  for _ in $(seq 1 60); do
    VM_IP="$(node "qm guest cmd $VM_ID network-get-interfaces 2>/dev/null" |
      grep -o '"ip-address" *: *"[0-9.]*"' | grep -o '[0-9]*\.[0-9]*\.[0-9]*\.[0-9]*' |
      grep -v '^127\.' | head -n 1 || true)"
    [[ -n "$VM_IP" ]] && break
    sleep 5
  done
  [[ -n "$VM_IP" ]] || die "the VM reported no address; set VM_IP in deploy.env"
fi
echo "==> VM address $VM_IP"

echo "==> waiting for ssh, cloud-init and Docker"
for _ in $(seq 1 60); do
  vm true 2>/dev/null && break
  sleep 5
done
vm true 2>/dev/null || die "cannot ssh to $VM_USER@$VM_IP"
vm "sudo cloud-init status --wait >/dev/null 2>&1 || true"
vm "docker compose version >/dev/null 2>&1" || die "Docker is not ready in the VM; see /var/log/cloud-init-output.log"

# 3. The code and the settings files.
if ((local_code)); then
  echo "==> copying the working tree to $remote_dir"
  vm "sudo mkdir -p $remote_dir && sudo chown $VM_USER $remote_dir"
  (cd "$root" && git ls-files -co --exclude-standard -z |
    tar --null --exclude=framework -T - -cf -) | vm "tar -xf - -C $remote_dir" ||
    die "could not copy the working tree to the VM"
else
  echo "==> updating $remote_dir from $REPO_URL ($REPO_REF)"
  vm "command -v git >/dev/null || sudo apt-get install -y git >/dev/null;       sudo mkdir -p $remote_dir && sudo chown $VM_USER $remote_dir;       if [ -d $remote_dir/.git ]; then git -C $remote_dir fetch --depth 1 origin $REPO_REF && git -C $remote_dir checkout -q FETCH_HEAD;       else git clone --depth 1 --branch $REPO_REF $REPO_URL $remote_dir; fi" ||
    die "could not fetch the repository in the VM"
fi

echo "==> copying the settings files"
mapfile -t settings_files < <(cd "$root" && find deploy -type f \( -name '*.env' -o -name 'market-analysis.toml' \) | sort)
((${#settings_files[@]} > 0)) || die "no deploy/*/*.env files found; copy the *.example files first"
(cd "$root" && tar -cf - "${settings_files[@]}") | vm "tar -xf - -C $remote_dir && chmod 600 $remote_dir/deploy/*/*.env"

# 4. The containers.
echo "==> deploying the services in the VM"
vm "cd $remote_dir && bash scripts/deploy-services.sh ${passthrough[*]:-}" || die "deploying the services failed"

echo
echo "Done. VM $VM_ID at $VM_IP."
echo "Public:   port 80 of $VM_IP serves NFHotel web, through nginx"
echo "Internal: ssh -L 8081:127.0.0.1:8081 $VM_USER@$VM_IP, then analysis.localhost:8081 and benchmark.localhost:8081"
