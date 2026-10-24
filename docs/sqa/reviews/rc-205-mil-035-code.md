# RC-205 Review of the code of MIL-035 (secrets as files, and their rotation)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-205 |
| CrossReference | [MIL-035], [ADR-0024], [ADR-0011], [QC-CS-001], [QC-SH-001], [QC-YML-001], [QC-WF-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-24 | Accepted | mikemike | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- **Instances reviewed:** the change of tasks 1 to 4 of MIL-035 (#284 to #287), pull request #308:
  - `ops/scripts/write-secret-files.sh` and `ops/scripts/check-secret-env.sh` (new);
  - the compose files of PostgreSQL, the API and the web application, and `ops/.gitignore`;
  - `deploy-services.sh`, `deploy-release.sh`, `e2e-stack.sh`, `test-compose-proxy.sh` and `build-release-images.sh`;
  - the tests `test-write-secret-files.sh`, `test-check-secret-env.sh` and `test-secret-rotation.sh` (new), `test-deploy-services-env.sh`, and the new job of `ops-ci.yaml`;
  - ADR-0024, the wiki page Proxmox-Deployment and the README.
- **Checklists used:** [QC-SH-001], [QC-YML-001], [QC-WF-001], and [QC-CS-001] for the reading side in `Program.cs` of the API and the web application, which did not change.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).

## Evidence

| Check | Result |
| --- | --- |
| Script tests | `test-write-secret-files.sh` 10 passed, `test-check-secret-env.sh` 5 passed, `test-deploy-services-env.sh` 12 passed, `test-deploy-release.sh` 59, `test-cd-steps.sh` 74 and `test-switch-vm-checkout.sh` 14 passed |
| Throwaway stack | `e2e-stack.sh up` ends with `check-secret-env.sh`: PostgreSQL, the API and the web application have no secret in their environment; `/run/secrets` holds the files; `/book` answers 200 |
| Rotation drill (CI job) | 10 passed: the old API token, the old signing key's session and the old database password are refused; the new values work; the web application was recreated with the new token; the API starts without the first Manager's password |
| CI of the pull request | 33 checks green, among them the end-to-end tests and the DAST baseline on the new compose files, the release build and the smoke test of the proxy |

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| CS | Configuration order | Pass | `AddKeyPerFile("/run/secrets", optional: true)` is added after the environment provider in both programs, so a file wins over a variable of the same name; nothing else in C# was needed |
| SH | One place for the knowledge | Pass | The list of the secrets is in `write-secret-files.sh` only; the check reads it with `--list` |
| SH | Values whole and never printed | Pass | Split at the first `=`, CR removed; the files are written with `printf '%s'` and moved into place; the check prints names only |
| SH | Input validated | Pass | A `;` or quote in the database password is refused, because it would break the connection string |
| SH | Formatting | Pass | shfmt caught unquoted map keys with a `-` in CI; quoted and green |
| YML | Compose | Pass | Each secret is declared once at the top level and mounted by name; yamllint clean |
| WF | The new job | Pass | `contents: read`, pinned checkout, a timeout; no secret needed |
| Security | Rollback | Pass | A secret in the environment after a deploy fails `deploy-release.sh`, which rolls back; a release from before has no check and deploys as before |
| Security | Host protection | Noted | The files are 0444 in a 0700 folder of `nfhotel`, as the settings files they come from; ADR-0024 decision 4 gives the reason |

## Overall Verdict

Go.

## Action Items

| # | Action | Owner | Due |
| --- | --- | --- | --- |
| 1 | After the CD run, check production with `check-secret-env.sh` and the site, and close MIL-035 with its gate review | AI | After the merge |
| 2 | The passport key cannot be rotated (ADR-0024 decision 7); a key ring and a re-encryption command need a milestone of their own | S01 | To decide |

---

[MIL-035]: ../../milestones/mil-035-secrets-as-files.md
[ADR-0024]: ../../adr/adr-0024-secrets-as-files.md
[ADR-0011]: ../../adr/adr-0011-postgresql-access-for-the-api.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
