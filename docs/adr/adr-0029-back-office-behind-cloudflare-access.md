# ADR-0029 The back office on the public address, behind Cloudflare Access

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0029 |
| CrossReference | [ADR-0028], [ADR-0005], [MIL-041], [MIL-072] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-11 | Deprecated | mikemike | S01 | Decisions 2, 3 and 5 made precise by MIL-072, after the validating route was found second in the list and its path and the guard of nginx were found to compare letter case: the validating route first, with the path `(?i)^/admin`; the guard of nginx for every spelling of `/admin`; the order of the routes checked in the log of `cloudflared` | [a1a7092] |
| 2026-11-11 | Accepted | mikemike | AI as stand-in for S01 | Decision 1: the session lasts 1 week, as the Access application is set; S01 decided in chat that this record follows the setting; accepted at the gate of MIL-072 (RC-339) | [3afb6a3] |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

**Who decided what.** S01 asked on 2026-10-25 for the back office to be reachable at `nfhotel.devmatiker.com/admin` and chose to put Cloudflare in front of it, for the e-mail addresses of the team. The design of this record was written by the AI assistant.

ADR-0028 decision 3 kept `/admin` on the tailnet name only: on the public name it answers 404, so a stranger never sees the staff sign-in. That forces every member of staff to run Tailscale, which S01 does not want for the back office.

Options considered:

1. **Open `/admin` on the public name as it is.** The hotel's sign-in has a per-address limit (ADR-0005) and Cloudflare's rate-limiting rule, but every bot on the internet sees it and can try passwords.
2. **A hidden path,** such as `/adminski`. Paths leak through history, bookmarks and shared links, and the application builds its own links to `/admin`, so the code would have to change for what is only obscurity.
3. **Cloudflare Access in front of `/admin`.** Before the hotel's sign-in appears, Cloudflare asks for an e-mail address and sends a one-time code to it, only if the address is on the list. The application does not change, and it is free for up to 50 users.

**2026-11-11 (MIL-072).** The validating route of decision 2 had been added second, after the route without a path, and `cloudflared` takes the first route that matches, so it never took a request. Its path `^/admin` and the `location ^~ /admin` of nginx also compared letter case, while the web application matches its routes without regard to it. Access at the edge still stood in front of every spelling, so no stranger reached the staff sign-in, but the check of the tunnel was not in effect. Decisions 2, 3 and 5 below are made precise accordingly.

## Decision

We follow option 3. It changes ADR-0028 decision 3 for `/admin` only.

1. **An Access application** for `nfhotel.devmatiker.com/admin`, with the login method One-time PIN and a policy that allows the listed addresses of the team. The session lasts 1 week, the setting of the application (S01, 2026-11-11): staff type a code once a week, an address taken off the list gets no new code at once, and a session already open ends within the week.
2. **The tunnel validates the token.** A published route of the tunnel, path `(?i)^/admin`, has "Validate the Access JWT at the tunnel" on, with the team name and the Application Audience (AUD) tag of the Access application, and it stands first in the list of routes: `cloudflared` takes the first route that matches, so below the route without a path it would never be used. The path ignores letter case, as the routes of the web application do. So a request for `/admin`, in any spelling, that did not pass Access stops at `cloudflared` and never reaches the virtual machine (VM).
3. **nginx adds a second line.** The public listener serves `/admin`, in any spelling, only to requests that carry `Cf-Access-Jwt-Assertion` (`location ~* ^/admin`); without it, `/admin` still answers 404. nginx does not validate the token; the tunnel does.
4. **Two locks.** Access decides who may see the sign-in; the hotel's own sign-in, roles and per-address limit stay as they are. A leaked password alone is not enough from the internet.
5. **The order of the rollout.** The Access application and the validating route, first in the list, exist before the nginx change is deployed, so `/admin` is never open on the public name without Access. After a change of the routes, the order is checked in the log of `cloudflared`, which writes its routes in order at every new configuration.

## Consequences

**Positive:**

- Staff reach the back office from any browser without Tailscale.
- A stranger sees Cloudflare's e-mail page, never the staff sign-in, and gets no code.

**Negative:**

- Cloudflare learns the e-mail addresses of the staff and when they sign in.
- A member of staff needs access to their e-mail to get a code; adding or removing someone is a change in the Cloudflare dashboard, written down in the wiki page Public-Access.
- The routes of the tunnel live in the dashboard, not in git, so no test of the repository sees their order; a later change of them is seen in the log of `cloudflared` (decision 5).

## Affected Artifacts

- [ADR-0028]: decision 3 changes for `/admin`; the rest stands.
- [MIL-041]: tasks 1 to 4.
- [MIL-072]: decisions 2, 3 and 5 made precise; the order and the path of the validating route, the guard of nginx for every spelling, and its test.

---

[ADR-0005]: ./adr-0005-backoffice-login.md
[ADR-0028]: ./adr-0028-public-access-through-cloudflare-tunnel.md
[MIL-041]: ../milestones/mil-041-admin-behind-cloudflare-access.md
[MIL-072]: ../milestones/mil-072-the-access-check-on-every-admin-path.md
[a1a7092]: https://github.com/NF-Hotel/NFHotel/commit/a1a709262eda420d46bb2be56f639718b97ba48c
[3afb6a3]: https://github.com/NF-Hotel/NFHotel/commit/3afb6a38797598a205e2c4c82dd19c352967ae89
