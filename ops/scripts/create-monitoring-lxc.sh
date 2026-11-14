#!/usr/bin/env bash
# Create the LXC container that runs the monitoring of NF Hotel (ADR-0020, MIL-023 task 7).
# S03 runs it once as root on the Proxmox node. It changes nothing without --apply.
#
#   1. refuse an ID that a container or a VM uses, and a template that is not downloaded
#   2. create an unprivileged Debian 13 container with nesting and keyctl (for Docker),
#      2 cores, 4 GiB memory and a 32 GiB disk, started at boot
#   3. start it and install, inside it, Docker Engine, containerd, runc and the compose plugin
#      from Docker's signed apt repository, the packages of the VM (create-docker-vm.sh,
#      ADR-0043), and git and Tailscale (from the signed Tailscale apt repository)
#   4. clone NF-Hotel/NFHotel to /opt/nfhotel-monitoring in it, and install Debian's security
#      updates from there with scripts/install-patching.sh, as on the VM
#   5. print the steps that need S03: tailscale up (an interactive sign-in), the tag
#      tag:monitoring, the settings and secrets, scripts/deploy-monitoring.sh, and
#      scripts/pull-monitoring.sh --install, which deploys every later change (ADR-0044, MIL-051)
#
# Usage: create-monitoring-lxc.sh <id> [--apply]
#   <id>     a free ID that neither `pct list` nor `qm list` shows, for example 102
#   --apply  create the container; without it the script prints what it would do
# Env:   MONITORING_TEMPLATE (default: the newest debian-13-standard template that
#        `pveam list local` shows, so a newer download needs no setting),
#        MONITORING_STORAGE (default local-lvm), MONITORING_BRIDGE (default vmbr0)
# Exit:  0 created (or a dry run); 1 a step failed; 2 usage error
# Needs: bash 4+, root on a Proxmox VE node (pct, qm, pveam)

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=ops/scripts/lib/common.sh
. "$ROOT/scripts/lib/common.sh"
template="${MONITORING_TEMPLATE:-}"
storage="${MONITORING_STORAGE:-local-lvm}"
bridge="${MONITORING_BRIDGE:-vmbr0}"
id="" apply=0

for arg in "$@"; do
  case "$arg" in
    --apply) apply=1 ;;
    -h | --help)
      usage
      exit 0
      ;;
    [0-9]*) id="$arg" ;;
    *)
      usage >&2
      exit 2
      ;;
  esac
done
[[ "$id" =~ ^[1-9][0-9]{2,8}$ ]] || {
  usage >&2
  exit 2
}
# The template: the one given, or the newest Debian 13 standard template downloaded to the
# storage local. A fixed default goes out of date as soon as Proxmox offers a newer one.
if [[ -z "$template" ]]; then
  template="$(pveam list local 2>/dev/null | awk '{print $1}' |
    grep -E '^local:vztmpl/debian-13-standard_[0-9.]+-[0-9]+_amd64\.tar\.zst$' | sort -V | tail -n 1)" || true
  if [[ -z "$template" ]]; then
    ((apply)) && die "no Debian 13 template is downloaded; run: pveam update; pveam available --section system | grep debian-13; pveam download local <file>"
    template="local:vztmpl/debian-13-standard_<newest>_amd64.tar.zst"
  fi
fi
[[ "$template" == *"<newest>"* || "$template" =~ ^[a-z0-9_-]+:vztmpl/[A-Za-z0-9._-]+$ ]] ||
  die "MONITORING_TEMPLATE is not a template volume"
echo "    template: $template"

run() {
  if ((apply)); then
    "$@"
  else
    echo "would run: $*"
  fi
}

if ((apply)); then
  [[ "$(id -u)" == 0 ]] || die "run it as root on the Proxmox node"
  command -v pct >/dev/null || die "pct not found: run it on the Proxmox node"
  pct status "$id" >/dev/null 2>&1 && die "container $id exists already"
  qm status "$id" >/dev/null 2>&1 && die "the VM $id uses this ID; choose one that neither pct list nor qm list shows"
  pveam list "${template%%:*}" | grep "${template#*:}" >/dev/null ||
    die "$template is not downloaded (pveam update && pveam download ${template%%:*} ${template##*/})"
fi

echo "==> 1. container $id (nf-monitoring)"
run pct create "$id" "$template" --hostname nf-monitoring --unprivileged 1 \
  --features nesting=1,keyctl=1 --cores 2 --memory 4096 --swap 512 \
  --rootfs "$storage:32" --net0 "name=eth0,bridge=$bridge,ip=dhcp" --onboot 1
run pct start "$id"

echo "==> 2. Docker from its signed repository, git and Tailscale inside it"
# Docker Engine, containerd, runc and the compose plugin come from Docker's repository, with the
# key, the list and the packages of create-docker-vm.sh, so the LXC runs the runtime of the VM and
# gets its fixes the same way (ADR-0043). Debian's own Docker packages are never installed: Debian
# 13 has no fix of the runc escapes of November 2025.
inside='set -e
export DEBIAN_FRONTEND=noninteractive
apt-get update -q
apt-get install -y -q git curl ca-certificates gnupg
install -d -m 0755 /etc/apt/keyrings /usr/share/keyrings
curl -fsSL https://download.docker.com/linux/debian/gpg -o /etc/apt/keyrings/docker.asc
chmod a+r /etc/apt/keyrings/docker.asc
echo "deb [arch=amd64 signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/debian trixie stable" >/etc/apt/sources.list.d/docker.list
curl -fsSL https://pkgs.tailscale.com/stable/debian/trixie.noarmor.gpg -o /usr/share/keyrings/tailscale-archive-keyring.gpg
curl -fsSL https://pkgs.tailscale.com/stable/debian/trixie.tailscale-keyring.list -o /etc/apt/sources.list.d/tailscale.list
apt-get update -q
apt-get install -y -q docker-ce docker-ce-cli containerd.io docker-compose-plugin
apt-get install -y -q tailscale
systemctl enable --now docker tailscaled'
run pct exec "$id" -- bash -c "$inside"

echo "==> 3. the checkout, and Debian's security updates as on the VM"
run pct exec "$id" -- git clone https://github.com/NF-Hotel/NFHotel.git /opt/nfhotel-monitoring
run pct exec "$id" -- bash /opt/nfhotel-monitoring/ops/scripts/install-patching.sh --apply

cat <<EOF
==> done. The steps that need S03, inside the container (pct enter $id):
  1. tailscale up --ssh            (sign in; then give the machine the tag tag:monitoring)
  2. cd /opt/nfhotel-monitoring/ops/deploy/monitoring
     cp monitoring.env.example monitoring.env   (set MONITORING_BIND to: tailscale ip -4)
     mkdir -m 700 secrets   and write the three files of secrets.example/README.md (chmod 600)
  3. bash /opt/nfhotel-monitoring/ops/scripts/deploy-monitoring.sh           (checks only)
     bash /opt/nfhotel-monitoring/ops/scripts/deploy-monitoring.sh --apply   (starts it)
  4. bash /opt/nfhotel-monitoring/ops/scripts/pull-monitoring.sh --install   (deploys each merged change)
EOF
