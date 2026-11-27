#!/usr/bin/env bash
# Tests of the start of the backup target on the backup host (MIL-091). start-target.js, which
# the scheduled task "\NFHotel\Backup target up" runs with Windows Script Host, runs docker
# compose up -d, leaves a service whose port answers alone, restarts the service backup-target
# once when nothing listens on its address and port while the host has the address, changes
# nothing while the host lacks the address, and ends with a code of its own for each case;
# register-start-task.ps1 delays the run at sign-in by 5 minutes. Node.js runs the script with
# stand-ins for Windows Script Host (test-start-target.js), so no Windows is needed.
#
# Run it in the Node.js image of Ops CI, from the root of the repository:
#   docker run --rm -v "$PWD/ops:/src:ro" node:26-trixie-slim bash /src/tests/test-start-target.sh
# To see which checks fail without a change, give the test older copies of the two files:
#   node ops/tests/test-start-target.js <start-target.js> <register-start-task.ps1>
# Exit:  0 every check passed; 1 a check failed
# Needs: bash, Node.js 18 or later
set -euo pipefail

exec node "$(dirname "${BASH_SOURCE[0]}")/test-start-target.js"
