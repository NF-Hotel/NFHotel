# RC-224 Code review of MIL-042

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-224 |
| CrossReference | [MIL-042], [ADR-0030], [QC-SH-001], [QC-WF-001], [QC-YML-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-26 | Accepted | mikemike | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- **Instances reviewed:**
  - the patch layer: `ops/deploy/market-analysis/Dockerfile`, `requirements.txt` (certifi 2026.10.19 with the hash of its wheel from PyPI), the compose build without the source arguments, `frozen-image` removed;
  - the freeze removed from `build-release-images.sh`, `release-steps.sh`, `cd-steps.sh`, `deploy-services.sh`, `scan-image.sh` (`--report-only`), `scan-running-release.sh`, the release-images action, and their tests;
  - `ops/tests/check-patch-layer.sh` and the release check (`packages: read`, the GHCR login, the new step);
  - the pip entry of Dependabot; the wiki pages CI-CD and Host-Hardening, the two READMEs.
- **Checklists used:** [QC-SH-001], [QC-WF-001], [QC-YML-001], the rows that the change touches.
- **Scope:** delta review.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).
- **Tools:** `ops/tests/lint.sh` (actionlint, shellcheck, shfmt, yamllint); `test-cd-steps.sh`, `test-scan-running-release.sh`, `test-deploy-services-env.sh` and `test-scan-gate.sh` in Debian 12; the patch layer built on a stand-in base (python:3.13-slim with certifi 2026.6.17, a stand-in package and the user `app`), checked by `check-patch-layer.sh`, and an image with one changed application file, which it refused.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| SH 1–2 | Shebang, strict mode, quoting, `[[ ]]` | Pass | `check-patch-layer.sh` follows `check-release-build.sh`: `set -uo pipefail`, a `check` that counts failures |
| SH 4 | shellcheck and bash -n | Pass | 0 findings |
| SH 5 | Errors and usage | Pass | A host that cannot build the analysis image (no GHCR login) leaves the service out with the reason, as before; a full SHA is required by the check |
| SH 7 | No secret on a command line or echoed | Pass | The job token goes to `docker login` on standard input |
| SH 11 | Behaviour is tested | Pass | `test-cd-steps.sh` 74/74 (verify now checks eight attestations and refuses an unattested market-analysis digest), `test-scan-running-release.sh` 10/10, `test-deploy-services-env.sh` 14/14, `test-scan-gate.sh` 20/20; the patch layer check passes on a good layer and fails on a changed application file |
| SH 12 | shfmt | Pass | No diff |
| SH 15 | Defined once | Pass | The base digest is written once, in the `FROM` line; the check reads it from there, and the pins from `requirements.txt` |
| WF 7 | Least permissions | Pass | The release check adds `packages: read` at job level only; nothing is pushed |
| WF 8 | Actions pinned by full SHA | Pass | No new action |
| WF 22 | Features verified on the instance that runs them | Fail | Not yet: the GHCR login and the build of the real base run only when this pull request runs the release check |
| YML 1–3 | Parses, formatting, yamllint | Pass | 0 findings |

## Findings

- **No dead freeze code is left.** With `frozen-image` gone no image is frozen, so the frozen paths of the build, the CD verify, the deploy and `--report-only` of the scan were removed instead of kept unused. ADR-0500's digest lives on as the base.
- **pip is removed from the layer,** as in the benchmark image: the service installs nothing, and pip's vendored libraries carry findings of their own. Each release builds from the base again, which still has pip.
- **The first release check found a second blocker.** With certifi fixed, the gate stopped at `zod 4.4.3` (CVE-2023-54404, high, fixed in 4.6.6) in `marimo/_lsp/copilot/node_modules`: the Copilot language server that marimo's editor starts with Node.js. The image has no Node.js and runs `marimo run`, so the layer removes the folder; `check-patch-layer.sh` checks that it is gone and that no Node.js is present. ADR-0030 decision 2 records it.
- **`scripts/test-compose-proxy.sh` and `tests/test-optional-services.sh`** now need a GHCR login (or a given image) for market-analysis; neither runs in CI.

## Overall Verdict

Go-with-conditions. Every Mandatory criterion that the change touches passes except QC-WF-001 criterion 22, which closes when the release check of this pull request is green on github.com.

## Action Items

| # | Action | Owner | Due |
| --- | --- | --- | --- |
| 1 | Confirm the release check is green on the pull request, with `check-patch-layer.sh` passing against the real base | AI | Before the merge |

---

[MIL-042]: ../../milestones/mil-042-patch-market-analysis-image.md
[ADR-0030]: ../../adr/adr-0030-patch-layer-on-frozen-market-analysis-image.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
