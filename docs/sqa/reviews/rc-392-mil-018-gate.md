# RC-392 Gate review of MIL-018 Guest login

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-392 |
| CrossReference | [MIL-018], [MIL-016], [MIL-017], [MIL-079], [ADR-0047], [UC-010], [SSD-001], [OC-001], [RC-083], [RC-084], [RC-358], [RC-388], [RC-389], [RC-390], [RC-391], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-15 | Accepted | mikemike | AI as stand-in for S01 | Initial version: the gate of MIL-018 after the merge of pull request 774 (afa2d09), its deploy and the run on the deployed site; Go; mode autonomous, not independent | pending |

---

## Artifact Under Review

- Instance reviewed: [MIL-018] at its gate. It covers the eight Go/No-Go criteria, with this evidence:
  - the code of tasks 3 to 6 on `main` at afa2d09 (pull request 774), reviewed in [RC-391];
  - [ADR-0047] (task 2), reviewed in [RC-388]; [UC-010] with [SSD-001] and [OC-001] (task 3), reviewed in [RC-389]; the amendments of SA-001, US-001, UCD-001, UC-004, DM-004 and DICT-001, reviewed in [RC-390];
  - NF Hotel CI on the last head of pull request 774 (0a4fe15), with the integration tests against PostgreSQL and the end-to-end tests;
  - the wiki pages of task 6 (Public-Access, Security-Controls and Proxmox-Deployment), published by NF Hotel Wiki from afa2d09 at 21:20Z and equal to `docs/wiki` apart from the line endings;
  - the deploy of afa2d09 by NF Hotel CD (run 43764033433) and the run of task 7 on the deployed site (2026-11-15, 21:35Z to 21:37Z).

  Before the gate: [RC-083] (task 1, the Business Case), [RC-084] (the plan, Go) and [RC-358] (the revision of 2026-11-14, Go).
- Checklist used: the Go/No-Go criteria of [MIL-018]. [QC-MIL-001] criterion 2 makes them the checklist of the gate.
- Scope: the gate (task 8 with the run of task 7). Decision 7 of the milestone refuses the sign-in for a booking cancelled more than 30 days ago; [ADR-0047] refuses every cancelled booking, which [RC-388] F3 accepted as stricter than the plan. No criterion names decision 7.
- Language and domain: en / it (the Metadata rows of MIL-018). The gate adds only a row to its Version History. This record is a technical type.
- Language reviewer: none.
- Mode: autonomous, not independent. S01 gave the acceptance of the plan of 2026-11-14, which has MIL-018 in its last wave, to the AI in chat ("Du godkender for mig"), and on 2026-11-15 asked the AI to finish the open milestones without a decision from S01 ("gennemfør derfor alt"). The AI built the code, made the run on the deployed site and wrote this record.

**Evidence of the checks before the merge (the worktree of the branch, 2026-11-15):** `dotnet build NFHotel.sln --configuration Release -warnaserror` with 0 warnings; `dotnet format --verify-no-changes` clean; the unit tests of Application (730, the architecture tests among them), Api (401), Web (834), ApiClient (118) and the Infrastructure classes that MIL-018 changed passed with 0 failed; `ops/tests/lint.sh` clean. After the SonarCloud gate of the pull request (rule S2583, [RC-391] F1), `InMemoryLoginThrottle.TryCountAttempt` became a compare-and-swap loop (0532e78); the build, the format check, the 13 throttle tests (the test of 16 threads five times more) and the 730 Application tests passed again on it. On the merged `main` (afa2d09) the build had 0 warnings again, and the unit tests of Application (730), Api (401), Web (834), ApiClient (118), Domain (485), External (217) and the security classes of Infrastructure (52) passed with 0 failed.

**Evidence of NF Hotel CI on 0a4fe15:** NF Hotel CI (run 43762112916): 3,004 tests, 0 failed and 14 skipped, the integration tests against PostgreSQL among them. NF Hotel E2E and DAST (run 43762112835): the 14 end-to-end tests passed, the five of `GuestBookingTests` among them (`GuestSession_BookSignOutSignInAndCancel_ReachesTheOwnBooking` books, signs out, signs in with the number and the email, and cancels); the guest part of `ops/tests/check-e2e-stack.sh` passed (a guest signs in with the number and the email in another letter case, reads the own booking, is refused another booking, its cancellation and a staff endpoint; a booking read without a staff or guest token gets 401; a wrong email gets 401), and so did the DAST baseline. NF Hotel Ops CI (run 43762112861) and NF Hotel Release check (run 43762112912) passed. SonarCloud's quality gate passed with no open issue and 97.4 % coverage on new code.

