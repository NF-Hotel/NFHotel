# MIL-063 The CD scope check waits for the tailnet path

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-063 |
| CrossReference | [BC-001], [ADR-0007], [ADR-0021], [ADR-0038], [MIL-021] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-05 | Accepted | mikemike | AI as stand-in for S01 | Initial version; the failed first attempt of CD run #170 on 2026-11-05, read in the journal of the VM, and S01's request in chat to plan the fix ("Ja, planlæg"); S01 accepted the plan in chat on 2026-11-05 ("Accepter planen") after RC-295 Go | pending |

---

## Purpose

Decide how the CD step "Check the tailnet scope of this node" stops failing a deploy when the runner has only just joined the tailnet. On 2026-11-05 at 19:30 UTC the first attempt of CD run #170 failed with "port 22 of nf-hotel does not answer" five seconds after the runner's node came up; the journal of `nf-hotel` shows that no connection from the runner reached its Tailscale SSH server in those seconds, while the VM, its Tailscale SSH and the policy were in order, and the second attempt passed the same check ten minutes later. The step tests port 22 once, with a five-second timeout, right after `tailscale up`; the path between a brand-new node and the VM (a DERP relay, then direct) is not always up that fast.

The step exists to prove, on every deploy, that the CI node reaches port 22 of the VM and nothing else ([MIL-021] criterion 7, [ADR-0007] decision 4). The fix keeps that proof and removes the race: the step waits for port 22 for up to a minute before it fails, says how long it waited, and then tests the other ports once each, as today. A port that must not answer still fails fast; an open one is still a warning, not a failed deploy.

## Deliverable

- **`ops/scripts/cd-steps.sh`**: in `scope`, port 22 is tried until it answers or `SCOPE_WAIT_SECONDS` (default 60) have passed, with five seconds per try as today; the log line says after how many seconds it answered; the other ports are tested once each after that. The header describes the wait.
- **`ops/tests/test-cd-steps.sh`**: a listener that starts a few seconds after the step passes; a port that never answers fails after the deadline (a short deadline for the test); the existing checks of the open port, the closed ports and the peers unchanged.
- **The wiki page Workflows**: the "Does" row of the CD workflow says that the step waits up to a minute for the tailnet path.
- **The proof on the host**: the CD run of the merge passes the scope step on its first attempt, and its log shows the wait.

Order: the review of this document first (QC-MIL-001 and QC-LANG-001), then tasks 1 to 3 on one branch, task 4 with the pull request and the deploy, then task 5.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | `cd-steps.sh scope` keeps trying port 22 of the VM until it answers or `SCOPE_WAIT_SECONDS` (default 60) have passed, and logs the seconds it took; a listener that starts late passes, a port that never answers fails after the deadline; proven by `test-cd-steps.sh` in Ops CI | Yes | No |
| 2 | The other ports are tested once each after port 22 answers; an open one is still a warning and a closed one still passes; the existing checks of `test-cd-steps.sh` pass unchanged | Yes | No |
| 3 | The CD run of the merge of this milestone passes the step "Check the tailnet scope of this node" on its first attempt, and its log names the seconds waited | Yes | No |
| 4 | The wiki page Workflows describes the wait in the "Does" row of the CD workflow, and `test-wiki-coverage.sh` passes | Yes | No |
| 5 | The code review against `QC-SH-001` is Go, `lint.sh` is clean, and the job `Ops script tests (test-cd-steps.sh)` is green | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-021 | Criterion 7 and the scope step it defines; the fix keeps its proof |
| ADR-0007 | Decision 4: the CI node reaches port 22 of the VM only, over the tailnet |
| ADR-0021 | The deploy after the merge, in which the step runs |
| ADR-0038 | The CD run of the merge, and so the proof of criterion 3, waits for the approval of S01 |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001 success criterion 26: every deploy runs the image digests of a green release after an approval; a deploy must not fail on a race in its own checks | Criteria 1, 3 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-07. It changes one function, its test and one wiki row, so it does not move a date of the Business Case.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | The wait in `cd-steps.sh scope` | A helper that calls `reach` until port 22 answers or `SCOPE_WAIT_SECONDS` (default 60) have passed, five seconds per try; `scope` uses it for the SSH port, fails with the seconds in the message when the deadline passes, and logs "answers (after N s)"; the other ports are tested once each as today. The header of `ops/scripts/cd-steps.sh` describes the wait. | No | criteria 1, 2 |
| 2 | Tests of the wait | `ops/tests/test-cd-steps.sh`: a listener on a new port started in the background after a few seconds, and `scope` with `SCOPE_WAIT_SECONDS=20` passes with "after N s" in its log; `scope` against a port that never answers fails after a short deadline with the seconds in its message; the four existing scope checks unchanged. The job `Ops script tests (test-cd-steps.sh)` of Ops CI runs it. | No | criteria 1, 2, 5 |
| 3 | The wiki row | Wiki page Workflows, the "Does" row of `NF Hotel CD`: the step waits up to a minute for the tailnet path to the VM before it checks what the node reaches. `bash ops/tests/test-wiki-coverage.sh` passes. | No | criterion 4 |
| 4 | Code review, pull request and deploy | The review record of `cd-steps.sh` and its test against `QC-SH-001` before the pull request; the pull request closes one issue per task and carries the milestone; merged in autonomous mode once every required check is green. The CD run of the merge, approved by S01 (ADR-0038), is the proof of criterion 3: its scope step passes on the first attempt and names the seconds waited. | No | criteria 3, 5 |
| 5 | Gate and review record | The gate review record against criteria 1 to 5 with the log of the CD run, the row of MIL-063 in the traceability matrix, the Accepted row of this document, and the closing of the GitHub milestone. | No | criteria 1 to 5 |

---

[BC-001]: ../business-case.md
[ADR-0007]: ../adr/adr-0007-cicd-and-supply-chain.md
[ADR-0021]: ../adr/adr-0021-automatic-deploy-when-green.md
[ADR-0038]: ../adr/adr-0038-approval-before-a-deploy.md
[MIL-021]: ./mil-021-secure-cicd-foundation.md
