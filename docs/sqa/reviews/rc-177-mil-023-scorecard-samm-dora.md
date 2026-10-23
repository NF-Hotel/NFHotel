# RC-177 Review of the Scorecard workflow, the SAMM baseline and the DORA metrics (MIL-023 task 5)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-177 |
| CrossReference | [MIL-023], [SAMM-001], [DORA-001], [DSA-001], [QC-SH-001], [QC-WF-001], [QC-YML-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Deprecated | mikemike | AI as stand-in for S01 | Initial version | pending |
| 2026-10-23 | Accepted | mikemike | AI as stand-in for S01 | Ported from NF-Hotel/DevOps#10 (was RC-507 there); paths under ops/ (RC-178) | pending |

---

## Artifact Under Review

- **Instances reviewed:**
  - `.github/workflows/scorecard.yaml` (new), and the same workflow in NF-Hotel/NFHotel (reviewed in RC-140 of NFHotel);
  - `.github/workflows/ci.yaml`: the pull request trigger without a branch filter, so that a stacked pull request gets CI, and the new matrix entry `test-dora-metrics.sh`;
  - `scripts/dora-metrics.sh`, `tests/test-dora-metrics.sh` and `tests/fixtures/dora/` (new);
  - [SAMM-001] `docs/sqa/samm-baseline.md`, [DORA-001] `docs/sqa/dora/dora-001-2026-10.md` and `docs/sqa/dora-metrics.md` (new); `docs/artifact-registry.md`.
- **Checklists used:** [QC-SH-001], [QC-WF-001], [QC-YML-001], the rows that the change touches; for the two documents, the evidence rules of [DSA-001] section 1.
- **Scope:** full review of the new files, delta review of `ci.yaml` and the registry.
- **Language and domain:** n/a (technical types).
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).
- **Tools:** `tests/lint.sh` (actionlint 1.7.7 with shellcheck, shellcheck 0.11.0, shfmt 3.14.1, yamllint 1.35.1); `tests/test-dora-metrics.sh` in Debian 12 (25 passed); `scripts/dora-metrics.sh fetch` and `report` against NF-Hotel/NFHotel for October 2026; OpenSSF Scorecard v5.5.0 (the version that scorecard-action v2.4.4 runs), built from the Go module proxy, against `main` of both repositories.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| WF 1 | Every action pinned by full commit SHA with the version in a comment | Pass | checkout v7.0.1, scorecard-action v2.4.4 (`2d11466`, released 2026-07-23), upload-artifact v4.6.2, codeql-action/upload-sarif v4.38.2 (`2892aa5`, 2026-09-24). Both new releases are older than two weeks; scorecard-action v2.5.0 (2026-10-12) is not |
| WF 2 | Least permissions | Pass | `contents: read` at the top (not `read-all`, which SonarCloud rule githubactions:S8234 rejects); the job has `contents: read`, `actions: read`, `security-events: write` (SARIF) and `id-token: write` (signing the published results), nothing else |
| WF 3 | Triggers | Pass | Push to `main`, `branch_protection_rule`, weekly (Tuesday 04:53 UTC; NFHotel 04:41) and by hand; no pull request trigger, because a pull request run can neither upload nor publish |
| WF 4 | No untrusted input in a `run:` line; no stored secret | Pass | The workflow has no `run:` step and uses only the job token |
| WF 5 | Timeouts and concurrency | Pass | `timeout-minutes: 15`; one run per ref |
| WF 6 | Header comment: purpose, triggers, secrets, permissions | Pass | Also names the restrictions that `publish_results` puts on the job |
| WF 7 | The CI trigger change | Pass | `pull_request` without `branches` runs CI for a pull request to any branch (this one is stacked on NF-Hotel/DevOps#8); pushes still run only on `main`. The ruleset checks are unchanged |
| SH 1–2 | Shebang, `set -euo pipefail` (the test `set -uo`), quoting, arrays, `[[ ]]` | Pass | `dora-metrics.sh`, `test-dora-metrics.sh` |
| SH 3 | Names and constants | Pass | Workflow names, environment and the two jq programs are readonly |
| SH 4 | shellcheck and bash -n | Pass | 0 findings, after SC2015 in the deployment loop was rewritten as an `if` |
| SH 5 | Errors and usage | Pass | Missing data files and an invalid month fail with 1, usage errors with 2; both tested |
| SH 7 | No secret in a script, on a command line or echoed | Pass | `fetch` uses `gh` with the caller's token from the environment; nothing is printed of it |
| SH 8 | Explicit flag for external changes | Pass | The script only reads the API; no subcommand writes outside `--out` |
| SH 9 | Header comment | Pass | Subcommands, the definition of a deployment, usage, environment, exit codes and needs |
| SH 10 | Tests | Pass | A hand-made month with a dry run, two failures in a row, a cancelled run, a rollback, a hotfix and a change not deployed; every metric is checked against a value computed by hand; the job log parser is tested with CRLF line ends |
| YML 1 | yamllint clean, line length 100 | Pass | `tests/lint.sh` |
| DOC 1 | Every score and figure rests on evidence of 2026-10-20; what was not checked is marked | Pass | SAMM-001 section 2 (E1 to E18); DORA-001 rests on the fetched data and the job logs |
| DOC 2 | No compliance is claimed | Pass | SAMM-001 section 1 says self-assessment, not independent |

## Scorecard results (first run)

Recorded in RC-140 of NFHotel and below, from Scorecard v5.5.0 run locally on 2026-10-20 at 14:22 UTC against `main` (the workflow itself first runs when it reaches `main`).

| Repository | Commit | Score |
| --- | --- | --- |
| NF-Hotel/NFHotel | `7c6f348` | 5.1 / 10 |
| NF-Hotel/DevOps | `9b572d2` | 2.8 / 10 |

The DevOps score reflects that `main` holds only the first commit of the migration until NF-Hotel/DevOps#1 to #10 are merged: no workflow, no `SECURITY.md` and no Dependabot configuration are on `main` yet, so Dangerous-Workflow, Token-Permissions and Pinned-Dependencies are not scored (n/a), and Security-Policy, Dependency-Update-Tool and SAST are 0. Code-Review is 0 because #6 was merged without an approval (SAMM-001 finding 4). Branch-Protection is 5 (1 approval; no code owner review). Binary-Artifacts and Vulnerabilities are 10. Expect the score to rise when the stack is merged; the next weekly run records it. The NFHotel checks, with the reasons, are in RC-140 of NFHotel (NF-Hotel/NFHotel#197).

## Findings

- **The DORA definitions are proxies, stated as such.** A failed CD run counts as a failed deployment, and a deployment counts as rework when it recovers an incident or ships a pull request labelled `hotfix`. The label does not exist yet; until it is used, rework comes only from recoveries.
- **The deployed commit is not `head_sha` of the CD run.** A manual run of CD runs on the head of `main` whatever release it deploys, so the script reads the release run from the job log. Job logs expire after 90 days; the method document says so.
- **CD did not run for the releases of 2026-10-20** (DORA-001 section 3). This is a finding of the report, not of the script; it is action 1 there.
- **SECURITY.md names a reporting channel that is off** (SAMM-001 finding 1). S01 decides.

## Overall Verdict

Go. The workflow, the script and the two documents meet the checklists; the Scorecard workflow's first run on `main` is to be recorded when the pull requests are merged.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| After the merge, record the first run of the workflow in code scanning (category `scorecard`) beside the local run above | S01 | First run on `main` |
| Enable private vulnerability reporting in both repositories, or change `SECURITY.md` | S01 | Next week |
| Find why the releases of 2026-10-20 started no CD run | S01 | Before the November report |
| Report October in full on 2026-11-02 | S01 | 2026-11-02 |

---

[MIL-023]: ../../milestones/mil-023-operational-quality.md
[SAMM-001]: ../samm-baseline.md
[DORA-001]: ../dora/dora-001-2026-10.md
[DSA-001]: ../devsecops-assessment.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
