# UC-010 View and Change Own Booking

## Metadata
| Key | Value |
| --- | --- |
| ID | UC-010 |
| CrossReference | [UCD-001], [US-001], [SA-001], [UC-001], [UC-004], [MIL-018], [MIL-079], [ADR-0005], [ADR-0047], [SSD-001] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-15 | Accepted | mikemike | AI as stand-in for S01 | Initial version; task 3 of MIL-018, written by the AI<br>Go in RC-389; accepted by the AI as stand-in for S01 | pending |

---

**Format:** Fully Dressed

## Fully Dressed

- **Scope:** NFHotel platform, the website of the hotel
- **Level:** user-goal
- **Primary Actor:** Guest (the person a booking was made for, who holds its booking number and the email it was made with)
- **Stakeholders and Interests:**
  - S01 — Team 2, the owner of the hotel: a guest serves the own booking without the staff (objective 21 of BC-001), and no guest reaches another guest's booking, guest details or passport number.
  - S03 — Proxmox VE administrator: the sign-in adds no service, no port and no secret to the server; it uses the key and the throttle of the staff sign-in.
  - S04 — Valdamar: what a guest may do can be checked in one table, the Access Control Matrix, against every role and permission.
  - S05 — Hotel staff: a change that a guest makes follows the same booking rules as theirs, and a guest never reaches the back office.
  - S06 — Hotel guests: the own booking, and nobody else's, without an account or a password, and their personal data kept private.
- **Preconditions:** The guest has a booking that is not cancelled and holds its booking number and the email it was made with.
- **Postconditions (success guarantee):** The guest has seen the own booking and nothing of any other booking or guest. A change of dates or a cancellation is saved under the booking rules of UC-001. The guest's sign-in ends at the sign-out or two hours after the sign-in, whichever comes first, and a staff sign-in in the same browser stays as it was. A refused sign-in tells nothing about whether the number exists.

### Main Success Scenario

1. The guest asks to see the booking and gives the booking number and the email of the booking.
2. The system checks that both belong to one booking that is not cancelled, signs the guest in to that booking, and shows it: the dates, the nights, the room type, the status and the payment, with the changes that the booking allows now.
3. The guest may give new dates for the stay.
4. The system checks the new dates against the booking rules, keeps the room, saves the dates and shows the booking with them.
5. The guest may cancel the booking.
6. The system cancels the booking and shows it as cancelled.
7. The guest signs out.
8. The system ends the guest's sign-in.

### Extensions (Alternative / Exception Flows)

- 1a. The guest has just made the booking on the website:
  1. The system offers to manage the booking at once and signs the guest in to it without asking for the number and the email (step 2).
- 2a. The number is unknown or is not a booking number, the email is not the one of the booking, or the booking is cancelled:
  1. The system says that the number and the email do not match a booking, in the same words in every case, and signs nobody in. The guest may try again (step 1).
- 2b. Five attempts for the booking number have failed within 15 minutes:
  1. The system refuses every further attempt for that number until the 15 minutes have passed, says so, and signs nobody in.
- 3a. The new dates are not valid, or the room is booked for a night of the new period:
  1. The system says why and keeps the booking as it was.
- 3b. The guest wants another room:
  1. The system offers no change of room. The guest asks the hotel, whose staff move the booking (UC-001, 5b).
- 3c, 5a. The booking can no longer be changed, because the guest has checked in or out or the booking is cancelled:
  1. The system offers neither new dates nor the cancellation, and refuses either if it is asked for.
- 7a. A member of staff is signed in to the back office in the same browser:
  1. The system ends only the guest's sign-in; the staff sign-in stays. While both are open, what the guest sees and changes is decided by the guest's sign-in alone.
- *a. At any time, the guest's sign-in has ended or is not valid:
  1. The system shows nothing of the booking and asks the guest to sign in again (step 1).
- *b. At any time, the guest asks for another booking or the details of another guest:
  1. The system refuses and shows nothing of it.
- *c. At any time, the guest asks for a function of the back office:
  1. The system asks for the staff sign-in (UC-004); the guest's sign-in gives no staff function.

### Special Requirements / Business Rules

| Step | Rule |
| --- | --- |
| 1, 2 | A guest signs in with the booking number and the email of the booking; there is no guest account and no password (ADR-0047, decision 1). The email is compared without letter case and surrounding spaces (MIL-079) |
| 2a, *a | An unknown number, a wrong email and a cancelled booking get the same refusal in the same words; an ended or invalid sign-in is refused in the same way and shows nothing of the booking (ADR-0047, decisions 3 and 7) |
| 2b | After five failed attempts for one booking number within 15 minutes, the next attempt for that number is refused until the 15 minutes have passed; failed lookups of the receipt count too (ADR-0005, MIL-079) |
| 2 | A sign-in lasts two hours and is not extended (ADR-0047, decision 2) |
| all | The role Guest holds `Bookings.Create`, `Bookings.EditOwn` and `Guests.ViewOwn` and nothing else: the guest sees and changes only the booking signed in to and its guest details, and never a passport number (ADR-0047, decisions 4 and 5; the Access Control Matrix) |
| 3, 4 | New dates keep the room and follow the checks of a new booking: valid dates and a free room for every night; the booking does not block itself (UC-001, 2 and 5b) |
| 3c, 5a | Only a pending or confirmed booking can be moved or cancelled; cancelling never deletes (UC-001, 5a and 5b; BR-11, BR-13, BR-14) |
| 7, 8, 7a | Signing out ends only the guest's sign-in (ADR-0047, decision 6) |

### Open Issues

- MIL-018 asks that the sign-in be refused for a booking cancelled more than 30 days ago. The time of a cancellation is not stored, so the system refuses the sign-in for every cancelled booking (ADR-0047, decision 7).

---

[UCD-001]: ../../use-case-diagram.md
[US-001]: ../../user-stories.md
[SA-001]: ../../stakeholder-analysis.md
[UC-001]: ../uc-001/uc.md
[UC-004]: ../uc-004/uc.md
[MIL-018]: ../../milestones/mil-018-guest-login.md
[MIL-079]: ../../milestones/mil-079-a-booking-opened-only-with-its-email.md
[ADR-0005]: ../../adr/adr-0005-backoffice-login.md
[ADR-0047]: ../../adr/adr-0047-guest-login.md
[SSD-001]: ./ssd.md
