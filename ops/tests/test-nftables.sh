#!/usr/bin/env bash
# Test of the host firewall (MIL-023 task 3): deploy/host/nftables/nfhotel.nft and
# scripts/apply-firewall.sh. The container stands in for the VM. Network namespaces stand in
# for the internet (on eth1), the tailnet (on an interface named tailscale0) and a container
# on a Docker bridge (br-test), with a nat table that publishes ports the way Docker does.
#
# It checks: the dry run changes nothing; from the internet only 80 and 443 answer; from the
# tailnet 22 answers too; the loopback is open; only the published ports 80 and 443 reach the
# container while a published 5432 does not; the container reaches out; the table stays when
# Docker's own table is replaced (as on a Docker restart); an unconfirmed change reverts by
# itself, to no table or to the table before; --confirm keeps it and installs the boot unit.
#
# Run it in Debian 13 with the repository at /src:
#   docker run --rm --cap-add NET_ADMIN --cap-add SYS_ADMIN --sysctl net.ipv4.ip_forward=1 \
#     -v "$PWD:/src:ro" debian:13-slim bash /src/tests/test-nftables.sh
# Exit:  0 every check passed; 1 a check failed
# Needs: Debian 13 (apt-get), the capabilities NET_ADMIN and SYS_ADMIN

# shellcheck disable=SC2015 # ok and ko always return 0, so `test && ok || ko` runs
# exactly one of them; the short form keeps each check on one line.
set -uo pipefail

apt-get -qq update >/dev/null
apt-get -qq install -y nftables iproute2 netcat-openbsd >/dev/null 2>&1
readonly APPLY=/src/scripts/apply-firewall.sh
export NFHOTEL_FIREWALL_STATE=/tmp/fw-state NFHOTEL_ETC_DIR=/tmp/fw-etc NFHOTEL_SYSTEMD_DIR=/tmp/fw-systemd
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

# peer <name> <vm interface> <vm address> <peer address>: a namespace joined to this one by a
# veth pair; prints the pid that holds the namespace.
peer() {
  unshare --net --fork sleep infinity >/dev/null 2>&1 &
  local pid=$!
  sleep 0.3
  ip link add "$2" type veth peer name "p-$1"
  ip link set "p-$1" netns "$pid"
  ip addr add "$3/24" dev "$2" && ip link set "$2" up
  nsenter -t "$pid" -n ip addr add "$4/24" dev "p-$1"
  nsenter -t "$pid" -n ip link set "p-$1" up
  nsenter -t "$pid" -n ip link set lo up
  echo "$pid"
}
# reach <pid> <address> <port>: a TCP connection from the namespace of pid succeeds.
reach() { nsenter -t "$1" -n nc -z -w 2 "$2" "$3" 2>/dev/null; }
listen() { nc -lk "$1" >/dev/null 2>&1 & }

internet="$(peer inet eth1 10.1.0.1 10.1.0.2)"
tailnet="$(peer ts tailscale0 100.64.0.1 100.64.0.2)"
# A container on a Docker-like bridge, its default route through the VM.
container="$(peer ctr c-veth 172.30.0.254 172.30.0.2)"
ip addr del 172.30.0.254/24 dev c-veth
ip link add br-test type bridge && ip addr add 172.30.0.1/24 dev br-test && ip link set br-test up
ip link set c-veth master br-test
nsenter -t "$container" -n ip route add default via 172.30.0.1
nsenter -t "$internet" -n nc -lk 9000 >/dev/null 2>&1 &
for port in 80 5432; do nsenter -t "$container" -n nc -lk "$port" >/dev/null 2>&1 & done
# Docker's part: publish 80 and 5432 of the container on the VM, and masquerade it going out.
nft -f - <<'EOF'
table ip dockersim {
  chain prerouting {
    type nat hook prerouting priority dstnat;
    tcp dport { 80, 5432 } dnat to 172.30.0.2
  }
  chain postrouting {
    type nat hook postrouting priority srcnat;
    ip saddr 172.30.0.0/24 oifname != "br-test" masquerade
  }
}
EOF
for port in 22 443 8080; do listen "$port"; done
sleep 0.5

echo "== before: everything answers"
reach "$internet" 10.1.0.1 22 && reach "$internet" 10.1.0.1 8080 && reach "$internet" 10.1.0.1 5432 &&
  reach "$container" 10.1.0.2 9000 && ok "with no firewall everything answers" || ko "test setup"

echo "== dry run"
out="$(bash "$APPLY" 2>&1)"
[[ "$out" == *"nft -c accepts it"* && "$out" == *"dry run"* ]] && ok "the dry run checks the ruleset" || ko "dry run: $out"
nft list table inet nfhotel >/dev/null 2>&1 && ko "the dry run loaded the table" || ok "the dry run loads nothing"

