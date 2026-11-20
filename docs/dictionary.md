# Domain Dictionary (PO and IT terms)

## Metadata
| Key | Value |
| --- | --- |
| ID | DICT-001 |
| CrossReference | [BC-001], [SA-001], [UC-007], [UC-010], [OC-001], [UC-011], [OC-002] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-19 | Deprecated | mikemike | AI as stand-in for S01 | Added Made on the public site (`MadeOnPublicSite`), which the limit per email of the public site counts (MIL-087, ADR-0051)<br>Go in RC-427; accepted by the AI as stand-in for S01 in autonomous mode | [a574bf6] |
| 2026-11-20 | Accepted | mikemike | AI as stand-in for S01 | Added Asked at (`CreatedAt`), Booking request (`BookingRequest`), Mailbox (`Mailbox`), Number of guests (`Guests`) and Verify (`Verify`); Made on the public site is counted per mailbox, and a Guest sign-in is also opened by the verification of a booking (MIL-088, UC-011, ADR-0052, ADR-0053)<br>Go in RC-435; accepted by the AI as stand-in for S01 in autonomous mode | [739a25b] |

---

## Purpose and Scope

Maps each Product Owner (PO) term of the hotel back office and of the booking on the website (bookings, booking requests, rooms and guests) to its professional IT term, the name used in the code and in the design artifacts. The terms come from the rule ledger (`docs/refactor/BusinessRules.md`), the analysis notes and the entities in `src/NFHotel.Domain`. PO language:
English (en), domain it, as set in the registry.

## Dictionary

