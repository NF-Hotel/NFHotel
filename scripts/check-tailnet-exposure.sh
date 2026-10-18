#!/usr/bin/env bash
# Check, from a device on the tailnet, which ports of the NF Hotel VM answer (ADR-0011
# decision 6, MIL-024 task 7). Read-only: a TCP connect per port and a PostgreSQL
# SSLRequest on 5432, no login. Run it from an administrator device and from a device
# that is not an administrator; together they show what the tailnet policy and the
# firewall of the VM (nftables) allow.
#
# Expected on every device: 22, 80 and 443 open; the internal ports (8080, 8081, 2718,
# 2719, 8000, 11434) closed. Port 5432 open only with --admin, closed otherwise.
#
# Usage: check-tailnet-exposure.sh [--host <name or IP>] [--admin]
#   --host   the VM on the tailnet (default nf-hotel)
#   --admin  this device is in group:nfhotel-admins, so 5432 may answer
# Exit:  0 every port is as expected; 1 a port is not; 2 usage error
# Needs: bash 4+ (with /dev/tcp), timeout, od

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=scripts/lib/common.sh
. "$ROOT/scripts/lib/common.sh"
readonly OPEN_PORTS=(22 80 443)
readonly CLOSED_PORTS=(8080 8081 2718 2719 8000 11434)

host=nf-hotel
admin=0
failures=0

while (($#)); do
  case "$1" in
    --host)
      host="${2:?--host needs a value}"
      shift
      ;;
    --admin) admin=1 ;;
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
[[ "$host" =~ ^[A-Za-z0-9.:-]+$ ]] || die "the host '$host' has characters that are not allowed"

is_open() {
  timeout 4 bash -c "exec 3<>/dev/tcp/$host/$1" 2>/dev/null
}

expect() {
  local port="$1" want="$2" got=closed
  is_open "$port" && got=open
  if [[ "$got" == "$want" ]]; then
    echo "PASS  $host:$port is $got"
  else
    echo "FAIL  $host:$port is $got, expected $want"
    failures=$((failures + 1))
  fi
}

for port in "${OPEN_PORTS[@]}"; do expect "$port" open; done
for port in "${CLOSED_PORTS[@]}"; do expect "$port" closed; done

if ((admin)); then
  expect 5432 open
else
  expect 5432 closed
fi
if is_open 5432; then
  # SSLRequest: PostgreSQL answers S (TLS offered) or N (no TLS); no credential is sent.
  answer="$(timeout 4 bash -c "exec 3<>/dev/tcp/$host/5432; printf '\x00\x00\x00\x08\x04\xd2\x16\x2f' >&3; head -c 1 <&3" 2>/dev/null | od -An -c | tr -d ' ')" || answer=""
  echo "INFO  5432 answers the PostgreSQL SSLRequest with '${answer:-nothing}' (N = no TLS)"
fi

((failures == 0)) || die "$failures port(s) not as expected"
echo "==> every port is as expected"
