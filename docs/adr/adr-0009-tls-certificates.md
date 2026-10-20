# ADR-0009 TLS certificates for the tailnet and the public name

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0009 |
| CrossReference | [ADR-0006], [ADR-0007], [MIL-021], [MIL-023], [DSA-001] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-18 | Deprecated | mikemike | S01; S03 | Initial version, decision D10 of DSA-001 | [064c27c] |
| 2026-10-18 | Accepted | mikemike | AI as stand-in for S01 | Accepted after RC-096 Go | [064c27c] |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

ADR-0006 put TLS in nginx with a self-signed development certificate, and left open how a trusted certificate is issued and renewed (decision 4). DSA-001 made this decision D10.

Two facts shape the choice:

- **The tailnet name already has a trusted certificate.** The host `nf-hotel` serves a Let's Encrypt certificate for `nf-hotel.taildb84b5.ts.net`, valid to 2027-01-04 (DSA-001 section 2.4.1). That certificate comes from `tailscale cert`.
- **No public host name exists.** The project does not control public DNS for NF Hotel yet, so no certificate can be issued for a public name today.

Options considered:

1. **Self-signed only.** No trust in browsers, and HSTS cannot be turned on. Acceptable for development only.
2. **`tailscale cert` for the tailnet name.** Let's Encrypt, issued through Tailscale with no open port. The name is reachable only inside the tailnet, unless Tailscale Funnel is used.
3. **certbot with the HTTP-01 webroot challenge** through the existing nginx. Needs a public name and port 80 reachable from the internet; renewed by a timer.
4. **certbot with a DNS-01 challenge.** No inbound port, but it needs an API token for the DNS provider, which the project does not have.
5. **A purchased certificate.** Manual renewal and a cost, with no benefit over Let's Encrypt here.

## Decision

We use `tailscale cert` for the tailnet name now, and certbot with the HTTP-01 webroot challenge for a public name once one exists. The VM is not exposed to the internet before that.

1. **Tailnet name.**
   - `tailscale cert` writes the certificate and key for `<host>.taildb84b5.ts.net` into the git-ignored `deploy/nginx/tls/` that ADR-0006 already mounts read-only.
   - A systemd timer renews them every month and reloads nginx when they change.
   - `NGINX_SERVER_NAME` is set to the tailnet name, and HSTS is on (`NGINX_HSTS=on`), because the certificate is trusted.
2. **Public name, later.**
   - certbot runs on the VM with `--webroot`, and nginx serves `/.well-known/acme-challenge/` from a read-only mount. This path is the exception for a certificate authority that ADR-0006 decision 2 already allows.
   - Renewal uses certbot's own timer, with a deploy hook that copies the files into `deploy/nginx/tls/` and reloads nginx.
3. **No private key in git** (unchanged from ADR-0006).
4. **The smoke test of CD** stays at "HTTPS answers 200". It adds a certificate check (`curl` without `--insecure`) once the trusted certificate is installed.

## Consequences

**Positive:**

- Browsers inside the tailnet trust the site today, with no open port and no DNS token, and HSTS can be turned on.
- The public path reuses nginx and needs no new service.

**Negative:**

- The tailnet certificate depends on Tailscale. If Tailscale stops issuing, the site falls back to the last certificate until it expires (90 days).
- HTTP-01 needs port 80 open to the internet for the public name, which widens the exposure that MIL-023 must firewall.
- Two issuing paths to maintain once a public name exists.

**Quality:** security (trusted transport, HSTS) and usability (no certificate warnings) improve (ISO/IEC 25010:2023).

## Affected Artifacts

- [ADR-0006]: decision 4 is decided here; the rest of ADR-0006 is unchanged.
- [MIL-021]: the CD smoke test adds a certificate check once a trusted certificate is installed.
- [MIL-023]: the renewal timers and the firewall for port 80 are part of the hardening.

---

[ADR-0006]: https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0006-tls.md
[ADR-0007]: ./adr-0007-cicd-and-supply-chain.md
[MIL-021]: ../milestones/mil-021-secure-cicd-foundation.md
[MIL-023]: ../milestones/mil-023-operational-quality.md
[DSA-001]: ../sqa/devsecops-assessment.md
[064c27c]: https://github.com/NF-Hotel/NFHotel/commit/064c27c1d90793a6f3782e61cf4a91a4b96eba84
