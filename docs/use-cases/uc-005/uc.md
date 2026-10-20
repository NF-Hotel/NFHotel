# UC-005 Change Own Password

## Metadata
| Key | Value |
| --- | --- |
| ID | UC-005 |
| CrossReference | [UCD-001], [US-001], [SA-001], [UC-004], [ADR-0005] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Proposed | mikemike | S01 | Initial version (MIL-030) | [0891cf1] |
| 2026-10-20 | Accepted | mikemike | AI as stand-in for S01 | Accepted after RC-153 Go | [0891cf1] |

---

**Format:** Fully Dressed

## Fully Dressed

- **Scope:** NFHotel platform, hotel back office
- **Level:** user-goal
- **Primary Actor:** Hotel staff
- **Stakeholders and Interests:**
  - S05 — Hotel staff: a password they chose, that nobody else knows, and that they can change without help.
  - S01 — Team 2: a password that has been seen by others (for example in a chat) is replaced at once and the old one stops working everywhere.
  - S03 — Proxmox VE administrator: the first password in the environment file is not a standing credential.
- **Preconditions:** The staff member is signed in (UC-004), or has signed in with a temporary password and has been sent to this function.
- **Postconditions (success guarantee):** The account has the new password. Every session that existed before, on any device, has ended, except the one that made the change. The change is in the audit trail.

### Main Success Scenario

1. Hotel staff open "Change password".
2. The system shows the form with the current password, the new password and its repetition.
3. Hotel staff fill in the three fields and confirm.
4. The system checks the current password and the rules for the new one.
5. The system stores the new password, ends the other sessions, and starts a new session for this device.
6. The system confirms the change and continues with the start of the role.

### Extensions (Alternative / Exception Flows)

- 4a. The current password is wrong:
  1. The system says so and changes nothing. After five wrong attempts in fifteen minutes it refuses further attempts for that account until the time has passed.
- 4b. The new password is shorter than 12 characters, equal to the user name, equal to the current password, or in the list of common passwords; or the repetition differs:
  1. The system names the rule that failed and changes nothing.
- 1a. The account is marked "must change password" (the first Manager, or after a reset by a Manager):
  1. The system opens this function at sign-in and offers no other function until the change is done. The current password is the temporary one.
- 5a. The session has ended while staff were filling in the form:
  1. The system asks them to sign in again and changes nothing.

### Special Requirements / Business Rules

| Step | Rule |
| --- | --- |
| 4 | 12 to 256 characters, no composition rules, not on the block list of the 1 000 most common passwords (ADR-0017) |
| 4 | The check of the current password takes the same time whatever the answer, and a wrong one counts against the account's own limit (ADR-0017) |
| 5 | Other sessions end because the account's security stamp rises; the API checks it on every call (ADR-0017) |
| 5 | The password, its hash and the tokens are never written to a log or to the audit trail |
| all | The password travels over TLS only (MIL-017) |

### Open Issues

- There is no "forgot my password" function: a Manager resets it (UC-006). A Manager who has lost their password uses the `reset-staff-password` command on the VM.
- No second factor yet.

---

[UCD-001]: ../../use-case-diagram.md
[US-001]: ../../user-stories.md
[SA-001]: ../../stakeholder-analysis.md
[UC-004]: ../uc-004/uc.md
[ADR-0005]: ../../adr/adr-0005-backoffice-login.md
[0891cf1]: https://github.com/NF-Hotel/NFHotel/commit/0891cf138311b2c3a9a6e2b20ad92c501ddf80cf
