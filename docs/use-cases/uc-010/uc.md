# UC-010 View and Change Own Booking

## Metadata
| Key | Value |
| --- | --- |
| ID | UC-010 |
| CrossReference | [UCD-001], [US-001], [SA-001], [UC-001], [UC-004], [MIL-018], [MIL-079], [ADR-0005], [ADR-0047], [SSD-001], [MIL-085], [ADR-0049], [MIL-086], [ADR-0050], [MIL-087], [ADR-0051], [UC-011], [MIL-088], [ADR-0052] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-19 | Deprecated | mikemike | AI as stand-in for S01 | The failed attempts from one address are counted under the address from which they reached the site, which no caller can set (MIL-087, ADR-0051)<br>Go in RC-427; accepted by the AI as stand-in for S01 in autonomous mode | [a574bf6] |
| 2026-11-20 | Accepted | mikemike | AI as stand-in for S01 | Extension 1a and its rule: a guest who verifies a booking made on the website through the link in the mail is signed in to it by the verification; the offer after the booking holds while the verification by email is switched off (MIL-088, UC-011, ADR-0052, ADR-0053)<br>Go in RC-435; accepted by the AI as stand-in for S01 in autonomous mode | pending |

---

**Format:** Fully Dressed

## Fully Dressed

- **Scope:** NFHotel platform, the website of the hotel
- **Level:** user-goal
- **Primary Actor:** Guest (the person a booking was made for, who holds its booking reference and the email it was made with)
- **Stakeholders and Interests:**
  - S01 — Team 2, the owner of the hotel: a guest serves the own booking without the staff (objective 21 of BC-001), and no guest reaches another guest's booking, guest details or passport number.
  - S03 — Proxmox VE administrator: the sign-in adds no service, no port and no secret to the server; it uses the signing key of the staff sign-in, and its limits on failed attempts are kept in the memory of the API.
  - S04 — Valdamar: what a guest may do can be checked in one table, the Access Control Matrix, against every role and permission.
  - S05 — Hotel staff: a change that a guest makes follows the same booking rules as theirs, and a guest never reaches the back office.
  - S06 — Hotel guests: the own booking, and nobody else's, without an account or a password, and their personal data kept private.
- **Preconditions:** The guest has a booking that is not cancelled and holds its booking reference and the email it was made with.
- **Postconditions (success guarantee):** The guest has seen the own booking and nothing of any other booking or guest. A change of dates or a cancellation is saved under the booking rules of UC-001. The guest's sign-in ends at the sign-out or two hours after the sign-in, whichever comes first, and a staff sign-in in the same browser stays as it was. A refused sign-in tells nothing about whether the reference exists.

### Main Success Scenario

1. The guest asks to see the booking and gives the booking reference and the email of the booking.
2. The system checks that both belong to one booking that is not cancelled, signs the guest in to that booking, and shows it: the booking reference, the dates, the nights, the room type, the status and the payment, with the changes that the booking allows now.
3. The guest may give new dates for the stay.
4. The system checks the new dates against the booking rules, keeps the room, saves the dates and shows the booking with them.
5. The guest may cancel the booking.
6. The system cancels the booking and shows it as cancelled.
7. The guest signs out.
8. The system ends the guest's sign-in.

### Extensions (Alternative / Exception Flows)

- 1a. The guest has just made the booking on the website ([UC-011]):
  1. With the verification by email, the system signed the guest in to the booking when the guest verified it, and shows the booking at once (step 2). While the verification by email is switched off, the system offers to manage the booking at once and signs the guest in to it without asking for the reference and the email (step 2).
- 2a. The reference is unknown or cannot be read, a booking number of the old kind included, the email is not the one of the booking, or the booking is cancelled:
  1. The system says that the reference and the email do not match a booking, in the same words in every case, and signs nobody in. The guest may try again (step 1).
- 2b. Five attempts for the booking reference have failed within 15 minutes, or twenty attempts from the guest's address:
  1. The system refuses every further attempt for that reference, or from that address, until the 15 minutes have passed, says so in words that name no booking, and signs nobody in.
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
| 1, 2 | A guest signs in with the booking reference and the email of the booking; there is no guest account and no password (ADR-0047, decision 1; ADR-0049, decision 2). The reference is a random universally unique identifier (UUID) of version 4, read in its usual form of 36 characters in any letter case; the email is compared in its normal form (trimmed, Unicode normal form C) and without letter case (ADR-0049, decision 1; MIL-079; ADR-0050, decision 4). Neither may be longer than a booking reference or the email of a booking can be (MIL-086) |
| 2a, *a | An unknown or unreadable reference, a booking number of the old kind, a wrong email and a cancelled booking get the same refusal in the same words; an ended or invalid sign-in is refused in the same way and shows nothing of the booking (ADR-0047, decisions 3 and 7; ADR-0049, decisions 2 and 5) |
| 2b | After five failed attempts for one reference within 15 minutes, the next attempt for that reference is refused until the 15 minutes have passed, and after twenty failed attempts from one address within 15 minutes, so is the next attempt from that address; the refusal names no booking (ADR-0049, decision 6). The address is the one from which the attempt reached the site, which no caller can set (ADR-0051, decision 4) |
| 1a | A guest who has just booked on the website is signed in without giving the booking reference and the email: by the verification of the booking through the link in the mail ([UC-011], step 9), or, while the verification by email is switched off, through the offer right after the booking. Either sign-in lasts two hours, as in step 2 (ADR-0047, decision 2; ADR-0052, decisions 5 and 8) |
| 2 | A sign-in lasts two hours and is not extended (ADR-0047, decision 2). The booking is shown as its receipt: the reference, the dates, the nights, the room type and the status, and nothing of its guest (ADR-0049, decision 7) |
| all | The role Guest holds `Bookings.Create`, `Bookings.EditOwn` and `Guests.ViewOwn` and nothing else: the guest sees and changes only the booking signed in to, no request of the guest names a booking after the sign-in, and no passport number is ever shown (ADR-0047, decisions 4 and 5; ADR-0049, decision 3; the Access Control Matrix) |
| 3, 4 | New dates keep the room and follow the checks of a new booking on the public site: valid dates, a stay that ends at most five years after today and has at most 30 nights (BR-N-08, BR-C-07), and a free room for every night; the booking does not block itself (UC-001, 2 and 5b). The guest is offered only the days that the rules allow, and a move that breaks a rule is refused with the text of the rule (MIL-086, ADR-0050) |
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
[MIL-085]: ../../milestones/mil-085-a-booking-reference-that-cannot-be-guessed.md
[ADR-0049]: ../../adr/adr-0049-booking-reference.md
[MIL-086]: ../../milestones/mil-086-booking-input-checked-by-one-set-of-rules.md
[ADR-0050]: ../../adr/adr-0050-where-the-rules-of-the-booking-input-live.md
[MIL-087]: ../../milestones/mil-087-limits-on-what-one-sender-can-ask.md
[ADR-0051]: ../../adr/adr-0051-limits-of-the-booking-flow-and-the-caller-address.md
[UC-011]: ../uc-011/uc.md
[MIL-088]: ../../milestones/mil-088-a-booking-verified-by-email.md
[ADR-0052]: ../../adr/adr-0052-a-booking-takes-a-room-once-its-email-is-verified.md
[a574bf6]: https://github.com/NF-Hotel/NFHotel/commit/a574bf6192840701214b20e4827c0748fb00c251
