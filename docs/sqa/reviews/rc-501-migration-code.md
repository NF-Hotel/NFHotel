# RC-501 Review of the code added or changed by the migration to NF-Hotel/DevOps

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-501 |
| CrossReference | [ADR-0012], [MIL-021], [MIL-024], [QC-SH-001], [QC-WF-001], [QC-YML-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-19 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [c1dc5f7] |

---

## Artifact Under Review

- **Instances reviewed:**
  - the actions `.github/actions/release-images` and `.github/actions/deploy-release`;
  - `scripts/action-ref.sh`, `scripts/release-steps.sh`, and the changes to `build-release-images.sh`, `deploy-release.sh`, `cd-steps.sh` and `install-deploy-user.sh`;
  - the compose build changes;
  - `.github/workflows/ci.yaml` and `tests/`.

  The rest of the code was copied unchanged; its reviews are RC-094, RC-095 and RC-101.
- **Checklists used:** [QC-SH-001], [QC-WF-001], [QC-YML-001] (the rows that the change touches; the others are unchanged from RC-094 and RC-095).
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).
- **Tools:** `tests/lint.sh` (actionlint 1.7.7 with shellcheck, shellcheck 0.11.0, shfmt 3.14.1, yamllint 1.35.1); the tests in Debian 12 and with PostgreSQL 16.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| SH 1–2 | Shebang, `set -euo pipefail`, quoting, arrays, `[[ ]]` | Pass | The new scripts `action-ref.sh`, `release-steps.sh`, the tests and `lint.sh`; the tests use `set -uo pipefail` on purpose (they must reach every check), stated by their structure |
| SH 3 | Names and constants | Pass | `DEVOPS_REPOSITORY`, `FULL_SHA`, `EF_VERSION`, the image names are readonly constants |
| SH 4 | shellcheck and bash -n | Pass | `tests/lint.sh`: shellcheck 0.11.0 `-x` on every `*.sh`: 0 findings, after SC2153 in `cd-steps.sh` was fixed by a rename |
| SH 5 | Errors and usage | Pass | `::error::` in the action scripts; usage on exit 2 |
| SH 7 | No secret in a script, on a command line or echoed | Pass | No secret is used; the CI has none |
| SH 8 | A script that changes state outside its folder needs an explicit flag | Pass | Pushing needs `push: 'true'`; the deploy needs `apply: 'true'`; the tests change only local containers, named `t-*`, and remove them |
| SH 9 | Header comment | Pass | Every new script and test |
| SH 11 | Behaviour is tested | Pass | deploy-release 20, cd-steps and action-ref 25, VM setup 17, nginx 10, database 34 checks, all passing locally |
| SH 12 | shfmt | Pass | `shfmt -i 2 -ci -d` on every `*.sh`: no diff, after `create-docker-vm.sh` (copied from NFHotel main, never checked there) was formatted |
| SH 15 | Defined once | Pass | `action-ref.sh` is shared by both actions; the linter versions are constants of `lint.sh` |
| WF 4 | Header comment of each workflow and action | Pass | `ci.yaml` and both `action.yml` state triggers, secrets (none), what changes outside the repository and the decision |
| WF 7 | Least permissions | Pass | `ci.yaml`: `contents: read`; the actions cannot set permissions, the NFHotel jobs hold them (ADR-0012) |
| WF 8 | Actions pinned by full SHA | Pass | checkout v7.0.1, setup-dotnet v6.0.0, download-artifact v4.3.0 and tailscale v4.2.0 by SHA; NFHotel pins these actions by SHA, enforced by `action-ref.sh` |
| WF 10 | Untrusted text only through env | Pass | `inputs.nfhotel-ref` goes to `actions/checkout` `ref:` only; every `run:` is one command with env |
| WF 11 | workflow_run and pull_request_target | N-A | Not used here; the CI has no secret or write token |
| WF 14–16 | runs-on, timeout, concurrency, filters | Pass | `ubuntu-24.04`, a time limit per job, concurrency cancelling older runs, triggers on `main` only |
| WF 20 | run blocks within the YAML limit | Pass | Every `run:` is one command; the logic is in `tests/` and `scripts/` |
| WF 22 | Features verified on the instance that runs them | Fail | Not yet: the CI runs on github.com when the pull request is opened; the remote use of the actions from NFHotel is verified by the NFHotel boundary pull request |
| YML 1–3 | Parses, formatting, yamllint | Pass | yamllint 1.35.1 strict with the committed `.yamllint`: 0 findings |

## Overall Verdict

Go-with-conditions — every Mandatory criterion that the change touches passes except QC-WF-001 criterion 22. That one closes when the DevOps CI and the NFHotel `release-check` workflow have run on github.com.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Record the CI runs on github.com (closes WF 22) | S01 | Before the merge of the migration pull request |
| Independent review of the migration pull request | Tirsvad (S01) | Before the merge |

---

[ADR-0012]: https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0012-devops-repository-ownership.md
[MIL-021]: ../../milestones/mil-021-secure-cicd-foundation.md
[MIL-024]: ../../milestones/mil-024-postgresql-api-readiness.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[c1dc5f7]: https://github.com/NF-Hotel/DevOps/commit/c1dc5f74e11ce4e10f7f1dbdf5ef304e4db8429e
