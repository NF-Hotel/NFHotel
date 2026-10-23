#!/usr/bin/env bash
# Tests of scripts/cd-steps.sh and scripts/action-ref.sh (MIL-021 task 6, ADR-0012 of
# NFHotel): the release run check, the manifest check, the check of the attestations
# (MIL-022 task 3), the deploy call, the summary, and
# the refusal of an action that is not pinned by a full commit SHA. Fakes stand in for gh
# and tailscale.
#
# Run it in Debian 12 with the repository at /src:
#   docker run --rm -v "$PWD:/src:ro" debian:12-slim bash /src/tests/test-cd-steps.sh
# Exit:  0 every check passed; 1 a check failed
# Needs: Debian 12 (apt-get, jq, git, python3)

# shellcheck disable=SC2015 # ok and ko always return 0, so `test && ok || ko` runs
# exactly one of them; the short form keeps each check on one line.
set -uo pipefail

apt-get -qq update >/dev/null && apt-get -qq install -y jq git python3 >/dev/null 2>&1
W="$(mktemp -d)"
trap 'rm -rf "$W"' EXIT
cd "$W" || exit 1
mkdir bin
pass=0
fail=0
ok() {
  echo "PASS  $1"
  pass=$((pass + 1))
}
ko() {
  echo "FAIL  $1"
  fail=$((fail + 1))
}

cat >bin/gh <<'EOF'
#!/bin/bash
# gh attestation verify oci://<ref> ...: digest b...b has no attestation at all, e...e has
# a build provenance but no SBOM; everything else verifies.
if [[ "$1" == attestation ]]; then
  [[ "$3" == *sha256:bbbb* ]] && exit 1
  [[ "$3" == *sha256:eeee* && "$*" == *--predicate-type* ]] && exit 1
  echo "$*" >>"${FAKE_GH_LOG:-/dev/null}"
  exit 0
fi
# gh api --paginate .../check-runs: the checks of the commit as name<TAB>status<TAB>conclusion,
# one file per poll (FAKE_CHECKS_DIR/1, 2, ...); the last file repeats.
if [[ "$*" == *"/check-runs"* ]]; then
  n=$(($(cat "$FAKE_CHECKS_DIR/count" 2>/dev/null || echo 0) + 1))
  echo "$n" >"$FAKE_CHECKS_DIR/count"
  f="$FAKE_CHECKS_DIR/$n"
  [[ -f "$f" ]] || f="$(ls "$FAKE_CHECKS_DIR"/[0-9]* | sort -V | tail -1)"
  cat "$f"
  exit 0
fi
case "${2##*/}" in
  1) echo '{"name":"NF Hotel Release","head_branch":"main","conclusion":"success","repository":{"full_name":"NF-Hotel/NFHotel"}}' ;;
  2) echo '{"name":"NF Hotel Release","head_branch":"feature","conclusion":"success","repository":{"full_name":"NF-Hotel/NFHotel"}}' ;;
  3) echo '{"name":"NF Hotel CI","head_branch":"main","conclusion":"success","repository":{"full_name":"NF-Hotel/NFHotel"}}' ;;
  4) echo '{"name":"NF Hotel Release","head_branch":"main","conclusion":"success","repository":{"full_name":"evil/NFHotel"}}' ;;
  *) exit 1 ;;
