# MIL-031 Consolidate NF-Hotel/DevOps into NF-Hotel/NFHotel

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-031 |
| CrossReference | [BC-001], [ADR-0016], [ADR-0012], [MIL-025] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Deprecated | mikemike | S01 | Initial version | [1b46421] |
| 2026-10-20 | Accepted | mikemike | AI as stand-in for S01 | Accepted after RC-147 Go (plan accepted by S01 in chat) | [1b46421] |

---

## Purpose

Decide whether NF Hotel can run its application and its operations from one repository (ADR-0016), with:

- no copy of the deploy files outside NFHotel;
- no pin between repositories;
- production deployed from NFHotel alone.

## Deliverable

- **The operations in NFHotel.** `ops/` holds the tree of NF-Hotel/DevOps at `b63fbed`, brought in by a subtree merge with its history. The composite actions are in `.github/actions/`.
- **The pipeline on one repository.** The release, CD, release check and E2E/DAST workflows call the local actions. An `ops` CI workflow runs the DevOps linters and script tests, and it is a required check. The VM deploys an NFHotel checkout.
- **The documents in `docs/`,** with their IDs:
  - DSA-001, ADR-0007 to ADR-0011, ADR-0500;
  - MIL-021 to MIL-025;
  - the review records;
  - the wiki pages CI-CD and Database Preparation.
- **Local work with one clone.** `dev-up.sh` and `dev-up.ps1` use `ops/` of the same checkout.
- **DevOps closed down.** Its open pull requests are closed with links. Its MIL-023 and framework work is moved as NFHotel pull requests. The repository is archived with a pointer README.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | `git log -- ops/` in NFHotel shows the commits of NF-Hotel/DevOps up to `b63fbed` | Yes | No |
| 2 | No workflow in NFHotel uses `NF-Hotel/DevOps`, and no file outside `ops/` is a copy of a deploy file (search) | Yes | No |
| 3 | The `ops` CI checks pass: lint and every script test. The release check, E2E and DAST pass with the local actions. Both CIs are required checks | Yes | No |
| 4 | A release from NFHotel `main` is deployed to `nf-hotel` by CD after an approval. `/opt/nfhotel` is an NFHotel checkout at the release commit, and the smoke test passes | Yes | No |
| 5 | The market analysis page answers with an analysis in production, the case that the drift broke | Yes | No |
| 6 | `dev-up.sh --check` passes with one NFHotel clone | Yes | No |
| 7 | NF-Hotel/DevOps is archived. Each of its open PRs is closed with a link to the NFHotel PR that carries it | Yes | No |
| 8 | Reviews against `qc-adr`, `qc-programming-shell`, `qc-workflow` and `qc-yaml` have the verdict `Go` | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| ADR-0016 | The decision this milestone carries out |
| NFHotel#195 (DevOps `b63fbed`) | The deploy that is ready may go first (ADR-0016 decision 7); the consolidation starts from the same commit |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| Success criterion 26 (approved pipeline): one pipeline, no drift between repositories | Tasks 2 to 5 |
| Success criterion 27 (verified supply chain): kept, with local actions instead of pinned remote ones | Task 3 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-03, two weeks. The VM switch (task 6) is done by S03 in a planned window.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Review ADR-0016 | Review the decision against `qc-adr`. On Go, ADR-0012 is marked Superseded by ADR-0016 and MIL-025 gets a row that its goal is reversed. | No | ADR-0016 |
| 2 | Bring DevOps in under ops/ | Subtree merge of NF-Hotel/DevOps `b63fbed` under `ops/`, with its history. The composite actions move to `.github/actions/`, and the scripts are checked to run from `ops/`. The old `deploy/` and infrastructure scripts of NFHotel are removed, since the DevOps copies carry their changes (DevOps#9). | No | ADR-0016 decision 1, 2 |
| 3 | Pipeline on local actions | The workflows call `./.github/actions/*`. The manifest's `devops_sha` equals `sha`. An `ops` CI workflow is filtered by path and holds the DevOps CI jobs. CODEOWNERS covers `ops/`. Dependabot covers the `ops/` images. Script tests are updated. | No | ADR-0016 decision 4 to 6 |
| 4 | Documents into docs/ | The infrastructure ADRs, milestones, DSA, review records and wiki pages come back with their IDs. The registry continues both sequences. README, CONTRIBUTING and SECURITY are updated. | No | ADR-0016 decision 3 |
| 5 | Local deploy with one clone | `dev-up.sh` and `dev-up.ps1` use `ops/` (replaces NFHotel#198), and the wiki page Local Development Deploy is updated. Tested with `--check` here, and as a full run by its author. | No | |
| 6 | Switch the VM to NFHotel | S03 switches `/opt/nfhotel` to an NFHotel checkout at the deployed release and installs `nfhotel-deploy` again, with a backup and the DevOps checkout kept as the way back. Then one approved CD from NFHotel `main`. | No | ADR-0016 decision 7 |
| 7 | Carry over and archive DevOps | Close DevOps #1, #3, #4, #8, #9 and #13 with links. Move #5, #10 and #12 as NFHotel PRs. Archive the repository with a pointer README. | No | ADR-0016 decision 8 |
| 8 | Check and review | Prove criteria 1 to 7; reviews against the four checklists. | No | |

---

[BC-001]: ../business-case.md
[ADR-0016]: ../adr/adr-0016-one-repository-for-application-and-operations.md
[ADR-0012]: ../adr/adr-0012-devops-repository-ownership.md
[MIL-025]: ./mil-025-devops-repository-consolidation.md
[1b46421]: https://github.com/NF-Hotel/NFHotel/commit/1b46421e16ad0138fc39ab6aa7bf0fe3a1799f38
