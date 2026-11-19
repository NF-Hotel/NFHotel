# ADR-0048 Where security.txt is served

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0048 |
| CrossReference | [MIL-084], [ADR-0028], [ADR-0029], [ADR-0037], [ADR-0021], [ADR-0046], [MIL-066], [DSA-001], [RC-400], [RC-402], [RC-405] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-18 | Deprecated | mikemike | AI as stand-in for S01 | Initial version; task 1 of MIL-084, whose amended plan S01 accepted in chat on 2026-11-18 (RC-400, Go); written by the AI in supervised mode and reviewed in RC-402; accepted with the exceptions E1 to E4 on 2026-11-19 by the AI as stand-in for S01, after S01 switched MIL-084 to autonomous mode in chat ("Automode og deploy") | [3b63129] |
| 2026-11-19 | Accepted | mikemike | AI as stand-in for S01 | Decision 6 and the consequences: the weekly look leaves out the Strict-Transport-Security of the front page, which the edge answers to a runner of GitHub by itself, so the look failed although the site sends the same value (found at the gate of MIL-084 on 2026-11-19); decisions 2 and 5 say who compares the two values; RC-405, Go; mode autonomous, not independent | pending |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

**Who decides.** S01 asked on 2026-11-17 for a `security.txt` after Request for Comments (RFC) 9116 at
`https://nfhotel.devmatiker.com/.well-known/security.txt`, with a disclosure policy behind it, done "rigtigt og
ordentligt" (properly): every requirement of the standards met or recorded as an exception. S01 accepted the amended
plan [MIL-084] on 2026-11-18 ([RC-400], Go). This record is task 1 of [MIL-084]. The artificial intelligence (AI)
assistant wrote it in supervised mode. On 2026-11-19 S01 switched [MIL-084] to autonomous mode in chat, and the AI
accepted this record as stand-in for S01 ([RC-402]), and with it the four exceptions of the table below.

**The problem.** A person who finds a weakness in NF Hotel gets no address from the site itself: on 2026-11-17 and
2026-11-18 the public site answered 404 at `/.well-known/security.txt`, and on 2026-11-17 at `/security.txt` too.
`SECURITY.md` names a private channel, the report form of GitHub, but only a finder who looks in the repository finds
it, and it names no scope, no rules for testing and nothing that is forbidden.

**The texts.** RFC 9116 (sections 1 to 6), RFC 8615 (sections 3 and 4) and the registry of `security.txt` fields of
the Internet Assigned Numbers Authority (IANA), last updated 2026-03-07, were read in full by the planning session on
2026-11-17 and again by the AI that wrote this record, on 2026-11-18. Named by them and used through them, but not
read: RFC 3339 (the date), RFC 5646 (the language tags), RFC 3986 and RFC 6068 (the addresses), RFC 2142 (the name of
a security mailbox), RFC 4880 (the signature) and RFC 5198 (the network form of Unicode). This record claims
conformance with RFC 9116 and with sections 3 and 4 of RFC 8615 only. It claims nothing about ISO/IEC 29147 (disclosure)
and ISO/IEC 30111 (the handling of a report), which the International Organization for Standardization (ISO) sells and
which were not read. What the policy and the handling of a report contain (a contact, a safe way to send, what to
include, what the finder can expect, the scope; to answer, to thank, no document to sign, news while the fix is open,
credit) follows the Vulnerability Disclosure Cheat Sheet of the Open Worldwide Application Security Project (OWASP)
and the page of the Vulnerability Disclosure Toolkit of the National Cyber Security Centre (NCSC) of the United
Kingdom, which the planning session read on 2026-11-17 and which name those two standards as their base.

**Forces.**

- The file speaks only for the name it is fetched from (RFC 9116, section 3.1), and a file fetched from an address
  that its Canonical fields do not name should not be trusted (section 2.5.2). The public name is the only name a
  finder outside reaches.
- The security headers of the site come from the web application (`src/NFHotel.Web/Security/SecurityHeaders.cs`),
  so a file that nginx serves gets none of them; RFC 8615, section 4.2, names `nosniff` and a content security policy
  for a well-known resource.
