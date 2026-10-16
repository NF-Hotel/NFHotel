# ADR-0004 Only an internal API reaches the database

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0004 |
| CrossReference | [BC-001], [MIL-015], [ADR-0003] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Deprecated | Claude (AI) | AI as stand-in for S01 (decision given by the user in chat) | Initial version, accepted after RC-064 Go | [b8170b7] |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S01 | Clarified: no endpoint for ValidateFields, the client runs the rule locally (reviewed in RC-065) | [b8170b7] |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

The Blazor web application is the only service that the internet reaches
([ADR-0003]). Today it also holds the database: the connection string of
PostgreSQL, the key that encrypts passport numbers, and the data access code.
A defect or a compromise of the internet-facing application is then a
compromise of the database. The user decided in chat that the database must not
be exposed to the internet and asked for an API in front of it.

Forces: the internet-facing process should hold as little as possible; the
screens and their tests must not change; the Application use cases are already
the contract between screens and data; one VM with 12 GiB memory runs
everything, so the extra service must be small.

Options considered:

1. **Keep direct access** and restrict the PostgreSQL role to what the application needs. The login and the key are still in the exposed process.
2. **A gRPC service** in front of the database. Strong contracts, but a second serialisation stack, browser tooling that the team does not use, and generated code for a contract that the C# interfaces already define.
3. **A REST API** (ASP.NET Core, controllers) that hosts the Application use cases and the persistence, and a client that implements the same interfaces over `HttpClient`.

## Decision

We follow option 3. A new project `NFHotel.Api` hosts the three use-case
services (bookings, guests, rooms) with the persistence and the passport key; a
new project `NFHotel.ApiClient` implements `IBookingService`, `IGuestService`
and `IRoomService` over HTTP; and `NFHotel.Web` uses the client and holds only an
API address and a token.

- **Contract.** One endpoint per interface method, except `ValidateFields`: it reads and writes nothing, so the client runs the same domain rule locally (`GuestService.Validate`) and the API has no endpoint for it. The Application DTOs are the request and response bodies. Every response is `{ "value": ..., "errors": ["code", ...] }`; the HTTP status says which kind of failure: 404 for a `*.not_found` code, 409 for a conflict, 422 for another rule error, 401 for a missing or wrong token. The routes and the envelope live in `NFHotel.Api.Contracts`, shared by both sides, so they cannot drift.
- **Same behaviour.** The client returns the same `Result` as the service would: the error codes of the envelope, and the same `ArgumentOutOfRangeException` for a key below one. A contract test sends every method through the client and the API and compares.
- **Network.** The API publishes no port and nginx has no route to it. It joins two Docker networks: `nfhotel-db` (with PostgreSQL) and `nfhotel-api` (with the web application). The web container leaves `nfhotel-db`.
- **Token.** The web application sends a bearer token from its environment; the API compares it in constant time and answers 401 otherwise. The token is a secret in the git-ignored env files, never in the repository.
- **Secrets move.** `ConnectionStrings__HotelDatabase` (built from `postgres.env`) and `Encryption__PassportKey` leave `nfhotel-web.env` and go to `nfhotel-api.env`.
- **External adapters.** The holiday client and the system clock do not touch the database. They move to a new project `NFHotel.External`, used by the API (the clock) and the web application (both), so that the web assembly holds no Npgsql or EF Core. The milestone named this project `NFHotel.Holidays`; it also holds the clock, hence the name.
- **Presentation pattern.** The API uses the MVC pattern of the `clean-architecture` skill: controllers are thin, one call to a use case each.

## Consequences

**Positive:**

- The internet-facing application holds neither the database login nor the passport key, and its binary cannot connect to PostgreSQL.
- The screens, the view models and their tests are unchanged: they still call the same interfaces.
- The API is a second place where authentication and rate limits can be added later, without touching the screens.

**Negative:**

- One more container, about 150 MiB of memory, and one more hop on every call.
- Two more projects to keep in step with the Application layer; the contract test and the shared routes limit the drift.
- A failure of the API takes the screens down as a database failure would; the client reports it as `api.unavailable`.
- A shared secret (the token) must be created and rotated by hand.

Quality attributes: security improves (smaller attack surface of the exposed process, defence in depth with network and token); performance falls slightly because of the extra hop; maintainability is neutral because the interfaces stay the single contract.

## Affected Artifacts

- [MIL-015] — decided here; its tasks build and deploy it
- [ADR-0003] — adds the API and the networks to the service picture
- [BC-001] — objective 18

---

[BC-001]: ../business-case.md
[MIL-015]: ../milestones/mil-015-database-api.md
[ADR-0003]: ./adr-0003-proxmox-vm-for-services.md
[b8170b7]: https://github.com/NF-Hotel/NFHotel/commit/b8170b7a84409f6e19f209090c310a9f42c53bc9
