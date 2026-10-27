# MIL-047 Code coverage in SonarQube Cloud

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-047 |
| CrossReference | [BC-001], [ADR-0021], [ADR-0033], [DSA-001] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-27 | Accepted | mikemike | S01 | Initial version; S01 asked in chat on 2026-10-27 to set up test coverage in SonarQube Cloud ("kan du sætte dette op") and accepted the plan ("Jeg accepterer planen for MIL-047"; accepted after RC-240 Go) | pending |

---

## Purpose

Decide whether NF Hotel may claim that SonarQube Cloud shows the code coverage of every change. Today it analyses by Automatic Analysis, which cannot import coverage, so every pull request shows 0.0 % coverage on new code, although CI measures it. ADR-0033 moves the analysis into CI.

## Deliverable

- The `build` job of `.github/workflows/ci.yaml` runs the SonarScanner for .NET around the build and the tests, and sends the coverage (Visual Studio XML) and the TRX files to SonarQube Cloud.
- Automatic Analysis turned off and the secret `SONAR_TOKEN` in the repository (S01).
- DSA-001 and the wiki page CI-CD say how the analysis runs.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | The SonarQube Cloud analysis of `main` after the merge has a coverage value above 0 % (`api/measures/component?metricKeys=coverage`) | Yes | No |
| 2 | The pull request of this milestone shows coverage on new code in the SonarQube Cloud comment | Yes | No |
| 3 | No token in the workflow, the log or a file; a run without `SONAR_TOKEN` builds and tests and skips the analysis | Yes | No |
| 4 | The scanner and the actions are pinned (version and commit digest), and the job keeps `permissions: contents: read` | Yes | No |
| 5 | CI is green, the review against `qc-workflow` and `qc-yaml` is Go, and the deploy after the merge succeeds | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| CI build job | Coverage is already measured with `dotnet-coverage` (Microsoft.Testing.Platform) |
| ADR-0021 | The check `SonarCloud Code Analysis` is part of the deploy gate |
| S01 | Turns off Automatic Analysis and adds the secret `SONAR_TOKEN` (task 2) |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001 quality objective: tested code, measured in CI | Tasks 1 to 4 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-03.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Scanner in the CI build job | In `ci.yaml`: checkout with `fetch-depth: 0`; install `dotnet-sonarscanner` 11.3.0; `begin` with `/k:NF-Hotel_NFHotel /o:nf-hotel`, the Visual Studio coverage and TRX paths and the migrations excluded from coverage; build; tests with `dotnet-coverage --output-format xml`; Cobertura made with `dotnet-coverage merge`; `end`. Every Sonar step runs only when `SONAR_TOKEN` is set, read from the environment. | No | criteria 3, 4 |
| 2 | SonarQube Cloud settings (S01) | S01 turns off Automatic Analysis (Administration, Analysis Method) and adds the analysis token as the repository secret `SONAR_TOKEN`, before the pull request runs. | No | criteria 1, 2 |
| 3 | Documents | DSA-001 section 4 and PW.8; the wiki page CI-CD; reviews against `qc-workflow` and `qc-yaml`. | No | criterion 5 |
| 4 | Release and check | Merge when green, the deploy, and the coverage value of `main` read from SonarQube Cloud; gate review. | No | criteria 1, 2, 5 |

---

[BC-001]: ../business-case.md
[ADR-0021]: ../adr/adr-0021-automatic-deploy-when-green.md
[ADR-0033]: ../adr/adr-0033-sonarqube-cloud-coverage.md
[DSA-001]: ../sqa/devsecops-assessment.md
