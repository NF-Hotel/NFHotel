# MIL-021 Secure CI/CD foundation

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-021 |
| CrossReference | [BC-001], [ADR-0003], [ADR-0006], [ADR-0007], [ADR-0008], [ADR-0010], [DSA-001], [US-001] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-18 | Deprecated | mikemike | AI as stand-in for S01 | Accepted after RC-089 Go | [f4649ad] |
| 2026-10-18 | Accepted | mikemike | AI as stand-in for S01 | Added task 10 and criterion 10, the reconciliation of the production VM (ADR-0010); accepted after RC-097 Go | [1b94761] |

---

## Purpose

Decide whether NF Hotel may deploy to the Proxmox VM through an approved, repeatable pipeline instead of by hand. Phase 1 of the CI/CD and DevSecOps assessment DSA-001 has four parts:

- CI becomes a required gate.
- `main` releases the web and API images to the GitHub Container Registry (GHCR).
- An approved CD workflow deploys them by digest over Tailscale, with health checks and rollback.
- The database gets a backup and a tested restore.

Supply-chain checks (phase 2, MIL-022) and hardening (phase 3, MIL-023) follow.

## Deliverable

- ADR-0007 and ADR-0008, reviewed.
- GitHub settings:
  - The ruleset on `main` gets required status checks (`Build, tests og sikkerhed`, `Formatering`, `Docker-build`), dismisses stale reviews, requires approval of the last push and blocks deletion.
  - Secret scanning with push protection and Dependabot alerts are turned on.
  - `SECURITY.md` and `CODEOWNERS` are added.
  - The Environment `production` gets required reviewers and is limited to `main`.
- `.github/workflows/release.yaml`: builds `nfhotel-web` and `nfhotel-api` on `main` after green CI, pushes them to GHCR, and records the digests in the job summary.
- `.github/workflows/cd.yaml`: Environment `production`, Tailscale with OpenID Connect (OIDC) federation (`tag:ci`), Tailscale SSH as `deploy`, deploy by digest, health checks, smoke test, rollback, concurrency, release record.
- `scripts/deploy-release.sh` on the VM, and compose files that read the image reference from `deploy/release.env` (keeping `build:` for local development).
- Tailscale policy: tags `tag:ci` and `tag:nfhotel-vm`, a grant from `tag:ci` to `tag:nfhotel-vm:22` only, and an `ssh` rule for the user `deploy`. These are configured by S03 and recorded in the wiki.
- Backups:
  - A `pg_dump` timer, encrypted with `age` and copied off the VM.
  - `scripts/restore-drill.sh`, plus one recorded restore drill.
- Wiki page "CI/CD": how a release flows, how to approve, how to roll back, how to restore.
- The running deployment of `nf-hotel` (`/opt/nfhotel/deployment/compose.yaml`) reconciled into the repository, with PostgreSQL no longer published to the tailnet (ADR-0010).

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | A pull request with a failing required check cannot be merged (ruleset API and a test PR) | Yes | No |
| 2 | A green merge to `main` produces web and api images in GHCR, identified by digest in the Release job summary | Yes | No |
| 3 | `NF Hotel CD` waits for approval in `production`, and the job receives no credential before approval | Yes | No |
| 4 | After the deploy, `docker inspect` on the VM shows the digests from the Release run, and no image was built on the VM | Yes | No |
| 5 | A deploy that changes only the web image does not recreate `postgres`, `nginx` or `ollama` (container IDs unchanged) | Yes | No |
| 6 | The smoke test passes (80 answers 301, `https://` answers 200); a forced failure rolls back to the previous digests, and the run is red | Yes | No |
| 7 | The CI node reaches only port 22 of `tag:nfhotel-vm` (a test from the runner to another tailnet node fails); no ssh key and no Tailscale secret is stored in GitHub | Yes | No |
| 8 | A dump from the timer is restored into a throwaway container, and the row counts match; the drill is recorded in an RC | Yes | No |
| 9 | `actionlint`, `shellcheck`, `shfmt -d` and `bash -n` are clean; reviews against `qc-yaml`, `qc-programming-shell` and `qc-adr` have the verdict `Go` | Yes | No |
| 10 | On `nf-hotel`, `/opt/nfhotel` is a git checkout of `NF-Hotel/NFHotel` whose compose files describe every running container, and port 5432 does not answer from the tailnet | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| PR #22 | The CI workflow whose result starts Release |
| DSA-001 decisions D1 to D8 | The choices this phase implements |
| S03 | The tailnet policy, Tailscale on the VM, the `deploy` user, the backup target and the host key confirmation |
| ADR-0003, ADR-0004, ADR-0006 | The VM, the networks and TLS that stay unchanged |
| ADR-0010 | The production VM `nf-hotel` as deployed; CD deploys only after the reconciliation |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| Objective 23 of BC-001: a release reaches production only through an approved pipeline, and the data can be restored | All tasks |
| Success criterion 26 of BC-001 | Criteria 1 to 8 |
| DSA-001 risks R1, R2, R5, R7, R8, R9, R13 | Tasks 2 to 8 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01; S03 for the tailnet, the VM, the backup and the deploy |