- The public site sends `Strict-Transport-Security: max-age=2592000`, the default of `app.UseHsts()` in
  `src/NFHotel.Web/Program.cs` (seen on 2026-11-17 and 2026-11-18); the HTTPS server of the tailnet sends the value of
  `NGINX_HSTS` from `nginx.env` on the virtual machine, which the wiki page Environment-Files sets to a year with a
  real certificate. The length of the policy of the public name is an open decision of S01 ([MIL-066]).
- Stale information is worse than none (RFC 9116, section 5.3): the date of the file is a promise that somebody reads
  the reports until then. The mailbox that S01 named is a personal school mailbox whose closing day nobody knows yet.
- The repository and the logs of its workflows are public.
- nginx reads its templates and snippets from the checkout of the release through read-only mounts, and a hash of
  them recreates the proxy when they change (`ops/scripts/deploy-services.sh`); its image
  (`ops/deploy/nginx/Dockerfile`) copies in no configuration.
- A red check on the newest commit of `main` stops its deploy ([ADR-0021]); every changed workflow keeps the rules of
  [ADR-0037].

**Options considered.**

1. **The web application serves the file** from `wwwroot/.well-known/` of `src/NFHotel.Web`. By default the .NET
   software development kit (SDK) leaves a folder whose name begins with a dot out of a project:
   `Microsoft.NET.Sdk.DefaultItems.targets` (line 56) adds `**/.*/**` to `DefaultExcludesInProjectFolder`, and the
   static web assets take `wwwroot\**` with those excludes (`Sdk.StaticWebAssets.StaticAssets.ProjectSystem.props`,
   line 31; both read in SDK 10.0.403 on 2026-11-18). The file would be neither published nor in the manifest of
   `MapStaticAssets`, so it would need an item or an endpoint of its own in the application, and it would go down with
   the application. The application would give it the security headers of the site.
2. **The managed `security.txt` of Cloudflare.** The dashboard of the zone can publish a file at the edge. It lives
   outside git: no review, no history, and the weekly comparison of the edge ([ADR-0046]) does not read it.
3. **nginx serves a file from git on the public listener (chosen).** An exact location of the listener that the
   tunnel reaches, with its own headers, and a read-only mount of a folder of the checkout.
4. **The same file on both listeners,** the HTTPS server of the tailnet too. The file names the public address as its
   Canonical, so under the name of the tailnet it would be a file that should not be trusted, for a name that nobody
   outside reaches (RFC 9116, sections 2.5.2 and 3.1; RFC 8615, section 4.3).
5. **A date written by each release,** the day of the release plus a number of days. The date would never run out
   while releases go on, and so it would say nothing about whether anybody reads the reports.

## Decision

The public site serves `/.well-known/security.txt` from `ops/deploy/nginx/well-known/security.txt` in git, through an
exact location on the public listener of nginx with its own headers and a redirect from `/security.txt` (option 3);
its Expires is a value in git that `ops/scripts/check-security-txt.sh` watches on every pull request and every week,
and the policy behind it is `SECURITY.md`.

1. **The file.** ASCII only, LF line ends and a newline at the end, two comment lines first, then the fields in this
   order: Contact, the private report form of GitHub that `SECURITY.md` names
   (`https://github.com/NF-Hotel/NFHotel/security/advisories/new`); Contact, the mailbox that S01 named in chat on
   2026-11-17 for this purpose, as a `mailto:` address; Expires; `Preferred-Languages: da, en`; Canonical, the public
   address; Policy, the page that GitHub makes of `SECURITY.md` (`https://github.com/NF-Hotel/NFHotel/security/policy`);
   and `Bug-Bounty: False`. Expires is 180 days after the day of the pull request at `00:00:00Z`, and never after the
   day the mailbox closes; S01 named no such day on 2026-11-18. One space follows each colon, as the grammar asks. Not
   in the file, each with its reason in the table: a signature, `Encryption`, `Acknowledgments`, `Hiring` and `CSAF`.
