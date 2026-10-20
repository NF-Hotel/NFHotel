# UC-004 Sign In To The Back Office

## Metadata
| Key | Value |
| --- | --- |
| ID | UC-004 |
| CrossReference | [UCD-001], [US-001], [SA-001], [ADR-0005] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-17 | Proposed | Claude (AI) | AI as stand-in for S01 | Initial version (MIL-016) | [1593b34] |
| 2026-10-17 | Deprecated | Claude (AI) | AI as stand-in for S01 | Accepted after RC-070 Go | [1593b34] |
| 2026-10-20 | Accepted | mikemike | AI as stand-in for S01 | Open issues closed by UC-005, UC-006 and ADR-0015 (MIL-030) (accepted after RC-136 Go) | pending |

---

**Format:** Fully Dressed

## Fully Dressed

- **Scope:** NFHotel platform, hotel back office
- **Level:** user-goal
- **Primary Actor:** Hotel staff
- **Stakeholders and Interests:**
  - S05 — Hotel staff: signs in once per shift and sees only the pages that the work needs.
  - S01 — Team 2: guest data and the passport number are not open to anyone who finds the address; no password or key is stored in clear text or in git.
  - S03 — Proxmox VE administrator: the signing key and the first password are secrets of the server, set by the deploy.
- **Preconditions:** The staff member has an active account with a user name, a password and a role (Receptionist, Manager or Housekeeping).
- **Postconditions (success guarantee):** The staff member has a session that lasts one shift, and every back office function is checked against the role of that session. Nobody without a session can read or change guests, bookings or rooms.

### Main Success Scenario

1. Hotel staff ask for a function of the back office.
2. The system asks them to sign in, remembering what they asked for.
3. Hotel staff enter user name and password.
4. The system checks the password and starts a session with the role of the staff member.
5. The system continues with what they asked for, or with the start of their role.
6. Hotel staff work with the functions that their role holds; the system offers only those.
7. Hotel staff sign out at the end of the shift, and the session ends.

### Extensions (Alternative / Exception Flows)

- 3a. The user name or the password is wrong, or the account is inactive:
  1. The system says that the user name or the password is wrong, without saying which, and starts no session.
- 3b. The user name has failed five times in fifteen minutes:
  1. The system refuses further attempts for that user name until the fifteen minutes have passed, even with the right password.
- 6a. Hotel staff ask for a function that their role does not hold, for example Housekeeping asks for the guests:
  1. The system tells them that the role has no access and offers the start of the role. Nothing of the refused function is shown.
- 6b. The session has ended while staff were working:
  1. The system asks them to sign in again and keeps nothing of the call that failed.
- 1a. A guest uses the public booking:
  1. The system serves it without a sign-in.

### Special Requirements / Business Rules

| Step | Rule |
| --- | --- |
| 4 | The password is never stored; only a salted hash is. The check takes the same time for an unknown user as for a wrong password (ADR-0005) |
| 4 | A session lasts 8 hours, one shift (ADR-0005) |
| 3b | Five failures per user name in a window of fifteen minutes; the window does not slide (ADR-0005) |
| 6 | Receptionist: bookings, guests, rooms to read. Manager: everything, including rooms, cleaning status and revenue. Housekeeping: rooms and their cleaning status only. The passport number is part of the guest, so only Receptionist and Manager see it (Access-Control-Matrix.md, D-04) |
| 6 | The role is checked where the data is served and again where the function is offered; the first is the authority (ADR-0005) |

### Open Issues

- The network carries the password in clear text until the public port has TLS (ADR-0005); the sign-in is not to be used over an untrusted network until then.
- A stolen session works until it expires; there is no revocation list. (Closed by ADR-0015: the security stamp ends old sessions.)
- The Guest role (a guest sees their own booking) is a later milestone; the booking receipt page is open by its number until then.
- Staff accounts are made by the Manager's command line, not on a screen. (Closed by UC-006.)

---

[UCD-001]: ../../use-case-diagram.md
[US-001]: ../../user-stories.md
[SA-001]: ../../stakeholder-analysis.md
[ADR-0005]: ../../adr/adr-0005-backoffice-login.md
[1593b34]: https://github.com/NF-Hotel/NFHotel/commit/1593b34bb7cc020a8564f1030f49e2c395803ad4