| PO term | Language | IT term | Definition | Used as PO term in | Used as IT term in |
| --- | --- | --- | --- | --- | --- |
| Active | en | IsActive | Whether a staff member may still sign in. | DM | OC, SD, DCD, ERD |
| Actual check-out date | en | CheckOutDate | The date the guest really left, set only when the guest leaves before the departure date. | DM, UC | OC, SD, DCD, ERD |
| Arrival date | en | StartDate | The first night of the booking. | DM, UC | OC, SD, DCD, ERD |
| Asked at | en | CreatedAt | The moment the guest asked for a booking request; its link lapses 30 minutes later. | DM | OC, DCD |
| Audit event | en | StaffAuditEvent | A record of an act on a staff account or at the sign-in: who, to whom, when, what and the result. | DM, UC | OC, SD, DCD, ERD |
| Bookability | en | RoomStatus | Whether the room may be let: available, out of service or maintenance. | DM, UC | OC, SD, DCD, ERD |
| Booking | en | Booking | A guest's claim on one room for the nights from an arrival date to a departure date. | DM, UC, US | OC, SD, DCD, ERD |
| Booking reference | en | BookingReference | The random reference by which staff and guest name one booking; without the email of the booking it opens nothing. | DM, UC, US | OC, SD, DCD, ERD |
| Booking request | en | BookingRequest | A booking that a guest asked for on the website and has not yet verified through the link mailed to the email; it holds no room, and it lapses 30 minutes after it was asked. | DM, UC | OC, SD, DCD |
| Booking status | en | BookingStatus | Where the booking is in its life: pending, confirmed, checked in, checked out or cancelled. | DM, UC | OC, SD, DCD, ERD |
| Capacity | en | Capacity | The number of guests the room is made for. | DM, UC | OC, SD, DCD, ERD |
| Check-in time | en | CheckInTime | The moment the guest was checked in. | DM, UC | OC, SD, DCD, ERD |
| Check-out time | en | CheckOutTime | The moment the guest was checked out. | DM, UC | OC, SD, DCD, ERD |
| Cleaning state | en | HousekeepingStatus | Where the room is in cleaning and service: clean, daily cleaning due, departure cleaning due, cleaning in progress, service required or service in progress. | DM, UC | OC, SD, DCD, ERD |
| Country | en | Country | The country the guest lives in. | DM, UC | OC, SD, DCD, ERD |
| Departure date | en | EndDate | The date the guest leaves, which is not a night of the booking. | DM, UC | OC, SD, DCD, ERD |
| Effective end date | en | EffectiveEndDate | The actual check-out date if there is one, otherwise the departure date. | DM, UC | OC, SD, DCD, ERD |
| Email | en | Email | The address at which the guest can be written to. | DM, UC | OC, SD, DCD, ERD |
| First name | en | FirstName | The given name of the guest. | DM, UC | OC, SD, DCD, ERD |
| Floor | en | Floor | The floor of the hotel the room is on. | DM, UC | OC, SD, DCD, ERD |
| Guest | en | Guest | A person who stays, or will stay, in the hotel and for whom a booking is made. | DM, UC, US | OC, SD, DCD, ERD |
| Guest sign-in | en | GuestSession | A guest's access to one booking, opened with the booking reference and the email of the booking, or by the verification of a booking made on the website; it ends at the sign-out or two hours after it was opened. | UC | OC, SD |
| Last name | en | LastName | The family name of the guest. | DM, UC | OC, SD, DCD, ERD |
| Made on the public site | en | MadeOnPublicSite | Whether the guest made the booking on the public site, as against hotel staff; only such bookings, while they are coming, count in the limit of coming bookings per mailbox of the public site. | DM, UC | OC, SD, DCD, ERD |
| Mailbox | en | Mailbox | The mailbox that an email reaches, which several spellings of the email share: the email without letter case and without a `+` and what follows it before the `@`, and for Gmail also without the dots before the `@`, in either domain of Gmail; the limits per email of the public site count by mailbox. | UC, US | OC, SD, DCD |
| Must change password | en | MustChangePassword | Whether a staff member has to choose a new password before using any other function. | DM, UC | OC, SD, DCD, ERD |
| Number of guests | en | Guests | The number of people in the party who will stay in the room, one to four on the website. | DM, UC, US | OC, SD, DCD |
| Number of nights | en | NumberOfNights | The count of nights from the arrival date to the effective end date. | DM, UC | OC, SD, DCD, ERD |
| Passport number | en | PassportNumber | The number of the guest's passport, which is personal data and is optional. | DM, UC | OC, SD, DCD, ERD |
| Password | en | PasswordHash | The secret a staff member types to sign in; only a salted hash of it is kept. | DM, UC | OC, SD, DCD, ERD |
| Phone number | en | PhoneNumber | The number at which the guest can be called. | DM, UC | OC, SD, DCD, ERD |
| Price change | en | PriceChange | A percentage added to or taken from the room rate of a room size from a first day to a last day, approved by a Manager and reverted, never deleted. | DM, UC, US | OC, SD, DCD, ERD |
| Price suggestion | en | PriceSuggestion | A price change that the system proposes from the figures of the market analysis and that takes effect only when a Manager approves it. | DM, UC, US | OC, SD, DCD, ERD |
| Pricing settings | en | PricingSettings | The values a Manager sets for the rule of the price suggestions: the surcharge, the discount, the two occupancy thresholds, the largest change, the lowest price and the room sizes that qualify for a discount. | DM, UC, US | OC, SD, DCD, ERD |
| Role | en | StaffRole | What a staff member does in the hotel: Receptionist, Manager or Housekeeping. | DM, UC, US | OC, SD, DCD, ERD |
| Room | en | Room | A lettable room of the hotel that a booking can claim. | DM, UC, US | OC, SD, DCD, ERD |
| Room number | en | RoomNumber | The number that identifies the room to staff and guests, and is used by no other room. | DM, UC | OC, SD, DCD, ERD |
| Room rate | en | RoomRate | The nightly price of a room size that a Manager sets, before any price change. | DM, UC, US | OC, SD, DCD, ERD |
| Room size | en | RoomSize | The kind of room: single, double or suite. | DM, UC | OC, SD, DCD, ERD |
| Season | en | Season | How busy a calendar month is by its occupancy in the market analysis: high, low or normal. | DM, UC, US | OC, SD, DCD, ERD |
| Security stamp | en | SecurityStamp | A number on the account that rises when the password changes or the account is disabled or enabled; a session made with an older number has ended. | DM, UC | OC, SD, DCD, ERD |
| Staff member | en | StaffMember | A person of the hotel who signs in to the back office with a user name and a password. | DM, UC, US | OC, SD, DCD, ERD |
| User name | en | UserName | The name a staff member signs in with, the same in any case. | DM, UC | OC, SD, DCD, ERD |
| Verify | en | Verify | What the guest does through the link mailed to the email of a booking request, within 30 minutes: it books the room, and shows that the guest reads that mailbox. | UC, US | OC, SD, DCD |

## Rules

- The Domain Model, use cases and user stories use the PO term; the Operation
  Contract, Sequence Diagram, Design Class Diagram and ERD use the IT term.
- One IT term per PO term and one PO term per IT term; no synonyms.

---

[BC-001]: ./business-case.md
[SA-001]: ./stakeholder-analysis.md
[UC-007]: ./use-cases/uc-007/uc.md
[UC-010]: ./use-cases/uc-010/uc.md
[OC-001]: ./use-cases/uc-010/oc.md
[UC-011]: ./use-cases/uc-011/uc.md
[OC-002]: ./use-cases/uc-011/oc.md
[a574bf6]: https://github.com/NF-Hotel/NFHotel/commit/a574bf6192840701214b20e4827c0748fb00c251
[739a25b]: https://github.com/NF-Hotel/NFHotel/commit/739a25b00890c77fe75ba0bb02fcd64a00548089
