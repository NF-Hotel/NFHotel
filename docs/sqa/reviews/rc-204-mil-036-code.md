# RC-204 Review of the code of MIL-036 (image scans, security alerts, load test and incidents)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-204 |
| CrossReference | [MIL-036], [ADR-0020], [ADR-0023], [QC-SH-001], [QC-YML-001], [QC-WF-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-24 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [c310ed5] |

---

## Artifact Under Review

- **Instances reviewed:** the change of tasks 1 to 4 of MIL-036 (#289 to #292), pull request #307:
  - `ops/scripts/scan-running-release.sh` and `.github/workflows/running-image-scan.yaml` (new);
  - the Loki ruler in `ops/deploy/monitoring/loki/config.yml`, `loki/rules/fake/security.yml` and `prometheus/rules/security.yml` (new), `rules-test.yml` and `ops/tests/test-monitoring-stack.sh`;
  - `ops/load/nfhotel.js`, `ops/scripts/load-test.sh` and `.github/workflows/load-test.yaml` (new);
  - `ops/scripts/check-log-privacy.sh` (new), its step in `.github/actions/e2e-tests/action.yml`, and the wiki page Incidents;
  - the tests `test-scan-running-release.sh` and `test-check-log-privacy.sh` (new).
- **Checklists used:** [QC-SH-001], [QC-YML-001] and [QC-WF-001]. No C# changed.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).

## Evidence

| Check | Result |
| --- | --- |
| Script tests | `test-scan-running-release.sh` 10 passed, `test-check-log-privacy.sh` 4 passed |
| Rule tests | `promtool test rules` passes, with the new test of StaffSignInFailures beside the tests of MIL-034 |
| Stack test | "failed SSH logins in the journal reach Alertmanager": twelve SSH failure lines pushed to Loki fire SshLoginFailures through the ruler |
| End-to-end run | The new step reports "no personal data of the test guest in the logs of 4 containers" |
| CI of the pull request | 33 checks green, among them the linters (actionlint, shellcheck, shfmt, yamllint), the monitoring configuration, the end-to-end tests and the DAST baseline |

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| SH | Strict mode, quoting, usage and exit codes in the header | Pass | Each new script has `set -euo pipefail`, a usage line, a documented exit code and a `die` or usage helper |
| SH | Input validated before use | Pass | The manifest's sha and image names are checked against digest-pinned patterns before a pull |
| SH | No secret printed | Pass | The GHCR login reads the token from stdin; the log check names the container, never the line |
| SH | Tests with a fake | Pass | Both new tests use a fake `docker`; no network, no real registry |
| WF | Least privilege | Pass | `contents: read` by default; `issues: write`, `actions: read` and `packages: read` only on the scan job |
| WF | Pinned actions and images | Pass | Actions by full SHA; k6 by digest |
| WF | Concurrency and timeouts | Pass | Each workflow has a concurrency group and a `timeout-minutes` |
| YML | Alert rules | Pass | Each has a `for`, a severity and a summary with the value; yamllint clean |
| Privacy | The log policy is checked, not only written | Pass | `check-log-privacy.sh` runs in every end-to-end run |

## Overall Verdict

Go.

## Action Items

| # | Action | Owner | Due |
| --- | --- | --- | --- |
| 1 | After the merge, run the image scan with a known vulnerable test image (the issue path) and once on production, run the load test by hand, deploy the monitoring, and close MIL-036 with its gate review | AI | After the merge |

---

[MIL-036]: ../../milestones/mil-036-security-operations.md
[ADR-0020]: ../../adr/adr-0020-monitoring-stack.md
[ADR-0023]: ../../adr/adr-0023-application-telemetry.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[c310ed5]: https://github.com/NF-Hotel/NFHotel/commit/c310ed55e5b2c580ea6f23484eee3023f479a862
