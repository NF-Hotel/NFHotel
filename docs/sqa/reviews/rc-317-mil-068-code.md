# RC-317 Code review of MIL-068: the script that removes the desktop

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-317 |
| CrossReference | [MIL-068], [RC-314], [CIS-001], [MIL-023], [MIL-066], [RC-313], [QC-SH-001], [QC-YML-001], [QC-WF-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-08 | Accepted | mikemike | AI as stand-in for S01 | Initial version; mode autonomous, not independent; the review of tasks 1 to 3 of MIL-068 before their pull request (task 4); Go | pending |

---

## Artifact Under Review

- **Instances reviewed** (the branch `mil-068-remove-desktop` against `main` at 53e0341, the merge of #602):
  - shell: the new `ops/scripts/remove-desktop.sh` (task 2) with the two lists of task 1, and the new `ops/tests/test-remove-desktop.sh` (task 2);
  - workflow: `.github/workflows/ops-ci.yaml`, the test in the matrix of the script tests, in `debian:13-slim` (task 2);
  - the wiki page Host-Hardening: the section "Without a desktop", its row in "What changes", and the update command of "Reboots", which now names Docker's five packages and Tailscale (task 3).
- **Not in this record:** rows 2.1.x and 2.2.x of [CIS-001] become Applied with the evidence of task 5, as task 3 says. Criteria 3 and 4 of [MIL-068] need the change on the VM (task 5, after S01's Proxmox snapshot and outage time), and criterion 6 includes this record and the checks of the pull request; the gate record of task 6 closes them.
- **Checklists used:** [QC-SH-001], [QC-YML-001] and [QC-WF-001].
- **Scope:** delta review of the changed files against `main`.
- **Language and domain:** n/a (technical type).
- **Language reviewer:** none.
- **Mode:** autonomous, not independent. The AI wrote the code and this record. S01 asked for the last milestones in autonomous mode in chat ("Fortsæt så med at lave de sidste milestones i automode on") and accepted the plan of MIL-068 in chat ("Ja til 1 ,2,3", [RC-314]). The reviewer is the AI as stand-in for S01, so every decision stays open to S01's challenge.

**Task 1, the read-only survey of the VM on 2026-11-08 over Tailscale SSH.** Each round marked the packages to keep as installed by hand in a copy of the apt state file only (`-o Dir::State::extended_states=<copy>`) and simulated the purge (`apt-get -s purge --auto-remove`); the checksum of `/var/lib/apt/extended_states` was the same before, during and after each round.

- Round 1 (19:06 UTC): the two tasks, GNOME, `gdm3`, LibreOffice and the services of finding 2 of [MIL-068] would take 643 packages, 1,660 MB, and none of the packages to keep. Without the marks, the same purge takes `jq`, `network-manager` and `sudo` along (finding 1). Firefox, GNOME Terminal, `xterm`, PipeWire, Plymouth, `xdg-desktop-portal` and the X11 utilities would stay, and so would the enabled service `low-memory-monitor`.
- Round 2 (19:10 UTC), with those added to the list: 1,083 packages, 3,325 MB, none of the packages to keep and no package of priority `required` or `important`; 519 packages stay. Every enabled unit of finding 2 of [MIL-068] goes with its package, and so do `cups.path`, `fwupd-refresh.timer` and `low-memory-monitor.service`. Of the programs that listen today, `avahi-daemon` and `cupsd` go, and `sshd`, `tailscaled` and `dockerd` stay; the ports of Docker include 5432, the database relay of the team on the tailnet address (`ops/deploy/postgres-tailnet`). The purge would also take `locales-all`.
- Round 3 (19:13 UTC): the locale of the VM, `en_US.UTF-8`, comes from `locales-all` only (`localedef --list-archive` lists nothing), so `locales-all` joined the packages to keep. `xterm` stayed in round 2 although `xorg` and `xinit`, which depend on it, go, so it joined the list. NetworkManager manages `ens18` ("Wired connection 1").
- The dry run of the script itself on the VM (19:26 UTC, `ssh root@nf-hotel 'bash -s' <ops/scripts/remove-desktop.sh`): the 52 names of the list are installed, and the 28 packages to keep; it would purge 1,092 packages, 3,098 MB, with 0 newly installed; no package to keep would go. The checksums of the marks and of the package states, and the default target (`graphical.target`), were the same before and after. Against round 2: `xterm` and the 9 packages that only it needs more, `locales-all` less.

**Tools and runs of 2026-11-08, on the PC of S01 (WSL 2, Docker 29.7.2):**

- `ops/tests/lint.sh` (layout, actionlint 1.7.7, shellcheck v0.11.0, shfmt v3.14.1, yamllint 1.35.1): clean.
- `test-remove-desktop.sh` in `debian:13-slim`, as Ops CI runs it: 21 passed, 0 failed. Written before the script: against no script, the 4 checks of the stand-ins passed and 15 failed.
- The mutation run: 18 copies of `remove-desktop.sh`, each without one rule, against the test in `debian:13-slim`. The script passes, and 16 copies fail at least one check:
  - no mark in the copy (the dry run then refuses, because `sudo` would go), and a refusal that reads the wrong column of `comm`: 9 checks each, from the dry run on;
  - an empty copy of the state file (every package then counts as installed by hand): `--apply` purges fewer packages than the dry run listed;
  - no refusal on the copy: `--apply` marks for real before the refusal on the real state stops it, so the refusal leaves a changed state: 4 checks;
  - no real mark: `--apply` refuses on the real state, 7 checks;
  - a purge without `--auto-remove`: the dependencies that only the list needed stay;
  - `remove` in place of `purge`: the configuration files of `netbase` stay;
  - no default target, at the purge or when nothing is left to purge: the target checks;
  - no root check: the check of root;
  - no check of what the purge left: the purge that removes nothing;
  - a dry run that goes on to purge: 3 checks, the dry run, its state check and the run from standard input;
  - no end of the run when the simulation fails: the failing simulation;
  - exit 1 for an unknown option: the usage check;
  - the purge read from `Remv` lines only: 6 checks, the refusal among them;
  - no end of the run when nothing is left to purge: the second `--apply`.
  The 2 copies that pass take out the repeated guards of finding 4.
- `test-wiki-coverage.sh`: 112 passed.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| SH 1 | Header line and `set` options | Pass | `remove-desktop.sh` starts with `#!/usr/bin/env bash` and `set -euo pipefail`; the test keeps `set -uo pipefail`, as the other tests of `ops/tests`, so that every check runs and is counted |
| SH 2 | Quoting, arrays, `[[ ]]` | Pass | Every expansion is quoted; the lists, the installed packages and the options of apt are arrays (`KEEP_PACKAGES`, `REMOVE_PACKAGES`, `LOCK_WAIT`, `remove`, `keep`, `options`), filled with `mapfile` from `dpkg-query`, never by word splitting; the test reads the list of the dry run with `read -ra` |
| SH 3 | Names | Pass | Both files are kebab-case; `snake_case` functions (`die`, `usage`, `installed`, `simulate`, `refuse_taken`, `report`, `set_default_target`; in the test `ok`, `ko`, `make_package`, `state`, `listed`) and variables; `UPPER_SNAKE` constants (`KEEP_PACKAGES`, `REMOVE_PACKAGES`, `STATE_FILE`, `LOCK_WAIT`, `SCRIPT`) |
| SH 4, 12 | shellcheck, `bash -n`, shfmt | Pass | `lint.sh` clean. The test adds one `disable`, SC2154 for the arrays that the `eval` of the lists defines, with its reason on the line, beside the SC2015 that every test of `ops/tests` has; finding 6 |
| SH 5 | Errors | Pass | `die` writes `error: …` to standard error and exits 1. A refusal names the packages to keep that would go; a simulation that fails shows the output of apt and ends the run before anything is listed; a purge that leaves a package of the list names it and leaves the default target; `--apply` without root says "run it as root"; an unknown option prints the usage and exits 2. All five are tested |
| SH 6 | Temporary files | Pass | One `mktemp -d` folder, removed by `trap … EXIT`, holds the copy of the state file and the output of the simulations; no fixed name in `/tmp`. The test works in its own `mktemp -d` folder in a throwaway container |
| SH 7 | Secrets | Pass | No secret; the output names packages, sizes and the host |
| SH 8 | State outside its folder | Pass | It changes the packages of the VM, so the dry run is the default and `--apply` purges, as the header says. The dry run marks only a copy of the state file: the packages, the marks and the target were the same after it, in the test and on the VM |
| SH 9 | Header | Pass | Purpose, the three steps, why the mark is needed, how to run it over SSH, the usage, the option, the exit codes and the needs; it reads no environment variable. The header of the test names its stand-ins and its checks |
| SH 10 | Implements the task it cites | Pass | Tasks 1 and 2 of MIL-068, and the wiki part of task 3; the commit has its `Task:` trailers. Deviations, in the findings: the lists are longer than the examples of the deliverable (finding 2); the rows of CIS-001 of task 3 wait for task 5 (finding 7) |
| SH 11 | Tested for success, failure and the bypass path | Pass | 21 checks in `debian:13-slim`: the refusal in the dry run and in `--apply`; the trap of the VM reproduced; the dry run lists, changes nothing, and runs from standard input; `--apply` purges with the configuration files and the dependencies that only the list needed, keeps `sudo` and marks it, sets the target, and a second run finds nothing; a purge that leaves a package; a simulation that fails; root; usage; the lists against MIL-068. The dry run is the path without `--apply`. 16 of 18 mutants fail (finding 4) |
| SH 13 | Safe to re-run | Pass | A second `--apply` finds nothing to purge and sets the default target again (tested); a package that is already installed by hand stays so |
| SH 14 | Needs stated | Pass | "bash 4.4+, apt-get, apt-mark and dpkg-query; root and systemctl for --apply"; `comm`, `paste`, `fmt` and `cmp` come from Debian's essential packages, and `DPkg::Lock::Timeout` from apt of Debian 11 and later. The test names `dpkg-deb`, `setpriv` and the Debian archive |
| SH 15 | Knowledge once | Pass | Each list is in the script once; the test reads both from it (`sed` and `eval`) instead of a copy. One function each for the installed packages, the simulation, the refusal and the report, used by the dry run and by `--apply`. `die` and `usage` are copies of `scripts/lib/common.sh`, as `deploy-release.sh` and `backup-database.sh` keep theirs, because the script also runs from standard input with no file beside it; the comment says so |
| YML 1 | Parses, no duplicate key | Pass | actionlint 1.7.7 and yamllint 1.35.1 read the file; no duplicate key |
| YML 2 | Indentation, LF, end of file | Pass | 2 spaces, LF, one newline at the end, no trailing space (yamllint) |
| YML 3 | yamllint | Pass | yamllint 1.35.1 with `ops/.yamllint` clean; no `disable` comment |
| YML 4 | Coerced scalars | Pass | The two new values are a file name and an image name, which YAML reads as strings |
| YML 5 | Safe types | Pass | No tag |
| YML 6 | Secrets | Pass | None |
| YML 7 | Embedded program text | Pass | No new program text; the run step is unchanged |
| YML 8 | Repeated blocks | Pass | One run step for every test; the image comes from the matrix, with the default `debian:12-slim` in one place |
| YML 9 | Anchors | N-A | None |
| YML 10 | Schema | Pass | actionlint 1.7.7 checks the workflow against the schema of GitHub Actions |
| YML 11 | Header comment | Pass | The comment over `include` now says why both entries run Debian 13 |
| YML 12 | Key names | Pass | The keys of GitHub Actions, unchanged |
| YML 13 | Block scalars | N-A | No multi-line string added |
| YML 14 | File name | Pass | Unchanged |
| YML 15 | A check without the real target | Pass | actionlint on the file, and the job runs on the pull request |
| WF 1 | Meets QC-YML-001 | Pass | The YML rows above |
| WF 2, 3, 4 | Place, keys, header | Pass | Unchanged: `.github/workflows/`, `name`, `on` and `jobs`, and a header that names the script tests |
| WF 5 | Unique names | Pass | `Ops script tests (${{ matrix.test }})` gives the new entry its own name |
| WF 6 | actionlint | Pass | actionlint 1.7.7 clean |
| WF 7 to 13 | Permissions, actions, secrets, untrusted text, triggers, state, token | Pass | Unchanged; the new entry runs the same step in a container with the `ops` folder read-only |
| WF 14 | Runner and time limit | Pass | `runs-on: ubuntu-24.04` and `timeout-minutes: 15` of the job hold for the new entry |
| WF 15, 16 | Concurrency, filters | Pass | Unchanged |
| WF 17 | Pinned tools | Pass | The workflow installs no tool. `debian:13-slim` is a tag on purpose, as for the entry of the backup target: the test has to see the packages of Debian 13 that the VM gets |
| WF 22 | Verified on the host | Pass | The matrix entry with `image` is the form that the entry of the backup target already runs on GitHub-hosted runners; the checks of the pull request show the new one |
| MIL-068 1 | The dry run on the VM lists what it would purge, and none of the packages to keep | Pass, before task 5 | The dry run of 19:26 UTC above; the gate record keeps the list of the dry run of task 5 |
| MIL-068 2 | The script proven in `debian:13-slim` | Pass | The 21 checks of SH 11, the refusal first, and the mutation run |
| MIL-068 5 | The wiki and the CIS rows | Open | The section of Host-Hardening and the update command now; rows 2.1.x and 2.2.x of CIS-001 with the evidence of task 5 |

## Findings

1. **The trap is wider than finding 4 of [MIL-068].** Without the marks, a purge of the list takes `network-manager` and `jq` along with `sudo` (round 1). The VM would then lose the network of `ens18` (finding 5 of [MIL-068]), and the deploy its `jq`. The script marks the packages to keep in the copy before the dry run and for real before the purge, and refuses when a package to keep would still go. The test reproduces the trap with a stand-in task, whose purge with `--auto-remove` takes `sudo`, and shows that the script keeps `sudo`.
2. **The lists are longer than the examples of the deliverable.** The list to purge has 52 names: "the rest of the desktop" became the programs and services that a purge of the tasks and the named services leaves behind (round 1), among them Firefox, GNOME Terminal, `xterm`, Yelp, PipeWire, Plymouth, `xdg-desktop-portal`, the X11 utilities, the enabled service `low-memory-monitor`, `ppp` and `wpasupplicant`. The list to keep has 28 names: the 17 of the deliverable, and 11 that a script of `ops/` or a unit on the VM needs, taken from the `Needs:` lines of the scripts that run on the VM: `openssh-client` (the copy to the backup target), `git`, `logrotate`, `iproute2`, `util-linux`, `diffutils`, `procps`, `openssl` (the database roles), `apparmor` (the default profile of the containers, marked automatic on the VM), `systemd-timesyncd` and `locales-all` (round 3).
3. **The dry run on the VM agrees with the survey.** 1,092 packages, 3,098 MB, 0 newly installed, no package to keep; the difference from round 2 is the two changes of round 3. It is the evidence of criterion 1 before task 5; the gate record keeps the list of the dry run of task 5.
4. **Two guards repeat a check.** The refusal on the real state (step 4) and the check after the purge that the packages to keep are still installed can fail only when the real marks differ from the copy, or when apt purges more than it simulated. No test makes them fail without a stand-in apt that does so, and the mutation run shows them as the 2 copies that pass. The copy without the refusal on the copy shows the first of them at work: it stopped that purge. They stay, because the purge cannot be undone except with the Proxmox snapshot.
5. **What the test cannot show.** The test proves the rules with stand-in packages: the marks, the refusal, the purge with `--auto-remove`, the configuration files, the default target. It cannot show the purge of 1,092 packages of GNOME and their removal scripts, the default target under systemd, or the restart. Task 5 shows them on the VM, after the snapshot.
6. **A lint hint that would have hidden a failure.** For the check of the two purged packages, shellcheck (SC2143) proposed `! … | grep -q`. Under `pipefail`, `dpkg-query` ends with 1 when one of the two names is gone, so that form would pass while the other package is still installed. The check reads the output with `awk` instead, and no pipeline of the test ends in `grep -q`, which can end its writer early under `pipefail`.
7. **The wiki says what is not done yet.** The section "Without a desktop" says that the desktop stays until task 5 and that row 2.1.x stays Deferred until then. The gate record of task 6 changes the section and rows 2.1.x and 2.2.x of [CIS-001] with the evidence of task 5.
8. **The lock of dpkg.** `unattended-upgrades` holds the lock of dpkg while it runs (daily, 06:28 CET), so the simulations and the purge wait up to 10 minutes for it (`DPkg::Lock::Timeout=600`) instead of failing at once.

## Overall Verdict

Go. Every Mandatory criterion of QC-SH-001, QC-YML-001 and QC-WF-001 that the change touches passes, criterion 2 of MIL-068 passes in the tests, and criterion 1 has its evidence before the change. Criteria 3 and 4 need task 5; criterion 5 needs the CIS rows of task 5, and criterion 6 the checks of the pull request; the gate record of task 6 closes them. The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Merge the pull request only when every required check is green | AI as stand-in for S01 | At the merge |
| Publish MIL-068 and MIL-069 as milestones and issues on GitHub (`sync-project.sh --apply`), so that the issues of tasks 1 to 4 can be closed | S01 | Before the merge, or close them by hand after it |
| Take a Proxmox snapshot of the VM and name a time for the short outage of task 5 | S01 | 2026-11-22 |
| Run the dry run and then `--apply` in a transient unit, restart the VM, and run the checks of criteria 3 and 4 | AI as stand-in for S01 | At the time S01 names |
| Write the gate record of task 6 with the list of the dry run, rows 2.1.x and 2.2.x of CIS-001 and the section of Host-Hardening | AI as stand-in for S01 | 2026-11-22 |

---

[MIL-068]: ../../milestones/mil-068-the-vm-without-a-desktop.md
[RC-314]: ./rc-314-mil-068.md
[CIS-001]: ../cis-checklist.md
[MIL-023]: ../../milestones/mil-023-operational-quality.md
[MIL-066]: ../../milestones/mil-066-findings-of-7-november.md
[RC-313]: ./rc-313-mil-066-gate.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
