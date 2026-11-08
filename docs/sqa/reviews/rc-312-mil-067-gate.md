# RC-312 Gate review of MIL-067 Bad input on the booking page and the staff pages

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-312 |
| CrossReference | [MIL-067], [RC-309], [RC-310], [RC-311], [ADR-0038], [QC-MIL-001], [RC-313] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-08 | Deprecated | mikemike | AI as stand-in for S01 | Initial version; mode autonomous, not independent; the gate of MIL-067 after the CD run of the merge and a full DAST scan; Go | [a6f0dec] |
| 2026-11-08 | Accepted | mikemike | AI as stand-in for S01 | Corrected: Proxy Disclosure (40025) is not an alert that the ZAP rules know; task 7 of MIL-066 removed its `IGNORE` line, so the full scan runs it again and it warns; the verdict is unchanged (RC-313) | pending |

---

## Artifact Under Review

- Instance reviewed: [MIL-067] at its gate: the seven Go/No-Go criteria, with the evidence of pull request #596 (merged as ebbf921 on 2026-11-08 at 15:59 UTC), the release run 42498021074, the CD run 42498483148 of that commit, and the full DAST scan 42498990627 after the deploy. Before the gate: [RC-309] (the plan, accepted by S01), [RC-310] (task 7) and [RC-311] (the code, Go-with-conditions until every required check of the pull request is green).
- Checklist used: the Go/No-Go criteria of [MIL-067]; [QC-MIL-001] criterion 2 is what makes them the checklist of the gate.
- Scope: the gate; criteria 1 to 5 and 7 cite RC-311 and the green checks of #596.
- Language and domain: en / it (the Metadata rows of MIL-067); this record is a technical type.
- Language reviewer: none.
- Mode: autonomous, not independent. S01 accepted the plan in chat ("Gennemfør 067", RC-309), handed the decision on task 7 and the rest of the run to the AI ("Ta selv stilling til alt", RC-310), and then asked the AI in chat to approve the CD run of the merge of #596 and to cancel the waiting CD run 42489880568 of the merge of #587, so that everything deploys at once ("1 ja", "2 ja").

**Evidence of 2026-11-08:**

- **The pull request.** #596: on its last commit 7b077b0, 51 checks passed and 1 was skipped by design (`DAST full scan`, which runs only by hand and on schedule); all 41 required checks of `main` passed. SonarQube Cloud passed its quality gate with 0 new issues, 0 security hotspots and 100.0% coverage on new code, after ed1836a had given the `var` of the first push their explicit types ([RC-311]). The pull request closed #589, #590, #591, #592 and #595 at the merge; #593 and #594 stay open for this gate.
- **The full DAST scan of the branch, before the merge.** Run 42497092075 of `e2e-dast.yaml`, started by hand on `mil-067-bad-input` at 7b077b0: "PASS: Integer Overflow Error [30003]", FAIL-NEW 0, WARN-NEW 1 (Proxy Disclosure [40025] x 18, a Medium alert on the name of nginx that the full scan runs again since task 7 of MIL-066 removed its `IGNORE` line; a warning does not fail the scan), PASS 142, and "PASS no High alert".
- **The release.** Run 42498021074 of ebbf921 logged "PASS … no secret, no critical finding and no high finding with a fix" for each of the six release images between 16:06 and 16:07 UTC: `nfhotel-web`, `nfhotel-api`, `market-analysis`, `benchmark`, `nfhotel-nginx` and `nfhotel-socket-proxy`.
- **The CD queue.** The CD run 42496528723 of bd97717, the merge of the plan, changed documents only and was cancelled under the standing request of S01 for such runs; GitHub had cancelled the pending run 42491590120 of b1abd8e (the ZAP rules of MIL-066) when 42496528723 entered the queue of `production`. At the request of S01 ("2 ja"), the AI cancelled the waiting run 42489880568 of 42de1fc (the code of MIL-066) once the run of ebbf921 was in the queue, so that one deploy brings the code of MIL-066, its ZAP rules and the code of MIL-067.
- **The deploy.** CD run 42498483148, approved by S01 in GitHub at 16:12 UTC (the approval by the AI at the request of S01 was refused by the safety check of the AI tool, so S01 approved it), ended with success at 16:15 UTC. Its verify step logged "build provenance and SBOM verified" for each of the six digests of the release manifest, `nfhotel-web@sha256:e6efcf08afe3…` and `nfhotel-api@sha256:2fec2741c8a0…` among them. The deploy took a backup of the database before the migrations and copied it with the settings archive to the backup target, applied the migration script as `nfhotel_migrator`, and recreated the API, the web application, the market analysis, the benchmark, nginx and the socket proxy, each healthy by 16:15 UTC; the smoke test answered 301 on port 80 and 200 on port 443. The settings archive of the deploy lists 15 paths, `/etc/nfhotel` and the certificate and key of nginx among them, which the gate of MIL-066 can cite.
- **The full DAST scan after the deploy.** Run 42498990627 of `e2e-dast.yaml`, started by hand on `main` at ebbf921 at 16:15 UTC, after the deploy: "PASS: Integer Overflow Error [30003]", FAIL-NEW 0, WARN-NEW 1 (Proxy Disclosure [40025] x 18, a Medium alert on the name of nginx that the full scan runs again since task 7 of MIL-066 removed its `IGNORE` line; a warning does not fail the scan), PASS 142, and "PASS no High alert".