2. **The route.** In `ops/deploy/nginx/templates/public.conf.template`, before `location /`, an exact location for
   `/.well-known/security.txt` with `alias` to the mounted file, `default_type text/plain` and `charset utf-8`, which
   give `Content-Type: text/plain; charset=utf-8` (RFC 9116, section 3), and three headers: `X-Content-Type-Options:
   nosniff` and `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'` (RFC 8615, section 4.2), and
   `Strict-Transport-Security: max-age=2592000`. An exact match wins over the pattern of `/admin` and over
   `location /`, so nothing reaches the application, no Access token is asked for, and nginx answers while the
   application is down. A second exact location answers `/security.txt` with 301 and the relative address
   `/.well-known/security.txt` (`absolute_redirect off`): nginx listens on port 8082 behind the tunnel and does not
   know the public scheme or port. *Why the value of Strict-Transport-Security is written out:* `${NGINX_HSTS}` would
   give the public name the value of the tailnet server through a side door, while its length is S01's open decision
   ([MIL-066]); a location with a header of its own inherits none, so the location names the value that the
   application sends for the public site, and the look by hand compares the two (decision 6). The HTTPS server of
   the tailnet is not changed: there the path goes to the application, as before. No other path under `/.well-known/`
   gets an answer of its own.
3. **The mount.** `docker-compose.yml` of the proxy mounts the folder `./well-known` read-only at
   `/etc/nginx/well-known`. The folder, not the file: a mount of a file keeps the old file when git replaces it, and a
   mount of the folder shows the file of the next release at once. `well-known` is not part of `NGINX_CONFIG_HASH` in
   `ops/scripts/deploy-services.sh`, so a new date does not restart the proxy; the first deploy recreates it anyway,
   because the compose file and the template change. The folder has no leading dot, so nobody overlooks it (RFC 8615,
   section 4.4). Write access is that of the repository: a pull request, which `CODEOWNERS` shows to S01, a release,
   and the run of continuous deployment (CD) that S01 approves; in production the file is a read-only mount of a
   read-only container (RFC 8615, section 4.1).
4. **The date is a value in git.** The check fails when Expires is past, more than 366 days ahead or fewer than 30
   days ahead. Thirty days before the date, every pull request fails until a person moves the date in one line, and
   so says again that the contacts are read.
5. **The check sorts what it finds into three kinds,** and each message names its kind and its source.
   - *A requirement of RFC 9116* is a failure: no Contact; no Expires or more than one; more than one
     Preferred-Languages, or one without a language tag; a line that is neither empty, a comment nor a field with a
     name, a colon, one space and a value; a value of Acknowledgments, Canonical, Contact, Encryption, Hiring or
     Policy that is no Uniform Resource Identifier (URI) at all, or a web address without `https://`; an Expires that
     is not a date and time of RFC 3339, or is past; a line without its line end; a byte order mark, a control
     character or bytes that are not UTF-8; and, for an address, another scheme than `https`, an answer that is
     neither 200 nor a redirect, or another type than `text/plain; charset=utf-8`.
   - *A rule of this project* is a failure: an Expires that is not `YYYY-MM-DDThh:mm:ssZ`, or lies more than 366 or
     fewer than 30 days ahead; not exactly one Canonical, or one other than the public address; no Policy; a Contact
     that is neither `https:` nor `mailto:`; a field that the registry of IANA does not hold, or `Bug-Bounty` twice or
     with a value other than `True` or `False`; bytes outside ASCII; a file over 32 kilobytes or 1,000 lines, or a
     field over 2,048 characters (the limits a reader may apply, RFC 9116, section 5.4); a signed file, which the
     check cannot read yet (E1); and, for an address, a redirect, or a file whose Canonical fields do not name the
     address it was fetched from (section 2.5.2 says such a file should not be trusted), a body that differs from the
     file on `main`, an address in the file that answers 400 or more or ends on another host, and, when it is given
     a page to compare with (`--hsts-like`), a Strict-Transport-Security other than the one of that page.
   - *A recommendation of RFC 9116 that this record lists as an exception* is a warning: `unsigned` (E1),
     `no-encryption` (E3) and `no-role-address` (E2). `--expect-warnings` names the expected ones, and a run whose
     warnings differ from them fails: a new gap fails, and so does an exception that is closed, until this record and
     the option change together.

   No message prints a mailbox address or the body of a file, because the output of the weekly look goes into a
   public issue.
