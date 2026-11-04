# RC-280 Review of ADR-0037, the code and the security audit report of MIL-059

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-280 |
| CrossReference | [MIL-059], [ADR-0037], [SAR-001], [QC-ADR-001], [QC-WF-001], [QC-YML-001], [QC-SH-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-04 | Accepted | Claude (AI) | AI as stand-in for S01 | Initial version; mode autonomous, not independent; S01 accepted the plan of MIL-059 in chat on 2026-11-04 ("Ja, inkl. indstillinger") | [742ce19] |

---

## Artifact Under Review

- Instance reviewed: [ADR-0037] (the hardening policy); the code of [MIL-059] tasks 2 to 4 on the branch `mil-059-github-actions-security-audit`: `.github/workflows/ci.yaml`, `.github/workflows/running-image-scan.yaml`, `.github/workflows/ops-ci.yaml`, `.github/dependabot.yml` and `ops/tests/test-workflow-policy.sh`; and the security audit report [SAR-001] of task 1 and task 8. MIL-059 task 8 names RC-279 for the ADR; the ADR was written after RC-279, so its review is here.
- Checklist used: [QC-ADR-001] for the ADR; [QC-WF-001] and [QC-YML-001] for the workflows and the Dependabot configuration; [QC-SH-001] for the test script; the report against the content the brief of S01 asked for (no checklist exists for the type SAR)
- Scope: full review
- Language and domain: n/a for the ADR and the code; en / it for the report (its Metadata rows)
- Language reviewer: none
- Mode: autonomous, not independent. S01 asked in chat on 2026-11-04 for the audit and accepted the plan; the reviewer is the AI that wrote the code and the documents, so every decision stays open to S01's challenge.

**Evidence run on 2026-11-04** (S01's PC; Git Bash for the policy test, WSL with Docker 29.7.2 for the containers):

- `bash ops/tests/test-workflow-policy.sh --self-test`: 8 passed, 0 failed; `bash ops/tests/test-workflow-policy.sh`: the 14 workflows and actions follow the policy.
- `bash ops/tests/lint.sh`: layout, actionlint 1.7.7, shellcheck 0.11.0, shfmt 3.14.1 and yamllint 1.35.1 clean.
- `test-cd-steps.sh`: 74 passed; `test-deploy-release.sh`: 59 passed; both unchanged by the milestone.
- `bash ops/tests/test-wiki-coverage.sh`: 91 passed.
- The settings of SEC-03, SEC-07 and SEC-12 applied through the API at 10:45 UTC with the values before and after in SAR-001 section 11; SEC-08 did not apply through the API and is listed for S01.
- The checks of the pull request, the SonarQube Cloud analysis of a pull request of this repository included, are read after the push and recorded on the pull request.

## Checklist Results

### QC-ADR-001 (ADR-0037)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | ID matches the file name, unique and sequential | Pass | `ADR-0037`, `adr-0037-github-actions-hardening-policy.md`, after ADR-0036 |
| 2 | Context states the problem, the forces and the options | Pass | The campaign's techniques, the four forces (review alone, a checking tool as a dependency, readable rules, host settings), three options |
| 3 | At least two options considered | Pass | The host's settings alone, a third-party scanner (zizmor), the repository's own policy test with the settings behind it |
| 4 | Decision stated plainly and matches an option | Pass | "We follow option 3", with the seven rules, the settings and the account controls named |
| 5 | Positive and negative consequences | Pass | Three of each |
| 6 | Quality-attribute impacts named | Pass | Security, maintainability, flexibility |
| 7 | Affected Artifacts linked and in CrossReference | Pass | MIL-059, DSA-001, ADR-0007, ADR-0019, ADR-0021, ADR-0032, all six in CrossReference |
| 8 | Status changes as Version History rows | Pass | Proposed, set to Accepted on this Go, `pending` until the commit |
| 9 | Superseded or deprecated ADR names its successor | N-A | |
| 10 | Language of the domain, no unexplained abbreviations | Pass | SHA, OIDC and CI are the terms of the pipeline's own documents |
| 11 | Status is one of the allowed values | Pass | |

### QC-WF-001 (ci.yaml, running-image-scan.yaml, ops-ci.yaml)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Meets the YAML checklist | Pass | Below |
| 2 | In `.github/workflows/` with one extension | Pass | `.yaml` throughout |
| 3 | `name`, `on` and `jobs`, explicit events with branches | Pass | Unchanged triggers |
| 4 | Header comment: purpose, triggers, secrets, changes outside | Pass | The headers stand; `ci.yaml` names `SONAR_TOKEN` and what runs without it |
| 5 | Every job and step named | Pass | "Test the workflow policy check", "Check the workflows against the policy (ADR-0037)", "Log out of GHCR" |
| 6 | Passes actionlint, version stated | Pass | actionlint 1.7.7 in `lint.sh`; the headers say "Verified with actionlint 1.7.7" |
| 7 | Least `permissions` per job, write only where needed | Pass | `contents: read` at the top of all ten; the policy test's rule 2 now refuses anything else |
| 8 | Every action pinned to a full SHA | Pass | Section 4 of SAR-001; rule 1 of the policy test; the repository setting requires it |
| 9 | Secrets only as `secrets.NAME` through `env:` of a step | Pass | `SONAR_TOKEN` on the two scanner steps; the job holds `secrets.SONAR_TOKEN != ''` only; rule 5 |
| 10 | No untrusted text interpolated into `run:` | Pass | `$GITHUB_WORKSPACE` replaced the one expression; rule 6 |
| 11 | `pull_request_target` and `workflow_run` do not run untrusted code | Pass | Unchanged: Release and CD check the source and check out the tested SHA; rule 4 refuses `pull_request_target` |
| 12 | A state-changing workflow starts by hand or from a protected branch, with a dry run | Pass | Unchanged: the running image scan writes an issue, daily or by hand; CD defaults to a dry run |
| 13 | The token has only the scope the job needs | Pass | The job token everywhere; no personal token |
| 14 | `runs-on` and `timeout-minutes` on every job | Pass | Unchanged |
| 15 | `concurrency` where runs conflict | Pass | Unchanged |
| 16 | Triggers filtered | Pass | Unchanged |
| 17 | Tools at pinned versions | Pass | The linters' images, the scanner and coverage tools at versions; the policy test needs no tool |
| 18 | A dependency cache where it matters | N-A | Unchanged |
| 19 | A failing step names the tool, the file and the reason | Pass | The policy test prints `FAIL  rule N  file:line  reason` and `error: N rule(s) broken` |
| 20 | `run:` within the limit on embedded text | Pass | The new steps are one line each |
| 21 | Shared logic as a composite action or reusable workflow | N-A | Nothing shared was added |
| 22 | Features verified on the host | Pass | actionlint locally; the pull request's run on GitHub |
| 23 | A reusable workflow states how a caller pins it | N-A | |
| 24 | A static test of the workflows runs on every pull request | Pass | `test-workflow-policy.sh` in the job Ops linters, a required check |

### QC-YML-001 (the three workflows and dependabot.yml)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Parses, no duplicate keys | Pass | yamllint and actionlint |
| 2 | Two spaces, LF, one final newline, no trailing space | Pass | yamllint with `ops/.yamllint` |
| 3 | Passes yamllint with the committed configuration | Pass | `lint.sh` |
| 4 | Coercible scalars quoted | Pass | Unchanged values; the new ones are plain words |
| 5 | Only safe types | Pass | |
| 6 | No secret in the file | Pass | `secrets.SONAR_TOKEN` by name |
| 7 | Embedded program text at most five lines | Pass | The new `run:` values are one line |
| 8 | Repeated blocks defined once | Pass | The Dependabot folders are a list |
| 9 | Anchors only where supported | N-A | |
| 10 | Validates against the tool's schema | Pass | actionlint for the workflows; Dependabot parses `dependabot.yml` after the merge |
| 11 | Header comment states purpose, tool and decision | Pass | `dependabot.yml` and the workflows have one; the new folders carry a comment with MIL-059 |
| 12 | Key names follow the tool's convention | Pass | |
| 13 | Block scalars with chomping where it matters | N-A | |
| 14 | Lowercase kebab-case file names, one extension style | Pass | |
| 15 | A change exercised by a check without the real target | Pass | `lint.sh` and the policy test |

### QC-SH-001 (ops/tests/test-workflow-policy.sh)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | `#!/usr/bin/env bash` and `set -euo pipefail`, or the exception explained | Pass | `set -uo pipefail` with the comment that every rule runs and the broken ones are counted, as the other tests of `ops/tests` do |
| 2 | Quoted expansions, arrays, `[[ ]]`, `$(...)` | Pass | shellcheck clean |
| 3 | Names | Pass | `test-workflow-policy.sh`, `check_pins`, `SHA_PIN` |
| 4 | shellcheck and `bash -n` clean | Pass | shellcheck 0.11.0 in `lint.sh`; no `disable` comment |
| 5 | Errors to standard error with `error:`, usage on bad arguments | Pass | `error: N rule(s) broken` and `error: no workflow or action under` to standard error; `--help` prints the header |
| 6 | `mktemp` with a `trap` | Pass | The self-test's fixtures |
| 7 | No secret written, echoed or on a command line | Pass | The script reads files only |
| 8 | A state-changing script defaults to a dry run | N-A | It changes nothing |
| 9 | Header comment with purpose, usage, exit codes, needs | Pass | |
| 10 | Implements a cited task or design | Pass | MIL-059 task 4, ADR-0037, QC-WF-001 criterion 24 |
| 11 | Tested for success and failure | Pass | The self-test: the good fixture passes, each of seven fixtures fails with its rule |
| 12 | Formatted with shfmt | Pass | shfmt 3.14.1 in `lint.sh` |
| 13 | Safe to re-run | Pass | Read-only; the fixtures are made in a fresh temporary folder |
| 14 | Bash version and tools stated | Pass | "Needs: bash 4+, grep, awk, sed, mktemp" |
| 15 | Each sequence, constant and message defined once | Pass | `broken`, `code`, `files` helpers; the rule messages in one place each |

### SAR-001 against the content S01 asked for

| # | Content asked for | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Summary of the investigated compromise indicators | Pass | Section 3, twelve indicators with what was checked and the result |
| 2 | Confirmed findings against suspected risks, with a conclusion on compromise | Pass | Section 2: no evidence; what stays inconclusive; section 10 separates Fixed, Open, Accepted and Unverified |
| 3 | Third-party action inventory | Pass | Section 4: nine external actions classified and verified pin by pin, four local |
| 4 | Credential exposure assessment, names only | Pass | Section 5; no value, no webhook address, no identifier of the tailnet trust in the report |
| 5 | CI/CD permission assessment | Pass | Section 6, every workflow |
| 6 | Deployment security assessment | Pass | Section 7, the five steps from the release to the VM and the rollback |
| 7 | Implemented improvements, with tests | Pass | Section 11, with the commit of each and the settings before and after |
| 8 | Test results | Pass | Section 12 |
| 9 | Remaining risks | Pass | Section 13 |
| 10 | Manual actions of the administrators | Pass | Section 14, seven numbered actions with the place in the interface |
| 11 | Findings table with severity, evidence, remediation and status | Pass | Section 10, fourteen rows, the counts stated |
| 12 | No speculative claim about a member's account | Pass | Section 9 says what the audit cannot tell and how S01 verifies it |

## Overall Verdict

Go: every Mandatory criterion passes for the ADR, the workflows, the configuration, the script and the report. The report records one setting that did not apply through the API (SEC-08) as Open for S01 instead of claiming it; the remaining findings are S01's own actions outside the repository.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| The manual actions of SAR-001 section 14: two-factor authentication, the roles, the secret scanning options, the GitHub Apps, the fork policy, the audit log | S01 | 2026-11-11 |
| Review and merge the pull request of MIL-059 (ADR-0019: the owner approves) | S01 | 2026-11-11 |

---

[MIL-059]: ../../milestones/mil-059-github-actions-security-audit.md
[ADR-0037]: ../../adr/adr-0037-github-actions-hardening-policy.md
[SAR-001]: ../security-audit-2026-11.md
[QC-ADR-001]: ../../../framework/qc/artifacts/qc-adr.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[742ce19]: https://github.com/NF-Hotel/NFHotel/commit/742ce193969dda9c8556362bd0c65c2d49f60c14
