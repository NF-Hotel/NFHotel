#!/usr/bin/env bash
# Tests of remove-desktop.sh (MIL-068 task 2) in Debian 13, the system of the VM. Stand-in
# packages, built with dpkg-deb, copy the marks of the VM: a task-desktop that depends on sudo
# and inetutils-telnet brings both in as dependencies (marked automatic), as the desktop task
# brought sudo, NetworkManager and jq onto the VM; an age that depends on inetutils-telnet
# makes a purge of the list take a package to keep along. A stand-in systemctl records the
# change of the default target; a stand-in apt-get whose purge does nothing stands for a
# purge that leaves a package behind, and one that always fails for a broken apt. The checks:
#   - the refusal, written first: when a package to keep would go, the dry run and --apply
#     refuse, and no package, mark or target changes
#   - the trap is real: without the mark, a purge of the task with --auto-remove takes sudo
#   - the dry run lists what it would purge and not sudo, changes nothing, and runs from
#     standard input too (bash -s, as over SSH)
#   - --apply purges the list, keeps sudo marked as installed by hand and sets
#     multi-user.target; a second run has nothing to purge
#   - a package of the list that is still installed after the purge is an error, and the
#     default target stays; a simulation that fails ends the run before it lists anything
#   - --apply needs root; an unknown option is a usage error
#   - the lists of the script hold the packages that MIL-068 names, and no package is on both
#
# Run it in Debian 13 with the repository's ops folder at /src:
#   docker run --rm -v "$PWD/ops:/src:ro" debian:13-slim bash /src/tests/test-remove-desktop.sh
# Exit:  0 every check passed; 1 a check failed
# Needs: Debian 13 (apt-get with the Debian archive, dpkg-deb, setpriv)

# shellcheck disable=SC2015 # ok and ko always return 0, so `test && ok || ko` runs
# exactly one of them; the short form keeps each check on one line.
set -uo pipefail

apt-get -qq update >/dev/null
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

readonly SCRIPT=/src/scripts/remove-desktop.sh
W="$(mktemp -d)"
mkdir -p "$W/bin" "$W/fake" "$W/broken"
cat >"$W/bin/systemctl" <<EOF
#!/bin/sh
echo "\$*" >>"$W/systemctl.log"
EOF
cat >"$W/fake/apt-get" <<'EOF'
#!/bin/sh
# A simulation goes to the real apt-get; the purge does nothing and ends with 0.
for argument in "$@"; do
  [ "$argument" = -s ] && exec /usr/bin/apt-get "$@"
done
exit 0
EOF
cat >"$W/broken/apt-get" <<'EOF'
#!/bin/sh
echo "E: the stand-in apt-get fails" >&2
exit 100
EOF
chmod +x "$W/bin/systemctl" "$W/fake/apt-get" "$W/broken/apt-get"

# make_package <name> <depends>: a stand-in package <name> of version 99, as $W/<name>.deb
make_package() {
  mkdir -p "$W/$1/DEBIAN"
  printf 'Package: %s\nVersion: 99\nArchitecture: all\nMaintainer: NF Hotel ops tests <ops@example.invalid>\nDepends: %s\nDescription: stand-in for the test of remove-desktop.sh\n' \
    "$1" "$2" >"$W/$1/DEBIAN/control"
  dpkg-deb --build --root-owner-group "$W/$1" "$W/$1.deb" >/dev/null
}

# state: the packages with their status, the marks and the target changes, which a dry run
# and a refusal must leave as they are
state() {
  dpkg-query -W -f='${Package} ${db:Status-Abbrev}\n' | sort
  apt-mark showmanual
  cat "$W/systemctl.log" 2>/dev/null
}

# listed <output>: the package names between "would purge" and "no package to keep would go"
listed() {
  sed -n '/would purge/,/no package to keep would go/p' <<<"$1" | sed '1d;$d' | xargs -n1 | sort | xargs
}