echo "== --apply"
bash "$APPLY" --apply --timeout 60 >/dev/null && ok "--apply loads the ruleset" || ko "--apply"
reach "$internet" 10.1.0.1 443 && ok "internet: 443 answers" || ko "internet: 443"
reach "$internet" 10.1.0.1 80 && ok "internet: 80 (published) reaches the container" || ko "internet: 80"
reach "$internet" 10.1.0.1 22 && ko "internet: 22 answers" || ok "internet: 22 is closed"
reach "$internet" 10.1.0.1 8080 && ko "internet: 8080 answers" || ok "internet: 8080 is closed"
reach "$internet" 10.1.0.1 5432 && ko "internet: published 5432 answers" || ok "internet: published 5432 is filtered"
reach "$tailnet" 100.64.0.1 22 && ok "tailnet: 22 answers" || ko "tailnet: 22"
reach "$tailnet" 100.64.0.1 443 && ok "tailnet: 443 answers" || ko "tailnet: 443"
reach "$tailnet" 100.64.0.1 8080 && ko "tailnet: 8080 answers" || ok "tailnet: 8080 is closed"
nc -z -w 2 127.0.0.1 8080 >/dev/null 2>&1 && ok "the loopback is open" || ko "loopback"
reach "$container" 10.1.0.2 9000 && ok "the container reaches out" || ko "container out"

echo "== Docker replaces its own table (as on a restart)"
nft delete table ip dockersim
nft -f - <<'EOF'
table ip dockersim {
  chain prerouting {
    type nat hook prerouting priority dstnat;
    tcp dport { 80, 5432 } dnat to 172.30.0.2
  }
  chain postrouting {
    type nat hook postrouting priority srcnat;
    ip saddr 172.30.0.0/24 oifname != "br-test" masquerade
  }
}
EOF
nft list table inet nfhotel >/dev/null 2>&1 && ok "the table inet nfhotel stays" || ko "table gone"
reach "$internet" 10.1.0.1 80 && ok "80 still reaches the container" || ko "80 after restart"
reach "$internet" 10.1.0.1 5432 && ko "5432 answers after the restart" || ok "5432 is still filtered"

echo "== --confirm"
bash "$APPLY" --confirm >/dev/null && ok "--confirm" || ko "--confirm"
cmp -s /src/deploy/host/nftables/nfhotel.nft /tmp/fw-etc/nftables.nft && [[ -f /tmp/fw-systemd/nfhotel-firewall.service ]] &&
  ok "the ruleset and the boot unit are installed" || ko "install"
sleep 1
nft list table inet nfhotel >/dev/null 2>&1 && [[ ! -f /tmp/fw-state/revert.pid ]] &&
  ok "the confirmed table stays and no timer is left" || ko "after confirm"
bash "$APPLY" --confirm >/dev/null 2>&1 && ko "a second --confirm was accepted" || ok "--confirm without a pending change is refused"
nft -f /tmp/fw-etc/nftables.nft && nft -f /tmp/fw-etc/nftables.nft && [[ "$(nft list tables | grep -c 'inet nfhotel')" == 1 ]] &&
  ok "loading the installed file twice keeps one table (boot and reload)" || ko "reload"

echo "== automatic revert"
nft delete table inet nfhotel
bash "$APPLY" --apply --timeout 10 >/dev/null
nft list table inet nfhotel >/dev/null 2>&1 && ok "loaded before the timer ends" || ko "not loaded"
sleep 12
nft list table inet nfhotel >/dev/null 2>&1 && ko "an unconfirmed change stayed" || ok "an unconfirmed change reverts to no table"
reach "$internet" 10.1.0.1 22 && ok "and 22 answers again" || ko "after revert"
nft -f - <<'EOF'
table inet nfhotel {
  chain marker-of-the-old-table {
  }
}
EOF
bash "$APPLY" --apply --timeout 10 >/dev/null
sleep 12
nft list table inet nfhotel 2>/dev/null | grep -q marker-of-the-old-table && ok "it reverts to the table before" || ko "revert to old"
bash "$APPLY" --apply --timeout 60 >/dev/null && bash "$APPLY" --revert >/dev/null &&
  nft list table inet nfhotel 2>/dev/null | grep -q marker-of-the-old-table && ok "--revert puts the old table back at once" || ko "--revert"
bash "$APPLY" --apply --timeout 5 >/dev/null 2>&1 && ko "accepted a 5 second timeout" || ok "refuses a timeout under 10 seconds"

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
