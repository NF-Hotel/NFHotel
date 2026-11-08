#!/usr/bin/env bash
# Remove the graphical desktop from the NF Hotel VM (MIL-068; CIS-001 rows 2.1.x and 2.2.x):
# the two tasks of the Debian installer (task-desktop, task-gnome-desktop), GNOME, gdm3,
# LibreOffice, CUPS, Avahi, ModemManager, Bluetooth and the other programs and services of the
# desktop, so that the VM starts multi-user.target and runs the services of the architecture
# only. Run it as root on the VM, from the checkout /opt/nfhotel or over SSH:
#   ssh root@<vm> 'bash -s' <ops/scripts/remove-desktop.sh               (dry run)
#   ssh root@<vm> 'bash -s -- --apply' <ops/scripts/remove-desktop.sh
#
# It:
#   1. lists the installed packages of the two lists below
#   2. marks the packages to keep as installed by hand in a copy of the apt state file and
#      simulates the purge of the list with --auto-remove (apt-get -s); it refuses when a
#      package to keep would go, and lists what would be purged; no package or mark changes
#   3. with --apply: marks the packages to keep as installed by hand, simulates again on the
#      real state, purges the list and what nothing needs any more, checks that no package of
#      the list is left and that the packages to keep are still installed, and sets
#      multi-user.target as the default target
#
# The mark is what makes the purge safe: on the VM, sudo, NetworkManager and jq came in as
# dependencies of the desktop tasks (marked automatic), so a purge of the tasks with
# --auto-remove would take the sudo of the deploy account and the network of ens18 along.
# Restart the VM after --apply: the default target takes effect at the next start.
#
# Usage: remove-desktop.sh [--apply]
#   --apply  purge; without it the script only simulates (dry run) and changes nothing
# Exit:  0 the dry run found nothing to refuse, or the purge is done; 1 refused or a step
#        failed; 2 usage error
# Needs: bash 4.4+, apt-get, apt-mark and dpkg-query; root and systemctl for --apply

set -euo pipefail

# The packages that the VM needs (MIL-068 task 1, the survey of 2026-11-08): the ones that
# MIL-068 names, and what a script of ops/ or a unit on the VM uses: openssh-client (the copy
# to the backup target), git (the checkout and the deploy), logrotate, iproute2 (ip, ss),
# util-linux (flock, setsid), diffutils (cmp, diff), procps (sysctl), openssl (the database
# roles), apparmor (the profile of the containers), systemd-timesyncd (the clock that TLS and
# the names of the backups need) and locales-all (the locale of the VM, en_US.UTF-8, which is
# not generated in the locale archive).
readonly KEEP_PACKAGES=(
  sudo openssh-server network-manager tailscale
  docker-ce docker-ce-cli containerd.io docker-compose-plugin docker-buildx-plugin
  nftables qemu-guest-agent unattended-upgrades age rsync jq curl ca-certificates
  openssh-client git logrotate iproute2 util-linux diffutils procps openssl apparmor
  systemd-timesyncd locales-all
)
# The desktop (the survey of 2026-11-08): the two tasks, and the programs and services of the
# desktop that a purge of the tasks alone leaves behind. What nothing needs any more goes too.
readonly REMOVE_PACKAGES=(
  task-desktop task-gnome-desktop gnome gnome-core gdm3 gnome-shell xwayland
  libreoffice-core libreoffice-common
  cups cups-browsed cups-daemon cups-client cups-common avahi-daemon modemmanager bluez
  packagekit colord fwupd ibus inetutils-telnet power-profiles-daemon accountsservice
  udisks2 switcheroo-control wpasupplicant upower low-memory-monitor ppp
  firefox-esr gnome-terminal yelp plymouth pipewire wireplumber xdg-desktop-portal
  desktop-base x11-utils x11-xserver-utils xterm poppler-utils pinentry-gnome3
  xorg-docs-core sound-theme-freedesktop dconf-service gstreamer1.0-plugins-base
  gstreamer1.0-libav gstreamer1.0-x mesa-va-drivers mesa-vdpau-drivers mesa-vulkan-drivers
)
readonly STATE_FILE=/var/lib/apt/extended_states
# A run of unattended-upgrades holds the lock of dpkg; wait for it instead of failing.
readonly LOCK_WAIT=(-o DPkg::Lock::Timeout=600)
# Sorting and the output of apt the same on every machine.
export LC_ALL=C

# The script also runs from standard input (bash -s over SSH), where no file is next to it,
# so it keeps its own copies of die and usage (scripts/lib/common.sh).
die() {
  echo "error: $*" >&2
  exit 1
}
usage() {
  if [[ -f "${BASH_SOURCE[0]:-}" ]]; then
    sed -n '2,/^set -euo/p' "${BASH_SOURCE[0]}" | sed '$d' | sed 's/^# \{0,1\}//'
  else
    echo "Usage: remove-desktop.sh [--apply]"
  fi
}

# installed <package>...: the installed packages among the arguments, one per line, sorted
installed() {
  dpkg-query -W -f='${Package} ${db:Status-Abbrev}\n' "$@" 2>/dev/null | awk '$2 ~ /^ii/ { print $1 }' | sort -u
}