6. **The watch.** The workflow Ops CI (continuous integration) runs the unit test of the check and, in the job
   `Ops nginx routes`, the check on the file of the commit. Every Monday a second job of `edge-comparison.yaml`,
   without an environment and without a secret, fetches the public file and runs the check with the address, the
   Canonical, the file of `main` and the addresses it names; a failure opens the issue "The public security.txt fails
   its weekly check", or comments on the open one, and a clean run closes it (RFC 9116, section 5.1). A failure
   leaves the run green, as a difference of the edge does ([ADR-0021], [ADR-0046]); a check that cannot run turns it
   red. Until the first deploy of the file the job reports it as missing. The job does not compare the
   Strict-Transport-Security of the file with the one of the front page: the edge answers the front page to a runner
   of GitHub by itself, without the headers of the site. On 2026-11-19 the job failed on that comparison alone, while
   the access log of nginx held the two requests of the runner for the file and none for the front page, and the same
   comparison passed from a workstation of the team (task 7 of [MIL-084]). The comparison is a step of the look by
   hand on the wiki page Public-Access (`--hsts-like`).
7. **The policy and the handling.** `SECURITY.md` says what is in scope, how to report, what a finder may and may
   not do, which findings are not handled, that neither the policy nor the file gives a permission to test (RFC 9116,
   section 5.5), and how a report is handled, with the times of [DSA-001]. The wiki page Incidents says how the team
   keeps it.

**The requirements, one by one.** Each requirement and recommendation of sections 2 to 6 of RFC 9116 and of sections
3 and 4 of RFC 8615, with how it is met; an **E** row is an exception, with its reason and the event that reopens it.
Rows for the reader of a file (a researcher) say what this project does for that reader.

