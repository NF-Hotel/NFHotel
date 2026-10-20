# MIL-025 DevOps repository consolidation

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-025 |
| CrossReference | [BC-001], [ADR-0012], [MIL-021], [MIL-024] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-19 | Accepted | mikemike | AI as stand-in for S01 | Accepted after RC-103 Go | [4c8f103] |
| 2026-10-20 | Deprecated | mikemike | AI as stand-in for S01 | Its goal is reversed by ADR-0016; the open tasks are replaced by MIL-031 | [1b46421] |

---

## Purpose

Decide whether `NF-Hotel/DevOps` can serve as the single authoritative repository for NF Hotel infrastructure and operations (ADR-0012). The application repository keeps only the application and the thin workflow entry points. The milestone moves the existing DevOps work across with its history references, and proves that the cross-repository pipeline builds the right commit with the right DevOps revision. It changes nothing in production.

## Deliverable

- ADR-0012, reviewed.
- **DevOps repository:**
  - a bootstrap commit, then a migration pull request with the infrastructure files, documents and review records of MIL-021 and MIL-024;
  - the infrastructure files of NFHotel `main`;
  - `MIGRATION.md` (source path, commit and pull request of every file);
  - its own artifact registry and traceability matrix (IDs from 500), CI (linters, script tests, a cross-repository build of the NFHotel images without push), `CODEOWNERS`, `SECURITY.md` and `CONTRIBUTING.md`.
- **Composite actions in DevOps:** `release-images` (builds and pushes the four images, writes the manifest with the NFHotel commit, the DevOps commit and the digests) and `deploy-release` (checks the release run and the manifest, joins the tailnet, runs the deploy command).
- **NFHotel boundary pull request:**
  - `release.yaml` and `cd.yaml` as thin entry points calling the actions by SHA;
  - the MIL-021 to MIL-024 work removed from the branch;
  - `CONTRIBUTING.md` with the ownership policy;
  - the README and wiki pointing to DevOps;
  - the Business Case and Project Plan updated.
- **Repository settings of DevOps:** secret scanning with push protection, Dependabot alerts, a ruleset for `main` (pull request with one approval, no force push, required checks).
- **Issue transfer:** the issues of MIL-021 to MIL-024 move to DevOps once the migration pull request is merged.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | Every DevOps asset of NFHotel (the inventory of DevOps `MIGRATION.md`) has a DevOps copy, or a recorded reason to stay; no file was lost (checksums of the copies match the source commit, apart from the recorded changes) | Yes | No |
| 2 | The DevOps CI on github.com builds the NFHotel images at a pinned NFHotel commit through the `release-images` action, without pushing, and the manifest names that commit and the DevOps commit | Yes | No |
| 3 | `github.action_path` gives the composite action the files of DevOps at the pinned SHA (shown in a CI run), and the action refuses a ref that is not a full SHA | Yes | No |
| 4 | NFHotel's `release.yaml` and `cd.yaml` contain no deploy logic: each step is a DevOps action pinned by full SHA or a pinned GitHub action; `permissions`, `environment` and `concurrency` are unchanged from MIL-021 | Yes | No |
| 5 | No secret, key or production connection string is in either repository; secret scanning with push protection is on in DevOps | Yes | No |
| 6 | The DevOps `main` ruleset requires a pull request with one approval and the CI checks; the migration PR is merged only after an independent review | Yes | No |
| 7 | After the first deploy from DevOps on `nf-hotel` (MIL-021 tasks 8 and 10), the infrastructure files are removed from NFHotel `main` by a reviewed pull request | Yes | No |
| 8 | Reviews against `qc-adr`, `qc-milestones-gateways`, `qc-programming-shell`, `qc-yaml` and `qc-workflow` have the verdict `Go` | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| PR #22 | The CI workflow that starts Release |
| MIL-021, MIL-024 | The work that moves |
| S01 | Approves the ruleset and the merge of the DevOps migration PR; Tirsvad or another member reviews it |
| S03 | The `production` environment and the Tailscale trust credential, unchanged in their owner but now used by actions from DevOps |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| Objective 23 of BC-001: an approved, traceable pipeline | Criteria 2 to 4 |
| ADR-0012 | All tasks |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01; S03 for the workflows and the deploy |

## Target Date

2026-11-01, two weeks. Task 7 waits for the first deploy from DevOps (MIL-021).

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Record the decision in ADR-0012 | Ownership boundary, composite actions, versioning, least privilege, migration, IDs, bootstrap; reviewed. | No | |
| 2 | Bootstrap and settings of DevOps | The first commit, secret scanning, Dependabot alerts, the ruleset. | No | |
| 3 | Migrate the assets | Copy with `MIGRATION.md`; the DevOps registry and traceability matrix; IDs kept. | No | |
| 4 | Composite actions and DevOps CI | `release-images`, `deploy-release`; linters, script tests and the cross-repository build in CI. | No | |
| 5 | NFHotel boundary | Thin entry workflows, the MIL-021 to MIL-024 work removed from the branch, `CONTRIBUTING.md`, README, wiki, BC and PP. | No | |
| 6 | Disposition of PR #117 and PR #150 | Mark them superseded with links; close them when the replacements are merged; transfer the issues to DevOps. | No | |
| 7 | Remove the old copies from NFHotel | After the first deploy from DevOps is verified, remove `deploy/`, the infrastructure scripts and the Proxmox tests from NFHotel. | No | |
| 8 | Check and review | Linters; reviews; the sync dry run in both repositories. | No | |

---

[BC-001]: ../business-case.md
[ADR-0012]: ../adr/adr-0012-devops-repository-ownership.md
[MIL-021]: https://github.com/NF-Hotel/DevOps/blob/main/docs/milestones/mil-021-secure-cicd-foundation.md
[MIL-024]: https://github.com/NF-Hotel/DevOps/blob/main/docs/milestones/mil-024-postgresql-api-readiness.md
[4c8f103]: https://github.com/NF-Hotel/NFHotel/commit/4c8f1030fdf53a5d9d827478244c55ecae73b8eb
[1b46421]: https://github.com/NF-Hotel/NFHotel/commit/1b46421e16ad0138fc39ab6aa7bf0fe3a1799f38
