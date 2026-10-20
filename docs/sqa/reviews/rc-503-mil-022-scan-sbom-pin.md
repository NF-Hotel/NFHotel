# RC-503 Review of the scan, SBOM, provenance and pinning change (MIL-022 tasks 1, 2 and 4)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-503 |
| CrossReference | [MIL-022], [DSA-001], [ADR-0500], [QC-SH-001], [QC-WF-001], [QC-YML-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [8806b2b] |

---

## Artifact Under Review

- **Instances reviewed:**
  - `scripts/scan-image.sh`, `scripts/scan-gate.sh` and `security/trivyignore.yaml` (new);
  - the scan, the build-then-push order and the digest files in `scripts/build-release-images.sh`, and the digest outputs of `scripts/release-steps.sh`;
  - the SARIF upload and attestation steps of `.github/actions/release-images/action.yml`;
  - `.github/dependabot.yml` (new), the digest pins in `deploy/*/Dockerfile` and `deploy/*/docker-compose.yml`, the removal of pip from the benchmark image;
  - `tests/test-scan-gate.sh` (new), the CI matrix and `.yamllint`.
- **Checklists used:** [QC-SH-001], [QC-WF-001], [QC-YML-001], the rows that the change touches.
- **Scope:** full review of the new files, delta review of the changed ones (earlier record RC-501).
- **Language and domain:** n/a (technical type).
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).
- **Tools:** `tests/lint.sh` (actionlint 1.7.7 with shellcheck, shellcheck 0.11.0, shfmt 3.14.1, yamllint 1.35.1); `tests/test-scan-gate.sh` in Debian 12; a local build of NFHotel `1d50f50` with the scan and Trivy 0.75.0.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| SH 1–2 | Shebang, `set -euo pipefail`, quoting, arrays, `[[ ]]` | Pass | Both new scripts; the test uses `set -uo pipefail` so it reaches every check |
| SH 3 | Names and constants | Pass | `TRIVY` (tag and digest), `IGNORE`, `MAX_DAYS` are readonly |
| SH 4 | shellcheck and bash -n | Pass | 0 findings, after SC2001 in `scan-gate.sh` was fixed |
| SH 5 | Errors and usage | Pass | Every failed scan, conversion or empty SBOM stops with `error:`; usage on exit 2; the gate names each blocking finding with its fix |
| SH 7 | No secret in a script, on a command line or echoed | Pass | Trivy reads local images through the daemon socket; no registry credential is passed to it |
| SH 8 | Explicit flag for external changes | Pass | Nothing leaves the machine without `--push`; `--no-scan` is refused with `--push`; a failed scan now stops the release before any image is pushed (build and scan all, then push) |
| SH 9 | Header comment | Pass | Every new script and the test |
| SH 11 | Behaviour is tested | Pass | `test-scan-gate.sh`: 17 checks (critical with and without a fix, high with and without, medium, missing report, usage, the ignore file rules). The local build: web 110, api 125 and benchmark 161 packages in their SBOMs, all three pass the gate |
| SH 12 | shfmt | Pass | No diff |
| SH 15 | Defined once | Pass | The gate rule lives only in `scan-gate.sh`; the Trivy image only in `scan-image.sh` |
| WF 4 | Header comment of each workflow and action | Pass | `action.yml` states what changes outside the repository and the permissions each input needs |
| WF 7 | Least permissions | Pass | The action needs `id-token`, `attestations` and `packages: write` only with `push: 'true'`, and `security-events: write` only with `upload-sarif: 'true'`; the callers hold them |
| WF 8 | Actions pinned by full SHA | Pass | upload-sarif v4.38.2, attest-build-provenance v4.2.2, attest v4.2.2, all released more than two weeks ago; `actions/attest-sbom` is not used (deprecated) |
| WF 10 | Untrusted text only through env | Pass | The digest outputs come from `docker image inspect` after a check against `sha256:<64 hex>` |
| WF 20 | run blocks within the YAML limit | Pass | No new `run:` block |
| WF 22 | Features verified on the instance that runs them | Fail | Not yet: the SARIF upload, the attestations and Dependabot run only on github.com, the last two only on a release from `main` and on the default branch |
| YML 1–3 | Parses, formatting, yamllint | Pass | 0 findings; `.yamllint` now allows a long unbreakable mapping value (an image with tag and digest), stated in a comment |
| YML 8 | Repetition defined once | Pass | The four upload steps and eight attestation steps differ only by image name: a composite action cannot loop over `uses:`. Accepted as the documented limit of the consumer |
| YML 11 | Header comment | Pass | `dependabot.yml` and `trivyignore.yaml` state their purpose, reader and rules |

## Findings

- **The gate found real blockers.** The benchmark image had four fixable high findings: setuptools 70.3.0, urllib3 2.7.0 (two) and msgpack 1.1.2. All four were in the copies that pip vendors in the Python base image. pip is now removed after the install. The image still imports `marimo` and `nfbench`, and it passes the gate.
- **The frozen image.** `market-analysis` is scanned report-only in a release (ADR-0500). It gets no attestation, because it was not built by this release. Task 3 (verify in CD) must therefore accept the frozen digest by its pin, or the freeze must end first.

## Overall Verdict

Go-with-conditions. Every Mandatory criterion that the change touches passes except QC-WF-001 criterion 22. That one closes when a release from `main` has run with the new pin and `gh attestation list` shows both attestations for each built digest (MIL-022 criterion 2).

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Give the NFHotel Release job `id-token: write`, `attestations: write` and `security-events: write`, and bump the DevOps pin there | S01 | With the next NFHotel pin pull request |
| Pin the .NET images and the NFHotel CI actions by digest and SHA, and add Dependabot for `nuget`, `docker` and `github-actions` there | S01 | Same pull request (task 4, NFHotel part) |
| Set the organization Actions policy to require SHA pinning | S01 | Before MIL-022 criterion 4 is claimed |
| Prove MIL-022 criterion 1 with a pull request that adds a known critical vulnerability, then revert it | S01 | Task 7 |

---

[MIL-022]: ../../milestones/mil-022-supply-chain-security.md
[DSA-001]: ../devsecops-assessment.md
[ADR-0500]: ../../adr/adr-0500-freeze-market-analysis-image.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[8806b2b]: https://github.com/NF-Hotel/DevOps/commit/8806b2bab34940e5ede5b195773e142a173307f7