## Target Date

2026-11-15. This is four weeks from the plan, provided PR #22 is merged first. The phase has two workflows, two scripts, a tailnet policy, a backup timer and a proof in the VM.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Amend Business Case and Project Plan | Add the objective, scope, success criteria and the MIL-021 to MIL-023 rows; review again. | No | |
| 2 | Review ADR-0007 and ADR-0008 | The CI/CD and the database decisions, reviewed against `qc-adr`. | No | |
| 3 | Repository settings | Ruleset with required checks, secret scanning and push protection, Dependabot alerts, `SECURITY.md`, `CODEOWNERS`, Environment `production`. | No | |
| 4 | Release workflow | Build and push web and api to GHCR on green `main`; digests in the job summary; least privilege. | No | |
| 5 | Tailscale and the deploy user | Tags, grant, `ssh` rule, workload identity federation; the `deploy` user with its single command; the host key confirmed. | No | |
| 6 | CD workflow and deploy script | `cd.yaml` and `deploy-release.sh`: approval, Tailscale, pull by digest, health, smoke test, rollback, release log. | No | |
| 7 | Backup and restore | The `pg_dump` timer with `age`, the off-VM copy, `restore-drill.sh`, the first drill recorded. | No | |
| 8 | Prove it | One approved deploy, one forced rollback, one test PR blocked by a red check; record the run links. | No | |
| 9 | Check and review | Linters; reviews against `qc-yaml`, `qc-programming-shell`, `qc-adr`; the wiki page. | No | |
| 10 | Reconcile the production deployment | Bring `/opt/nfhotel/deployment/compose.yaml` of `nf-hotel` into the repository (secrets as references to git-ignored files), map its services and volumes to the release, back up the database first, stop publishing PostgreSQL to the tailnet; done by S03 with S01 (ADR-0010). | No | |

---

[BC-001]: ../business-case.md
[ADR-0003]: ../adr/adr-0003-proxmox-vm-for-services.md
[ADR-0006]: ../adr/adr-0006-tls.md
[ADR-0007]: ../adr/adr-0007-cicd-and-supply-chain.md
[ADR-0008]: ../adr/adr-0008-database-migration-and-backup.md
[ADR-0010]: ../adr/adr-0010-production-vm-as-deployed.md
[DSA-001]: ../sqa/devsecops-assessment.md
[US-001]: ../user-stories.md
[f4649ad]: https://github.com/NF-Hotel/NFHotel/commit/f4649adfd68aa312e04533632f0f66ee32df245f
[1b94761]: https://github.com/NF-Hotel/NFHotel/commit/1b94761ddb5113cd0d34915c01fe7f0f278bba60
