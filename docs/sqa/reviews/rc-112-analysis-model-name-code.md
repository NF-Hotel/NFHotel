# RC-112 Review of the exact model name for the market analysis (MIL-020)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-112 |
| CrossReference | [MIL-020], [ADR-0014], [QC-CS-001], [QC-SH-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-19 | Accepted | Claude (AI) | AI at the request of S01, not independent | Initial version | pending |

---

## Artifact Under Review

- Instance reviewed: [MIL-020] task 9: `scripts/resolve-model-name.sh`, `scripts/test-resolve-model-name.sh` and `apply_default_model` in `scripts/deploy-services.sh`; `IsUnknownModel` in `BenchmarkRunListViewModel` and the hint in `BenchmarkRuns.razor` (Web); `deploy/market-analysis/market-analysis.toml.example`
- Checklist used: [QC-CS-001] for the C# change and [QC-SH-001] for the shell scripts
- Scope: full review
- Language and domain: n/a for a technical type
- Language reviewer: none
- Mode: autonomous, not independent

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Naming follows the conventions | Pass | `IsUnknownModel` follows the other members of the view model; the shell scripts are named by what they do |
| 2 | Async methods end in `Async`; one top-level type per file | Pass | No new type; the architecture tests pass |
| 3 | Formatted per `.editorconfig`; no unexplained analyzer suppressions | Pass | Build 0 warnings with warnings as errors; `dotnet format whitespace` clean |
| 4 | Nullable reference types enabled; `!` justified | Pass | No new `!` |
| 5 | `IDisposable` objects are disposed | Pass | No new disposable |
| 6 | Async code uses `await` throughout | Pass | No new async code |
| 7 | Specific exceptions; arguments validated; no empty `catch` | Pass | The script validates its argument and exits 2; a model that is not installed is exit 3 with a warning, not a silent change |
| 8 | Public types and members have XML documentation | Pass | Enforced by the build; the scripts have a header with usage, exit codes and needs |
| 9 | State through properties; immutability where it fits | Pass | `IsUnknownModel` reads state and changes none |
| 10 | Money uses `decimal`; points in time use `DateTimeOffset` | N-A | No money or time |
| 11 | Public async APIs accept and pass a `CancellationToken` | N-A | No new async API |
| 12 | Tests cover new behaviour, are named `Method_Condition_Expected`, and do not depend on order, time or the network | Pass | One new test of the hint; 14 shell checks of the name resolution with no Docker; whole solution 1102 tests, 0 failed; the proxy test of the compose setup passes |
| 13 | Classes and members trace to the Design Class Diagram | N-A | The project DCD has no benchmark classes; see the open action of RC-105 |
| 14 | Each piece of knowledge is defined in one place | Pass | The rule that a name must match exactly is one script, used by the deploy script; the page only hints |
| 15 | Namespace and project references point inward | Pass | The view model reads its own state |
| 16 | SOLID holds | Pass | One script does one thing and prints one name |
| 17 | Source is laid out in the layers and folders of the clean architecture folder structure | Pass | The scripts are in `scripts/` |
| 18 | File and folder names are English with no space or odd character | Pass | All names checked |

## Overall Verdict

Go — every mandatory criterion passes or is not applicable. The review is not independent. Found by running the service: with the default `phi3.5` the market analysis selected no model and made no AI insight; with `phi3.5:latest` it selected Ollama and the model, and the cancellation analysis got an executive summary. After the change the deploy script wrote `phi3.5:latest` from the default `phi3.5` in the development VM. A run after the change (timeout 240 seconds, 423 seconds in all) gave an executive summary for the lead time analysis and no `TIMEOUT`; the cancellation analysis was `BAD_STRUCTURE`, the room value analysis `GUARDRAIL_REJECTED` and the guest mix analysis `MODEL_ERROR`. A small model gives a summary for some analyses and not for others, and which ones changes from run to run; that is a model-quality result and is the first action item.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Make the market analysis guardrails pass for small models, or score the guardrail pass rate per model in the benchmark: with `phi3.5` the room value analysis was `GUARDRAIL_REJECTED` | S01 | When the market analysis repository is next changed |
| Let the market analysis service read the default itself (see RC-109) | S01 | When the market analysis repository is changed |

---

[MIL-020]: ../../milestones/mil-020-start-benchmark-run.md
[ADR-0014]: ../../adr/adr-0014-default-ai-model.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