**Evidence of the run on the deployed site (2026-11-15, 21:35Z to 21:37Z):** two runs from the PC, outside the host desktop. First a headless Chromium in Microsoft's Playwright container (`mcr.microsoft.com/playwright/dotnet:v1.62.0-noble`, a one-off script): `/book` offered 3 room types for 17 to 19 March 2031 without a sign-in; the test booking for a single room with a made-up email on example.com got the number FLZ-158425 (with no guest cookie yet); "Manage your booking" opened /my-booking, which showed the same number, with the guest cookie `nfhotel.guest`: Secure, HttpOnly, SameSite=Lax, path /my-booking, expires in 119 min; "Sign out" went to /booking and removed the cookie, and `/my-booking` then ended at /booking. Then, from WSL (curl 8.14.1), `GUEST_EMAIL=<the email> bash ops/scripts/check-guest-login.sh --cancel <site> FLZ-158425`, 15 checks, all PASS: the sign-in page answers; the sign-in page has its form; a wrong email is refused on the page; the refusal says that the booking was not found; a wrong email sets no guest cookie; the number and the email sign the guest in; the guest cookie is Secure, HttpOnly, SameSite=Lax, for /my-booking, and ends; the own booking opens; the own booking shows its number; the guest cancels the booking; the own booking says that it is cancelled; the notice of the cancellation is shown; the guest signs out; the sign-out removes the guest cookie; after the sign-out the own booking asks for the sign-in (F4).

## Checklist Results

| # | Criterion (MIL-018) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | A call to read a booking without a staff or guest token gets 401; creating a booking and listing free rooms still work without a token | Pass | `GuestAccessTests.GuestOperation_NoStaffOrGuestToken_ReturnsUnauthorized` and `PublicBooking_NoToken_ReturnsTheBookingWithASessionThatOpensIt`; `EndpointProtectionTests` keeps the list of actions that need only the token of the web application (the booking flow and the two sign-ins); on the deployed site the test booking listed the free room types and was made without a sign-in (above) |
| 2 | A guest token for booking A gets 403 on booking B, on the guest of B, and on cancel and reschedule of B; on its own booking it is allowed | Pass | `GuestAccessTests.GuestOperation_AnotherBooking_ReturnsForbidden` (read, guest, cancel and reschedule of another booking), `GuestOperation_OwnBooking_IsAllowed`, `Reschedule_OwnBookingToAnotherRoom_ReturnsForbidden`; `GuestOwnershipTests` and `GuestAccessServiceTests` in Application |
| 3 | A wrong email, an unknown number and an expired or tampered token give the same refusal, with no difference in the answer text; the sixth failed attempt for a number in 15 minutes gets 429 | Pass | `GuestAccessTests.Refusal_EveryKindOfFailedSignIn_HasTheStatusAndTextOfAWrongEmail`, `SignIn_SixthAttemptAfterFiveFailures_ReturnsTooManyRequests`, `GuestToken_SignedWithoutTheRoleGuestOrABooking_IsRefused`; `JwtGuestTokenIssuerTests`; the throttle counts each attempt before the read, so attempts at the same time cannot pass it together ([RC-391] F1, `TryCountAttempt_ManyThreadsAtOnce_CountsNoMoreThanTheLimit`, `LookUpReceiptAsync_ManyAtTheSameTime_ReadsOnlyAsManyAsTheThrottleAllows`); on the deployed site a wrong email was refused with the one refusal and set no cookie |
| 4 | No answer to a guest token contains a passport number; a guest token on a staff endpoint gets 403 | Pass | `GuestAccessTests.GuestAnswers_GuestWithAPassportNumber_NeverContainTheNumber`, `StaffEndpoint_GuestToken_ReturnsForbidden`, `StaffHeader_GuestToken_ReturnsUnauthorized`, `StaffAndGuestToken_BothValid_TheStaffTokenDecides`, `GuestEndpoints_EveryActionWithAGuestPermission_IsOneOfTheFourGuestOperations` |
| 5 | In the deployed VM a guest can book, sign out, look the booking up with number and email, and cancel it; the guest cookie has `Secure` and `HttpOnly` | Pass | The run above: the guest booked without a sign-in, opened the own booking, signed out, looked the booking up with the number and the email, cancelled it and signed out; the guest cookie has `Secure` and `HttpOnly` (and SameSite=Lax, path /my-booking). The end-to-end test `GuestSession_BookSignOutSignInAndCancel_ReachesTheOwnBooking` runs the same steps in CI |
| 6 | The permission table test states allowed or refused for every role (including Guest) and every permission, and equals the Access Control Matrix | Pass | `AccessControlMatrixTests`: every permission has exactly one row, the header names the four roles in the order of the cells, every cell is allowed or refused, `HasPermission` equals the matrix for every role and permission, and the cases cover four roles times the whole permission list |
| 7 | The staff sign-in tests of MIL-016 and the TLS tests of MIL-017 still pass unchanged | Pass, with a note | Pull request 774 does not change `StaffLoginTests`, `StaffAccountEndpointTests`, the end-to-end tests `StaffSignIn` and `BackOfficeAccessTests`, or `ops/tests/test-nginx.sh`, and they passed in CI (above). `InMemoryLoginThrottleTests` only gained the tests of `TryCountAttempt` (116 lines added, none removed). `EndpointProtectionTests` gained the guest sign-in in its list of actions that need only the token of the web application (F1) |
| 8 | `dotnet build` has 0 warnings, format is clean, all tests pass; `qc-programming-csharp`, `qc-programming-shell` and `qc-yaml` reviews have the verdict `Go`; the ADR of task 2 has a `Go` review | Pass, with a note | The checks above and NF Hotel CI; [RC-391]: QC-CS-001 Go and QC-SH-001 Go; QC-YML-001 is not applicable, because MIL-018 changes no YAML file (F2); [RC-388]: ADR-0047 Go |

