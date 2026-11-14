# RC-367 Gate review of MIL-054 Test coverage of 95 % or more

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-367 |
| CrossReference | [MIL-054], [RC-258], [RC-260], [ADR-0033], [MIL-057], [QC-MIL-001], [QC-CS-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-14 | Accepted | mikemike | AI as stand-in for S01 | Initial version: the gate of MIL-054 on the measurement of `main` at 9de9be4 (SonarQube Cloud and the coverage floor of NF Hotel CI) and on pull request #435; Go; mode autonomous, not independent | pending |

---

## Artifact Under Review

- Instance reviewed: [MIL-054] at its gate. It covers the seven Go/No-Go criteria, with this evidence:
  - pull request #435, merged as 4158da2 on 2026-11-01, which added the tests, the floor step and its test;
  - the analysis of `main` at 9de9be4 on SonarQube Cloud, of 2026-11-14 at 17:29:52 UTC;
  - NF Hotel CI run 43621113638 of 9de9be4, its steps "test-check-coverage-floor.sh" and "check-coverage-floor.sh";
  - the coverage settings on `main`: `dotnet-coverage.config.xml` and `SonarQube.Analysis.xml`.

  Before the gate: [RC-258] (the plan) and [RC-260] (the code, Go).
- Checklist used: the Go/No-Go criteria of [MIL-054]. [QC-MIL-001] criterion 2 makes them the checklist of the gate.
- Scope: the gate. Criteria 1 to 3 rest on the measurement of `main` on 2026-11-14, criteria 4 and 6 on the diff of
  #435, criterion 5 on the settings on `main`, and criterion 7 on [RC-260].
- Language and domain: en / it (the Metadata rows of MIL-054). The language of the document was reviewed in RC-258, and
  the gate adds only a row to its Version History. This record is a technical type.
- Language reviewer: none.
- Mode: autonomous, not independent. S01 gave the acceptance of the plan of 2026-11-14, which has the gates of the
  older milestones as its third wave, to the AI in chat ("Du godkender for mig"). The AI wrote the code of #435 and this
  record.

**Evidence of 2026-11-14:**

- **SonarQube Cloud.** The measures of project `NF-Hotel_NFHotel` on branch `main`, read from its public API after the
  analysis of 9de9be4 at 17:29:52 UTC: line coverage 95.9 %, 331 of 8,105 lines to cover not run by a test.
- **The coverage floor in CI.** NF Hotel CI run 43621113638 of 9de9be4 ended with success. Its step
  `bash scripts/test-check-coverage-floor.sh` logged "20 checks, 0 failed". The step
  `bash scripts/check-coverage-floor.sh TestResults/coverage.cobertura.xml` logged one line per project, each "ok":

  | Project | Lines | Line coverage |
  | --- | --- | --- |
  | NFHotel.Api | 588 of 612 | 96.08 % |
  | NFHotel.Api.Contracts | 3 of 3 | 100.00 % |
  | NFHotel.ApiClient | 342 of 346 | 98.84 % |
  | NFHotel.Application | 2,579 of 2,612 | 98.74 % |
  | NFHotel.Domain | 709 of 709 | 100.00 % |
  | NFHotel.External | 1,104 of 1,144 | 96.50 % |
  | NFHotel.Infrastructure | 1,463 of 1,500 | 97.53 % |
  | NFHotel.Web | 4,959 of 5,164 | 96.03 % |
  | Total | 11,747 of 12,090 | 97.16 % |

- **Pull request #435.** `git diff 4158da2^1 4158da2 -- src/` is empty. Under `tests/` it adds 24 files: 2 in
  NFHotel.Api.Tests, 2 in NFHotel.Application.Tests, 1 in NFHotel.Domain.Tests and 19 in NFHotel.Web.Tests, with 182
  new public methods. Of these, 179 are tests named in three parts, two are `Dispose` and one is
  `Constructor_AndCalls_MissingArguments_Throw` (F2). No added line has `Thread.Sleep`, `Task.Delay` with a fixed time
  or an address of the network.
- **The coverage settings on `main`.** `dotnet-coverage.config.xml` measures the `NFHotel.*` assemblies without the test
  assemblies and `NFHotel.TestSupport`, and excludes the sources under `Migrations/` and `HotelDbContextFactory.cs`,
  with the reason in a comment. `SonarQube.Analysis.xml` sets `sonar.coverage.exclusions` to
  `**/Migrations/**,ops/**,scripts/*.ps1,**/HotelDbContextFactory.cs`, with the reason of each in the comments above it.

## Checklist Results

| # | Criterion (MIL-054) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | SonarQube Cloud shows 95.0 % or more line coverage on `main` after the merge | Pass | 95.9 % on `main` at 9de9be4, analysis of 2026-11-14 17:29:52 UTC; this is also the action item of [RC-260] |
| 2 | Each project under `src/` has 90 % or more line coverage in the Cobertura report of CI | Pass | Run 43621113638: the lowest project is NFHotel.Web at 96.03 %, the total 97.16 % |
| 3 | The coverage floor step fails CI when the report is under the floor (shown by a test of the step with a report under the floor) and passes on `main` | Pass | `test-check-coverage-floor.sh`: 20 checks, 0 failed; the floor step passed on 9de9be4 |
| 4 | Every new test is named `Method_Condition_Expected`, runs without the network and without a fixed sleep, and the unit and integration tests stay apart (BC-001 objective 15) | Pass | The diff of #435 and [RC-260] criterion 12. The integration tests against a real PostgreSQL stay in `tests/NFHotel.Infrastructure.Tests/Integration` (Testcontainers), and #435 adds nothing there; the API tests run the host in memory with `Microsoft.AspNetCore.Mvc.Testing`. One name has four parts (F2) |
| 5 | The only coverage exclusions are the generated migrations, `ops/` (tested in Ops CI), and the two files named in the deliverable, each with its reason in the configuration file | Pass | The settings above; the pattern `scripts/*.ps1` also takes `dev-f5.ps1`, added by [MIL-057] with its reason (F1) |
| 6 | No code under `src/` changes except to fix a defect a new test found, and each such fix has its regression test | Pass | #435 changes nothing under `src/` |
| 7 | The code review against `QC-CS-001` is Go, and the lines still not covered are listed in it with the reason | Pass | [RC-260]: Go, with the section "Lines still not covered, and why" |

## Findings

- **F1, note: one exclusion pattern now takes two launchers.** The deliverable names `scripts/dev-up.ps1`. Commit
  6b3a952 of [MIL-057] (2026-11-02) widened the Sonar pattern to `scripts/*.ps1`, so that `scripts/dev-f5.ps1`, the
  launcher for Visual Studio, is left out too; the comment above the property names both files and the reason (no test
  runner for PowerShell in CI). Both are developer launchers for Windows, which the deliverable meant, and neither is
  product code, so the criterion holds.
- **F2, note: one test name has four parts.** `Constructor_AndCalls_MissingArguments_Throw` names two members in its
  first part. It reads as method, condition and expected result, and [RC-260] passed the naming, so the name stays.
- **F3, note: two measurements of the same code.** The total in CI is 2.16 points over the floor of 95 % and the lowest
  project 6.03 points over 90 %. SonarQube Cloud counts 8,105 lines to cover and CI 12,090; they read different
  reports: SonarQube Cloud the Visual Studio XML of `dotnet-coverage` (its C# analyzer does not read Cobertura, as
  `SonarQube.Analysis.xml` says), CI the Cobertura report. Both are over the target.

## Overall Verdict

Go. The seven criteria pass on the evidence of 2026-11-14. S01 gave the acceptance to the AI in chat on 2026-11-14
("Du godkender for mig"), and the AI accepted this record and the gate row of MIL-054 as stand-in for S01; the gate of
MIL-054 has passed. MIL-054 has no milestone or issues on GitHub, so its pull request closes none. The reviewer is not
independent: the AI wrote the code of #435 and this record.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept this record and the gate row of MIL-054, or return them | S01; done on 2026-11-14 by the AI as stand-in, at S01's word in chat | At the review of the gate pull request |

---

[MIL-054]: ../../milestones/mil-054-test-coverage.md
[RC-258]: ./rc-258-mil-054-plan.md
[RC-260]: ./rc-260-mil-054-code.md
[ADR-0033]: ../../adr/adr-0033-sonarqube-cloud-coverage.md
[MIL-057]: ../../milestones/mil-057-start-from-visual-studio.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
