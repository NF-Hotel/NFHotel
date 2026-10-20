# RC-140 Review of the OpenSSF Scorecard workflow and its first result (MIL-023 task 5)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-140 |
| CrossReference | [MIL-023], [ADR-0012], [QC-WF-001], [QC-YML-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Proposed | mikemike | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- **Instance reviewed:** `.github/workflows/scorecard.yaml` (new). The same workflow in NF-Hotel/DevOps, the DORA metrics script and the SAMM baseline are reviewed in RC-507 of NF-Hotel/DevOps.
- **Checklists used:** [QC-WF-001], [QC-YML-001], the rows that the change touches.
- **Scope:** full review of the new file; the first Scorecard result of this repository.
- **Language and domain:** n/a (technical type).
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).
- **Tools:** actionlint 1.7.7 and yamllint 1.35.1 in containers; OpenSSF Scorecard v5.5.0 (the version that scorecard-action v2.4.4 runs), built from the Go module proxy and run locally with `--show-details` against `main` at `7c6f348`, on 2026-10-20 at 14:22 UTC.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| WF 1 | Every action pinned by full commit SHA with the version in a comment | Pass | checkout v7.0.1, scorecard-action v2.4.4 (`2d11466`, released 2026-07-23), upload-artifact v4.6.2, codeql-action/upload-sarif v4.38.2 (`2892aa5`, 2026-09-24). Both are older than two weeks; scorecard-action v2.5.0 (2026-10-12) is not |
| WF 2 | Least permissions | Pass | `contents: read` at the top (not `read-all`, which SonarCloud rule githubactions:S8234 rejects); the job adds `security-events: write` for the SARIF and `id-token: write` to sign the published results; `contents` and `actions` read |
| WF 3 | Triggers | Pass | Push to `main`, `branch_protection_rule`, Tuesday 04:41 UTC and by hand. No pull request trigger: a pull request run can neither upload nor publish |
| WF 4 | No untrusted input in a `run:` line; no stored secret | Pass | No `run:` step; only the job token. Without a personal token the Branch-Protection check sees less than an administrator would; accepted, no token is stored for it |
| WF 5 | Timeouts and concurrency | Pass | `timeout-minutes: 15`; one run per ref |
| WF 6 | Header comment | Pass | Purpose, triggers, secrets, permissions and the restrictions of `publish_results` |
| YML 1 | actionlint and yamllint clean, line length 100 | Pass | Both clean on `.github/workflows/` |

## First Scorecard result

Score **5.1 / 10** for NF-Hotel/NFHotel at `7c6f348` (NF-Hotel/DevOps at `9b572d2`: 2.8, see RC-507 there). The workflow itself first runs when it reaches `main`; its result in code scanning (category `scorecard`) is recorded then.

| Check | Score | Reason (Scorecard) | Next step |
| --- | --- | --- | --- |
| Binary-Artifacts | 10 | No binaries in the repository | — |
| Branch-Protection | 5 | 1 required approval; code owner review not required; up-to-date branches off | S01 decides; a second approval is not realistic for two maintainers |
| CI-Tests | 10 | 7 of 7 merged pull requests checked by CI | — |
| CII-Best-Practices | 0 | No OpenSSF Best Practices badge | Optional; the questionnaire overlaps the SAMM baseline |
| Code-Review | 10 | All changesets reviewed | — |
| Contributors | 10 | Contributors from 10 organizations | — |
| Dangerous-Workflow | 0 | Untrusted code checkout of `github.event.workflow_run.head_sha` in `release.yaml:44` | See finding 1 |
| Dependency-Update-Tool | 0 | No Dependabot configuration on `main` | NF-Hotel/NFHotel#195 adds it |
| Fuzzing | 0 | No fuzzing | Not planned |
| License | 0 | No license file | S01 decides the license |
| Maintained | 0 | Repository created within the last 90 days | Rises by itself |
| Packaging | n/a | No packaging workflow detected | — |
| Pinned-Dependencies | 3 | `ci.yaml` actions by tag; base images of `Dockerfile`, `Dockerfile.api` and `deploy/market-analysis/Dockerfile` without a digest | NF-Hotel/NFHotel#195 pins the actions and the two app images; `deploy/` is an old copy that MIL-025 task 7 removes |
| SAST | 10 | CodeQL on all commits | — |
| Security-Policy | 4 | `SECURITY.md` found; no link or contact in it that Scorecard recognizes | See finding 2 |
| Signed-Releases | n/a | No GitHub releases (images are attested in GHCR instead) | — |
| Token-Permissions | 10 | Workflow tokens follow least privilege | — |
| Vulnerabilities | 10 | No open vulnerabilities | — |

## Findings

1. **Dangerous-Workflow is a known false positive, but it costs the whole check.** The Release workflow checks out `workflow_run.head_sha` with the package token present. It runs only for a successful CI run of a **push** to `main` in this repository (the `if:` of the job), so the commit is never from a fork or a pull request. Scorecard cannot see the condition. Options for S01: keep it and accept the 0, or check out `github.event.workflow_run.head_sha` only after comparing it with the head of `main`, which does not change the score either. Recommendation: accept, and say so in the code scanning alert when it appears.
2. **The reporting channel of `SECURITY.md` does not work.** It sends reporters to "Report a vulnerability" in the Security tab, but private vulnerability reporting is disabled (`GET /repos/NF-Hotel/NFHotel/private-vulnerability-reporting` answers `enabled: false`). Enabling it is a repository setting for S01.

## Overall Verdict

Go for the workflow. The score is a baseline, not a gate; findings 1 and 2 go to S01.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Record the first run of the workflow on `main` (code scanning, category `scorecard`) | S01 | After the merge |
| Enable private vulnerability reporting, or change `SECURITY.md` | S01 | Next week |
| Decide on the Dangerous-Workflow alert of `release.yaml` (accept or change) | S01 | When the alert appears |
| Choose a license | S01 | Before a wider launch |

---

[MIL-023]: https://github.com/NF-Hotel/DevOps/blob/main/docs/milestones/mil-023-operational-quality.md
[ADR-0012]: ../../adr/adr-0012-devops-repository-ownership.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