# simulate [<state file>]: the packages that the purge of the list would take, one per line,
# sorted; with a state file, apt reads the marks from it instead of the real one. Call it
# with its output going to a file, so that die ends the script.
simulate() {
  local -a options=("${LOCK_WAIT[@]}")
  [[ -z "${1:-}" ]] || options+=(-o "Dir::State::extended_states=$1")
  if ! apt-get -s "${options[@]}" purge --auto-remove "${remove[@]}" </dev/null >"$work/simulation" 2>&1; then
    sed 's/^/    /' "$work/simulation" >&2
    die "apt-get could not simulate the purge"
  fi
  awk '$1 == "Purg" || $1 == "Remv" { print $2 }' "$work/simulation" | sort -u
}

# refuse_taken <file>: refuse when a package to keep is among the packages in <file>
refuse_taken() {
  local taken
  taken="$(printf '%s\n' "${keep[@]}" | comm -12 - "$1" | paste -sd ' ' -)"
  [[ -z "$taken" ]] || die "refused: the purge would also take these packages to keep: $taken"
}

# report <file>: the packages in <file>, with their number and installed size
report() {
  local size
  size="$(dpkg-query -W -f='${Package} ${Installed-Size}\n' |
    awk 'NR == FNR { goes[$1]; next } $1 in goes { kib += $2 } END { print int(kib / 1024) }' "$1" -)"
  echo "    apt: $(grep -E '^[0-9]+ upgraded' "$work/simulation" || echo 'no summary')"
  echo "    would purge $(wc -l <"$1") packages ($size MB):"
  fmt -w 96 "$1" | sed 's/^/      /'
  echo "    no package to keep would go"
}

set_default_target() {
  echo "==> set multi-user.target as the default target"
  systemctl set-default multi-user.target || die "systemctl could not set the default target"
}

apply=0
while (($#)); do
  case "$1" in
    --apply) apply=1 ;;
    -h | --help)
      usage
      exit 0
      ;;
    *)
      usage >&2
      exit 2
      ;;
  esac
  shift
done
((! apply)) || [[ "$(id -u)" == 0 ]] || die "run it as root (sudo) for --apply"

work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
mapfile -t remove < <(installed "${REMOVE_PACKAGES[@]}")
mapfile -t keep < <(installed "${KEEP_PACKAGES[@]}")
mapfile -t keep_absent < <(printf '%s\n' "${KEEP_PACKAGES[@]}" | sort -u | comm -23 - <(printf '%s\n' "${keep[@]}"))

echo "==> 1. the packages of the lists on $(uname -n)"
echo "    to remove, installed (${#remove[@]}): ${remove[*]}"
echo "    to keep, installed (${#keep[@]}): ${keep[*]}"
((${#keep_absent[@]} == 0)) || echo "    to keep, not installed: ${keep_absent[*]}"
if ((${#remove[@]} == 0)); then
  echo "    none of the packages to remove is installed"
  ((! apply)) || set_default_target
  exit 0
fi

echo "==> 2. simulate the purge with the packages to keep marked as installed by hand (in a copy of the apt state)"
copy="$work/extended_states"
if [[ -f "$STATE_FILE" ]]; then
  cp "$STATE_FILE" "$copy"
else
  : >"$copy"
fi
if ((${#keep[@]})); then
  apt-mark -o "Dir::State::extended_states=$copy" manual "${keep[@]}" </dev/null >/dev/null ||
    die "apt-mark could not mark the packages to keep in the copy"
fi
simulate "$copy" >"$work/purge"
refuse_taken "$work/purge"
report "$work/purge"
if ((! apply)); then
  echo "==> dry run: nothing changed; give --apply to purge, then restart the VM"
  exit 0
fi

echo "==> 3. mark the packages to keep as installed by hand"
if ((${#keep[@]})); then
  apt-mark manual "${keep[@]}" </dev/null || die "apt-mark could not mark the packages to keep"
fi

echo "==> 4. simulate again on the real state"
simulate "" >"$work/purge-real"
refuse_taken "$work/purge-real"
cmp -s "$work/purge" "$work/purge-real" ||
  echo "    the real state gives $(wc -l <"$work/purge-real") packages, the copy gave $(wc -l <"$work/purge")"

echo "==> 5. purge"
DEBIAN_FRONTEND=noninteractive apt-get -y "${LOCK_WAIT[@]}" purge --auto-remove "${remove[@]}" </dev/null ||
  die "apt-get could not purge the packages"

echo "==> 6. check"
mapfile -t left < <(installed "${REMOVE_PACKAGES[@]}")
((${#left[@]} == 0)) || die "still installed after the purge: ${left[*]}"
if ((${#keep[@]})); then
  mapfile -t kept < <(installed "${keep[@]}")
  ((${#kept[@]} == ${#keep[@]})) ||
    die "a package to keep is gone: $(printf '%s\n' "${keep[@]}" | comm -23 - <(printf '%s\n' "${kept[@]}") | paste -sd ' ' -)"
fi
echo "    no package of the list is installed, and the ${#keep[@]} packages to keep are"
set_default_target
echo "==> done: restart the VM, so that it starts without the desktop"
