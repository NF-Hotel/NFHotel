# MIL-054 Test coverage of 95 % or more

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-054 |
| CrossReference | [BC-001], [ADR-0033], [MIL-047] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-01 | Accepted | mikemike | S01 | Initial version; S01 asked in chat on 2026-11-01 how to get the code coverage over 90 %, and to plan for close to 100 % where possible ("planlæg at ram næsten 100% hvis muligt"); S01 accepted the plan in chat ("Ja, godkend og byg") after RC-258 Go | [4395227] |

---

## Purpose

Decide whether the automated tests run enough of the product code that a change which breaks a screen, a rule or an endpoint is caught in CI before it is deployed. SonarQube Cloud measures 77.0 % line coverage on `main` (1,462 of 6,355 lines not run by a test, read on 2026-11-01). The rules of the domain and the use cases are covered (Domain 95 %, Application 93 %, Infrastructure 95 %); the gap is the web front end: `src/NFHotel.Web` is at 58 %, and the back-office pages for bookings and guests are at 0 %.

The target is **95 % or more** for the whole product code and **90 % or more** for each project under `src/`, held by a check in CI so it does not fall back. 100 % is not the target: the event handlers in the `@code` blocks of the Razor pages are run only by an interactive renderer, and the composition roots (`Program.cs`) only by a started host. Those lines are covered where an existing test host reaches them, and the rest is named in the final review, not hidden by an exclusion.

## Deliverable

- **Unit tests of the ViewModels** of the web front end (MVVM), with the use-case services faked by NSubstitute, as the existing ViewModel tests do: the admin bookings, rooms, guests, staff, dashboard, benchmark and market analysis pages, and the guest booking flow.
- **Component tests of the Razor pages and components** with the `HtmlRenderer` already used by `MarketChartComponentTests`: each page renders its states (loading, empty, a list, an error, a form with validation messages).
- **Tests of the remaining gaps** in the API host (`ApiStartup`, `MarketAnalysisRunWorker`) through the existing `WebApplicationFactory` of `NFHotel.Api.Tests`, and in Application and Domain (`ErrorCodes`, `BookingMarketDetail`, `RoomMapping` and the last branches of `BookingService`).
- **A defect found by a new test** is fixed in the same milestone, with the test kept as its regression test.
- **The coverage scope** stated once: code that is not product code is excluded with a reason (the developer script `scripts/dev-up.ps1`, the design-time `HotelDbContextFactory` used only by `dotnet ef`), in both `dotnet-coverage.config.xml` and `SonarQube.Analysis.xml`; ADR-0033 gets the new target and the scope.
- **A coverage floor in CI**: a step of the `build` job of `ci.yaml` that reads the Cobertura report and fails when the total line coverage is under 95 % or a project under `src/` is under 90 %.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | SonarQube Cloud shows 95.0 % or more line coverage on `main` after the merge | Yes | No |
| 2 | Each project under `src/` has 90 % or more line coverage in the Cobertura report of CI | Yes | No |
| 3 | The coverage floor step fails CI when the report is under the floor (shown by a test of the step with a report under the floor) and passes on `main` | Yes | No |
| 4 | Every new test is named `Method_Condition_Expected`, runs without the network and without a fixed sleep, and the unit and integration tests stay apart (BC-001 objective 15) | Yes | No |
| 5 | The only coverage exclusions are the generated migrations, `ops/` (tested in Ops CI), and the two files named in the deliverable, each with its reason in the configuration file | Yes | No |
| 6 | No code under `src/` changes except to fix a defect a new test found, and each such fix has its regression test | Yes | No |
| 7 | The code review against `QC-CS-001` is Go, and the lines still not covered are listed in it with the reason | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-047 | Coverage is measured in CI and imported into SonarQube Cloud |
| ADR-0033 | The record of how coverage is measured and gated; amended by task 1 |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001 objective 15: the tests follow the test rules of the coding conventions | Tasks 2 to 8 |
| BC-001 objective 14: the tests run on xUnit v3 | Tasks 2 to 8 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-22. It adds tests and changes no behaviour, so it does not move a date of the Business Case.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Coverage scope and target | Exclude `scripts/dev-up.ps1` and the design-time `HotelDbContextFactory` from coverage in `dotnet-coverage.config.xml` and `SonarQube.Analysis.xml` with the reason, and amend ADR-0033 with the target of 95 % in total and 90 % per project. | No | criterion 5 |
| 2 | Tests of the admin bookings pages | `BookingFormViewModel`, `BookingListViewModel` and `BookingList.razor` are at 0 % (310 lines): unit tests of create, edit, status changes, validation and conflicts, and component tests of the list. | No | criteria 1, 2, 4 |
| 3 | Tests of the guest booking flow | `BookingFlowViewModel`, `BookingReceiptViewModel`, `GuestFieldErrors`, `BookingNumberText`, `Book.razor` and `BookingReceiptCard.razor` (234 lines, 17 %): each step of the flow, the field errors and the receipt. | No | criteria 1, 2, 4 |
| 4 | Tests of the admin rooms pages | `RoomListViewModel`, `RoomFormViewModel` and `RoomList.razor` (211 lines, 19 %): filters, create and edit, status changes and their refusals. | No | criteria 1, 2, 4 |
| 5 | Tests of the admin guests page | `GuestListViewModel` and `GuestList.razor` (158 lines, 0 %): search, paging, edit and the errors from the use case. | No | criteria 1, 2, 4 |
| 6 | Tests of the benchmark and market analysis pages | `BenchmarkRuns`, `BenchmarkStart`, `ModelsPanel` and `MarketAnalysisReport` (about 160 lines): the states of each page and the run list. | No | criteria 1, 2, 4 |
| 7 | Tests of the shared web code, staff and dashboard | `BadgeClass`, `DisplayText`, `UseCaseRunner`, `StaffPolicies`, `StatusBadge`, `StaffList`, `StaffAudit` and `AdminDashboard` (about 150 lines). | No | criteria 1, 2, 4 |
| 8 | Tests of the API host and the last Application and Domain gaps | `ApiStartup` and `MarketAnalysisRunWorker` through the test host; `ErrorCodes`, `BookingMarketDetail`, `RoomMapping`, `BookingService` and the repositories. | No | criteria 1, 2, 4, 6 |
| 9 | Coverage floor in CI and code review | The step in `ci.yaml` that fails under 95 % in total or 90 % per project, with a test of the step; the review record against `QC-CS-001` listing the lines left uncovered and why. | No | criteria 3, 7 |

---

[BC-001]: ../business-case.md
[ADR-0033]: ../adr/adr-0033-sonarqube-cloud-coverage.md
[MIL-047]: mil-047-sonarqube-cloud-coverage.md
[4395227]: https://github.com/NF-Hotel/NFHotel/commit/4395227efef641b6f8939f00e52c3640334b1032
