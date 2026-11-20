# UC-011 Book a Room and Verify It by Email

## Metadata
| Key | Value |
| --- | --- |
| ID | UC-011 |
| CrossReference | [UCD-001], [US-001], [SA-001], [DM-004], [SSD-002], [UC-001], [UC-010], [BC-001], [MIL-088], [ADR-0047], [ADR-0049], [ADR-0050], [ADR-0051], [ADR-0052], [ADR-0053] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-20 | Accepted | mikemike | AI as stand-in for S01 | Initial version; task 3 of MIL-088, written by the AI: a booking on the website is asked for, verified through a link mailed to its email and only then booked (ADR-0052, ADR-0053)<br>Go in RC-435; accepted by the AI as stand-in for S01 in autonomous mode | pending |

---

**Format:** Fully Dressed

## Fully Dressed

- **Scope:** NFHotel platform, the website of the hotel
- **Level:** user-goal
- **Primary Actor:** Guest (a person who wants to stay at the hotel, books on the website without an account, and reads the mail of the email given)
- **Stakeholders and Interests:**
  - S06 — Hotel guests: a room booked on the website in a few minutes, without an account or a password, with a mail that any mail program can read; no booking made in their email without them, and their personal data kept only as long as the booking needs it.
  - S01 — Team 2, the owner of the hotel: a room is taken only by a booking that somebody stands behind, so nobody can make the hotel look full with bookings that nobody verifies, and the hotel's mail goes to nobody who did not ask for it (objective 43 of BC-001).
  - S05 — Hotel staff: a verified booking reaches them as a pending booking with its booking reference, which they confirm as any other booking; a request that nobody verified never reaches them and holds no room.
  - S03 — Proxmox VE administrator: the mails leave the server by one known way, to the mail relay, and the password of the relay is kept as the other secrets are.
  - S04 — Valdamar: every rule and limit of the flow can be checked in the rule ledger and traced to its decision.
- **Preconditions:** The guest can read the mail of the email given. The verification by email is switched on (extension *a covers the hotel while it is off).
- **Postconditions (success guarantee):** The booking is stored as pending with its booking reference, in a free room of the chosen size that sleeps the party, for the nights asked for, together with the guest's details, and the guest is signed in to it. The booking request is gone. No room was held before the guest verified the booking. Whatever happens, a booking request holds no room, and one that nobody verifies is deleted, with its personal data, within ten minutes after its link lapsed.

### Main Success Scenario

1. The guest gives the arrival date, the departure date and the number of guests.
2. The system shows the room sizes that are free for every night of the stay and sleep the party.
3. The guest chooses a room size, gives the guest's details (first name, last name, email, phone number, country and, if the guest wishes, the passport number) and asks for the booking.
4. The system checks the answers by the rules of a booking on the website, keeps them as a booking request that holds no room, mails a link to the email, and tells the guest to open the mail and until when the link works.
5. The guest opens the link in the mail.
6. The system asks the guest to verify the booking, and changes nothing until the guest does.
7. The guest verifies the booking.
8. The system takes the booking request and books a free room of the chosen size for the stay: the booking is pending, has a booking reference, and is stored together with the guest.
9. The system signs the guest in to the booking, shows it with its booking reference (the own booking of UC-010), and mails the booking reference and the dates to the email.

### Extensions (Alternative / Exception Flows)

- 2a. The dates or the party break a rule of the website, or no room size that sleeps the party is free for every night:
  1. The system says why and shows no room size. The guest may give other dates or another party (step 1).
- 2b. More than 60 searches for free rooms came from the guest's address within a minute:
  1. The system refuses the search with the text of a refusal by a limit and shows no room size. The guest searches again when the minute has passed.
- 4a. An answer breaks a rule of the booking on the website:
  1. The system names every broken rule at its answer, keeps nothing and sends no mail. The guest corrects the answers (step 3).
- 4b. The last free room of the size went to another booking since step 2:
  1. The system says that the room size is no longer free for the dates, keeps nothing, sends no mail and shows the room sizes that are free now (step 2).
- 4c. The mailbox of the email already has three coming bookings made on the public site:
  1. The system refuses the request with the text of a refusal by a limit, keeps nothing and sends no mail.
- 4d. Five requests for a booking came from the guest's address within the hour, or twenty within the day:
  1. The system refuses the request with the text of a refusal by a limit, keeps nothing and sends no mail. The guest may ask again when the time has passed, or call the hotel.
