# UC-006 Manage Staff Accounts

## Metadata
| Key | Value |
| --- | --- |
| ID | UC-006 |
| CrossReference | [UCD-001], [US-001], [SA-001], [UC-004], [UC-005], [ADR-0005] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Proposed | mikemike | S01 | Initial version (MIL-030) | [ea8a47f] |
| 2026-10-20 | Accepted | mikemike | AI as stand-in for S01 | Accepted after RC-154 Go | [ea8a47f] |

---

**Format:** Fully Dressed

## Fully Dressed

- **Scope:** NFHotel platform, hotel back office
- **Level:** user-goal
- **Primary Actor:** Manager (a member of hotel staff with the role Manager)
- **Stakeholders and Interests:**
  - S01 — Team 2: accounts are made, stopped and recovered on a screen, not by commands on the server, and every such act can be traced to a person.
  - S05 — Hotel staff: a new colleague gets access to the functions of the role only; a colleague who leaves loses access at once.
  - S03 — Proxmox VE administrator: no one needs a shell on the VM to manage people.
- **Preconditions:** The Manager is signed in (UC-004) and has changed any temporary password (UC-005).
- **Postconditions (success guarantee):** The accounts are as the Manager asked. A disabled account has no session and cannot sign in. Every act is in the audit trail with the Manager's name. At least one active Manager remains.

### Main Success Scenario

1. The Manager opens "Staff".
2. The system lists the accounts with user name, role, state (active, disabled, must change password) and the time of the last change.
3. The Manager chooses to create an account and gives user name, role and a temporary password.
4. The system creates the account as active and "must change password", and shows it in the list.
5. The Manager disables an account that is no longer needed.
6. The system disables it and ends its sessions at once.
7. The Manager resets the password of an account whose owner has lost it, giving a new temporary password.
8. The system stores it, marks the account "must change password", ends its sessions and clears its sign-in limit.
9. The Manager opens "Audit" and reads who did what and when.

### Extensions (Alternative / Exception Flows)

- 3a. The user name is taken, empty or too long, the role is not Receptionist, Manager or Housekeeping, or the temporary password breaks the rules of UC-005:
  1. The system names the field and the rule and creates nothing.
- 5a. The Manager chooses their own account, or the only active Manager:
  1. The system refuses and says that at least one active Manager must remain, and that you cannot disable yourself.
- 5b. The Manager wants an account back:
  1. The Manager enables it; the system marks it "must change password" and clears the sign-in limit.
- 7a. The Manager resets their own password:
  1. The system points them to "Change password" (UC-005), which needs the current password.
- 1a. A Receptionist or Housekeeping user asks for "Staff" or "Audit":
  1. The system tells them that the role has no access (UC-004, 6a).
- 6a. Too many changes in a minute (more than 30):
  1. The system refuses with "try again later" and changes nothing.

### Special Requirements / Business Rules

| Step | Rule |
| --- | --- |
| 3, 7 | A temporary password follows the rules of UC-005 and is shown once, to the Manager who typed it; it is never stored in clear text or written to a log |
| 5 | A Manager cannot disable their own account, and the last active Manager cannot be disabled (ADR-0017) |
| 6, 8 | Sessions end because the account's security stamp rises; the API checks the stamp and the active flag on every staff call (ADR-0017) |
| 9 | The audit trail is append-only and holds no password, hash or token; only a Manager reads it (ADR-0017) |
| all | The Manager owns "staff accounts" in the Access Control Matrix; no other role holds the permission |

### Open Issues

- A change of role of an existing account is not in this use case (open question for S01).
- An account is never deleted, only disabled, so the audit trail keeps a name for every actor.

---

[UCD-001]: ../../use-case-diagram.md
[US-001]: ../../user-stories.md
[SA-001]: ../../stakeholder-analysis.md
[UC-004]: ../uc-004/uc.md
[UC-005]: ../uc-005/uc.md
[ADR-0005]: ../../adr/adr-0005-backoffice-login.md
[ea8a47f]: https://github.com/NF-Hotel/NFHotel/commit/ea8a47fc392a25ce295dae854d1727f8c8934a95
