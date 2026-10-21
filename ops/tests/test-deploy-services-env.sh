#!/usr/bin/env bash
# Test of the settings that scripts/deploy-services.sh reads from the settings files: a value
# that ends with = (base64) must reach compose whole. A fake docker records API_TOKEN and
# POSTGRES_PASSWORD at `compose up`, so no container runs.
#
# Run it with the repository at /src or from a checkout:  bash tests/test-deploy-services-env.sh
# Exit:  0 every check passed; 1 a check failed
set -uo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
mkdir -p "$work/repo/scripts" "$work/repo/deploy/postgres" "$work/repo/deploy/nfhotel-api" "$work/repo/deploy/nfhotel-web" "$work/bin"
cp -r "$root/scripts/." "$work/repo/scripts/"
token='abc+def/ghi0123456789ABCDEFGHIJKLMNOPQR='
password='pw+with/equals=='
printf 'POSTGRES_USER=u\nPOSTGRES_PASSWORD=%s\nPOSTGRES_DB=d\n' "$password" >"$work/repo/deploy/postgres/postgres.env"
printf '# a comment\nApi__Token=%s\nOther=1\n' "$token" >"$work/repo/deploy/nfhotel-api/nfhotel-api.env"
: >"$work/repo/deploy/nfhotel-web/nfhotel-web.env"
cat >"$work/bin/docker" <<FAKE
#!/bin/bash
case "\$*" in
  *" up "*) echo "\$PWD api_token=\${API_TOKEN-} pg=\${POSTGRES_PASSWORD-}" >>"$work/calls" ;;
  # deploy-services.sh waits for a healthy database and API before web.
  inspect\ -f*) echo "true healthy" ;;
esac
exit 0
FAKE
chmod +x "$work/bin/docker"

pass=0
fail=0
PATH="$work/bin:$PATH" bash "$work/repo/scripts/deploy-services.sh" --no-build postgres api web >"$work/out" 2>&1 || echo "note: the script exited non-zero: $(tail -1 "$work/out")"
if grep -q "api_token=$token " "$work/calls" 2>/dev/null; then
  echo "PASS  the API token keeps its trailing ="
  pass=$((pass + 1))
else
  echo "FAIL  the API token lost its trailing = ($(grep -o 'api_token=[^ ]*' "$work/calls" 2>/dev/null | head -1))"
  fail=$((fail + 1))
fi
if grep -q "pg=$password\$" "$work/calls" 2>/dev/null; then
  echo "PASS  the database password keeps its trailing ="
  pass=$((pass + 1))
else
  echo "FAIL  the database password lost its trailing ="
  fail=$((fail + 1))
fi
echo "RESULT: $pass passed, $fail failed"
((fail == 0))
