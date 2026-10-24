# MIL-033 Security headers and cookie flags

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-033 |
| CrossReference | [BC-001], [MIL-017], [MIL-027], [ADR-0006], [ADR-0016] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-24 | Proposed | mikemike | S01 | Initial version, from a draft of 2026-10-20 that was never committed (it was numbered MIL-028, a number since given to the run API of the market analysis); the operations live in `ops/` of this repository since ADR-0016, so one pull request carries the work | [fb7531c] |

---

## Purpose

Decide whether NF Hotel may claim that its pages are served with a strict set of browser security headers and that no cookie leaves the browser in clear text, proven by the DAST scans that MIL-027 added.

Production answered on 2026-10-24 with `Strict-Transport-Security` and `Server: nginx/1.29.8`, and no other security header. The DAST scans of MIL-027 pass with 0 High alerts, but report Medium and Low alerts on every page:

| ZAP rule | Alert | Cause |
| --- | --- | --- |
| 10055 | CSP: wildcard directive, `unsafe-inline`, a directive with no fallback | No full Content Security Policy is sent; without `default-src`, every source is allowed |
| 10021 | X-Content-Type-Options header missing | Not set by the web application or nginx |
| 10063 | Permissions-Policy header not set | Not set |
| 90004 | Cross-Origin-Embedder-Policy, -Opener-Policy and -Resource-Policy missing | Not set |
| 10011 | Cookie without the Secure flag | The antiforgery cookie `.AspNetCore.Antiforgery.*` keeps the default `SameAsRequest`, and the request reaches the web application over HTTP behind the TLS proxy |
| 10036 | Server leaks its version (`nginx/1.29.8`) | nginx `server_tokens` is on |
| 40025 | Proxy disclosure (full scan) | nginx answers with its name and version |

Each alert is either fixed, or accepted with a reason and an expiry, so that a new one stands out in the next scan.

## Deliverable

- **Security headers from the web application.** A middleware in `NFHotel.Web` sets the headers on every response, pages and static files alike. The application knows its own pages, and an integration test can check the headers without nginx.
  - A Content Security Policy with `default-src 'self'` and no `unsafe-inline`, `unsafe-eval` or wildcard source. It names `script-src`, `style-src`, `img-src`, `font-src` and `connect-src` (the Blazor websocket is same origin), and the directives with no fallback: `object-src 'none'`, `base-uri 'self'`, `form-action 'self'` and `frame-ancestors 'none'`.
  - `X-Content-Type-Options: nosniff`, a `Permissions-Policy` that turns off the browser features the site does not use, `Cross-Origin-Opener-Policy: same-origin`, `Cross-Origin-Embedder-Policy: require-corp` and `Cross-Origin-Resource-Policy: same-origin`.
- **No inline code in the pages.** Blazor Server must keep working under that policy.
  - The inline import map (`<ImportMap />`) is removed, or allowed by its hash, since no page loads a JavaScript module.
  - The reconnect dialog gets its own component and stylesheet, because the default one that `blazor.web.js` injects uses an inline style.
