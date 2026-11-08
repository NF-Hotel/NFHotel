# MIL-067 Bad input on the booking page and the staff pages

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-067 |
| CrossReference | [BC-001], [US-001], [UC-004], [UC-005], [DSA-001], [ADR-0038], [MIL-027], [MIL-044], [MIL-066] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-08 | Deprecated | mikemike | S01 | Initial version: one task for each place where the web application answers bad input with an error page or sends back what it should not (the guests of the booking page, the run of the benchmark page, the password of the login form), from the full DAST scans of 2026-11-08; planned at S01's request in chat ("1.ja") and accepted by S01 in chat ("Gennemfør 067", RC-309) | [d677364] |
| 2026-11-08 | Accepted | mikemike | AI as stand-in for S01 | Finding 4 and task 7: the change-password form sends the typed passwords back as the login form did, found while task 4 was done; criterion 7, the row of US-001.10, and the gate against criteria 1 to 7; accepted by the AI as stand-in for S01 at S01's request in chat ("Ta selv stilling til alt", RC-310) | [6523bd3] |

---

## Purpose

Decide that the web application answers bad input in an address or a form with its page and a message, never with an error page, and that it never sends a typed password back to the browser.

The full dynamic application security testing (DAST) scan of 2026-11-08 on main (run 42477953901) raised a new alert, and the full scan of the branch of [MIL-066] task 7 (run 42479467906) raised it again. While the evidence in the reports of these scans was read for the ZAP rules of [MIL-066], two more places of the same kind were found in the code, and a fourth was found while task 4 was done.

| # | Finding | Where | What it allows or breaks |
| --- | --- | --- | --- |
| 1 | `GET /book?guests=<a number of 44 digits>` answers `500 Internal Server Error`: ZAP rule Integer Overflow Error (30003), rated Medium, on `/book?guests=2` and on `/book?checkIn=2026-11-09&checkOut=2026-11-10&guests=2`. `Book.razor.cs` binds `guests` as `int?` with `[SupplyParameterFromQuery]`, and Blazor throws when the value does not parse as an `Int32`, so the exception handler answers with the error page. The dates are strings that `BookingFlowViewModel.Prefill` parses, so a bad date only leaves the box empty | `src/NFHotel.Web/Features/Booking` | Anyone, without signing in, can make the public site answer 500 at will: errors in the logs, server errors in the monitoring, and a guest who mistypes the address gets an error page in place of the booking box |
| 2 | `BenchmarkStart.razor.cs` binds `again`, a globally unique identifier (GUID), as `Guid?` in the same way, so `/admin/benchmark/start?again=<not a GUID>` answers 500 to a signed-in staff member. Found in the code; the scans do not reach the page | `src/NFHotel.Web/Features/Admin/Benchmark` | The same error behind the sign-in |
| 3 | The login form can send the typed password back: `OnValidSubmit` runs `SignInAsync`, which clears `Input.Password`, only when the form is valid, and `InputText` writes its value into the `value` attribute of the field. A post with an empty user name and a password would return the password in the page. Found in the code; not yet reproduced | `src/NFHotel.Web/Features/Admin/Login` | The password travels back to the browser in the page of the answer (encrypted, and with `no-store`), where it does not belong ([UC-004]) |
| 4 | The change-password form sends the typed passwords back in the same way: `OnValidSubmit` runs `ChangeAsync`, which clears the three passwords, only when the form is valid. A post with two different new passwords returned the current password in its field. Found while task 4 was done, and shown with a test that was not kept | `src/NFHotel.Web/Features/Admin/Staff` | The current password of a signed-in staff member travels back to the browser in the page of the answer (encrypted, and with `no-store`), where it does not belong ([UC-005]) |

No ZAP rule is added: the alert is fixed, not accepted.

## Deliverable

- **The booking page** reads `guests` as text and lets the viewmodel parse it, as it does the dates: a value that is not a whole number, or that does not fit an `Int32`, is ignored and the box keeps its default; a whole number is kept between 1 and `MaxGuests` as today; the page answers 200.
- **The benchmark page** reads `again` as text and parses it in the same way.
- **A rule for every query parameter:** a test that fails when a property with `[SupplyParameterFromQuery]` in `NFHotel.Web` has a type other than `string`, so the error cannot come back with a new page.
- **The login form**, which never writes a posted password back into the page, whether the form is valid or not.
- **The change-password form**, which never writes a posted password back into the page either.
- **The proof:** the tests, the code review, the deploy, and a full DAST scan without the alert.

