# RC-219 Gate review of MIL-040

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-219 |
| CrossReference | [MIL-040], [RC-217], [RC-218], [ADR-0028], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-25 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [4537604] |

---

## Artifact Under Review

- **Instance reviewed:** the eight Go/No-Go criteria of [MIL-040], after #326 was deployed by CD (40228346331 and, with the token, its rerun), S01 created the tunnel, wrote its token on the VM and added the route `nfhotel.devmatiker.com`, and #333 moved `cloudflared` to 2026.10.0 (CD 40232067777).
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).

## Criteria

| # | Criterion | Result | Evidence |
| --- | --- | --- | --- |
| 1 | An ADR records the choice, the alternatives and the personal data Cloudflare handles | Go | ADR-0028 (RC-218) |
| 2 | From outside the tailnet, `/` and `/book` answer 200 with a valid certificate, and `http://` redirects | Go | Over the public internet through Cloudflare: `/` 200, `/book` 200, certificate verify result 0, `http://nfhotel.devmatiker.com/` 301 to `https://` |
| 3 | No port opened in the home router, `cloudflared` publishes none, the VM firewall unchanged | Go | The tunnel connects outbound (Copenhagen and Berlin edges); the container has no published port; no forwarding was added for MIL-040. The router has two older SSH forwardings to other machines, not part of NF Hotel, which S01 was advised to remove |
| 4 | On the public name, `/admin` and the other API routes answer 404; the back office still works on the tailnet | Go | `/admin` 404, `/admin/login` 404, `/mobile-api/api/bookings` 404, `/mobile-api/` 404; `https://nf-hotel.taildb84b5.ts.net/admin/login` 200 |
| 5 | The mobile sign-in and rooms routes answer as on the development stack | Go | Sign-in with a wrong password 401 and rooms without a staff token 401, both answered by the API through the tunnel. A sign-in with a real staff account was not made, because the AI holds no staff password; the mobile app's own test covers it |
| 6 | The VM runs as `Production`, and an error page shows no stack trace | Go | `ASPNETCORE_ENVIRONMENT=Production` in the web and API containers; `/Error` on the public name holds no stack trace |
| 7 | The monitoring probes the public address, and a rule test shows the alert | Go | `probe_success{job="public_site"}` is 1 for both addresses; `PublicSiteDown` is loaded and inactive; its promtool tests pass |
| 8 | The token is in no git file, log or environment; CI green; code review Go | Go | `check-secret-env.sh` passes for `cloudflared`; the token is a 0444 secret file read with `--token-file`; RC-218 Go. S01 also pasted the token into the chat with the AI on purpose (a prototype); the wiki now says how to rotate it |

## Findings of the check

| # | Finding | What was done |
| --- | --- | --- |
| 1 | Cloudflare adds a public host name only after a connector is connected, so the wiki's order (name before token) could not be followed | The wiki page Public-Access now has the token first (Part B) and the name after (Part C), with the dashboard's current menu names |
| 2 | The dashboard marked `cloudflared` 2025.9.1 as unsupported | #333 moved it to 2026.10.0, pinned by digest |
| 3 | SonarCloud rated new code B for security on the plain-HTTP hop of the nginx test, and the CD gate refused to deploy | #330 and #331 marked it with NOSONAR and the reason, as the smoke test of `deploy-release.sh` does |

## Overall Verdict

Go. MIL-040 is complete; this pull request closes #325.

## Action Items

| # | Action | Owner | Due |
| --- | --- | --- | --- |
| 1 | Rotate the tunnel token, which was pasted into the chat (wiki page Public-Access, Rotate the token) | S01 | When convenient |
| 2 | Remove the two SSH port forwardings in the home router and raise its firewall to Medium | S01 | Soon |

---

[MIL-040]: ../../milestones/mil-040-public-access.md
[RC-217]: rc-217-mil-040.md
[RC-218]: rc-218-mil-040-code.md
[ADR-0028]: ../../adr/adr-0028-public-access-through-cloudflare-tunnel.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[4537604]: https://github.com/NF-Hotel/NFHotel/commit/45376044ae91bb1584a91d84c8446d61b661e4ae
