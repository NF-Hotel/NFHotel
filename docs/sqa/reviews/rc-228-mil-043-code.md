# RC-228 Code review of MIL-043

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-228 |
| CrossReference | [MIL-043], [ADR-0031], [QC-CS-001], [QC-YML-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-26 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [43bcf70] |

---

## Artifact Under Review

- **Instances reviewed:**
  - Data Protection: `NFHotel.Web/Security/DataProtectionRegistration.cs`, `Program.cs`, the root `Dockerfile` (`/app/keys`, two Danish comments translated), `ops/deploy/nfhotel-web/docker-compose.yml` (the volume `keys`);
  - the run record: `BenchmarkRequest` and `BenchmarkRequestErrors` (Domain), `IBenchmarkRequestRepository`, the DTOs, `IBenchmarkRequestService` and `BenchmarkRequestService` (Application), `BenchmarkRunStates.Interrupted`, the configuration, repository, `DbSet` and migration `AddBenchmarkRequest` (Infrastructure), the three endpoints of `BenchmarkController` and the routes, `ApiBenchmarkRequestService`;
  - the back office: `BenchmarkRunTracker`, `BenchmarkStartViewModel`, `BenchmarkRunListViewModel`, the two pages, the error texts;
  - the tests, US-001.04 and the wiki pages CI-CD and Proxmox-Deployment.
- **Checklists used:** [QC-CS-001], [QC-YML-001], the rows that the change touches.
- **Scope:** delta review.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).
- **Tools:** `dotnet build` (analyzers as errors, 0 warnings), `dotnet format whitespace --verify-no-changes`, `scripts/check-comment-language.sh`, `ops/tests/lint.sh`; the test projects: Domain 33 and Application 8 new, Web 262, Api 175, ApiClient 86, Infrastructure 4 new against PostgreSQL (Testcontainers), all passing.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| CS 1–2 | Naming, `Async` suffix, one type per file | Pass | The new types follow the AI default feature they are modelled on |
| CS 3 | Formatted, no unexplained suppressions | Pass | Build clean; the migration's generated `using` lines trimmed as the other migrations have them; the snapshot keeps its stored line endings |
| CS 4 | Nullable, `!` justified | Pass | `!` only in tests after an `Assert.NotNull` or a known record |
| CS 6 | Async throughout | Pass | No blocking call; `ConfigureAwait(false)` as in the files around |
| CS 7 | Validation at the boundaries, no empty `catch` | Pass | The entity checks every field and the transitions; a failed record or state write is returned or, in the tracker, left for the next look with the reason in a comment |
| CS 10 | Points in time are `DateTimeOffset` | Pass | Requested and updated at, stored in UTC |
| CS 12 | Tests cover new behaviour | Pass | State transitions allowed and refused, boundaries of models, runs and error length, the service's idempotency and window, the endpoints' permissions, the repository and migration on PostgreSQL, the tracker, both pages, and two instances sharing (or not) the key folder |
| CS 13 | Traces to the design | Pass | The project DCD and ERD do not model the benchmark classes (`BenchmarkRun`, `AiDefault` are recorded in ADR-0013 and ADR-0014); `BenchmarkRequest` follows them and is recorded in ADR-0031. No change to the project models |
| CS 14 | Defined once | Pass | The states live in the entity and, as the service names them, in `BenchmarkRunStates`; the tracker is the one place that writes states, for both pages; the key path is one setting |
| CS 15–17 | Dependencies inward, SOLID, layers | Pass | The domain knows no EF; the web application reaches the record only through the API (criterion 21 holds: no database setting in the web container) |
| CS 18 | File names | Pass | English, no space |
| YML 1–3 | Compose parses, yamllint | Pass | 0 findings |

## Findings

- **Who reconciles a lost run.** The web application, not the API, talks to the benchmark service (MIL-020), so it records the states it reads. A run that nobody looks at stays `queued` until a benchmark page opens; then it is marked. ADR-0031 records this.
- **US-001.04** gained one acceptance criterion; its Version History keeps the two latest rows, as rule 7 asks.
- **Not checked here:** criteria 2, 4 and 5 of MIL-043 need the deploy; they are proved in the gate review.

## Overall Verdict

Go. Every Mandatory criterion that the change touches passes.

## Action Items

None.

---

[MIL-043]: ../../milestones/mil-043-state-across-deploys.md
[ADR-0031]: ../../adr/adr-0031-state-that-survives-a-deploy.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[43bcf70]: https://github.com/NF-Hotel/NFHotel/commit/43bcf70046d2f2537672030b83f39f8b3fda4551