Order: the review of this document first (QC-MIL-001 and QC-LANG-001), then tasks 1 to 4 and 7 on one branch, task 5 with its pull request and the deploy, and last task 6. Task 7 was added when tasks 1 to 4 were done, so it has the next number and comes before task 5.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | Tests through `WebFactory` prove that `/book` answers 200 with the booking box when `guests` is larger than `Int32.MaxValue`, not a number, or empty, and that each of these leaves the default number of guests; that a whole number fills the box, kept between 1 and `MaxGuests`; and the tests of the values that do not parse fail against the code before the change | Yes | No |
| 2 | A test proves that `/admin/benchmark/start?again=<not a GUID>` answers the page to a signed-in staff member whom `StaffPolicies.BenchmarkRun` allows, and fails against the code before the change | Yes | No |
| 3 | A test fails when a property with `[SupplyParameterFromQuery]` in `NFHotel.Web` has a type other than `string`, and passes on the code after the change | Yes | No |
| 4 | A test posts the login form with an empty user name and a password and finds the password nowhere in the answer; the review record says whether it failed against the code before the change (finding 3 reproduced) or not | Yes | No |
| 5 | The code review against `QC-CS-001` is Go, the tests and the analyzers pass, and every required check of the pull request is green | Yes | No |
| 6 | The continuous deployment (CD) run of the merge, approved by S01 ([ADR-0038]), deploys and its smoke test passes; a full DAST scan after it, started by hand, has no Integer Overflow Error (30003) and no High alert | Yes | No |
| 7 | A test through `WebFactory` posts the change-password form with two different new passwords and finds none of the three passwords in the answer, and fails against the code before the change | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-066 | Its CD runs come first: one CD run at a time in `production` |
| MIL-027 | The DAST scans that found the alert, and the full scan that proves the fix |
| MIL-044 | The ZAP rules, to which this milestone adds none |
| DSA-001 | Section 8: a finding is fixed, or accepted by S01; this one is fixed |
| ADR-0038 | The CD run of the merge waits for the approval of S01 |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001 success criterion 31: from outside the tailnet, the site and `/book` answer 200 | Criteria 1, 3 and 6: `/book` answers 200 whatever its address holds |
| BC-001 success criterion 22: the back office login | Criteria 4 and 7: the login form and the change-password form keep the passwords out of their answers |
| US-001.09 Sign in to the back office | Task 4 |
| US-001.10 Change my password | Task 7 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-15, one week after the scan that found the alert; it does not move a date of the Business Case.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | The guests of the booking page | First a test through `WebFactory` that reproduces finding 1: `/book?guests=` with a number larger than `Int32.MaxValue`, or a value that is not a number, answers 500. Then `Book.razor.cs` reads `guests` as `string?`, and `BookingFlowViewModel.Prefill` parses it with `int.TryParse` in the invariant culture, as it parses the dates; a value that does not parse is ignored, and a whole number is kept between 1 and `MaxGuests` as today. The tests of criterion 1 pass. | No | criterion 1 |
| 2 | The run of the benchmark page | First a test with a signed-in staff member whom `StaffPolicies.BenchmarkRun` allows, which reproduces finding 2 on `/admin/benchmark/start?again=<not a GUID>`. Then `BenchmarkStart.razor.cs` reads `again` as `string?` and parses it with `Guid.TryParse`; a value that is not a GUID is ignored. | No | criterion 2 |
| 3 | A rule for every query parameter | A test in `NFHotel.Web.Tests` finds every property with `[SupplyParameterFromQuery]` in the assembly of `NFHotel.Web` by reflection and fails on a type other than `string`, so a new page cannot bring the error back; its message names the page and the property. | No | criterion 3 |
| 4 | The password of the login form | First a test that posts the login form with an empty user name and a password and looks for the password in the answer. When it finds it (finding 3), the login page clears `Input.Password` before each render after a post, whether the form is valid or not, and the test passes; when it does not, the test stays as a regression test and the review record says so. | No | US-001.09, criterion 4 |
| 5 | Code review, pull request and deploy | The review record of the C# changes and their tests against `QC-CS-001` before the pull request; the pull request closes one issue per task it completes and carries the milestone. The CD run of the merge, approved by S01 ([ADR-0038]), deploys the virtual machine; then one full DAST scan, started by hand. | No | criteria 5, 6 |
| 6 | Gate and review record | The gate review record against criteria 1 to 7 with the logs of the tests, the deploy and the DAST scan, the row of MIL-067 in the traceability matrix, the Accepted row of this document, and the closing of the GitHub milestone. | No | criteria 1 to 7 |
| 7 | The passwords of the change-password form | First a test through `WebFactory` that posts the change-password form with two different new passwords and looks for the three passwords in the answer; it reproduces finding 4. Then the page takes the three passwords out of the form whether the form is valid or not, as task 4 does for the login form, and the test passes. | No | US-001.10, criterion 7 |

---

[BC-001]: ../business-case.md
[US-001]: ../user-stories.md
[UC-004]: ../use-cases/uc-004/uc.md
[UC-005]: ../use-cases/uc-005/uc.md
[DSA-001]: ../sqa/devsecops-assessment.md
[ADR-0038]: ../adr/adr-0038-approval-before-a-deploy.md
[MIL-027]: ./mil-027-dast-and-end-to-end-tests.md
[MIL-044]: ./mil-044-security-overview.md
[MIL-066]: ./mil-066-findings-of-7-november.md
[d677364]: https://github.com/NF-Hotel/NFHotel/commit/d67736407551851d5d6afa01c13f53eed1fa3bee
[6523bd3]: https://github.com/NF-Hotel/NFHotel/commit/6523bd34d78cf3c019b6a60cac3adf1c42c1b0e4
