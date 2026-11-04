# ADR-0038 Approval before a deploy, behind the gate

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0038 |
| CrossReference | [ADR-0021], [ADR-0019], [ADR-0007], [ADR-0018], [ADR-0037], [SAR-001], [MIL-021], [SA-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-04 | Accepted | mikemike | AI as stand-in for S01 | Initial version; the decision was made by S01 in chat on 2026-11-04 and the setting applied the same day; accepted after RC-281 Go | [7af26c6] |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

**Who decided what.** S01 asked in chat on 2026-11-04 how to make the deployment manual again, so that a merge no longer deploys by itself, and chose the required reviewer on the environment when the AI proposed it ("Ja, gør det"). The reasons, the options and the consequences below were written by the AI.

Since ADR-0021 (2026-10-23) a green merge to `main` reaches the VM by itself: `NF Hotel Release` builds and attests the images, and `NF Hotel CD` deploys them once every check of the commit is green. ADR-0021 removed the required reviewers of the environment `production` that ADR-0007 had set and ADR-0019 had narrowed to S01, and named the loss in its first negative consequence: no person stands between `main` and production. Since then:

- every merge is a deploy, so S01 cannot merge a change in the morning and let it reach the guests' site at a quiet hour;
- the security audit of MIL-059 (SAR-001, 2026-11-04) found no compromise, but its finding SEC-02 says that one taken-over account of the five administrators can change a workflow, merge it after the checks and reach production with no person in between; the policy test of ADR-0037 narrows what such a workflow can do, and puts nobody back in the path;
- a rollback drill, a redeploy and the automatic deploy all run as soon as their conditions hold, with no moment at which S01 decides.

Forces: the gate of ADR-0021, with the attestation check, the backup, the smoke test and the rollback, protects against a broken release, not against a release at a wrong moment or from a wrong hand; an approval costs S01 one click per deploy, and in October approvals were missed or given to the wrong run (the context of ADR-0021), so a deploy then waits, and GitHub expires a waiting run after 30 days; S01 is the only person in the daily work (SA-001, ADR-0019), so the approver is S01 and self-review must stay allowed.

Options considered:

1. **Keep the automatic deploy** (ADR-0021). No click, but no person between `main` and production, and no choice of the moment.
2. **A required reviewer on the environment `production`, with the gate behind it.** Every CD run, the automatic one included, waits in the environment until S01 approves it; after the approval the run verifies the attestations, waits for every check and deploys as before. One setting of the host, no change of the workflows, and the deployment history records who approved which run.
3. **Manual runs only.** Remove the `workflow_run` trigger of `cd.yaml`, so CD runs only by hand with the run ID of a release and `apply`. No run waits anywhere, but each deploy needs the run ID looked up and typed, a release that nobody deploys leaves no pending run that shows it, and the control is a line in the workflow, not a setting the owner holds outside the repository.
4. **A wait timer on the environment.** The job starts after a delay, during which the run can be cancelled. It gives time to stop a deploy, not a decision: a run nobody looks at deploys anyway.

## Decision

We follow option 2. A deploy to production needs an approval by S01 in the environment `production` before anything runs, and the gate of ADR-0021 stays behind the approval.

1. **The environment `production`** requires a review by `codemikemike` (S01). "Prevent self-review" is off, so S01 approves the runs S01 started (ADR-0019). Only `main` may deploy, admins cannot bypass, and the environment holds the tailnet settings as before. The AI applied the setting through the API on 2026-11-04 at 13:34 UTC on S01's word in chat; the values before and after are kept with the prompt log of the session.
2. **Every run waits.** The automatic run after a green `NF Hotel Release`, a manual run (`workflow_dispatch`) and the rollback drill wait in the environment. GitHub notifies the reviewer, and S01 approves or rejects the run under "Review deployments" on its page. A run that nobody reviews expires after 30 days; its release is then deployed by hand with the run ID of its `NF Hotel Release` run (the wiki page CI-CD).
3. **No credential before the approval.** The job does not start before the approval, so the OIDC token for the tailnet and the GitHub token for the attestations and the checks are issued only after it.
4. **The gate of ADR-0021 is unchanged.** After the approval the job reads the manifest, verifies the attestations, waits until every check of the release commit has finished green, and deploys; one red check still stops it. Decision 2 of ADR-0021 ("No approval") is replaced by this record; its decisions 1, 3, 4 and 5 stay.
5. **Unchanged:** the workflows, the deploy action and the scripts (only their comments and the wiki now say that a person approves); the ruleset on `main`; the backup, the smoke test and the rollback; one deploy at a time (`concurrency: production`).

## Consequences

**Positive:**

- S01 decides when a release goes out: a merge is no longer a deploy. A change can be merged when it is ready and deployed when no guest is booking.
- A person stands again between `main` and production, before any credential exists. A merged change of a taken-over account (SAR-001 SEC-02), or a change S01 wants to see once more, waits for S01's click, and the deployment history of GitHub records who approved which run and when.
- The gate keeps its promise: an approval cannot push a red commit through, because the checks are read after the approval.

**Negative:**

- Every deploy costs S01 one click, and a forgotten run deploys nothing: the cost that ADR-0021 removed is back. A release nobody approves for 30 days expires and must be deployed by hand with its run ID.
- The approval is S01's alone (ADR-0019): a moment of choice, not a second pair of eyes, and S01 can approve the wrong run, as happened in October. The automated controls (the checks, the attestations, the smoke test with its rollback) stay the protection against a broken release.
- A rollback through CD (a manual run with an earlier release) waits for the same approval, so the fastest rollback stays `nfhotel-deploy rollback` on the VM.

**Quality:** security improves, because a credential for production exists only after a person has approved the run and the deployment history names that person; reliability of the release path is unchanged, the gate and the rollback stay; the time from a merge to production grows by the time until S01 approves (ISO/IEC 25010:2023).

## Affected Artifacts

- [ADR-0021] — decision 2 ("No approval", the required reviewers removed) is replaced by this record; the gate of its decision 1 stays and runs after the approval.
- [ADR-0019] — point 2 (the environment `production`: S01 approves alone, self-review allowed) is in force again, with `codemikemike` as the only required reviewer.
- [ADR-0007] — decision 2 ("Approval first") is in force again, behind the gate of ADR-0021.
- [ADR-0018] — the human gate its context relies on is the deploy approval again, not the checks alone.
- [ADR-0037] — unchanged; its context names ADR-0019 as the review decision the policy compensates for, and this record puts a person back before production, not before the merge.
- [SAR-001] — finding SEC-02 is partly mitigated: a merged workflow of a taken-over account no longer reaches production without S01's approval; the finding stays open for the roles of the members.
- [MIL-021] — criterion 3 (CD waits for approval in `production`, and the job receives no credential before approval) describes the current state again.
- [SA-001] — no change of roles; S01 holds S03 as before.

---

[ADR-0021]: ./adr-0021-automatic-deploy-when-green.md
[ADR-0019]: ./adr-0019-approvals-by-the-owner.md
[ADR-0007]: ./adr-0007-cicd-and-supply-chain.md
[ADR-0018]: ./adr-0018-deploy-command-updates-itself.md
[ADR-0037]: ./adr-0037-github-actions-hardening-policy.md
[SAR-001]: ../sqa/security-audit-2026-11.md
[MIL-021]: ../milestones/mil-021-secure-cicd-foundation.md
[SA-001]: ../stakeholder-analysis.md
[7af26c6]: https://github.com/NF-Hotel/NFHotel/commit/7af26c62c64d653f912ba876b1d2fd4c7c170f40
