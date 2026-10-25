# ADR-0029 The back office on the public address, behind Cloudflare Access

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0029 |
| CrossReference | [ADR-0028], [ADR-0005], [MIL-041] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-25 | Accepted | mikemike | AI as stand-in for S01 | Initial version; written by the AI in autonomous mode under MIL-041, which S01 accepted in chat on 2026-10-25; the choice of Cloudflare Access and the list of addresses are S01's | pending |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

**Who decided what.** S01 asked on 2026-10-25 for the back office to be reachable at `nfhotel.devmatiker.com/admin` and chose to put Cloudflare in front of it, for the e-mail addresses of the team. The design of this record was written by the AI assistant.

ADR-0028 decision 3 kept `/admin` on the tailnet name only: on the public name it answers 404, so a stranger never sees the staff sign-in. That forces every member of staff to run Tailscale, which S01 does not want for the back office.

Options considered:

1. **Open `/admin` on the public name as it is.** The hotel's sign-in has a per-address limit (ADR-0005) and Cloudflare's rate-limiting rule, but every bot on the internet sees it and can try passwords.
2. **A hidden path,** such as `/adminski`. Paths leak through history, bookmarks and shared links, and the application builds its own links to `/admin`, so the code would have to change for what is only obscurity.
3. **Cloudflare Access in front of `/admin`.** Before the hotel's sign-in appears, Cloudflare asks for an e-mail address and sends a one-time code to it, only if the address is on the list. The application does not change, and it is free for up to 50 users.

## Decision

We follow option 3. It changes ADR-0028 decision 3 for `/admin` only.

1. **An Access application** for `nfhotel.devmatiker.com/admin`, with the login method One-time PIN and a policy that allows the listed addresses of the team. The session lasts 24 hours.
2. **The tunnel validates the token.** A second published route of the tunnel, path `^/admin`, has "Validate the Access JWT at the tunnel" on, with the team name and the AUD tag of the Access application. So a request for `/admin` that did not pass Access stops at `cloudflared` and never reaches the VM.
3. **nginx adds a second line.** The public listener serves `/admin` only to requests that carry `Cf-Access-Jwt-Assertion`; without it, `/admin` still answers 404. nginx does not validate the token; the tunnel does.
4. **Two locks.** Access decides who may see the sign-in; the hotel's own sign-in, roles and per-address limit stay as they are. A leaked password alone is not enough from the internet.
5. **The order of the rollout.** The Access application and the validating route exist before the nginx change is deployed, so `/admin` is never open on the public name without Access.

## Consequences

**Positive:**

- Staff reach the back office from any browser without Tailscale.
- A stranger sees Cloudflare's e-mail page, never the staff sign-in, and gets no code.

**Negative:**

- Cloudflare learns the e-mail addresses of the staff and when they sign in.
- A member of staff needs access to their e-mail to get a code; adding or removing someone is a change in the Cloudflare dashboard, written down in the wiki page Public-Access.

## Affected Artifacts

- [ADR-0028]: decision 3 changes for `/admin`; the rest stands.
- [MIL-041]: tasks 1 to 4.

---

[ADR-0005]: ./adr-0005-backoffice-login.md
[ADR-0028]: ./adr-0028-public-access-through-cloudflare-tunnel.md
[MIL-041]: ../milestones/mil-041-admin-behind-cloudflare-access.md
