# RC-160 Gate check of MIL-031 (one repository)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-160 |
| CrossReference | [MIL-031], [ADR-0016], [RC-148], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-21 | Accepted | mikemike | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- **Instance reviewed:** the Go/No-Go criteria of [MIL-031], after its code (RC-148), the VM switch and three deploys from NFHotel.
- **Checklist used:** [QC-MIL-001], the criteria of the milestone itself.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`). S01 asked for this check in chat on 2026-10-21.

## Go/No-Go Criteria

| # | Criterion | Result | Evidence |
| --- | --- | --- | --- |
| 1 | `git log -- ops/` shows the commits of NF-Hotel/DevOps up to `b63fbed` | Go | `b63fbed` is an ancestor of `main`, and `git log -- ops/` lists the DevOps commits |
| 2 | No workflow uses `NF-Hotel/DevOps`, and no deploy file is copied outside `ops/` | Go | `check-layout.sh` passes in "Ops linters" on every pull request. A search of `.github/workflows` finds no `NF-Hotel/DevOps/` |
| 3 | The `ops` CI checks pass; release check, E2E and DAST pass; both CIs are required checks | No-Go | Every check passes on `main` 8a077b0. But the ruleset "Approve merges" requires only the six application checks. The `NF Hotel Ops CI` jobs are not required (RC-148 action item, still open) |
| 4 | A release from `main` is deployed by CD after an approval; `/opt/nfhotel` is an NFHotel checkout at the release commit; the smoke test passes | Go | CD runs 39531508012 (1e284e2, attempt 3), 39559106805 (13e2b61) and 39561453009 (8a077b0). Attestations were verified, the smoke test answered 301 and 200, and `/opt/nfhotel` is NF-Hotel/NFHotel |
| 5 | The market analysis page answers with an analysis in production | Not checked | The page is behind the staff login, and the reviewer has no login. The service is healthy in every deploy, and `/admin/market-analysis` redirects to the login. S01 checks the page after signing in |
| 6 | `dev-up.sh --check` passes with one clone | Go | A full local run on 2026-10-20 (home and `/book` 200, `/admin` 302) |
| 7 | NF-Hotel/DevOps is archived, and each of its open PRs is closed with a link | No-Go | DevOps is not archived and has 10 open pull requests (NFHotel#205) |
| 8 | Reviews `Go` | Go | RC-146 and RC-147 `Go`. RC-148 was `Go-with-conditions` on QC-WF-001 criterion 22, which is now met by the CD runs in criterion 4 |

## Findings

- **Ops CI is not required** (criterion 3). A pull request that breaks an `ops` script test can still be merged once the six application checks pass. Making the nine `NF Hotel Ops CI` jobs required is a change to the ruleset, which S01 decides.
- **DevOps is still open** (criterion 7). This is MIL-031 task 7: close its pull requests with links, move #5, #10 and #12, and archive it.
- **The production faults found during the switch** were fixed in NFHotel#220: the backup path, and the rollback across the switch. The benchmark settings on the VM were brought to the format of `notebook-benchmark` 409693d. ADR-0018 (MIL-032) removes the manual install that caused the stale commands.

## Overall Verdict

No-Go for now. Criteria 3 and 7 are open, and criterion 5 needs S01's check. NFHotel#206 stays open until they pass.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Add the `NF Hotel Ops CI` jobs to the required checks of the ruleset "Approve merges" | S01 | Before the next `ops` change |
| Sign in and check that `/admin/market-analysis` shows an analysis | S01 | Next login |
| Carry over and archive NF-Hotel/DevOps (NFHotel#205) | S01 | MIL-031 target date, 2026-11-03 |

---

[MIL-031]: ../../milestones/mil-031-consolidate-devops-into-nfhotel.md
[ADR-0016]: ../../adr/adr-0016-one-repository-for-application-and-operations.md
[RC-148]: ./rc-148-mil-031-consolidation-code.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
