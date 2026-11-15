#!/usr/bin/env bash
# OpenTofu for the public edge of NF Hotel (MIL-078, ADR-0046): the routes of the tunnel nf-hotel
# and the Access application of the back office with its policy Team, as code in
# ops/infra/cloudflare/. It runs tofu from its official image, pinned by digest below, as the
# user who runs this script, without capabilities and with the code read-only. The image holds
# only the tofu binary, so the certificate authorities of this machine are lent to it. There is
# no backend and no state is kept: every plan imports the live objects through the import blocks
# of main.tf, and the state of a plan or an apply lives in the memory of its container only.
#
# Usage: edge-tofu.sh fmt|format|validate|lock|plan|plan-json|apply
#   fmt        tofu fmt -check, without a network (Ops CI): lists each file that is not in the
#              format of tofu (the image has no diff program, so no diff is shown)
#   format     tofu fmt, without a network: rewrites the files that fmt lists
#   validate   tofu init with the lock file read-only, so a provider that does not match its
#              checksums is refused, then tofu validate (Ops CI); no token, no account
#   lock       tofu init -upgrade, which rewrites .terraform.lock.hcl; after a change of the
#              provider version in versions.tf
#   plan       the plan against the live account, in full on the terminal: for a person on their
#              own machine, never in CI, because it shows the identifiers
#   plan-json  the same plan as the JSON stream of tofu on standard output, for
#              edge-comparison-steps.sh, which reduces it before anything is printed; the stream
#              holds the identifiers. The output of tofu init goes to standard error
#   apply      the plan, then the change after "yes" at the prompt of tofu: the only subcommand
#              that changes anything outside the repository. For S01 or S03 on their own machine,
#              in a terminal, with the token that writes
#   -h, --help this text
#
# Env:   CLOUDFLARE_API_TOKEN  the token, for plan, plan-json and apply; plan and apply ask for
#                              it without echo when it is not set and a terminal is attached
#        TF_VAR_*              the values of variables.tf; EDGE_ENV_FILE (default
#                              ops/infra/cloudflare/edge.env, ignored by git) adds them from a file
#        CA_BUNDLE             the certificate authorities for tofu (default: the first of
#                              /etc/ssl/certs/ca-certificates.crt and /etc/ssl/cert.pem)
#        NFHOTEL_DOCKER        the docker command (default docker), for the tests
# Exit:  0 done; 1 no certificate bundle, no token, or apply without a terminal; 2 usage error;
#        otherwise the exit status of tofu (1 an error, 3 fmt found a file to format)
# Needs: bash 4.4+, docker, and a network for init (the registry of OpenTofu and the releases
#        of the provider on GitHub) and for plan and apply (the API of Cloudflare)

set -euo pipefail

OPS="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=ops/scripts/lib/common.sh
. "$OPS/scripts/lib/common.sh"

readonly CODE="$OPS/infra/cloudflare"
# OpenTofu 1.13.9, the minimal image (the tofu binary on scratch): the digest of its index for
# every platform. A new version changes this line and, for a new minor, versions.tf.
readonly IMAGE="ghcr.io/opentofu/opentofu:1.13.9-minimal@sha256:84af0049634b7a7c455ab79c485818a8ef7f775cad3b2d42ce1c98ade720b5d1"
docker="${NFHOTEL_DOCKER:-docker}"
env_file="${EDGE_ENV_FILE:-$CODE/edge.env}"

# run_tofu <docker option>... -- <tofu argument>...: one tofu command in the pinned image, with
# the work folder of this run as /work, which holds the provider (TF_DATA_DIR).
run_tofu() {
  local opts=()
  while [[ "$1" != -- ]]; do
    opts+=("$1")
    shift
  done
  shift
  "$docker" run --rm --read-only --tmpfs /tmp --user "$(id -u):$(id -g)" \
    --cap-drop ALL --security-opt no-new-privileges \
    -e HOME=/tmp -e TF_DATA_DIR=/work/data -e TF_IN_AUTOMATION=1 \
    -v "$ca_bundle:/etc/ssl/certs/ca-certificates.crt:ro" -v "$work:/work" -w /src \
    "${opts[@]}" "$IMAGE" "$@"
}

