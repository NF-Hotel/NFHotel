# MIL-072 The Access check on every path to the back office

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-072 |
| CrossReference | [BC-001], [DSA-001], [ADR-0028], [ADR-0029], [MIL-040], [MIL-041], [MIL-051] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-11 | Accepted | mikemike | S01 | Initial version: the routes of the tunnel and the guard of nginx found on 2026-11-11 to let spellings of `/admin` with capital letters past the check of the Access token; planned at S01's request in chat ("5 a": the finding is fixed first) | pending |

---

## Purpose

Decide that every request for the back office from the public internet, in any spelling of `/admin`, passes Cloudflare Access before it reaches the virtual machine (VM), as decisions 2 to 4 of [ADR-0029] intend.

On 2026-11-11, while it prepared a closed lab of the public access path, the artificial intelligence (AI) assistant read the remote configuration of the tunnel in the log of `cloudflared`, which Loki keeps. It reproduced what it saw offline, with the `cloudflared` image of production (2026.10.7, the same digest) and with nginx 1.29 and the template of main, and reported the findings to S01 in chat the same day. Nothing was tested against production.

| # | Finding | Where | What it allows or breaks |
| --- | --- | --- | --- |
| 1 | The route without a path comes first in the list of published routes, and `cloudflared` takes the first route that matches. The route with the path `^/admin`, which validates the Access token ([ADR-0029], decision 2), therefore never takes a request: `cloudflared tunnel ingress rule` with the same rules in the same order sends `/admin/login` to the route without a path | The published routes of the tunnel `nf-hotel`, managed in the Cloudflare dashboard and not in git | Decision 2 is not in effect. Only Access at the edge and the header check of nginx stand in front of the back office, and nginx checks only that the header `Cf-Access-Jwt-Assertion` is there, not that the token is valid ([ADR-0029], decision 3) |
| 2 | The path `^/admin` of the validating route and `location ^~ /admin` in `ops/deploy/nginx/templates/public.conf.template` compare letter case. In the closed lab, with a stand-in for the web application, `/ADMIN/login`, `/Admin/login` and `/aDmin/login` reached the stand-in without the header; `/admin/login`, `//admin/login`, `/%61dmin/login`, `/admin%2Flogin` and `/./admin/login` got 404. With the validating route moved first but its path unchanged, `/ADMIN/login` still took the route without a path | The validating route, and the public listener of nginx (port 8082) | ASP.NET Core matches routes without regard to letter case by default, so the web application would answer such a spelling with the staff sign-in page once a request gets past the edge |
| 3 | Unknown: whether the path `admin` of the Access application at the edge compares letter case. If it does, a stranger who asks for `/ADMIN/login` reaches the staff sign-in page without the one-time PIN of Access | The Access application NF Hotel back office | The hotel's own sign-in, the roles and the per-address limit of the sign-in still stand; what is lost is the first of the two locks of [ADR-0029], decision 4 |

The order matters, and the repository is public. Task 1 is a change in the dashboard and closes findings 1 and 2 at the tunnel for every spelling, so it comes first, at once after this document is accepted. Until it has passed its check, this document, the test and the fix of nginx stay local and are not pushed, so the finding is not described in public while the tunnel still lets it through.

## Deliverable

- **The routes of the tunnel.** The validating route first in the list, with the path `(?i)^/admin`, a regular expression of Go that ignores letter case, so `cloudflared` validates the Access token for every spelling of `/admin` before the request reaches the VM. The closed lab with the rules in that order: `cloudflared tunnel ingress validate` accepts them; `/admin/login`, `/ADMIN/login`, `/Admin/login`, `/aDmin/login`, `/admin` and `/administrator` take the validating route; `/book`, `/mobile-api/api/rooms` and `/` take the route without a path. The routes stay in the dashboard, so the wiki page Public-Access holds the steps and the check.
- **The second line of nginx for every spelling.** `location ~* ^/admin` instead of `location ^~ /admin`: nginx compares the normalised path without regard to letter case. In the closed lab every one of ten spellings without the header got 404 (`/admin/login`, `/ADMIN/login`, `/Admin/login`, `/aDmin/login`, `//admin/login`, `/%61dmin/login`, `/%41DMIN/login`, `/admin%2Flogin`, `/./admin/login`, `/administrator`); with the header, `/admin/login` and `/ADMIN/login` reached the stand-in, and `/book` was unchanged. nginx still only checks that the header is there; the token is validated by the tunnel.
- **A regression test** in `ops/tests/test-nginx.sh`, which Ops CI already runs: spellings with capital letters get 404 without the header and reach the web application with it. With the template of today the test fails.
- **ADR-0029 made precise.** A Version History row: decision 2 with the validating route first and the path `(?i)^/admin`, decision 3 for every spelling of `/admin`, and decision 5 with the order of the routes checked before a deploy.
- **The deploy** of the fix through continuous deployment (CD) after S01's approval, with a check inside the VM and the checks from outside again.
- **Not part of this milestone:** a recurring probe that would notice a later change of the routes in the dashboard. The public probe of the monitoring (job `public_site`) checks the site only; a probe of `/ADMIN/login` is a change of the monitoring, which is deployed by hand until [MIL-051] is built. S01 decides on it at the gate.