make_package task-desktop "sudo, inetutils-telnet"
make_package age inetutils-telnet
apt-get -qq install -y "$W/task-desktop.deb" >/dev/null 2>&1

echo "== a package to keep that the purge would take along (the refusal, written first)"
apt-get -qq install -y "$W/age.deb" >/dev/null 2>&1
before="$(state)"
out="$(bash "$SCRIPT" 2>&1)"
status=$?
((status == 1)) && [[ "$out" == *"refused"*"age"* && "$(state)" == "$before" ]] &&
  ok "the dry run refuses when age, a package to keep, would go with inetutils-telnet, and changes nothing" ||
  ko "dry run with the stand-in age (status $status): $out"
out="$(PATH="$W/bin:$PATH" bash "$SCRIPT" --apply 2>&1)"
status=$?
((status == 1)) && [[ "$out" == *"refused"*"age"* && "$(state)" == "$before" && ! -e "$W/systemctl.log" ]] &&
  ok "--apply refuses before it marks or purges a package or changes the default target" ||
  ko "--apply with the stand-in age (status $status): $out"
dpkg --purge age >/dev/null 2>&1

echo "== the marks of the VM"
[[ "$(apt-mark showauto sudo inetutils-telnet | sort | xargs)" == "inetutils-telnet sudo" ]] &&
  ok "the stand-in task brought sudo and inetutils-telnet in as dependencies, as on the VM" ||
  ko "marks: $(apt-mark showauto | xargs)"
apt-get -s purge --auto-remove task-desktop 2>/dev/null | grep '^Purg sudo ' >/dev/null &&
  ok "without the mark, a purge of the task with --auto-remove would take sudo along" ||
  ko "the trap of the VM is not reproduced"

echo "== the dry run"
before="$(state)"
out="$(bash "$SCRIPT" 2>&1)"
status=$?
would="$(listed "$out")"
((status == 0)) && [[ " $would " == *" inetutils-telnet "* && " $would " == *" task-desktop "* && " $would " != *" sudo "* ]] &&
  ok "the dry run lists task-desktop, inetutils-telnet and what only they need as the packages it would purge, and not sudo" ||
  ko "dry run (status $status): $out"
[[ "$(state)" == "$before" ]] && ok "the dry run changes no package and no mark" || ko "the dry run changed the system"
out="$(bash -s <"$SCRIPT" 2>&1)"
status=$?
((status == 0)) && [[ "$(listed "$out")" == "$would" && "$(state)" == "$before" ]] &&
  ok "it runs from standard input too, as over SSH (bash -s), with the same list" || ko "bash -s (status $status): $out"