| Source | Asks | How it is met |
| --- | --- | --- |
| RFC 9116 2, 4 | The file MUST follow the grammar of section 4; a field MUST be a name and a value, each field on its own line, and values MUST NOT be chained unless the field allows it | The file; `check-security-txt.sh` on every pull request (Ops CI) and every week on the live file |
| 2 | Field names are case insensitive | The check compares names without regard to case |
| 2.1, 2.2 | A comment begins with `#`; every line MUST end with CRLF or LF | Two comment lines; LF (`.gitattributes`) and a newline at the end; the check fails a line without its end |
| 2.3 | RECOMMENDED: an OpenPGP cleartext signature | **E1.** No signature: S01 keeps no OpenPGP key, and a key held by a workflow would prove nothing about who wrote the file. Reopens when S01 has a key (a hardware key is planned): sign the file, publish the key, teach the check to read a signed file, and drop `unsigned` from the expected warnings |
| 2.3 | RECOMMENDED with a signature: the Canonical field | Met already: one Canonical |
| 2.4, registry of IANA | Registered fields are optional; a reader MUST ignore a field it does not know | Six fields of the registry are used; the check refuses a name outside the registry, as a guard against a misspelt name |
| 2.5.1 | Acknowledgments (optional): a page that names finders; `https` | Not used: no such page exists, and none is invented. Reopens with the first advisory that names a finder |
| 2.5.2 | Canonical: `https`; a file fetched from an address that it does not name SHOULD NOT be trusted | One Canonical, the public address, and the file is served at that address only; the check with `--url` fails a file that does not name the address it came from |
| 2.5.3 | Contact MUST be present, a URI, `https` for a web address and `mailto:` for a mailbox, and SHOULD be in the order of preference | The report form first, the mailbox second |
| 2.5.3 | A security mailbox should follow RFC 2142 (`security@`) | **E2.** The mailbox that S01 named is a personal school mailbox, not a role address in a domain of the project, which has no mail domain of its own. Reopens when S01 makes a role address, and at the latest when the school mailbox closes; the second Contact then changes, and `no-role-address` leaves the expected warnings |
| 2.5.3, 5.7 | RECOMMENDED: encryption for a mailbox; keys, unless HTTPS is used for the report | **E3.** No key, as E1. The first Contact is a form over HTTPS, and the policy asks for no details in a plain e-mail. Reopens with E1 |
| 2.5.4 | Encryption (optional): a URI of a key, never the key | Not used (E3) |
| 2.5.5, 5.3 | Expires MUST be present, once, as RFC 3339; RECOMMENDED less than a year ahead; organisations must use it | A value in git, 180 days ahead when it is set; the check fails 30 days before it and beyond 366 days, and the weekly look opens an issue |
| 2.5.6 | Hiring (optional) | Not used: no position to point to |
| 2.5.7, 3.1 | Policy: `https`; organisations SHOULD use it for the scope and the process | The page that GitHub makes of `SECURITY.md`, which names the scope, the rules and the handling; it answered 200 without a sign-in on 2026-11-17 and 2026-11-18 |
| 2.5.8 | Preferred-Languages: at most once, at least one value, language tags | `da, en` |
| Registry | CSAF (optional) | Not used: advisories are published on GitHub, not in the Common Security Advisory Framework |
| Registry | Bug-Bounty (once): `True` or `False` | `False`: no reward is paid, as the policy says |
| 3 | MUST be under `/.well-known/` | The exact location in nginx |
| 3 | A file or a redirect at the top level may stay for older readers; if both exist, the one under `/.well-known/` MUST be used | `/security.txt` answers 301 to the file, so the file exists once |
| 3, 5.7 | MUST be fetched over `https`; web addresses in the file over `https` too | Cloudflare ends Transport Layer Security (TLS) for the public name and sends `http://` to `https://` ("Always Use HTTPS", wiki page Public-Access); every address in the file is `https` or `mailto:` |
| 3 | MUST be `text/plain` with the charset `utf-8` | The location; `ops/tests/test-nginx.sh`; the check with `--url` |
| 3, 5.2 | A fetch may be redirected; readers should record and inspect redirects | The Canonical address answers 200 without a redirect; the weekly look fails a redirect, and a named address that ends on another host |
| 3.1 | The file MUST apply only to the name it is fetched from | Served on the public name only; `SECURITY.md` names the scope and says that the file speaks for that name only |
| 4 | Plain text, UTF-8 in the network form of Unicode | ASCII only, without control characters or a byte order mark (the check); ASCII is UTF-8 and unchanged by every normalization form. RFC 5198 itself was not read |
| 5.1 | Use Canonical, sign, and watch the file and what it refers to | Canonical; E1; the weekly look compares the live file with `main` and asks every address it names |
| 5.3 | What the file refers to should be current, reachable, controlled by the organisation and kept secure | The file and the policy are in the repository of the organisation and change only by pull request (`CODEOWNERS`); the form is GitHub's form of that repository; the mailbox is E2 |
| 5.4 | Files, resources and reports can be large, malformed or hostile | The check holds the file to 32 kilobytes, 1,000 lines and 2,048 characters a field, prints no value of the live file but its addresses, and reads the live file with the same limits; the wiki page Incidents treats a report as untrusted |
| 5.5 | The file implies no permission to test | One plain sentence in `SECURITY.md`, with the section named |
| 5.6 | Reserve both names, so that no third party makes a page there | Both are exact locations of nginx; no user of the site can make a page |
| 5.7 | Readers validate the certificate; organisations sign, and give keys unless reports go over HTTPS | The certificate of Cloudflare for the public name; E1; E3 |
| 5.8 | Spam and automatic reports | The policy names what is not handled, and the wiki page Incidents how such a report is answered; a public personal mailbox gets spam, one more reason to close E2 |
| 6 | Addresses in the file MUST NOT point into `/.well-known/` unless registered | Only the Canonical does, and `security.txt` is registered (section 6.1) |
| RFC 8615 3 | The default port of the scheme; no resource is assumed at `/.well-known/` itself | Port 443 at the edge; every other path under `/.well-known/` goes to the application as before |
| 4.1 | Control who can write to a well-known location | Git, a pull request that `CODEOWNERS` shows to S01, a release and the CD run that S01 approves; a read-only mount in a read-only container |
| 4.2 | `nosniff`; as good practice a content security policy, a media type of the application, Referrer-Policy and no compression of secrets | `nosniff` and a content security policy that allows nothing. `text/plain` is fixed by RFC 9116; Referrer-Policy and the rule on compression do not apply, because neither the address nor the body holds a secret |
| 4.3 | A well-known resource applies to its origin only | As RFC 9116, section 3.1, above |
| 4.4 | A folder whose name begins with a dot is easily overlooked | The folder in git is `well-known`, without the dot |
| OWASP cheat sheet, NCSC page | Safe harbour: a promise not to take legal steps against a finder in good faith | **E4.** Not given: S01's task says that the policy must not give anybody a permission to test, and such a promise is commonly read as one; OWASP advises legal advice before writing one. Reopens when S01 has taken legal advice |

