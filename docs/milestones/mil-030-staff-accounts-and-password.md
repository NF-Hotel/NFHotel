# MIL-030 Staff accounts and password change

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-030 |
| CrossReference | [BC-001], [MIL-016], [UC-004], [UC-005], [UC-006], [ADR-0004], [ADR-0005] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Proposed | mikemike | S01 | Initial version | [0891cf1] |
| 2026-10-20 | Accepted | mikemike | AI as stand-in for S01 | Accepted after RC-149 Go; plan accepted by the user in chat | [0891cf1] |

---

## Purpose

Decide whether the back office can run in production without the ad-hoc commands that MIL-016 left behind. Today a staff account is made by the `create-staff` command on the VM, or by the first Manager that the API creates at start from `Staff__InitialManager__UserName` and `Staff__InitialManager__Password`. There is no way to change a password, to disable an account, or to replace the initial Manager password: `EnsureFirstManagerAsync` does nothing once any account exists, so editing the environment file changes nothing. The initial password of the production Manager was pasted in a chat and must be treated as compromised, so the owner needs the screen and the rules that replace it.

What is found in the code today (ADR-0005, MIL-016, `src/`):

| Fact | Consequence for this milestone |
| --- | --- |
| `StaffMember` has user name, hash, role and `IsActive`; the only change method is `Deactivate()` | Add change of hash, reactivation, a security stamp and a "must change password" flag |
| `IStaffAccountService` has `CreateAsync` and `EnsureFirstManagerAsync` and no endpoint; the minimum password is 12 characters and the maximum 256 | Add the use cases and endpoints, and keep the length rules in one place |
| The token is an 8 hour HS256 JWT with `sub`, `name` and `role`, validated by signature and lifetime only; UC-004 lists "a stolen session works until it expires; there is no revocation list" as open | A password change or a disable must end the old sessions at once, so the API checks the account on each staff call |
| Sign-in is throttled per user name by `InMemoryLoginThrottle` (5 failures in a window of 15 minutes) | The check of the current password in a password change needs the same protection |
| There is no audit trail anywhere in the code, and ADR-0005 does not ask for one | Audit of account events is new in this milestone and is decided in a new ADR |
| The Access Control Matrix says that the Manager owns "staff accounts"; no permission exists for it in `Permissions.cs` | Add `Staff.Manage` (Manager only) and `Staff.ChangeOwnPassword` (every staff role) |

## Decisions to record in ADR-0017

| # | Decision | Why |
| --- | --- | --- |
| 1 | Each account has a security stamp (an integer that rises on every password change, reset, disable and role change). The token carries it as a claim, and the API compares it, and the active flag, with the stored account on every staff call. A mismatch gives 401 | Ends every old session at once, including a stolen one; closes the open issue of UC-004. The cost is one indexed read per staff call |
| 2 | A password change by the owner of the account needs the current password and issues a new token in the same answer, so the person stays signed in on this device and every other session ends | Least surprise for the person; other devices are the ones to cut off |
| 3 | A reset by a Manager sets a temporary password that the Manager types, and sets "must change password". An account with that flag can sign in and can do nothing but change its password until it has | The Manager never learns a lasting password; no mail server is needed |
| 4 | The first Manager is created with "must change password". The initial password in the environment file only seeds an empty database; after the first change, the variable is removed from the VM so that it holds no secret | The compromised password stops working the first time the owner signs in, and a copy in the environment file is not a standing credential |
| 5 | Password rules, one function in Application used by create, change and reset: 12 to 256 characters, not equal to the user name, not equal to the current password, not in a list of the 1 000 most common passwords shipped as a resource. No composition rules (NIST SP 800-63B) | Length and a block list stop more guessing than "one capital letter" rules |
| 6 | The Manager cannot disable or demote their own account, and the last active Manager cannot be disabled, so the back office cannot lock itself out. A break-glass command `reset-staff-password <user name>` on the VM (password in `STAFF_PASSWORD`, as `create-staff` does) remains for a lost last Manager | Recovery without a database edit |
| 7 | Staff account events are written to an append-only table `StaffAuditEvent` in the same transaction as the change: when, actor id and name, action, target id and name, outcome, remote address. No password, hash, token or stamp is ever written. Actions: sign-in success and failure, password change, reset, create, disable, enable, lockout. Only a Manager can read it, on a page; nothing updates or deletes a row | Who did what, and a lockout or a guessing attempt becomes visible; data minimisation: no more than the account events |
| 8 | Rate limiting: the current-password check of a change uses `ILoginThrottle` with its own key per account (5 failures in 15 minutes, then 429), the Manager endpoints are limited per actor (30 writes per minute), and a reset or an enable also clears the sign-in throttle of the target | Stops guessing through the change form and a runaway script; a Manager can unlock a person |

