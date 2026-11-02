# UC-007 Approve a Price Suggestion

## Metadata
| Key | Value |
| --- | --- |
| ID | UC-007 |
| CrossReference | [UCD-001], [US-001], [SA-001], [UC-004], [UC-006], [MIL-051] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-01 | Accepted | Claude (AI) | S01 | Initial version (MIL-051) ; accepted by S01 in chat on 2026-11-01 after RC-259 Go-with-conditions | [3a17eee] |

---

**Format:** Fully Dressed

## Fully Dressed

- **Scope:** NFHotel platform, hotel back office
- **Level:** user-goal
- **Primary Actor:** Manager (a member of hotel staff with the role Manager)
- **Stakeholders and Interests:**
  - S01 — Team 2, the owner of the hotel: a surcharge in high season and a discount on single rooms in low season are applied on time, by a rule the owner can read, and no price changes without a Manager.
  - S05 — Hotel staff: the price on the room page is the price in force on the day, and a wrong change can be undone.
  - S04 — Valdamar: each approval can be traced to a person, a time and the figures it rests on.
- **Preconditions:** The Manager is signed in (UC-004). A market analysis report has completed and holds the occupancy by month of at least twelve months. A room rate is set for each room size.
- **Postconditions (success guarantee):** A price change exists for the dates the Manager approved, or the Manager reverted one; the price of each room size on each day is the room rate with the price changes in force on that day. The approval or the undo is in the audit trail with the Manager's name and the old and the new values. No price changed without an approval.

### Main Success Scenario

1. The Manager opens "Price suggestions".
2. The system lists the open price suggestions. Each shows the room size, the season, the first and the last day, the percentage, the old and the new nightly price, and the occupancy and the threshold it rests on. When the report has an AI text for the analysis, the system shows it beside the list, labelled as an explanation.
3. The Manager reads a price suggestion and approves it. The Manager may change the first and the last day before approving.
4. The system creates the price change for those days and shows it as in force or coming.
5. The Manager sees on the room page that the price of the room size is the new one on the days of the price change.
6. The Manager reverts a price change that is no longer wanted.
7. The system ends the price change that day, or withdraws it if it has not begun, and keeps it in the list as reverted.

### Extensions (Alternative / Exception Flows)

- 2a. The report has less than twelve months of occupancy, or no report has completed:
  1. The system says that there is no basis for a price suggestion and lists none.
- 2b. A price change for the same room size already covers some of the days:
  1. The system does not list that price suggestion for those days.
- 3a. The Manager approves a price suggestion a second time (a repeated click):
  1. The system creates no second price change and shows the one that exists.
- 3b. The last day is before the first day, or the days overlap a price change in force for the room size:
  1. The system names the problem and creates nothing.
- 3c. The new price would go above the largest change or below the lowest price that the Manager has set:
  1. The system shows the price cut to the limit and says so in the price suggestion.
- 1a. A Receptionist or Housekeeping user asks for "Price suggestions":
  1. The system tells them that the role has no access (UC-004, 6a).
- 4a. Too many changes in a minute (more than 30):
  1. The system refuses with "try again later" and changes nothing.

### Special Requirements / Business Rules

| Step | Rule |
| --- | --- |
| 2 | A month is high season when its occupancy is at or above the high threshold (80 percent by default) and low season when it is at or below the low threshold (40 percent by default); a month without a figure is normal; the occupancy of a calendar month is the average over the years of the report period |
| 2 | In high season the suggestion is a surcharge of 20 percent for every room size; in low season it is a discount of 15 percent for the room sizes that qualify, which are single rooms by default; both are settings of the Manager |
| 2 | The rule reads only the figures of the report; the AI text never sets a season or a price |
| 3, 4 | A price change is never larger than the largest change (30 percent by default) and never takes the price below the lowest price; two price changes in force for one room size never overlap |
| 4, 7 | A price change applies to bookings made after it is in force; a booking already made keeps its price (to be confirmed by S01) |
| 7 | A price change is never deleted, only reverted, so the list shows what was in force |
| 4, 7 | Every approval and undo, every change of a room rate and every change of the settings is in the audit trail (UC-006 step 9) |
| all | The Manager owns "prices" in the Access Control Matrix; no other role holds the permission |

### Open Issues

- Whether a price change touches bookings already made, how many years of the report the season is read from (proposed: the period of the report, which the Manager chooses, at least twelve months) and the default percentages are for S01 to confirm in MIL-051 task 2.
- Prices per night for suites and double rooms in low season are not suggested; a later story may add them.

---

[UCD-001]: ../../use-case-diagram.md
[US-001]: ../../user-stories.md
[SA-001]: ../../stakeholder-analysis.md
[UC-004]: ../uc-004/uc.md
[UC-006]: ../uc-006/uc.md
[MIL-051]: ../../milestones/mil-051-room-price-suggestions.md
[3a17eee]: https://github.com/NF-Hotel/NFHotel/commit/3a17eeed8127f7dab532e5afb78b09e005ec3118
