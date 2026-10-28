# RC-221 Gate review of MIL-041

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-221 |
| CrossReference | [MIL-041], [RC-220], [ADR-0029], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-25 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [1868d87] |

---

## Artifact Under Review

- **Instance reviewed:** the six Go/No-Go criteria of [MIL-041], after S01 created the Access application `NF Hotel back office` (One-time PIN, policy `Team` with the listed addresses) and the tunnel route `^/admin` that validates the Access token, and after #341 was deployed by CD (40240800156).
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).

## Criteria

| # | Criterion | Result | Evidence |
| --- | --- | --- | --- |
| 1 | An ADR records the change and the alternatives | Go | ADR-0029: Access over an open sign-in and a hidden path; ADR-0028 decision 3 changed |
| 2 | Without an Access session, `/admin/login` shows the Access page, not the hotel's sign-in | Go | From the internet: 302 to `devmatiker.cloudflareaccess.com`; also with a forged `Cf-Access-Jwt-Assertion` header |
| 3 | A listed address gets a code, and the hotel's sign-in and the back office work after it | Go | S01 signed in with a one-time code to `syneuro@gmail.com`, got the hotel's sign-in and used the back office ("fedt, det virker", 2026-10-25) |
| 4 | Without the token header, `/admin` on the public listener answers 404; the route validates the token | Go | `test-nginx.sh`: 404 without the header, the web application with it; the route `^/admin` has "Protect with Access" with the team `devmatiker` and the application's AUD tag |
| 5 | The rest of the public site, the mobile routes and the tailnet back office are unchanged | Go | `/` 200, `/book` 200, `/mobile-api/api/rooms` 401 without a token, `/mobile-api/api/bookings` 404; `https://nf-hotel.taildb84b5.ts.net/admin/login` 200 |
| 6 | CI green and the code review Go | Go | #341: every check green; the change is one nginx location and its tests, reviewed against `qc-programming-shell` and `qc-yaml` here: the header check is a second line behind the tunnel's validation, never the only one |

## Overall Verdict

Go. MIL-041 is complete; this pull request closes #337 and #340.

## Action Items

| # | Action | Owner | Due |
| --- | --- | --- | --- |
| 1 | Add Jakob's e-mail address to the policy `Team` when it is known (wiki page Public-Access, Add or remove a member of staff) | S01 | When known |

---

[MIL-041]: ../../milestones/mil-041-admin-behind-cloudflare-access.md
[RC-220]: rc-220-mil-041.md
[ADR-0029]: ../../adr/adr-0029-back-office-behind-cloudflare-access.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[1868d87]: https://github.com/NF-Hotel/NFHotel/commit/1868d87b99ee23b4348bcb4984b2df04db7699df
