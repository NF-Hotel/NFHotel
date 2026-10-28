# RC-161 Review of eleven ADRs against the code and production

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-161 |
| CrossReference | [ADR-0003], [ADR-0005], [ADR-0006], [ADR-0007], [ADR-0008], [ADR-0009], [ADR-0010], [ADR-0011], [ADR-0014], [ADR-0016], [ADR-0500], [QC-ADR-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-21 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [12a64c3] |

---

## Artifact Under Review

- **Instances reviewed:** all 19 ADRs in `docs/adr/` (ADR-0001 to ADR-0018 and ADR-0500). Each was checked against the code on `main`, the CI and CD runs, and the production VM `nf-hotel`. Eleven needed an update: [ADR-0003], [ADR-0005], [ADR-0006], [ADR-0007], [ADR-0008], [ADR-0009], [ADR-0010], [ADR-0011], [ADR-0014], [ADR-0016] and [ADR-0500].
- **Checklist used:** [QC-ADR-001], and for each claim of fact, the evidence named below.
- **Language and domain:** en / it.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`). S01 asked in chat on 2026-10-21 to update the ADRs that are wrong or out of date, after checking them against the code, production, the server and the product.
- **Evidence:**
  - a search of every file path named in an ADR against the repository;
  - the CD log of run 39561453009 (8a077b0);
  - `curl` against `https://100.72.194.127/` (HSTS header, 301 on port 80);
  - a read-only command that S03 ran on `nf-hotel` on 2026-10-21. It covered the certificate, the systemd timers, port 5432, the stopped containers and their mounts, the Docker volumes, the count of one settings line, the backup settings and files, and the memory, CPU and disk.

## How the ADRs were changed

The decisions stand; none is reversed, so no new ADR is needed. Each ADR gets four things:

- its paths corrected where ADR-0016 moved the files to `ops/`;
- statements of fact corrected where the code differs;
- a paragraph **Current state (2026-10-21, RC-161)** before its Consequences, with the evidence and what is still open;
- a new Version History row with a Change summary.

The Context of each ADR is left as it was. It records the facts at the time of the decision, such as the 12 GiB VM of ADR-0004, ADR-0005 and ADR-0015. Following the Version History rule of the framework:

- the oldest of two rows and its unused commit link are removed;
- the row before becomes `Deprecated`;
- the new row is `Accepted` after this review.

This also corrects ADR-0007 and ADR-0016, which each had two `Accepted` rows.

## Findings per ADR

| ADR | Was | Is (evidence) | Change |
| --- | --- | --- | --- |
| 0001, 0002, 0004, 0013, 0015, 0017, 0018 | — | Match the code | None |
| 0012 | Superseded by ADR-0016 | Correct, and kept as history | None |
| 0003 | `deploy/market-analysis`; VM size 4 vCPU, 8 GiB, 60 GiB | `ops/deploy/market-analysis`. `nf-hotel` has 4 vCPU, 20 GiB, 111 GB and Debian 13 (VM) | Path; current state |
| 0005 | Negatives on plain HTTP, no revocation, migrations at start | Resolved by ADR-0006, ADR-0017, and ADR-0008/0011 (`MigrateOnStart=false`, logged by CD); ADR-0015 opens two routes to the staff token | Current state; cross-references |
| 0006 | `deploy/nginx/tls`, `scripts/make-dev-certificate.sh`; certificate decision deferred | Paths under `ops/`. ADR-0009 decided it, and HSTS `max-age=2592000` is on in production (`curl`) | Paths; current state |
| 0007 | Builds two images; deploy user runs `scripts/deploy-release.sh`; digests in `deploy/release.env`; healthy within 3 minutes | Four images (`market-analysis` frozen, ADR-0500); the user runs `/usr/local/sbin/nfhotel-deploy`; digests passed in the environment to `deploy-services.sh --no-build`; `--wait-timeout 300` | Decisions 1, 5, 6, 7 matched to the code |
| 0008 | Dumps copied off the VM; monthly drill | 14 dumps on the VM, `BACKUP_TARGET` empty, so there is no copy off the VM (D7 decided, not in place, NFHotel#124). One drill (RC-505) | Current state, with what is not in place |
| 0009 | Renewal timer monthly; `deploy/nginx/tls/` | `nfhotel-cert-renew.timer` runs daily. It was installed by hand and is **not in the repository**. Let's Encrypt YE1 certificate, valid to 2027-01-04 | Paths; current state |
| 0010 | Decision 3 names NF-Hotel/DevOps | NF-Hotel/NFHotel since the switch of 2026-10-21. 5432 is not listening on the host. VMID 100 is in the wiki | Decision 3; current state; the VM size added to the wiki page Proxmox Deployment |
| 0011 | `scripts/db/`, `api.conf.template` | `ops/scripts/db/`, `ops/deploy/nginx/optional/api.conf` | Paths; current state |
| 0014 | `scripts/deploy-services.sh` | `ops/scripts/deploy-services.sh`, which writes the model for the frozen image too | Path; current state |
| 0016 | `ops/db/`; Ops CI only when `ops/**` changes | `ops/scripts/db/`. Ops CI runs on every pull request (RC-148), and its jobs are required since 2026-10-21. Decision 8 is open (NFHotel#205) | Decisions 1 and 5; current state |
| 0500 | `deploy/market-analysis/frozen-image`, `tests/check-release-build.sh` | Under `ops/`. The freeze is in force; S01's decision is open | Paths; current state |

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | ID matches the file | Pass | Unchanged |
| 2–5 | Context, options, decision, consequences | Pass | Unchanged; no decision is reversed |
| 7 | Affected artifacts linked and in CrossReference | Pass | Every ADR named in a new paragraph is in its CrossReference, with a link definition (checked by a script) |
| 8 | Changes as new Version History rows | Pass | One new row each, with Change and Commit; the two retained rows rule is followed |
| 9 | Successor and back link | Pass | ADR-0005, ADR-0006, ADR-0007 and ADR-0010 now name the later ADRs that changed them |
| 11 | Status values | Pass | `Accepted` and `Deprecated` only; ADR-0007 and ADR-0016 no longer have two `Accepted` rows |

## Findings outside the ADR text

These are facts on the VM that differ from the decisions. None is changed by this record.

1. **No copy of the backups off the VM** (ADR-0008, D7, NFHotel#124). A lost disk loses the database and its 14 backups together. This is the largest risk found.
2. **The certificate renewal is not in git** (ADR-0009, ADR-0010). `nfhotel-cert-renew.timer` and its service exist only on the VM. If the VM is rebuilt, the renewal is lost, and the certificate expires on 2027-01-04.
3. **A stopped stack from before the reconciliation** (ADR-0010, ADR-0011). It is `nfhotel-nginx`, `nfhotel-python-api` and `nfhotel-postgres`, with the volume `nfhotel-postgres_nfhotel_postgres_data`. ADR-0011 decision 1 allows one database instance. This one is stopped, but no record says that its data was carried over to `nfhotel-postgres_pgdata`.
4. **The first Manager's password is still in the API settings file.** The line `Staff__InitialManager__Password=` is present once. ADR-0017 says to remove it once the owner has changed the password.

## Overall Verdict

Go for the eleven updated ADRs.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Set up the copy off the VM (`BACKUP_TARGET`, D7), then run a restore from that copy | S03 | NFHotel#124 |
| Bring `nfhotel-cert-renew.service` and `.timer` into `ops/deploy/systemd/`, so that CD installs them (ADR-0018) | S01 | Before 2026-12-04, a month before the certificate expires |
| Compare the old volume `nfhotel-postgres_nfhotel_postgres_data` with production (tables and row counts, read-only). Then decide whether to remove the stopped stack, after a backup of that volume | S03 | Next maintenance window |
| Check that the first Manager has changed the password, then remove `Staff__InitialManager__Password` from `nfhotel-api.env` | S03 | At once |

---

[ADR-0003]: ../../adr/adr-0003-proxmox-vm-for-services.md
[ADR-0005]: ../../adr/adr-0005-backoffice-login.md
[ADR-0006]: ../../adr/adr-0006-tls.md
[ADR-0007]: ../../adr/adr-0007-cicd-and-supply-chain.md
[ADR-0008]: ../../adr/adr-0008-database-migration-and-backup.md
[ADR-0009]: ../../adr/adr-0009-tls-certificates.md
[ADR-0010]: ../../adr/adr-0010-production-vm-as-deployed.md
[ADR-0011]: ../../adr/adr-0011-postgresql-access-for-the-api.md
[ADR-0014]: ../../adr/adr-0014-default-ai-model.md
[ADR-0016]: ../../adr/adr-0016-one-repository-for-application-and-operations.md
[ADR-0500]: ../../adr/adr-0500-freeze-market-analysis-image.md
[QC-ADR-001]: ../../../framework/qc/artifacts/qc-adr.md
[12a64c3]: https://github.com/NF-Hotel/NFHotel/commit/12a64c3225c75ebc1b83a0e6d0ee08115ec1a493
