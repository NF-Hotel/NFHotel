#!/usr/bin/env bash
# Tests of scripts/create-monitoring-lxc.sh (ADR-0020, MIL-023 task 7): the newest downloaded
# Debian 13 template is found by itself, a given template wins, an ID that a container or a
# VM uses is refused, no downloaded template is refused, and the dry run changes nothing.
# Fakes stand in for pct, qm and pveam, so nothing is created.
#
# Run it in Debian 12 with the repository at /src:
#   docker run --rm -v "$PWD:/src:ro" debian:12-slim bash /src/tests/test-create-monitoring-lxc.sh
# Exit:  0 every check passed; 1 a check failed
# Needs: Debian 12 (coreutils)

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

# The fakes log their calls; FAKE_CTS and FAKE_VMS are the IDs in use, FAKE_TEMPLATES the
# downloaded templates.
mkdir -p "$W/bin"
cat >"$W/bin/pveam" <<'EOF'
#!/bin/bash
echo "NAME                                                  SIZE"
for t in $FAKE_TEMPLATES; do echo "$t   123.45MB"; done
EOF
cat >"$W/bin/pct" <<'EOF'
#!/bin/bash
echo "pct $*" >>"$FAKE_LOG"
if [[ "$1" == status ]]; then [[ " $FAKE_CTS " == *" $2 "* ]] && { echo "status: running"; exit 0; }; exit 2; fi
exit 0
EOF
cat >"$W/bin/qm" <<'EOF'
#!/bin/bash
[[ "$1" == status && " $FAKE_VMS " == *" $2 "* ]] && { echo "status: running"; exit 0; }
exit 2
EOF
chmod +x "$W/bin/"*
export PATH="$W/bin:$PATH" FAKE_LOG="$W/log" FAKE_CTS="" FAKE_VMS="100 101"
export FAKE_TEMPLATES="local:vztmpl/debian-12-standard_12.7-1_amd64.tar.zst local:vztmpl/debian-13-standard_13.1-2_amd64.tar.zst local:vztmpl/debian-13-standard_13.6-1_amd64.tar.zst"
run() {
  : >"$W/log"
  bash "$OPS/scripts/create-monitoring-lxc.sh" "$@" >"$W/out" 2>&1
}

run 102 && grep -q "template: local:vztmpl/debian-13-standard_13.6-1_amd64.tar.zst" "$W/out" &&
  ok "the newest downloaded Debian 13 template is chosen by itself" || ko "newest: $(cat "$W/out")"
grep -q "would run: pct create 102 local:vztmpl/debian-13-standard_13.6-1" "$W/out" && [[ ! -s "$W/log" ]] &&
  ok "the dry run prints the create and calls nothing" || ko "dry run: $(cat "$W/log")"

MONITORING_TEMPLATE=local:vztmpl/debian-13-standard_13.1-2_amd64.tar.zst run 102 &&
  grep -q "template: local:vztmpl/debian-13-standard_13.1-2" "$W/out" && ok "a given template wins" || ko "given: $(cat "$W/out")"
MONITORING_TEMPLATE='local:vztmpl/x;rm -rf' run 102 && ko "accepted a bad template" || ok "a template that is not a volume name is refused"

run 101 --apply
[[ $? == 1 ]] && grep -q "the VM 101 uses this ID" "$W/out" && ok "an ID of a VM is refused" || ko "VM ID: $(cat "$W/out")"
FAKE_CTS=102 run 102 --apply
[[ $? == 1 ]] && grep -q "container 102 exists already" "$W/out" && ok "an ID of a container is refused" || ko "CT ID"
FAKE_TEMPLATES="local:vztmpl/debian-12-standard_12.7-1_amd64.tar.zst" run 102 --apply
[[ $? == 1 ]] && grep -q "no Debian 13 template is downloaded" "$W/out" && ! grep -q "pct create" "$W/log" &&
  ok "no downloaded Debian 13 template is refused before anything is created" || ko "no template: $(cat "$W/out")"

run 102 --apply && grep -q "pct create 102 local:vztmpl/debian-13-standard_13.6-1_amd64.tar.zst" "$W/log" &&
  grep -q "pct exec 102" "$W/log" && ok "--apply creates, starts and sets up the container" || ko "apply: $(cat "$W/log")"
run 12 && ko "accepted a two-digit ID" || ok "an ID below 100 is refused"

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
