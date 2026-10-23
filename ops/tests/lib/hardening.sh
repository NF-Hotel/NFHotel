#!/usr/bin/env bash
# The container hardening of MIL-023 task 2 (CIS-001, MIL-023 criterion 3), checked with
# docker inspect, for tests/check-e2e-stack.sh and tests/test-optional-services.sh. Source it;
# do not run it.
#
# Usage: . "$ROOT/tests/lib/hardening.sh"; missing="$(hardening_missing <container>)"
# Needs: docker, awk

# hardening_missing <container>: print the hardening settings the container lacks, separated
# by spaces; print nothing when it has them all: cap_drop ALL, no-new-privileges, a read-only
# root filesystem, memory, CPU and PID limits, and not privileged.
hardening_missing() {
  docker inspect --format '{{json .HostConfig.CapDrop}}|{{json .HostConfig.SecurityOpt}}|{{.HostConfig.ReadonlyRootfs}}|{{.HostConfig.Memory}}|{{.HostConfig.NanoCpus}}|{{.HostConfig.PidsLimit}}|{{.HostConfig.Privileged}}' "$1" |
    awk -F'|' '{
      m = ""
      if ($1 !~ /"ALL"/) m = m " cap_drop"
      if ($2 !~ /"no-new-privileges(:true)?"/) m = m " no-new-privileges"
      if ($3 != "true") m = m " read_only"
      if ($4 + 0 <= 0) m = m " mem_limit"
      if ($5 + 0 <= 0) m = m " cpus"
      if ($6 + 0 <= 0) m = m " pids_limit"
      if ($7 != "false") m = m " privileged"
      sub(/^ /, "", m)
      print m
    }'
}