echo "== --apply"
out="$(PATH="$W/bin:$PATH" bash "$SCRIPT" --apply 2>&1)"
status=$?
((status == 0)) && ok "--apply ends with 0" || ko "--apply (status $status): $out"
left="$(dpkg-query -W -f='${Package} ${db:Status-Abbrev}\n' task-desktop inetutils-telnet 2>/dev/null | awk '$2 !~ /^.n/ { print $1 }' | xargs)"
[[ -z "$left" ]] && ok "it purged task-desktop and inetutils-telnet, configuration files and all" || ko "left: $left"
read -ra would_list <<<"$would"
still="$(dpkg-query -W -f='${Package} ${db:Status-Abbrev}\n' "${would_list[@]}" 2>/dev/null | awk '$2 !~ /^.n/ { print $1 }' | xargs)"
((${#would_list[@]} > 2)) && [[ -z "$still" ]] &&
  ok "every package of the dry run's list is gone with its configuration files, the ${#would_list[@]} with the dependencies that only the list needed" ||
  ko "of the dry run's list, still there: $still"
dpkg-query -W -f='${db:Status-Abbrev}' sudo 2>/dev/null | grep '^ii' >/dev/null && apt-mark showmanual | grep -x sudo >/dev/null &&
  ok "it kept sudo and marked it as installed by hand" || ko "sudo: $(dpkg-query -W -f='${db:Status-Abbrev}' sudo 2>&1)"
[[ "$(cat "$W/systemctl.log" 2>/dev/null)" == "set-default multi-user.target" ]] &&
  ok "it set multi-user.target as the default target" || ko "systemctl: $(cat "$W/systemctl.log" 2>&1)"
out="$(PATH="$W/bin:$PATH" bash "$SCRIPT" --apply 2>&1)"
status=$?
((status == 0)) && [[ "$out" == *"none of the packages to remove is installed"* ]] &&
  (($(grep -c '^set-default multi-user.target$' "$W/systemctl.log") == 2)) &&
  ok "a second --apply finds nothing to purge, sets the default target again and ends with 0" ||
  ko "second --apply (status $status): $out"

echo "== a purge that leaves a package of the list"
apt-get -qq install -y "$W/task-desktop.deb" >/dev/null 2>&1
rm -f "$W/systemctl.log"
out="$(PATH="$W/fake:$W/bin:$PATH" bash "$SCRIPT" --apply 2>&1)"
status=$?
((status == 1)) && [[ "$out" == *"still installed"*"task-desktop"* && ! -e "$W/systemctl.log" ]] &&
  ok "--apply fails and names a package of the list that the purge left, and keeps the default target" ||
  ko "a purge that removed nothing (status $status): $out"
out="$(PATH="$W/broken:$PATH" bash "$SCRIPT" 2>&1)"
status=$?
((status == 1)) && [[ "$out" == *"could not simulate"* && "$out" != *"would purge"* ]] &&
  ok "a simulation that fails ends the dry run with 1, before it says what would go" ||
  ko "a simulation that fails (status $status): $out"

echo "== root and usage"
out="$(setpriv --reuid=65534 --regid=65534 --clear-groups bash "$SCRIPT" --apply 2>&1)"
status=$?
((status == 1)) && [[ "$out" == *"run it as root"* ]] && ok "--apply refuses to run without root" ||
  ko "not root (status $status): $out"
bash "$SCRIPT" --purge >/dev/null 2>&1
status=$?
((status == 2)) && ok "an unknown option is a usage error (2)" || ko "unknown option (status $status)"
[[ "$(bash "$SCRIPT" --help 2>&1)" == *"Usage: remove-desktop.sh [--apply]"* ]] && ok "--help prints the usage" || ko "--help"

echo "== the lists"
eval "$(sed -n '/^readonly KEEP_PACKAGES=(/,/^)/p; /^readonly REMOVE_PACKAGES=(/,/^)/p' "$SCRIPT")"
missing=""
for package in sudo openssh-server network-manager tailscale docker-ce docker-ce-cli containerd.io \
  docker-compose-plugin docker-buildx-plugin nftables qemu-guest-agent unattended-upgrades age rsync jq \
  curl ca-certificates; do
  # shellcheck disable=SC2154 # the eval above defines KEEP_PACKAGES and REMOVE_PACKAGES
  [[ " ${KEEP_PACKAGES[*]} " == *" $package "* ]] || missing+="$package "
done
[[ -z "$missing" ]] && ok "the packages to keep hold the ones that MIL-068 names" || ko "not kept: $missing"
missing=""
for package in task-desktop task-gnome-desktop gdm3 gnome-shell libreoffice-core cups avahi-daemon \
  modemmanager bluez packagekit inetutils-telnet; do
  [[ " ${REMOVE_PACKAGES[*]} " == *" $package "* ]] || missing+="$package "
done
[[ -z "$missing" ]] && ok "the packages to remove hold the ones that criterion 3 of MIL-068 names" ||
  ko "not removed: $missing"
both="$(comm -12 <(printf '%s\n' "${KEEP_PACKAGES[@]}" | sort) <(printf '%s\n' "${REMOVE_PACKAGES[@]}" | sort) | xargs)"
[[ -z "$both" ]] && ok "no package is on both lists" || ko "on both lists: $both"

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
