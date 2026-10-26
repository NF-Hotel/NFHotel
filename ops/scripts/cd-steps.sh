#!/usr/bin/env bash
# The steps of the deploy-release action (.github/actions/deploy-release, ADR-0007,
# MIL-021 task 6), called by cd.yaml of NF-Hotel/NFHotel (ADR-0012 of NFHotel), one
# subcommand per step (QC-YML-001 criterion 7). It runs on the GitHub runner of the
# NFHotel job, never on the VM.
#
# Subcommands:
#   check-run      the run in RUN_ID must be a successful "NF Hotel Release" run of main
#                  in this repository; writes id=<run> to GITHUB_OUTPUT
#   read-manifest  read release.json, check every field against a strict pattern and
#                  write sha and one digest per image to GITHUB_OUTPUT; when the manifest
#                  names a migration script, migrations.sql must be beside it with that
#                  SHA-256, which is written as migrations
#   verify         check the build provenance and the SBOM attestation of every image
#                  digest with gh attestation verify, signed by the release workflow of
#                  this repository (MIL-022 task 3). A digest without both
#                  attestations stops the deploy before the tailnet is joined
#   gate           wait until every check of the release commit has finished, and stop
#                  unless all of them are green: success, skipped or neutral (ADR-0021).
#                  The checks are those GitHub records on the commit (the workflows of
#                  this repository and the apps, such as SonarCloud), apart from this
#                  job. One red, cancelled or timed-out check stops the deploy at once;
#                  a commit with no checks is refused; checks still running after the
#                  wait (GATE_TRIES x GATE_INTERVAL seconds, 45 minutes) fail the step
#   deploy         run the deploy command on the VM over Tailscale SSH; changes the VM,
#                  so it is only called after the gate and, for a manual run, only
#                  when APPLY is true. It first runs update with DEVOPS_SHA, so the
#                  commands of the release, taken only from a commit on main, do the
#                  rest (ADR-0018, MIL-032). With MIGRATIONS set it then sends migrations.sql
#                  to the migrate command (backup, then the script as nfhotel_migrator),
#                  and deploys only when that succeeded (MIL-022 task 6)
#   scope          check what this CI node reaches in the tailnet: port 22 of the VM must
#                  answer; its other ports are warnings, and the peers seen are listed (MIL-021
#                  criterion 7)
#   summary        append the audit record of the run to GITHUB_STEP_SUMMARY
#
# Environment (set by the workflow through env:, never interpolated):
#   GITHUB_REPOSITORY, GITHUB_OUTPUT, GITHUB_STEP_SUMMARY, GITHUB_SERVER_URL,
#   GITHUB_ACTOR, GITHUB_EVENT_NAME   set by the runner
#   GH_TOKEN     the job token, for check-run, verify and gate (attestations: read,
#                packages: read, checks: read)
#   GATE_TRIES GATE_INTERVAL   the polls of gate (default 90 x 30 seconds)
#   RUN_ID       the release run, for check-run and summary
#   VM_HOST      the tailnet name of the VM, for deploy
#   SHA DEVOPS_SHA WEB API ANALYSIS BENCHMARK   the checked manifest fields, for verify,
#                deploy and summary
#   MIGRATIONS   the checked SHA-256 of migrations.sql, for deploy (empty: none)
#   OUTCOME      the job status, for summary
#
# Usage: cd-steps.sh check-run|read-manifest|verify|gate|scope|deploy|summary
# Exit:  0 the step passed; 1 the step failed (with an ::error:: line); 2 usage error
# Needs: bash 4+, gh, jq, docker (for verify), tailscale (for deploy)

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=ops/scripts/lib/common.sh
. "$ROOT/scripts/lib/common.sh"
readonly RELEASE_WORKFLOW="NF Hotel Release"
readonly IMAGE_NAMES=(nfhotel-web nfhotel-api market-analysis benchmark)
readonly MANIFEST=release.json
readonly MIGRATIONS_FILE=migrations.sql
readonly FENCE='```'
readonly OWNER=NF-Hotel
readonly SBOM_PREDICATE=https://spdx.dev/Document/v2.3
# The check of this job, left out of the gate: it is running while the gate runs.
readonly DEPLOY_CHECK="Deploy to production"

