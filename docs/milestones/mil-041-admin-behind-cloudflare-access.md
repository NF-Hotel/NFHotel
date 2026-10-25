# MIL-041 The back office on the public address, behind Cloudflare Access

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-041 |
| CrossReference | [BC-001], [ADR-0028], [MIL-040] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-25 | Accepted | mikemike | S01 | Initial version; S01 asked in chat on 2026-10-25 for the back office to be reachable at `nfhotel.devmatiker.com/admin`, with Cloudflare in front ("ok lad os sætte en cloudflare foran"), for the team's e-mail addresses; S01 accepted it in chat ("Jeg accepterer planen for MIL-041"); accepted after RC-220 Go | [58af7e8] |

---

## Purpose

Decide whether NF Hotel may claim that its staff reach the back office from the public address without Tailscale, while a stranger never sees the staff sign-in. MIL-040 kept `/admin` on the tailnet only (ADR-0028 decision 3). S01 now wants it on `nfhotel.devmatiker.com/admin`, guarded by Cloudflare Access: a one-time code by e-mail to a listed address before the hotel's own sign-in appears.

## Deliverable

- An ADR that changes ADR-0028 decision 3: `/admin` on the public address behind Cloudflare Access, compared with a hidden path and with an open sign-in, with the reasons.
- A Cloudflare Access application for `nfhotel.devmatiker.com/admin`, allowing the team's e-mail addresses with a one-time PIN, and a tunnel route for `^/admin` that validates the Access token at the tunnel, so a request that did not pass Access never reaches nginx.
- The public listener of nginx serves `/admin` only to requests that carry the Access token header; every other request for `/admin` still answers 404.
- The wiki page Public-Access: how a member of staff signs in, how to add or remove an address, and the setup steps.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | An ADR records the change and the alternatives, with the reasons | Yes | No |
| 2 | From the internet, `https://nfhotel.devmatiker.com/admin/login` without an Access session shows the Cloudflare Access page, not the hotel's sign-in | Yes | No |
| 3 | A listed address gets a code, and after it the hotel's sign-in and the back office work | Yes | No |
| 4 | A request to `/admin` that reaches the public listener without the Access token header answers 404 (tested in `test-nginx.sh`); the tunnel route validates the token | Yes | No |
| 5 | The rest of the public site and the mobile routes are unchanged, and the back office still works on the tailnet | Yes | No |
| 6 | CI is green and the code review against `qc-programming-shell` and `qc-yaml` is Go | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-040 | The tunnel, the public listener and its route |
| S01 | Creates the Access application and the route in the Cloudflare dashboard, before the nginx change is deployed |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| Objective 28: the hotel on the public internet, now with the back office for the listed staff | Tasks 2 to 4 |
| Objective 22 (back office login): no back office page answers without a valid sign-in | Task 3 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-01.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | ADR for the back office behind Access | Write the ADR that changes ADR-0028 decision 3, with the alternatives (Access, a hidden path, an open sign-in) and what Cloudflare learns. | No | criterion 1 |
| 2 | Access application and route | Guide S01 through the Access application (the listed e-mails, one-time PIN, session length) and the tunnel route `^/admin` with token validation; record the settings in the wiki. | No | criteria 2, 3 |
| 3 | Public listener opens `/admin` behind the token | nginx serves `/admin` on port 8082 only when `Cf-Access-Jwt-Assertion` is present, with tests in `test-nginx.sh`; deployed only after task 2. | No | criteria 4, 5 |
| 4 | Wiki page Public-Access | How staff sign in, how to add or remove an address, and the setup steps. | No | criterion 3 |
| 5 | Check and review | Prove the criteria from the internet and the tailnet; reviews. | No | criteria 2 to 6 |

---

[BC-001]: ../business-case.md
[ADR-0028]: ../adr/adr-0028-public-access-through-cloudflare-tunnel.md
[MIL-040]: ../milestones/mil-040-public-access.md
[58af7e8]: https://github.com/NF-Hotel/NFHotel/commit/58af7e8092d84266aa8df114f77cc273f6ab6980
