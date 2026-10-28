# RC-163 Review of the code that finishes MIL-021 (off-VM copy, rollback drill, tailnet scope)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-163 |
| CrossReference | [MIL-021], [ADR-0007], [ADR-0008], [DSA-001], [QC-SH-001], [QC-WF-001], [QC-YML-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-21 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [7853a62] |

---

## Artifact Under Review

- **Instances reviewed (MIL-021 tasks 7 and 8):**
  - `ops/scripts/backup-database.sh`: the copy sends every backup the target lacks, takes `BACKUP_SSH_PORT`, and on failure warns with the time of the last copy, without failing the backup;
  - `ops/deploy/backup-target/` (new): the Dockerfile, the entrypoint, the compose file and the settings example of the write-only target;
  - `deploy --drill` in `ops/scripts/deploy-release.sh`, the input `rollback_drill` of `cd.yaml` and of the deploy action, and its use in `cd-steps.sh deploy`;
  - `cd-steps.sh scope` and the step "Check the tailnet scope of this node";
  - the tests `test-backup-target.sh` (new, in Ops CI), `test-deploy-release.sh` and `test-cd-steps.sh`;
  - the root `.gitignore` exception for `ops/deploy/backup-target/`, which the Visual Studio pattern `Backup*/` hid.
- **Checklists used:** [QC-SH-001], [QC-WF-001], [QC-YML-001].
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`). S01 accepted the plan for MIL-021 in autonomous mode in chat on 2026-10-21, with the legion laptop as backup host and a rollback drill in production.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| SH 1–4, 12 | Header, quoting, names, shellcheck, shfmt | Pass | `ops/tests/lint.sh`: 0 findings. Its first run found SC1087, SC2181 and one shfmt layout, all fixed |
| SH 5 | Errors and usage | Pass | The entrypoint refuses a key that is not one ed25519 key. `backup-database.sh` refuses a port that is not a number. `deploy` refuses an eighth argument other than `--drill` (exit 2) |
| SH 7 | No secret | Pass | The target holds only public keys. The backups are `age`-encrypted on the VM, so the laptop cannot read them |
| SH 8 | Explicit change | Pass | The drill runs only from a manual CD run with `apply` and `rollback_drill`, behind the `production` approval |
| SH 9 | Header | Pass | Each new or changed script states its steps, settings and exit codes |
| SH 11 | Behaviour tested | Pass | `test-backup-target.sh`, 15 checks: refused keys, a host key that stays the same after a restart, write-only (no read back, no shell, no root), the copy, a target that is off (a warning, and the backup is kept), the catch-up of all missing backups, and only backup files copied. `test-deploy-release.sh`, 56 checks: the drill rolls back, is not recorded and fails the run. `test-cd-steps.sh`, 63 checks: `--drill` only when asked; the scope records closed ports, warns on an open port or another peer, and fails when ssh does not answer |
| SH 13 | Safe to re-run | Pass | `--ignore-existing` on a write-only target; a second copy sends nothing |
| WF 4, 7, 12 | Header, permissions, changes outside the repository | Pass | `cd.yaml` documents the drill. Permissions are unchanged, and the drill needs the same approval |
| WF 6, YML 1–3 | actionlint, yamllint | Pass | 0 findings |
| WF 22 | Verified on the instance | Not yet | The drill run and the scope step run on GitHub with the first CD after the merge (MIL-021 task 8) |

The real image of the target was built and run on the legion laptop. A file was written with the key, and a shell was refused (`rrsync error: SSH_ORIGINAL_COMMAND does not run rsync`).

## Findings

- **rrsync needs python3** on Debian 12. The first test run showed it: `/usr/bin/env: 'python3': No such file or directory`. The image and the test install it.
- **A laptop as backup host is off at times.** The copy is therefore a warning, not a failure, so a deploy (whose first step is a backup) is never blocked by the laptop. A long gap shows in every backup log ("the last copy off the VM was at …"). There is no alert: monitoring is outside MIL-021.
- **The scope check warns and does not fail.** A change of the tailnet policy is then visible in the run summary without stopping a release. Port 22 must still answer, or the deploy fails as before.

## Overall Verdict

Go. QC-WF-001 criterion 22 closes with the runs of MIL-021 task 8.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Set up the target on the laptop and the settings on the VM (wiki page CI-CD, one-time setup step 5) | S03 | After the merge |
| Run the rollback drill and record the run links in NFHotel#125 | S01 | After the merge |

---

[MIL-021]: ../../milestones/mil-021-secure-cicd-foundation.md
[ADR-0007]: ../../adr/adr-0007-cicd-and-supply-chain.md
[ADR-0008]: ../../adr/adr-0008-database-migration-and-backup.md
[DSA-001]: ../devsecops-assessment.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[7853a62]: https://github.com/NF-Hotel/NFHotel/commit/7853a620ea10e270620236ac74fea5f77cd29ac2
