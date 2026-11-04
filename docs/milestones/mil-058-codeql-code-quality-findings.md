# MIL-058 CodeQL code quality findings

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-058 |
| CrossReference | [BC-001], [DSA-001], [ADR-0032], [MIL-022], [MIL-044], [MIL-053] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-04 | Accepted | mikemike | S01 | Initial version; S01 asked in chat on 2026-11-04, in autonomous mode, to resolve the 57 CodeQL code quality findings that GitHub reports for commit `42b4dce`, the head of `main`; S01 accepted the plan in chat the same day ("Ja, inkl. CodeQL CLI") after RC-277 Go | pending |

---

## Purpose

Decide whether the 57 findings that GitHub's CodeQL code quality analysis reports for NFHotel at commit `42b4dce076d5e16d21b48b74ffb70143def6a7f9` (the head of `main` on 2026-11-04) are resolved: fixed where they are real defects, recorded with a reason where they are not, without a regression, a weakened security control or a changed behaviour. The findings are reliability and maintainability findings, not security alerts: the code scanning tab (ADR-0032) and the scan exporter of MIL-053 count the security alerts, and neither sees these.

| Rule | Findings | Priority | What it says |
| --- | ---: | --- | --- |
| `cs/path-combine` | 18 | High | `Path.Combine` discards the earlier parts when a later part is an absolute path |
| `cs/local-not-disposed` | 14 | High | A local `IDisposable` is never disposed |
| `cs/dereferenced-value-may-be-null` | 5 | High | A value that may be null is dereferenced |
| `cs/catch-of-all-exceptions` | 2 | Medium | A `catch` takes every exception |
| `js/trivial-conditional` | 2 | Medium | A condition is always true or always false |
| `js/redundant-assignment` | 1 | Medium | A variable is assigned to itself |
| `js/unneeded-defensive-code` | 2 | Low | A check guards a case that cannot happen |
| `cs/linq/missed-select` | 6 | Low | A `foreach` that only maps each element could be a `Select` |
| `cs/linq/missed-where` | 5 | Low | A `foreach` whose body is one `if` could be a `Where` |
| `cs/linq/missed-firstordefault` | 1 | Low | A `foreach` that returns the first match could be a `FirstOrDefault` |
| `py/ineffectual-statement` | 1 | Low | A statement has no effect |

The findings are only on the code quality tab of the repository, which GitHub serves to signed-in members; the code scanning API lists 0 open alerts, and the analyses it lists (`/language:csharp`, `/language:javascript-typescript`, `/language:python`, `/language:actions`, default suite) have 0 results. The gate therefore starts by reproducing the analysis, so that every disposition is made against a finding and not against a rule name. What the repository shows on 2026-11-04, before the analysis is reproduced:

- `cs/path-combine`: exactly 18 calls of `Path.Combine` in the C# code, all under `tests/` (the architecture tests that read source files, the E2E settings and screenshots, the test data of the market analysis and the seeders). Every part is a constant, `AppContext.BaseDirectory`, `Path.GetTempPath()` or the repository root the test finds; none comes from a user. `Path.Join` keeps the result for every one of them and does not discard an earlier part, so the rule is resolved without a behaviour change.
- `cs/catch-of-all-exceptions`: the two `catch (Exception)` clauses without a filter are `MarketAnalysisRunWorker.RunAsync` in `src/NFHotel.Api/Startup/` (a background worker with no caller to throw to; the exception is logged and the run is marked failed; a CA1031 pragma with that reason is already there) and the helper `Throws` of `tests/NFHotel.Api.Tests/ContractParityTests.cs`, which describes the exception type for the parity table. The other `catch (Exception ... when ...)` clauses are filtered and not findings.
- JavaScript: `ops/load/nfhotel.js` (the k6 script) has none of the three patterns, so the five findings are in the vendored Bootstrap 5.3.3 under `src/NFHotel.Web/wwwroot/lib/bootstrap/dist/js/`, to be confirmed by the analysis.
- Python: the one statement is in `ops/deploy/monitoring/scan-exporter/`, to be located by the analysis.
- `cs/local-not-disposed`, `cs/dereferenced-value-may-be-null` and the LINQ rules: located by the analysis. The compiler's nullable analysis is on with warnings as errors, so a null finding is a place where CodeQL does not follow a guard the compiler accepts; the fix is a guard both accept, never the null-forgiving operator.

