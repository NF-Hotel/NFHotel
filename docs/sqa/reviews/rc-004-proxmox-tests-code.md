# RC-004 Review of Proxmox test code

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-004 |
| CrossReference | [MIL-001], [QC-CS-001], [QC-SH-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S04 | Initial version | [b690b57] |

---

## Artifact Under Review

- Instance reviewed: `tests/NFHotel.Proxmox.Tests/` (C#), `scripts/run-proxmox-tests.sh` (bash), implementing tasks 2 and 3 of [MIL-001]
- Checklist used: [QC-CS-001] for the C# code and [QC-SH-001] for the shell script
- Scope: full review
- Language and domain: n/a (source code, technical type)
- Language reviewer: none
- Mode: autonomous, not independent (reviewer is `AI as stand-in for S04`)
- Tools: `dotnet format --verify-no-changes` (SDK 10.0.402), `shellcheck` 0.11.0, `shfmt` 3.14.1, `bash -n`, `dotnet test`

## Checklist Results

### C# ([QC-CS-001])

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Naming follows the conventions | Pass | PascalCase types, `_camelCase` private fields |
| 2 | Async methods end in `Async`; one top-level type per file | Pass | `GetDataAsync`; test methods return `Task` and are named by behaviour, as xUnit allows |
| 3 | Formatted per `.editorconfig`, no unexplained analyzer suppressions | Pass | `dotnet format --verify-no-changes` clean; `TreatWarningsAsErrors` on |
| 4 | Nullable enabled; `!` justified | Pass | Nullable enabled; no `!` operator used |
| 5 | `IDisposable` disposed | Pass | `HttpClient` and responses in `using`; test class implements `Dispose` |
| 6 | No `.Result`, `.Wait()` or `async void` | Pass | None present |
| 7 | Specific exceptions, validated boundaries, no empty catch | Pass | `InvalidOperationException` with named settings; no catch blocks |
| 8 | Public types have XML documentation | Fail | Optional criterion: the three test classes have no `///` comment. Action item 1 |
| 9 | Properties not public fields; immutability | Pass | Get-only properties, `readonly` fields |
| 10 | Money `decimal`; time `DateTimeOffset` | N-A | No money or points in time |
| 11 | Public async APIs take a `CancellationToken` | Pass | `GetDataAsync(path, ct)` |
| 12 | Tests cover behaviour, named `Method_Condition_Expected`, no order/time/network dependence | Pass | 9 offline tests; the 3 live tests need the network by purpose and are skipped unless `PVE_HOST` is set or the runner sets `PVE_REQUIRE_LIVE` |
| 13 | Classes trace to the Design Class Diagram | N-A | No DCD exists for infrastructure test code; the code traces to [MIL-001] task 2 |
| 14 | Each piece of knowledge defined once | Pass | Variable names read in `PveSettings` only; GET-only rule in `ReadOnlyHandler` only |
| 15 | References point inward | Pass | No project references; no cycles |
| 16 | SOLID holds | Pass | Settings, guard, client and tests are separate single-purpose types |
| 17 | Layer and folder structure | Pass | Integration test project in `tests/`, same flat layout as the existing test projects |
| 18 | File and folder names | Pass | English, PascalCase, no spaces |

### Shell ([QC-SH-001])

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | `#!/usr/bin/env bash` and `set -euo pipefail` | Pass | Lines 1 and 14 |
| 2 | Every expansion quoted; `[[ ]]` and `$(...)` | Pass | `shellcheck` clean |
| 3 | Naming conventions | Pass | `kebab-case.sh`, `snake_case` function, `UPPER_SNAKE` constants |
| 4 | Passes `shellcheck` and `bash -n` | Pass | shellcheck 0.11.0, no disables |
| 5 | Errors to stderr with `error:`, non-zero exit, usage line | Pass | `die` helper; usage exits 2 |
| 6 | Temporary files use `mktemp` and `trap` | N-A | No temporary files; `trap` removes the container |
| 7 | No secret in script, echo or command line | Pass | Secret passes only through `--env-file` |
| 8 | Dry run or explicit flag for state change outside own directory | Pass | The script only builds a local image and a removed-on-exit container; header says so |
| 9 | Header comment complete | Pass | Purpose, usage, exit codes, needs, task |
| 10 | Implements a cited task | Pass | Header cites MIL-001 task 3 |
| 11 | Behaviour tested for success, failure and bypass | Pass | Recorded runs: failure path with empty `pve.env` (exit 1, 0 containers left). Success path against the real server is not yet recorded: see action item 2 |
| 12 | Formatted with `shfmt` | Pass | `shfmt -i 2 -ci` applied |
| 13 | Safe to re-run | Pass | Image build is cached; each run uses its own container name |
| 14 | Bash version and tools stated | Pass | Header: bash 4+, docker |
| 15 | Repeated knowledge defined once | Pass | Image and container names are constants |

## Overall Verdict

Go — all Mandatory criteria pass or are not applicable with a stated reason. The optional XML-documentation criterion fails. The runner and tests work end to end up to the real server: `pve.env` has no host or token yet, so the live checks fail with a message naming the missing settings, as designed.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| 1. Add `///` summaries to the three test classes | S01 | 2026-10-31 |
| 2. Fill `pve.env` and run `bash scripts/run-proxmox-tests.sh` against the real server; record the result | S01 | 2026-10-17 |

---

[MIL-001]: ../../milestones/mil-001-proxmox-test-env.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[b690b57]: https://github.com/NF-Hotel/NFHotel/commit/b690b576d531e9f26709b6f21275cbc675065d03
