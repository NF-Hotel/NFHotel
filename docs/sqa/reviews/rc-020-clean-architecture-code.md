# RC-020 Review of the clean architecture restructuring

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-020 |
| CrossReference | [MIL-005], [ADR-0001], [QC-CS-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S01 | Initial version | [9b18534] |

---

## Artifact Under Review

- Instance reviewed: `src/NFHotel.Domain`, `src/NFHotel.Application`, `src/NFHotel.Infrastructure` (restructured), the Web project (compile fixes only) and the tests, implementing tasks 3 to 8 of [MIL-005] as decided in [ADR-0001]
- Checklist used: [QC-CS-001]
- Scope: full review
- Language and domain: n/a (source code, technical type)
- Language reviewer: none
- Mode: autonomous, not independent (reviewer is `AI as stand-in for S01`)
- Tools: `dotnet build` (0 warnings, 0 errors), `dotnet format --verify-no-changes`, `dotnet ef migrations has-pending-model-changes`, `dotnet test` (all projects)

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Naming follows the conventions | Pass | PascalCase types, `_camelCase` private fields; `Entity<TKey>`, `Repository<TEntity, TKey>`, `EntityConfiguration<TEntity, TKey>` |
| 2 | Async methods end in `Async`; exceptions end in `Exception`; one top-level type per file | Pass | A scan of the three projects found no file with more than one top-level type; an architecture test checks `Dto/` |
| 3 | Formatted per `.editorconfig`, no unexplained suppressions | Pass | `dotnet format --verify-no-changes` clean; IDE0005 (unused usings) is now a warning in `.editorconfig`, with `TreatWarningsAsErrors` |
| 4 | Nullable enabled; `!` justified in a comment | Pass | The three uses of `!` in the changed projects (`Entity.Id`, `BookingService`, `HolidayApiOptions`) have a comment; the two earlier ones were added in this change |
| 5 | `IDisposable` disposed | Pass | Test contexts in `using` |
| 6 | No `.Result`, `.Wait()` or `async void` | Pass | Unchanged |
| 7 | Specific exceptions, validated boundaries, no empty catch | Pass | `ArgumentNullException.ThrowIfNull` in the new base classes |
| 8 | Public types have XML documentation | Pass | `GenerateDocumentationFile` with warnings as errors in the four projects |
| 9 | Properties not public fields; immutability | Pass | `Id` has a protected setter |
| 10 | Money `decimal`; time `DateTimeOffset` | N-A | Not touched |
| 11 | Public async APIs take a `CancellationToken` | Pass | `IRepository.GetByIdAsync` and the queries |
| 12 | Tests cover new behaviour, named `Method_Condition_Expected` | Pass | 6 architecture tests, 11 composition tests, 2 key-column tests added; 285 domain tests unchanged and green |
| 13 | Classes trace to the Design Class Diagram; deviations recorded | Pass | The deviations from the earlier structure are recorded in [ADR-0001]; the Danish `docs/dcd.md` is not updated (action item 1) |
| 14 | Each piece of knowledge defined once | Pass | The key column rule is in `EntityConfiguration`; get, add and update are in `Repository<TEntity, TKey>`; the three repositories only add their own queries |
| 15 | Namespace and project references point inward | Pass | `Domain` references nothing (existing test), `Application` references only `Domain`; the repository interfaces moved to `Domain` and no DTO is referenced there |
| 16 | SOLID holds | Pass | Narrow query ports per entity beside the entity repositories; one reason to change per class |
| 17 | Layout follows the clean architecture folder structure | Pass | `Entities`, `ValueObjects`, `Repositories`, `Errors` (+ `Rules`) in `Domain`; `UseCases`, `Dto/<Entity>`, `Ports`, `Errors` in `Application`; `Persistence`, `Repositories`, `Config` in `Infrastructure`; `Rules` and the unchanged `Security`, `Time`, `Holidays` folders of the adapters are recorded in the ADR. Enforced by `StructureTests` |
| 18 | File and folder names are English, no space, language casing | Pass | |

## Overall Verdict

Go — all Mandatory criteria pass or are not applicable with a stated reason. Evidence on 2026-10-16: build 0 warnings; Domain 285, Application 12, Infrastructure 47 (PostgreSQL in a container included) and Proxmox 9 tests pass; no new migration; the Docker smoke test (nginx, web and analysis service) passes 9 checks with the restructured web application.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| 1. Update `docs/dcd.md` (Danish) to the new folder and base type names | S01 | 2026-10-30 |
| 2. Decide whether `Security`, `Time` and `Holidays` in `Infrastructure` move into skill folders (they are adapters; the skill lists `Persistence`, `Repositories`, `Messaging`, `Config`) | S01 | 2026-10-30 |

---

[MIL-005]: ../../milestones/mil-005-clean-architecture.md
[ADR-0001]: ../../adr/adr-0001-clean-architecture-structure.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[9b18534]: https://github.com/NF-Hotel/NFHotel/commit/9b185345b6fb55c37dcf95516495448f1af4440c