## Deliverable

- **The baseline**: the 57 findings as a CodeQL analysis reports them (rule, file, line, message), from the CodeQL CLI run locally with the `code-quality` suite for C#, JavaScript and Python when S01 allows the download of the bundle, otherwise from the analysis GitHub runs on the pull request; the count 57 is compared with the fresh result before any change.
- **The fixes**, in small commits grouped by rule: `Path.Join` for the 18 test paths; `using`, `using var` or `await using` for the disposables the code owns, and a recorded reason where the object is owned by the dependency injection container or handed to the component under test; a null guard the compiler and CodeQL both accept, with a test for the null path where the behaviour was undefined; the broad catches narrowed where a narrower type is right, otherwise recorded as accepted with the reason that stands in the code; the LINQ rewrites where the query reads better and keeps deferred execution, ordering, exceptions and performance; the Python statement removed or made effective. Vendored Bootstrap is not edited by hand: a change there is lost at the next upgrade, so its findings are recorded as third-party code with the version, for S01 to dismiss in the tab with that reason.
- **The tests**: a regression test for every confirmed defect, in the test project of the layer, named `Method_Condition_Expected`; the existing tests unchanged otherwise.
- **The findings report**, in the review record of the code: one row per baseline finding with the rule and location, the root cause, the decision (fixed, already resolved, false positive, accepted or unresolved), the change, the verification and the remaining risk; the totals per decision against the original 57; and the number a fresh analysis shows afterwards.
- **The documentation**: the wiki page Security-Controls says what the code quality analysis covers, that its findings are not alerts, and where their dispositions are recorded; `test-wiki-coverage.sh` passes.
- **The pull request**, from the branch `mil-058-codeql-code-quality-findings`, with the summary of the dispositions and one `Closes #N` per task, open for S01's review; the AI does not merge it (ADR-0019; S01 asked for a pull request ready for human review).