fail() {
  echo "::error::$*"
  exit 1
}

check_run() {
  local run
  [[ "${RUN_ID:-}" =~ ^[0-9]+$ ]] || fail "the release run ID must be a number"
  run="$(gh api "repos/$GITHUB_REPOSITORY/actions/runs/$RUN_ID")" || fail "cannot read run $RUN_ID"
  jq -e --arg workflow "$RELEASE_WORKFLOW" --arg repo "$GITHUB_REPOSITORY" \
    '.name == $workflow and .head_branch == "main" and .conclusion == "success" and .repository.full_name == $repo' \
    <<<"$run" >/dev/null ||
    fail "run $RUN_ID is not a successful $RELEASE_WORKFLOW run of main in $GITHUB_REPOSITORY"
  echo "id=$RUN_ID" >>"$GITHUB_OUTPUT"
}

read_manifest() {
  local sha ops_sha name ref
  [[ -f "$MANIFEST" ]] || fail "$MANIFEST is missing"
  sha="$(jq -r '.sha' "$MANIFEST")"
  [[ "$sha" =~ ^[0-9a-f]{40}$ ]] || fail "bad commit in $MANIFEST"
  echo "sha=$sha" >>"$GITHUB_OUTPUT"
  ops_sha="$(jq -r '.devops_sha' "$MANIFEST")"
  [[ "$ops_sha" =~ ^[0-9a-f]{40}$ ]] || fail "bad DevOps commit in $MANIFEST"
  echo "devops_sha=$ops_sha" >>"$GITHUB_OUTPUT"
  local migrations
  migrations="$(jq -r '.migrations // ""' "$MANIFEST")"
  if [[ -n "$migrations" ]]; then
    [[ "$migrations" =~ ^[0-9a-f]{64}$ ]] || fail "bad migration script hash in $MANIFEST"
    [[ -f "$MIGRATIONS_FILE" ]] || fail "$MANIFEST names a migration script, but $MIGRATIONS_FILE is missing"
    [[ "$(sha256sum "$MIGRATIONS_FILE" | cut -d' ' -f1)" == "$migrations" ]] ||
      fail "$MIGRATIONS_FILE does not have the SHA-256 of $MANIFEST"
  fi
  echo "migrations=$migrations" >>"$GITHUB_OUTPUT"
  for name in "${IMAGE_NAMES[@]}"; do
    ref="$(jq -r --arg n "$name" '.images[$n]' "$MANIFEST")"
    [[ "$ref" =~ ^ghcr\.io/nf-hotel/${name}@sha256:[0-9a-f]{64}$ ]] || fail "bad digest for $name in $MANIFEST"
    echo "${name//-/_}=$ref" >>"$GITHUB_OUTPUT"
  done
}

verify() {
  local name var ref logged_in=""
  [[ -n "${GITHUB_REPOSITORY:-}" ]] || fail "GITHUB_REPOSITORY is not set"
  local signer="$GITHUB_REPOSITORY/.github/workflows/release.yaml"
  for name in "${IMAGE_NAMES[@]}"; do
    case "$name" in
      nfhotel-web) var=WEB ;;
      nfhotel-api) var=API ;;
      market-analysis) var=ANALYSIS ;;
      benchmark) var=BENCHMARK ;;
    esac
    ref="${!var:-}"
    [[ "$ref" =~ ^ghcr\.io/nf-hotel/${name}@sha256:[0-9a-f]{64}$ ]] || fail "no checked digest for $name"
    if [[ -z "${logged_in:-}" ]]; then
      # The packages are private: gh reads the digest through the Docker credentials.
      docker login ghcr.io --username "${GITHUB_ACTOR:-x}" --password-stdin <<<"${GH_TOKEN:?GH_TOKEN is not set}" >/dev/null ||
        fail "cannot log in to ghcr.io with the job token (packages: read)"
      logged_in=1
    fi
    echo "==> verifying $ref"
    gh attestation verify "oci://$ref" --owner "$OWNER" --signer-workflow "$signer" >/dev/null ||
      fail "$name has no valid build provenance from $signer: $ref"
    gh attestation verify "oci://$ref" --owner "$OWNER" --signer-workflow "$signer" \
      --predicate-type "$SBOM_PREDICATE" >/dev/null ||
      fail "$name has no valid SBOM attestation from $signer: $ref"
    echo "    build provenance and SBOM verified"
  done
}

