#!/usr/bin/env bash
# Make a self-signed certificate and key for the nginx proxy (MIL-017, ADR-0006), for a
# development host. For a real host name put a certificate from a public authority in the
# same two files instead; the nginx configuration does not change.
#
# Usage: make-dev-certificate.sh <directory> [name-or-address...]
#   directory   where fullchain.pem and privkey.pem are written (created if missing)
#   names       extra host names or IP addresses for the subject alternative names;
#               localhost, 127.0.0.1 and ::1 are always included
# An existing pair is never overwritten. The key is readable by its owner only.
# Exit:  0 the pair exists; 1 openssl failed; 2 usage error
# Needs: bash 4+, openssl
set -euo pipefail
export MSYS_NO_PATHCONV=1 # Git Bash would turn the /CN=... subject into a path

die() {
  echo "error: $*" >&2
  exit "${2:-1}"
}

(($# >= 1)) || die "usage: $0 <directory> [name-or-address...]" 2
dir="$1"
shift

if [[ -f "$dir/fullchain.pem" && -f "$dir/privkey.pem" ]]; then
  echo "certificate exists in $dir; not changed"
  exit 0
fi

san="DNS:localhost,IP:127.0.0.1,IP:::1"
for name in "$@"; do
  [[ -n "$name" && "$name" != "_" ]] || continue
  if [[ "$name" =~ ^[0-9.]+$ || "$name" == *:* ]]; then
    san+=",IP:$name"
  else
    san+=",DNS:$name"
  fi
done

mkdir -p "$dir"
# A native Windows openssl needs a Windows path; elsewhere cygpath does not exist.
native="$(cygpath -m "$dir" 2>/dev/null || echo "$dir")"
umask 077
openssl req -x509 -newkey rsa:2048 -sha256 -days 365 -nodes \
  -keyout "$native/privkey.pem" -out "$native/fullchain.pem" \
  -subj "/CN=nfhotel-dev" -addext "subjectAltName=$san" >/dev/null 2>&1 ||
  die "openssl could not make the certificate"
# The proxy container reads the files as another user; the folder is mounted read-only.
chmod 644 "$dir/fullchain.pem" "$dir/privkey.pem"
echo "self-signed certificate written to $dir (valid 365 days; names: $san)"
