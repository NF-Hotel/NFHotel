# ADR-0006 TLS at the public port

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0006 |
| CrossReference | [BC-001], [MIL-017], [ADR-0005], [ADR-0003], [ADR-0009], [ADR-0016], [RC-161] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-17 | Deprecated | Claude (AI) | AI as stand-in for S01 | Accepted after RC-080 Go | [065c6e8] |
| 2026-10-21 | Accepted | mikemike | AI as stand-in for S01 | Paths under ops/ and the later certificate decision ADR-0009 (RC-161) | pending |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

Since [ADR-0005] a member of staff types a password into the public port, and nginx serves plain HTTP there: the password and the session cookie cross the network in clear text, and the cookie cannot be marked `Secure`. ADR-0005 and RC-076 name this as the first open item.

Forces: only nginx faces the internet ([ADR-0003]); the web application and the API sit on Docker networks that nothing outside the VM can reach; the project has no public host name or DNS yet, so a certificate from a public authority cannot be issued by these scripts today; the repository must hold no key; one nginx configuration should serve a development certificate and a real one.

Options considered for where to end TLS:

1. **In the web application** (Kestrel with a certificate). The key would sit in the process that handles all page code, and the API and the other services would still need their own answer.
2. **In nginx, with files for the certificate** and the application on plain HTTP inside the Docker network. One place holds the key and the configuration.
3. **In a cloud or router in front of the VM.** Outside the project; not available for the development VM.

Options considered for the certificate: only a self-signed one; certbot on the VM (HTTP challenge on port 80); a DNS-challenge client; a purchased certificate.

## Decision

We follow option 2.

- **Termination.** nginx listens on 443 with TLS 1.2 and 1.3 only and a modern cipher list. The web application stays on plain HTTP on the network `nfhotel-proxy`, with `X-Forwarded-Proto: https` set by nginx.
- **Redirect.** Port 80 answers only `301` to the same path on HTTPS. No page and no sign-in is served in clear text. The redirect keeps a non-standard HTTPS port when one is set (`NGINX_REDIRECT_PORT`).
- **Certificate files.** `ops/deploy/nginx/tls/fullchain.pem` and `privkey.pem`, git-ignored and mounted read-only. `ops/scripts/make-dev-certificate.sh` makes a self-signed pair with the host name and the addresses of the VM in the alternative names, and `deploy-services.sh` runs it when the pair does not exist. A certificate from a public authority is put in the same two files; the nginx configuration does not change. nginx does not start without the files, so the deploy never serves HTTP by accident.
- **Issuing and renewing** a public certificate is not automated here. It needs a public host name and DNS that the owner (S01, S03) has not decided; when they have, the choice between certbot on the VM, a DNS-challenge client and a purchased certificate is a new ADR.
- **HSTS.** `Strict-Transport-Security` is sent only when `NGINX_HSTS` holds a value; it is empty for the development certificate, because a browser that has seen HSTS for a self-signed certificate cannot be let past the warning.
- **Web application.** With `Tls__Require=true` (set in the env file of the deployment) it redirects any request that arrives as HTTP, after the forwarded headers, to HTTPS, and the staff cookie is always `Secure`. Without the setting (local development) it behaves as before and the cookie is `Secure` when the request is HTTPS.
- **Internal listener.** Port 8081 for the analysis and benchmark services is unchanged and stays on the loopback of the VM.

**Current state (2026-10-21, [RC-161]).** The later decision of the fourth point is [ADR-0009]. `nf-hotel` serves a Let's Encrypt certificate from `tailscale cert` for `nf-hotel.taildb84b5.ts.net`, valid to 2027-01-04, and HSTS is on (`Strict-Transport-Security: max-age=2592000`). The files are under `ops/` since [ADR-0016].

## Consequences

**Positive:**

- The password, the cookie and every page of the public service are private on the wire; the open item of ADR-0005 is closed.
- The key is in one place, outside git, and the same configuration works for a development and a real certificate.
- A deploy without a certificate fails loudly instead of serving HTTP.

**Negative:**

- A self-signed certificate makes a browser warn; the staff must trust it once or the owner must install a real certificate before the service goes to real users.
- A real certificate must be renewed by hand until a later ADR automates it; an expired certificate stops the service.
- Port 443 must be reachable from the internet in addition to port 80 (router, firewall), which is outside these scripts.
- The hop from nginx to the web application is plain HTTP; it is inside the VM on an internal network, which is accepted.

Quality attributes: security improves (confidentiality of credentials and session on the wire); availability depends on the certificate not expiring; operability gets one more file to look after.

## Affected Artifacts

- [MIL-017] — decided here; its tasks build and deploy it
- [ADR-0005] — closes its first negative consequence
- [ADR-0003] — adds port 443 to the picture of exposed ports
- [BC-001] — objective 20

---

[BC-001]: ../business-case.md
[MIL-017]: ../milestones/mil-017-tls.md
[ADR-0005]: ./adr-0005-backoffice-login.md
[ADR-0003]: ./adr-0003-proxmox-vm-for-services.md
[065c6e8]: https://github.com/NF-Hotel/NFHotel/commit/065c6e85054e7b24dc5848e241038ede6d747f49
[ADR-0009]: ./adr-0009-tls-certificates.md
[ADR-0016]: ./adr-0016-one-repository-for-application-and-operations.md
[RC-161]: ../sqa/reviews/rc-161-adr-refresh.md