## Consequences

**Positive:**

- A finder finds the contacts and the terms where the standard says to look, also while the web application is down.
- The file changes like code: a pull request, a review, a release and the deploy that S01 approves, with its history
  in git; no code of the web application changes, and the tailnet server stays as it is.
- The date cannot run out unseen: the check fails 30 days before it on every pull request, and the weekly look opens
  an issue.
- The weekly look also finds a changed file and a dead or redirected address on the live site (RFC 9116, section
  5.1).

**Negative:**

- From 30 days before the date, the job `Ops nginx routes` fails on every pull request and every push to `main`, and
  CD deploys nothing ([ADR-0021]) until the date is moved; a fix in that window has to carry the new date.
- The value of Strict-Transport-Security stands in two places, the default of `app.UseHsts()` and the location; a
  change of one alone is found only by the look by hand and by task 7 of [MIL-084], not by the pull request and not
  by the weekly look, to which the edge does not show the front page of the site (decision 6).
- The file is not signed (E1), and the mailbox is a personal one without a key (E2, E3): a finder must trust HTTPS
  and the Canonical, and spam reaches a person.
- No promise about legal steps (E4): a cautious finder may not report at all.
- Until the first deploy the weekly job reports the file as missing, and the new check `Ops script tests
  (test-check-security-txt.sh)` gates no merge until S01 adds it to the required checks of the ruleset.
- The weekly look depends on pages of GitHub: if the report form stops sending a visitor without a session to the
  sign-in of `github.com`, the look reports the address as failed.

**Quality:** security (a private channel and its terms where a finder looks, without weakening any sign-in or
header), reliability (no stale contact: the date and the addresses are watched), maintainability (the file and its
route in git, tested in continuous integration) and compatibility (the format of RFC 9116) (ISO/IEC 25010:2023).

## Affected Artifacts

- [MIL-084]: tasks 2 to 6 carry out this decision; task 7 checks it from outside after the deploy.
- [ADR-0028]: unchanged; the public listener answers one more path itself, and the tunnel and its routes stay as
  they are.
- [ADR-0029]: unchanged; the exact location lies outside `/admin`, and Access still stands before every spelling of
  `/admin`.
- [ADR-0037]: the second job of `edge-comparison.yaml` keeps its rules.
- [ADR-0046]: the weekly workflow gets a second job, without the environment `cloudflare-read` and without a secret.
- [ADR-0021]: the reason why a failure of the weekly look leaves the run green.
- [MIL-066]: the length of Strict-Transport-Security on the public name stays S01's open decision; this location
  copies the value of the application.
- [DSA-001]: the times of `SECURITY.md` for a critical and a high finding are its thresholds, unchanged.

---

[MIL-084]: ../milestones/mil-084-a-security-txt-and-a-disclosure-policy.md
[ADR-0028]: ./adr-0028-public-access-through-cloudflare-tunnel.md
[ADR-0029]: ./adr-0029-back-office-behind-cloudflare-access.md
[ADR-0037]: ./adr-0037-github-actions-hardening-policy.md
[ADR-0021]: ./adr-0021-automatic-deploy-when-green.md
[ADR-0046]: ./adr-0046-the-tunnel-routes-and-access-in-opentofu.md
[MIL-066]: ../milestones/mil-066-findings-of-7-november.md
[DSA-001]: ../sqa/devsecops-assessment.md
[RC-400]: ../sqa/reviews/rc-400-mil-084.md
[RC-402]: ../sqa/reviews/rc-402-adr-0048.md
[RC-405]: ../sqa/reviews/rc-405-the-weekly-look-without-the-front-page.md
[3b63129]: https://github.com/NF-Hotel/NFHotel/commit/3b6312966875c845a2881da6dc02f9729e0e184e
