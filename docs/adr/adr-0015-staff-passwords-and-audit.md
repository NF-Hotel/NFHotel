# ADR-0015 Staff passwords, sessions and audit

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0015 |
| CrossReference | [BC-001], [MIL-030], [ADR-0005], [UC-005], [UC-006] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Proposed | mikemike | AI as stand-in for S01 | Initial version | pending |
| 2026-10-20 | Accepted | mikemike | AI as stand-in for S01 | Accepted after RC-134 Go | pending |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

[ADR-0005] gave the back office staff accounts, a password hash, an 8 hour JWT and a sign-in throttle, and left four things open: there is no way to change a password, accounts are made only by a command on the VM, a token cannot be revoked, and nothing records who did what. The initial password of the production Manager has been seen in a chat, and `EnsureFirstManagerAsync` creates the Manager only in an empty table, so editing the environment file does not replace it.

Options considered:

1. **Reset by a mailed one-time link.** The usual self-service route, but the hotel has no mail service in the stack and a mail server is one more thing to run and secure.
2. **No revocation, shorter tokens.** Simple, but a stolen or old session would still work for minutes or hours after a password change or a disable.
3. **A revocation list of token ids.** Works, but needs a store to keep and expire the ids.
4. **A security stamp per account**, compared on every staff call. One integer on the row that already exists, and the check needs only the account row.
5. **A second factor (TOTP).** Worth doing later; it does not replace the need to change passwords and end sessions.

## Decision

We follow option 4 for sessions, and a Manager-typed temporary password for recovery.

- **Security stamp.** `StaffMember.SecurityStamp` (integer, starts at 1) rises on every password change, reset, disable and enable. The token carries it as the claim `stamp`. The API validates the token as before, then loads the account and refuses with 401 when the account is missing or inactive or the stamp differs. This also makes a disable effective at once.
- **Own password change.** `POST api/staff/me/password` takes the current and the new password. The current password is verified with the hasher; a wrong one counts as a failure of the key `pwd:<id>` in `ILoginThrottle` (5 in 15 minutes, then 429). On success the hash is replaced, the stamp rises, the "must change" flag is cleared, and a new token is returned so the caller stays signed in.
- **Reset by a Manager.** `POST api/staff/{id}/password` sets a temporary password typed by the Manager, raises the stamp, sets "must change password" and clears the sign-in throttle of the target. An account with the flag gets a token whose claim `must_change` is true; the API accepts such a token only on `POST api/staff/me/password`, and the web application sends it to the change page.
- **First Manager.** The account made from `Staff__InitialManager__*` has "must change password". After the owner has changed it, the password variable is removed from the VM environment file; the user name variable may stay.
- **Password policy.** One function in Application, used for create, change and reset: 12 to 256 characters, not equal to the user name (any case), not equal to the current password, not in a list of common passwords (an embedded list). No composition rules (NIST SP 800-63B).
- **Lock-out protection.** A Manager cannot disable their own account. The last active Manager cannot be disabled. The break-glass command `reset-staff-password <user name>` takes the password from `STAFF_PASSWORD`, like `create-staff`, and sets "must change password".
- **Permissions.** `Staff.Manage` (Manager only) and `Staff.ChangeOwnPassword` (Receptionist, Manager, Housekeeping) join the role table; the Access Control Matrix row for staff accounts is the Manager.
- **Audit.** The table `staff_audit_event` is append-only: id, time (UTC), actor id and name (null for an anonymous sign-in attempt), action, target id and name, outcome (`Success` or the error code), remote address. It is written in the same transaction as the change for account events, and by the sign-in use case for sign-in success, the sign-in failure of an existing account, and lockout. A made-up user name writes nothing, so guessing cannot fill the table. Nothing in it is a password, a hash, a token or a stamp. Only `GET api/staff/audit` (Manager) reads it; the code has no update or delete. Retention is open (question to S01); until decided, nothing is deleted.
- **Rate limits.** Besides the change-password key above, the Manager write endpoints are limited to 30 requests a minute per actor, answered with 429.
- **Uniform answers.** A wrong current password and a missing account give the same error code, so the change endpoint does not tell which accounts exist.

## Consequences

**Positive:**

- The compromised initial password stops working the first time the owner changes it, and every other session of the account ends with it.
- A leaving member loses access at once, not at the end of the shift.
- Accounts are managed on a screen; no one needs a shell on the VM except for the break-glass case.
- Account changes and failed sign-ins can be traced to a person and an address.

**Negative:**

- One account read per staff call (by primary key; negligible for a hotel's traffic).
- A temporary password is typed by the Manager, who therefore knows it until the person changes it. The forced change limits that window.
- The throttles are in memory: a restart of the API clears them (as in ADR-0005).
- The audit table grows without a retention rule until S01 decides one.
- The remote address is the one that nginx forwards, which is only as trustworthy as the proxy chain.

Quality attributes: security improves (revocation, accountability, recovery); usability improves (self-service); maintainability is neutral (one policy function, one table); performance is neutral (one extra read).

## Affected Artifacts

- [MIL-030] — decided here; its tasks build and deploy it
- [ADR-0005] — closes its open items "no revocation" and "no account management"
- [UC-005], [UC-006] — the use cases that apply these rules
- [BC-001] — the new objective and success criterion

---

[BC-001]: ../business-case.md
[MIL-030]: ../milestones/mil-030-staff-accounts-and-password.md
[ADR-0005]: ./adr-0005-backoffice-login.md
[UC-005]: ../use-cases/uc-005/uc.md
[UC-006]: ../use-cases/uc-006/uc.md
