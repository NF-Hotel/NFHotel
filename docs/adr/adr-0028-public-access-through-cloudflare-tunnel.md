# ADR-0028 NF Hotel on the public internet through Cloudflare Tunnel

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0028 |
| CrossReference | [ADR-0006], [ADR-0009], [ADR-0015], [ADR-0024], [ADR-0025], [MIL-040] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-25 | Deprecated | mikemike | AI as stand-in for S01 | Initial version; written by the AI in autonomous mode under MIL-040, which S01 accepted in chat on 2026-10-25; the choice of Cloudflare Tunnel and the domain are S01's | [21ba03e] |
| 2026-10-25 | Accepted | mikemike | AI as stand-in for S01 | Decision 3 changed by ADR-0029: `/admin` on the public name behind Cloudflare Access (MIL-041) | pending |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

**Who decided what.** S01 asked on 2026-10-25 for the site to be public on the internet in a safe way with a free certificate, chose Cloudflare Tunnel after the options below were shown, and bought the domain `devmatiker.com` at Cloudflare. The design of this record was written by the AI assistant.

Until now the site is reachable only from the tailnet (ADR-0009): its certificate comes from `tailscale cert` for `nf-hotel.taildb84b5.ts.net`, and ADR-0009 kept the VM off the internet until a public name existed. A guest without Tailscale cannot book, and the mobile app reaches the API only on the development stack (ADR-0015, where the public address was left to a later ADR). The VM stands on the Proxmox node in a private home (ADR-0025).

Options considered:

1. **Open ports 80 and 443 in the home router,** with a free name (DuckDNS) and certbot. Free, but the home address becomes public, every scanner on the internet reaches nginx directly, and nothing stands between a flood of requests and the home connection.
2. **Tailscale Funnel.** No open port and no domain: Tailscale relays the public traffic into the tailnet to the VM, and the certificate stays the one of `tailscale cert`. Free, and TLS ends on the VM, so Tailscale does not see the content. It gives no protection against a flood of requests and no filter of known attacks, and the address stays a `ts.net` name.
3. **Cloudflare Tunnel.** A `cloudflared` container on the VM makes an outbound connection to Cloudflare, which serves the public name. No open port, the home address stays hidden, the certificate is free and renewed by Cloudflare, and Cloudflare filters floods and known attacks before they reach the home. It needs a domain at Cloudflare, and Cloudflare ends TLS, so it sees the requests in clear.

## Decision

We follow option 3.

1. **The public name** is `nfhotel.devmatiker.com`, not the bare domain, so the domain stays free for other uses.
2. **The path.** `cloudflared` (`ops/deploy/cloudflared`, pinned by digest, no published port, on the proxy network only) forwards the requests for the public name to a separate listener of nginx, port 8082 on the proxy network (`templates/public.conf.template`). The port is not published on the host. The tailnet server on 443 does not change.
3. **What is public.** The site, the guest booking, and the two routes of the mobile app (`/mobile-api/api/auth/login` and `/mobile-api/api/rooms`, ADR-0015). The back office answers 404 on the public name and stays on the tailnet name; every other path of `/mobile-api/` answers 404, and the API has no other public path. *Why the back office stays off:* a staff sign-in that strangers cannot see cannot be guessed at; Cloudflare Access could add a login in front of it later, but the tailnet already gives that. **Changed by ADR-0029 (2026-10-25):** `/admin` is now on the public name behind Cloudflare Access.
4. **The client.** nginx takes the client's address from `CF-Connecting-IP` (trusted only from the Docker networks), so the per-address sign-in limit of the API sees the guest, not the tunnel. It reports `X-Forwarded-Proto: https`, because the client spoke HTTPS to Cloudflare; with the scheme of the plain hop, the web application would redirect for ever.
5. **The token** is a secret file (ADR-0024): S01 writes it once into `cloudflared.env` on the VM, the deploy writes it to `secrets/TUNNEL_TOKEN`, and `cloudflared` reads it with `--token-file`. It is never in git, a log or an environment variable, and `check-secret-env.sh` checks the tunnel when it runs.
6. **Production.** The deploy of a release runs the API and the web application as `Production` whatever the settings files say, so an error shows the error page and never a stack trace.
7. **Optional and safe to lose.** Without `cloudflared.env` the deploy leaves the tunnel out and the site stays on the tailnet. A tunnel that does not turn healthy only warns: the hotel keeps running on the tailnet, and `PublicSiteDown` alerts.
8. **Cloudflare settings,** set by S01 in the dashboard and written down in the wiki page Public-Access: the public host name pointing to `http://nginx:8082`, SSL mode Full, "Always Use HTTPS", and a rate-limiting rule on the sign-in paths.
9. **Personal data (GDPR).** Cloudflare ends TLS and so processes what guests send (name, e-mail, phone, passport number) as a processor under its Data Processing Addendum, which applies to every Cloudflare account. It stores no page content; its logs hold addresses and paths. The hotel names Cloudflare as a processor in its privacy notice.
10. **Turning it off.** Removing `cloudflared.env` and deploying, or pausing the tunnel in the dashboard, takes the site off the internet at once and leaves the tailnet unchanged.

## Consequences

**Positive:**

- Guests book from anywhere, and the mobile app works away from the tailnet, without a single port opened at home.
- Floods and known attacks are filtered by Cloudflare before they reach the home connection.
- The certificate of the public name is free and renewed by Cloudflare; the tailnet certificate of ADR-0009 stays as it is.

**Negative:**

- Cloudflare sees the requests in clear, including personal data; that is a processor relation the hotel must name.
- The public site depends on Cloudflare and on the home connection; the tailnet does not.
- The listener on port 8082 trusts the client address that Cloudflare sends; another container on the proxy network could send a false one.

## Affected Artifacts

- [ADR-0009]: decision "the VM is not exposed to the internet before a public name exists" is met: the name exists, and the VM is still not exposed, the tunnel is.
- [ADR-0015]: the public address of the mobile routes, which it left to a later ADR.
- [ADR-0024]: one more secret file.
- [MIL-040]: tasks 1 to 6.

---

[ADR-0006]: ./adr-0006-tls.md
[ADR-0009]: ./adr-0009-tls-certificates.md
[ADR-0015]: ./adr-0015-mobile-app-staff-token-access.md
[ADR-0024]: ./adr-0024-secrets-as-files.md
[ADR-0025]: ./adr-0025-recovery-objectives.md
[MIL-040]: ../milestones/mil-040-public-access.md
[21ba03e]: https://github.com/NF-Hotel/NFHotel/commit/21ba03eedb3836936f623ea3bd93871f097cab3c
