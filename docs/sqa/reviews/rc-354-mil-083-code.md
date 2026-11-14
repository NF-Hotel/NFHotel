# RC-354 Code review of MIL-083: Docker's packages and Debian's security updates in the monitoring LXC, and the floor of runc

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-354 |
| CrossReference | [MIL-083], [ADR-0043], [RC-352], [RC-353], [CIS-001], [MIL-080], [MIL-082], [RC-350], [QC-SH-001], [QC-YML-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-14 | Accepted | mikemike | AI as stand-in for S01 | Initial version; mode autonomous, not independent; Go-with-conditions: every check of the pull request green | [5d447a4] |

---

## Artifact Under Review

- Instance reviewed: the code and the records of [MIL-083] tasks 4 to 6 on the branch `mil-083-monitoring-lxc-patched`:
  - task 4: `ops/scripts/create-monitoring-lxc.sh` and its test `ops/tests/test-create-monitoring-lxc.sh`, the header
    of `ops/scripts/install-patching.sh` and `ops/deploy/host/README.md`;
  - task 5: the floor of runc in `ops/scripts/check-container-runtime.sh` with its unit test
    `ops/tests/test-check-container-runtime.sh`, the rule `ContainerRuntimeKnownEscape` in
    `ops/deploy/monitoring/prometheus/rules/security.yml` with its test in `rules-test.yml`, and its row on the wiki
    page Alerts;
  - task 6: the wiki pages Container-Escape, Monitoring and Host-Hardening, and [CIS-001] rows 1.2.2 and 1.2.2.1.
- Checklist used: [QC-SH-001] (`qc-programming-shell`), [QC-YML-001] (`qc-yaml`). No workflow changed, so
  `qc-workflow` does not apply. The wiki pages and the rows of [CIS-001] have no checklist of their own; they are read
  against the deliverable of [MIL-083] below.
- Scope: full review of the new and changed files
- Language and domain: n/a (technical type)
- Language reviewer: none
- Mode: autonomous, not independent (the reviewer is AI as stand-in for S01, who accepted the plan of [MIL-083] in chat
  on 2026-11-14, with the merge and the approval of its CD run)

## Checklist Results

### QC-SH-001: `check-container-runtime.sh`, `test-check-container-runtime.sh`, `create-monitoring-lxc.sh`, `test-create-monitoring-lxc.sh` and the header of `install-patching.sh`

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | `#!/usr/bin/env bash` and `set -euo pipefail`, or a comment explains the exception | Pass | The scripts keep both; the tests keep the convention of `ops/tests`, `set -uo pipefail` with each assertion counted |
| 2 | Expansions quoted, lists as arrays, `[[ ]]` and `$(...)` | Pass | The floor is the array `RUNC_FLOOR`, read as `"${RUNC_FLOOR[@]}"`; the version is matched with `[[ =~ ]]` and its numbers read in base 10 (`10#`), so a `08` is not octal; shellcheck clean |
| 3 | Naming | Pass | snake_case functions (`runc_floor`, `with_runc`) and variables (`floor_state`, `runc_below`, `runtime_failed`, `vm_docker`); `RUNC_FLOOR` and `RUNC_ESCAPES` in UPPER_SNAKE, set once and read-only |
| 4 | shellcheck and `bash -n`, every `disable` explained | Pass | shellcheck v0.11.0 `-x` over the five files: clean; no new `disable` |
| 5 | Errors on standard error with `error:`, a usage line for bad arguments | Pass | A runc below its floor is a `FAIL` line of the report, or a `WARN` line with `--deploy`, as every rule of the check is, followed by what to install and the wiki page that says how; the usage errors are unchanged (`die`, exit 2) |
| 6 | `mktemp` and `trap ... EXIT`, no fixed `/tmp` names | Pass | The tests work in their `mktemp -d` folder; the scripts write no temporary file |
| 7 | No secret in the script, its output or a command line | Pass | The LXC script writes the public key of Docker's repository and its list; the report of the check holds versions only |
| 8 | A change of state outside its folder needs an explicit flag | Pass | `create-monitoring-lxc.sh` and `install-patching.sh` print what they would do and change nothing without `--apply`; the check only reads |
| 9 | Header: purpose, usage, options, environment, exit codes | Pass | The header of the check names the floor, what `--deploy` and `--metrics` do with it and when it exits 1; the LXC script names its three steps and what is left to S03; `install-patching.sh` names the LXC and its checkout |
| 10 | Implements a cited task; deviations recorded | Pass | Each commit names [MIL-083] and its task (`Task: MIL-083#N`); F1 and F2 below record what differs from the tasks |
| 11 | Success, failure and bypass paths tested | Pass | The unit test passes 119 of 119 in `debian:13-slim`: five versions below a floor fail (Debian's `1.1.15+ds1`, a release candidate, `0.9.9`, and `1.2.7` and `1.3.2` next to their fixes), ten at or above it pass (`1.2.10` and `1.10.0` compared as numbers, a leading `v`, the VM's `1.5.2` and the LXC's `1.5.3`), four versions that cannot be read warn, `--deploy` warns and exits 0, a broken container still fails, and the report says 1, 0 or nothing. Its new checks failed 23 times on the check before the floor. The test of the LXC script passes 15 of 15; its six new checks failed on the script before task 4, and a line that installs Debian's `docker-buildx` is caught while Docker's `docker-buildx-plugin` is not |
| 12 | Formatted with `shfmt` | Pass | shfmt v3.14.1 `-i 2 -ci`: no difference |
| 13 | Safe to re-run | Pass | The check only reads; `install-patching.sh` installs the same files again; the LXC script refuses an ID that a VM or a container already has |
| 14 | Bash version and tools stated; GNU-only options named | Pass | `Needs:` names bash 4; the last row of the floor is now read as `${#RUNC_FLOOR[@]} - 1`, not with the negative index of bash 4.3 (F5), and the function gives the same answers in bash 4.2 |
| 15 | Each sequence, constant, path or message defined once | Pass | The floor and its advisories are defined once in the check; the test reads the VM's Docker packages from `create-docker-vm.sh`; the key and the list of Docker's repository are written in both scripts, and the test holds the LXC's to the same lines (F6) |

### QC-YML-001: `ops/deploy/monitoring/prometheus/rules/security.yml` and `rules-test.yml`

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Parses safely, no duplicate keys | Pass | yamllint (`key-duplicates`); promtool reads both files |
| 2 | Two spaces, LF, one final newline | Pass | yamllint |
| 3 | Passes yamllint with its committed configuration | Pass | yamllint 1.35.1 `-s -c ops/.yamllint` over both files: clean |
| 4 | Scalars YAML would coerce are quoted | Pass | The series values `"0 0 1 1"` and `"1x3"`, and the summary |
| 5 | Plain types only | Pass | |
| 6 | No secret | Pass | |
| 7 | Embedded program text of five lines at most | Pass | The expression is one line |
| 8 | A repeated block defined once | Pass | The rule sits with the other rules of the runtime check, in their form |
| 9 | Anchors only where supported | N-A | None |
| 10 | Validates against the schema of its tool | Pass | `ops/tests/check-monitoring.sh`: `promtool check config`, `promtool test rules`, `amtool check-config`, `loki -verify-config`, `blackbox_exporter --config.check`, `alloy fmt` and the dashboards pass with the pinned images |
| 11 | A header comment states purpose, tool and decision | Pass | The files keep their headers; the comment of the rule names the floor, [MIL-083] and ADR-0043 |
| 12 | Key names follow the tool, one case style | Pass | |
| 13 | Block scalars for multi-line strings | Pass | The description is a folded block scalar (`>-`) |
| 14 | Lowercase kebab-case file names | Pass | Unchanged names |
| 15 | A check runs without the real target | Pass | The test of the rule feeds its series to promtool: empty at 1 minute, firing at 2, and silent for another host |

### The wiki pages and CIS-001, against the deliverable of [MIL-083]

- **Container-Escape** says that both hosts take Docker Engine, containerd and runc from Docker's repository, what the
  floor is and when a new escape goes into it, that the LXC is updated after a snapshot by S03 on a node with Proxmox VE
  9.1.1 or later, and that the LXC has run runc 1.5.3 since 2026-11-14 while the VM gets it in the monthly window.
- **Monitoring** says what step B4 now installs, and its new section updates Docker in the LXC with a snapshot and its
  rollback; **Host-Hardening** has the LXC in its patching row; **Alerts** has the row of `ContainerRuntimeKnownEscape`.
- [CIS-001] rows 1.2.2 and 1.2.2.1 add the LXC, with the evidence of task 3 below. `test-wiki-coverage.sh` passes 116 of
  116.

## The criteria of MIL-083 that this review can see

| # | Status | Evidence |
| --- | --- | --- |
| 1 | Pass | ADR-0043 is Accepted with [RC-353] Go |
| 2 | Pass | In the LXC on 2026-11-14, `dpkg-query`: `docker-ce` and `docker-ce-cli` 5:29.9.3-1~debian.13~trixie, `containerd.io` 2.4.2-4~debian.13~trixie, `docker-compose-plugin` 5.7.1-1, `docker-buildx-plugin` 0.39.0-1 and `docker-ce-rootless-extras`, as on the VM; no `docker.io`, `docker-cli`, `containerd`, `runc`, `docker-compose` or `docker-buildx` of Debian installed. `apt-cache policy` names `https://download.docker.com/linux/debian trixie/stable`, signed with the key `9DC8 5822 9FC7 DD38 854A E2D8 8D81 803C 0EBF CD88`, which was compared before the list was written. `runc --version`: 1.5.3, a line above the last row of the floor |
| 3 | Pass | `deploy-monitoring.sh --apply` exited 0 at 07:34 UTC: the six containers started, the check passed all six and printed `Docker Engine 29.9.3, containerd v2.4.2, runc 1.5.3; AppArmor off, seccomp on`; the storage driver stayed `overlay2` and the four volumes are there. Prometheus answered `count(up)` = 10 for 07:23:15 UTC, before the change, and its oldest sample is the same as before; at 07:34:54 UTC 10 of 10 targets were up, the VM's jobs `site`, `public_site`, `redirect`, `integrations/unix` and `scans` among them, and no alert was firing or pending |
| 4 | Pending | `unattended-upgrades` 2.12 is installed by `install-patching.sh --apply`: `apt-config dump` shows the Debian security origin only and `Automatic-Reboot "false"`, and the check of the script passed. The unit of the daily timer, `apt-daily-upgrade.service`, started by hand at 07:37 UTC, installed the 31 security updates that were waiting ("All upgrades installed"), with no reboot needed and no container restarted; the timer's own first run is due on 2026-11-15 at 06:35 UTC (F3) |
| 5 | Pass | The unit test and the test of the LXC script above |
| 6 | Pending | The wiki pages and [CIS-001] above; `test-wiki-coverage.sh` 116 of 116; this review; the linters of `lint.sh` at its versions are clean (F7); the checks of the pull request |

## Findings of the review

- **F1, deviation from task 3: two more Debian packages.** Debian's `docker-buildx` 0.13.1, which came with `docker.io`,
  owns the plugin file that Docker's `docker-buildx-plugin` installs. dpkg refused that one package and left the others
  unpacked; `docker-buildx` has no reverse dependency, so it was removed with `dpkg -r`, the install was finished, and
  `docker-buildx-plugin` was installed after it, as the VM has it. The eight packages that came with `docker.io` and
  were no longer needed (`criu` with its libraries and Python modules, and `tini`) were removed too, so the LXC has the
  Docker packages of the VM; `criu` is what a checkpoint and restore of a container needs. The test of the LXC script
  now names `docker-buildx` among the Debian packages it refuses.
- **F2, deviation from task 2: the snapshot was taken by the AI.** S01 said in chat on 2026-11-14 that the AI should
  take it itself, in the Proxmox web interface in a session S01 signed in to. The AI read the version through the
  interface (Proxmox VE 9.2.11, so the requirement of ADR-0043 holds), stopped the monitoring stack so that its volumes
  were not written, and took the snapshot `before-mil-083` of LXC 102 at 07:28 UTC: the task ended `TASK OK` and the
  snapshot is the parent of the current state. No rollback was needed.
- **F3, note: the first run of `unattended-upgrades` was started by hand.** The timer had run at 06:25 UTC, before the
  package was installed, so its next run is on 2026-11-15. The unit was started once, so the 31 waiting updates
  (OpenSSL, zlib, liblzma, util-linux, apt and others) did not wait a day. The gate reads the timer's own run.
- **F4, note: the monitoring was down for eleven minutes.** The stack was stopped at 07:23 UTC and was running again at
  07:34 UTC, so nothing was scraped and no alert could fire in that time; Prometheus has that gap. The VM's own checks
  and timers ran on.
- **F5, fixed: a negative array index.** `${RUNC_FLOOR[-1]}` needs bash 4.3, and the header names bash 4. The function
  now reads the last row by its length; the unit test passes 119 of 119, and the function gives the same five answers
  in a `bash:4.2` container.
- **F6, note: the repository lines are written twice.** The VM gets Docker's key and list through cloud-init in
  `create-docker-vm.sh`, the LXC through `pct exec` in `create-monitoring-lxc.sh`. A shared function would not serve
  both forms; the test reads the VM's package list from its script and requires the same key and list lines in the
  LXC's.
- **F7, note: the linters ran in WSL.** As F7 of [RC-350]: shellcheck v0.11.0, shfmt v3.14.1, yamllint 1.35.1 and the
  pinned images of `check-monitoring.sh` ran over the changed files in WSL. The job Ops linters runs `lint.sh` itself.
- **F8, note: the floor is kept by hand, and the deploy of the VM warns.** As [RC-353] says: a new escape goes into
  `RUNC_FLOOR` with its unit test, and the deploy of the VM only warns, because neither a release nor its rollback
  changes the runtime; the timer's check fails and `ContainerRuntimeKnownEscape` fires.
- **F9, note: the snapshot and the thin pool of the node.** LVM warned, when it made the snapshot, that the thin
  volumes of the node's pool add up to more than the pool and its volume group can hold (407 GiB against 338 GiB; 41 %
  used) and that the pool does not extend itself. The snapshot keeps the old blocks of every write of Prometheus and
  Loki, so it should be removed once the gate of [MIL-083] is Go.

## Overall Verdict

Go-with-conditions. Every applicable criterion of [QC-SH-001] and [QC-YML-001] passes. The conditions: every check of
the pull request is green before the merge; the gate reads the floor's line in the deploy of the merge on the VM and in
the LXC, and the first run of the daily timer in the LXC. The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Merge only with every check of the pull request green | S01 (the AI as stand-in) | At the merge |
| Approve the CD run of the merge and read the floor's line for runc 1.5.2 in the deploy of the VM; deploy the monitoring from `main` and read it for runc 1.5.3 in the LXC | S01 (the AI as stand-in) | 2026-11-14 |
| Read the first run of the daily timer in `/var/log/unattended-upgrades/unattended-upgrades.log` of the LXC | S01 (the AI as stand-in) | 2026-11-15 |
| Remove the snapshot `before-mil-083` of LXC 102 once the gate of [MIL-083] is Go | S03 (held by S01) | After the gate |
| Install runc 1.5.3 on `nf-hotel` with the packages of Docker's repository; the LXC has it since 2026-11-14 ([RC-350]) | S01, S03 for the reboot | The next monthly window |

---

[MIL-083]: ../../milestones/mil-083-the-monitoring-lxc-patched-as-the-vm-is.md
[ADR-0043]: ../../adr/adr-0043-docker-packages-and-security-updates-in-the-monitoring-lxc.md
[RC-352]: ./rc-352-mil-083.md
[RC-353]: ./rc-353-adr-0043.md
[CIS-001]: ../cis-checklist.md
[MIL-080]: ../../milestones/mil-080-every-container-runs-with-the-rights-of-its-profile.md
[MIL-082]: ../../milestones/mil-082-a-compromised-container-kept-away-from-the-host.md
[RC-350]: ./rc-350-mil-082-code.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[5d447a4]: https://github.com/NF-Hotel/NFHotel/commit/5d447a4df5d42d92ba91228dc0027e78e09fa317