## Deliverable

- ADR-0017 recording the decisions above, reviewed.
- Use cases UC-005 (a staff member changes their own password) and UC-006 (a Manager manages staff accounts) with wireframes, domain model and system sequence diagrams; `docs/domain-model.md`, `docs/dictionary.md`, `docs/use-case-diagram.md` and `docs/user-stories.md` updated; UC-004 open issues amended.
- Domain and Application: the security stamp, the "must change password" flag, change and reset of the password, disable and enable, the last-Manager rule, the password policy, the audit event, and the permissions `Staff.Manage` and `Staff.ChangeOwnPassword` in the role table.
- Infrastructure and Api: the migration (two columns on the staff table and the audit table), the stamp claim and its check on every staff call, the endpoints below, the throttles, and the `reset-staff-password` command.
- Web: the page "Change password" for every role, the page "Staff" for the Manager (list, create, disable, enable, reset), the page "Audit" for the Manager, the redirect of a "must change" account to the change page, navigation by role.
- Deploy: the initial Manager flag, removal of `Staff__InitialManager__Password` from the VM after the change, the wiki and the env example updated.
- Production: the owner changes the production Manager password through the new page, and the old password is shown to be refused.
- Unchanged: the public booking flow, the guest sign-in (MIL-018), the token lifetime and signing key.

| Endpoint (below `api/staff`, staff token required) | Permission |
| --- | --- |
| `POST me/password` (current, new) | `Staff.ChangeOwnPassword` |
| `GET` list, `POST` create | `Staff.Manage` |
| `POST {id}/disable`, `POST {id}/enable`, `POST {id}/password` (reset) | `Staff.Manage` |
| `GET audit` (paged) | `Staff.Manage` |

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | A signed-in staff member changes their password with the right current password; a wrong current password, a new password shorter than 12, equal to the user name, equal to the old one or in the block list is refused with a field code; the sixth wrong current password in the window gets 429 | Yes | No |
| 2 | After a change, the token issued before it gets 401 on the next staff call, the new token works, and the old password no longer signs in | Yes | No |
| 3 | A Manager creates an account, disables it (its token gets 401 at once and it cannot sign in), enables it again, and resets its password (the person must change the temporary password before any other page opens) | Yes | No |
| 4 | A Receptionist and a Housekeeping user get 403 on every `Staff.Manage` endpoint; every role may change its own password; the permission table equals the Access Control Matrix | Yes | No |
| 5 | A Manager cannot disable themselves, and the last active Manager cannot be disabled; the refusal names the rule | Yes | No |
| 6 | Each account event of decision 7 writes one audit row in the same transaction; a test searches the audit rows, the logs and the API answers for the test passwords, the hashes and the tokens and finds none; no endpoint updates or deletes an audit row | Yes | No |
| 7 | The first Manager is created with "must change password"; a second start changes nothing; `reset-staff-password` recovers an account when the password comes from `STAFF_PASSWORD` | Yes | No |
| 8 | In the VM, the owner has replaced the production Manager password through the page; the initial password is refused with 401; `Staff__InitialManager__Password` is absent from the VM environment file; the audit page shows the change | Yes | No |
| 9 | `dotnet build` has 0 warnings, format is clean, all tests pass (including the end-to-end test of the change flow in a Docker container), the ZAP baseline reports no new High or Medium alert on the new pages; `qc-programming-csharp`, `qc-programming-shell` and `qc-adr` reviews have the verdict `Go` | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-016 | The staff table, hasher, token issuer, throttle and the `create-staff` command that this milestone extends |
| MIL-017 | The password travels in clear text inside the TLS tunnel only; the pages must not be used without it |
| MIL-027 | The end-to-end tests and the DAST scan that cover the new pages |
| NF-Hotel/DevOps (ADR-0012) | The CD deploys the migration; the VM environment file and `deploy-services.sh` live there |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| New objective and success criterion (next free numbers, task 1): staff accounts are managed on a screen, a password is changed with the current one, and a change or a disable ends the old sessions | All tasks |
| Success criterion 22 (back office login): the same permission and 401/403/429 rules for the new endpoints | Tasks 5 to 7 |
| `docs/refactor/Access-Control-Matrix.md`: the Manager owns staff accounts | Task 5 |
| UC-004 open issues: "no revocation list" and "staff accounts are made by the command line" | Tasks 4, 5 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01; S03 for the VM steps and the secret handling |