- 4e. Three mails with a link went to the mailbox of the email within the hour, or five within the day:
  1. The system sends no new mail and keeps the earlier request as it was, and tells the guest to open the newest mail from the hotel and use its link, because only that one works.
- 4f. The hotel has sent 400 mails within the last 24 hours:
  1. The system keeps nothing new, sends no mail, and tells the guest that booking requests are paused for now and to try again later or to call the hotel.
- 4g. The mail relay does not take the mail:
  1. The system tells the guest that the mail with the link could not be sent, that nothing is booked, and to try again later. It keeps no request of this asking, and an earlier request of the email that this asking replaced stays replaced; the mail counts against no limit of the mailbox.
- 5a. The mail does not arrive, the guest wants it again, or the guest typed a wrong email:
  1. The guest asks for the mail again with the same answers, or goes back, corrects the email and asks again (step 3).
  2. The system handles it as step 4: the new request replaces the earlier request of the same mailbox, the link of the earlier mail stops working, and the limits of 4e and 4f hold. The system tells the guest that a new link was sent and that the link of the earlier mail no longer works.
- 5b. Nobody opens the link, because the email is a stranger's and its owner does nothing, or because the guest gives up:
  1. The mail says that a receiver who did not ask needs to do nothing. The link lapses 30 minutes after the request, and the system deletes the request, with its personal data, within the next ten minutes. No room was held, and no booking and no guest are stored.
- 7a. The link has lapsed, has been used, or is not a link of the hotel:
  1. The system says that the link does not work any more and that the guest can ask for the booking again, in the same words in every case, and books nothing (step 1).
- 7b. The last free room of the size went to another booking since the request:
  1. The system says that the room size is no longer free for the dates and books nothing. The request is gone; the guest may ask again for other dates (step 1).
- 7c. The mailbox of the email has reached three coming bookings made on the public site since the request:
  1. The system refuses with the text of a refusal by a limit and books nothing. The request is gone.
- 7d. The guest verifies twice, or verifies one link in two places at once:
  1. The system books once; every other verification gets the answer of 7a.
- 9a. The mail with the booking reference cannot be sent, because the mail relay does not take it, 400 mails have been sent within the last 24 hours, or the sending fails in another way:
  1. The booking stands. The system shows it with its booking reference, which finds the booking again (UC-010), and sends no mail.
- *a. The verification by email is switched off:
  1. In step 4 the system books a free room of the chosen size at once, without a request and without a mail: the booking is pending, has a booking reference and is stored together with the guest. The system shows the booking with its booking reference and offers to sign the guest in to it (UC-010). Steps 5 to 9 do not happen.
  2. A link of a request made while the verification was on gets the answer of 7a.
- *b. The verification by email was switched on or off after the guest started, so the guest asks to book in the way that no longer holds:
  1. The system refuses, says that the way to book has changed and asks the guest to start again (step 1); nothing is kept or booked.

### Special Requirements / Business Rules