# init_tofu [<tofu argument>...]: install the provider into the work folder, checked against the
# checksums of the lock file, which stays as it is.
init_tofu() {
  run_tofu -v "$CODE:/src:ro" -- init -backend=false -input=false -lockfile=readonly "$@"
}

# need_token <subcommand>: CLOUDFLARE_API_TOKEN must be set. plan and apply ask for it, without
# echo, when a terminal is attached; it never goes on a command line.
need_token() {
  if [[ -z "${CLOUDFLARE_API_TOKEN:-}" && "$1" != plan-json && -t 0 ]]; then
    read -rsp "Cloudflare token for $1 (not shown): " CLOUDFLARE_API_TOKEN
    echo >&2
  fi
  [[ -n "${CLOUDFLARE_API_TOKEN:-}" ]] ||
    die "CLOUDFLARE_API_TOKEN is not set; give the token through the environment, never as an argument"
  export CLOUDFLARE_API_TOKEN
}

# value_options: the docker options that hand the token and the values of variables.tf to tofu
# by name, one per line, so no value is on a command line.
value_options() {
  local name
  printf '%s\n' -e CLOUDFLARE_API_TOKEN
  [[ ! -f "$env_file" ]] || printf '%s\n' --env-file "$env_file"
  while IFS= read -r name; do
    [[ "$name" != TF_VAR_* ]] || printf '%s\n' -e "$name"
  done < <(compgen -e)
}

(($# == 1)) || {
  usage >&2
  exit 2
}
case "$1" in
  -h | --help)
    usage
    exit 0
    ;;
  fmt | format | validate | lock | plan | plan-json | apply) ;;
  *)
    usage >&2
    exit 2
    ;;
esac

ca_bundle="${CA_BUNDLE:-}"
if [[ -z "$ca_bundle" ]]; then
  for candidate in /etc/ssl/certs/ca-certificates.crt /etc/ssl/cert.pem; do
    if [[ -f "$candidate" ]]; then
      ca_bundle="$candidate"
      break
    fi
  done
fi
[[ -n "$ca_bundle" && -f "$ca_bundle" ]] ||
  die "no certificate bundle${ca_bundle:+ at $ca_bundle}; set CA_BUNDLE to the PEM file of the certificate authorities"

case "$1" in
  plan | plan-json | apply)
    need_token "$1"
    mapfile -t values < <(value_options)
    ;;
esac
[[ "$1" != apply || -t 0 ]] || die "apply needs a terminal, for the yes at the prompt of tofu"

work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

# The state of a plan or an apply is in /state, a file system in the memory of its container.
readonly STATE=(--tmpfs /state)
readonly STATE_FILE=-state=/state/terraform.tfstate

case "$1" in
  fmt)
    run_tofu --network none -v "$CODE:/src:ro" -- fmt -check -recursive
    ;;
  format)
    run_tofu --network none -v "$CODE:/src" -- fmt -recursive
    ;;
  validate)
    init_tofu
    run_tofu -v "$CODE:/src:ro" -- validate
    ;;
  lock)
    run_tofu -v "$CODE:/src" -- init -backend=false -input=false -upgrade
    ;;
  plan)
    init_tofu
    run_tofu "${STATE[@]}" -v "$CODE:/src:ro" "${values[@]}" -- plan -input=false "$STATE_FILE"
    ;;
  plan-json)
    init_tofu -no-color >&2
    run_tofu "${STATE[@]}" -v "$CODE:/src:ro" "${values[@]}" -- plan -input=false "$STATE_FILE" -json
    ;;
  apply)
    init_tofu
    run_tofu -i -t "${STATE[@]}" -v "$CODE:/src:ro" "${values[@]}" -- apply "$STATE_FILE"
    ;;
esac
