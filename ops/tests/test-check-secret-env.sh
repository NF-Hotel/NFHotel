#!/usr/bin/env bash
# Tests of scripts/check-secret-env.sh (MIL-035 criterion 2): containers whose secrets are files
# pass; a secret of the list, or a variable named like a secret, fails and only its name is
# printed; a _FILE variable is allowed; a missing service fails. A fake stands in for docker.
#
# Run it in Debian 12 with the repository at /src:
#   docker run --rm -v "$PWD:/src:ro" debian:12-slim bash /src/tests/test-check-secret-env.sh
# Exit:  0 every check passed; 1 a check failed
# Needs: Debian 12

# shellcheck disable=SC2015 # ok and ko always return 0, so `test && ok || ko` runs
# exactly one of them; the short form keeps each check on one line.
set -uo pipefail

OPS="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
W="$(mktemp -d)"
trap 'rm -rf "$W"' EXIT
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

# The fake answers ps with $W/ps/<compose service> and inspect with $W/env/<container>.
mkdir -p "$W/ps" "$W/env"
cat >"$W/docker" <<'EOF'
#!/bin/bash
case "$1" in
  ps) cat "$FAKE_DIR/ps/${3#label=com.docker.compose.service=}" 2>/dev/null ;;
  inspect) cat "$FAKE_DIR/env/${4}" ;;
esac
EOF
chmod +x "$W/docker"
export NFHOTEL_DOCKER="$W/docker" FAKE_DIR="$W"
echo "nfhotel-postgres-postgres-1" >"$W/ps/postgres"
echo "nfhotel-api-nfhotel-api-1" >"$W/ps/nfhotel-api"
printf '%s\n' nfhotel-web-nfhotel-web-1 nfhotel-e2e-nfhotel-web-nfhotel-web-1 >"$W/ps/nfhotel-web"
printf '%s\n' POSTGRES_USER=nfhotel POSTGRES_PASSWORD_FILE=/run/secrets/POSTGRES_PASSWORD PG_SHA256=abc >"$W/env/nfhotel-postgres-postgres-1"
printf '%s\n' Database__MigrateOnStart=false APP_UID=1654 Staff__InitialManager__MustChangePassword=true >"$W/env/nfhotel-api-nfhotel-api-1"
printf '%s\n' Api__BaseUrl=http://nfhotel-api:8080 DataProtection__KeysPath=/app/keys >"$W/env/nfhotel-web-nfhotel-web-1"
check() { bash "$OPS/scripts/check-secret-env.sh" "$@" >"$W/out" 2>&1; }

check --name nfhotel-e2e && ko "passed without the containers of the prefix" || ok "a missing service fails"
check && grep -c PASS "$W/out" | grep -qx 4 && ok "containers with their secrets in files pass; _FILE and a known setting are allowed" ||
  ko "clean: $(cat "$W/out")"

echo "Api__Token=s3cr3t-value" >>"$W/env/nfhotel-web-nfhotel-web-1"
check
[[ $? == 1 ]] && grep -q "nfhotel-web-nfhotel-web-1 has secrets in its environment: Api__Token" "$W/out" &&
  ! grep -q s3cr3t-value "$W/out" && ok "a secret of the list fails, by name only" || ko "listed: $(cat "$W/out")"

printf '%s\n' Database__MigrateOnStart=false NEW_SIGNING_KEY=x >"$W/env/nfhotel-api-nfhotel-api-1"
check
[[ $? == 1 ]] && grep -q "NEW_SIGNING_KEY" "$W/out" && ok "a variable named like a secret fails" || ko "named: $(cat "$W/out")"

check --extra && ko "accepted an unknown option" || ok "an unknown option is a usage error"

# The tunnel of MIL-040 is checked when it runs, and its absence is no failure.
echo "nfhotel-cloudflared-cloudflared-1" >"$W/ps/cloudflared"
printf '%s\n' NFHOTEL_SETTINGS_HASH=abc TUNNEL_TOKEN=leaked >"$W/env/nfhotel-cloudflared-cloudflared-1"
printf '%s\n' Database__MigrateOnStart=false >"$W/env/nfhotel-api-nfhotel-api-1"
echo "Api__BaseUrl=http://nfhotel-api:8080" >"$W/env/nfhotel-web-nfhotel-web-1"
check
[[ $? == 1 ]] && grep -q "nfhotel-cloudflared-cloudflared-1 has secrets in its environment: TUNNEL_TOKEN" "$W/out" &&
  ok "a tunnel token in the environment fails" || ko "tunnel: $(cat "$W/out")"
echo "NFHOTEL_SETTINGS_HASH=abc" >"$W/env/nfhotel-cloudflared-cloudflared-1"
check && grep -q "nfhotel-cloudflared-cloudflared-1 has no secret" "$W/out" &&
  ok "the tunnel with its token as a file passes" || ko "tunnel clean: $(cat "$W/out")"

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
