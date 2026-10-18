#!/usr/bin/env bash
# Shared helpers for the release and setup scripts in scripts/ (MIL-021). Source it;
# do not run it. The commands installed on the VM as standalone copies
# (deploy-release.sh as nfhotel-deploy, backup-database.sh as nfhotel-backup) keep
# their own copies of these two functions, because nothing is installed beside them.
#
# Usage: . "$(dirname "${BASH_SOURCE[0]}")/lib/common.sh"
# Needs: bash 4+, sed

# die <message>: print an error to standard error and exit 1.
die() {
  echo "error: $*" >&2
  exit 1
}

# usage: print the header comment of the calling script, from line 2 to `set -euo`.
usage() {
  sed -n '2,/^set -euo/p' "${BASH_SOURCE[1]}" | sed '$d' | sed 's/^# \{0,1\}//'
}
