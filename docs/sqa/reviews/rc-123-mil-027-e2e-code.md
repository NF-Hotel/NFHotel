# RC-123 Review of the end-to-end tests and the E2E and DAST workflow (MIL-027)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-123 |
| CrossReference | [MIL-027], [QC-CS-001], [QC-WF-001], [QC-YML-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [adb99a6] |

---

## Artifact Under Review

- **Instances reviewed:**
  - `tests/NFHotel.E2E.Tests` (new): `E2EFactAttribute`, `E2ESettings`, `BrowserFixture`, `BrowserTest`, `Pages`, `StaffSignIn`, `GuestBookingTests`, `BackOfficeAccessTests`;
  - `.github/workflows/e2e-dast.yaml` and `security/zap-rules.tsv` (new);
  - the `E2EFact` addition to `TestRulesTests` and the solution entry.
- **Checklists used:** [QC-CS-001] for the tests, [QC-WF-001] and [QC-YML-001] for the workflow.
- **Scope:** full review of the new files.
- **Language and domain:** n/a (technical type).
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).
- **Tools:**
  - `dotnet build` (0 warnings, warnings as errors), `dotnet format whitespace --verify-no-changes`, `scripts/check-comment-language.sh`;
  - the full `dotnet test` of the solution: 892 passed, 12 skipped (the 9 end-to-end tests without a stack, the 3 Proxmox live tests);
  - actionlint 1.7.7 and yamllint 1.35.1 on the workflow;
  - a local run of the 9 end-to-end tests against the stack of NF-Hotel/DevOps (DevOps RC-504): 9 passed.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| CS naming | Names and test names | Pass | Every test is `Method_Condition_Expected`; `TestRulesTests` now also reads `[E2EFact]`, and its three tests pass |
| CS async | Asynchronous code | Pass | Every Playwright call is awaited; no `.Result`, `.Wait()` or `async void` |
| CS determinism | No sleep and no clock in tests | Pass | Playwright waits for each locator and for a quiet network; the booking dates are fixed in 2030, so no test reads the clock |
| CS isolation | Tests are independent | Pass | A new browser context per test, so no cookie or session leaks. The rate limit test uses a user name of its own. The two booking tests use different dates, so they never take the same room night |
| CS secrets | No secret in code | Pass | The accounts and passwords come from the environment that the runner fills from the stack's generated `stack.env`. No value is in git or printed |
| CS skip | A skipped test explains itself | Pass | `[E2EFact]` skips with the reason and the command that runs it. With `E2E_REQUIRE=true` (the runner) a missing setting fails instead |
| CS comments | XML comments in English | Pass | Each type and the non-obvious steps (the empty-state button, the change event of `@bind`) |
| WF 4 | Header comment of the workflow | Pass | The triggers, what is attacked and what never is, the secrets (none) and the permissions per job |
| WF 7 | Least permissions | Pass | `contents: read` at the top. `security-events: write` only for the two DAST jobs, `issues: write` only for the full scan |
| WF 8 | Actions pinned by full SHA | Pass | checkout v7.0.1, and the DevOps actions at `6873188`. That commit is on NF-Hotel/DevOps#4, so the pin moves to its merge commit later |
| WF 10 | Untrusted text only through env | Pass | The issue step reads the run id and the title from `env:` |
| WF 14–16 | runs-on, timeout, concurrency | Pass | `ubuntu-24.04`, a time limit per job, concurrency per event so a schedule never cancels a pull request run |
| WF 20 | run blocks within the YAML limit | Pass | One `run:` of four folded lines |
| WF 22 | Features verified on the instance that runs them | Fail | Not yet: the workflow runs on github.com first in this pull request |
| YML 1–3 | Parses, formatting, yamllint | Pass | 0 findings, with the two documented line-length exceptions for the pinned action references (as in `release-check.yaml`) |

## Findings

- **The plan against the application.**
  - The booking flow ends on the receipt page `/booking/<number>`, not on a confirmation step, so the first booking test checks that page. That is MIL-027 criterion 1: a receipt page that fails breaks the test.
  - The guest form shows its field errors only after a submit, so the invalid-email test submits and checks that the page stays on `/book`.
  - The Manager holds every back office permission, so the Manager test opens all five pages. Criterion 2 asks for a refused page per role, and no such page exists for the Manager; the matrix is still covered.
- **Task 2 deviation** (DevOps RC-504): the room that the guests book is created by the Manager in the test fixture, because `seed-market-data` is not on `main` yet.
- **A Danish comment in `src/`.** `RolePermissions.HasPermission(StaffRole, string)` in `NFHotel.Application` has the Danish summary "Kontrollerer om en medarbejderrolle har en bestemt permission". `check-comment-language.sh` does not catch it. It is outside this milestone and left for its own change.

## Overall Verdict

Go-with-conditions. Every Mandatory criterion passes except QC-WF-001 criterion 22. That one closes when "End-to-end tests" and "DAST baseline" have run green on this pull request.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Rerun "End-to-end tests" twice on the same commit (MIL-027 criterion 5) | S01 | Task 9 |
| Move the DevOps pin to the merge commit of NF-Hotel/DevOps#4 | S01 | After that merge |
| Add "End-to-end tests" and "DAST baseline" to the ruleset "Approve merges" | S01 | Task 8, after this merge |
| Translate the Danish summary in `RolePermissions` and widen the language check | S01 | Its own change |

---

[MIL-027]: ../../milestones/mil-027-dast-and-end-to-end-tests.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[adb99a6]: https://github.com/NF-Hotel/NFHotel/commit/adb99a62a6b4f841da9ea06399194c4700f79f74
