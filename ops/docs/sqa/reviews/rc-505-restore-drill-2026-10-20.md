# RC-505 Restore drill of the production backup, 2026-10-20 (MIL-021 task 7)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-505 |
| CrossReference | [MIL-021], [ADR-0008] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Accepted | mikemike | AI as stand-in for S03 | Initial version | [095666a] |

---

## Artifact Under Review

- **Instance:** the daily backup `nfhotel-daily-20261020T103213Z.dump.age` of `nf-hotel`, made by `nfhotel-backup.timer` (`/usr/local/sbin/nfhotel-backup --apply`) and encrypted with `age` to `age1u23c4gkyxayufmhs6kw756qdhtu4pgscqy5c6m7st2nfmpurrekqmdv0qe`.
- **Procedure:** `scripts/restore-drill.sh` of this repository at `667d0a6`, run on S03's machine in a `docker:27-cli` container with the host's Docker. The identity was S03's key file, whose public recipient matches the recipient above. The key is never on the VM.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S03`; S01 holds S03 for now).

## Checklist Results

| # | Criterion (MIL-021 criterion 8, ADR-0008) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The backup decrypts with S03's identity | Pass | Step 1 of the drill |
| 2 | It restores into a throwaway PostgreSQL 16 with no network | Pass | `pg_restore --exit-on-error`, 10 tables restored |
| 3 | The migration history is present | Pass | 6 rows; latest `20261019172453_AddBookingMarketDetail` |
| 4 | The row counts match production | Pass | Counted read-only on `nf-hotel` at 14:05 CEST: `__EFMigrationsHistory` 6, `staff_member` 1, the other 8 tables 0. These are identical to the restore |
| 5 | Nothing is left behind | Pass | The script removes the container and the decrypted dump; the encrypted copy was in a temporary folder of S03's machine |

## Findings

- **Two age keys exist.** S03's machine holds `~/.nfhotel/backup-age.key` (recipient `age1q06hr…`), which the VM no longer uses, and `~/.age/nfhotel-backup.key` (`age1u23c4…`), which it uses. Only the second can read today's backups. Keep one key, in the password manager of S03, and delete the other copy once S03 has confirmed that no older backup needs it.
- **No off-VM copy yet.** `BACKUP_TARGET` in `/etc/nfhotel/backup.env` is empty, so the backup lives only on the VM. A failure of the VM's disk loses both the database and its backups. MIL-021 task 7 is not complete until the write-only copy (`rrsync`) to a second host runs.
- The production data is still almost empty (one staff account), so this drill proves the procedure, not a restore of real volume.

## Overall Verdict

Go for the restore procedure and this backup. MIL-021 task 7 stays open for the off-VM copy.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Choose the second host and set `BACKUP_TARGET` (write-only `rrsync` account) | S03 | Before MIL-021 closes |
| Keep one age identity in the password manager; remove the unused copy | S03 | Next week |
| Run the next drill in a month and record it (MIL-023 task 6 needs two) | S03 | 2026-11-20 |

---

[MIL-021]: ../../milestones/mil-021-secure-cicd-foundation.md
[ADR-0008]: ../../adr/adr-0008-database-migration-and-backup.md
[095666a]: https://github.com/NF-Hotel/DevOps/commit/095666a7cfff99037c4a602ab46a6d12d590c6e3
