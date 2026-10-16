#!/usr/bin/env bash
# Create a Debian virtual machine with Docker and the compose plugin on a
# Proxmox VE node (ADR-0003). The VM is built from the official Debian cloud
# image and configured by cloud-init; nothing is installed by hand.
#
# Usage: create-docker-vm.sh (--container <name> | --ssh <user@host>) [options]
#   --container <name>   run the Proxmox commands inside this Docker container
#                        (the throwaway Proxmox of MIL-003)
#   --ssh <user@host>    run the Proxmox commands on the node over ssh
#   --name <name>        VM name (default: nfhotel-services)
#   --vmid <id>          VM id (default: the next free id of the cluster)
#   --cores <n>          vCPUs (default: 4)
#   --memory <MiB>       memory (default: 12288)
#   --disk <GiB>         disk size (default: 60)
#   --storage <name>     storage for the disk (default: local-lvm)
#   --snippets <name>    storage that allows snippets (default: local)
#   --bridge <name>      network bridge (default: vmbr0)
#   --ssh-key-file <f>   public key for the VM user (default: ~/.ssh/id_ed25519.pub)
#   --start              start the VM when it is created
#   --dry-run            print the commands and run nothing
# Output: the VM id on stdout when the VM was created.
# Exit:   0 created (or dry run); 1 a step failed; 2 usage error
# Needs:  bash 4+, and docker or ssh on this machine; qm and pvesh on the node
# Task:   MIL-004 task 3 (docs/milestones/mil-004-market-analysis-service.md)
# Changes state: yes, on the Proxmox node it is pointed at. It never changes or
# removes an existing VM: a VM id that already exists is an error.

set -euo pipefail

# Git Bash on Windows rewrites arguments that look like paths; stop it.
export MSYS_NO_PATHCONV=1

readonly IMAGE_URL="https://cloud.debian.org/images/cloud/bookworm/latest/debian-12-genericcloud-amd64.qcow2"
readonly IMAGE_FILE="/var/lib/vz/template/iso/debian-12-genericcloud-amd64.qcow2"
readonly SNIPPET_DIR="/var/lib/vz/snippets"

container=""
ssh_target=""
vm_name="nfhotel-services"
vmid=""
cores=4
memory=12288
disk_gib=60
storage="local-lvm"
snippets_storage="local"
bridge="vmbr0"
ssh_key_file="${HOME}/.ssh/id_ed25519.pub"
start_vm=false
dry_run=false
created_vmid=""

die() {
  echo "error: $*" >&2
  exit 1
}

usage() {
  sed -n '2,/^$/p' "$0" | sed 's/^# \{0,1\}//' >&2
  exit 2
}

# cleanup: when a step failed after the VM was created by this script, remove that
# half-built VM again. It only ever removes the VM id this run created.
cleanup() {
  local status=$?
  if [[ $status -ne 0 && -n "$created_vmid" && "$dry_run" == false ]]; then
    echo "removing the half-built VM $created_vmid" >&2
    node qm destroy "$created_vmid" --purge >/dev/null 2>&1 || true
  fi
  exit "$status"
}

# node <command...>: run a command on the Proxmox node.
node() {
  if [[ "$dry_run" == true ]]; then
    printf '+ %s\n' "$*" >&2
    return 0
  fi
  if [[ -n "$container" ]]; then
    docker exec -i "$container" "$@"
  else
    # The command is meant to run on the node, so expanding it on this side is correct.
    # shellcheck disable=SC2029
    ssh "$ssh_target" "$@"
  fi
}

# node_out <command...>: like node, but the output is returned even in a dry run.
node_out() {
  if [[ "$dry_run" == true ]]; then
    printf '+ %s\n' "$*" >&2
    echo "9000"
    return 0
  fi
  node "$@"
}

parse_args() {
  while [[ $# -gt 0 ]]; do
    case "$1" in
      --container) container="${2:?missing value for $1}"; shift 2 ;;
      --ssh) ssh_target="${2:?missing value for $1}"; shift 2 ;;
      --name) vm_name="${2:?missing value for $1}"; shift 2 ;;
      --vmid) vmid="${2:?missing value for $1}"; shift 2 ;;
      --cores) cores="${2:?missing value for $1}"; shift 2 ;;
      --memory) memory="${2:?missing value for $1}"; shift 2 ;;
      --disk) disk_gib="${2:?missing value for $1}"; shift 2 ;;
      --storage) storage="${2:?missing value for $1}"; shift 2 ;;
      --snippets) snippets_storage="${2:?missing value for $1}"; shift 2 ;;
      --bridge) bridge="${2:?missing value for $1}"; shift 2 ;;
      --ssh-key-file) ssh_key_file="${2:?missing value for $1}"; shift 2 ;;
      --start) start_vm=true; shift ;;
      --dry-run) dry_run=true; shift ;;
      -h | --help) usage ;;
      *) echo "unknown option: $1" >&2; usage ;;
    esac
  done

  if [[ -z "$container" && -z "$ssh_target" ]]; then
    echo "give --container or --ssh" >&2
    usage
  fi
  if [[ -n "$container" && -n "$ssh_target" ]]; then
    echo "give only one of --container and --ssh" >&2
    usage
  fi
  [[ -r "$ssh_key_file" ]] || die "cannot read the public key file $ssh_key_file"
}

