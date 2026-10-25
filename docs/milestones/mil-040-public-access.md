# MIL-040 NF Hotel on the public internet through Cloudflare Tunnel

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-040 |
| CrossReference | [BC-001], [ADR-0006], [ADR-0009], [ADR-0015], [ADR-0024], [ADR-0025] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-25 | Accepted | mikemike | S01 | Initial version; S01 asked in chat on 2026-10-25 for the site to be public in a safe way with a free certificate, chose Cloudflare Tunnel and bought the domain `devmatiker.com`; S01 accepted it in chat ("Jeg accepterer planen for MIL-040"); accepted after RC-217 Go | [7e61d06] |

---

## Purpose

Decide whether NF Hotel may claim that guests and the mobile app reach it from the public internet, safely, without opening a port of the home network. Today the site is reachable only from the tailnet (ADR-0009): a guest without Tailscale cannot open it, and the mobile app works only against the development stack (ADR-0015). S01 chose Cloudflare Tunnel over Tailscale Funnel and over opening ports in the home router, and bought `devmatiker.com` at Cloudflare.

## Deliverable

- An ADR that chooses Cloudflare Tunnel for the public entrance, compared with Tailscale Funnel and with ports opened in the home router, with the reasons; it names Cloudflare as a processor of the guests' personal data (GDPR) and what it sees.
- The public host name `nfhotel.devmatiker.com`, served through a `cloudflared` container next to nginx that only makes an outbound connection; its tunnel token is a secret file (ADR-0024).
- On the public host name: the site, the guest booking and the mobile app routes of ADR-0015 (`/mobile-api/api/auth/login`, `/mobile-api/api/rooms`). The back office (`/admin`) and every other API route stay on the tailnet only.
- The VM runs as `Production`, so an error never shows a stack trace to a stranger.
- The monitoring probes the public address and alerts in Discord when it fails.
- The wiki page Public-Access: what is public and why, how the tunnel is installed step by step, and how to turn it off.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | An ADR records the choice, the alternatives and the personal data Cloudflare handles, with the reasons | Yes | No |
| 2 | From outside the tailnet (a GitHub runner), `https://nfhotel.devmatiker.com/` and `/book` answer 200 with a valid certificate, and `http://` redirects to `https://` | Yes | No |
| 3 | No port is opened in the home router, the `cloudflared` container publishes no port, and the firewall rules of the VM are unchanged | Yes | No |
| 4 | On the public host name, `/admin` and the API routes other than the two mobile routes answer 403 or 404; on the tailnet name the back office still works | Yes | No |
| 5 | On the public host name, the mobile app's sign-in and rooms routes answer as on the development stack (401 for a wrong password, 200 for the rooms with a staff token) | Yes | No |
| 6 | The VM runs with `ASPNETCORE_ENVIRONMENT=Production`, and an error page shows no stack trace | Yes | No |
| 7 | The monitoring probes the public address; a rule test shows that a failed probe alerts | Yes | No |
| 8 | The token is never in git, a log or an environment variable (`check-secret-env.sh`); CI is green and the code review against `qc-programming-shell` and `qc-yaml` is Go | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| ADR-0024 | The tunnel token is a secret file like the others |
| ADR-0015 | The mobile routes that become public |
| The Proxmox node | The deploy and criteria 2 to 7 need it online; it was offline on 2026-10-25 |
| S01 | Creates the tunnel in the Cloudflare dashboard and writes its token on the VM; the AI never sees the token |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| Objective 21: a guest signs in with the booking number and the email, now from the internet | Tasks 2 and 3 |
| Objective 26: staff work with the rooms from the mobile app, now away from the tailnet | Tasks 2 and 3 |
| Out of scope until now: "the public address and TLS of the API" for the mobile app need their own ADR | Task 1 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-08. The plan, the code and the tests in CI do not wait for the Proxmox node; the deploy and the checks of criteria 2 to 7 do, so the date moves if the node is not back by then.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | ADR for public access | Write the ADR: Cloudflare Tunnel compared with Tailscale Funnel and open ports; what Cloudflare sees; how `/admin` stays off the public name; how to turn the tunnel off. Amend BC-001, whose out-of-scope list says the public address needs its own ADR. | No | criterion 1 |
| 2 | Tunnel service | A `cloudflared` service in `ops/deploy/cloudflared`, pinned by digest, no port, on the proxy network only, reading its token from a secret file; started by `deploy-services.sh` only when the token exists; tests with a fake. | No | criteria 3, 8 |
| 3 | Public server in nginx | A server block for `nfhotel.devmatiker.com` that serves the site and the two mobile routes and refuses `/admin` and the other API routes; tests in `test-nginx.sh`. | No | criteria 4, 5 |
| 4 | Production environment | Switch the settings of the VM to `Production`, check what changes (error pages, development-only features), and prove on the throwaway stack that an error shows no stack trace. | No | criterion 6 |
| 5 | Public probe and alert | A blackbox probe of the public address in the monitoring, an alert with a rule test, and a runbook row. | No | criterion 7 |
| 6 | Wiki page Public-Access | What is public and why, the tunnel installed step by step (S01 writes the token with a command that keeps it out of the history), the Cloudflare settings (SSL mode, a rate-limit rule on sign-in), and how to turn it off. | No | criterion 1 |
| 7 | Check and review | S01 creates the tunnel and writes the token; deploy; prove the criteria from outside the tailnet; reviews. | No | criteria 2 to 8 |

---

[BC-001]: ../business-case.md
[ADR-0006]: ../adr/adr-0006-tls.md
[ADR-0009]: ../adr/adr-0009-tls-certificates.md
[ADR-0015]: ../adr/adr-0015-mobile-app-staff-token-access.md
[ADR-0024]: ../adr/adr-0024-secrets-as-files.md
[ADR-0025]: ../adr/adr-0025-recovery-objectives.md
[7e61d06]: https://github.com/NF-Hotel/NFHotel/commit/7e61d06e11643d9ba346473ff12249a9a5cd63d5
