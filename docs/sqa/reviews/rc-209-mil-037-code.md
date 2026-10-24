# RC-209 Review of the change of MIL-037 (recovery objectives, runbook and settings backup)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-209 |
| CrossReference | [MIL-037], [ADR-0025], [ADR-0008], [QC-SH-001], [QC-ADR-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-24 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [9119b55] |

---

## Artifact Under Review

- **Instances reviewed:** the change of tasks 1 to 3 of MIL-037 (#294 to #296), pull request #312:
  - ADR-0025 (new);
  - the settings archive in `ops/scripts/backup-database.sh`, and `ops/tests/test-backup-settings.sh` (new) in the script tests of `ops-ci.yaml`;
  - the wiki page Disaster-Recovery (new), the sidebar and Home.
- **Checklists used:** [QC-SH-001] and [QC-ADR-001].
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).

## Evidence

| Check | Result |
| --- | --- |
| Settings backup test | 6 passed: the archive is written 0600, encrypted with age, holds the settings, the certificate, `/etc/nfhotel` and the key but not the derived secret files, unpacks in place, and is pruned |
| Tests of the backup's neighbours | `test-vm-setup.sh` 19, `test-deploy-release.sh` 59 and `test-backup-target.sh` 17 passed |
| CI of the pull request | Every check green, among them the database job, which runs the real backup on a runner without a deployment (the archive is skipped with a warning there) |
| Linters | `ops/tests/lint.sh` clean |

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| SH | Strict mode kept, one responsibility | Pass | The archive is one more step of the backup, using its settings, recipient, folder, copy and retention |
| SH | No secret in clear on disk | Pass | `tar` streams into `age`; the archive is written as `.partial`, 0600, then moved |
| SH | Errors | Pass | A failed encryption stops the backup, so `BackupStale` alerts; a host without a deployment warns and backs up the database only |
| SH | Retention | Pass | `prune` takes a name pattern, so dumps and archives are each kept 14 daily and 8 weekly |
| ADR | Context, options, decision, consequences | Pass | Three options; the RTO and the RPO with their reasons; each accepted single point of failure with what happens |
| ADR | Honest about limits | Pass | A lost node breaks the RTO; the monitoring history is accepted as lost; the drill needs S03 |
| Runbook | Each step names its command and its reason | Pass | Ten steps for the VM, four for the LXC |

## Overall Verdict

Go.

## Action Items

| # | Action | Owner | Due |
| --- | --- | --- | --- |
| 1 | Run the rebuild drill (#297) on a throwaway VM with S03 at the node, and record the times against the RTO and the RPO | S03 with the AI | When S03 is available |
| 2 | After the first night, check that `nfhotel-settings-daily-*.tar.age` is on the VM and on the laptop, and restore one on S03's machine (MIL-037 criterion 4) | S03 | 2026-10-25 |

---

[MIL-037]: ../../milestones/mil-037-disaster-recovery.md
[ADR-0025]: ../../adr/adr-0025-recovery-objectives.md
[ADR-0008]: ../../adr/adr-0008-database-migration-and-backup.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-ADR-001]: ../../../framework/qc/artifacts/qc-adr.md
[9119b55]: https://github.com/NF-Hotel/NFHotel/commit/9119b55c52778a45adb8bd19ad1a19c4ae64e64c