## Target Date

2026-11-10, three weeks: one new table, a change to the token check on every staff call, three screens and a production step. The production password change (task 9) is done as soon as the page is deployed, not at the end.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Amend Business Case and Project Plan | Add the objective, the scope item, the success criterion and the MIL-030 schedule row; review again. | No | |
| 2 | Record the decisions in ADR-0017 | The eight decisions above, with the options considered (a one-time reset link by mail, no revocation and a shorter token, a revocation list instead of a stamp, a TOTP second factor). | No | |
| 3 | Change own password | A staff member changes their password with the current one and stays signed in on this device; includes the forced change after a reset and for the first Manager; wireframe kept with the use case. | Yes | UC-005 |
| 4 | Manage staff accounts | A Manager lists, creates, disables, enables and resets accounts and reads the audit trail; wireframes kept with the use case. | Yes | UC-006 |
| 5 | Domain, Application and permissions | Security stamp, must-change flag, change and reset, last-Manager rule, password policy and block list, audit event port, the two permissions in the role table; unit tests for every rule and for every role and operation. | No | |
| 6 | Infrastructure and API | Migration, stamp claim and its check on each staff call, endpoints, throttles, audit writer, the `reset-staff-password` command; API tests for 401, 403, 429 and the session end. | No | |
| 7 | Web | Change password page, Staff page, Audit page, redirect for "must change", navigation by role; page tests and an end-to-end test of the change flow (environment: Docker container). | No | |
| 8 | Deploy and wiki | Env example without the initial password after first use, the wiki page for account management and the recovery command. | No | |
| 9 | Replace the production password | Deploy through the CD, the owner signs in with the initial password, changes it on the page, removes `Staff__InitialManager__Password` from the VM environment file, and the commands and evidence are recorded (criterion 8). | No | |
| 10 | Check and review | Build, format, test, ZAP baseline; reviews against `qc-programming-csharp`, `qc-programming-shell` and `qc-adr`. | No | |

---

[BC-001]: ../business-case.md
[MIL-016]: ./mil-016-backoffice-login.md
[UC-004]: ../use-cases/uc-004/uc.md
[UC-005]: ../use-cases/uc-005/uc.md
[UC-006]: ../use-cases/uc-006/uc.md
[ADR-0004]: ../adr/adr-0004-database-api.md
[ADR-0005]: ../adr/adr-0005-backoffice-login.md
[0891cf1]: https://github.com/NF-Hotel/NFHotel/commit/0891cf138311b2c3a9a6e2b20ad92c501ddf80cf
