# MIL-035 Secrets as files, and their rotation

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-035 |
| CrossReference | [BC-001], [ADR-0011], [MIL-023], [RC-185] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-24 | Accepted | mikemike | S01 | Initial version; S01 asked in chat on 2026-10-24 for every gap of the DevOps review to be closed in autonomous mode ("Lav allesammen i automode on"), which accepts this plan; accepted after RC-198 Go | pending |

---

## Purpose

Decide whether NF Hotel may claim that no secret is passed to a container as an environment variable, and that each secret can be replaced by a written procedure. Today the database password (inside the connection string), the API token, the staff token signing key, the passport encryption key and the first manager password are environment variables. Anyone who can inspect the containers can read them, which RC-185 recorded for the socket proxy of Alloy.

## Deliverable

- An ADR that chooses Compose secrets (files mounted at `/run/secrets`) over environment variables and over an external vault, with the reasons.
- Each secret of the API, the web application and PostgreSQL is a file in `/etc/nfhotel/secrets` (root, 0600 or readable by the container user only), mounted into the container that needs it and read through `AddKeyPerFile` or a `_FILE` variable.
- The deploy creates the files from the settings files once and stops putting the values into the environment.
- A rotation procedure for each secret, tested on the throwaway stack, in the wiki.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | An ADR records the choice and the alternatives, with the reasons | Yes | No |
| 2 | In production, `docker inspect` of the API, the web application and PostgreSQL shows no secret in `Config.Env` (checked by a script that knows the names of the secrets) | Yes | No |
| 3 | Each container reads its secrets from `/run/secrets`, and the site, the sign-in and the API work after the deploy | Yes | No |
| 4 | The rotation of each secret is written in the wiki and was done once on the throwaway stack in CI | Yes | No |
| 5 | CI is green and the code review against `qc-programming-csharp` and `qc-programming-shell` is Go | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-023 | The hardened containers and their read-only mounts |
| ADR-0011 | The roles and the connection string of the API |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| Success criterion 21 (security): no secret readable outside the process that needs it | Tasks 2 to 4 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-21. The VM changes through CD; the files are created by the deploy.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | ADR for secrets as files | Write the ADR: Compose secrets as files, compared with environment variables and with a vault such as OpenBao. | No | RC-185 |
| 2 | Applications read files | The API reads its secrets from `/run/secrets` as the web application already does; PostgreSQL uses `POSTGRES_PASSWORD_FILE`. | No | criterion 3 |
| 3 | Deploy writes the files | The deploy moves each secret from the settings files into `/etc/nfhotel/secrets`, mounts it, and removes it from the environment; a check fails a deploy that would put a secret into the environment. | No | criterion 2 |
| 4 | Rotation | Write the rotation of each secret, and rotate them on the throwaway stack in CI. | No | criterion 4 |
| 5 | Check and review | Prove the criteria in production; reviews. | No | criterion 5 |

---

[BC-001]: ../business-case.md
[ADR-0020]: ../adr/adr-0020-monitoring-stack.md
[ADR-0011]: ../adr/adr-0011-postgresql-access-for-the-api.md
[MIL-023]: ./mil-023-operational-quality.md
[RC-185]: ../sqa/reviews/rc-185-alloy-docker-discovery.md