esac
EOF
# shellcheck disable=SC2016 # the fake script expands these when it runs, not here
cat >bin/tailscale <<'EOF'
#!/bin/bash
[[ "$1" == status ]] && { echo "$FAKE_STATUS"; exit 0; }
echo "tailscale $*"
[[ "$*" == *" update "* ]] && { [[ -n "${FAKE_UPDATE_OUT:-}" ]] && echo "$FAKE_UPDATE_OUT"; exit "${FAKE_UPDATE_EXIT:-0}"; }
[[ "$*" == *" migrate "* ]] && { cat >"${FAKE_STDIN:-/dev/null}"; exit "${FAKE_MIGRATE_EXIT:-0}"; }
exit "${FAKE_EXIT:-0}"
EOF
printf '#!/bin/bash\ncat >/dev/null; exit 0\n' >bin/docker
chmod +x bin/*
export PATH="$W/bin:$PATH" GITHUB_REPOSITORY=NF-Hotel/NFHotel GITHUB_SERVER_URL=https://github.com \
  GITHUB_ACTOR=tester GITHUB_EVENT_NAME=workflow_run
step() {
  : >out
  GITHUB_OUTPUT="$W/out" bash /src/scripts/cd-steps.sh "$@" >log 2>&1
}
expect() {
  local want="$1" desc="$2"
  shift 2
  "$@"
  local got=$?
  if [[ "$got" == "$want" ]]; then ok "$desc"; else ko "$desc (exit $got: $(grep -m1 '::error::' log))"; fi
}

RUN_ID=1 expect 0 "check-run accepts a successful Release run of main" step check-run
grep -qx "id=1" out && ok "check-run writes the run ID" || ko "check-run output"
RUN_ID=2 expect 1 "check-run refuses a run of another branch" step check-run
RUN_ID=3 expect 1 "check-run refuses another workflow" step check-run
RUN_ID=4 expect 1 "check-run refuses a run of another repository" step check-run
RUN_ID=x9 expect 1 "check-run refuses a run ID that is not a number" step check-run

D="$(printf 'a%.0s' $(seq 64))"
APP="$(printf 'c%.0s' $(seq 40))"
OPS="$(printf 'd%.0s' $(seq 40))"
manifest() {
  printf '{"sha":"%s","devops_sha":"%s","images":{"nfhotel-web":"%s","nfhotel-api":"ghcr.io/nf-hotel/nfhotel-api@sha256:%s","market-analysis":"ghcr.io/nf-hotel/market-analysis@sha256:%s","benchmark":"ghcr.io/nf-hotel/benchmark@sha256:%s"}}' \
    "$1" "$2" "$3" "$D" "$D" "$D" >release.json
}
manifest "$APP" "$OPS" "ghcr.io/nf-hotel/nfhotel-web@sha256:$D"
expect 0 "read-manifest accepts a valid manifest" step read-manifest
grep -qx "devops_sha=$OPS" out && grep -qx "sha=$APP" out && ok "read-manifest writes both commits" || ko "manifest outputs"
manifest "$APP" "$OPS" "ghcr.io/nf-hotel/nfhotel-web@sha256:$D\$(id)"
expect 1 "read-manifest refuses shell text in a digest" step read-manifest
manifest "$APP" main "ghcr.io/nf-hotel/nfhotel-web@sha256:$D"
expect 1 "read-manifest refuses a DevOps ref that is not a full SHA" step read-manifest
manifest main "$OPS" "ghcr.io/nf-hotel/nfhotel-web@sha256:$D"
expect 1 "read-manifest refuses an NFHotel ref that is not a full SHA" step read-manifest

VM_HOST="" expect 1 "deploy refuses an empty host" step deploy
VM_HOST="x;id" expect 1 "deploy refuses a host with shell text" step deploy
VM_HOST=nf-hotel SHA="$APP" DEVOPS_SHA="$OPS" WEB=w API=a ANALYSIS=n BENCHMARK=b expect 0 "deploy calls the deploy command" step deploy
grep -q "tailscale ssh deploy@nf-hotel sudo -n /usr/local/sbin/nfhotel-deploy deploy $APP $OPS w a n b" log &&
  ok "deploy passes both commits and the four digests in order" || ko "deploy command line: $(head -1 log)"
FAKE_EXIT=1 VM_HOST=nf-hotel SHA="$APP" DEVOPS_SHA="$OPS" WEB=w API=a ANALYSIS=n BENCHMARK=b expect 1 "a failed remote deploy fails the step" step deploy

GITHUB_STEP_SUMMARY="$W/sum" RUN_ID=1 SHA="$APP" DEVOPS_SHA="$OPS" OUTCOME=success bash /src/scripts/cd-steps.sh summary
grep -q "NFHotel commit: \`$APP\`" sum && grep -q "DevOps commit: \`$OPS\`" sum && ok "the summary records both commits" || ko "summary"

# action-ref (ADR-0016): the actions are local, so the commit is the checkout that holds
# the script; a ref, when there is one, must be a full SHA, and a given devops-sha must match.
git init -q "$W/checkout" && mkdir -p "$W/checkout/scripts"
cp /src/scripts/action-ref.sh "$W/checkout/scripts/"
git -C "$W/checkout" -c user.email=t@t -c user.name=t add -A >/dev/null
git -C "$W/checkout" -c user.email=t@t -c user.name=t commit -qm checkout
HEAD_SHA="$(git -C "$W/checkout" rev-parse HEAD)"
mkdir -p "$W/nogit" && cp /src/scripts/action-ref.sh "$W/nogit/"
ref() {
  : >out
  GITHUB_OUTPUT="$W/out" bash "${SCRIPT:-$W/checkout/scripts/action-ref.sh}" >log 2>&1
}
ACTION_REF="" expect 0 "action-ref resolves a local action to the commit of its checkout" ref
grep -qx "devops_sha=$HEAD_SHA" out && ok "action-ref writes the commit of the checkout" || ko "action-ref output: $(cat out)"
ACTION_REF="" DEVOPS_SHA_INPUT="$HEAD_SHA" expect 0 "action-ref accepts a devops-sha that is the checkout" ref
ACTION_REF="" DEVOPS_SHA_INPUT="$OPS" expect 1 "action-ref refuses a devops-sha that differs from the checkout" ref
ACTION_REF="" SCRIPT="$W/nogit/action-ref.sh" expect 1 "action-ref refuses an action outside a git checkout" ref
ACTION_REF="$OPS" expect 0 "action-ref accepts a full SHA ref" ref
grep -qx "devops_sha=$OPS" out && ok "action-ref writes the SHA of the ref" || ko "action-ref ref output"
ACTION_REF=main expect 1 "action-ref refuses a branch" ref
ACTION_REF=v1 expect 1 "action-ref refuses a tag" ref

# verify (MIL-022 task 3): both attestations per built image, the frozen one by its pin.
FROZEN="$(tr -d '[:space:]' </src/deploy/market-analysis/frozen-image)"
B="$(printf 'b%.0s' $(seq 64))"
E="$(printf 'e%.0s' $(seq 64))"
verify_with() {
  WEB="ghcr.io/nf-hotel/nfhotel-web@sha256:$1" API="ghcr.io/nf-hotel/nfhotel-api@sha256:$D" \
    ANALYSIS="$2" BENCHMARK="ghcr.io/nf-hotel/benchmark@sha256:$D" GH_TOKEN=t FAKE_GH_LOG="$W/gh.log" step verify
}
: >gh.log
expect 0 "verify accepts attested digests and the frozen pin" verify_with "$D" "$FROZEN"
[[ "$(grep -c -- '--signer-workflow NF-Hotel/NFHotel/.github/workflows/release.yaml' gh.log)" == 6 ]] &&
  ok "verify checks provenance and SBOM of the three built images, signed by the release workflow" ||
  ko "verify calls ($(wc -l <gh.log))"
grep -q "market-analysis" gh.log && ko "verify does not query the frozen image" || ok "verify does not query the frozen image"
expect 1 "verify refuses a digest without any attestation" verify_with "$B" "$FROZEN"
expect 1 "verify refuses a digest with provenance but no SBOM" verify_with "$E" "$FROZEN"
expect 1 "verify refuses a market-analysis digest other than the frozen pin" \
  verify_with "$D" "ghcr.io/nf-hotel/market-analysis@sha256:$D"
expect 1 "verify refuses a missing digest" verify_with "" "$FROZEN"

# Migrations by CD (MIL-022 task 6): the manifest names the script by its SHA-256, the
# file must match it, and deploy runs migrate first and deploys only when it succeeded.
printf 'SELECT 1;\n' >migrations.sql
M="$(sha256sum migrations.sql | cut -d' ' -f1)"
manifest_m() {
  printf '{"sha":"%s","devops_sha":"%s","migrations":"%s","images":{"nfhotel-web":"ghcr.io/nf-hotel/nfhotel-web@sha256:%s","nfhotel-api":"ghcr.io/nf-hotel/nfhotel-api@sha256:%s","market-analysis":"ghcr.io/nf-hotel/market-analysis@sha256:%s","benchmark":"ghcr.io/nf-hotel/benchmark@sha256:%s"}}' \
    "$APP" "$OPS" "$1" "$D" "$D" "$D" "$D" >release.json
}
manifest_m "$M"
expect 0 "read-manifest accepts a migration script with its hash" step read-manifest
grep -qx "migrations=$M" out && ok "read-manifest writes the hash of the migration script" || ko "migrations output"
manifest_m "$(printf 'f%.0s' $(seq 64))"
expect 1 "read-manifest refuses a migration script with another hash" step read-manifest
manifest_m "x;id"
expect 1 "read-manifest refuses a migration hash that is not 64 hex" step read-manifest
manifest_m "$M"
mv migrations.sql m.keep
expect 1 "read-manifest refuses a manifest whose migration script is missing" step read-manifest
mv m.keep migrations.sql
manifest "$APP" "$OPS" "ghcr.io/nf-hotel/nfhotel-web@sha256:$D"
expect 0 "read-manifest accepts a manifest without migrations" step read-manifest
grep -qx "migrations=" out && ok "no migrations gives an empty hash" || ko "empty migrations output"
deploy_m() {
  VM_HOST=nf-hotel SHA="$APP" DEVOPS_SHA="$OPS" WEB=w API=a ANALYSIS=n BENCHMARK=b MIGRATIONS="$M" \
    FAKE_STDIN="$W/sent.sql" step deploy
}
expect 0 "deploy with migrations runs migrate, then deploy" deploy_m
[[ "$(grep -c '^tailscale ssh' log)" == 3 ]] && head -1 log | grep -q "nfhotel-deploy update $OPS" &&
  sed -n 2p log | grep -q "nfhotel-deploy migrate $M" && sed -n 3p log | grep -q "nfhotel-deploy deploy $APP" &&
  ok "update, then migrate with the hash, then deploy" || ko "order: $(tr '\n' '|' <log)"
cmp -s migrations.sql "$W/sent.sql" && ok "migrate receives the script on standard input" || ko "script sent"
FAKE_MIGRATE_EXIT=1 expect 1 "a failed migrate fails the step" deploy_m
grep -q "nfhotel-deploy deploy" log && ko "nothing is deployed after a failed migrate" || ok "nothing is deployed after a failed migrate"
# The update of the commands (ADR-0018) comes first; when it fails, nothing else runs.
FAKE_UPDATE_EXIT=2 expect 1 "a failed update fails the step" deploy_m
grep -qE "nfhotel-deploy (migrate|deploy)" log && ko "nothing runs after a failed update" || ok "nothing runs after a failed update"
VM_HOST=nf-hotel SHA="$APP" DEVOPS_SHA="x;id" WEB=w API=a ANALYSIS=n BENCHMARK=b expect 1 "deploy refuses a bad operations commit" step deploy
# When update installed a new deploy command, that command runs update once more, so the
# files it knows and the old one did not are installed in the same deploy.
FAKE_UPDATE_OUT="    installed /usr/local/sbin/nfhotel-deploy from ops/scripts/deploy-release.sh" expect 0 "a new deploy command runs update again" deploy_m
[[ "$(grep -c 'nfhotel-deploy update' log)" == 2 ]] && ok "update runs twice after a new deploy command" || ko "second update: $(tr '\n' '|' <log)"
expect 0 "an unchanged deploy command runs update once" deploy_m
[[ "$(grep -c 'nfhotel-deploy update' log)" == 1 ]] && ok "update runs once when the deploy command is unchanged" || ko "single update"

# The rollback drill (MIL-021 criterion 6) passes --drill to the deploy command, only
# when ROLLBACK_DRILL is true.
VM_HOST=nf-hotel SHA="$APP" DEVOPS_SHA="$OPS" WEB=w API=a ANALYSIS=n BENCHMARK=b ROLLBACK_DRILL=true expect 0 "a drill run calls deploy" step deploy
grep -q "nfhotel-deploy deploy $APP $OPS w a n b --drill" log && ok "a drill passes --drill" || ko "drill: $(tr '\n' '|' <log)"
VM_HOST=nf-hotel SHA="$APP" DEVOPS_SHA="$OPS" WEB=w API=a ANALYSIS=n BENCHMARK=b ROLLBACK_DRILL=false expect 0 "a normal run calls deploy" step deploy
grep -q -- "--drill" log && ko "a normal run passed --drill" || ok "a normal run passes no --drill"

# The tailnet scope (MIL-021 criterion 7): a listener on 127.0.0.1 stands in for ssh of
# the VM, and the status of tailscale names the peers.
python3 -m http.server 18022 --bind 127.0.0.1 >/dev/null 2>&1 &
python3 -m http.server 18080 --bind 127.0.0.1 >/dev/null 2>&1 &
for _ in $(seq 50); do
  (echo >/dev/tcp/127.0.0.1/18080) 2>/dev/null && break
  sleep 0.1
done
scope() { VM_HOST=127.0.0.1 SCOPE_SSH_PORT=18022 SCOPE_OTHER_PORTS="$1" step scope; }
export FAKE_STATUS='{"Peer":{"k1":{"DNSName":"127.0.0.1."}}}'
expect 0 "scope passes when only ssh answers" scope 18081
grep -q "Ports 18081 of 127.0.0.1 do not answer" log && grep -q "No peer other than" log && ! grep -q "::warning::" log &&
  ok "the scope records the closed ports and no other peer" || ko "scope: $(tr '\n' '|' <log)"
expect 0 "an open port is a warning, not a failure" scope 18080
grep -q "::warning::tag:ci reaches ports 18080" log && ok "an open port is reported" || ko "open port: $(tr '\n' '|' <log)"
FAKE_STATUS='{"Peer":{"k1":{"DNSName":"127.0.0.1."},"k2":{"DNSName":"legion.example.ts.net."}}}' expect 0 "another peer seen does not fail the step" scope 18081
grep -q "Peers seen .*legion.example.ts.net" log && ! grep -q "::warning::" log && ok "another peer is listed, without a warning" || ko "peer: $(tr '\n' '|' <log)"
VM_HOST=127.0.0.1 SCOPE_SSH_PORT=18099 expect 1 "scope fails when ssh does not answer" step scope
kill %1 %2 2>/dev/null

# The gate (ADR-0021): every check of the release commit must be green, this job left out.
G="$W/checks"
polls() {
  rm -rf "$G" && mkdir -p "$G"
  local i=0
  for content in "$@"; do
    i=$((i + 1))
    printf '%b' "$content" >"$G/$i"
  done
}
gate() { FAKE_CHECKS_DIR="$G" GATE_INTERVAL=0 GATE_TRIES="${TRIES:-5}" SHA="$APP" step gate; }
T='\t'
polls "NF Hotel CI${T}completed${T}success\nOps linters${T}completed${T}success\nDAST full scan${T}completed${T}skipped\nDeploy to production${T}in_progress${T}\n"
expect 0 "the gate passes when every check is green or skipped" gate
grep -q "All 3 checks" log && ok "the gate counts the checks without this job" || ko "gate count: $(tr '\n' '|' <log)"
polls "NF Hotel CI${T}completed${T}success\nEnd-to-end tests${T}in_progress${T}\n" \
  "NF Hotel CI${T}completed${T}success\nEnd-to-end tests${T}completed${T}success\n"
expect 0 "the gate waits for a running check, then passes" gate
grep -q "waiting (1/5) for: End-to-end tests" log && ok "the gate names the check it waits for" || ko "gate wait: $(tr '\n' '|' <log)"
polls "NF Hotel CI${T}completed${T}success\nOps script tests (test-cd-steps.sh)${T}completed${T}failure\nDAST baseline${T}in_progress${T}\n"
expect 1 "one red check stops the deploy at once" gate
grep -q "Ops script tests (test-cd-steps.sh) (failure)" log && ok "the red check is named" || ko "gate red: $(tr '\n' '|' <log)"
polls "NF Hotel CI${T}completed${T}cancelled\n"
expect 1 "a cancelled check stops the deploy" gate
polls "SonarCloud Code Analysis${T}completed${T}timed_out\n"
expect 1 "a timed-out check of an app stops the deploy" gate
polls "Deploy to production${T}in_progress${T}\n"
expect 1 "a commit with no checks but this job is refused" gate
polls "End-to-end tests${T}queued${T}\n"
TRIES=3 expect 1 "checks still running after the wait fail the step" gate
SHA=abc expect 1 "the gate refuses a bad commit" step gate

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
