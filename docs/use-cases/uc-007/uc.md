# UC-007 Approve a Price Suggestion

## Metadata
| Key | Value |
| --- | --- |
| ID | UC-007 |
| CrossReference | [UCD-001], [US-001], [SA-001], [UC-004], [UC-006], [MIL-055], [MIL-071] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-01 | Deprecated | Claude (AI) | S01 | Initial version (MIL-055) ; accepted by S01 in chat on 2026-11-01 after RC-264 Go-with-conditions | [3a17eee] |
| 2026-11-14 | Accepted | mikemike | AI as stand-in for S01 | The suggestions come from the market analysis service ([MIL-071]): stakeholder, precondition, step 2, extension 2a and the rules of step 2 rewritten<br>3b names a last day before today (RC-266 N3); 3c and the rules name the largest discount (N1) and when the lowest price is checked (N5)<br>Accepted by the AI as stand-in for S01 at S01's word in chat ("Du godkender for mig"), RC-377; mode autonomous, not independent | pending |

---

**Format:** Fully Dressed

## Fully Dressed

- **Scope:** NFHotel platform, hotel back office
- **Level:** user-goal
- **Primary Actor:** Manager (a member of hotel staff with the role Manager)
- **Stakeholders and Interests:**
  - S01 — Team 2, the owner of the hotel: a surcharge on the busy days and a discount on the quiet days of the next two months are suggested on time, from figures the owner can read, and no price changes without a Manager.
  - S05 — Hotel staff: the price on the room page is the price in force on the day, and a wrong change can be undone.
  - S04 — Valdamar: each approval can be traced to a person, a time and the figures it rests on.
- **Preconditions:** The Manager is signed in (UC-004). A run of the market analysis service has completed, and its report holds price suggestions. A room rate is set for each room size.
- **Postconditions (success guarantee):** A price change exists for the dates the Manager approved, or the Manager reverted one; the price of each room size on each day is the room rate with the price changes in force on that day. The approval or the undo is in the audit trail with the Manager's name and the old and the new values. No price changed without an approval.

### Main Success Scenario

1. The Manager opens "Price suggestions".
2. The system lists the open price suggestions of the next two months. Each shows the room size, the reason (a holiday by its name, or the occupancy of the same days in earlier years), the first and the last day, the percentage, the old and the new nightly price, and the occupancy of each earlier year and the threshold it rests on. The explanation of the analysis and further ideas without a price are shown beside the list; a text written by an AI is marked as such.
3. The Manager reads a price suggestion and approves it. The Manager may change the first and the last day before approving.
4. The system creates the price change for those days and shows it as in force or coming.
5. The Manager sees on the room page that the price of the room size is the new one on the days of the price change.
6. The Manager reverts a price change that is no longer wanted.
7. The system ends the price change that day, or withdraws it if it has not begun, and keeps it in the list as reverted.

### Extensions (Alternative / Exception Flows)

- 2a. No report has completed, or the report holds no price suggestions:
  1. The system says that there is no basis for a price suggestion and lists none.
- 2b. A price change for the same room size already covers some of the days:
  1. The system does not list that price suggestion for those days.
- 3a. The Manager approves a price suggestion a second time (a repeated click):
  1. The system creates no second price change and shows the one that exists.
- 3b. The last day is before the first day or before today, or the days overlap a price change in force for the room size:
  1. The system names the problem and creates nothing.
- 3c. The change would be larger than the largest change, the new price would go below the lowest price that the Manager has set, or a discount would take the whole price:
  1. The system shows the price cut to the limit and says so in the price suggestion.
- 1a. A Receptionist or Housekeeping user asks for "Price suggestions":
  1. The system tells them that the role has no access (UC-004, 6a).
- 4a. Too many changes in a minute (more than 30):
  1. The system refuses with "try again later" and changes nothing.

### Special Requirements / Business Rules

| Step | Rule |
| --- | --- |
| 2 | The market analysis service works out the suggestions of the next two months from the holidays and from the occupancy of the same days in earlier years; the system holds no pricing rule of its own and never makes a suggestion when the service has none (MIL-071) |
| 2 | The high and the low threshold (80 and 40 percent by default), the surcharge (20 percent), the discount (15 percent) and the room sizes that qualify for a discount (single rooms by default) are settings of the Manager, sent to the service with each run |
| 2 | A suggestion rests only on the figures of the report; a text written by an AI never sets a period, a percentage or a price |
| 3, 4 | A price change is never larger than the largest change (30 percent by default), a discount is at most 99.99 percent, and a discount never takes the price below the lowest price; two price changes in force for one room size never overlap |
| 3, 4 | The largest change and the lowest price are applied when the suggestions are listed and again when one is approved; an approved change keeps its percentage, so a room rate lowered or a lowest price raised later can give a price below the new lowest price |
| 4, 7 | A price change applies to bookings made after it is in force; a booking already made keeps its price (to be confirmed by S01) |
| 7 | A price change is never deleted, only reverted, so the list shows what was in force |
| 4, 7 | Every approval and undo, every change of a room rate and every change of the settings is in the audit trail (UC-006 step 9) |
| all | The Manager owns "prices" in the Access Control Matrix; no other role holds the permission |

### Open Issues

- Whether a price change touches bookings already made and the default percentages are for S01 to confirm (MIL-055 task 2); which earlier years the service reads is decided by the service (MIL-071).
- Prices per night for suites and double rooms in low season are not suggested; a later story may add them.

---

[UCD-001]: ../../use-case-diagram.md
[US-001]: ../../user-stories.md
[SA-001]: ../../stakeholder-analysis.md
[UC-004]: ../uc-004/uc.md
[UC-006]: ../uc-006/uc.md
[MIL-055]: ../../milestones/mil-055-room-price-suggestions.md
[MIL-071]: ../../milestones/mil-071-price-suggestions-from-the-service.md
[3a17eee]: https://github.com/NF-Hotel/NFHotel/commit/3a17eeed8127f7dab532e5afb78b09e005ec3118
