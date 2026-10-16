# RC-050 Review of the move to xUnit v3

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-050 |
| CrossReference | [MIL-011], [QC-CS-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- Instance reviewed: the four test projects of `tests/` after tasks 2 to 5 of [MIL-011]: `xunit.v3` 4.0.2 in place of `xunit` 2.9.3, `OutputType` `Exe`, `ValueTask` in `IAsyncLifetime`, `TestContext.Current.CancellationToken` at 24 call sites, a source-information constructor in `LiveFactAttribute`, the test-platform opt-in in `global.json`, and the same opt-in in the Dockerfile of the Proxmox tests
- Checklist used: [QC-CS-001]
- Scope: full review
- Language and domain: n/a (source code, technical type)
- Language reviewer: none
- Mode: autonomous, not independent (reviewer is `AI as stand-in for S01`)
- Tools: `dotnet build` (0 warnings, 0 errors), `dotnet format --verify-no-changes`, `dotnet test --solution NFHotel.sln`, a `docker build` and run of the Proxmox test image

## Go / No-Go criteria of MIL-011

| # | Criterion | Result | Evidence |
| --- | --- | --- | --- |
| 1 | No reference to `xunit` 2.x; each test project references `xunit.v3` | Pass | A search of `*.csproj`, `*.props` and `*.targets` shows only `xunit.v3` |
| 2 | Same test count per project | Pass | 285 + 15 + 47 + 12 = 359 before and after; 3 skipped before and after |
| 3 | 0 build warnings; format clean | Pass | See Tools |
| 4 | `src/` unchanged, `tests/` diff limited to what v3 requires | Pass with a note | No source file of `src/` was changed by this milestone. `src/NFHotel.Infrastructure/NFHotel.Infrastructure.csproj` carries the owner's package update to EF Core 10 / Npgsql 10, made before this milestone and needed because the test projects already reference EF Core 10 packages; it is committed with this change and is not part of it |
| 5 | Runs from the command line and from CI | Pass | `dotnet test --solution` passes. The CI workflow only runs `dotnet build NFHotel.sln` and is unaffected |
| 6 | This review has the verdict `Go` | Pass | Below |

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Naming follows the conventions | Pass | No identifiers changed except the `ValueTask` return types of the lifetime methods |
| 2 | Async methods end in `Async`; exceptions end in `Exception`; one top-level type per file | Pass | `InitializeAsync` and `DisposeAsync` keep their names; no file gained a type |
| 3 | Formatted per `.editorconfig`, no unexplained suppressions | Pass | `dotnet format --verify-no-changes` clean; the xUnit analyzers (xUnit1051, xUnit3003) are satisfied by code, not suppressed |
| 4 | Nullable enabled; `!` justified in a comment | Pass | Unchanged; no `!` added |
| 5 | `IDisposable` disposed | Pass | Unchanged |
| 6 | No `.Result`, `.Wait()` or `async void` | Pass | Unchanged |
| 7 | Specific exceptions, validated boundaries, no empty catch | N-A | Not touched |
| 8 | Public types have XML documentation | N-A | Test code |
| 9 | Properties not public fields; immutability | N-A | Not touched |
| 10 | Money `decimal`; time `DateTimeOffset` | N-A | Not touched |
| 11 | Public async APIs take a `CancellationToken` | Pass | 24 test calls now pass `TestContext.Current.CancellationToken`, the xUnit v3 way to make a test cancellable |
| 12 | Tests cover new behaviour, named `Method_Condition_Expected`, no dependence on order, time or network | Pass | No test added, removed or renamed. Count before and after: Domain 285, Application 15, Infrastructure 47, Proxmox 12 (9 passed, 3 skipped as before without `PVE_HOST`); total 359 |
| 13 | Classes trace to the Design Class Diagram | N-A | No class of the product changed |
| 14 | Each piece of knowledge is defined once | Pass | The opt-in to the test platform is in `global.json` once; the Proxmox test image repeats it because its build context has no `global.json` (stated in a comment) |
| 15 | References point inward, no cycles | Pass | No reference changed except the package `xunit` to `xunit.v3` |
| 16 | SOLID | N-A | Not touched |
| 17 | Layer folders | N-A | No folder changed |
| 18 | File and folder names | Pass | No file added or renamed in `tests/` |

## Overall Verdict

Go — every applicable criterion of QC-CS-001 and every Go criterion of MIL-011 passes. Two things a reader must know: on the .NET 10 SDK `dotnet test` runs xUnit v3 on the Microsoft Testing Platform, so `global.json` opts in with `"test": {"runner": "Microsoft.Testing.Platform"}`; and `.runsettings` and `coverlet.collector` belong to the older VSTest path and do not apply to the command-line run (the coverage settings still work in an IDE that uses VSTest).

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Decide whether coverage is wanted on the command line; it then needs `coverlet.MTP` or the Microsoft code-coverage extension | S01 | Open |

---

[MIL-011]: ../../milestones/mil-011-xunit-v3.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
