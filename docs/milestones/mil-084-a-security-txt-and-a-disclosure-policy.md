# MIL-084 A security.txt and a disclosure policy for the public site

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-084 |
| CrossReference | [BC-001], [ADR-0028], [ADR-0029], [ADR-0037], [MIL-040], [MIL-041], [MIL-066], [MIL-072], [MIL-078], [DSA-001] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-17 | Deprecated | mikemike | S01 | Initial version: the plan of a `security.txt` after RFC 9116 and its disclosure policy, which S01 asked for in chat on 2026-11-17; supervised mode. S01 accepted the first plan in chat the same day ("1. Ja"), named a mailbox for the Contact and left the wording of the policy to the AI<br>Amended the same day, before any commit, after S01 asked whether every well-known principle and standard was in it ("Det skal gøres rigtigt og ordentligt"): the texts the plan rests on are named, findings 7 to 13 and criterion 8 are new, the file is served on the public name only, the top-level address redirects, the field `Bug-Bounty` is added, and ADR-0048 gets a table of every requirement. S01 accepted the amended plan in chat on 2026-11-18 ("1. Ja") after RC-400 Go, and named no day on which the mailbox closes, so Expires is 180 days ahead<br>On 2026-11-19 S01 switched the milestone to autonomous mode in chat ("Ja, automode"; "Automode og deploy" in the session that builds it): the AI acts as stand-in for S01 where the plan names S01's look and S01's acceptance, opens and merges the pull request, and approves the run of CD that its own merge starts ("du må gerne godkende deploy") | [91b74ec] |
| 2026-11-19 | Accepted | mikemike | AI as stand-in for S01 | Gate passed: RC-404 Go on criteria 1 to 8; tasks 1 to 6 in pull request 834 (RC-402 Go; RC-403 Go-with-conditions, its condition met), deployed by the CD run 44497614288, which the AI approved at S01's words in chat ("du må gerne godkende deploy."; "Automode og deploy"); task 7: the look from outside, and the weekly look, changed in pull request 836 after its first run on GitHub (RC-405 Go) and run by hand on main (44512660870, passed); the CD run of pull request 836 waits until the VM can pull images again; mode autonomous, not independent | pending |

---

## Purpose

Decide that a person who finds a vulnerability in NF Hotel can see, at the address the standard names, where to report it and on which terms: `https://nfhotel.devmatiker.com/.well-known/security.txt`, a file after Request for Comments (RFC) 9116 that is served from git, and behind it a policy that names the scope and gives no permission to test.

The plan rests on these texts, read for it on 2026-11-17:

- RFC 9116: the file, its fields, its address and type, and its security considerations, among them that the file implies no permission to test (section 5.5).
- RFC 8615: the path `/.well-known/`, the control of who can write to it, and the headers of a resource under it (section 4).
- The registry of `security.txt` fields of the Internet Assigned Numbers Authority (IANA), last updated 2026-03-07: ten fields, among them `Bug-Bounty`.
- The Vulnerability Disclosure Cheat Sheet of the Open Worldwide Application Security Project (OWASP) and the page of the Vulnerability Disclosure Toolkit of the National Cyber Security Centre (NCSC) of the United Kingdom: what a policy says, and how a report is answered.

Named by those texts and used through them, but not read: RFC 3339 (the date), RFC 5646 (the language tags), RFC 3986 and RFC 6068 (the addresses), RFC 2142 (the name of a security mailbox), RFC 4880 (the signature), and the two standards that the International Organization for Standardization (ISO) sells: ISO/IEC 29147 on disclosure and ISO/IEC 30111 on the handling of a report. The plan claims conformance with RFC 9116 and with section 4 of RFC 8615 only, and ADR-0048 says for each of their requirements how it is met.

On 2026-11-17 the artificial intelligence (AI) assistant read the repository at 3b973c7 and asked the public site and the two addresses of GitHub:

