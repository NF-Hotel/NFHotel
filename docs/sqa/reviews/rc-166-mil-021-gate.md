# RC-166 Gate check of MIL-021 (secure CI/CD foundation)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-166 |
| CrossReference | [MIL-021], [ADR-0007], [ADR-0008], [RC-163], [RC-164], [RC-165], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-22 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [0aac8e5] |

---

## Artifact Under Review

- **Instance reviewed:** the ten Go/No-Go criteria of [MIL-021], as amended by RC-164.
- **Checklist used:** [QC-MIL-001], the criteria of the milestone itself.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`). S01 accepted the plan for MIL-021 in autonomous mode in chat on 2026-10-21.
- **Evidence:**
  - the GitHub runs named below;
  - read-only commands that S03 ran on `nf-hotel` on 2026-10-22;
  - the restore drill run on the legion laptop.

## Go/No-Go Criteria

| # | Criterion | Result | Evidence |
| --- | --- | --- | --- |
| 1 | A PR with a failing required check cannot be merged | Go | Test PR #193: the required check Formatting failed, `mergeStateStatus: BLOCKED`, and the PR was closed unmerged. Since 2026-10-21 the ruleset requires 19 checks, the Ops CI jobs included, with no bypass |
| 2 | A green merge to `main` produces images in GHCR, identified by digest in the Release job summary | Go | Release run 39649163690 (`9cfd923`) built web, api and benchmark, carried the frozen `market-analysis` digest, and wrote the manifest with the digests and attestations |
| 3 | CD waits for approval in `production`, and the job receives no credential before approval | Go | CD runs 39650132524, 39760042572 and 39765086655 each waited until Tirsvad approved. The tailnet is joined by OIDC inside the approved job; no Tailscale secret or ssh key is stored |
| 4 | After the deploy, the VM runs the digests of the Release run, and no image was built on the VM | Go | `docker ps` on `nf-hotel` (2026-10-22): web `ff94bf791b50`, api `60a9463d4783`, benchmark `3848af4e339f` and market analysis `6bd53d9e59af` match the release digests. nginx, ollama and postgres are digest-pinned third-party images. No container runs a local `:local` build. The deploy runs `deploy-services.sh --no-build` |
| 5 | A deploy that changes only the web image does not recreate `postgres`, `nginx` or `ollama` | Go-with-conditions | `postgres` and `ollama` were kept (`Running`) in runs 39650132524, 39760042572 and 39765086655. `nginx` was recreated on every deploy by design (`--force-recreate`); RC-165 (#232) changed that, so it is recreated only when its configuration changes. Run 39765086655 recreated it once more, because that release added its configuration hash. The condition is closed by the CD of the merge of this record: a release with new application images only, and `nfhotel-nginx-nginx-1 Running` |
| 6 | The smoke test passes; a forced failure rolls back to the previous digests, and the run is red | Go | Rollback drill, run 39650132524: `9cfd923` deployed (smoke test 301 and 200), treated as failed, rolled back to `8660b81` (smoke test 301 and 200 again), and the run ended red. The drilled release is not in the release log |
| 7 | The CI node reaches only port 22 of `tag:nfhotel-vm`; no ssh key and no Tailscale secret in GitHub | Go | The scope step of runs 39760042572 and 39765086655 reports "Ports 80 443 5432 8081 of nf-hotel do not answer". The tailnet policy tests deny `tag:ci` the backup target, the Proxmox node and the Sports Club VM; the policy cannot be saved otherwise. Other peers are only seen, because the member grant lets members reach every node (RC-165) |
| 8 | A dump from the timer, taken from the off-VM copy, is restored, and the row counts match; recorded in an RC | Go | `nfhotel-daily-20261022T100035Z.dump.age` from `C:\Users\syneu\nfhotel-backup-target\backups` on the legion laptop, restored with `restore-drill.sh`: 11 tables, `booking` 156 686, `booking_market_detail` 156 686, `guest` 1 968, `room` 294, `staff_member` 1. Latest migration `20261020110754_AddStaffPasswordAndAudit`. These are the counts of production. Recorded here |
| 9 | Linters clean; reviews against `qc-yaml`, `qc-programming-shell` and `qc-adr` are `Go` | Go | `ops/tests/lint.sh` passes in Ops CI on `main`. Reviews: RC-092 and RC-093 (ADR-0007 and ADR-0008), RC-094 and RC-095 (shell and workflows of MIL-021), RC-163 and RC-165 (the code that finished it), all `Go` |
| 10 | `/opt/nfhotel` is a checkout of `NF-Hotel/NFHotel` (ADR-0016) whose compose files describe every running container; 5432 does not answer from the tailnet | Go | The VM was switched on 2026-10-21, and each CD checks out the release commit. The eight running containers are the services of `ops/deploy/` (`docker ps`). Nothing listens on 5432 on the host, and the scope step finds 5432 closed |

## Findings

- **The off-VM copy is in place** (D7, revised). All 18 backups since 2026-10-21 reached the laptop, the older ones by the catch-up of the first run.
- **The restore drill is not yet scheduled monthly.** That is planned for MIL-033 (Operate).
- **The member grant of the tailnet policy is broad** (`autogroup:member` and `autogroup:shared` to every node and port). It does not affect the criteria of MIL-021, which concern what the CI node and the VM may reach. It is planned for MIL-033.
- **No staff sign-in has been recorded in production** (`staff_audit_event` has 0 rows). The first Manager has therefore not changed the initial password, which also explains RC-161 action 4.
- **There is no alert on a failed timer or a failed copy.** Planned for MIL-034 (Monitor).

## Overall Verdict

Go-with-conditions. Criterion 5 closes with the CD of the merge of this record. The milestone closes once that run shows `nfhotel-nginx-nginx-1 Running`.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Check `nfhotel-nginx-nginx-1 Running` in the CD of this merge, record the run on NFHotel#125, and close MIL-021 | S01 (AI) | That CD |
| Sign in as the first Manager, change the password (MIL-030), then remove `Staff__InitialManager__Password` from `nfhotel-api.env` | S03 | At once |

---

[MIL-021]: ../../milestones/mil-021-secure-cicd-foundation.md
[ADR-0007]: ../../adr/adr-0007-cicd-and-supply-chain.md
[ADR-0008]: ../../adr/adr-0008-database-migration-and-backup.md
[RC-163]: ./rc-163-mil-021-finish-code.md
[RC-164]: ./rc-164-mil-021-amendment.md
[RC-165]: ./rc-165-nginx-recreate-and-scope.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[0aac8e5]: https://github.com/NF-Hotel/NFHotel/commit/0aac8e50d72fd71b38b97ce6b19cefbcecec6ce0
