# MIL-015 A database API: only the API reaches the database

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-015 |
| CrossReference | [BC-001], [MIL-014], [ADR-0003] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S01 | Accepted after RC-063 Go | [9e6d2d4] |

---

## Purpose

Decide whether the web application can stop talking to PostgreSQL itself and go
through an API that only the web application can reach. Today the Blazor
application holds the connection string and the passport encryption key, and
runs the data access code. A defect or a compromise of the internet-facing
application would then be a defect or a compromise of the database. With an
API in between, only the API holds the database login and the key; the web
application holds a token for the API and nothing else; and PostgreSQL and the
API are never reachable from the internet (ADR-0003).

## Decisions to record in ADR-0004

| # | Decision | Why |
| --- | --- | --- |
| 1 | A new project `NFHotel.Api`, an ASP.NET Core API with the MVC pattern (controllers) as the `clean-architecture` skill says for an API. It references Application and Infrastructure and registers them as `AddInfrastructure` does today | One place that holds the database login, the passport key and the data access |
| 2 | The use-case interfaces (`IBookingService`, `IGuestService`, `IRoomService`) are the contract. Each gets a controller that maps one method to one endpoint and returns the `Result` as a small envelope: the value, or the list of error codes, with the HTTP status | The Application DTOs are already the request and response types, so no second model is made |
| 3 | A new project `NFHotel.ApiClient` implements the same three interfaces by calling the API with `HttpClient`, and `NFHotel.Web` references it instead of Infrastructure | The view models and their tests do not change; they still call `IBookingService` |
| 4 | `HolidayApiClient` is not a database client, but it sits in the Infrastructure assembly with Npgsql and EF Core. It moves to `NFHotel.Holidays` so the web assembly carries no database code | A binary without Npgsql cannot connect to the database even by mistake |
| 5 | The API is reachable only on a Docker network that the web application shares with it (`nfhotel-api`, internal). It publishes no port and nginx has no route to it. The web application authenticates with a bearer token from its environment; the API refuses a call without it | Defence in depth: the network keeps the internet out, the token keeps any other container out |
| 6 | The database network `nfhotel-db` is joined by the API only. The web container loses `ConnectionStrings__HotelDatabase`, the passport key and the `nfhotel-db` network | The point of the milestone |

## Deliverable

- ADR-0004 recording the decisions above, reviewed.
- `NFHotel.Api` with controllers for bookings, guests and rooms, the token check, a health endpoint and the error envelope.
- `NFHotel.ApiClient` implementing the three interfaces; `NFHotel.Holidays` with the holiday client.
- `NFHotel.Web` referencing Application, ApiClient and Holidays only; no EF Core or Npgsql package in its output.
- `deploy/nfhotel-api` with a compose file and `nfhotel-api.env.example`; `deploy/nfhotel-web` without database settings; `deploy-services.sh` and `deploy.sh` know the new service; the wiki page describes the new network picture.
- Tests: controller tests with fake services, client tests with a stub handler, a contract test that sends every request type through both and compares, architecture tests for the new references, and an integration test of the API against PostgreSQL.
- Unchanged: the screens, the Application use cases, the domain and the database schema.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | An architecture test shows that `NFHotel.Web` and `NFHotel.ApiClient` have no reference to `NFHotel.Infrastructure`, EF Core or Npgsql, and the web build output holds no `Npgsql.dll` | Yes | No |
| 2 | In the deployed VM the web container has no database setting in its environment and is not on `nfhotel-db`; `docker inspect` shows it | Yes | No |
| 3 | A request to the API without the token, or with a wrong one, gets 401 and changes nothing | Yes | No |
| 4 | From the VM host, the API and PostgreSQL publish no port; from outside the VM only port 80 of nginx answers; checked as the deploy check of MIL-004 | Yes | No |
| 5 | For every method of the three interfaces, the call through `ApiClient` to the API gives the same `Result` (value or error codes) as the call to the service directly; the contract test passes | Yes | No |
| 6 | The existing view-model tests pass unchanged, and a full booking, check-in and check-out can be done in the deployed application | Yes | No |
| 7 | The passport number is encrypted in the database and readable in the application, as before (the integration test of MIL-012 passes against the API path) | Yes | No |
| 8 | `dotnet build` has 0 warnings, format is clean, all tests pass; `qc-programming-csharp`, `qc-yaml` and `qc-programming-shell` reviews have the verdict `Go`; ADR-0004 has a `Go` review | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-014 | The API adds a network hop, so the async and cancellation rules must hold before it: every call passes its token |
| MIL-013 | The two guards are part of the service behaviour that the contract test fixes |
| ADR-0003 | The deployment target and the network rules |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| New objective 18 of an amended BC-001: the database is reachable only through an API that is not exposed to the internet | All tasks |
| Success criterion 21 | Criteria 1 to 4 |
| Constraint of the Business Case: only the web application is exposed (ADR-0003, MIL-004) | Task 8 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01; S03 for the network and deploy changes |

## Target Date

2027-01-22 — three weeks after MIL-014 (2026-12-30): a new service, a client, a split of one project and a deploy change.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Amend Business Case and Project Plan | Add objective 18, the scope items, success criterion 21 and the MIL-015 schedule row; review again. | No | |
| 2 | Record the decisions in ADR-0004 | The six decisions above, with the options considered (direct database access with a restricted role; a gRPC service; a REST API). | No | |
| 3 | Split `NFHotel.Holidays` out of Infrastructure | Move the holiday client, its options and tests; Infrastructure keeps persistence, security and time. | No | |
| 4 | Create `NFHotel.Api` | Controllers, the result envelope, the token check, the health endpoint, `AddInfrastructure` in its composition root; controller tests. | No | |
| 5 | Create `NFHotel.ApiClient` | The three interface implementations over `HttpClient`, the token header, the error mapping; client tests with a stub handler; the contract test. | No | |
| 6 | Switch `NFHotel.Web` to the client | Remove the Infrastructure reference and the database settings; add the client and the holiday project; architecture tests for the references. | No | |
| 7 | Deploy | `deploy/nfhotel-api`, the networks `nfhotel-api` (internal) and `nfhotel-db` for the API only, env examples without secrets, `deploy-services.sh`, `deploy.sh` and the smoke test; the wiki. | No | |
| 8 | Prove it in the VM | Deploy to the Proxmox VE container as in MIL-004; check criteria 2, 4 and 6 and record the commands. | No | |
| 9 | Check and review | Build, format, test; reviews against `qc-programming-csharp`, `qc-yaml`, `qc-programming-shell` and `qc-adr`. | No | |

---

[BC-001]: ../business-case.md
[MIL-014]: ./mil-014-async-where-it-matters.md
[ADR-0003]: ../adr/adr-0003-proxmox-vm-for-services.md
[9e6d2d4]: https://github.com/NF-Hotel/NFHotel/commit/9e6d2d40d332b50fbd405564fcced006e241effd
