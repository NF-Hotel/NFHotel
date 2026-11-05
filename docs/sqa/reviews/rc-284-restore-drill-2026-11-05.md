# RC-284 Restore drill of the production backup, 2026-11-05 (MIL-023 task 6)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-284 |
| CrossReference | [MIL-023], [ADR-0008], [MIL-021] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-05 | Accepted | Claude (AI) | AI as stand-in for S03 | Initial version; mode autonomous, not independent; S01 accepted the plan in chat on 2026-11-05 ("Accepter planen") with the drill today rather than on 2026-11-20 | [c6a8e10] |

---

## Artifact Under Review

- Instance: the daily backup `nfhotel-daily-20261105T020810Z.dump.age` of `nf-hotel` (3 413 630 bytes, made at 02:08 UTC by the backup timer and encrypted with `age` to the recipient of S03), copied to a temporary folder on the PC of S01 over Tailscale SSH in check mode and deleted after the drill.
- Procedure: `ops/scripts/restore-drill.sh` of this repository at `af622af`, run on 2026-11-05 at 07:16 UTC in a `docker:28-cli` container with `bash` and `age` added, the Docker of the PC through its socket, and the age identity of S03 as a read-only mount. The key never left the PC and nothing of it was printed; the drill is the same as the first one, [RC-505], which used `docker:27-cli`.
- Checklist used: MIL-021 criterion 8 and ADR-0008, the checklist of [RC-505]; this is drill 2 of the two that MIL-023 criterion 6 asks for.
- Scope: full drill
- Language and domain: n/a (technical type)
- Language reviewer: none
- Mode: autonomous, not independent. The reviewer is the AI as stand-in for S03, the role S01 holds; S01 asked in chat on 2026-11-05 to finish the open milestones and accepted the plan with the drill today.

## Checklist Results

| # | Criterion (MIL-021 criterion 8, ADR-0008) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The backup decrypts with the identity of S03 | Pass | Step 1 of the drill |
| 2 | It restores into a throwaway PostgreSQL 16 with no network | Pass | `pg_restore --exit-on-error` into `postgres:16-alpine` started with `--network none`; 15 tables restored |
| 3 | The migration history is present | Pass | 11 rows; latest `20261101155447_AddPriceChange` |
| 4 | The row counts match production | Pass | Counted read-only on `nf-hotel` at 07:14 UTC with an exact `count(*)` per table: `__EFMigrationsHistory` 11, `ai_default` 2, `benchmark_request` 7, `benchmark_result` 28, `benchmark_run` 4, `booking` 156 687, `booking_market_detail` 156 686, `guest` 1 969, `market_analysis_run` 4, `price_change` 0, `pricing_settings` 0, `room` 294, `room_rate` 0, `staff_audit_event` 33, `staff_member` 1; the restore reports the same fifteen counts |
| 5 | Nothing is left behind | Pass | No `nfhotel-restore-drill-*` container after the run; the decrypted dump lived in the temporary folder of the CLI container, removed with it; the encrypted copy was deleted from the scratch folder of the PC |

## Findings

- **Sixteen days after drill 1**, not a month: S01 chose the date so that MIL-023 criterion 6 can close now. The daily backups of 2026-11-04 and 2026-11-05 (database and settings) were in `/var/backups/nfhotel`; the off-VM copy was not part of this drill.
- **Real volume this time:** 156 687 bookings and 1 969 guests, the synthetic data of the market analysis and the benchmark, against one staff account in drill 1, so the drill now proves a restore of volume, not only the procedure.
- **The script runs wherever Docker runs:** `age` and bash 4 are not installed on the Windows PC of S01, so the drill ran inside a Docker CLI container with the same steps; no tool was installed on the PC.
- **Two age identities still exist on the PC** (`~/.age/nfhotel-backup.key`, which the VM uses, and `~/.nfhotel/backup-age.key`, which it no longer uses): the action item of RC-505 is still open.

## Overall Verdict

Go for the restore procedure and this backup. MIL-023 criterion 6, two monthly restore drills recorded, is met with [RC-505] (2026-10-20) and this record; task 6 (#139) closes with the pull request of this record. The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Run drill 3 and record it (ADR-0008: a drill every month and before a major migration) | S03 | 2026-12-05 |
| Keep one age identity in the password manager of S03 and remove the unused `backup-age.key` (RC-505) | S03 | Open since 2026-10-20 |

---

[MIL-023]: ../../milestones/mil-023-operational-quality.md
[ADR-0008]: ../../adr/adr-0008-database-migration-and-backup.md
[MIL-021]: ../../milestones/mil-021-secure-cicd-foundation.md
[RC-505]: ./rc-505-restore-drill-2026-10-20.md
[c6a8e10]: https://github.com/NF-Hotel/NFHotel/commit/c6a8e107296f35bf1ef99511403cd752e79c5a1a
