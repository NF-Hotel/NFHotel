# RC-504 Review of the end-to-end stack, test runner and DAST scan (MIL-027 of NFHotel)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-504 |
| CrossReference | [MIL-027], [QC-SH-001], [QC-WF-001], [QC-YML-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [f824b5b] |

---

## Artifact Under Review

- **Instances reviewed:**
  - `scripts/e2e-stack.sh`, `scripts/run-e2e-tests.sh`, `scripts/dast-scan.sh` and `deploy/e2e/nginx-no-host-ports.yml` (new);
  - the actions `.github/actions/e2e-tests` and `.github/actions/dast-scan` (new);
  - `tests/test-dast-rules.sh` and `tests/check-e2e-stack.sh` (new), and the CI job "End-to-end stack (MIL-027)".
- **Checklists used:** [QC-SH-001], [QC-WF-001], [QC-YML-001].
- **Scope:** full review of the new files.
- **Language and domain:** n/a (technical type).
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`). MIL-027 lives in NF-Hotel/NFHotel and was accepted there after RC-122.
- **Tools:** `tests/lint.sh` (actionlint 1.7.7, shellcheck 0.11.0, shfmt 3.14.1, yamllint 1.35.1); `tests/test-dast-rules.sh` in Debian 12. A local run against NFHotel `main` plus the end-to-end project: the stack, `check-e2e-stack.sh`, the end-to-end tests and the ZAP baseline.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| SH 1–2 | Shebang, `set -euo pipefail`, quoting, arrays, `[[ ]]` | Pass | The three scripts; the two tests use `set -uo pipefail` so they reach every check |
| SH 3 | Names and constants | Pass | `PLAYWRIGHT`, `ZAP` and `CURL` (tag and digest), `ROLES`, `TARGET`, `MAX_DAYS` are readonly |
| SH 4 | shellcheck and bash -n | Pass | 0 findings; one SC2016 disable, with the reason on the line above (the curl script is expanded inside its container) |
| SH 5 | Errors and usage | Pass | Each failed start, build, sign-in or scan stops with `error:`; usage on exit 2; the gate names each blocking alert |
| SH 7 | No secret in a script, on a command line or echoed | Pass | Every secret is generated per run and kept in files of mode 600 under a 700 folder. The passwords reach the containers by `--env-file`, never by argument, and are never printed; `stack.env` is printed nowhere |
| SH 8 | Explicit flag for external changes | Pass | Nothing outside the runner's Docker changes. The stack has no host port (`!reset` override); ZAP and the tests attack `https://nginx` on the stack's own network only |
| SH 9 | Header comment | Pass | Every new script and test |
| SH 11 | Behaviour is tested | Pass | `test-dast-rules.sh`: 14 checks (the rules file and the SARIF conversion). `check-e2e-stack.sh`: 10 checks of the running stack. Locally: the 9 end-to-end tests pass, and the ZAP baseline runs signed out and signed in (it reached `/admin/revenue` as the Manager), with 0 High alerts |
| SH 12 | shfmt | Pass | No diff |
| SH 15 | Defined once | Pass | The stack is started by one script for the tests, the scan and the CI check; it uses the compose files of `deploy/` |
| WF 4 | Header comment of each workflow and action | Pass | Both actions state what they start, what they attack, the permissions and what is uploaded |
| WF 7 | Least permissions | Pass | `e2e-tests` needs `contents: read` only; `dast-scan` needs `security-events: write` only with `upload-sarif: 'true'` |
| WF 8 | Actions pinned by full SHA | Pass | setup-dotnet v6.0.0, upload-artifact v4.6.2 and upload-sarif v4.38.2, as elsewhere in this repository; the images are pinned by digest |
| WF 10 | Untrusted text only through env | Pass | Every input reaches `run:` through `env:` |
| WF 20 | run blocks within the YAML limit | Pass | Every `run:` is one command |
| WF 22 | Features verified on the instance that runs them | Fail | Not yet: the actions run on github.com first from the NFHotel pull request of MIL-027 |
| YML 1–3 | Parses, formatting, yamllint | Pass | 0 findings |
| YML 8 | Repetition defined once | Pass | The stack start and removal appear in both actions; a composite action cannot call another local one without a pinned reference, so they stay as one-line calls of the same script |

## Findings

- **Deviation from task 2.** The plan seeds the bookings with `seed-market-data`. That command is only on the MIL-026 branch, not on `main`. So the stack creates the three staff accounts with `create-staff`, and the tests create the room they book through the Manager's room form, which is itself an end-to-end flow. When MIL-026 is merged, the stack can add the bookings.
- **ZAP findings below the gate.** The baseline reported 0 High alerts and these Medium and Low alerts:
  - a Content Security Policy that allows `unsafe-inline` and a wildcard;
  - no `X-Content-Type-Options`, `Permissions-Policy` or cross-origin isolation headers;
  - nginx sends its version in `Server`;
  - one cookie without `Secure` on the public pages.

  They do not fail the gate (High only, as MIL-027 sets it), but they are real hardening work for the web application and nginx.

## Overall Verdict

Go-with-conditions. Every Mandatory criterion passes except QC-WF-001 criterion 22. That one closes when the NFHotel pull request of MIL-027 has run both actions green on github.com.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Run both actions from the NFHotel pull request and record the runs | S01 | MIL-027 task 9 |
| Plan the hardening of the headers, the CSP and `server_tokens` as its own milestone | S01 | Next planning round |

---

[MIL-027]: https://github.com/NF-Hotel/NFHotel/blob/main/docs/milestones/mil-027-dast-and-end-to-end-tests.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[f824b5b]: https://github.com/NF-Hotel/DevOps/commit/f824b5ba4b2a09f7c61df95847cf066bba8fc349
