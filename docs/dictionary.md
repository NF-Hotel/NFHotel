# Domain Dictionary (PO and IT terms)

## Metadata
| Key | Value |
| --- | --- |
| ID | DICT-001 |
| CrossReference | [BC-001], [SA-001] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Deprecated | Claude (AI) | AI as stand-in for S01 (terminology confirmed by the user) | Initial version | [c74d991] |
| 2026-10-17 | Accepted | Claude (AI) | AI as stand-in for S01 (terminology confirmed by the user) | Added Staff member, User name, Role, Password and Active (MIL-016) | [1593b34] |

---

## Purpose and Scope

Maps each Product Owner (PO) term of the hotel back office (bookings, rooms and guests) to its professional IT term, the name used in the code and in the design artifacts. The terms come from the rule ledger (`docs/refactor/BusinessRules.md`), the analysis notes and the entities in `src/NFHotel.Domain`. PO language:
English (en), domain it, as set in the registry.

## Dictionary

| PO term | Language | IT term | Definition | Used as PO term in | Used as IT term in |
| --- | --- | --- | --- | --- | --- |
| Booking | en | Booking | A guest's claim on one room for the nights from an arrival date to a departure date. | DM, UC, US | OC, SD, DCD, ERD |
| Room | en | Room | A lettable room of the hotel that a booking can claim. | DM, UC, US | OC, SD, DCD, ERD |
| Guest | en | Guest | A person who stays, or will stay, in the hotel and for whom a booking is made. | DM, UC, US | OC, SD, DCD, ERD |
| Booking number | en | BookingNumber | The number by which staff and guest refer to one booking. | DM, UC | OC, SD, DCD, ERD |
| Arrival date | en | StartDate | The first night of the booking. | DM, UC | OC, SD, DCD, ERD |
| Departure date | en | EndDate | The date the guest leaves, which is not a night of the booking. | DM, UC | OC, SD, DCD, ERD |
| Actual check-out date | en | CheckOutDate | The date the guest really left, set only when the guest leaves before the departure date. | DM, UC | OC, SD, DCD, ERD |
| Effective end date | en | EffectiveEndDate | The actual check-out date if there is one, otherwise the departure date. | DM, UC | OC, SD, DCD, ERD |
| Number of nights | en | NumberOfNights | The count of nights from the arrival date to the effective end date. | DM, UC | OC, SD, DCD, ERD |
| Check-in time | en | CheckInTime | The moment the guest was checked in. | DM, UC | OC, SD, DCD, ERD |
| Check-out time | en | CheckOutTime | The moment the guest was checked out. | DM, UC | OC, SD, DCD, ERD |
| Booking status | en | BookingStatus | Where the booking is in its life: pending, confirmed, checked in, checked out or cancelled. | DM, UC | OC, SD, DCD, ERD |
| Bookability | en | RoomStatus | Whether the room may be let: available, out of service or maintenance. | DM, UC | OC, SD, DCD, ERD |
| Cleaning state | en | HousekeepingStatus | Where the room is in cleaning and service: clean, daily cleaning due, departure cleaning due, cleaning in progress, service required or service in progress. | DM, UC | OC, SD, DCD, ERD |
| Room size | en | RoomSize | The kind of room: single, double or suite. | DM, UC | OC, SD, DCD, ERD |
| Room number | en | RoomNumber | The number that identifies the room to staff and guests, and is used by no other room. | DM, UC | OC, SD, DCD, ERD |
| Floor | en | Floor | The floor of the hotel the room is on. | DM, UC | OC, SD, DCD, ERD |
| Capacity | en | Capacity | The number of guests the room is made for. | DM, UC | OC, SD, DCD, ERD |
| First name | en | FirstName | The given name of the guest. | DM, UC | OC, SD, DCD, ERD |
| Last name | en | LastName | The family name of the guest. | DM, UC | OC, SD, DCD, ERD |
| Email | en | Email | The address at which the guest can be written to. | DM, UC | OC, SD, DCD, ERD |
| Phone number | en | PhoneNumber | The number at which the guest can be called. | DM, UC | OC, SD, DCD, ERD |
| Country | en | Country | The country the guest lives in. | DM, UC | OC, SD, DCD, ERD |
| Passport number | en | PassportNumber | The number of the guest's passport, which is personal data and is optional. | DM, UC | OC, SD, DCD, ERD |
| Staff member | en | StaffMember | A person of the hotel who signs in to the back office with a user name and a password. | DM, UC, US | OC, SD, DCD, ERD |
| User name | en | UserName | The name a staff member signs in with, the same in any case. | DM, UC | OC, SD, DCD, ERD |
| Role | en | StaffRole | What a staff member does in the hotel: Receptionist, Manager or Housekeeping. | DM, UC, US | OC, SD, DCD, ERD |
| Password | en | PasswordHash | The secret a staff member types to sign in; only a salted hash of it is kept. | DM, UC | OC, SD, DCD, ERD |
| Active | en | IsActive | Whether a staff member may still sign in. | DM | OC, SD, DCD, ERD |

## Rules

- The Domain Model, use cases and user stories use the PO term; the Operation
  Contract, Sequence Diagram, Design Class Diagram and ERD use the IT term.
- One IT term per PO term and one PO term per IT term; no synonyms.

---

[BC-001]: ./business-case.md
[SA-001]: ./stakeholder-analysis.md
[c74d991]: https://github.com/NF-Hotel/NFHotel/commit/c74d991b66d29df8c8cfb8d07848abed38ca386d
[1593b34]: https://github.com/NF-Hotel/NFHotel/commit/1593b34bb7cc020a8564f1030f49e2c395803ad4
