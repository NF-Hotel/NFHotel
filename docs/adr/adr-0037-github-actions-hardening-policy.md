# ADR-0037 GitHub Actions hardening policy

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0037 |
| CrossReference | [MIL-059], [DSA-001], [ADR-0007], [ADR-0019], [ADR-0021], [ADR-0032] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-04 | Accepted | mikemike | AI as stand-in for S01 | Initial version; written by the AI under MIL-059, which S01 accepted in chat on 2026-11-04; accepted after RC-280 Go | [f7e9868] |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

**Who decided what.** S01 asked on 2026-11-04 for a security audit of the workflows and the CI/CD supply chain against the techniques of the GhostAction campaign, and accepted the plan of MIL-059 in chat, with four repository settings the AI may apply. The policy of this record was written by the AI.

The GhostAction campaign of 2026 took over GitHub accounts and then changed or added workflows in the repositories those accounts could write to: a step that sent the secrets of the job to a host of the attacker, an action pinned to a commit the attacker controlled, a trigger that ran the attacker's code with a write token. The workflows of NFHotel already follow most of the rules that stop this (MIL-022, ADR-0007, the rules on the wiki page Workflows), but the rules were kept by review, by actionlint and by the Scorecard after the merge: nothing in the pipeline refused a change that broke one, and the settings of the repository allowed any action from any publisher. The audit of MIL-059 found no sign of the campaign in this repository, and the weaknesses that the security audit report lists.

Forces: a rule that only a reviewer checks is not a control when one account merges alone (ADR-0019); a tool that checks the rules is itself a supply chain dependency; the rules must be readable by the team and run on a developer machine without a tool to install; a setting of the host holds outside the repository and cannot be changed by a pull request.

Options considered:

1. **The settings of the host alone.** Require every action to be pinned to a commit SHA, and allow only GitHub-owned, verified and listed publishers. They refuse an unpinned or foreign action and hold outside the repository, but say nothing about token permissions, a credential kept in a checkout, a trigger, a secret or an expression in a script.
2. **A third-party scanner in CI**, such as zizmor, next to actionlint. It covers template injection, excessive permissions, unpinned actions, credential persistence and dangerous triggers with maintained rules, but is one more pinned dependency to update and to trust, and its rules are not the team's own words.
3. **A policy test of the repository's own rules, with the settings of option 1 behind it.** A bash test of seven rules, written as the team states them, with a self-test of fixtures that break each rule; run by the required check Ops linters, so a change that breaks a rule cannot merge; no dependency beyond grep, awk and sed. It reads the layout of this repository (two-space indentation, steps at six spaces), not YAML in general, so another layout needs the test updated.

## Decision

We follow option 3. The seven rules of `ops/tests/test-workflow-policy.sh` are the policy: (1) every action outside this repository is pinned to a full commit SHA with its version in a comment; (2) every workflow declares `permissions: contents: read` at the top, and a job declares the write access it needs; (3) every checkout sets `persist-credentials: false`; (4) no workflow is triggered by `pull_request_target`; (5) a secret is read only by the step that uses it, and a job holds at most a yes/no derived from one; (6) no expression is interpolated into a `run:` script, event data reaches a script through `env:`; (7) no download or decoded text is piped to a shell. Behind the test, the repository requires SHA pinning as a setting and allows only GitHub-owned, verified and the two listed publishers (`ossf/scorecard-action`, `tailscale/github-action`), and the ruleset "Approve merges" requires every check that runs on a pull request, CodeQL and SonarQube Cloud included. The controls of the accounts, two-factor authentication, the roles of the members and the GitHub Apps, are the organisation's and are decided by S01 outside this repository; the security audit report lists them with their steps.

## Consequences

**Positive:**

- A workflow change that breaks a rule fails the required check Ops linters before anyone reads it, and the host refuses an unpinned or foreign action even if the test were removed.
- The rules are the team's own text in one file, and run on a developer machine with `bash ops/tests/test-workflow-policy.sh`.
- A secret or a token reaches only the step that needs it, so a step that the campaign would add gets no credential it was not given.

**Negative:**

- The test knows the layout of this repository; a workflow written with another indentation, or a reusable workflow, needs the test extended before it passes.
- A new publisher of an action must be added to the allowed patterns of the setting by hand before its pull request can run.
- The rules do not cover the inputs of a composite action that a caller fills from event data; the caller's `with:` is reviewed as before.

**Quality:** security improves, because an account or an action that turns against the repository is stopped by a check and a setting; maintainability holds, one test and no new dependency; flexibility decreases a little, because the layout and the publishers are fixed (ISO/IEC 25010:2023).

## Affected Artifacts

- [MIL-059]: tasks 4 and 7 implement this record; the security audit report of MIL-059 records every setting before and after.
- [DSA-001]: risks R2, R3 and R8; the policy is a mitigation of R3.
- [ADR-0007]: the pipeline this policy protects.
- [ADR-0019]: the review decision this policy compensates for.
- [ADR-0021]: the release gate, which stays as it is.
- [ADR-0032]: the findings of the audit are shown where someone can act on them.

---

[MIL-059]: ../milestones/mil-059-github-actions-security-audit.md
[DSA-001]: ../sqa/devsecops-assessment.md
[ADR-0007]: ./adr-0007-cicd-and-supply-chain.md
[ADR-0019]: ./adr-0019-approvals-by-the-owner.md
[ADR-0021]: ./adr-0021-automatic-deploy-when-green.md
[ADR-0032]: ./adr-0032-what-code-scanning-shows.md
[f7e9868]: https://github.com/NF-Hotel/NFHotel/commit/f7e9868ef2889b4a15ea91d698d14fde5763ebb9
