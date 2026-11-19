# RC-404 Gate review of MIL-084 A security.txt and a disclosure policy for the public site

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-404 |
| CrossReference | [MIL-084], [ADR-0048], [RC-400], [RC-402], [RC-403], [RC-405], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-19 | Accepted | mikemike | AI as stand-in for S01 | Initial version: the gate of MIL-084 after the deploy of pull request 834, the look from outside, the change of the weekly look in pull request 836 and its run by hand; Go on criteria 1 to 8; mode autonomous, not independent | pending |

---

## Artifact Under Review

- Instance reviewed: [MIL-084] at its gate. It covers the eight Go/No-Go criteria, with this evidence:
  - the documents and the code of tasks 1 to 6 on `main` since 46b29fb (pull request 834, merged on 2026-11-19 at
    09:22:14 UTC), reviewed in [RC-402] ([ADR-0048], Go) and [RC-403] (the code, Go-with-conditions);
  - the change of the weekly look on `main` since 7fd5bea (pull request 836, merged on 2026-11-19 at 11:09:47 UTC),
    reviewed in [RC-405] (Go);
  - the checks of pull request 834 on its last head (323cbab): 60 passed and 2 skipped, which skip by design; and of
    pull request 836 on its last head (be1b24e): 60 passed and the same 2 skipped (OWASP ZAP and DAST full scan);
  - the run 44497614288 of NF Hotel CD, which the AI approved as stand-in for S01, and the run 44514175991 of the
    merge of pull request 836, which waits (below);
  - the look from outside of task 7 (below);
  - the runs by hand 44498395515 and 44512660870 of NF Hotel edge comparison (below).

  Before the gate: [RC-400] (the plan, Go; S01 accepted the amended plan in chat on 2026-11-18).
- Checklist used: the Go/No-Go criteria of [MIL-084]. [QC-MIL-001] criterion 2 makes them the checklist of the gate.
- Scope: the gate (task 8).
- Language and domain: en / it (the Metadata rows of MIL-084). The gate adds only a row to its Version History. This
  record is a technical type.
- Language reviewer: none.
- Mode: autonomous, not independent. S01 switched [MIL-084] to autonomous mode in chat on 2026-11-19 ("Ja, automode"
  in the main session; "Automode og deploy" in the session that builds the milestone) and allowed the AI to approve
  the run of CD that the merge of its own pull request starts ("du må gerne godkende deploy."). The AI built the
  code, merged the pull requests, approved the run of pull request 834, made the look and wrote this record.

**The runs of CD.** The AI approves a run only through the API of GitHub as the signed-in account, with a comment
that names the pull request, after it has seen that the run is a `workflow_run` of `main` at the merge it expects,
that no other deploy runs and that every required check of the commit is green. S01's words: "du må gerne godkende
deploy." (the main session, 2026-11-19) and "Automode og deploy" (the session of MIL-084, 2026-11-19, its answer to
"Hvad må jeg selv gøre med MIL-084?").

- Run 44497614288, at 46b29fb (pull request 834): approved at 09:34:53 UTC with the comment "Deploy of pull request
  #834 (MIL-084: security.txt and the disclosure policy)."; the job "Deploy to production" ran from 09:34:56 to
  09:37:51 UTC and passed, without a rollback.
- Run 44514175991, at 7fd5bea (pull request 836), created at 11:22:04 UTC: not approved; it waits. At 11:00:54 UTC
  the run 44510422356 of the merge of pull request 837 (MIL-085, not part of this milestone) failed: the registry of
  GitHub refused the new image to the VM, and the deploy rolled back to 46b29fb. Until S01 renews the read credential
  of the VM for that registry, every deploy fails the same way, so at 11:10 UTC the main session asked that no run of
  CD be approved, and the AI left this one waiting (the stop rule of `AGENTS.md`: a required token is missing). The
  gate does not need it: pull request 836 changed nothing that runs on the VM. The weekly look runs on a runner of
  GitHub from `main`, so its change took effect at the merge (the run 44512660870 below), and the file on the VM is
  still the one of 46b29fb, which is the same as on `main` (F8).

