#!/usr/bin/env bash
# Apply the host firewall of the NF Hotel VM (MIL-023 task 3, CIS-001):
# deploy/host/nftables/nfhotel.nft, the table inet nfhotel. S01 or S03 runs it as root in
# the checkout /opt/nfhotel, logged in over Tailscale: after the change, port 22 answers only
# on tailscale0.
#
# A firewall change can lock the operator out, so it is done in two steps:
#   --apply    checks the ruleset, keeps the table that is loaded now, loads the new one and
#              starts a timer that puts the old table back after --timeout seconds
#   --confirm  run from a NEW ssh session over Tailscale before the timer ends: it stops the
#              timer and makes the ruleset permanent (/etc/nfhotel/nftables.nft and
#              nfhotel-firewall.service, which loads it at boot)
# If the new session cannot log in, wait: the old table comes back by itself. --revert puts
# it back at once. An established session keeps working throughout.
#
# Without an action the script is a dry run: it checks the ruleset with `nft -c` and shows
# what is loaded now, and changes nothing.
#
# Usage: apply-firewall.sh [--apply [--timeout <seconds>] | --confirm | --revert]
#   --timeout  seconds before an unconfirmed ruleset is reverted (default 120, 10 to 3600)
# Env:   NFHOTEL_FIREWALL_STATE  folder of the pending change (default /var/lib/nfhotel/firewall)
#        NFHOTEL_ETC_DIR         folder of the installed ruleset (default /etc/nfhotel)
#        NFHOTEL_SYSTEMD_DIR     folder of the unit (default /etc/systemd/system)
# Exit:  0 done; 1 a step failed; 2 usage error
# Needs: bash 4+, root (except the dry run), nftables, setsid; systemd for --confirm

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=ops/scripts/lib/common.sh
. "$ROOT/scripts/lib/common.sh"
readonly RULESET="$ROOT/deploy/host/nftables/nfhotel.nft"
readonly UNIT_SOURCE="$ROOT/deploy/systemd/nfhotel-firewall.service"
readonly TABLE=(inet nfhotel)
readonly STATE="${NFHOTEL_FIREWALL_STATE:-/var/lib/nfhotel/firewall}"
readonly ETC_DIR="${NFHOTEL_ETC_DIR:-/etc/nfhotel}"
readonly SYSTEMD_DIR="${NFHOTEL_SYSTEMD_DIR:-/etc/systemd/system}"
readonly REVERT_FILE="$STATE/revert.nft" TIMER_PID="$STATE/revert.pid" REVERT_LOG="$STATE/revert.log"
action=check
timeout=120

while (($#)); do
  case "$1" in
    --apply | --confirm | --revert) action="${1#--}" ;;
    --timeout)
      timeout="${2:?--timeout needs a value}"
      shift
      ;;
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
if [[ ! "$timeout" =~ ^[0-9]+$ ]] || ((timeout < 10 || timeout > 3600)); then
  die "--timeout must be 10 to 3600 seconds"
fi
[[ "$action" == check ]] || [[ "$(id -u)" == 0 ]] || die "run it as root (sudo)"
command -v nft >/dev/null 2>&1 || die "nft is missing (apt-get install nftables)"

# stop_timer: end a pending automatic revert, if there is one.
stop_timer() {
  if [[ -f "$TIMER_PID" ]]; then
    kill "$(cat "$TIMER_PID")" 2>/dev/null || true
    rm -f "$TIMER_PID"
  fi
}

case "$action" in
  check)
    echo "==> checking $RULESET"
    nft -c -f "$RULESET" || die "nft refuses the ruleset"
    echo "    nft -c accepts it"
    if nft list table "${TABLE[@]}" >/dev/null 2>&1; then
      echo "==> the table ${TABLE[*]} is loaded now"
    else
      echo "==> no table ${TABLE[*]} is loaded now"
    fi
    [[ -f "$TIMER_PID" ]] && echo "==> a change is waiting for --confirm"
    # Debian's own nftables.service flushes the whole ruleset, Docker's rules included.
    if systemctl is-enabled nftables.service >/dev/null 2>&1; then
      echo "warning: nftables.service is enabled; its 'flush ruleset' would remove Docker's rules"
    fi
    client="${SSH_CONNECTION:-}"
    client="${client%% *}"
    if [[ -n "$client" && ! "$client" =~ ^100\.(6[4-9]|[7-9][0-9]|1[01][0-9]|12[0-7])\. ]]; then
      echo "warning: this session comes from $client, not the tailnet; after --apply a new ssh"
      echo "         session must use Tailscale"
    fi
    echo "==> dry run; give --apply to load it with an automatic revert"
    ;;

  apply)
    nft -c -f "$RULESET" || die "nft refuses the ruleset"
    install -d -m 0700 "$STATE"
    stop_timer
    # The revert file puts back exactly what is loaded now: the old table, or none.
    {
      echo "table ${TABLE[*]}"
      echo "delete table ${TABLE[*]}"
      nft list table "${TABLE[@]}" 2>/dev/null || true
    } >"$REVERT_FILE"
    nft -f "$RULESET"
    echo "==> loaded $RULESET"
    # setsid takes the timer out of this session, so it still runs if the session drops.
    # shellcheck disable=SC2016 # the inner script expands its own arguments
    setsid bash -c 'sleep "$1"; nft -f "$2" && echo "$(date -u +%FT%TZ) reverted" >>"$3"; rm -f "$4"' \
      _ "$timeout" "$REVERT_FILE" "$REVERT_LOG" "$TIMER_PID" </dev/null >/dev/null 2>&1 &
    echo "$!" >"$TIMER_PID"
    echo "==> the old table comes back in $timeout seconds unless you confirm. From a NEW ssh"
    echo "    session over Tailscale, run: sudo $0 --confirm"
    ;;

  confirm)
    [[ -f "$TIMER_PID" ]] || die "no change is waiting; run --apply first"
    stop_timer
    install -D -o root -g root -m 0644 "$RULESET" "$ETC_DIR/nftables.nft"
    install -D -o root -g root -m 0644 "$UNIT_SOURCE" "$SYSTEMD_DIR/nfhotel-firewall.service"
    if [[ -d /run/systemd/system ]]; then
      systemctl daemon-reload
      systemctl enable nfhotel-firewall.service
    else
      echo "    systemd is not running here; enable nfhotel-firewall.service by hand"
    fi
    echo "==> confirmed; the firewall is permanent and loads at boot"
    ;;

  revert)
    [[ -f "$REVERT_FILE" ]] || die "nothing to revert"
    stop_timer
    nft -f "$REVERT_FILE"
    echo "==> the table from before the last --apply is back"
    ;;
esac
