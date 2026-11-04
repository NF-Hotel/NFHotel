# MIL-060 Staff see and trade their shifts and rooms in the mobile app

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-060 |
| CrossReference | [BC-001], [MIL-059], [ADR-0015], [ADR-0017] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-04 | Proposed | Claude (AI) | Not reviewed yet | Initial plan, at the request of S01 in chat; the open decisions below must be taken before the milestone can be accepted | pending |
| 2026-11-04 | Proposed | Claude (AI) | Not reviewed yet | S01 took decisions 1 to 3 in chat and added the rooms of the housekeepers: a standing assignment, a minimum of three rooms unless a Manager approves, and trades of rooms between housekeepers that the colleague accepts. The scope, the criteria and the tasks follow; the code of tasks 2 to 6 exists | pending |

---

## Purpose

S01 wants the members of staff to see their shifts in the mobile app and to trade them with each other, and a housekeeper to see the rooms assigned to her or him and to give or trade them to another housekeeper. NF Hotel had no shifts and no room assignments: no table, no API, no screen, and neither the app nor its earlier prototype (`ZanjiOfficial/NFHotelRepo`) held any such code. This milestone is a new feature across the domain, the database, the API, the web back office and the app. The gate decides whether staff can run their own shifts and rooms from the phone within rules a Manager controls.

## Decisions taken

S01 answered in chat on 2026-11-04:

| Question | Decision |
| --- | --- |
| Room assignments | Standing: a room keeps its housekeeper until a Manager or a trade changes it |
| Who must agree to a trade of rooms | The colleague accepts; a Manager approves only a trade that leaves one of the two with fewer rooms than before and fewer than three |
| Trades of shifts | Giving away and swapping; the colleague accepts or takes it, then a Manager approves; offers close 24 hours before the shift |
| Where a Manager plans | In the app and in the web back office |

## Proposed scope

The proposal below is the smallest version that is useful on its own. Each point marked **(decision)** is a default that S01 can change.

- **Shifts.** A shift has a date, a start and an end time, the role it needs (Receptionist, Housekeeping or Manager) and the member who works it. A Manager plans the shifts week by week, in the app and in the web back office **(decision 1)**.
- **Rooms of the housekeepers.** A Manager gives each room a housekeeper, freely. A housekeeper sees the own rooms and gives some to a colleague, or trades them for rooms of the colleague; the colleague accepts or declines. A trade that leaves one of the two with fewer rooms than before and fewer than three waits for a Manager's approval; any other is carried out when the colleague accepts.
- **My shifts.** Every member sees the own shifts of the coming weeks in the app, and a Manager sees everybody's.
- **Give a shift away.** A member offers one of the own shifts. Members with the same role see the open offers and one of them takes it **(decision 3)**.
- **Swap.** A member asks a colleague to swap: my shift for one of yours. The colleague accepts or declines **(decision 3)**.
- **Approval.** A Manager approves or rejects a taken offer or an accepted swap; only then does the shift change hands **(decision 2)**.
- **Rules the system enforces.** Only a member with the shift's role can take it; a member cannot hold two shifts that overlap; an offer closes a fixed time before the shift starts **(decision 4)**; an offer can be withdrawn until it is taken; a disabled or deleted account loses its open offers.
- **Trail.** Every step of a trade (offered, taken, accepted, declined, approved, rejected, withdrawn, expired) is recorded with who did it and when, like the audit trail of [ADR-0017].

### The life of a trade

```
Offered ──take / accept──▶ Pending approval ──approve──▶ Done (the shift changes hands)
   │                              │
   │ withdraw / cut-off            └──reject──▶ Rejected (the shift stays)
   ▼
Withdrawn / Expired
```

A declined swap goes back to nothing; the shift stays with the member who asked.

### Sketch of the model and the API

| Part | Proposal |
| --- | --- |
| Entities | `Shift` (start, end, role, assignee); `ShiftTrade` (the shift offered, the shift asked for in a swap or none, offered by, taken by, state, the time of each step); `RoomAssignment` (room, housekeeper; one per room); `RoomTrade` (from, to, the rooms given and the rooms taken as arrays, state, the time of each step). A deleted account takes its assignments and trades with it and leaves its shifts without a member |
| Permissions | `Shifts.View` and `Shifts.Trade` for every staff role; `Shifts.Manage` for a Manager; `RoomAssignments.View` for Housekeeping and Manager; `RoomAssignments.Trade` for Housekeeping; `RoomAssignments.Manage` for a Manager. Rows in the access control matrix |
| Routes | `GET /api/shifts?from=&to=`; `POST`, `PUT`, `DELETE /api/shifts`; `GET`, `POST /api/shift-trades`; `POST /api/shift-trades/{id}/take`, `/accept`, `/decline`, `/withdraw`, `/approve`, `/reject`; `GET /api/room-assignments`; `PUT /api/room-assignments/{roomId}`; `GET`, `POST /api/room-trades`; `POST /api/room-trades/{id}/accept`, `/decline`, `/withdraw`, `/approve`, `/reject` |
| App access | The four route groups accept the staff token alone ([ADR-0015], amended) and pass the nginx filter of the app |
| App screens | A menu on the rooms screen. For every member "My shifts and rooms": the own shifts with give away and swap, the swaps and the shifts open to the member, the own trades; for a housekeeper a tab of rooms with "Give or trade rooms" and the trades to answer. For a Manager "Planning": the week of shifts, the rooms of the housekeepers, and the trades to approve |
| Web back office | "Shifts": a week plan in the time of the hotel with the trades to approve. "Room assignments": a housekeeper per room, the count of each housekeeper, and the trades to approve |

