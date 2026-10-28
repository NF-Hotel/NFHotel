#!/usr/bin/env bash
# Create a per-run Proxmox API token inside the throwaway container and print
# shell `export` lines for it, so the token lives only in the environment of
# the caller: it is never written to a file and never put on a command line.
#
# Usage: eval "$(pve-token.sh [--container <name>])"
#   --container  name of the Proxmox container (default: nfhotel-pve-proxmox-1)
# Output: export PVE_API_TOKEN_ID, PVE_API_TOKEN_SECRET and PVE_NODE
# Exit:  0 token created; 1 setup failed; 2 usage error
# Needs: bash 4+, docker, sed
# Task:  MIL-003 task 3 (docs/milestones/mil-003-throwaway-proxmox.md)
# Changes state: yes, but only inside the named throwaway container.

set -euo pipefail

# Git Bash on Windows rewrites arguments that look like paths (/nodes); stop it.
export MSYS_NO_PATHCONV=1

die() {
  echo "error: $*" >&2
  exit 1
}

# json_field <name> <json>: the first string value of "name" (flat JSON only).
json_field() {
  local name="$1" json="$2"
  printf '%s' "$json" |
    sed -n "s/.*\"$name\" *: *\"\\([^\"]*\\)\".*/\\1/p" | head -n 1
}

main() {
  local container="nfhotel-pve-proxmox-1" token_name created token_id secret node

  while [[ $# -gt 0 ]]; do
    case "$1" in
      --container)
        container="${2:?usage: $0 [--container <name>]}"
        shift 2
        ;;
      *)
        echo "usage: $0 [--container <name>]" >&2
        exit 2
        ;;
    esac
  done

  token_name="nfh$(date +%s)"
  created="$(docker exec "$container" pveum user token add root@pam "$token_name" \
    --privsep 0 --output-format json)" ||
    die "could not create a token in container $container"

  token_id="$(json_field full-tokenid "$created")"
  secret="$(json_field value "$created")"
  [[ -n "$token_id" && -n "$secret" ]] ||
    die "the token reply from Proxmox could not be read"

  node="$(json_field node "$(docker exec "$container" pvesh get /nodes \
    --output-format json)")"
  [[ -n "$node" ]] || die "could not read the node name from Proxmox"

  printf "export PVE_API_TOKEN_ID='%s'\n" "$token_id"
  printf "export PVE_API_TOKEN_SECRET='%s'\n" "$secret"
  printf "export PVE_NODE='%s'\n" "$node"
}

main "$@"
