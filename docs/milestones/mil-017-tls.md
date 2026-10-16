# MIL-017 TLS at the public port

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-017 |
| CrossReference | [BC-001], [MIL-016], [ADR-0005], [ADR-0003] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-17 | Proposed | Claude (AI) | S01 | Initial version | [d94daf4] |
| 2026-10-17 | Accepted | Claude (AI) | AI as stand-in for S01 | Accepted after RC-079 Go | pending |

---

## Purpose

Decide whether the sign-in of the back office may be used over a network that is not trusted. Since MIL-016 a member of staff types a password into the public port, but nginx serves plain HTTP: the password and the session cookie cross the network in clear text, and the cookie cannot be marked `Secure`. ADR-0005 names this as the first open item. This milestone puts TLS in front of the public port, makes HTTP redirect to HTTPS, and makes the cookie `Secure`, so that the one service that the internet reaches is also private on the wire.

## Decisions to record in ADR-0006

| # | Decision | Why |
| --- | --- | --- |
| 1 | nginx terminates TLS on port 443 of the VM (TLS 1.2 and 1.3 only, no old ciphers); the web application stays on plain HTTP inside the Docker network `nfhotel-proxy`, which is not reachable from outside | One place holds the certificate and its key; the application and the API need no change of address |
| 2 | Port 80 only redirects to the same address on HTTPS (301), except the path that a certificate authority needs for a challenge, if one is used | No page and no sign-in is served in clear text |
| 3 | The certificate and key are files in a git-ignored folder `deploy/nginx/tls/` that is mounted read-only into nginx. A script makes a self-signed certificate for the development VM; for a real host name the owner puts a certificate from a public authority in the same files | The repository holds no key; the same nginx configuration serves a development certificate and a real one |
| 4 | Certificates from a public authority (Let's Encrypt or a purchased one) are issued and renewed outside these scripts for now; the options (certbot on the VM, a DNS-challenge client, a purchased certificate) are recorded with their trade-offs | Issuing needs a public host name and DNS that the project does not control yet; the decision of the owner (S01, S03) |
| 5 | The server sends `Strict-Transport-Security` (one year) only when the certificate is not self-signed, and `X-Forwarded-Proto` to the web application, which trusts the forwarded headers of the proxy network only | HSTS on a self-signed development certificate would lock a browser out; the application must know that the request was HTTPS |
| 6 | The staff cookie is always `Secure`, and the web application redirects any request that arrives as HTTP to HTTPS | Closes the open item of ADR-0005; a cookie never travels in clear text |
| 7 | The internal listener on 8081 (analysis, benchmark) stays on the loopback of the VM and is not changed | Not reachable from outside; reached through an ssh tunnel |

## Deliverable

- ADR-0006 recording the decisions above, reviewed.
- nginx: a `listen 443 ssl` server for the web application, a port 80 server that redirects, the TLS settings in a snippet, the certificate folder mounted read-only, port 443 published by the compose file (80 stays published for the redirect).
- `scripts/make-dev-certificate.sh` that creates the self-signed certificate and key in `deploy/nginx/tls/` (git-ignored), with the host name and the IP of the VM in the subject alternative names; `deploy-services.sh` runs it when no certificate exists and the host is a development host.
- The web application: forwarded headers trusted from the proxy network, `Secure` cookie, redirect to HTTPS, HSTS only when told to.
- The smoke test and the wiki: HTTP redirects, HTTPS answers, the cookie is `Secure`, the weak protocols are refused.
- Tests: a web test that a request marked `X-Forwarded-Proto: http` is redirected and that the cookie has the `Secure` attribute; the existing sign-in tests keep passing over the forwarded HTTPS.
- Unchanged: the services behind nginx, the API, the database and the network rules of ADR-0004.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | In the deployed VM `http://<vm>/anything` answers 301 to the same path on `https://`, and no page of the application is served on port 80 | Yes | No |
| 2 | `https://<vm>/` answers 200 with the development certificate; `openssl s_client` shows TLS 1.3 or 1.2, and TLS 1.1 and 1.0 are refused | Yes | No |
| 3 | The staff cookie set at the sign-in has the `Secure` and `HttpOnly` attributes, and a browser (or curl with the CA) can sign in over HTTPS end to end | Yes | No |
| 4 | No private key or certificate is in git; `git check-ignore deploy/nginx/tls/*` is true and the folder is mounted read-only | Yes | No |
| 5 | `Strict-Transport-Security` is absent with the self-signed certificate and present with `NGINX_HSTS=on` | Yes | No |
| 6 | From outside the VM only ports 80 and 443 of nginx answer; the API, PostgreSQL and the web container still publish nothing | Yes | No |
| 7 | The internal listener on 8081 is unchanged and still bound to the loopback only | Yes | No |
| 8 | `dotnet build` has 0 warnings, format is clean, all tests pass; `qc-programming-csharp`, `qc-programming-shell` and `qc-yaml` reviews have the verdict `Go`; ADR-0006 has a `Go` review | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-016 | The sign-in and its cookie are what TLS protects; the `Secure` flag changes the cookie of MIL-016 |
| MIL-004 | The nginx compose file and templates that are extended |
| ADR-0003, ADR-0005 | The network picture and the open item this milestone closes |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| New objective 20 of an amended BC-001: the one service that the internet reaches is served only over TLS | All tasks |
| Success criterion 23 | Criteria 1 to 3 |
| ADR-0005 negative consequences and RC-076 action item 1 | Tasks 2 to 5 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01; S03 for the certificate, the ports and the deploy changes |

## Target Date

2027-03-05 — two weeks after MIL-016 (2027-02-19): one proxy configuration, one script, a small change of the web application and a deploy check.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Amend Business Case and Project Plan | Add objective 20, the scope items, success criterion 23 and the MIL-017 schedule row; review again. | No | |
| 2 | Record the decisions in ADR-0006 | The seven decisions above, with the options for the certificate (self-signed only, certbot on the VM, a DNS-challenge client, a purchased certificate) and for where to terminate TLS (nginx, the web application). | No | |
| 3 | Terminate TLS in nginx | The 443 server, the redirect on 80, the TLS snippet, the mounted certificate folder, the published port, HSTS by setting. | No | |
| 4 | Development certificate script | `scripts/make-dev-certificate.sh`, called by the deploy when no certificate exists; the folder is git-ignored. | No | |
| 5 | Web application behind the proxy | Trust the forwarded headers of the proxy network only, redirect HTTP to HTTPS, `Secure` cookie; tests. | No | |
| 6 | Smoke test, wiki and deploy | HTTP redirect, HTTPS answer, protocol and cookie checks in the smoke test; the wiki page; open the certificate options for the owner. | No | |
| 7 | Prove it in the VM | Deploy to the Proxmox VE container; check criteria 1 to 3, 5 and 6 and record the commands. | No | |
| 8 | Check and review | Build, format, test; reviews against `qc-programming-csharp`, `qc-programming-shell`, `qc-yaml` and `qc-adr`. | No | |

---

[BC-001]: ../business-case.md
[MIL-016]: ./mil-016-backoffice-login.md
[ADR-0005]: ../adr/adr-0005-backoffice-login.md
[ADR-0003]: ../adr/adr-0003-proxmox-vm-for-services.md
[d94daf4]: https://github.com/NF-Hotel/NFHotel/commit/d94daf4e834d5d1b03a97f63e81b50caf725bb98
