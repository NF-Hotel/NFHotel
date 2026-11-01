# RC-257 Code review of MIL-053

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-257 |
| CrossReference | [MIL-053], [QC-PY-001], [QC-SH-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-01 | Accepted | mikemike | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- Instance reviewed: the code of [MIL-053] tasks 1 to 5: `ops/deploy/monitoring/scan-exporter/` (Python), `ops/scripts/deploy-monitoring.sh`, `ops/tests/test-scan-exporter.sh` and `ops/tests/test-monitoring-stack.sh` (shell), the compose service, the scrape job, `prometheus/rules/scans.yml` and its tests, and the dashboard
- Checklist used: [QC-PY-001] and [QC-SH-001]
- Scope: full review
- Language and domain: n/a (code)
- Language reviewer: none
- Mode: supervised, not independent. S01 asked in chat on 2026-11-01 for the implementation; the reviewer is the AI that wrote the code, so the review stays open to S01's challenge.

**Evidence run on 2026-11-01** (Docker in WSL on S01's PC):
- `ops/tests/test-scan-exporter.sh`: ruff check and format clean, `mypy --strict` no issues in 14 files, pytest 22 passed.
- `ops/tests/check-monitoring.sh`: 7 of 7 pass, with `promtool test rules` including the 7 new rule tests.
- `ops/tests/test-monitoring-stack.sh` as root: 18 passed, 0 failed (six services, the token file, the scrape, the source state without a token).
- `ops/tests/lint.sh`: actionlint, shellcheck, shfmt and yamllint clean.
- `ops/tests/test-wiki-coverage.sh`: 91 passed.
- One live read against the public APIs without a token: the five workflow runs and the SonarQube Cloud measures matched sonarcloud.io; code scanning reported not read, as designed. The dashboard was opened in a local Grafana and every panel showed data or its no-data text.

## Checklist Results

### QC-PY-001

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | PEP 8 casing | Pass | Modules `snake_case`, classes `PascalCase`, constants `UPPER_SNAKE`; ruff `N` rules clean |
| 2 | Names state purpose | Pass | `open_alert_samples`, `last_analysis_samples`, `read_token`, `SourceError`; no single-letter names outside comprehensions |
| 3 | Formatter and linter, no unexplained suppressions | Pass | ruff format and check; each `noqa` has its reason (`S310` after the https check, `S104` on the compose network, `A002`/`N802` names fixed by `http.server`) |
| 4 | Every signature annotated | Pass | `mypy --strict` |
| 5 | No bare except, cause kept | Pass | Narrow excepts, `raise ... from err`; a failed source is logged and reported as `nfhotel_scan_source_up 0`, not swallowed |
| 6 | No mutable defaults, no shadowed builtins | Pass | `format` in `log_message` is the name `http.server` fixes, marked |
| 7 | Context managers | Pass | `urlopen` in `with`; files read with `Path.read_text` |
| 8 | Docstrings | Pass | Every module, class and public function |
| 9 | Logging, no secrets in logs | Pass | `logging` only; error messages carry the URL path and status, never the headers that hold the token; the token is never a metric |
| 10 | Traces to the Design Class Diagram | N-A | A plain technical task of MIL-053 with no DCD; the design is the milestone's deliverable |
| 11 | Tests for new behaviour, no network | Pass | 22 tests with a fake client: counting, pagination, a failed and a malformed source, the token sent to GitHub only, no token |
| 12 | Strict type checking | Pass | `Any` only in the `Json` alias, with the reason in a comment |
| 13 | Dependencies declared and pinned | Pass | Standard library only at run time; the test tools are pinned in `test-scan-exporter.sh` |
| 14 | DRY | Pass | Metric names and help text once in `prometheus_text.py`; the Sonar metric list once in `samples.py`, used by the collector and the conversion |
| 15 | Imports point inward | Pass | `domain` imports nothing of the package; `application` imports `domain`; `infrastructure` and `presentation` import inward; `main.py` is the composition root |
| 16 | SOLID | Pass | The collector depends on the `JsonClient` protocol it owns; the HTTP client is given to it |
| 17 | Clean architecture folders | Pass | `scan_exporter/domain`, `application`, `infrastructure`, `presentation`; tests in `tests/unit` |
| 18 | File and folder names | Pass | |

### QC-SH-001

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Shebang and strict mode | Pass | `test-scan-exporter.sh` uses `set -euo pipefail`; the changed scripts keep theirs |
| 2 | Quoting, arrays, `[[ ]]` | Pass | shellcheck clean |
| 3 | Names | Pass | `test-scan-exporter.sh`, `scan_token` |
| 4 | shellcheck and `bash -n` | Pass | `lint.sh` |
| 5 | Errors to stderr, non-zero exit | Pass | `die` in `deploy-monitoring.sh`; the warning for a missing token goes to stderr |
| 6 | Temporary files | Pass | The stack test keeps its `mktemp -d` and trap; the exporter test works in a throwaway container |
| 7 | No secret in the script or on a command line | Pass | The token is read from its file by the exporter; the wiki asks for it with `read -s` |
| 8 | Dry run by default | Pass | `deploy-monitoring.sh` still changes nothing without `--apply`; the empty token file is made only with `--apply` |
| 9 | Header comment | Pass | Step 2 of `deploy-monitoring.sh` names the optional token; the new script has purpose, usage, exit and needs |
| 10 | Cites its task | Pass | MIL-053 in each header |
| 11 | Success, failure and bypass tested | Pass | Stack test: no token (empty file, source down); `deploy-monitoring.sh` refuses a token file readable by others or not starting with `github_pat_` (by reading, not tested in CI) |
| 12 | shfmt | Pass | `lint.sh` |
| 13 | Safe to re-run | Pass | `install` makes the empty file only when it is missing; the stack test redeploys |
| 14 | Tools stated | Pass | |
| 15 | Defined once | Pass | The python image is read from the compose file by `test-scan-exporter.sh`, as `check-monitoring.sh` does for its images |

## Overall Verdict

Go: every Mandatory criterion passes or does not apply.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Create the read-only fine-grained token and deploy on `nf-monitoring` (wiki page Monitoring, Security scans); then compare the dashboard with GitHub code scanning (MIL-053 criterion 6) | S01, S03 | 2026-11-08 |

---

[MIL-053]: ../../milestones/mil-053-security-scan-metrics.md
[QC-PY-001]: ../../../framework/qc/code/qc-programming-python.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
