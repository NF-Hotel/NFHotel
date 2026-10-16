# UC-003 Manage Guests

## Metadata
| Key | Value |
| --- | --- |
| ID | UC-003 |
| CrossReference | [UCD-001], [US-001], [SA-001] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S01 (terminology confirmed by the user) | Initial version, derived from the rule ledger | [a29c54a] |

---

**Format:** Fully Dressed

## Fully Dressed

- **Scope:** NFHotel platform, hotel back office
- **Level:** user-goal
- **Primary Actor:** Hotel staff
- **Stakeholders and Interests:**
  - S05 — Hotel staff: a guest is found quickly and the details are correct.
  - S01 — Team 2: the passport number is personal data and is protected (D-04).
- **Preconditions:** None.
- **Postconditions (success guarantee):** The guest is stored with valid details; a guest with bookings is never deleted; the passport number is stored only when given.

### Main Success Scenario

1. Hotel staff search for a guest by name, country or email.
2. The system lists the guests that match every search word, sorted by name.
3. When the guest is not found, hotel staff register the guest with name, email, phone number, country and, if given, passport number.
4. The system checks the details and stores the guest.
5. Hotel staff change the details of a guest when they change.
6. Hotel staff delete a guest who is no longer needed.

### Extensions (Alternative / Exception Flows)

- 3a. A required detail is missing or invalid:
  1. The system reports every problem at once and stores nothing.
- 6a. Hotel staff delete a guest with bookings:
  1. The system refuses.
- 6b. Hotel staff delete a guest without bookings:
  1. The system deletes the guest.

### Special Requirements / Business Rules

| Step | Rule |
| --- | --- |
| 2 | An empty search lists all guests; search words are combined with AND (BR-68 to BR-70, BR-116) |
| 4 | First name, last name, email, phone number and country are required; the passport number is optional (BR-52 to BR-57, BR-91 to BR-96) |
| 4 | The passport number is stored encrypted and is never shown in a list (B-09, D-04) |
| 6a | A guest with bookings is not deleted (D-05) |

### Open Issues

- Deleting a guest is a new rule that is not in the ledger; S01 is to confirm it.
- How long guest data is kept is not decided (D-05).

---

[UCD-001]: ../../use-case-diagram.md
[US-001]: ../../user-stories.md
[SA-001]: ../../stakeholder-analysis.md
[a29c54a]: https://github.com/NF-Hotel/NFHotel/commit/a29c54a0e6cc95b2b627748aa841ee24a399c15d