| # | Finding | Where | What it means |
| --- | --- | --- | --- |
| 1 | The public site answers 404 at `/.well-known/security.txt` and at `/security.txt` | A request from outside the tailnet | A finder gets no address from the site itself |
| 2 | A policy exists. `SECURITY.md` asks for a private report through the form of GitHub, which is turned on for the repository, confirms a report within 5 working days and aims a fix at 7 days for a critical issue and at 30 days for a high one ([DSA-001]). It names no scope, no rule for testing and nothing that is forbidden, and it does not say that it permits no testing | `SECURITY.md`; the setting read through the API of GitHub | The contact and the policy of the file are there already; the policy is extended, and no second one is written |
| 3 | The public name reaches the listener on port 8082 of nginx through the tunnel. Only `/admin` stands behind Cloudflare Access; every other path goes to the web application | `ops/deploy/nginx/templates/public.conf.template`, `ops/infra/cloudflare/main.tf` ([ADR-0028], [ADR-0029], [MIL-072]) | A location for exactly this path answers in nginx, before the application and without a sign-in |
| 4 | nginx reads its templates and snippets from the checkout of the release through read-only mounts; its image holds no configuration | `ops/deploy/nginx/docker-compose.yml` | A file in git reaches production with the ordinary deploy; nothing is changed inside a running container |
| 5 | The security headers of the site come from the web application, so a file that nginx serves gets none of them | `src/NFHotel.Web/Security/SecurityHeaders.cs`, `templates/public.conf.template` | The location of the file names its headers itself, or the dynamic application security testing (DAST) scan reports them missing |
| 6 | The web application would need new code to serve the file: by default the static files of ASP.NET Core leave out a folder whose name begins with a dot | `src/NFHotel.Web` | One more reason to serve the file from nginx, where no code of the application changes |
| 7 | A file that names its Canonical address is to be trusted only when it is fetched from that address, and it applies only to the name it is fetched from | RFC 9116, sections 2.5.2 and 3.1; RFC 8615, section 4.3 | The file is served on the public name only. The HTTPS server of the tailnet, whose name is not public, is not changed |
| 8 | A signature of the file is recommended, and so is a key for encryption when a Contact is an e-mail address. A security mailbox should have the name that RFC 2142 gives it, and what the file refers to should be controlled by the organisation | RFC 9116, sections 2.3, 2.5.3, 5.3 and 5.7 | S01 keeps no OpenPGP key yet, and the mailbox S01 named is a personal one. Each recommendation that is not followed is an exception in ADR-0048, with its reason and the event that reopens it |
| 9 | The file and what it refers to are to be watched for tampering, a report can be hostile, and a well-known location is to be protected against unwanted writes and served with `nosniff` and a content security policy | RFC 9116, sections 5.1 and 5.4; RFC 8615, sections 4.1 and 4.2 | The weekly look also asks the addresses the file names; the location sends its headers; `CODEOWNERS` already asks S01 for every change under `ops/` and of `SECURITY.md` |
| 10 | Nothing says how the team handles a report: who reads it, how it is rated, how a fix stays private until it is released, and when the incident page applies | `SECURITY.md`, the wiki page Incidents | The policy tells the person outside what to expect; a section of the wiki tells the team how to keep it |
| 11 | The private report form of GitHub needs an account: without one it answers with the sign-in of GitHub, on the same host. The page of the policy answers 200 without a sign-in | Two requests on 2026-11-17 | A finder without an account uses the second Contact. The weekly look accepts an answer on the host that the file names and refuses a redirect to another |
| 12 | The public site sends `Strict-Transport-Security: max-age=2592000`, set by the web application; the HTTPS server of the tailnet sends the value of `NGINX_HSTS` | A request on 2026-11-17; `templates/default.conf.template` | The location of the file sends the value of the application, so the public name keeps one policy. Its length stays the open decision of S01 ([MIL-066]) |
| 13 | The registry of IANA holds the field `Bug-Bounty`; its value `False` says that no reward is paid | The registry, read on 2026-11-17 | The file says in one line what the policy says in a sentence |

## Deliverable