Order: the review of this document first (QC-MIL-001 and QC-LANG-001), then task 1, then tasks 2 to 4 on one branch with their pull request, pushed only after task 1 has passed; task 5 after S01 approves its CD run, and task 6 last.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | After task 1, the configuration that `cloudflared` writes into its log has the validating route first, with the path `(?i)^/admin` and the Access check on, and the route without a path after it | Yes | No |
| 2 | After task 1, from outside the tailnet and without an Access session, `/admin/login`, `/ADMIN/login`, `/Admin/login`, `/aDmin/login` and `/%41DMIN/login` on the public name answer with Cloudflare's sign-in page or 403, never with the hotel's sign-in page; signed in to Access, S01 reaches the hotel's sign-in at `/admin/login`; `/book` answers 200, and the public probe stays green | Yes | No |
| 3 | The regression test fails with the template of 1c2726b and passes with the fix, and Ops CI runs it | Yes | No |
| 4 | After the deploy, inside the VM, the public listener of nginx answers 404 to `/ADMIN/login` without the header, and the checks of criterion 2 still hold | Yes | No |
| 5 | ADR-0029 and the wiki page Public-Access say what the deliverable says, and `test-wiki-coverage.sh` passes | Yes | No |
| 6 | The code review against `QC-SH-001` is Go, `lint.sh` is clean, and every required check of the pull request is green | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-040 | The tunnel and the public listener of nginx ([ADR-0028]) |
| MIL-041 | The Access application, the validating route and the header check of nginx, which this milestone puts in effect for every spelling of `/admin` |
| ADR-0029 | Decisions 2, 3 and 5, made precise here |
| DSA-001 | A finding is fixed, or accepted by S01 |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001 objective 19: the back office available only to signed-in staff | Criteria 2 and 4: no spelling of `/admin` reaches the hotel's sign-in from the internet without Access |
| BC-001 success criterion 22: back office login | Criteria 2 and 4: the two locks of ADR-0029, decision 4, hold for every spelling |
| BC-001 objective 28 and success criterion 31: the hotel on the public internet through Cloudflare Tunnel | Criteria 1 and 2: the routes of the tunnel as ADR-0029 decides, and `/book` and the public probe unchanged |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-15. A short window, because finding 1 leaves the back office with one lock fewer until task 1; it does not move a date of the Business Case.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | The routes of the tunnel | First one GET each of `/admin/login` and `/ADMIN/login` on the public name without an Access session, keeping only the status code and which page answers, with no sign-in attempt; this settles finding 3. Then, in the Cloudflare dashboard, S01 (or the AI assistant in S01's browser, on S01's yes in chat) moves the validating route to the top of the list and sets its path to `(?i)^/admin`. Then the checks of criteria 1 and 2. Nothing of this milestone is pushed before this task passes. | No | criteria 1, 2 |
| 2 | The regression test | Written first: checks in `ops/tests/test-nginx.sh` that `/ADMIN/login`, `/Admin/login`, `/aDmin/login` and `/%41DMIN/login` get 404 from the public listener without the header `Cf-Access-Jwt-Assertion`, and that `/ADMIN/login` with it reaches the web application. With `location ^~ /admin` the first three reach the web application (the closed lab of 2026-11-11), so the test fails until task 3. | No | criterion 3 |
| 3 | The nginx fix and ADR-0029 | `location ~* ^/admin` instead of `location ^~ /admin` in `ops/deploy/nginx/templates/public.conf.template`, and its comment, so every spelling without the header gets 404. ADR-0029 gets a Version History row: the validating route first with the path `(?i)^/admin` (decision 2), the header check for every spelling (decision 3), and the order of the routes checked before a deploy (decision 5). | No | criteria 3, 5 |
| 4 | The wiki, code review and pull request | Public-Access: step 4 of the Access setup puts the validating route at the top of the list with the path `(?i)^/admin`, and says why; Check it adds a spelling with capital letters. The review record against `QC-SH-001` for the test, and against the criteria of this milestone for the template and ADR-0029, before the pull request, which closes one issue per task it completes. | No | criteria 5, 6 |
| 5 | The deploy and the checks | S01 approves the CD run of the merge. Then, over Tailscale Secure Shell (SSH) and without printing a secret, the AI assistant asks the public listener of nginx inside the VM for `/ADMIN/login` without the header (404), and repeats the checks of criterion 2 from outside. | No | criteria 2, 4 |
| 6 | Gate and review record | The gate review record against criteria 1 to 6, with the answers of task 1 before and after the change, S01's decision on the recurring probe, the row of MIL-072 in the traceability matrix, the Accepted row of this document, and the closing of the GitHub milestone. | No | criteria 1 to 6 |

---

[BC-001]: ../business-case.md
[DSA-001]: ../sqa/devsecops-assessment.md
[ADR-0028]: ../adr/adr-0028-public-access-through-cloudflare-tunnel.md
[ADR-0029]: ../adr/adr-0029-back-office-behind-cloudflare-access.md
[MIL-040]: ./mil-040-public-access.md
[MIL-041]: ./mil-041-admin-behind-cloudflare-access.md
[MIL-051]: ./mil-051-monitoring-continuous-deployment.md
