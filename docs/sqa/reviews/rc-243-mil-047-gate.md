# RC-243 Gate review of MIL-047

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-243 |
| CrossReference | [MIL-047], [ADR-0033], [RC-240], [RC-242], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-28 | Accepted | mikemike | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- **Instance reviewed:** the five Go/No-Go criteria of [MIL-047], after #386 (`8a3dc9e`) and #387 (`408e831`) were merged, released and deployed by CD (run 40600463205).
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`). S01 turned off Automatic Analysis and added the secret `SONAR_TOKEN` (task 2).

## Criteria

| # | Criterion | Result | Evidence |
| --- | --- | --- | --- |
| 1 | The analysis of `main` has a coverage value above 0 % | Go | `api/measures/component`, branch `main`: coverage 74.3 % (5 680 lines to cover, 1 457 uncovered); quality gate OK after #387 |
| 2 | The pull request shows coverage on new code | Go | #386: coverage 74.8 %, quality gate OK; #387: quality gate OK |
| 3 | No token in the workflow, the log or a file; a run without the secret skips the analysis | Go | The first run of #386, before the secret existed, skipped the four Sonar steps and stayed green; the log shows `SONAR_TOKEN: ***`; `SonarQube.Analysis.xml` holds no token |
| 4 | Scanner and actions pinned; `contents: read` | Go | `dotnet-sonarscanner` 11.3.0; no new action; the workflow permissions are unchanged |
| 5 | CI green, code review Go, deploy succeeds | Go | RC-242 Go; CD of `408e831` succeeded; `/` 200, `/book` 200, `/admin/login` 302 |

## Findings

- **The first CI analysis found an issue that Automatic Analysis did not gate.** docker:S6471 (the Proxmox test image ran as root) made the quality gate of `main` red, and the deploy gate of ADR-0021 stopped the CD run of `8a3dc9e`, as ADR-0033 says it will. #387 runs the image as `app`; its 9 tests pass and 3 skip, as before.
- **The scanner refuses `sonar.organization` in the settings file;** it is passed as `/o:` (`efbce15`).

## Overall Verdict

Go: every criterion is met. MIL-047 is closed.

## Action Items

| # | Action | Owner | Due |
| --- | --- | --- | --- |
| 1 | Renew `SONAR_TOKEN` before it expires; an expired token fails the analysis step | S01 | At the expiry chosen in SonarQube Cloud |

---

[MIL-047]: ../../milestones/mil-047-sonarqube-cloud-coverage.md
[ADR-0033]: ../../adr/adr-0033-sonarqube-cloud-coverage.md
[RC-240]: ./rc-240-mil-047.md
[RC-242]: ./rc-242-mil-047-code.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