- **ADR-0048.** The file is served by nginx from git, on the public name only; its date is a value in git that a check watches. The record names the alternatives (the web application, the managed `security.txt` of Cloudflare, a date written by the release) and holds a table of every requirement and recommendation of sections 2 to 6 of RFC 9116 and of sections 3 and 4 of RFC 8615: how each is met, or, as an exception, the reason it is not and the event that reopens it. The exceptions known today: no signature and no key for encryption, a mailbox that is not a role address of the project, and no promise about legal steps in the policy.
- **The policy.** `SECURITY.md` says what is in scope, how to report (also without a name, and without the details of a weakness in a plain e-mail), what a finder may do, what is not allowed (denial of service, destructive tests, reading or changing other people's data, social engineering), which findings are not handled, that neither the policy nor the file gives a permission to test (RFC 9116, section 5.5), and how a report is handled and made public: the times, the news a finder gets, the confidentiality of the finder, and a date of publication agreed with the finder, normally within 90 days. The address of the policy is the page GitHub makes of that file.
- **The file.** `ops/deploy/nginx/well-known/security.txt` with Contact twice, in the order of preference (the private report form of GitHub that `SECURITY.md` names, then the mailbox S01 named in chat on 2026-11-17, or a role address if S01 makes one), Expires, Preferred-Languages (`da, en`), Canonical, Policy and `Bug-Bounty: False`. No contact is invented.
- **The route.** A location for exactly `/.well-known/security.txt` on the public listener of nginx: `text/plain; charset=utf-8`; `X-Content-Type-Options: nosniff`, a content security policy that allows nothing, and the Strict-Transport-Security that the application sends for the public site; no sign-in, nothing passed to the application. The top-level address `/security.txt` redirects to it. The folder is one more read-only mount of the service.
- **The check.** `ops/scripts/check-security-txt.sh` reads a file or an address and sorts what it finds into three kinds. What RFC 9116 requires is a failure: a missing Contact or Expires, a repeated Expires or Preferred-Languages, a line that is neither a field with its value, a comment nor empty, a web address without `https://`, a date that is not RFC 3339 or is past, a body that is not UTF-8, and, for an address, another answer than 200 as `text/plain; charset=utf-8` over HTTPS without a redirect, or a file that does not name that address as Canonical. What this project asks on top is a failure too: a date more than a year or fewer than 30 days ahead, another Canonical than the public address, a field that the registry of IANA does not hold, and a file over 32 kilobytes, 1,000 lines or 2,048 characters in a field. What RFC 9116 recommends and ADR-0048 lists as an exception is a warning, and the run fails when the warnings are not exactly the expected ones. Its unit test feeds it good and bad files; Ops CI runs both, and runs the check on the real file.
- **The route tested.** `ops/tests/test-nginx.sh` asks the public listener for the file and compares the status, the type, the headers and the body, asks for the redirect of `/security.txt`, and shows that the HTTPS server of the tailnet hands the path to the application as before; its checks of `/admin` and of the mobile routes stay as they are.
- **The weekly look at the live file.** The comparison of Mondays ([MIL-078]) also fetches the public file, compares it with the file on `main`, asks the addresses the file names, and opens an issue when the file is missing, differs, expires within 30 days, or names an address that fails or leaves its host.
- **The handling of a report.** A section of the wiki page Incidents: who reads the reports and how often, how a report is answered, checked and rated, how a fix stays private until it is released, how the advisory names the finder, that the content of a report is treated as untrusted, and when the incident steps and the 72-hour rule apply.
- **The records.** The wiki page Public-Access says where the file is, how it is published, how it is tested, and how the contact and the date are kept; the page Security-Controls names the control.

Order: the review of this document; then tasks 1 to 6 on one branch and its pull request, which the AI merges when every required check is green; task 7 after the run of continuous deployment (CD) is approved; the gate last. Nothing reaches production without an approved run: S01's, or, at S01's word in chat on 2026-11-19, the AI's for the run that the merge of its own pull request starts.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | ADR-0048 is Accepted after its review: nginx serves the file from git on the public name only, the date is a value in git that the check watches, the three alternatives are named with the reason each lost, and its table has every requirement and recommendation of sections 2 to 6 of RFC 9116 and of sections 3 and 4 of RFC 8615, each met or an exception with its reason and the event that reopens it; the exceptions are accepted by the AI as stand-in for S01 and named in the summary of the run | Yes | No |
| 2 | `SECURITY.md` names the scope, the way to report, what is allowed, what is forbidden (denial of service, destructive tests, other people's data), the findings that are not handled, the handling and the publication of a report with its times, and says that neither it nor the file gives a permission to test; its wording is the one that S01 left to the AI in chat on 2026-11-17, and the summary of the run shows it to S01 | Yes | No |
| 3 | The file has at least one Contact, exactly one Expires, Preferred-Languages, Canonical (the public address) and Policy, the line `Bug-Bounty: False`, and no invented contact; `check-security-txt.sh` passes on it in Ops CI with exactly the warnings that ADR-0048 lists as exceptions, and its unit test passes with every bad file of the deliverable refused | Yes | No |
| 4 | `test-nginx.sh` passes in Ops CI: on the public listener the path answers 200 as `text/plain; charset=utf-8` with the body of the file, `X-Content-Type-Options: nosniff`, the content security policy and the Strict-Transport-Security of the public site, without the Access header and without a session; `/security.txt` answers 301 with a relative address; the HTTPS server of the tailnet hands the path to the application as before; every check that was there before still passes | Yes | No |
| 5 | After the deploy, a request from outside the tailnet gets 200 over HTTPS with no redirect, `text/plain; charset=utf-8`, the headers of criterion 4 with a Strict-Transport-Security equal to the one of the front page, and a body equal to the file on `main`; the same request over HTTP is redirected to HTTPS; `/security.txt` redirects to the file; the page of the policy answers 200 and the report form answers on `github.com`; `/admin` still shows the sign-in of Cloudflare Access; the DAST baseline of the pull request reports no new alert | Yes | No |
| 6 | The weekly comparison fetches the public file and passes; its test shows that a missing file, a different file, a date fewer than 30 days ahead and a named address that fails or leaves its host each open the issue | Yes | No |
| 7 | The wiki pages Public-Access and Security-Controls say what the deliverable says and `test-wiki-coverage.sh` passes; the code review against `QC-SH-001`, `QC-YML-001` and `QC-WF-001` is Go; `lint.sh` is clean; every check of the pull request is green | Yes | No |
| 8 | The wiki page Incidents says how a report from outside is handled: who reads it and how often, how it is answered, checked and rated, how a fix stays private until it is released, how the finder is named, that its content is untrusted, and when the incident steps apply; `CODEOWNERS` covers the file, its route and `SECURITY.md` | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-040 | The public listener of nginx behind the tunnel, where the file is served |
| MIL-041, MIL-072 | The Access check on every path of `/admin`, which this change leaves as it is |
| MIL-066 | The open decision on the length of Strict-Transport-Security, which this change leaves as it is |
| MIL-078 | The weekly comparison workflow, which gets the look at the live file |
| ADR-0028, ADR-0029 | The public name, the tunnel and Access |
| ADR-0037 | The rules that a changed workflow has to keep |
| DSA-001 | The times of `SECURITY.md` for a critical and a high finding |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001 objective 42: a person who finds a vulnerability reaches the team and knows the terms | Criteria 1 to 4 and 8: the decision, the policy, the file, its route, and the handling of a report |
| BC-001 success criterion 45: vulnerability disclosure | Criteria 3 to 6: the fields, the answer without a sign-in, the file on the site equal to `main`, and the date watched |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-27. No date of the Business Case moves.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | ADR-0048 Where security.txt is served | Write the decision record before any code: nginx serves `/.well-known/security.txt` from a file in git on the public name only, and Expires is a value in git that the check watches. Name the alternatives and why each lost: the web application (a dot folder is not served by default, and the file would go down with the application), the managed file of Cloudflare (outside git), and a date written by the release (nobody then stands behind it). Add the table: every requirement and recommendation of sections 2 to 6 of RFC 9116 and of sections 3 and 4 of RFC 8615, each with how it is met, or, as an exception, the reason it is not and the event that reopens it (no signature, no key for encryption, a mailbox that is not a role address, no promise about legal steps). Its review record against `QC-ADR-001`. | No | criterion 1 |
| 2 | The disclosure policy in SECURITY.md | Extend the policy that exists, in English: the systems in scope (the public site and the code of this repository) and out of scope (the accounts at Cloudflare, GitHub and Tailscale, and other people's systems), how to report, also without a name and without the details of a weakness in a plain e-mail, what a finder may do, what is forbidden (denial of service, destructive tests, reading or changing other people's data, social engineering, physical access), the findings that are not handled, one plain sentence that neither the policy nor the file gives a permission to test (RFC 9116, section 5.5), and how a report is confirmed, fixed and made public: the times of today, the news a finder gets, the confidentiality of the finder, and a date agreed with the finder, normally within 90 days. S01 left the wording to the AI on 2026-11-17 and sees it in the summary of the run: no promise about legal steps, nothing to sign, and one plain sentence that no reward is paid. | No | criterion 2 |
| 3 | The file and its route in nginx | Add `ops/deploy/nginx/well-known/security.txt` with Contact twice in the order of preference (the report form of GitHub from `SECURITY.md`, then the mailbox S01 named or a role address), Expires (180 days after the day of the pull request at 00:00 UTC, and never after the day the mailbox closes), `Preferred-Languages: da, en`, Canonical, Policy and `Bug-Bounty: False`. Mount the folder read-only in the service, and add to `public.conf.template` a location for exactly this path (the type `text/plain; charset=utf-8`, `nosniff`, a content security policy that allows nothing, and the Strict-Transport-Security that the application sends for the public site) and a redirect from `/security.txt`. The HTTPS server of the tailnet is not changed. No file of the web application changes. | No | criteria 3, 4 |
| 4 | The check and the tests | `ops/scripts/check-security-txt.sh` for a file or an address, with the requirements of RFC 9116 as failures, the rules of this project as failures, and the recommendations that ADR-0048 lists as exceptions as warnings that must be exactly the expected ones; `ops/tests/test-check-security-txt.sh` with its good and bad files, in the matrix of Ops CI; a step of Ops CI runs the check on the real file. `ops/tests/test-nginx.sh` mounts the folder and asks the public listener: the status, the type, the headers, the body equal to the file, no Access header, the redirect of `/security.txt`, and the path still handed to the application on the HTTPS server of the tailnet; the checks of `/admin` and of the mobile routes stay untouched. Write each test first and see it fail. | No | criteria 3, 4 |
| 5 | The weekly look at the live file | A second job in `.github/workflows/edge-comparison.yaml`, without the environment and without a secret, runs the check on the public address, compares the body with the file on `main`, asks the addresses the file names, and opens, updates or closes one issue the way the comparison does. The workflow keeps the rules of ADR-0037, and the test of task 4 covers the four failures: a missing file, a different file, a date fewer than 30 days ahead, and a named address that fails or leaves its host. | No | criterion 6 |
| 6 | The records, the code review and the pull request | The wiki page Public-Access gets a section: where the file is, how a change reaches the site (pull request, release, and the CD run that S01 approves), how to test it, and how the contact and the date are kept (the check fails 30 days before the date, and the weekly issue). The page Incidents gets the handling of a report from outside: who reads it and how often, how it is answered, checked and rated, how a fix stays private until it is released, how the advisory names the finder, that the content of a report is untrusted, and when the incident steps and the 72-hour rule apply. Security-Controls names the control. The code review record against `QC-SH-001`, `QC-YML-001` and `QC-WF-001`, and `lint.sh`; then the commits and the pull request, which closes one issue per task it completes and is merged when every required check is green. | No | criteria 7, 8 |
| 7 | The deploy and the look from outside | After the merge and the approval of the CD run: ask the public address from outside the tailnet for the status, the type, the headers and the body, compare the body with the file on `main` and the Strict-Transport-Security with the one of the front page; ask the same address over HTTP and `/security.txt` for their redirects, and the page of the policy and the report form for an answer; ask `/admin` and see the sign-in of Cloudflare Access; read the DAST baseline of the pull request. The AI approves only the run that the merge of its own pull request starts, at S01's word in chat on 2026-11-19 ("du må gerne godkende deploy"), after it has seen that no other deploy runs and that every required check of the commit is green. | No | criterion 5 |
| 8 | Gate and review record | The gate review record against criteria 1 to 8, the rows of MIL-084 and ADR-0048 in the traceability matrix, the Accepted row of this document, and the closing of the GitHub milestone. | No | criteria 1 to 8 |

---

[BC-001]: ../business-case.md
[ADR-0028]: ../adr/adr-0028-public-access-through-cloudflare-tunnel.md
[ADR-0029]: ../adr/adr-0029-back-office-behind-cloudflare-access.md
[ADR-0037]: ../adr/adr-0037-github-actions-hardening-policy.md
[MIL-040]: ./mil-040-public-access.md
[MIL-041]: ./mil-041-admin-behind-cloudflare-access.md
[MIL-066]: ./mil-066-findings-of-7-november.md
[MIL-072]: ./mil-072-the-access-check-on-every-admin-path.md
[MIL-078]: ./mil-078-the-tunnel-routes-and-access-as-code.md
[DSA-001]: ../sqa/devsecops-assessment.md
[91b74ec]: https://github.com/NF-Hotel/NFHotel/commit/91b74ecad8fe5599e484de8c979956a5ab7832f0