- **Secure antiforgery cookie.** Behind the TLS proxy (`Tls:Require`, ADR-0006), the antiforgery cookie is always `Secure` and `HttpOnly`, like the staff cookie.
- **nginx hides its version.** `server_tokens off` in `ops/deploy/nginx`, so the `Server` header says `nginx` only.
- **Rules file.** An alert that cannot be fixed without breaking a page is set to WARN in `ops/security/zap-rules.tsv`, with its reason and an expiry at most 30 days ahead, and S01 approves it in the pull request.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | The ZAP baseline, signed out and signed in, reports 0 alerts of rules 10055, 10021, 10063, 90004, 10011 and 10036, or each remaining one is a WARN line in `ops/security/zap-rules.tsv` with a reason and an expiry at most 30 days ahead | Yes | No |
| 2 | The ZAP full scan, started by hand against the throwaway stack, reports 0 alerts of rules 10036 and 40025, or each remaining one is accepted as in criterion 1 | Yes | No |
| 3 | An integration test reads the headers of a page and of a static file. The policy has `default-src`, `object-src`, `base-uri`, `form-action` and `frame-ancestors`, and 0 `unsafe-inline`, `unsafe-eval` or `*` sources. The five other headers have the values in the Deliverable | Yes | No |
| 4 | An integration test behind `Tls:Require` gets the antiforgery cookie with `Secure` and `HttpOnly` | Yes | No |
| 5 | The end-to-end tests are green, and each one fails on a Content Security Policy violation in the browser console. The guest booking flow and every back office page they open still work over the Blazor websocket | Yes | No |
| 6 | `ops/tests/test-nginx.sh` checks that the `Server` header is `nginx` with no version | Yes | No |
| 7 | CI is green, production answers with the headers after the CD run, and the reviews against `qc-programming-csharp` and `qc-programming-shell` have the verdict `Go` | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-017 | The TLS proxy, `Tls:Require` and the cookie flags (ADR-0006) that the antiforgery cookie follows |
| MIL-027 | The end-to-end tests, the throwaway stack and the DAST scans that find the alerts and prove the fix |
| ADR-0016 | nginx, the scan and the rules file are in `ops/` of this repository, so one pull request carries the code and the operations |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| Success criterion 23 (TLS at the public port): no cookie in clear text, the antiforgery cookie as well as the staff cookie | Task 3 |
| Success criterion 22 (back office login): the back office pages keep working under the stricter policy | Tasks 2, 5 |
| MIL-027 criterion 3: the DAST baseline reports on the pages that a real guest and a real member of staff see | Tasks 1, 4, 6 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-07, two weeks. It needs no manual change on the VM: CD deploys the web application and the nginx configuration.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Security headers middleware | Add a middleware in `NFHotel.Web/Security`, registered in `Program.cs` before the static files, that sets the Content Security Policy and `X-Content-Type-Options`, `Permissions-Policy`, COOP, COEP and CORP on every response. Integration tests in `NFHotel.Web.Tests` check a page and a static file (criterion 3). | No | ZAP 10055, 10021, 10063, 90004 |
| 2 | Blazor pages without inline code | Remove `<ImportMap />` from `App.razor` (or allow it by hash) and add a reconnect component with its own stylesheet, so that `script-src 'self'` and `style-src 'self'` hold. Check every page for a `style` attribute or an inline script. | No | ZAP 10055 |
| 3 | Secure antiforgery cookie | Set the antiforgery cookie to `Secure` (always behind `Tls:Require`) and `HttpOnly`, as the staff sign-in does for its cookie. An integration test behind `Tls:Require` checks the flags (criterion 4). | No | ZAP 10011, ADR-0006 |
| 4 | nginx without its version | Set `server_tokens off` for every server of `ops/deploy/nginx`, and extend `ops/tests/test-nginx.sh` to check the `Server` header (criterion 6). | No | ZAP 10036, 40025 |
| 5 | No CSP violation in the browser | The end-to-end tests fail on any `Content Security Policy` message in the browser console, and still pass for the guest booking flow and the back office pages (environment: Docker container). | No | |
| 6 | Scans and rules file | Run `ops/scripts/dast-scan.sh baseline`, signed out and signed in, and the full scan by hand against the stack. Fix what remains, or add a WARN line with a reason and an expiry to `ops/security/zap-rules.tsv` for S01 to approve (criteria 1, 2). | No | |
| 7 | Check and review | Prove criteria 1 to 7 with the scan reports, the CI runs and the headers in production after CD. Review records against `qc-programming-csharp` and `qc-programming-shell`. | No | |

---

[BC-001]: ../business-case.md
[MIL-017]: ./mil-017-tls.md
[MIL-027]: ./mil-027-dast-and-end-to-end-tests.md
[ADR-0006]: ../adr/adr-0006-tls.md
[ADR-0016]: ../adr/adr-0016-one-repository-for-application-and-operations.md
[fb7531c]: https://github.com/NF-Hotel/NFHotel/commit/fb7531cdd2ebddca9554fe33a83153b57e66d7c2
