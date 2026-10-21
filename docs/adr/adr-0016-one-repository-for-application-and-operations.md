# ADR-0016 One repository for the application and its operations

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0016 |
| CrossReference | [BC-001], [SA-001], [ADR-0007], [ADR-0012], [MIL-025], [MIL-031], [ADR-0018], [RC-161] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Deprecated | mikemike | AI as stand-in for S01 | States that S01 decided and the AI wrote the reasons, which S01 accepted | [fdfc65c] |
| 2026-10-21 | Accepted | mikemike | AI as stand-in for S01 | Matched to the repository (RC-161): ops/scripts/db, Ops CI on every pull request; state of decisions 7 and 8 | pending |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

**Who wrote what.** The decision is S01's, given in chat on 2026-10-20 ("let us keep it all in NF Hotel and write why in an ADR", translated from Danish). The reasons below were written by the AI assistant from the events of that day and from the analysis it gave S01 before the decision. S01 read them and accepted this ADR. They are not a quote of S01.

ADR-0012 (2026-10-19) made `NF-Hotel/DevOps` the authoritative repository for infrastructure and operations. NFHotel kept the application and thin workflows that call DevOps composite actions pinned by commit SHA. One day of running that split showed its cost for a team of this size:

- **Two copies drifted, and production broke.** Developers kept changing the deploy files in NFHotel (MIL-020, MIL-026, ADR-0014, the nginx, Ollama and timeout fixes). Production runs the DevOps copies, so it lacked those changes. The market analysis of MIL-026 could not reach its service in production, because its network existed only in the NFHotel copy (DevOps#11).
- **One change, two repositories.** A feature that changes the application and its compose file, such as MIL-026, needs two pull requests, two reviews and a pin bump before it can be deployed. On 2026-10-20 this produced:
  - a stack of five DevOps pull requests (#1 → #3 → #4 → #8 → #9) and a combined one (#13);
  - review-record and milestone IDs that collided across the repositories (RC-131 on two branches);
  - three pin moves in NFHotel in one day (`24d979e`, `933c2e6`, `b63fbed`).
- **Two clones to run anything locally.** The local deploy of MIL-020 (`dev-up.sh`) needs a DevOps checkout beside NFHotel (NFHotel#198).
- **Nothing outside NF Hotel uses DevOps.** Its actions build NFHotel from an NFHotel checkout, its compose files describe NFHotel's services, its database roles serve NFHotel's EF Core model, and NFHotel's E2E and DAST run in DevOps's stack. The repository is the operations of one application, not a platform for several.

The team is two developers (S01 in SA-001), with S01 also holding S03. The separation of duties that a second repository would give is already enforced inside one repository by GitHub: the ruleset on `main` (required checks, a review by someone other than the author, last push approved), CODEOWNERS, and the environment `production`, whose approver cannot be the person who started the deploy.

Options considered:

1. **Keep two repositories (ADR-0012)**, and fix the drift with discipline and a check that the deploy files exist only in DevOps. The pin bumps, the second CI, the cross-repository test checkouts and the two clones stay.
2. **One repository, NFHotel, with the operations under `ops/`.** DevOps's tree is brought in with its history, the workflows call local actions, and DevOps is archived.
3. **One repository, DevOps,** with the application moved into it. Rejected: the application, its issues, milestones, reviews and history are in NFHotel, and the project baseline (BC-001, SA-001, PP-001) is there.

## Decision

We follow option 2: NF-Hotel/NFHotel holds the application and its operations; the operations live under `ops/`, and NF-Hotel/DevOps is archived. This supersedes ADR-0012.

1. **Layout.** `ops/` holds the tree of NF-Hotel/DevOps as it is: `ops/deploy/`, `ops/scripts/`, `ops/tests/`, `ops/security/`, with the database scripts in `ops/scripts/db/`. The composite actions go to `.github/actions/`, beside the workflows that use them. The scripts find their root from their own location, so they run unchanged from `ops/`.
2. **History.** DevOps is brought in with a subtree merge of its combined head (`b63fbed`, NF-Hotel/DevOps#13) under `ops/`, so its commits stay in NFHotel's history and `git log -- ops/` shows them. No history is rewritten. DevOps's `MIGRATION.md` stays as the record of the first move.
3. **Documents.** The infrastructure documents return to `docs/` with their IDs: DSA-001, ADR-0007 to ADR-0011, ADR-0500, MIL-021 to MIL-025, and their review records. The IDs from 500 that DevOps allocated stay as they are, and NFHotel continues its own sequences. The wiki pages of DevOps (CI-CD, Database Preparation) join the NFHotel wiki.
4. **Pipeline.**
   - The workflows call `./.github/actions/<action>` at the commit under test, so there is no pin and no bump.
   - The release manifest keeps `devops_sha`, now equal to `sha`, so the deploy command and the release log keep their format.
   - The VM checks out NFHotel at the release commit. `nfhotel-deploy` accepts the origin `NF-Hotel/NFHotel` and uses `ops/` of it.
5. **Checks.** An `ops` CI workflow runs the linters and the script tests of DevOps on every pull request to `main` and on `main`; a path filter would leave its required checks unreported on other pull requests (RC-148). The application CI runs when the application changes. Both are required checks.
6. **Ownership.** CODEOWNERS names the reviewers of `ops/`, `.github/` and the Dockerfiles. Changes there need their review, as in DevOps.
7. **Order.** The deploy that is ready (NFHotel#195 on DevOps `b63fbed`) may go first: this decision changes where the code lives, not what runs. Then `/opt/nfhotel` on the VM is switched to an NFHotel checkout by S03, in a reviewed step with a backup and a way back.
8. **DevOps.** Its open pull requests are closed with a link to the NFHotel PR that carries their content. Its MIL-023 work (#10, #12) and framework update (#5) are moved as NFHotel pull requests of their own. The repository is then archived (read-only, not deleted), with a README that points here.

**Current state (2026-10-21, [RC-161]).** Decisions 1 to 7 are done. The VM was switched on 2026-10-21, and CD has deployed from NFHotel since then. The `NF Hotel Ops CI` jobs are required checks of the ruleset "Approve merges" since 2026-10-21. Decision 8 is open: NF-Hotel/DevOps is not archived and has open pull requests (NFHotel#205). Since [ADR-0018], CD installs the root commands of `ops/` from the release commit.

## Consequences

**Positive:**

- One source of truth: the deploy files that are reviewed are the ones that run, so the drift that broke the market analysis cannot happen again.
- A change to the application and its deployment is one pull request, one review and one release.
- No pins between repositories, no Dependabot bumps of them, one registry of IDs and one clone for local work.
- The protections stay as they are: the ruleset, CODEOWNERS and the `production` approval.

**Negative:**

- The NFHotel repository is larger, and its CI runs more workflows, filtered by path.
- Anyone with write access to NFHotel also reaches `ops/`. Review by CODEOWNERS is the control, not repository permissions.
- The move is a one-time cost: the VM checkout is switched once, and the open DevOps PRs need to be carried over.
- If a second application ever needs the same platform, the operations may have to be split out again. That would be a new ADR, with this one as the record of why it was merged.

**Quality:** maintainability and reliability improve (one copy, no drift); security is unchanged (ISO/IEC 25010:2023).

## Affected Artifacts

- [ADR-0012]: superseded by this ADR.
- [ADR-0007]: the release and CD use local actions; the manifest's `devops_sha` equals `sha`.
- [MIL-025]: its goal is reversed; its open tasks are replaced by [MIL-031].
- [MIL-031]: carries out this decision.

---

[BC-001]: ../business-case.md
[SA-001]: ../stakeholder-analysis.md
[ADR-0007]: ./adr-0007-cicd-and-supply-chain.md
[ADR-0012]: ./adr-0012-devops-repository-ownership.md
[MIL-025]: ../milestones/mil-025-devops-repository-consolidation.md
[MIL-031]: ../milestones/mil-031-consolidate-devops-into-nfhotel.md
[fdfc65c]: https://github.com/NF-Hotel/NFHotel/commit/fdfc65cf7e3e03cacac7a0e9393f031dc80679a4
[ADR-0018]: ./adr-0018-deploy-command-updates-itself.md
[RC-161]: ../sqa/reviews/rc-161-adr-refresh.md
