# MIL-011 Move the tests from xUnit v2 to xUnit v3

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-011 |
| CrossReference | [BC-001], [MIL-010] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S01 | Accepted after RC-049 Go | pending |

---

## Purpose

Decide whether the four test projects can move from xUnit v2 (`xunit` 2.9.x) to
xUnit v3 (`xunit.v3`) without losing or changing a test. xUnit v2 is in
maintenance only; v3 is the supported line, runs each test project as its own
executable and removes the `Xunit.Abstractions` package namespace. The test
code and the code under test keep their behaviour.

## Deliverable

- `NFHotel.Domain.Tests`, `NFHotel.Application.Tests`, `NFHotel.Infrastructure.Tests` and `NFHotel.Proxmox.Tests` reference `xunit.v3` and the matching `xunit.runner.visualstudio`, with no reference to `xunit` 2.x.
- The source changes xUnit v3 requires: `OutputType` `Exe` for each test project, `ValueTask` instead of `Task` in `IAsyncLifetime`, and `ITestOutputHelper` from the `Xunit` namespace.
- The same tests, in the same classes, with the same names.
- Unchanged: the production code under `src/`, test data, test names and the PostgreSQL and Proxmox test fixtures' behaviour.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | No `.csproj`, props or targets file references the package `xunit` (2.x) or `xunit.abstractions`; each test project references `xunit.v3` | Yes | No |
| 2 | `dotnet test` on the solution reports the same number of tests as before the change, per project, none skipped that ran before | Yes | No |
| 3 | `dotnet build` has 0 warnings and `dotnet format --verify-no-changes` is clean | Yes | No |
| 4 | `git diff` of `src/` is empty; the diff of `tests/` shows only project files, usings, fixture signatures and what xUnit v3 requires | Yes | No |
| 5 | The solution runs from the command line and from the repository CI (`.github/workflows/ci.yaml`) with the same result | Yes | No |
| 6 | A review record against `qc-programming-csharp` has the verdict `Go` | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-010 | It touched the comments of the same test files; the test files are moved after it to avoid conflicts |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| New objective 14 of an amended BC-001: the tests run on the supported xUnit line (v3) | All tasks |
| Success criterion 17 | Criteria 1 to 5 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-12-11 — a week after MIL-010 (2026-12-04); four small test projects (16 test files, 5 fixture or lifetime uses).

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Amend Business Case and Project Plan | Add objective 14, the scope item, success criterion 17 and the MIL-011 schedule row; review again. | No | |
| 2 | Move the Domain tests | `NFHotel.Domain.Tests`: package references, `OutputType`, usings. | No | |
| 3 | Move the Application tests | `NFHotel.Application.Tests`: same changes. | No | |
| 4 | Move the Infrastructure tests | `NFHotel.Infrastructure.Tests`: same changes, plus `IAsyncLifetime` returning `ValueTask` and the PostgreSQL collection fixture. | No | |
| 5 | Move the Proxmox tests | `NFHotel.Proxmox.Tests`: same changes; the tests that need a Proxmox container stay as they are. | No | |
| 6 | Check and review | Build, format and test the whole solution; compare the test counts with the counts before; check CI; record an RC against `qc-programming-csharp`. | No | |

---

[BC-001]: ../business-case.md
[MIL-010]: ./mil-010-english-source-comments.md