Not in this milestone: the scanning configuration, a rule suppression or a dismissal to make a number look better; an upgrade of Bootstrap; refactoring beyond the finding at hand.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | The findings report lists every one of the 57 baseline findings with rule, location, root cause, decision, change, verification and remaining risk, gives the totals per decision, and states where the baseline came from (the CLI run or the analysis of the pull request), not from rule names | Yes | No |
| 2 | `dotnet restore NFHotel.sln --locked-mode`, `dotnet build --configuration Release` (warnings as errors), `dotnet test` and `dotnet format whitespace NFHotel.sln --verify-no-changes` pass locally; `ops/tests/test-scan-exporter.sh` passes; no `packages.lock.json`, scanning configuration or analyzer suppression is changed; the pull request's checks NF Hotel CI, CodeQL and SonarQube Cloud are green | Yes | No |
| 3 | A fresh CodeQL quality analysis of the branch (the CLI, or GitHub on the pull request) shows none of the findings recorded as fixed, and no new quality finding; what remains is only the findings recorded as accepted or false positive, each with its reason | Yes | No |
| 4 | No null-forgiving operator, pragma or empty `catch` was added to silence a finding; every new disposal is of an object the code owns; the existing tests pass unchanged except where a test was added; the authentication, authorization, logging and error contracts are the same | Yes | No |
| 5 | The review of the code against `QC-CS-001` (and `QC-PY-001` for the Python change) is Go and holds the findings report; the pull request is open for S01 with the summary and the closing lines, and the AI has not merged it | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-022 | CodeQL is set up (default setup) and is one of the checks a release waits for |
| MIL-044 | ADR-0032: a finding is shown where someone can act on it, with a recorded reason when it is dismissed; the same rule is applied to the quality findings in third-party code |
| MIL-053 | The scan exporter counts code scanning alerts and analyses; this milestone records that the quality findings are not among them |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001 objective 23: a change reaches the server only after the required CI checks pass; CodeQL is one of them (wiki page Security-Controls, "Every check green before deploy") and its quality findings are the maintainability and reliability debt behind the green check | Tasks 1 to 7 |
| DSA-001 section 8 (quality gate matrix): every finding has an owner, a decision and a reason | Tasks 1, 7 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-11. The work changes the test code, a few source files, the scan exporter and one wiki page; it does not move a date of the Business Case.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Reproduce the 57 findings as the baseline | Run the CodeQL `code-quality` suite for C#, JavaScript and Python with the CodeQL CLI on the branch (if S01 allows the download of the bundle), otherwise take the analysis GitHub runs on the pull request, and write the baseline of the report: rule, file, line and message of every finding, compared with the 57 of commit `42b4dce` and with what the repository shows (18 `Path.Combine` calls in `tests/`, 2 unfiltered `catch (Exception)`). No code changes. | No | criterion 1 |
| 2 | Replace `Path.Combine` in the 18 test paths | `Path.Join` in the 18 calls under `tests/` (architecture tests, E2E settings and screenshots, market analysis test data, the seeder tests); none of the parts comes from a user, and no part is absolute but the first, so the paths are the same. The tests that read those files are the regression tests, run on Windows locally and on Linux in CI. | No | criteria 2, 3 |
| 3 | Dispose the local disposables the code owns | For each of the 14 `cs/local-not-disposed` findings: `using`, `using var` or `await using` where the method owns the object; where the object is owned by the dependency injection container, returned, or handed to the component under test (an `HttpClient` built with `disposeHandler: false` for a client under test), record the reason instead. A test where the disposal changes an observable behaviour. | No | criteria 2, 3, 4 |
| 4 | Null dereferences and the broad catches | For each of the 5 `cs/dereferenced-value-may-be-null` findings: a guard the compiler and CodeQL both accept (a pattern, `is null`, `?? throw`), never `!`, with a test for the null path where the behaviour was undefined; the error contract of the API and the pages stays. The 2 `cs/catch-of-all-exceptions`: `MarketAnalysisRunWorker.RunAsync` keeps its catch (a worker with no caller; logged and the run marked failed, as the CA1031 reason says) and is recorded as accepted unless a narrower set of types covers what the runner throws; the test helper `Throws` of `ContractParityTests` is recorded as accepted (it describes the exception type). | No | criteria 2, 3, 4 |
| 5 | LINQ, the Python statement and the vendored JavaScript | The 12 LINQ findings: rewrite as `Select`, `Where` or `FirstOrDefault` only where the query reads better and keeps deferred execution, ordering, exceptions and performance; leave the loop, with the reason, where it has side effects. The `py/ineffectual-statement` in the scan exporter: removed or made effective, `test-scan-exporter.sh` green. The 5 JavaScript findings: confirmed in the vendored Bootstrap 5.3.3 under `wwwroot/lib/`, recorded as third-party code with the version for S01 to dismiss in the tab; `ops/load/nfhotel.js` checked clean. | No | criteria 1, 3 |
| 6 | Validate locally and in CI | `dotnet restore --locked-mode`, `dotnet build --configuration Release`, `dotnet test`, `dotnet format whitespace --verify-no-changes` locally; `ops/tests/test-scan-exporter.sh` for the Python; push and read the checks of the pull request (NF Hotel CI, CodeQL, SonarQube Cloud); a fresh quality analysis compared with the baseline, finding by finding. The local build does not stand in for GitHub's analysis. | No | criteria 2, 3 |
| 7 | Findings report, review, wiki and pull request | The review record against `QC-CS-001` (and `QC-PY-001`) with the findings report (rule and location, root cause, decision, change, verification, remaining risk; totals fixed, already resolved, false positive, accepted, unresolved against 57; the number of the fresh analysis); the wiki page Security-Controls with the code quality analysis and where the dispositions are; the pull request with the summary and one `Closes #N` per task, open for S01; nothing merged by the AI. | No | criteria 1, 5 |

---

[BC-001]: ../business-case.md
[DSA-001]: ../sqa/devsecops-assessment.md
[ADR-0032]: ../adr/adr-0032-what-code-scanning-shows.md
[MIL-022]: ./mil-022-supply-chain-security.md
[MIL-044]: ./mil-044-security-overview.md
[MIL-053]: ./mil-053-security-scan-metrics.md
