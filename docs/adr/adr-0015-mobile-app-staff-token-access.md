# ADR-0015 The mobile app reaches the API with the staff token only

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0015 |
| CrossReference | [BC-001], [MIL-029], [ADR-0004], [ADR-0005] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Proposed | Claude (AI) | S01 | Initial version, from the decisions of S01 in chat: login and rooms, staff token only, Docker development server | pending |
| 2026-10-20 | Accepted | Claude (AI) | AI at the request of the user, not independent | Accepted after RC-131 Go | pending |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

The Flutter mobile app (`NF-Hotel/MobileApp`) has no network code yet; its login screen is empty and its rooms are local models. S01 decided in chat that it should sign in and work with rooms against this API.

[ADR-0004] makes every `/api` call carry the service token of the web application, and [ADR-0005] adds the staff token in `X-Staff-Token`. The service token is a shared secret held by one server process. An app on a phone cannot keep a secret: anyone can unpack it, and one leak would open every public endpoint (bookings, free rooms) to the internet.

Options considered:

1. **Ship the service token in the app.** Simple, but the secret is public the day the app is installed.
2. **A separate mobile gateway** that holds the service token. One more service on a VM with 12 GiB for two endpoints.
3. **Accept the staff token alone on a small set of routes.** The staff token is already signed, expires after 8 hours and carries a role; the API already checks the permission of each endpoint.

## Decision

We follow option 3.

- **Which routes.** `POST /api/auth/login` and the rooms endpoints (`/api/rooms`, read and the housekeeping actions) accept a call that has a valid staff token and no service token. A call without either is still refused with 401. Every other route still needs the service token.
- **Order of checks.** The service-token middleware lets a call through when the service token is right, or when the route is on the list above and `X-Staff-Token` validates; the permission attribute then decides as before, so a Housekeeping member still cannot reach bookings or guests (those routes are not on the list, and the permission is not enough).
- **Login without the service token.** The login is open to the internet on this route; the existing throttle (5 failures per user name in 15 minutes, 429 afterwards) is the protection. The throttle is also applied per client address so that one address cannot try many user names.
- **App side.** The app keeps the staff token in the platform's secure storage (Keychain, Keystore), never in plain preferences, sends it in `X-Staff-Token`, and signs the member out on 401.
- **Development address.** The app is tested against the development server that `scripts/dev-up.sh` starts in Docker (Proxmox VE container, VM, the seven services; the site is at `https://localhost:8443/` with a development certificate). The API publishes no port there, so a development-only nginx route (`/mobile-api/` to the `nfhotel-api` service, present only when `ASPNETCORE_ENVIRONMENT=Development`) makes the listed routes reachable on that address. The app takes its base URL at build time (`--dart-define=API_BASE_URL=...`; default `https://10.0.2.2:8443/mobile-api` for the Android emulator, `https://localhost:8443/mobile-api` otherwise) and trusts the development certificate in debug builds only. The development accounts (Manager, Receptionist, Housekeeping) are the ones `dev-up.sh` creates. Reaching the API from the internet in production (nginx route, TLS, a change to [ADR-0003] and [ADR-0006]) is not part of this decision and needs its own ADR before any release build.

## Consequences

**Positive:**

- No secret in the app; a stolen app holds nothing a stranger can use.
- The permission matrix of [ADR-0005] is reused, so the app cannot do more than the back office.
- Small change: one middleware rule, no new service.

**Negative:**

- The login endpoint is reachable without the service token, so its throttling must hold; a defect there is now an internet-facing defect.
- The route list is a second place that names the open routes; a test must fail when a route outside the list accepts a staff token alone.
- The release build stays blocked until the API address, TLS and network exposure are decided.

## Affected Artifacts

- [ADR-0004] — the service token is no longer required on the routes listed above.
- [ADR-0005] — the staff token may travel without the service token on those routes.
- [MIL-029] — the plan that builds this.

[BC-001]: ../business-case.md
[MIL-029]: ../milestones/mil-029-mobile-app-staff-api.md
[ADR-0003]: ./adr-0003-proxmox-vm-for-services.md
[ADR-0004]: ./adr-0004-database-api.md
[ADR-0005]: ./adr-0005-backoffice-login.md
[ADR-0006]: ./adr-0006-tls.md