## Checklist Results

| # | Criterion (MIL-067) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Tests through `WebFactory` prove that `/book` answers 200 with the booking box when `guests` is larger than `Int32.MaxValue`, not a number, or empty, and that each of these leaves the default number of guests; that a whole number fills the box, kept between 1 and `MaxGuests`; and the tests of the values that do not parse fail against the code before the change | Pass | `BookingAddressTests` (13 tests) and the two theories of `BookingFlowViewModelTests`, red before b206e5a as [RC-311] records, and green in `NF Hotel CI` on #596; the full scan of the branch passed 30003 |
| 2 | A test proves that `/admin/benchmark/start?again=<not a GUID>` answers the page to a signed-in staff member whom `StaffPolicies.BenchmarkRun` allows, and fails against the code before the change | Pass | `AdminAccessTests.Get_BenchmarkStartAgainNotARun_ShowsThePage` and `BenchmarkStartPageTests.Again_NotARun_OpensThePageAsWithout`, red before 88d8b82 ([RC-311]) |
| 3 | A test fails when a property with `[SupplyParameterFromQuery]` in `NFHotel.Web` has a type other than `string`, and passes on the code after the change | Pass | `QueryParameterTests.QueryParameters_OfEveryPage_AreText` named `Book.GuestsQuery` (`Int32?`) and `BenchmarkStart.Again` (`Guid?`) before tasks 1 and 2, and passes on ebbf921 ([RC-311]) |
| 4 | A test posts the login form with an empty user name and a password and finds the password nowhere in the answer; the review record says whether it failed against the code before the change (finding 3 reproduced) or not | Pass | `AdminAccessTests.SignIn_EmptyUserName_DoesNotSendThePasswordBack` failed before 5d8fcf1: finding 3 reproduced ([RC-311]) |
| 5 | The code review against `QC-CS-001` is Go, the tests and the analyzers pass, and every required check of the pull request is green | Pass | [RC-311] Go-with-conditions, and its condition is met: all 41 required checks of #596 passed on 7b077b0, and SonarQube Cloud reported 0 new issues |
| 6 | The continuous deployment (CD) run of the merge, approved by S01 ([ADR-0038]), deploys and its smoke test passes; a full DAST scan after it, started by hand, has no Integer Overflow Error (30003) and no High alert | Pass | CD run 42498483148, approved by S01, deployed ebbf921 and its smoke test answered 301 and 200; the full scan 42498990627 after it passed 30003 and found no High alert |
| 7 | A test through `WebFactory` posts the change-password form with two different new passwords and finds none of the three passwords in the answer, and fails against the code before the change | Pass | `StaffPasswordPageTests.Post_ChangeRefusedByTheForm_DoesNotSendThePasswordsBack` (two cases) failed before edf9d8e: finding 4 reproduced ([RC-311]) |

## Findings

- **The first version of the code review missed seven `var`.** SonarQube Cloud found six of them on the first push of #596; ed1836a gave them their types, and [RC-311] records its own mistake. Criterion 5 rests on the second push.
- **One deploy for two milestones.** The waiting CD run of MIL-066 (42489880568) was cancelled at the request of S01, so the run of ebbf921 deploys the code of MIL-066 and of MIL-067 together; the gate of MIL-066 names this run as its deploy.
- **The full scan tests the code, not the site.** `e2e-dast.yaml` scans a throwaway stack built from the ref, so the scan of the branch before the merge already showed that 30003 was gone; criterion 6 still asks for the scan after the deploy, so that the code scanned is the code that runs.

## Overall Verdict

Go: the seven criteria pass on the evidence of 2026-11-08, and the condition of [RC-311] is met. The gate of MIL-067 is passed: issues #593 and #594 close through the pull request of this record, and the GitHub milestone #49 is closed after the merge. The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Close the GitHub milestone #49 after the merge of this record, and check that #593 and #594 are closed | AI as stand-in for S01 | At the merge |
| Put the members of the files under `src/` in `#region` blocks, or propose upstream to change criterion 21 of QC-CS-001 (the risk exception of RC-311) | S01 | 2026-12-31 |
| Decide the use of `var` in the repository (F3 of RC-265 and RC-266): a waiver, or a conversion as a milestone of its own | S01 | Open since 2026-11-06 |

---

[MIL-067]: ../../milestones/mil-067-bad-input-on-the-web-pages.md
[RC-309]: ./rc-309-mil-067.md
[RC-310]: ./rc-310-mil-067-task-7.md
[RC-311]: ./rc-311-mil-067-code.md
[RC-313]: ./rc-313-mil-066-gate.md
[ADR-0038]: ../../adr/adr-0038-approval-before-a-deploy.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[a6f0dec]: https://github.com/NF-Hotel/NFHotel/commit/a6f0dec4b0a066af5c4a5b3357834cd28fba6762