**The look from outside (task 7),** from S01's PC through WSL, 2026-11-19, 09:38:53 to 09:38:56 UTC, read-only:

- `https://nfhotel.devmatiker.com/.well-known/security.txt`, no redirect followed: HTTP/2 200,
  `content-type: text/plain; charset=utf-8`, `x-content-type-options: nosniff`,
  `content-security-policy: default-src 'none'; frame-ancestors 'none'`, `strict-transport-security: max-age=2592000`,
  no `location` and no Access header; the body, 458 bytes, equal to the file of `main` (the same SHA-256).
- The front page: HTTP/2 200 with `strict-transport-security: max-age=2592000`, equal to the file.
- The same address over HTTP: 301 to the HTTPS address. `/security.txt`: 301 to `/.well-known/security.txt`.
- The page of the policy: 200. The report form: 302 to the sign-in of `github.com`.
- `/admin` without a session: 302 to the team host of Cloudflare Access (`*.cloudflareaccess.com`), path
  `/cdn-cgi/access/login/nfhotel.devmatiker.com`.
- `check-security-txt.sh` on the live address with the Canonical, the file of `main`, the addresses it names and the
  front page (`--hsts-like`): passes, with the three expected warnings `unsigned`, `no-encryption` and
  `no-role-address`.

**The DAST baseline.** The run 44474022006 of the e2e and DAST workflow on pull request 834 (head 323cbab): the
signed-out scan of the site had no failure and one warning (10027, suspicious comments), with 5 ignored and 62
passed; the scan of `/admin` had no failure and two warnings (10027 and 10029, cookie poisoning), with 5 ignored and
61 passed. The run on `main` before [MIL-084] (44464623722, at 04c0a87) gave the same rules and the same counts, so
there is no new alert. The scan runs against the HTTPS server of the tailnet in the stack of the test, which this
milestone left unchanged by design ([ADR-0048], decision 2); the public listener is checked by `test-nginx.sh` and by
the look.

**The weekly look, run by hand.**

- Run 44498395515, at 46b29fb, 2026-11-19, attempts at 09:39 and 09:41 UTC: the look found one failure, "ADR-0048
  rule: Strict-Transport-Security is 'max-age=2592000', and https://nfhotel.devmatiker.com/ sends ''", opened issue
  835 and commented on it; the run stayed green, as a finding does. The cause and the change are in [RC-405]: the
  edge answers the front page to a runner of GitHub by itself, so the look no longer compares with it, and the check
  by hand keeps that comparison.
- Run 44512660870, at 7fd5bea (the merge of pull request 836), 2026-11-19, 11:12:10 to 11:12:26 UTC: the look passed
  with the three expected warnings (`unsigned`, `no-encryption`, `no-role-address`). Issue 835 was closed by then:
  the merge of pull request 836 had closed it at 11:09:48 UTC (F7), so the run found no open issue and changed none.

## Checklist Results

