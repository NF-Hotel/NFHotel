# UC-002 Manage Rooms

## Metadata
| Key | Value |
| --- | --- |
| ID | UC-002 |
| CrossReference | [UCD-001], [US-001], [SA-001] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S01 (terminology confirmed by the user) | Initial version, derived from the rule ledger | pending |

---

**Format:** Fully Dressed

## Fully Dressed

- **Scope:** NFHotel platform, hotel back office
- **Level:** user-goal
- **Primary Actor:** Hotel staff
- **Stakeholders and Interests:**
  - S05 — Hotel staff: only rooms that can be let are offered, and the cleaning state is known.
  - S01 — Team 2: room rules are written down once.
- **Preconditions:** None.
- **Postconditions (success guarantee):** The room is stored with its number, floor, size, capacity, bookability and cleaning state; a room with bookings is never deleted.

### Main Success Scenario

1. Hotel staff register a room with number, floor, size and capacity.
2. The system checks the details and that no other room has the number.
3. The system stores the room as available and clean.
4. Hotel staff change the details of the room when they change.
5. Hotel staff take the room out of service or send it to maintenance, and return it when it can be let again.
6. Hotel staff record each step of cleaning and service of the room.
7. Hotel staff delete a room that is no longer used.

### Extensions (Alternative / Exception Flows)

- 2a. A detail is invalid or the number is in use:
  1. The system reports every problem at once and stores nothing.
- 7a. Hotel staff delete a room that has bookings:
  1. The system refuses and offers taking the room out of service instead.
- 7b. Hotel staff delete a room without bookings:
  1. The system deletes it.

### Special Requirements / Business Rules

| Step | Rule |
| --- | --- |
| 2 | Floor and capacity are positive; the number is not empty and is unique (BR-97 to BR-100, BR-113; uniqueness is a new rule, see Open Issues) |
| 5 | Bookability and cleaning state are separate: a room can be available and still need cleaning (B-04, BR-126) |
| 6 | Cleaning and service follow the steps clean, due, in progress, clean (F-01) |
| 7a | A room with bookings is not deleted (BR-78, BR-112) |

### Open Issues

- A unique room number is a new rule that is not in the ledger; S01 is to confirm it.
- The price of a room is not modelled; there is no price model.

---

[UCD-001]: ../../use-case-diagram.md
[US-001]: ../../user-stories.md
[SA-001]: ../../stakeholder-analysis.md
