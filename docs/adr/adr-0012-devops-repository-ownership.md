# ADR-0012 NF-Hotel/DevOps owns infrastructure and operations; NF-Hotel/NFHotel owns the application

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0012 |
| CrossReference | [BC-001], [ADR-0003], [ADR-0006], [ADR-0007], [ADR-0010], [MIL-025], [ADR-0016] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-19 | Accepted | mikemike | AI as stand-in for S01 | Accepted after RC-102 Go | [4c8f103] |
| 2026-10-20 | Superseded by ADR-0016 | mikemike | AI as stand-in for S01 | S01 decided to keep the application and its operations in NF-Hotel/NFHotel (ADR-0016, RC-146) | pending |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

All infrastructure and DevSecOps work of NF Hotel has been done in the application repository `NF-Hotel/NFHotel`:

- **On `main`:** the compose files under `deploy/`, the deploy scripts under `scripts/`, the Proxmox test project `tests/NFHotel.Proxmox.Tests`, the settings examples `deploy.env.example` and `pve.env.example`, and the wiki page Proxmox Deployment.
- **Unmerged:** the CI/CD and database work of MIL-021 (PR #117) and MIL-024 (PR #150), with DSA-001 and ADR-0007 to ADR-0011.

The organization repository `NF-Hotel/DevOps` exists, is public, and is empty (no commit, no branch).

S01 decided on 2026-10-19 that `NF-Hotel/DevOps` is the central and authoritative repository for all DevOps and DevSecOps work, past, present and future. This ADR records how that is done without losing history, weakening a protection or touching production.

Facts that shape the design (GitHub documentation, *Reusing workflow configurations*, *Creating a composite action*, *Contexts reference*):

- A reusable workflow called from another repository runs in the context of the caller. `actions/checkout` fetches the caller repository, the `github` context and the token are the caller's, and permissions can only be kept or reduced.
- A composite action runs as steps of the caller's job and reads its own files through `github.action_path`. `github.action_ref` gives the ref it was called with, readable only through `env`.
- A public repository's actions and workflows can be used from any repository. A caller pins them with `@<ref>`; a full commit SHA is the safest option.
- GitHub keeps the environment, its required reviewers and its secrets with the repository that runs the job. Packages pushed with a repository's `GITHUB_TOKEN` are linked to that repository.

Options considered:

1. **Everything stays in NFHotel.** Rejected by the decision of S01.
2. **Everything, workflows included, moves to DevOps,** which builds by checking out NFHotel. The release job would run in DevOps:
   - it would need a token that can read NFHotel events and write NFHotel packages;
   - the production environment and its approvals would sit away from the code they release;
   - a change in NFHotel would not start the pipeline without a cross-repository trigger and a credential.
3. **Reusable workflows in DevOps, called by thin workflows in NFHotel.** The job runs in NFHotel, but every called workflow must check out DevOps a second time to reach its scripts, at a ref that cannot be tied to the `uses:` ref without repeating it.
4. **Composite actions in DevOps, called by thin workflows in NFHotel.**
   - The job, its triggers, permissions, environment, concurrency and OIDC claims stay in NFHotel.
   - The logic and scripts live in DevOps and are fetched at exactly the pinned SHA.

## Decision

We follow option 4: `NF-Hotel/DevOps` owns infrastructure and operations, `NF-Hotel/NFHotel` owns the application and keeps only thin workflow entry points that call DevOps composite actions pinned by full commit SHA.

1. **Ownership boundary.**

   | Area | Owner | Notes |
   | --- | --- | --- |
   | Application code (`src/`), tests (`tests/` except the Proxmox tests), EF Core migrations, `appsettings*.json` | NFHotel | |
   | `Dockerfile`, `Dockerfile.api` | NFHotel | Shared: how the application is packaged. DevOps reviews changes through CODEOWNERS. |
   | Application CI (`ci.yaml`: build, test, format, NuGet audit, image build check) | NFHotel | The required checks of NFHotel |
   | Entry workflows `release.yaml`, `cd.yaml` | NFHotel | Shared: triggers, permissions, environment; their steps are DevOps actions |
   | Composite actions, scripts, compose files, nginx, systemd, PostgreSQL roles, backup, Tailscale and nftables configuration, Proxmox scripts and tests | DevOps | |
   | GHCR images `ghcr.io/nf-hotel/*` | Pushed by NFHotel's `GITHUB_TOKEN`; release logic owned by DevOps | Linked to NFHotel, labelled with the NFHotel commit |
   | Environment `production`, Tailscale trust credential | NFHotel settings, owned by S03 | The job that deploys runs in NFHotel |
   | Business Case, Stakeholder Analysis, Project Plan, application ADRs and milestones | NFHotel | The project baseline |
   | Infrastructure ADRs, milestones, DSA, runbooks, compliance evidence and review records | DevOps | |

2. **Versioning across the repositories.**
   - NFHotel calls `NF-Hotel/DevOps/.github/actions/<action>@<40-character SHA>`, never a branch or a tag.
   - The release action records its own ref (`github.action_ref`) in the release manifest and refuses to run when that ref is not a full SHA.
   - The VM checks out DevOps at that SHA for the compose files, and runs the images by the digests in the same manifest.
   - A release is therefore identified by the NFHotel commit, the DevOps commit and four digests.
   - Dependabot proposes updates of the pinned SHA as reviewed pull requests (MIL-022).
3. **Least privilege across the repositories.**
   - DevOps is public, so no token is needed to use its actions, and NFHotel holds no DevOps credential.
   - DevOps's own CI reads NFHotel anonymously (public), and never receives NFHotel secrets or a package token.
   - The release job keeps `packages: write` and the CD job keeps `id-token: write`, both in NFHotel.
4. **Migration without losing history.**
   - Files are copied into DevOps at a recorded NFHotel commit, and `MIGRATION.md` in DevOps lists each file with its source path, commit and pull request.
   - No history is rewritten in either repository, and no commit is fabricated. NFHotel's history stays the record of how the work was made.
   - Unmerged work moves with its documents (DSA-001, ADR-0007 to ADR-0011, MIL-021 to MIL-024, and their review records), keeping its IDs. PRs #117 and #150 are superseded by one migration PR in DevOps and one boundary PR in NFHotel, and are closed with a link once those are merged.
   - Completed milestones and accepted ADRs on NFHotel `main` (MIL-001 to MIL-004, MIL-017, ADR-0003, ADR-0006) stay where they were accepted. DevOps refers to them; ADR-0010 already amends ADR-0003.
   - The infrastructure files on NFHotel `main` stay until their DevOps copies have been deployed and verified (MIL-025 task 7). They are then removed by a reviewed pull request.
   - The issues of MIL-021 to MIL-024 are transferred to DevOps with GitHub's issue transfer, which keeps their history and redirects their links.
5. **Artifact IDs.**
   - IDs that exist keep their numbers.
   - NFHotel continues its sequences (next MIL-026, ADR-0013, RC-104).
   - DevOps allocates new IDs from 500 (MIL-500, ADR-0500, RC-500), so the two registries never collide without coordination.
6. **Exceptions.** DevOps work in NFHotel needs an ADR in DevOps stating why, reviewed like any ADR. The policy is in `CONTRIBUTING.md` of both repositories.
7. **Bootstrap.** The empty DevOps repository has no `main` to open a pull request against. A single first commit on `main` holds only a README that points to the migration pull request. That is the one exception to "never commit on main", recorded here; everything after it goes through pull requests.

## Consequences

**Positive:**

- One authoritative place for infrastructure, its decisions and its evidence; the application repository holds no deploy logic.
- The protections stay where GitHub enforces them: the required checks, the `production` approvals and the Tailscale trust are unchanged.
- Each release is traceable to two commits and four digests.
- Nothing is rewritten or fabricated, and production is not touched by the migration.

**Negative:**

- A change that spans both repositories needs two pull requests: a DevOps change, then the SHA bump in NFHotel.
- During the migration the infrastructure files exist in both repositories, and the NFHotel copies must not be edited.
- DevOps needs its own settings: secret scanning, CODEOWNERS, rulesets and CI.
- Composite actions cannot set `permissions`, `environment` or `concurrency`. Those stay in the NFHotel entry workflows, which must be reviewed together with each action change.

**Quality:** maintainability and security (clear ownership, least privilege, traceable releases) improve (ISO/IEC 25010:2023).

## Affected Artifacts

- [ADR-0007]: CI, release and CD keep their design, split between the repositories as above.
- [ADR-0010]: the VM's checkout becomes `NF-Hotel/DevOps`.
- [ADR-0003], [ADR-0006]: stay in NFHotel as accepted history; DevOps refers to them.
- [MIL-025]: implements the migration.

---

[BC-001]: ../business-case.md
[ADR-0003]: ./adr-0003-proxmox-vm-for-services.md
[ADR-0006]: ./adr-0006-tls.md
[ADR-0007]: https://github.com/NF-Hotel/DevOps/blob/main/docs/adr/adr-0007-cicd-and-supply-chain.md
[ADR-0010]: https://github.com/NF-Hotel/DevOps/blob/main/docs/adr/adr-0010-production-vm-as-deployed.md
[MIL-025]: ../milestones/mil-025-devops-repository-consolidation.md
[4c8f103]: https://github.com/NF-Hotel/NFHotel/commit/4c8f1030fdf53a5d9d827478244c55ecae73b8eb
[ADR-0016]: ./adr-0016-one-repository-for-application-and-operations.md
