# MIL-060 Staff see their shifts and trade them in the mobile app

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

---

## Purpose

S01 wants the members of staff to see their shifts in the mobile app and to trade them with each other. NF Hotel has no shifts today: no table, no API, no screen, and neither the current app nor its earlier prototype (`ZanjiOfficial/NFHotelRepo`) holds any shift code. This milestone is therefore a new feature across the domain, the database, the API, the web back office and the app. This document is the plan; nothing is built until S01 has taken the decisions in "Open decisions" and accepted it.

## Proposed scope

The proposal below is the smallest version that is useful on its own. Each point marked **(decision)** is a default that S01 can change.

- **Shifts.** A shift has a date, a start and an end time, the role it needs (Receptionist, Housekeeping or Manager) and the member who works it. A Manager plans the shifts in the web back office, week by week **(decision 1)**.
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
| Entities | `Shift` (id, start, end, role, assignee); `ShiftTrade` (id, the shift offered, the shift asked for in a swap or none, offered by, taken by, state, the time of each step) |
| Permissions | `Shifts.ViewOwn` for every role; `Shifts.Trade` for every role; `Shifts.Manage` (plan, approve, reject, see all) for a Manager. New rows in the access control matrix |
| Routes | `GET /api/shifts?from=&to=`; `POST`, `PUT`, `DELETE /api/shifts` for a Manager; `GET /api/shift-trades`; `POST /api/shift-trades` (offer or swap); `POST /api/shift-trades/{id}/take`, `/accept`, `/decline`, `/withdraw`, `/approve`, `/reject` |
| App access | `/api/shifts` and `/api/shift-trades` would join the routes that accept the staff token alone: another amendment of [ADR-0015] and of the nginx filter |
| App screens | "My shifts" (a list per week), "Open shifts" (the offers a member can take), and for a Manager "To approve"; in the look of the app since [MIL-059] |
| Web back office | A week plan to create, move and delete shifts, and the same approval list |

## Open decisions

S01 decides these before the milestone is accepted. The proposal above assumes the first answer each time.

| # | Question | Options |
| --- | --- | --- |
| 1 | Who plans the shifts, and where? | A Manager in the web back office only / also in the app / imported from another rota system |
| 2 | Does every trade need a Manager's approval? | Always / only a swap / never, the trade is done when it is taken |
| 3 | What can be traded in the first version? | Giving away and swapping / giving away only |
| 4 | How long before the start does an offer close? | 24 hours / another time / no limit |
| 5 | Should the system enforce working-time rules, such as the 11 hours of rest between shifts that the Danish working-time rules require, or a weekly maximum? | Warn the Manager at approval / refuse the trade / not in the first version |
| 6 | Should the app tell members about a new offer or an approval? Push messages need Firebase Cloud Messaging and Apple Push Notification service, and their own ADR | Not in the first version (the app shows it on refresh) / push from the start |
| 7 | May a member take a shift of another role, for example a Receptionist a Housekeeping shift? | No / yes, if a Manager marks the member as qualified |
| 8 | How long is the history of shifts and trades kept? It is personal data | 12 months / until the end of the payroll year / another period |

## Go / No-Go Criteria

To be made final with the decisions. As proposed:

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | A Manager plans a week of shifts in the web back office, and each member sees exactly the own shifts of that week in the app | Yes | No |
| 2 | A member gives a shift away, a member with the same role takes it, a Manager approves it, and both apps then show the new assignee | Yes | No |
| 3 | A swap that the colleague accepts and a Manager approves exchanges the two shifts; a declined or rejected one changes nothing | Yes | No |
| 4 | Taking a shift of another role, a shift that overlaps one of the own, or an offer after its cut-off is refused with its own code | Yes | No |
| 5 | Every step of a trade is in the trail with who and when | Yes | No |
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

To be set when the open decisions are taken. A first estimate for the proposed scope is three weeks after acceptance.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Decisions | S01 answers the open decisions; the BC objective, the user stories and the use case are written | Yes | |
| 2 | Domain and database | `Shift` and `ShiftTrade` with their rules and states, the migration, domain and PostgreSQL tests | No | |
| 3 | API | The use cases and routes, the permissions, the access control matrix, the trail, application and API tests | No | |
| 4 | Access from the app | The ADR-0015 amendment, `StaffTokenRoutes`, the nginx filter, API and ops tests | No | ADR-0015 |
| 5 | Web back office | The week plan and the approval list, with tests | Yes | |
| 6 | Mobile app | "My shifts", "Open shifts" and "To approve", with unit and widget tests | Yes | |
| 7 | End-to-end check and review | The criteria against the development server; the reviews; the RC | No | |

---

[BC-001]: ../business-case.md
[MIL-059]: ./mil-059-staff-accounts-in-the-app.md
[ADR-0015]: ../adr/adr-0015-mobile-app-staff-token-access.md
[ADR-0017]: ../adr/adr-0017-staff-passwords-and-audit.md
