# RC-077 Review of the deploy files for the back office login (MIL-016)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-077 |
| CrossReference | [MIL-016], [RC-066], [QC-YML-001], [QC-SH-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-17 | Accepted | Claude (AI) | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- Instance reviewed: `deploy/nfhotel-api/nfhotel-api.env.example` (the staff settings) and the changed `scripts/test-compose-proxy.sh`; the compose files are unchanged because the API reads the new settings from its env file
- Checklist used: the criteria of [QC-YML-001] and [QC-SH-001] that apply to an env example and a script (each line names the criterion; the numbering of [RC-066] is kept)
- Scope: the changes of this milestone
- Mode: autonomous, not independent (reviewer is `AI as stand-in for S01`)
- Tools: `yamllint` 1.38.0, `shellcheck` 0.11.0, `shfmt -i 2 -ci`, the smoke test, a deploy to a Proxmox VE container

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | YAML: parses, no duplicate keys, 2-space indentation, passes `yamllint` | N-A | No YAML changed; `yamllint` with `.yamllint` still prints nothing for `deploy/` |
| 2 | YAML: no secret in the file | Pass | The env example holds placeholders only (`<signing-key-of-at-least-32-characters>`, `<password-of-at-least-12-characters>`); the real values are in the git-ignored `nfhotel-api.env`, which `git check-ignore` confirms |
| 3 | YAML: header comment states purpose, tool and decision | Pass | The new block of the env example cites MIL-016 and ADR-0005 and says that the secrets belong to the API only |
| 4 | YAML: exercised by a check that runs without the real target | Pass | The smoke test starts the API with a throwaway signing key and checks, through nginx and from the web container, the redirect to the login, the open login page, the 401 for a staff endpoint with the web token alone, and that the web container holds no signing key; 19 of 19 checks pass |
| 5 | Shell: `set -euo pipefail`, quoting, `shellcheck` | Pass | `shellcheck` 0.11.0 clean for all scripts |
| 6 | Shell: no secret in script, echo or command line | Pass | The smoke test makes its signing key with `openssl rand` into a temporary env file removed at the end; the proof scripts for the VM keep passwords and tokens in shell variables and print only status codes; the `create-staff` command takes the password from the environment, never from an argument |
| 7 | Shell: tested for success, failure and bypass | Pass | Success: the smoke test and a deploy to the Proxmox VE container. Failure: the first deploy attempt with `--skip-vm` stopped with "no VM found" because `deploy.env` has no VM id, and the run without the flag found the VM; a wrong password, a tampered token and a locked user name were refused in the VM. Bypass: the staff header alone, without the web token, is refused (401 with the service code) |
| 8 | Shell: formatted with `shfmt` | Pass | `shfmt -i 2 -ci` clean for `deploy.sh`, `deploy-services.sh` and `test-compose-proxy.sh` |

## Overall Verdict

Go — the applicable criteria pass. The smoke test passes 19 of 19 checks; in the VM the Manager from the environment file signed in through nginx and the web application (302 to the start page, an `HttpOnly` cookie, the rooms page and the revenue page 200), a wrong password gave no cookie, and the public booking flow worked with the web token alone.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| None | | |

---

[MIL-016]: ../../milestones/mil-016-backoffice-login.md
[RC-066]: ./rc-066-database-api-deploy.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
