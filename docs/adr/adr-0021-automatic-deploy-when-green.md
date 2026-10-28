# ADR-0021 Deploy automatically when every check of the commit is green

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0021 |
| CrossReference | [ADR-0007], [ADR-0018], [ADR-0019], [MIL-021], [MIL-022], [MIL-027] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-23 | Accepted | mikemike | AI as stand-in for S01 | Initial version; the decision was made by S01 in chat on 2026-10-23, accepted after RC-170 Go | [330b524] |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

**Who wrote what.** The decision is S01's, given in chat on 2026-10-23: "we should have more automatic CD, so I do not have to click by hand. It must deploy only if EVERYTHING is green." The reasons, the options and the design below were written by the AI assistant.

Since ADR-0007, a deploy waits in the environment `production` until a required reviewer approves it. ADR-0019 let S01 approve alone. Since then the approval is one person confirming what the checks have already confirmed. It has costs:
- Every merge waits for S01 to open GitHub and click.
- The approvals of 2026-10-21 and 2026-10-22 were missed or given to the wrong run several times, and that held the MIL-021 drills for hours.
- An approval says nothing that the checks cannot.

The CD job also started as soon as `NF Hotel Release` finished. Release waits only for `NF Hotel CI`, so a deploy could start while other checks of the same commit were still running or had already failed:
- `NF Hotel Ops CI`;
- `NF Hotel E2E and DAST`;
- CodeQL;
- SonarCloud.

The human approval was what closed that gap.

Before a deploy, these checks already run on every commit of `main`:
- build, unit and integration tests, and formatting;
- the release build;
- end-to-end tests and the DAST baseline;
- the linters and script tests of `ops/`;
- CodeQL and SonarCloud;
- Scorecard.

Every deploy also does these:
- verifies the attestations of each image;
- takes a backup before the migrations;
- runs a smoke test, with a rollback when it fails (MIL-021).

Options considered:

1. **Keep the manual approval** (ADR-0007, ADR-0019). It is the safest, but S01 has to click on every merge.
2. **Remove the approval and deploy when Release is green.** It is simple, but a deploy could start while E2E, DAST or Ops CI were still running, or after they had failed. That is not "everything green".
3. **Remove the approval, and gate the deploy on every check of the commit.** CD waits until every check that GitHub records on the release commit has finished, and deploys only when all are green.
4. **Deploy on a schedule** (for example nightly) with a gate. That gives fewer deploys, but a fix waits for the next night, and it gives nothing that option 3 does not.

## Decision

We follow option 3. CD deploys a release without a human approval, but only when every check of its commit on `main` has finished green.

1. **The gate.**
   - **When:** the step "Wait until every check is green" of the deploy action runs before any credential for the VM, after the attestations are verified.
   - **What it reads:** every check run that GitHub records on the release commit (`GET /commits/{sha}/check-runs`, latest per name). That covers the workflows of this repository and the apps such as SonarCloud.
   - **Green:** `success`, `skipped` or `neutral`.
   - **It stops at once on** any other result (`failure`, `cancelled`, `timed_out`, `action_required`).
   - **It refuses** a commit with no checks.
   - **It waits** for checks still running (90 × 30 seconds), and it fails when they have not finished by then.
   - **It leaves out** the job's own check, "Deploy to production".
   - The result is in the run summary.
2. **No approval.** The environment `production` keeps its other rules:
   - only `main` may deploy;
   - admins cannot bypass;
   - it holds the tailnet settings.

   Its required reviewers are removed.
3. **Unchanged:**
   - the ruleset on `main` (19 required checks, no bypass, no direct push);
   - the attestation check;
   - the backup before the migrations;
   - the smoke test and the automatic rollback;
   - the tailnet scope check;
   - one deploy at a time (`concurrency: production`).
4. **A manual run** (`workflow_dispatch`) passes the same gate. The rollback drill too.
5. **The CD job** reads the checks with `checks: read`, and its time limit rises to 75 minutes for the wait.

## Consequences

**Positive:**

- A green merge reaches production with no click, usually within the time of the slowest check.
- "Green" means every check of the commit, not only the one that started Release. A red E2E, DAST, Ops CI, CodeQL or SonarCloud now stops a deploy. Before, only the person who approved could stop it.
- The gate and its result are in the run summary, so every deploy shows why it went ahead.

**Negative:**

- **No person stands between `main` and production.** A change that passes every check reaches the VM by itself, including a change to the commands that run as root (ADR-0018). The controls are the checks, the attestations, the smoke test with its rollback, and the branch rules. A broken change that the checks miss is rolled back only when the smoke test fails.
- A check that is flaky or slow now holds the deploy. A red check needs a re-run or a fix before anything deploys.
- A check that GitHub never records cannot be waited for. The gate trusts the list of checks on the commit; the ruleset still demands the 19 required ones before the merge.
- A migration is applied before the deploy and is not undone by a rollback. ADR-0008 keeps migrations backward compatible (expand, then contract) for this reason.

## Affected Artifacts

- [ADR-0007] — decision 2 (approval first) is replaced by the gate.
- [ADR-0019] — the deploy approval of S01 is no longer needed; the merge rule of ADR-0019 is unchanged.
- [ADR-0018] — the human gate it relied on is now the checks.
- [MIL-021] — criterion 3 (CD waits for approval) describes the state before this decision.

---

[ADR-0007]: ./adr-0007-cicd-and-supply-chain.md
[ADR-0018]: ./adr-0018-deploy-command-updates-itself.md
[ADR-0019]: ./adr-0019-approvals-by-the-owner.md
[MIL-021]: ../milestones/mil-021-secure-cicd-foundation.md
[MIL-022]: ../milestones/mil-022-supply-chain-security.md
[MIL-027]: ../milestones/mil-027-dast-and-end-to-end-tests.md
[330b524]: https://github.com/NF-Hotel/NFHotel/commit/330b52465cc857807b7c427354cc42e0ec2c2c2e