## Open decisions

Decisions 1 to 4 are taken (see "Decisions taken"). These remain; the code assumes the first answer each time.

| # | Question | Options |
| --- | --- | --- |
| 5 | Should the system enforce working-time rules, such as the 11 hours of rest between shifts that the Danish working-time rules require, or a weekly maximum? | Warn the Manager at approval / refuse the trade / not in the first version |
| 6 | Should the app tell members about a new offer or an approval? Push messages need Firebase Cloud Messaging and Apple Push Notification service, and their own ADR | Not in the first version (the app shows it on refresh) / push from the start |
| 7 | May a member take a shift of another role, for example a Receptionist a Housekeeping shift? | No / yes, if a Manager marks the member as qualified |
| 8 | How long is the history of shifts and trades kept? It is personal data | 12 months / until the end of the payroll year / another period |

## Go / No-Go Criteria

To be made final with the decisions. As proposed:

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | A Manager plans a week of shifts in the app or the web back office, and each member sees exactly the own shifts of that week in the app | Yes | No |
| 2 | A member gives a shift away, a member with the same role takes it, a Manager approves it, and both apps then show the new assignee | Yes | No |
| 3 | A swap that the colleague accepts and a Manager approves exchanges the two shifts; a declined or rejected one changes nothing | Yes | No |
| 4 | Taking a shift of another role, a shift that overlaps one of the own, or an offer after its cut-off is refused with its own code | Yes | No |
| 5 | Every step of a trade is in the trail with who and when | Yes | No |
| 5a | A housekeeper sees the own rooms; a gift or a trade of rooms that the colleague accepts moves the rooms at once, unless it leaves one of the two below three rooms: then it moves only when a Manager approves | Yes | No |
| 5b | A trade whose rooms or shifts changed hands in another way meanwhile is called off with its own code when it is accepted or approved | Yes | No |
| 6 | With the staff token alone, the permissions decide as in the access control matrix; every route outside the list stays 401 | Yes | No |
| 7 | Tests pass, the analysers are clean, and the reviews have the verdict `Go` | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-059 | The staff accounts and the screens of the app that this builds on |
| ADR-0015 | Another amendment for the shift routes, `Accepted` before a release build |
| ADR-0017 | The pattern of the trail |
| A new ADR | Only if decision 6 is push messages |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| No objective of BC-001 or user story covers shifts yet | S01 to add an objective and the user stories (plan shifts, see own shifts, give away, swap, approve) before the milestone is accepted |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01; S02 for the mobile track |

## Target Date

2026-11-20 — for S01 to confirm. Review, the end-to-end check, decisions 5 to 8 and the PostgreSQL tests on a machine with Docker remain.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Decisions | S01 answers decisions 5 to 8; the BC objective, the user stories and the use case are written | Yes | |
| 2 | Domain and database | `Shift`, `ShiftTrade`, `RoomAssignment` and `RoomTrade` with their rules and states, the migration `AddShiftsAndRoomAssignments`, domain and PostgreSQL tests | No | |
| 3 | API | The use cases and routes, the permissions, the access control matrix, application and API tests | No | |
| 4 | Access from the app | The ADR-0015 amendment, `StaffTokenRoutes`, the nginx filter, API and ops tests | No | ADR-0015 |
| 5 | Web back office | The pages "Shifts" and "Room assignments", with view model tests | Yes | |
| 6 | Mobile app | The menu, "My shifts and rooms" and "Planning", with unit and widget tests | Yes | |
| 7 | End-to-end check and review | The criteria against the development server; the reviews; the RC | No | |

---

[BC-001]: ../business-case.md
[MIL-059]: ./mil-059-staff-accounts-in-the-app.md
[ADR-0015]: ../adr/adr-0015-mobile-app-staff-token-access.md
[ADR-0017]: ../adr/adr-0017-staff-passwords-and-audit.md
