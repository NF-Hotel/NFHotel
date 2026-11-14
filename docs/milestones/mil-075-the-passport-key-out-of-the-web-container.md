# MIL-075 The passport key out of the web container

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-075 |
| CrossReference | [BC-001], [ADR-0004], [ADR-0024], [MIL-015], [MIL-035] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-12 | Accepted | mikemike | S01 | Initial version: the web container gets the passport key as a secret file, though BC-001 objective 18 and the web's compose file say that it holds none, and no code of the web reads it; planned at S01's request in chat ("Skriv 1–4", to the plan of 2026-11-12) | pending |

---

## Purpose

Take the key that encrypts the passport numbers of the guests out of the web container, so that only the database API holds it, as objective 18 of [BC-001] and [ADR-0004] say; and make sure that a secret of one service never reaches the settings of another, so that this change cannot move the key into the environment of the web instead.

The database API ([ADR-0004], [MIL-015]) holds the persistence and the passport key, and the web application gets its use cases through the API. Each secret is a file that only its container reads ([ADR-0024], [MIL-035]). Read on 2026-11-12 at 1c2726b:

| # | Finding | Where | What it allows or breaks |
| --- | --- | --- | --- |
| 1 | The web service mounts `Encryption__PassportKey` as a secret file | `ops/deploy/nfhotel-web/docker-compose.yml` (the service's `secrets` and the top-level `secrets`); `ops/scripts/write-secret-files.sh` (the list `SECRETS[web]` and the loop of the web) | A process that can read the files of the web container, the container that serves the public pages, can read the key that decrypts every passport number in the database. Success criterion 21 is not met |
| 2 | No code of the web reads the key: `Program.cs` calls `AddApiClient`, not the registration of Infrastructure that checks the key at start-up; the comment above the call still describes that registration | `src/NFHotel.Web/Program.cs` | The comment says that the web stops without the key, which is no longer true |
| 3 | The settings file that the deploy writes for a service is the service's settings file on the production virtual machine (VM) without the lines of its own secrets, so a line with the secret of another service passes into it | `write-secret-files.sh`, the step that writes `settings.env` | Taking the key off the web's list alone would put the line `Encryption__PassportKey=` of `nfhotel-web.env` on the VM into the environment of the web container. The check of the environment after the deploy (`check-secret-env.sh`) would find it by its name and roll the deploy back (success criterion 38), but the change must not rest on that check |
| 4 | The web's settings file on the VM, its example file, and the wiki pages Environment-Files and Proxmox-Deployment still list the key for the web, and say that the copies of the API and the web must be equal | `nfhotel-web.env` on the VM, `ops/deploy/nfhotel-web/nfhotel-web.env.example`, `docs/wiki/Environment-Files.md`, `docs/wiki/Proxmox-Deployment.md` | An operator who follows them puts the key back |

## Deliverable

- **The key leaves the web.** `Encryption__PassportKey` leaves the web's list and loop in `write-secret-files.sh` and the web's compose file. The API keeps it as before. The stand-in stacks of the tests (`ops/scripts/lib/stack.sh`, `ops/scripts/test-compose-proxy.sh`) write the key only into the settings of the API, so they need no change.
- **No secret crosses to another service.** `write-secret-files.sh` leaves out of every settings file a line whose key is the secret of any service, not only of its own, and names the lines it left out in a warning, so that a key left in a settings file on the VM never reaches an environment. A secret file that a service no longer lists is deleted from its folder, so the web's copy of the key on the VM goes with the first deploy.
- **The comment and the documents.** The comment in `src/NFHotel.Web/Program.cs`; `nfhotel-web.env.example`, the table of the web and the table of the secrets that must match in Environment-Files, and the table of the secrets in Proxmox-Deployment, without the key for the web; a Version History row of [ADR-0024]. After the deploy, S01 or S03 removes the line from `nfhotel-web.env` on the VM, which ends the warning.

Order: the review of this document first (QC-MIL-001 and QC-LANG-001), then tasks 1 to 3 on one branch with their pull request, task 4 after S01 approves the run of continuous deployment (CD), and task 5 last.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | The tests in `ops/tests/test-write-secret-files.sh` show that the web gets no file and no settings line `Encryption__PassportKey`, also when its settings file holds the line; that a settings line whose key is the secret of another service is left out with a warning, while every setting a service needs stays; that a secret file a service no longer lists is deleted; and that the compose file of the web names no passport key. Each test fails without the change, and Ops CI runs them | Yes | No |
| 2 | A test through `WebFactory` starts the web application with no passport key in its configuration and gets the front page and the sign-in page | Yes | No |
| 3 | After the deploy, inside the VM: the web container has no file `/run/secrets/Encryption__PassportKey` and no such variable (`docker inspect`), its secret folder holds no key file, and the check of the environment after the deploy passed | Yes | No |
| 4 | After the deploy, S01 opens a guest with a passport number in the back office and reads it as before, and the log of the next deploy shows no warning for the web's settings | Yes | No |
| 5 | The example file, Environment-Files, Proxmox-Deployment and ADR-0024 say what the deliverable says, `test-wiki-coverage.sh` passes, the code review against `QC-CS-001` and `QC-SH-001` is Go, `lint.sh` is clean, and every required check of the pull request is green | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-015 | The database API, which holds the persistence and the passport key |
| MIL-035 | The secrets as files, and the script that writes them |
| ADR-0004 | The web gets its use cases from the API, never from the database |
| ADR-0024 | Each secret a file that only its container reads |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001 objective 18 and success criterion 21: no database settings and no passport key in the web container | Criteria 1 to 4 |
| BC-001 objective 35 and success criterion 38: each secret a file that only its container reads, and 0 secrets in the environment of a container | Criteria 1 and 3 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-19. A small change of one script, one compose file and one comment; it does not move a date of the Business Case.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | The secret files per service | Written first: tests in `ops/tests/test-write-secret-files.sh` that the web gets no passport key, also when its settings file holds the line; that a line with the secret of another service never reaches a settings file and is named in a warning, while the settings that each service needs stay; that a secret file a service no longer lists is deleted; and that the compose file of the web names no passport key. Then `write-secret-files.sh`: the web's list and loop without the key, the secrets of every service as the lines left out of every settings file, the warning and the deletion | No | criterion 1 |
| 2 | The web without the key | Written first: a test through `WebFactory` that the web starts and answers the front page and the sign-in page with no passport key in its configuration. Then the web's compose file and example file, and the comment in `Program.cs` | No | criterion 2 |
| 3 | The documents, code review and pull request | Environment-Files, Proxmox-Deployment and the Version History row of ADR-0024; the review record against `QC-CS-001` and `QC-SH-001`; the pull request, which closes the issues of tasks 1 to 3 | No | criteria 1, 2 and 5 |
| 4 | The deploy and the checks | S01 approves the CD run of the merge. Over Tailscale Secure Shell (SSH) and without printing a secret, the AI assistant reads the secrets and the environment of the web container and the web's secret folder; S01 or S03 removes the line from `nfhotel-web.env` on the VM; S01 opens a guest with a passport number in the back office | No | criteria 3 and 4 |
| 5 | Gate and review record | The gate review record against criteria 1 to 5, the row of MIL-075 in the traceability matrix, the Accepted row of this document, and the closing of the GitHub milestone | No | criteria 1 to 5 |

---

[BC-001]: ../business-case.md
[ADR-0004]: ../adr/adr-0004-database-api.md
[ADR-0024]: ../adr/adr-0024-secrets-as-files.md
[MIL-015]: ./mil-015-database-api.md
[MIL-035]: ./mil-035-secrets-as-files.md
