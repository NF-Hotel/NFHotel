# RC-208 Gate review of MIL-035

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-208 |
| CrossReference | [MIL-035], [RC-198], [RC-205], [ADR-0024], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-24 | Accepted | mikemike | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- **Instance reviewed:** the five Go/No-Go criteria of [MIL-035], after its pull request #308 was merged (`df162f1`) and deployed by CD (run 40132921572).
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).

## Criteria

| # | Criterion | Result | Evidence |
| --- | --- | --- | --- |
| 1 | An ADR records the choice and the alternatives | Go | ADR-0024: environment variables, Compose secrets, a vault and Swarm secrets, with the reasons; and why the files live next to the settings, not in `/etc/nfhotel/secrets` as the plan first said |
| 2 | In production, `docker inspect` of the API, the web application and PostgreSQL shows no secret, checked by a script | Go | `check-secret-env.sh` on `nf-hotel` after the CD: PASS for all three; the deploy runs it too and would have rolled back |
| 3 | Each container reads its secrets from `/run/secrets`, and the site, the sign-in and the API work | Go | `/run/secrets` of the API holds its six files; the site and `/book` answer 200; the API logged no error after its restart and the benchmark service no 401. The staff sign-in was not tried in production, because the AI holds no staff password there; the rotation drill signs in on the same images with the secrets as files |
| 4 | The rotation of each secret is in the wiki and was done once on the throwaway stack in CI | Partly | The wiki page Proxmox-Deployment, "Secrets and their rotation". The CI job rotated the database password, the API token, the signing key and the first Manager's password (10 passed). **The passport key cannot be rotated:** it encrypts stored data and the application has one key (ADR-0024 decision 7) |
| 5 | CI green and the code review Go | Go | #308: 33 checks green; RC-205 Go |

## Overall Verdict

Go-with-conditions. Criteria 1, 2, 3 and 5 are met. Criterion 4 is met for every secret except the passport key, whose rotation needs a key ring and a re-encryption command, which is outside the accepted plan. MIL-035 stays open (#288) until S01 decides whether that becomes a milestone of its own or is accepted as a limit.

## Action Items

| # | Action | Owner | Due |
| --- | --- | --- | --- |
| 1 | Decide on the rotation of the passport key: a new milestone (key ring and re-encryption), or accept the limit in ADR-0024 | S01 | To decide |

---

[MIL-035]: ../../milestones/mil-035-secrets-as-files.md
[RC-198]: rc-198-mil-035.md
[RC-205]: rc-205-mil-035-code.md
[ADR-0024]: ../../adr/adr-0024-secrets-as-files.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
