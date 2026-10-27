# ADR-0033 SonarQube Cloud analyses in CI, with code coverage

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0033 |
| CrossReference | [MIL-047], [ADR-0021], [ADR-0032], [DSA-001] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-27 | Accepted | mikemike | AI as stand-in for S01 | Initial version; S01 asked in chat on 2026-10-27 to set up test coverage in SonarQube Cloud ("kan du sætte dette op", with the page Test coverage overview of SonarQube Cloud); written by the AI under MIL-047, which S01 accepted in chat on 2026-10-27; accepted after RC-241 Go | pending |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

**Who decided what.** S01 asked on 2026-10-27 for code coverage in SonarQube Cloud. The options and the policy of this record were written by the AI assistant.

SonarQube Cloud analyses NFHotel (organization `nf-hotel`, project `NF-Hotel_NFHotel`) by **Automatic Analysis**: the GitHub app reads the code itself, with no step in CI. Automatic Analysis cannot import a coverage report, so every pull request shows "0.0% coverage on new code" (DSA-001, section 4). CI already measures coverage with `dotnet-coverage`, but only as Cobertura, which the C# analyzer of SonarQube Cloud does not read; it reads the Visual Studio XML format and OpenCover.

Options considered:

1. **Keep Automatic Analysis.** No change, and no coverage in SonarQube Cloud.
2. **A separate workflow that analyses after CI.** The SonarScanner for .NET must wrap the build (`begin`, build, tests, `end`), so a separate workflow builds and tests the solution a second time: about 10 more minutes per run.
3. **The scanner in the existing `build` job of CI.** `begin` before the build, the tests with `dotnet-coverage` writing the Visual Studio XML format, `end` after them. One build, one test run.

## Decision

We follow option 3.

1. **Analysis method.** Automatic Analysis is turned off in SonarQube Cloud (Administration, Analysis Method); the analysis runs in the `build` job of `NF Hotel CI`, on every pull request and every push to `main`.
2. **Scanner.** `dotnet-sonarscanner` as a global tool with a pinned version, like `dotnet-coverage`, with its settings in `SonarQube.Analysis.xml` at the root of the repository. It wraps a second build after the normal one, because it turns warnings as errors off in the build it wraps. The checkout fetches the whole history (`fetch-depth: 0`), which the new-code period and the blame data need.
3. **Coverage.** `dotnet-coverage` writes `TestResults/coverage.xml` (Visual Studio XML), passed as `sonar.cs.vscoveragexml.reportsPaths`; the TRX files are passed as `sonar.cs.vstest.reportsPaths`. The Cobertura file of the artifact is made from the same run with `dotnet-coverage merge`, so it is not lost. The migrations stay out of coverage, as in `dotnet-coverage.config.xml`.
4. **Token.** The analysis token is the repository secret `SONAR_TOKEN`, read by the scanner from the environment, never written on a command line or in a file. A run without the secret (a pull request from a fork, or Dependabot without its own copy of the secret) builds and tests as before and skips the analysis.
5. **Quality gate.** The quality gate stays `Sonar way`. With coverage imported, its condition "coverage on new code at least 80 %" applies. A red gate turns the check `SonarCloud Code Analysis` red, and the deploy gate of ADR-0021 waits on every check of the commit, so new code without tests stops the deploy.

## Consequences

**Positive:**

- Coverage of new code is shown on every pull request, next to the other findings.
- The analysis sees the code as it is built, with the same SDK and analyzers as CI.

**Negative:**

- The `build` job takes about 2 minutes more and needs the secret `SONAR_TOKEN`; the secret has to be renewed when the token expires.
- New code with less than 80 % coverage fails the quality gate and stops the deploy. A change of the gate is a new decision of S01, recorded in this ADR.
- Automatic Analysis must be off before the first run, or the scanner step fails.

**Quality:** maintainability (testability) and functional suitability (correctness) improve, because untested new code becomes visible (ISO/IEC 25010:2023).

## Affected Artifacts

- [DSA-001]: section 4 (the SonarQube Cloud row) and the PW.8 row.
- [MIL-047]: tasks 1 to 4.

---

[MIL-047]: ../milestones/mil-047-sonarqube-cloud-coverage.md
[ADR-0021]: ./adr-0021-automatic-deploy-when-green.md
[ADR-0032]: ./adr-0032-what-code-scanning-shows.md
[DSA-001]: ../sqa/devsecops-assessment.md
