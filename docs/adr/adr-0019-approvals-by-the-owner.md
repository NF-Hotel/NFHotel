# ADR-0019 The owner merges and approves deploys alone, behind the required checks

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0019 |
| CrossReference | [SA-001], [ADR-0007], [ADR-0016], [ADR-0018], [ADR-0021] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-22 | Deprecated | mikemike | AI as stand-in for S01 | Initial version; the decision was made and applied by S01 in chat on 2026-10-22, accepted after RC-167 Go | [cd9cdf4] |
| 2026-10-23 | Accepted | mikemike | AI as stand-in for S01 | Amended by ADR-0021: no deploy approval (RC-170) | [330b524] |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

**Who wrote what.** The decision is S01's, given in chat on 2026-10-22: "Jens should not be added as a reviewer; I must be able to approve myself from now on". S01 then chose these answers:
- for pull requests, "no approval for anyone";
- for production, "you approve yourself";
- for CODEOWNERS, "remove him".

The reasons and consequences below were written by the AI assistant. They are not a quote of S01.

Since ADR-0007, two people stood between a change and production:
- a pull request needed one approving review by someone other than its author, and the latest push had to be approved;
- a deploy needed an approval in the environment `production`, by a required reviewer (Tirsvad or codemikemike) who was not the person who started the run.

ADR-0018 counted on both people when it let CD install the deploy commands that run as root.

In practice, S01 writes and reviews most changes, and S03 operates the VM (SA-001); S01 holds S03 too. Every change waited for Tirsvad, who is not part of the daily work, and approvals were dismissed again whenever `main` moved (NFHotel#230). On 2026-10-21 and 2026-10-22 these waits held the MIL-021 drills and the gate for hours. The CODEOWNERS file asked Tirsvad for every change of `ops/`, `.github/` and the ADRs.

Options considered:

1. **Keep two people.** It is the strongest control, but it depends on someone outside the daily work, and it blocks every merge and deploy.
2. **A bypass for S01 only.** Others still need an approval, and S01's merges show as bypasses.
3. **No approval for anyone; S01 approves deploys alone.** The required checks and the deploy approval stay; the second person goes.

## Decision

Amended by [ADR-0021] (2026-10-23): the deploy approval of point 2 is removed; CD deploys when every check of the commit is green. The merge rule of point 1 is unchanged.

We follow option 3. A pull request needs no approving review, and its merge still needs all 19 required checks (ruleset "Approve merges"). A deploy to `production` still needs an approval, and S01 may approve a run that S01 started. Tirsvad stays a possible approver and is no longer asked for review by CODEOWNERS.

1. **Ruleset "Approve merges".** These are unchanged:
   - a pull request is required;
   - all 19 checks must be green, the Ops CI jobs included;
   - no direct push, no force push, no deletion, and no bypass actor.

   These change:
   - required approving reviews: 1 → 0;
   - approval of the last push: on → off.
2. **Environment `production`.** "Prevent self-review" is off. The required reviewers are still Tirsvad and codemikemike, only `main` may deploy, and admins cannot bypass. Every approval is recorded in the deployment history.
3. **CODEOWNERS** names `@codemikemike` only.
4. **ADR-0007 decision 2 and ADR-0018** are amended by this decision; both name it.

## Consequences

**Positive:**

- S01 can merge and deploy without waiting for someone outside the daily work. A green change reaches production the same day.
- Approvals are no longer dismissed when `main` moves, because none is required.
- The automated controls stay as they are:
  - 19 required checks, among them tests, formatting, the release build, E2E and DAST;
  - the attestation check before every deploy;
  - the rollback;
  - the reviewed-`main` rule for the root commands of ADR-0018.

**Negative:**

- **No four-eyes principle.** One person can now write, merge and deploy a change, including one to what runs as root on the VM (ADR-0018). A mistake or a compromised account of S01 reaches production after the checks alone. This is accepted by S01.
- The audit trail shows that S01 alone approved a change, not that a second person saw it.
- Any team member with write access can now merge a green pull request without review. The deploy approval by S01 or Tirsvad stays the human gate before production.

## Affected Artifacts

- [ADR-0007] — decision 2 is amended: the person who started a CD run may approve it.
- [ADR-0018] — its context counted two people; the human gate is now the deploy approval alone.
- [ADR-0016] — its context named the review by someone other than the author; that review is no longer required.
- [SA-001] — no change of roles; S01 holds S03 as before.

---

[SA-001]: ../stakeholder-analysis.md
[ADR-0007]: ./adr-0007-cicd-and-supply-chain.md
[ADR-0016]: ./adr-0016-one-repository-for-application-and-operations.md
[ADR-0018]: ./adr-0018-deploy-command-updates-itself.md
[cd9cdf4]: https://github.com/NF-Hotel/NFHotel/commit/cd9cdf46d68e881cc977f9a79fef4494a1a1048b
[ADR-0021]: ./adr-0021-automatic-deploy-when-green.md
[330b524]: https://github.com/NF-Hotel/NFHotel/commit/330b52465cc857807b7c427354cc42e0ec2c2c2e
