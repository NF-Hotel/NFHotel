# RC-066 Review of the API deploy files and scripts (MIL-015)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-066 |
| CrossReference | [MIL-015], [RC-051], [QC-YML-001], [QC-SH-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S01 | Initial version | [b8170b7] |

---

## Artifact Under Review

- Instance reviewed: `Dockerfile.api`, `deploy/nfhotel-api/`, the changed `deploy/nfhotel-web/docker-compose.yml` and env example, and the changed `scripts/deploy-services.sh`, `scripts/deploy.sh` and `scripts/test-compose-proxy.sh`
- Checklist used: the criteria of [QC-YML-001] and [QC-SH-001] that apply to a compose file and a script (the numbering of [RC-051] is not repeated; each line names the criterion)
- Scope: the changes of this milestone
- Mode: autonomous, not independent (reviewer is `AI as stand-in for S01`)
- Tools: `yamllint` 1.38.0, `docker compose config`, `shellcheck` 0.11.0, `shfmt -i 2 -ci`, the smoke test, a deploy to a Proxmox VE container

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | YAML: parses, no duplicate keys, 2-space indentation, passes `yamllint` | Pass | yamllint 1.38.0 with `.yamllint` prints nothing for `deploy/`; `docker compose config` accepts all seven compose files |
| 2 | YAML: no secret in the file | Pass | The API and the web compose take the database password and the token from the git-ignored env files by variable; the examples hold placeholders |
| 3 | YAML: header comment states purpose, tool and decision | Pass | `deploy/nfhotel-api/docker-compose.yml` cites MIL-015 and ADR-0004 |
| 4 | YAML: exercised by a check that runs without the real target | Pass | The smoke test starts the API and the web service with throwaway settings and checks the network and port rules |
| 5 | Shell: `set -euo pipefail`, quoting, `shellcheck` | Pass | `shellcheck` 0.11.0 clean for the four scripts |
| 6 | Shell: no secret in script, echo or command line | Pass | `deploy-services.sh` reads `Api__Token` and `POSTGRES_*` into the environment and never prints them; the smoke test uses a throwaway token |
| 7 | Shell: tested for success, failure and bypass | Pass | Success: a full deploy of seven services to the Proxmox VE container and the 14-check smoke test. Failure: the first smoke run stopped with `API_TOKEN: readonly variable`, fixed. Bypass: `--down` and service names were not run again in this milestone |
| 8 | Shell: formatted with `shfmt` | Pass | `shfmt -i 2 -ci` clean for `deploy.sh`, `deploy-services.sh` and `test-compose-proxy.sh` |

## Overall Verdict

Go — the applicable criteria pass. The smoke test passes 14 of 14 checks, including that the API publishes no port, the web container is not on the database network and holds no database setting, and the API answers 401 without the token.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| None | | |

---

[MIL-015]: ../../milestones/mil-015-database-api.md
[RC-051]: ./rc-051-mil-004-deploy-files.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[b8170b7]: https://github.com/NF-Hotel/NFHotel/commit/b8170b7a84409f6e19f209090c310a9f42c53bc9
