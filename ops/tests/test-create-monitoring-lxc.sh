#!/usr/bin/env bash
# Tests of scripts/create-monitoring-lxc.sh (ADR-0020, MIL-023 task 7): the newest downloaded
# Debian 13 template is found by itself, a given template wins, an ID that a container or a
# VM uses is refused, no downloaded template is refused, and the dry run changes nothing.
# Inside the container it installs the Docker packages of the VM from Docker's signed
# repository, no Docker package of Debian, and Debian's security updates with
# install-patching.sh from its checkout (ADR-0043, MIL-083 task 4).
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
# The runtime of the VM (ADR-0043): its packages, read from create-docker-vm.sh, from Docker's
# signed repository, and none of the Docker packages of Debian.
vm_docker="$(sed -n 's/^ *- apt-get install -y \(docker-ce .*\)$/\1/p' "$OPS/scripts/create-docker-vm.sh")"
[[ -n "$vm_docker" ]] && grep -qxF "apt-get install -y -q $vm_docker" "$W/log" &&
  ok "the container gets the Docker packages of the VM: $vm_docker" ||
  ko "Docker packages (the VM's: '$vm_docker'): $(grep -F 'apt-get install' "$W/log")"
grep -qxF 'curl -fsSL https://download.docker.com/linux/debian/gpg -o /etc/apt/keyrings/docker.asc' "$W/log" &&
  grep -qF 'deb [arch=amd64 signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/debian trixie stable' "$W/log" &&
  ok "they come from Docker's signed repository for Debian 13" || ko "Docker's repository: $(grep -F docker "$W/log" | head -6)"
! grep -qE '(^|[[:space:]])(docker\.io|docker-compose|docker-cli|docker-buildx|containerd|runc)([[:space:]]|$)' "$W/log" &&
  ok "no Docker package of Debian (docker.io, docker-cli, docker-compose, docker-buildx, containerd, runc)" ||
  ko "a Docker package of Debian: $(grep -E 'docker\.io|docker-compose |docker-cli |docker-buildx |containerd |runc' "$W/log")"
# Debian's security updates as on the VM: install-patching.sh from the checkout, after Docker.
awk '/^apt-get install -y -q docker-ce / { d = NR }
  /^pct exec 102 -- git clone https:\/\/github.com\/NF-Hotel\/NFHotel.git \/opt\/nfhotel-monitoring$/ { c = NR }
  /^pct exec 102 -- bash \/opt\/nfhotel-monitoring\/ops\/scripts\/install-patching.sh --apply$/ { p = NR }
  END { exit !(d && c && p && d < c && c < p) }' "$W/log" &&
  ok "it clones the checkout and installs Debian's security updates with install-patching.sh, after Docker" ||
  ko "the checkout and the patching: $(grep -F 'pct exec' "$W/log")"
sed -n '/^==> done/,$p' "$W/out" | grep -q "deploy-monitoring.sh --apply" && ! sed -n '/^==> done/,$p' "$W/out" | grep -q "git clone" &&
  ok "the steps left to S03 end with the deploy, without the clone" || ko "S03's steps: $(sed -n '/^==> done/,$p' "$W/out")"
run 102 && grep -qxF "would run: pct exec 102 -- bash /opt/nfhotel-monitoring/ops/scripts/install-patching.sh --apply" "$W/out" &&
  [[ ! -s "$W/log" ]] && ok "the dry run prints the patching and runs nothing" || ko "dry run of the patching: $(grep -F install-patching "$W/out")"
run 12 && ko "accepted a two-digit ID" || ok "an ID below 100 is refused"

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