# user_data <public key>: the cloud-init user data that installs Docker.
user_data() {
  local public_key="$1"
  cat <<EOF
#cloud-config
hostname: ${vm_name}
users:
  - name: nfhotel
    groups: [sudo, docker]
    shell: /bin/bash
    sudo: "ALL=(ALL) NOPASSWD:ALL"
    lock_passwd: true
    ssh_authorized_keys:
      - ${public_key}
package_update: true
packages:
  - ca-certificates
  - curl
  - gnupg
  - qemu-guest-agent
  - git
runcmd:
  - install -m 0755 -d /etc/apt/keyrings
  - curl -fsSL https://download.docker.com/linux/debian/gpg -o /etc/apt/keyrings/docker.asc
  - chmod a+r /etc/apt/keyrings/docker.asc
  - echo "deb [arch=amd64 signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/debian bookworm stable" > /etc/apt/sources.list.d/docker.list
  - apt-get update
  - apt-get install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin
  - systemctl enable --now docker qemu-guest-agent
  - docker network create nfhotel-proxy
  - docker network create --internal nfhotel-db
EOF
}

main() {
  parse_args "$@"

  local public_key snippet volume
  trap cleanup EXIT
  public_key="$(tr -d '\n' <"$ssh_key_file")"

  if [[ -z "$vmid" ]]; then
    vmid="$(node_out pvesh get /cluster/nextid)" || die "could not read the next free VM id"
  fi
  [[ "$vmid" =~ ^[0-9]+$ ]] || die "the VM id '$vmid' is not a number"

  if [[ "$dry_run" == false ]] && node qm status "$vmid" >/dev/null 2>&1; then
    die "VM $vmid already exists; nothing was changed"
  fi

  snippet="nfhotel-${vmid}-user-data.yaml"

  echo "creating VM $vmid ($vm_name): ${cores} vCPU, ${memory} MiB, ${disk_gib} GiB on ${storage}" >&2

  node mkdir -p "$SNIPPET_DIR" "$(dirname "$IMAGE_FILE")"
  if [[ "$dry_run" == true ]]; then
    echo "+ write ${SNIPPET_DIR}/${snippet}" >&2
  else
    user_data "$public_key" | node tee "${SNIPPET_DIR}/${snippet}" >/dev/null ||
      die "could not write the cloud-init snippet"
  fi

  node sh -c "test -s '$IMAGE_FILE' || curl -fsSL -o '$IMAGE_FILE' '$IMAGE_URL'" ||
    die "could not download the Debian cloud image"

  created_vmid="$vmid"
  node qm create "$vmid" --name "$vm_name" --memory "$memory" --cores "$cores" \
    --cpu host --ostype l26 --agent enabled=1 \
    --net0 "virtio,bridge=${bridge}" --scsihw virtio-scsi-pci \
    --serial0 socket --vga serial0 ||
    die "qm create failed"
  node qm importdisk "$vmid" "$IMAGE_FILE" "$storage" ||
    die "importing the disk failed"
  # The volume name depends on the storage type (LVM: local-lvm:vm-100-disk-0,
  # directory: local:100/vm-100-disk-0.raw), so read it back from the VM.
  if [[ "$dry_run" == true ]]; then
    volume="${storage}:vm-${vmid}-disk-0"
  else
    volume="$(node sh -c "qm config '$vmid' | sed -n 's/^unused0: //p'")"
    [[ -n "$volume" ]] || die "the imported disk was not found on VM $vmid"
  fi
  node qm set "$vmid" --scsi0 "$volume" --boot order=scsi0 \
    --ide2 "${storage}:cloudinit" \
    --cicustom "user=${snippets_storage}:snippets/${snippet}" \
    --ipconfig0 ip=dhcp ||
    die "qm set failed"
  node qm resize "$vmid" scsi0 "${disk_gib}G" || die "resizing the disk failed"

  if [[ "$start_vm" == true ]]; then
    node qm start "$vmid" || die "starting the VM failed"
  fi

  echo "$vmid"
}

main "$@"
