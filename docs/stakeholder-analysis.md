# Stakeholder Analysis

## Metadata
| Key | Value |
| --- | --- |
| ID | SA-001 |
| CrossReference | [BC-001] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Deprecated | Claude (AI) | S04 (confirmed language/domain); AI as stand-in for S04 | Accepted after RC-002 Go | [b690b57] |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S01 (instructed by the user) | Token role row and revocation rationale changed for the throwaway container | pending |

---

## Purpose

Identifies who influences or is affected by the Proxmox integration test
environment, so that owners, reviewers and RACI entries elsewhere can cite
stable IDs. The method is a power/interest grid; concerns are mapped to FURPS+.

## Stakeholder Summary Table

| ID | Name | Role/Title | Organization | Power Level | Interest Level | Quadrant | Primary Concern (Business Language) |
| --- | --- | --- | --- | --- | --- | --- | --- |
| S01 | Team 2 | Owner of NF Hotel; its members are all developers; owner of the credentials | NFHotel 2.0 project | HIGH | HIGH | Manage Closely | Tests that run alone, repeatably, and never leak secrets |
| S02 | Other NFHotel 2.0 guilds | Developers of the DevOps, frontend, mobile and cybersecurity tracks | NFHotel 2.0 project | MEDIUM | HIGH | Keep Informed | A proven target environment before they depend on it |
| S03 | Proxmox VE administrator | Owner of the Proxmox server | NFHotel 2.0 project (held by S01 for now) | HIGH | MEDIUM | Keep Satisfied | The server is never changed or loaded by tests |
| S04 | Valdamar | Stand-in for the Product Owner; reviewer and approver of the artifacts | NFHotel 2.0 project | HIGH | HIGH | Manage Closely | A clear plan and a result he can accept or reject |

## Power/Interest Classification Rationale

- S01 is the owner of NF Hotel, owns the credentials and builds the tests: high power, high interest. S01 is Team 2; all its members are developers.
- S02 uses the result but cannot block it: medium power, high interest.
- S03 can refuse access to the Docker host or stop the container at any time: high power; interest is medium because the tests are small and read-only.
- S04 (Valdamar) stands in for the Product Owner: he reviews and accepts every artifact, so high power and high interest. S01 and S03 are the same group today (Team 2); the roles stay separate so the Business Case can name who guards the server.

## Primary Concerns and FURPS+ Mapping

| ID | Concern | FURPS+ attribute |
| --- | --- | --- |
| S01 | Run without interaction | Usability, Functionality |
| S01 | Credentials stay out of git | Functionality (security) |
| S02 | Clear pass/fail message naming the failing check | Usability |
| S03 | Read-only access, bounded request time | Reliability, Performance |
| S04 | Reviewable, traceable artifacts | Maintainability |

## Communication Requirements

| ID | Channel | Frequency | Deliverable | Phase / Milestone |
| --- | --- | --- | --- | --- |
| S01 | Chat | Per milestone | Run summary with review records | MIL-001 |
| S02 | Repository README and test output | On change | Runner usage and token instructions | MIL-001 |
| S03 | Chat | Once | Root password in `pve.env` only; token created per run | MIL-003 |
| S04 | Chat | Per milestone | Review records and run summary | MIL-001 |

## Conflicting Interests and Mitigations

| Conflict | Stakeholders | Mitigation |
| --- | --- | --- |
| Useful tests (deep checks) versus no server changes | S02, S03 | Only GET requests; deeper checks need a new milestone and S03's consent |
| Easy sharing of config versus secret hygiene | S02, S01 | Share `pve.env.example` only; `pve.env` stays local and ignored |

## Traceability Analysis

### Business Goal Alignment

| Stakeholder | Concern | Business Case objective |
| --- | --- | --- |
| S01 | Run without interaction | Objective 2 |
| S01 | Credentials stay out of git | Objective 1 |
| S02 | Clear pass/fail message | Objective 3 |
| S03 | Read-only access | Objective 4 |
| S04 | Reviewable, traceable artifacts | Objectives 1-4 |

## Sign-Off

Reviewer and approver: S04 (in autonomous mode the AI acts as his stand-in, see the review record).

---

[BC-001]: ./business-case.md
[b690b57]: https://github.com/NF-Hotel/NFHotel/commit/b690b576d531e9f26709b6f21275cbc675065d03
[88a4946]: https://github.com/NF-Hotel/NFHotel/commit/88a49468aa21b75a0a5f9c8fd5a214164e1676ca