| Step | Rule |
| --- | --- |
| 1, 2, 2a | The website offers only what a booking on the website may ask for: an arrival that is not before today, at most 30 nights, a stay that ends at most five years after today, and one to four guests (BR-C-07, BR-N-08; ADR-0050) |
| 2b | A search for free rooms is limited to 60 in a minute per address of the caller: the address from which the search reached the site, which the caller cannot set (the table "Limits of the booking flow" of the ledger; ADR-0051, decisions 2 and 4) |
| 3, 4, 4a | The answers are checked by the table "Validation rules of the booking flow" of the ledger, the same rules as for a booking made at once, before anything is kept and before any mail is built (ADR-0050; ADR-0052, decisions 1 and 4) |
| 4 | A booking request holds no room and stores no guest. It keeps the dates, the room size, the number of guests and the guest's details; the passport number is kept encrypted, as a guest's is (ADR-0052, decisions 1 and 7) |
| 4c, 4d | A request counts as a booking request in the limits per address: 5 in an hour and 20 in a day. A mailbox may have at most 3 coming bookings made on the public site (the table "Limits of the booking flow" of the ledger; ADR-0051, decisions 2 and 3; ADR-0052, decision 1) |
| 4, 4c, 4e, 5a | The limits per email count by mailbox: the email without letter case and without a `+` and what follows it before the `@`, and for Gmail also without the dots before the `@`, with `googlemail.com` counted as `gmail.com` (ADR-0052, decision 6) |
| 4e, 5a | A mailbox has at most one open booking request: a new request replaces the earlier one, and the link of the earlier mail stops working. At most 3 mails with a link go to one mailbox in an hour and 5 in a day; a mail that the relay did not take is not counted (ADR-0052, decision 6) |
| 4, 5b | The mail is plain text with a fixed subject: the dates, the room size, the number of guests, the link, the time it lapses, and one sentence for a receiver who did not ask. It holds no text that the sender typed, and the email only as the address it goes to (ADR-0052, decision 4) |
| 4f, 9a | At most 400 mails leave the hotel in 24 hours, the mails of every mailbox together; an alert warns at 200 (ADR-0053, decision 6) |
| 4g, 9a | When the mail relay does not take the mail with the link, the guest is told and nothing is kept or booked. When the mail with the booking reference is not sent, whatever the cause, the booking stands and the guest sees it with its booking reference (ADR-0052, decision 5; ADR-0053, decision 5) |
| 5, 6, 7a | The link works once and for 30 minutes from the request. Its secret part cannot be guessed, and the hotel keeps it only in a form that does not work as a link. The link always leads to the hotel's own website, never to an address taken from the request (ADR-0052, decision 2). Opening the link changes nothing: only the guest's act of verifying books, so a mail program that opens the link books nothing. An unknown link, a used one and a lapsed one get the same answer (ADR-0052, decision 3) |
| 7, 7b, 7d, 8 | The booking request is taken once, whatever follows: of two verifications of one link one books, and a verification that books nothing leaves no request behind. The bookings of the website choose their rooms one after the other, so of 20 verifications at once for the last room of a size exactly one gets it and the others are told that the room is gone. When a booking of hotel staff takes the chosen room at the last moment, the booking moves on to the next free room of the size that sleeps the party, up to five rooms (ADR-0052, decision 5) |
| 7c, 8 | The limit of 3 coming bookings per mailbox is checked again when the guest verifies (ADR-0052, decision 5) |
| 8 | The guest and the booking are stored together or not at all. The booking is pending, has its booking reference and is marked as made on the public site, and hotel staff confirm it as any other booking (UC-001; ADR-0049; ADR-0051, decision 3; ADR-0052, decision 5) |
| 9 | The guest is signed in only after verifying the booking; the sign-in lasts two hours, as the guest sign-in of UC-010 (ADR-0047; ADR-0052, decision 5). The second mail carries the booking reference and the dates (ADR-0052, decision 5) |
| 5b | A request that nobody verifies is deleted, with its personal data, at the latest ten minutes after its link lapsed (ADR-0052, decision 7) |
| *a | The verification by email is switched on where the hotel has a mail relay. With it off, a booking on the website is made at once under the same rules and limits, and its guest and booking are stored together too (MIL-088, decision 10; ADR-0052, decision 8) |
| *b | With the verification on, a booking at once is refused, so nobody books around the verification; with it off, a booking request is refused (ADR-0052, decision 8) |
| all | Hotel staff book as before, without a request and without a mail. A booking request is not a booking: hotel staff do not see it, and it counts in no report (UC-001; ADR-0052, decision 9) |

### Open Issues

- The website of production has no mail relay yet: until S01 has set one (task 9 of MIL-088), the verification is switched off there, and the website books at once (*a) behind the limits per address and per mailbox.
- Somebody who knows the email of a guest can replace the guest's open request, whose link then stops working, and can use up the five mails of the mailbox for a day; the guest can ask again the next day or call the hotel (ADR-0052, Consequences).
- A mail that arrives later than 30 minutes, or lands among unwanted mail, costs the booking; the guest asks again within the limits of 4e (ADR-0052, Consequences).

---

[UCD-001]: ../../use-case-diagram.md
[US-001]: ../../user-stories.md
[SA-001]: ../../stakeholder-analysis.md
[DM-004]: ../../domain-model.md
[SSD-002]: ./ssd.md
[UC-001]: ../uc-001/uc.md
[UC-010]: ../uc-010/uc.md
[BC-001]: ../../business-case.md
[MIL-088]: ../../milestones/mil-088-a-booking-verified-by-email.md
[ADR-0047]: ../../adr/adr-0047-guest-login.md
[ADR-0049]: ../../adr/adr-0049-booking-reference.md
[ADR-0050]: ../../adr/adr-0050-where-the-rules-of-the-booking-input-live.md
[ADR-0051]: ../../adr/adr-0051-limits-of-the-booking-flow-and-the-caller-address.md
[ADR-0052]: ../../adr/adr-0052-a-booking-takes-a-room-once-its-email-is-verified.md
[ADR-0053]: ../../adr/adr-0053-mail-from-the-application-through-a-relay.md
