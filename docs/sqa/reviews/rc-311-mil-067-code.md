# RC-311 Code review of MIL-067: the bad input of the booking page and the staff pages

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-311 |
| CrossReference | [MIL-067], [QC-CS-001], [RC-310], [RC-278] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-08 | Accepted | mikemike | AI as stand-in for S01 | Initial version; mode autonomous, not independent; Go-with-conditions until every required check of the pull request is green | [3ed21af] |

---

## Artifact Under Review

- Instance reviewed: the code of [MIL-067] tasks 1 to 4 and 7 on the branch `mil-067-bad-input` (b206e5a, 88d8b82, 5b9fba9, 5d8fcf1, edf9d8e): `Book.razor`, `Book.razor.cs` and `BookingFlowViewModel.cs` of the booking page; `BenchmarkStart.razor.cs`; `Login.razor` and `Login.razor.cs`; `ChangePassword.razor` and `ChangePassword.razor.cs`; and the tests in `tests/NFHotel.Web.Tests`: `BookingAddressTests` and `QueryParameterTests` (new), `BookingFlowViewModelTests`, `AdminAccessTests`, `BenchmarkStartPageTests` and `StaffPasswordPageTests`
- Checklist used: [QC-CS-001] (C# and Razor), and the criteria of [MIL-067]
- Scope: full review of the changed code
- Language and domain: n/a (code)
- Language reviewer: none
- Mode: autonomous, not independent. S01 accepted the plan in chat ("Gennemfør 067", RC-309) and handed the decision on task 7 to the AI ("Ta selv stilling til alt", [RC-310]); the reviewer is the AI that wrote the code, so every decision below stays open to S01's challenge.

**Evidence run on 2026-11-08** (S01's PC, Windows 11, .NET SDK 10.0.403, Docker Desktop 29.7.2):

- Each test was run against the code before its change and failed as the plan says:
  - task 1: `BookingAddressTests` answered 500 for `guests` set to the 44 digits of the scan, `2147483648`, `two` and `2.5`, and for the address with dates (`System.InvalidOperationException: Cannot parse the value '…' as type 'System.Nullable`1[System.Int32]' for 'guests'`); the empty value and the seven whole numbers passed (13 tests, 5 failed);
  - task 2: `/admin/benchmark/start?again=` answered 500 to a Manager for `not-a-run` and `12345`, and the page host test of a value that is not a run threw the same exception for `Guid?`; the empty value and the page host test of a run that the address names passed (13 tests, 3 failed);
  - task 3: the rule failed with "Read these query parameters as text and parse them in their page: NFHotel.Web.Features.Booking.Book.GuestsQuery is Int32?; NFHotel.Web.Features.Admin.Benchmark.BenchmarkStart.Again is Guid?.";
  - task 4: the post without a user name returned `value="a passphrase that stays in the browser"` in the password field (finding 3 reproduced); the post with a wrong password did not;
  - task 7: both posts that the form refuses returned the typed passwords: the current one, the new one and the repeat for two different new passwords, and the new one twice for a missing current password (finding 4 reproduced).
- After the changes, `dotnet test` of `NFHotel.Web.Tests`: 731 passed, 0 failed.
- `dotnet build NFHotel.sln --configuration Release`: build succeeded, warnings as errors, 0 warnings.
- `dotnet test NFHotel.sln --configuration Release --no-build`: 2553 tests, 2541 passed, 12 skipped (the end-to-end tests of `NFHotel.E2E.Tests`, which need `E2E_BASE_URL`), 0 failed, 46 s; the integration tests ran against Docker.
- `dotnet format whitespace NFHotel.sln --verify-no-changes --no-restore`: no change.
- The plan gate: the commit hooks do not run in this worktree, whose `framework` folder is empty, so `check-plan.sh` of the framework was run by hand on each commit message after its files were staged. It passed the five messages, each with its `Task: MIL-067#N` trailer, and refused a message without one.
- The local SDK (10.0.403) rewrites `src/NFHotel.Web/packages.lock.json` on a restore (`Microsoft.AspNetCore.App.Internal.Assets` 10.0.11 to 10.0.14), the trap that [RC-278] records; the file was restored and is in no commit.
- The checks of the pull request (NF Hotel CI, CodeQL, SonarQube Cloud) are read after the push; they are criterion 5 of MIL-067, which this record cannot hold.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | `PascalCase` types, methods, properties and constants; `camelCase` parameters and locals; `_camelCase` private fields; `I` interfaces | Pass | New members `GuestsQuery`, `AgainQuery`, `TryParseQueryGuests`, `ForgetPassword` and `ForgetPasswords`; locals `party`, `again`, `password`, `current` and `fresh`; test constants `ScanValue` and `DeclaredProperties` |
| 2 | `Async` and `Exception` suffixes; one top-level type per file, named after the file | Pass | The async methods keep their names (`SignInAsync`, `ChangeAsync`, `OnInitializedAsync`); the two new test classes are in `BookingAddressTests.cs` and `QueryParameterTests.cs` |
| 3 | Formatted per `.editorconfig`, no unexplained analyzer suppressions, explicit types | Pass | The format check passes; the Release build has 0 warnings with warnings as errors; no suppression added. The changed lines keep `var` where their files already use it, as rule 1 of the coding conventions says; the project-wide use of `var` predates this milestone (RC-266, [RC-278]) |
| 4 | Nullable reference types enabled; `!` justified in a comment | Pass | No `!` added; `Input?.Password ?? string.Empty` reads the form without one |
| 5 | `IDisposable` disposed with `using`; no leaks on exception paths | Pass | The tests dispose every `HttpClient`, `HttpResponseMessage` and `FormUrlEncodedContent` with `using`; the source adds no disposable |
| 6 | `await` throughout; no `.Result`, `.Wait()` or `async void` | Pass | None added |
| 7 | Specific exceptions, validated arguments, `throw;`, no empty `catch` | Pass | The values of the address are read with `int.TryParse` and `Guid.TryParse`, so a bad value takes no exception path; no throw or catch added |
| 8 | XML documentation comments on public types and members | Pass | `GuestsQuery`, `AgainQuery` and `Prefill` say that the value is text and why; the private `TryParseQueryGuests`, `ForgetPassword`, `ForgetPasswords`, `SignInAsync` and `ChangeAsync` have comments too, and so do the new tests |
| 9 | State through properties; immutability where it fits | Pass | The parameters of the address stay properties, as Blazor needs |
| 10 | `decimal` for money; `DateTimeOffset` for points in time | N-A | No money and no point in time |
| 11 | Public async APIs accept and pass a `CancellationToken` | Pass | The changed calls pass `ComponentToken` as before |
| 12 | Tests cover new behaviour, are named `Method_Condition_Expected`, and do not depend on order, time or the network | Pass | Every task has its tests, each run against the code before the change (above): positive (a whole number fills the box; a run of the address gives its choice), negative (values that do not parse; posts that the form refuses), boundary (one above the largest integer, the largest and the smallest integer, 0, 4 and 5), edge (an empty value, white space), equivalence (a word, a decimal, a missing field, a repeat that differs), regression (the value of the scan, findings 3 and 4). The tests through `WebFactory` run in the process, and none of their addresses asks the API (`http://api.invalid/`); the one test with dates uses 2020, which is in the past on any day |
| 13 | Classes and members trace to the Design Class Diagram | N-A | The changes are in the presentation layer, which the Design Class Diagrams do not draw; no class or operation of the design changed |
| 14 | Each piece of knowledge in one place | Pass | The party of the address is read in one method beside the dates (`TryParseQueryGuests` next to `TryParseQueryDate`); each form empties its passwords in one method. `ForgetPassword` and `ForgetPasswords` look alike but belong to two forms whose fields change for different reasons, so they are not merged |
| 15 | References point inward | Pass | The changes stay in `NFHotel.Web`; no reference added |
| 16 | SOLID holds | Pass | Each new method has one job; no type test added |
| 17 | Clean architecture layout | Pass | Two test files added to the existing project `tests/NFHotel.Web.Tests`, beside the tests of the same pages; no file moved |
| 18 | File and folder names | Pass | `BookingAddressTests.cs`, `QueryParameterTests.cs` |
| 19 | Observers through `event` and delegates | N-A | No event |
| 20 | SIMD and CUDA | N-A | No acceleration |
| 21 | Every type under `src/` groups its members in `#region` blocks | Fail | None of the five changed source files has a region, and none of the 462 C# files under `src/` outside the migrations has one: the criterion was never applied to the code that predates it ([RC-278] records the same). This milestone adds no type and splits no member. Covered by the risk exception below |

## Criteria of MIL-067

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | `/book` answers 200 with the box for values that do not parse and keeps the default; a whole number fills the box within its bounds; red before the change | Pass | `BookingAddressTests` (13 tests) and the two theories of `BookingFlowViewModelTests`; red before as above |
| 2 | `/admin/benchmark/start?again=<not a GUID>` answers the page to a staff member whom `StaffPolicies.BenchmarkRun` allows; red before the change | Pass | `AdminAccessTests.Get_BenchmarkStartAgainNotARun_ShowsThePage` (a Manager) and `BenchmarkStartPageTests.Again_NotARun_OpensThePageAsWithout`; red before |
| 3 | A test fails on a query parameter of a type other than `string`, and passes after the change | Pass | `QueryParameterTests.QueryParameters_OfEveryPage_AreText` named both typed parameters before tasks 1 and 2, and passes now |
| 4 | The login form without a user name sends no password back; the record says whether it failed before | Pass | `AdminAccessTests.SignIn_EmptyUserName_DoesNotSendThePasswordBack` failed before the change: finding 3 reproduced |
| 5 | The code review is Go, the tests and the analyzers pass, and every required check of the pull request is green | Pending | This record and the local runs above; the checks of the pull request are read after the push |
| 6 | The CD run approved by S01 deploys; a full DAST scan has no 30003 and no High alert | Pending | After the merge; the gate (task 6) records it |
| 7 | The change-password form with two different new passwords sends none of the three back; red before the change | Pass | `StaffPasswordPageTests.Post_ChangeRefusedByTheForm_DoesNotSendThePasswordsBack` (two cases) failed before the change: finding 4 reproduced |

## Findings

- **Findings 3 and 4 were real.** Both forms wrote a typed password back into its field when their validation refused the post: `InputText` writes its value into the page, and the passwords were cleared only in the handler of a valid form. Both forms now empty their passwords on an invalid submit too, and take them out of the form before the use case is asked, so the passwords are gone whatever the answer.
- **The guests are read as Blazor read them.** `int.TryParse` with `NumberStyles.Integer` and the invariant culture reads white space around the number and a leading sign (" 3 " is 3, and "-1" is kept at 1), as a test records; what did not parse before now leaves the default instead of an error page.
- **An empty value was never the fault.** Blazor reads an empty value of a nullable parameter as no value, so the empty `guests` and `again` passed before the change; they stay as edge cases.
- **The four commits of tasks 1 to 4 were rebased** onto the change of the plan ([RC-310]) before they were pushed, so the hashes in this record are those of the branch.
- **`AdminAccessTests.cs` has doubled blank lines in two of its tests**, from before this milestone; they were left alone, as rule 1 of the coding conventions says.

## Risk Exceptions

| Finding | Owner | Approver | Expiry | Compensating controls |
| --- | --- | --- | --- | --- |
| QC-CS-001 criterion 21 (Mandatory): the five source files changed by MIL-067, like every C# file under `src/`, have no `#region` blocks; a layout change of every file is outside the plan of MIL-067 | S01 | S01 (the AI as stand-in at S01's request in chat, "Ta selv stilling til alt"; autonomous mode, open to S01's challenge) | 2026-12-31 | The changed members are small and commented; the files are as before this milestone in that respect; the exception of [RC-278] for the same criterion runs to the same date, and the regions of `src/` are a task of their own |

## Overall Verdict

Go-with-conditions. Every Mandatory criterion of QC-CS-001 passes except criterion 21, which the risk exception covers, and criteria 1 to 4 and 7 of MIL-067 pass with tests that failed against the code before the change. The condition is criterion 5 of MIL-067: every required check of the pull request is green. The gate record of MIL-067 names the run and states that the condition is met.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Read the checks of the pull request, and record in the gate that the condition is met | AI as stand-in for S01 | At the merge |
| Approve the CD run of the merge (ADR-0038) | S01 | At the merge |
| Start the full DAST scan by hand after the deploy (criterion 6) | AI as stand-in for S01 | After the deploy |
| Put the members of the files under `src/` in `#region` blocks, or propose upstream to change criterion 21 | S01 | 2026-12-31 |

---

[MIL-067]: ../../milestones/mil-067-bad-input-on-the-web-pages.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[RC-310]: ./rc-310-mil-067-task-7.md
[RC-278]: ./rc-278-mil-058-code.md
[3ed21af]: https://github.com/NF-Hotel/NFHotel/commit/3ed21af7d6e4ceaaae49ebfd4da2c8e40eec7071