## Findings

- **F1, note: a MIL-016 test gained a line.** `EndpointProtectionTests` dates from MIL-016 and lists the actions that need only the token of the web application. MIL-018 added `GuestAuthController.SignInAsync` to the list, because the guest sign-in is public by decision 2 of [ADR-0047]; every other line of the test, and its assertions, are unchanged. The staff sign-in itself is tested by files the pull request does not touch.
- **F2, note: no YAML to review.** Criterion 8 names a `qc-yaml` review. MIL-018 changes no workflow and no other YAML file (`git diff --name-only` of the pull request), so [RC-391] scores QC-YML-001 not applicable, and the criterion is met for the files that exist.
- **F3, note: a test booking in production.** Task 7 made the booking FLZ-158425 for 17 to 19 March 2031 with the name Smoke Guest and a made-up email on example.com; the check cancelled it, and it stays in the database as cancelled. The browser half ran from a one-off script that is not in the repository; the end-to-end test `GuestSession_BookSignOutSignInAndCancel_ReachesTheOwnBooking` is the kept form of the same steps.
- **F4, note: the check does not run under Git Bash.** The first run of `check-guest-login.sh`, from Git Bash on Windows, failed 9 of its 15 checks: the native curl of Git for Windows cannot open the MSYS path of the temporary file in `--data-urlencode <field>@<file>`, so neither sign-in was sent, no attempt was counted, and the cancel without a guest cookie was refused with 400. The same script from WSL passed all 15. The header of the script does not say that it needs a Linux curl; a line there, or the Windows path of the file, is left to S01.

## Overall Verdict

Go. Every criterion passes: 1 to 4 and 6 on the tests of pull request 774, which NF Hotel CI ran on its last head, 5 on the run on the deployed site after the deploy of afa2d09 (NF Hotel CD run 43764033433), 7 and 8 on the files the pull request leaves unchanged, the checks and the reviews [RC-388] and [RC-391]. F1 to F4 are notes. S01 gave the acceptance of the plan of 2026-11-14 to the AI in chat ("Du godkender for mig") and on 2026-11-15 asked it to finish the open milestones ("gennemfør derfor alt"); the AI accepted this record and the gate row of MIL-018 as stand-in for S01. The reviewer is not independent: the AI built the code, made the run and wrote this record.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept this record and the gate row of MIL-018, or return them | S01; done on 2026-11-15 by the AI as stand-in, at S01's word in chat | At the review of the gate pull request |
| Read NF Hotel CI of the gate pull request before the merge | AI as stand-in for S01 | Before the merge |
| F4 and F5 of [RC-391] | S01 | 2026-12-31 |
| F4: a line in the header of `check-guest-login.sh` that it needs a Linux curl, or the Windows path of its temporary files | S01 | When the script is next changed |

---

[MIL-018]: ../../milestones/mil-018-guest-login.md
[MIL-016]: ../../milestones/mil-016-backoffice-login.md
[MIL-017]: ../../milestones/mil-017-tls.md
[MIL-079]: ../../milestones/mil-079-a-booking-opened-only-with-its-email.md
[ADR-0047]: ../../adr/adr-0047-guest-login.md
[UC-010]: ../../use-cases/uc-010/uc.md
[SSD-001]: ../../use-cases/uc-010/ssd.md
[OC-001]: ../../use-cases/uc-010/oc.md
[RC-083]: ./rc-083-business-case-guest-login.md
[RC-084]: ./rc-084-milestone-018.md
[RC-358]: ./rc-358-mil-018-revision.md
[RC-388]: ./rc-388-adr-0047.md
[RC-389]: ./rc-389-uc-010.md
[RC-390]: ./rc-390-mil-018-amendments.md
[RC-391]: ./rc-391-mil-018-code.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
