#!/usr/bin/env bash
# Check that no secret is in the environment of the PostgreSQL, API and web containers (MIL-035
# criterion 2, ADR-0024). It reads Config.Env with docker inspect and fails on a variable that
# is one of the secrets of scripts/write-secret-files.sh, or whose name says it holds one
# (password, token, secret, key), unless it names a file (_FILE) or is a setting of NOT_SECRET
# below. Only the names are printed, never the values. Run by scripts/deploy-release.sh after
# each deploy and by the end-to-end run.
#
# Usage: check-secret-env.sh [--name <prefix>]
#   --name   check only the containers whose name starts with prefix (the throwaway stack)
# Env:   NFHOTEL_DOCKER (default docker; for the tests)
# Exit:  0 no secret in the environment; 1 a secret was found, or a container of the three
#        services is missing; 2 usage error
# Needs: bash 4+, docker

set -euo pipefail

OPS="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
docker="${NFHOTEL_DOCKER:-docker}"
prefix=""

usage() {
  echo "usage: check-secret-env.sh [--name <prefix>]" >&2
  exit 2
}

while (($#)); do
  case "$1" in
    --name)
      (($# >= 2)) || usage
      prefix="$2"
      shift 2
      ;;
    *) usage ;;
  esac
done

# The compose service of each container, and the service of write-secret-files.sh.
declare -A SERVICES=([postgres]=postgres [nfhotel-api]=api [nfhotel-web]=web)
# Settings whose name sounds like a secret but whose value is not one.
readonly NOT_SECRET=" Staff__InitialManager__MustChangePassword "
found=0
for compose_service in postgres nfhotel-api nfhotel-web; do
  mapfile -t names < <("$docker" ps --filter "label=com.docker.compose.service=$compose_service" \
    --format '{{.Names}}' | grep "^$prefix" || true)
  if ((${#names[@]} == 0)); then
    echo "FAIL  no running container of $compose_service${prefix:+ named $prefix*}"
    found=1
    continue
  fi
  mapfile -t secrets < <(bash "$OPS/scripts/write-secret-files.sh" --list "${SERVICES[$compose_service]}")
  for container in "${names[@]}"; do
    bad=()
    while IFS= read -r variable; do
      name="${variable%%=*}"
      [[ -n "$name" ]] || continue
      if [[ " ${secrets[*]} " == *" $name "* ]]; then
        bad+=("$name")
      elif [[ "${name,,}" =~ (password|token|secret|key) && "$name" != *_FILE && "$NOT_SECRET" != *" $name "* ]]; then
        bad+=("$name")
      fi
    done < <("$docker" inspect --format '{{range .Config.Env}}{{println .}}{{end}}' "$container")
    if ((${#bad[@]})); then
      echo "FAIL  $container has secrets in its environment: ${bad[*]}"
      found=1
    else
      echo "PASS  $container has no secret in its environment"
    fi
  done
done
exit "$found"
