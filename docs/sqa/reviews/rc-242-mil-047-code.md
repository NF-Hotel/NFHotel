# RC-242 Code review of MIL-047

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-242 |
| CrossReference | [MIL-047], [ADR-0033], [QC-YML-001], [QC-WF-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-27 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [6f7749b] |

---

## Artifact Under Review

- **Instances reviewed:** the `build` job of `.github/workflows/ci.yaml` (the token in the job environment, the full checkout, the scanner steps, the coverage in Visual Studio XML and Cobertura), `SonarQube.Analysis.xml`, the wiki page CI-CD, DSA-001 section 4 and PW.8.
- **Checklists used:** [QC-YML-001], [QC-WF-001], the rows that the change touches.
- **Scope:** delta review.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).
- **Tools:** actionlint 1.7.7 and yamllint 1.37.1 with the repository's `.yamllint`: clean. The scanner itself runs only with the secret, so it is proven by the CI run of the pull request (criterion 2 of MIL-047).

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| QC-WF 4 | Header names the purpose and the secrets | Pass | The header names `SONAR_TOKEN`, that it is optional, and what is sent outside the repository |
| QC-WF 5 | Every step has a unique, descriptive name | Pass | Five new steps, each named |
| QC-WF 6 | Passes actionlint, version stated | Pass | actionlint 1.7.7 |
| QC-WF 7 | Least permissions | Pass | The workflow keeps `contents: read`; the scanner needs no GitHub token |
| QC-WF 8 | Actions pinned | Pass | No new action; checkout stays pinned by commit |
| QC-WF 9 | No secret in the file or on a command line | Pass | `secrets.SONAR_TOKEN` goes into the job `env:`; the scanner reads `SONAR_TOKEN` from the environment, and no `/d:sonar.token=` is passed |
| QC-WF 11 | No secrets for code from a fork | Pass | The trigger stays `pull_request`; a fork gets no secret and the steps are skipped by `if: env.SONAR_TOKEN != ''` |
| QC-WF 14 | `runs-on` and `timeout-minutes` | Pass | 30 minutes, for the second build and the analysis |
| QC-WF 17 | Tools at pinned versions | Pass | `dotnet-sonarscanner` 11.3.0, `dotnet-coverage` 18.12.0 |
| QC-WF 20, QC-YML 7 | Embedded program text at most five lines | Pass | The settings moved to `SonarQube.Analysis.xml`, so `begin` is two lines |
| QC-YML 3 | yamllint clean | Pass | yamllint 1.37.1 |
| QC-YML 6 | No secret in the file | Pass | See QC-WF 9; `SonarQube.Analysis.xml` holds no token |
| QC-YML 11 | Header comment | Pass | Both files say what they are for and name ADR-0033 |

The warnings-as-errors gate is kept: the normal build runs first with `TreatWarningsAsErrors`, and the scanner wraps a second build with `--no-incremental`, since the scanner turns that setting off in the build it wraps. The Cobertura file of the artifact is still made, from the same run, by `dotnet-coverage merge`.

## Overall Verdict

Go: every Mandatory criterion passes.

## Action Items

None.

---

[MIL-047]: ../../milestones/mil-047-sonarqube-cloud-coverage.md
[ADR-0033]: ../../adr/adr-0033-sonarqube-cloud-coverage.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[6f7749b]: https://github.com/NF-Hotel/NFHotel/commit/6f7749b4fe530edcf83694abb358090cd4da9050
