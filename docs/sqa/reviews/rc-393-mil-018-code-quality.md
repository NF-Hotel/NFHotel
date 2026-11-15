# RC-393 Review of the code-quality follow-up of MIL-018 against QC-CS-001

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-393 |
| CrossReference | [MIL-018], [ADR-0047], [QC-CS-001], [RC-391], [RC-392] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-15 | Accepted | mikemike | AI as stand-in for S01 | Initial version: review of the fixes for the two notes of GitHub code quality on pull request 774 against QC-CS-001; Go; mode autonomous, not independent | [ba23b07] |

---

## Artifact Under Review

- Instance reviewed: the branch `mil-018-code-quality` (from `main` at d73ea44). It answers the two notes that GitHub
  code quality (CodeQL) left on the last head of pull request 774 (0a4fe15) on 2026-11-15 at 21:00Z, before its merge
  (F1):
  - `GuestClaims.BookingIdOf` in `src/NFHotel.Web/Security/GuestClaims.cs` (task 5 of [MIL-018], the guest cookie):
    the note "Constant condition" on the null-conditional `guest?.` after `TokenOf(guest) is not null`, which is true
    only for a principal that is not null (F2). The method now checks `guest is not null` first and reads the claim
    with `guest.FindFirst`.
  - `AccessControlMatrixTests.ReadMatrix` in `tests/NFHotel.Application.Tests/Security/AccessControlMatrixTests.cs`
    (the permission table test of criterion 6, task 3): the note "Missed opportunity to use Select" on the loop that
    mapped each line to its match. The lines are now mapped with `Select` and kept with `Where`; the body of the loop,
    its assertion and the rows it returns are unchanged.
- Checklist used: [QC-CS-001] (`framework/qc/code/qc-programming-csharp.md`), all 21 criteria, for the changed lines.
- Scope: the two methods. The rest of both files stands as [RC-391] reviewed it.
- Language and domain: n/a for a technical type.
- Language reviewer: none.
- Mode: autonomous, not independent. S01 turned on Auto-fix for pull request 774 and asked in chat on 2026-11-15 that
  the open milestones be finished in autonomous mode ("gennemfør derfor alt"); the AI wrote the fix, ran the checks
  below and wrote this record.

### Checks that were run

| Check | Result |
| --- | --- |
| `dotnet build NFHotel.sln --configuration Release -warnaserror` | Build succeeded, 0 Warning(s) |
| `dotnet format NFHotel.sln --verify-no-changes` | exit 0: nothing to change in the whole solution |
| `dotnet test`, NFHotel.Web.Tests, `GuestClaimsTests` among them (a valid guest, and each kind of principal that is not a valid guest) | 834 passed, 0 failed |
| `dotnet test`, NFHotel.Application.Tests, `AccessControlMatrixTests` among them | 730 passed, 0 failed |
| Not run here | the integration and end-to-end tests and the CodeQL analysis: NF Hotel CI and the CodeQL default setup run them on the pull request |