| # | Criterion (MIL-084) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | ADR-0048 is Accepted after its review, with the decision, the alternatives and the table of the deliverable; the exceptions accepted by the AI as stand-in for S01 and named in the summary of the run | Pass | [ADR-0048] is Accepted; [RC-402] is Go against QC-ADR-001: one sentence of decision (nginx serves the file from git on the public name only, the date a value in git that the check watches), five options with the reason each lost, and the table of sections 2 to 6 of RFC 9116 and sections 3 and 4 of RFC 8615, each line met or an exception with its reason and what reopens it. The AI accepted the exceptions E1 to E4 as stand-in for S01 on 2026-11-19; the summary of the run names them. Decision 6 changed on 2026-11-19 with the weekly look ([RC-405], Go) |
| 2 | `SECURITY.md` names what the criterion lists, gives no permission to test, keeps the wording S01 left to the AI, and the summary of the run shows it to S01 | Pass | `SECURITY.md` on `main` has the scope and what is out of it, the way to report (the private form of GitHub, the mailbox S01 named as the fallback, no name needed, no details in a plain e-mail), what a finder may do, what is forbidden (denial of service and load, destroying or changing data, other people's data, the back office and other accounts, social engineering, physical access), the findings that are not handled, the sentence that neither the policy nor the file is a permission to test (RFC 9116, section 5.5), and the handling: 5 working days to confirm, news at least every 30 days, the fix times of [DSA-001], the confidentiality of the finder and a date of publication agreed with the finder. The AI looked at the wording as stand-in for S01 and changed nothing ([RC-403]); the summary of the run shows it |
| 3 | The fields of the file; the check passes in Ops CI with exactly the exceptions as warnings; its unit test refuses every bad file | Pass | The file has two Contacts, one Expires (2027-05-18T00:00:00Z), `Preferred-Languages: da, en`, Canonical (the public address), Policy and `Bug-Bounty: False`, and no invented contact. On pull request 834 the step "Check security.txt (MIL-084)" of Ops nginx routes passed with the warnings `unsigned`, `no-encryption` and `no-role-address` (E1 to E3), and Ops script tests (test-check-security-txt.sh) passed 107 of 107, every bad file refused |
| 4 | `test-nginx.sh` passes in Ops CI: the answer of the public listener, the redirect, the tailnet server unchanged, every older check | Pass | Ops nginx routes passed on pull request 834: 43 of 43 checks, among them the 14 new ones (the status, the type, the body, the three headers, no Access header, no session, the 301 with a relative address, the tailnet server handing the path to the application) |
| 5 | After the deploy, from outside the tailnet: the answer of the file, HTTP redirected, `/security.txt` redirected, the policy page and the report form, `/admin` behind Access, the DAST baseline without a new alert | Pass | The deploy of 46b29fb (run 44497614288), the look from outside and the DAST baseline above: 200 over HTTPS without a redirect, the type, the headers of criterion 4 with the Strict-Transport-Security of the front page, the body of `main`; HTTP and `/security.txt` redirected; the policy 200 and the report form on `github.com`; `/admin` at the sign-in of Access; no new alert |
| 6 | The weekly comparison fetches the public file and passes; its test shows the four failures each opening the issue | Pass | The run by hand 44512660870 on `main` after [RC-405]: the look passed (above). Ops script tests (test-edge-comparison.sh) passed on pull request 836, 55 of 55: a missing file, a different file, a date fewer than 30 days ahead and a named address that fails or leaves its host each open the issue, and a look that passes closes it. The first run by hand failed only on the comparison with the front page, which the edge answers to a runner by itself ([RC-405]); issue 835, which it opened, is closed (F7) |
| 7 | The wiki pages, `test-wiki-coverage.sh`, the code review Go, `lint.sh` clean, every check of the pull request green | Pass | The pages Public-Access and Security-Controls say what the deliverable says ([RC-403], and [RC-405] for the look); Ops wiki coverage and Ops linters (`lint.sh` clean, the workflow policy and its self-test) passed on pull requests 834 and 836; [RC-403] is Go-with-conditions and its condition is met (F1); [RC-405] is Go; every check of both pull requests passed, but for the two that skip by design |
| 8 | The wiki page Incidents on a report from outside; `CODEOWNERS` covers the file, its route and `SECURITY.md` | Pass | The section "A vulnerability report from outside" of the page Incidents says who reads a report and how often, how it is answered, checked and rated, how a fix stays private until it is released, how the advisory names the finder, that the content of a report is untrusted, and when the incident steps and the 72-hour rule apply. `CODEOWNERS` names the owner for `/ops/` (the file and its route), `/.github/` (the workflows) and `/SECURITY.md` |

## Findings

- **F1, note: the condition of the code review is met.** [RC-403] gave Go-with-conditions: every required check of
  the pull request green before the merge. Pull request 834 merged with every check green, so the review counts as
  Go for criterion 7.
- **F2, for S01: the environment `cloudflare-read` is open to every branch.** The pages Public-Access and Workflows
  say that only `main` may use it; on 2026-11-19 the API of GitHub showed no deployment branch policy for it. It holds
  no secret yet (task 5 of [MIL-078] is S01's), and the comparison job of the runs by hand compared nothing, so
  nothing is exposed today. The limit must be set before the secrets go in. Not a criterion of [MIL-084]; nothing was
  changed.
- **F3, for S01: the new check is not required.** `Ops script tests (test-check-security-txt.sh)` ran on pull
  requests 834 and 836, but the ruleset "Approve merges" does not require it ([RC-403] F7). Adding it is S01's.
- **F4, note: the date closes the gate in 2027.** From 2027-04-18, 30 days before the Expires of 2027-05-18, the
  step of Ops nginx routes fails on every pull request and every push to `main`, so CD stops until a pull request
  moves the date ([ADR-0048], its first negative consequence); the weekly look opens its issue from the same day.
- **F5, note: where the look came from.** The look of task 7 ran from S01's PC in WSL over the internet; the public
  name resolves to Cloudflare, so the request took the way a finder's takes. No booking, sign-in or other write was
  made; the answers of `/admin` and of the report form were read without following them.
- **F6, for S01: the edge answers the front page to a runner of GitHub by itself** ([RC-405] F1). The rule that does
  it is part of the Cloudflare account; a monitor in a datacenter sees the front page only as the edge answers it.
  Not a criterion of [MIL-084]; nothing was changed.
- **F7, note: issue 835 closed at the merge, not by the look.** A sentence of the description of pull request 836
  said that a look that passes would close the issue, in words that GitHub reads as a closing keyword, so the merge
  closed issue 835 at 11:09:48 UTC, three minutes before the look passed on the same commit. The evidence is the
  same, and a comment on the issue names the run that passed. A description keeps issue numbers to the lines of its
  own (`Closes #N`, `Refs #N`).
- **F8, for S01: the deploy of pull request 836 waits.** The run 44514175991 of NF Hotel CD waits for its approval
  until the VM can pull images from the registry of GitHub again (above). While it waits it holds the queue of CD
  (the concurrency group `production`), so a later run waits behind it. Approving it deploys `main` at 7fd5bea, the
  change of MIL-085 included; no criterion of [MIL-084] depends on it.

## Overall Verdict

Go on criteria 1 to 8, by the AI as stand-in for S01 in autonomous mode. Every criterion passes with the evidence
above: the decision and its exceptions, the policy, the file and its check, the route, the deploy and the look from
outside, the weekly look after its change ([RC-405]), the records and the handling of a report. The deploy of pull
request 836 waits (F8); no criterion depends on it. The reviewer is not independent, and each stand-in decision is
named in the summary of the run.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept this record and the gate row of MIL-084, or return them | S01; done on 2026-11-19 by the AI as stand-in, at S01's word in chat | At the review of the gate pull request |
| Read the checks of the gate pull request before the merge | AI as stand-in for S01 | Before the merge |
| Renew the read credential of the VM for the registry of GitHub, so the waiting run 44514175991 can be approved (F8) | S01 | Before the next deploy |
| Set the deployment branch policy of `cloudflare-read` to `main` before its secrets go in (F2) | S01 | With task 5 of MIL-078 |
| Make `Ops script tests (test-check-security-txt.sh)` a required check (F3) | S01 | Open |
| Move the Expires of the file in a pull request (F4) | S01 | Before 2027-04-18 |

---

[MIL-084]: ../../milestones/mil-084-a-security-txt-and-a-disclosure-policy.md
[ADR-0048]: ../../adr/adr-0048-where-security-txt-is-served.md
[RC-400]: ./rc-400-mil-084.md
[RC-402]: ./rc-402-adr-0048.md
[RC-403]: ./rc-403-mil-084-code.md
[RC-405]: ./rc-405-the-weekly-look-without-the-front-page.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[DSA-001]: ../devsecops-assessment.md
[MIL-078]: ../../milestones/mil-078-the-tunnel-routes-and-access-as-code.md