deploy() {
  [[ "${VM_HOST:-}" =~ ^[a-z0-9.-]+$ ]] || fail "set the environment variable NFHOTEL_VM_HOST of production"
  local status=0
  [[ "${DEVOPS_SHA:-}" =~ ^[0-9a-f]{40}$ ]] || fail "bad operations commit"
  tailscale ssh "deploy@$VM_HOST" sudo -n /usr/local/sbin/nfhotel-deploy update "$DEVOPS_SHA" \
    2>&1 | tee update.log || status=$?
  ((status == 0)) || fail "the update of the commands on $VM_HOST failed (exit $status); nothing was migrated or deployed"
  # A new deploy command may install files that the old one did not know (such as the
  # certificate renewal); it runs once more, so they are in place in the same deploy.
  if grep -q "installed /usr/local/sbin/nfhotel-deploy from" update.log; then
    tailscale ssh "deploy@$VM_HOST" sudo -n /usr/local/sbin/nfhotel-deploy update "$DEVOPS_SHA" \
      2>&1 | tee -a update.log || status=$?
    ((status == 0)) || fail "the second update on $VM_HOST failed (exit $status); nothing was migrated or deployed"
  fi
  if [[ -n "${MIGRATIONS:-}" ]]; then
    [[ "$MIGRATIONS" =~ ^[0-9a-f]{64}$ ]] || fail "bad migration script hash"
    tailscale ssh "deploy@$VM_HOST" sudo -n /usr/local/sbin/nfhotel-deploy migrate "$MIGRATIONS" \
      <"$MIGRATIONS_FILE" 2>&1 | tee migrate.log || status=$?
    ((status == 0)) || fail "the migrations on $VM_HOST failed (exit $status); nothing was deployed"
  fi
  local drill=()
  [[ "${ROLLBACK_DRILL:-false}" == "true" ]] && drill=(--drill)
  tailscale ssh "deploy@$VM_HOST" \
    sudo -n /usr/local/sbin/nfhotel-deploy deploy "$SHA" "$DEVOPS_SHA" "$WEB" "$API" "$ANALYSIS" "$BENCHMARK" "${drill[@]}" \
    2>&1 | tee deploy.log || status=$?
  ((status == 0)) || fail "the deploy on $VM_HOST failed (exit $status); see the deploy log"
}

# gate: deploy only when everything on the release commit is green (ADR-0021).
gate() {
  [[ "${SHA:-}" =~ ^[0-9a-f]{40}$ ]] || fail "bad release commit"
  local tries="${GATE_TRIES:-90}" interval="${GATE_INTERVAL:-30}" n checks pending failed count
  [[ "$tries" =~ ^[0-9]+$ && "$interval" =~ ^[0-9]+$ ]] || fail "GATE_TRIES and GATE_INTERVAL must be numbers"
  for ((n = 1; n <= tries; n++)); do
    checks="$(gh api --paginate "repos/$GITHUB_REPOSITORY/commits/$SHA/check-runs?per_page=100" \
      --jq '.check_runs[] | [.name, .status, (.conclusion // "")] | @tsv')" || fail "cannot read the checks of $SHA"
    checks="$(awk -F'\t' -v self="$DEPLOY_CHECK" '$1 != self' <<<"$checks")"
    [[ -n "$checks" ]] || fail "the commit $SHA has no checks; nothing is deployed"
    count="$(wc -l <<<"$checks")"
    failed="$(awk -F'\t' '$2 == "completed" && $3 !~ /^(success|skipped|neutral)$/ { print "  " $1 " (" $3 ")" }' <<<"$checks")"
    if [[ -n "$failed" ]]; then
      printf 'Not deployed: these checks of %s are not green:\n%s\n' "$SHA" "$failed" | tee gate.log
      fail "not every check of $SHA is green; nothing is deployed"
    fi
    pending="$(awk -F'\t' '$2 != "completed" { print $1 }' <<<"$checks" | paste -sd, -)"
    if [[ -z "$pending" ]]; then
      echo "- All $count checks of \`$SHA\` are green." | tee gate.log
      return 0
    fi
    echo "waiting ($n/$tries) for: $pending"
    sleep "$interval"
  done
  fail "checks of $SHA still running after $((tries * interval)) seconds: $pending; nothing is deployed"
}

