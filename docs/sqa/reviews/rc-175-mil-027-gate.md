# RC-175 Gate check of MIL-027 (DAST and end-to-end tests)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-175 |
| CrossReference | [MIL-027], [RC-122], [RC-123], [RC-504], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-23 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [5fb6c93] |

---

## Artifact Under Review

- **Instance reviewed:** the seven Go/No-Go criteria of [MIL-027].
- **Checklist used:** [QC-MIL-001].
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`). S01 accepted the plan for the remaining milestones on 2026-10-23.

## Go/No-Go Criteria

| # | Criterion | Result | Evidence |
| --- | --- | --- | --- |
| 1 | A PR that breaks the booking flow (the receipt page answers 500) fails "End-to-end tests"; made with a temporary change, then reverted | Go | Proof PR #244 made the receipt page throw. "End-to-end tests" failed (run 39922876515): `Receipt_UnknownBookingNumber_AnswersWithoutAServerError` and `Book_FreeRoomWithCompleteForm_ShowsTheReceipt`, 2 of 12. The PR was blocked, closed unmerged, and its branch deleted |
| 2 | The end-to-end tests cover the guest booking, field errors, the login redirect, one allowed and one refused page per role, Housekeeping without guest data, and 429 | Go | `tests/NFHotel.E2E.Tests`, 12 tests, among them:<br>- `Book_FreeRoomWithCompleteForm_ShowsTheReceipt`;<br>- `Book_GuestFormWithInvalidEmail_ShowsTheFieldErrorAndSendsNothing`;<br>- `AdminPage_WithoutSession_RedirectsToTheLogin`;<br>- `Receptionist_OpensBookingsAndSales_SeesBookingsAndIsRefusedSales`;<br>- `Housekeeping_SignsIn_LandsOnRoomsAndIsRefusedBookingsAndGuests`;<br>- `Manager_OpensEveryBackOfficePage_IsRefusedNone`;<br>- `SignIn_SixthFailureInARow_ShowsTooManyAttempts`;<br>- `SignOut_AfterSignIn_EndsTheSession` |
| 3 | The ZAP baseline runs on every PR, signed out and signed in, with 0 High; every IGNORE or WARN rule has a reason and an expiry within 30 days | Go | "DAST baseline" is a step of `e2e-dast.yaml` on every PR; it passed on 34 PR runs. `ops/security/zap-rules.tsv` holds no IGNORE or WARN rule, so every rule keeps the ZAP default, and any High fails the scan |
| 4 | The weekly active scan has run on `main` and its report is stored; it targets only the job's own Docker network | Go | Run 39381990963 (`workflow_dispatch` on `main`): "DAST full scan" succeeded, and its report is the artifact `dast-full`. Its target was `https://nginx` on the network `nfhotel-e2e-proxy` of the job, not the VM or the tailnet. The weekly `schedule` (Mondays 03:17 UTC) is in `e2e-dast.yaml` |
| 5 | The tests are repeatable: no fixed sleeps, data seeded on each run, no real secrets, three reruns of one commit all green | Go | No `WaitForTimeout`, `Thread.Sleep` or `Task.Delay` in the test project. The stack seeds its data and makes throwaway accounts on each run. Run 39924351561 of PR #247 (commit `c5ba081`): attempt 1 and reruns 2, 3 and 4 all green ("End-to-end tests" and "DAST baseline") |
| 6 | "End-to-end tests" and "DAST baseline" are required checks of "Approve merges" | Go | Both are among the 19 required checks of the ruleset |
| 7 | Reviews against `qc-programming-csharp`, `qc-programming-shell`, `qc-workflow` and `qc-yaml` are `Go` | Go | RC-123 (C#, workflow, YAML) and RC-504 (shell, workflow, YAML) were Go-with-conditions on QC-WF-001 criterion 22: the checks had to run green on GitHub. They have, on 34 PR runs. The three failed PR runs are the two proof PRs (#242, #244) and a Dependabot bump of Playwright before its image was bumped too (#212) |

## Findings

- **E2E and DAST on `main` are cancelled when a newer commit arrives** (`cancel-in-progress`). The deploy gate of ADR-0021 then refuses the older release, and the newest one deploys. That is intended, but three reruns of a `main` commit cannot be proven while `main` moves. The reruns were therefore made on a PR run.
- **The full scan reports warnings, not High.** One is a missing `X-Content-Type-Options` header (rule 10021) on the web pages. It belongs to the security headers work.

## Overall Verdict

Go. MIL-027 is complete.

## Action Items

None.

---

[MIL-027]: ../../milestones/mil-027-dast-and-end-to-end-tests.md
[RC-122]: ./rc-122-mil-027.md
[RC-123]: ./rc-123-mil-027-e2e-code.md
[RC-504]: ./rc-504-mil-027-stack-and-dast.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[5fb6c93]: https://github.com/NF-Hotel/NFHotel/commit/5fb6c93e575a1371fbea5f1cf91b610ba47418fc