## Checklist Results: QC-CS-001

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Types, methods, properties and constants are `PascalCase`; parameters and locals `camelCase`; private fields `_camelCase`; interfaces start with `I` | Pass | The new local `permissionRows`; the lambda parameters `line` and `match` |
| 2 | Async methods end in `Async`; exceptions end in `Exception`; one top-level type per file, named after the file | Pass | No method or type added or renamed |
| 3 | Code is formatted per the project's `.editorconfig` and builds with no unexplained analyzer suppressions; locals, `foreach` variables and `out` arguments have an explicit type, never `var` | Pass | Format clean, no suppression; `IEnumerable<Match> permissionRows`, `Match match` and `out int bookingId` |
| 4 | Nullable reference types are enabled; the null-forgiving operator (`!`) is justified in a comment | Pass | No `!` added: the check `guest is not null` gives the compiler the state that `guest?.` stood in for, and the build has no nullable warning |
| 5 | `IDisposable` objects are disposed with `using`; no resource leaks on exception paths | N-A | No disposable object in the changed lines |
| 6 | Async code uses `await` throughout: no `.Result` or `.Wait()` and no `async void` outside event handlers | N-A | No async code in the changed lines |
| 7 | Specific exceptions are thrown; arguments are validated at public boundaries; rethrow uses `throw;`; no empty `catch` | Pass | `BookingIdOf` takes a missing principal by its contract and returns `null` for it, as before |
| 8 | Public types and members have XML documentation comments | Pass | The summary, parameter and return comments of `BookingIdOf` stand and still describe it |
| 9 | State is exposed through properties, not public fields; immutability (`readonly`, `init`, `record`) is preferred where it fits | N-A | No state in the changed lines |
| 10 | Money uses `decimal`; points in time use `DateTimeOffset` | N-A | Neither in the changed lines |
| 11 | Public async APIs accept and pass a `CancellationToken` where the operation can be cancelled | N-A | No async API in the changed lines |
| 12 | Tests cover new behaviour, are named `Method_Condition_Expected`, and do not depend on order, time or the network | Pass | No new behaviour (F2). `GuestClaimsTests` covers each branch of `BookingIdOf`: a valid guest, and `BookingIdOf_NoValidGuest_IsNull` for each kind of principal that is not one; every test of `AccessControlMatrixTests` reads the matrix through `ReadMatrix` |
| 13 | Classes and members trace to the Design Class Diagram they implement; deviations are recorded | Pass | No signature changed |
| 14 | Each piece of knowledge is defined in one place; code is merged only where it expresses the same knowledge | Pass | `TokenOf` stays the one reader of the token, and `BookingIdOf` still asks it |
| 15 | Namespace and project references point inward: business-rule code references no framework, database, UI or delivery-mechanism code, and there are no reference cycles | Pass | No reference changed |
| 16 | SOLID holds | Pass | No responsibility moved |
| 17 | Source is laid out in the layers and folders of the clean architecture folder structure | Pass | No file moved |
| 18 | File and folder names are English, contain no space and no character outside letters, digits, `-`, `_` and `.`, and follow the casing of the language for files | Pass | No file added or renamed |
| 19 | Observers are notified through the `event` keyword and delegates | N-A | No observer or event |
| 20 | Code optimised with SIMD or CUDA keeps a scalar reference implementation | N-A | No such code |
| 21 | Every type under `src/` groups its members in `#region` blocks in the standard order and names | Pass | `BookingIdOf` stays in `#region Public methods` |

## Findings

- **F1, note: the notes came before the merge and were not read.** GitHub code quality posted both notes at 21:00Z.
  The AI merged pull request 774 at 21:19:55Z on its green checks and its passed SonarCloud gate, without reading
  the review threads, and Auto-fix reported the notes only after the merge. Neither note changed a result. The pull
  request of this branch answers both threads.
- **F2, note: no behaviour change.** `TokenOf` returns a token only for an authenticated principal that is not null,
  so after `TokenOf(guest) is not null` the principal cannot be null, and CodeQL read the `?.` that followed as a
  check that never fails. The compiler's nullable analysis does not see through the call, which is why the code had
  `?.`. With the explicit check first, `BookingIdOf` returns the same booking key, or `null`, for every input as
  before; the tests of the method pass unchanged.

## Overall Verdict

Go. Every applicable criterion of [QC-CS-001] passes; 5, 6, 9 to 11, 19 and 20 do not apply to the changed lines. F1
and F2 are notes. S01 asked in chat on 2026-11-15 that the open milestones be finished in autonomous mode, and the AI
accepted this record as stand-in for S01. Not verified here: the integration and end-to-end tests and the CodeQL
analysis, which run on the pull request. The reviewer is not independent: the AI wrote the fix and this record.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept this record, or return it | S01; done on 2026-11-15 by the AI as stand-in, at S01's word in chat | At the review of the pull request of `mil-018-code-quality` |
| Read NF Hotel CI, the CodeQL analysis and the review threads of the pull request: no new note | AI as stand-in for S01 | Before the merge |

---

[MIL-018]: ../../milestones/mil-018-guest-login.md
[ADR-0047]: ../../adr/adr-0047-guest-login.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[RC-391]: ./rc-391-mil-018-code.md
[RC-392]: ./rc-392-mil-018-gate.md
[ba23b07]: https://github.com/NF-Hotel/NFHotel/commit/ba23b07aa6dd2a94a89db71a8958bd1332ddf9a8