# reach <port>: whether a TCP connection to that port of the VM opens within 5 seconds.
reach() {
  timeout 5 bash -c "exec 3<>/dev/tcp/$VM_HOST/$1" 2>/dev/null
}

# scope: what this CI node reaches in the tailnet (MIL-021 criterion 7, ADR-0007
# decision 4). Port 22 of the VM must answer, and its other ports should not: the policy
# grants tag:ci only tcp:22 of tag:nfhotel-vm, and its tests deny the other nodes. An open
# port is a warning in the run and in the summary, not a failed deploy, so a change of the
# policy is seen without blocking a release. The peers seen are listed for the record.
scope() {
  [[ "${VM_HOST:-}" =~ ^[a-z0-9.-]+$ ]] || fail "set the environment variable NFHOTEL_VM_HOST of production"
  local peers others port open=()
  peers="$(tailscale status --json | jq -r '(.Peer // {}) | to_entries[] | .value.DNSName | sub("[.]$"; "")')" ||
    fail "cannot read the tailnet status"
  others="$(grep -v -e "^${VM_HOST}\$" -e "^${VM_HOST}[.]" <<<"$peers" | grep . || true)"
  # The ports are variables for the tests only.
  local ssh_port="${SCOPE_SSH_PORT:-22}" other_ports="${SCOPE_OTHER_PORTS:-80 443 5432 8081}"
  reach "$ssh_port" || fail "port $ssh_port of $VM_HOST does not answer"
  for port in $other_ports; do
    if reach "$port"; then open+=("$port"); fi
  done
  {
    echo "- Port $ssh_port of $VM_HOST answers."
    if ((${#open[@]})); then
      echo "- **Also open from tag:ci:** ${open[*]} (the policy should allow only $ssh_port)."
    else
      echo "- Ports $other_ports of $VM_HOST do not answer."
    fi
    if [[ -n "$others" ]]; then
      # A peer is seen when either side may reach the other. Members may reach every node,
      # this one too; what this node may reach is fixed by the policy and its tests.
      echo "- Peers seen (they may reach this node; it reaches only port 22 of the VM): $(tr '\n' ' ' <<<"$others")"
    else
      echo "- No peer other than $VM_HOST is seen."
    fi
  } | tee scope.log
  ((${#open[@]} == 0)) || echo "::warning::tag:ci reaches ports ${open[*]} of $VM_HOST; the policy should allow only 22"
}

summary() {
  {
    echo "## Deployment to production: ${OUTCOME:-unknown}"
    echo
    echo "- NFHotel commit: \`${SHA:-none}\`"
    echo "- DevOps commit: \`${DEVOPS_SHA:-none}\`"
    echo "- Release run: $GITHUB_SERVER_URL/$GITHUB_REPOSITORY/actions/runs/${RUN_ID:-none}"
    echo "- Started by: $GITHUB_ACTOR ($GITHUB_EVENT_NAME); the approver is in the environment history"
    if [[ -f "$MANIFEST" ]]; then
      printf '\n%sjson\n%s\n%s\n' "$FENCE" "$(cat "$MANIFEST")" "$FENCE"
    fi
    if [[ -f gate.log ]]; then
      printf '\n### Checks of the commit (ADR-0021)\n\n%s\n' "$(cat gate.log)"
    fi
    if [[ -f scope.log ]]; then
      printf '\n### Tailnet scope of this CI node\n\n%s\n' "$(cat scope.log)"
    fi
    if [[ -f deploy.log ]]; then
      printf '\n<details><summary>Deploy log</summary>\n\n%s\n%s\n%s\n</details>\n' "$FENCE" "$(tail -n 60 deploy.log)" "$FENCE"
    fi
  } >>"$GITHUB_STEP_SUMMARY"
}

case "${1:-}" in
  check-run) check_run ;;
  read-manifest) read_manifest ;;
  verify) verify ;;
  deploy) deploy ;;
  gate) gate ;;
  scope) scope ;;
  summary) summary ;;
  -h | --help) usage ;;
  *)
    usage >&2
    exit 2
    ;;
esac
