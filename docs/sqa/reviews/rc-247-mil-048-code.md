# RC-247 Team database roles, relay and firewall rule (MIL-048 tasks 1 to 3 and 5)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-247 |
| CrossReference | [MIL-048], [ADR-0034], [QC-SH-001], [QC-YML-001], [QC-WF-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-28 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [102cae2] |

---

## Artifact Under Review

- **Instances reviewed:**
  - `ops/scripts/manage-team-db-users.sh` and `ops/scripts/db/team-roles.sql`, with `ops/tests/test-team-db-users.sh`;
  - the stack `ops/deploy/postgres-tailnet` (compose file, `haproxy.cfg`, settings example) and the service `dbrelay` in `ops/scripts/deploy-services.sh`, with `ops/tests/test-deploy-services-env.sh`;
  - the rule for 5432 on `tailscale0` in `ops/deploy/host/nftables/nfhotel.nft`, with `ops/tests/test-nftables.sh`;
  - the new job of `.github/workflows/ops-ci.yaml`;
  - the wiki page Database-Access, and the changes to CIS-001 and DSA-001.
- **Checklists used:** [QC-SH-001], [QC-YML-001], [QC-WF-001].
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| SH 11 | The scripts are tested | Pass | Each test was run on Docker, and all passed: `test-team-db-users.sh` (25 checks, against PostgreSQL 16 over TCP as DBeaver connects), `test-nftables.sh` (30, 3 of them new) and `test-deploy-services-env.sh` (17, 3 new) |
| SH 1–4, 12 | Lint | Pass | `ops/tests/lint.sh` is clean: layout, actionlint, shellcheck, shfmt and yamllint |
| SH 7 | Safe by default | Pass | Dry run without `--apply`; `--add` of an existing role is refused; a failed `--add` deletes its password file |
| SH 9 | No secret on a command line or in output | Pass | The passwords go to `psql` on standard input; `log_statement` is off in that session; the tests check that neither the script output nor the server log holds the password |
| SH | Refusals are real | Pass | The checks for refused statements need "permission denied" or "must be owner", so a failed login cannot pass as a refusal |
| YML | The relay compose file | Pass | Image pinned by digest; `read_only`, `cap_drop: [ALL]`, `no-new-privileges`, memory, CPU and PID limits. Tried on Docker: a connection through the relay works, and it follows a new address of the database container through Docker's DNS (`IP changed … by 'docker/dns'`) |
| YML | The port is bound to the tailnet address only | Pass | `${NFHOTEL_TAILNET_IP:?}` stops compose without the address, so the relay cannot publish on every interface by mistake |
| WF 1–21 | The new job | Pass | Pinned checkout, `persist-credentials: false`, a time limit, no secret, and the job permissions inherited from the workflow (`contents: read`) |
| WF 22 | Verified on GitHub | Not yet | Closes with the green Ops CI of this pull request |
| MIL-048 3 | The internet and the LAN do not reach 5432 | Pass (in test) | `test-nftables.sh`: a published 5432 is filtered from the internet side and reaches the container from `tailscale0`, also after Docker replaces its table. On the VM, it is checked at the gate |

## Findings

- **The Tailscale policy may still allow every device on the tailnet.** If so, the grant restricts nothing, and the database password is the only lock. The wiki page says so, and changing the policy is S01's decision (MIL-048 task 4).
- **`ALTER SYSTEM SET log_line_prefix`** changes the log format of the whole server, not only for the team. No parser of the logs depends on the old format: Alloy sends the lines to Loki as they are.

## Overall Verdict

Go-with-conditions. QC-WF-001 criterion 22 closes with the green Ops CI of this pull request. MIL-048 criteria 2, 3 and 5 are checked on `nf-hotel` at the gate.

## Action Items

| # | Action | Owner | Due |
| --- | --- | --- | --- |
| 1 | Write `postgres-tailnet.env` on `nf-hotel`, deploy, apply the firewall, and add the four roles | AI and S01 | MIL-048 gate |
| 2 | Add the Tailscale grant and hand each person their password | S01 | MIL-048 gate |

---

[MIL-048]: ../../milestones/mil-048-team-database-access.md
[ADR-0034]: ../../adr/adr-0034-team-database-access.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[102cae2]: https://github.com/NF-Hotel/NFHotel/commit/102cae2e4dd44d67b4d8c71f0ad731456d1938e5
