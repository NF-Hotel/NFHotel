# RC-250 Review of the MIL-049 reference pages and the wiki coverage test

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-250 |
| CrossReference | [MIL-049], [QC-SH-001], [QC-YML-001], [QC-WF-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-30 | Accepted | mikemike | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- **Instances reviewed:**
  - `docs/wiki/Workflows.md`, `docs/wiki/Containers-and-Images.md`, `docs/wiki/Security-Controls.md` and `docs/wiki/Alerts.md`, against the Go/No-Go criteria of [MIL-049];
  - the links in `docs/wiki/Home.md`, `docs/wiki/_Sidebar.md` and `docs/wiki/Appendix.md`;
  - `ops/tests/test-wiki-coverage.sh`, against [QC-SH-001];
  - the job `Ops wiki coverage` in `.github/workflows/ops-ci.yaml`, against [QC-WF-001] and [QC-YML-001].
- **Checklists used:** [QC-SH-001], [QC-WF-001], [QC-YML-001], and the criteria of [MIL-049].
- **Scope:** full review.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`). S01 accepted the plan in chat on 2026-10-30 ("Accepter planen").

## Checklist Results

### The pages (MIL-049)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Every workflow, composite action, compose service and alert rule is on its page | Pass | `bash ops/tests/test-wiki-coverage.sh`: 10 workflows, 4 composite actions, 19 compose services and 19 alert rules (18 in Prometheus, 1 in Loki), 56 passed, 0 failed. Removing the backticks around `Watchdog` in a copy made the test fail with `alert rule Watchdog is missing from Alerts.md` |
| 2 | Each choice names why and at least one alternative, or links the ADR that does; each fact names its file | Pass | Each workflow has Why, Alternatives and Decision rows; each control row has Why, Alternatives and Remaining risk; alternatives that no ADR records are marked "Not decided in an ADR". Facts were checked against the files: the rule files and `alertmanager.yml`, `prometheus.yml`, `public.conf.template`, `AddressLoginThrottle.cs`, `StaffAccountService.cs`, the compose files. Two claims were corrected during the review: the public routes (the web application with `/admin` behind Access, not only the booking) and the sign-in throttles (per user name and per address) |
| 3 | No secret, password, token or private address beyond the tailnet names | Pass | A search for addresses, `hc-ping`, Discord webhook URLs, `token=` and `password=` found only the tailnet address of the relay, which was replaced by "the tailnet address of `nf-hotel`". The secret files are named, never their content |
| 4 | Linked from Home, the sidebar and Appendix | Pass | One line in Home, four entries in `_Sidebar.md`, a section "Reference pages in this wiki" in Appendix. The published wiki is checked after the merge (MIL-049 task 6) |

### `ops/tests/test-wiki-coverage.sh` (QC-SH-001)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Shebang and `set -euo pipefail`, or the exception explained | Pass | `set -uo pipefail`; a comment says why `-e` is left out (every check is counted) |
| 2 | Quoted expansions, arrays, `[[ ]]` and `$(...)` | Pass | The names are arrays from `mapfile`; every expansion is quoted |
| 3 | Naming | Pass | `kebab-case.sh`, `snake_case` functions and variables, `ROOT` and `WIKI` as constants |
| 4 | shellcheck and `bash -n` | Pass | `ops/tests/lint.sh`: shellcheck v0.11.0 clean; the one `disable` (SC2015) is explained, as in the other tests |
| 5 | Errors and usage | Pass | No arguments; a failed check prints `FAIL` and the exit is 1, the convention of every test in `ops/tests/` |
| 6 | Temporary files | N/A | None are made |
| 7 | No secret | Pass | Reads only repository files |
| 8 | Changes outside its directory | N/A | Read-only |
| 9 | Header comment | Pass | Purpose, how to run, exit codes and needs |
| 10 | Cites its task | Pass | MIL-049, task 5 |
| 11 | Success and failure tested | Pass | The run above, and the failing copy (criterion 1 of the pages) |
| 12 | shfmt | Pass | shfmt v3.14.1 clean |
| 13 | Safe to re-run | Pass | Read-only |
| 14 | Needs stated; GNU-only options named | Pass | bash, awk, grep; `find -printf` is GNU, which the runner and Debian have |
| 15 | Defined once | Pass | One `check_names` for the four kinds |

### The job `Ops wiki coverage` (QC-WF-001, QC-YML-001)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | YAML checklist | Pass | yamllint 1.35.1 clean (`ops/.yamllint`) |
| 4 | Header comment | Pass | The header of `ops-ci.yaml` now names the wiki coverage, and a comment on the job says why it runs on the runner |
| 5 | Names | Pass | Job `Ops wiki coverage`, steps "Check out" and "Check that the wiki names every workflow, container and alert" |
| 6 | actionlint | Pass | actionlint 1.7.7 clean |
| 7 | Permissions | Pass | The workflow's `contents: read`; no secret |
| 8 | Pinned actions | Pass | `actions/checkout` by commit SHA, as in the other jobs |
| 14 | `runs-on` and `timeout-minutes` | Pass | `ubuntu-24.04`, 5 minutes |
| 16 | Filtered triggers | Pass | The workflow's triggers are unchanged; its header says why it runs on every pull request |
| 20 | Short `run:` | Pass | One line; the logic is the script |

The other criteria of QC-WF-001 (2, 3, 9 to 13, 15, 17 to 19, 21 to 24) are unchanged by this job and were reviewed with the workflow before.

## Overall Verdict

Go: every Mandatory criterion passes. The new check is not a required check of the ruleset; adding it is S01's decision and is not part of MIL-049.

## Action Items

None.

---

[MIL-049]: ../../milestones/mil-049-devops-security-reference.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
