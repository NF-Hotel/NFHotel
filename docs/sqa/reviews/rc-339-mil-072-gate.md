# RC-339 Gate review of MIL-072 The Access check on every path to the back office

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-339 |
| CrossReference | [MIL-072], [RC-330], [RC-331], [ADR-0029], [ADR-0038], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-13 | Accepted | mikemike | AI as stand-in for S01 | Initial version; mode autonomous, not independent; the gate of MIL-072 after the CD run of f83859e and the checks from outside of 2026-11-13; Go-with-conditions: the request inside the VM (criterion 4) waits for S01 | pending |

---

## Artifact Under Review

- Instance reviewed: [MIL-072] at its gate: the six Go/No-Go criteria, with the evidence of task 1 (2026-11-11), of pull request #649 (merged by S01 as f83859e on 2026-11-12 at 16:26 UTC), of its continuous deployment (CD) run 43262765251 and of the checks from outside on 2026-11-13. Before the gate: [RC-330] (the plan) and [RC-331] (the code).
- Checklist used: the Go/No-Go criteria of [MIL-072]; [QC-MIL-001] criterion 2 is what makes them the checklist of the gate.
- Scope: the gate; criteria 3, 5 and 6 cite [RC-331] and the green checks of #649. This record also carries the edits of 2026-11-11 that make decision 1 of [ADR-0029] and the wiki page Public-Access say 1 week (finding 1).
- Language and domain: en / it (the Metadata rows of MIL-072); this record is a technical type.
- Language reviewer: none.
- Mode: autonomous, not independent. S01, the approving reviewer of MIL-072, asked in chat on 2026-11-12 for work in autonomous mode with the right to merge and to open pull requests ("færdiggør milestones i automode med rettigheder til self merge og PR og Deploy", "Du behøver ik spørg mig om noget"), and on 2026-11-13 pasted the open milestones MIL-072 and MIL-071 from GitHub into the chat. The AI writes this record as stand-in for S01.

**Evidence of 2026-11-11 to 2026-11-13:**

- **Task 1, before the change (2026-11-11, 20:54:33 UTC).** On the public name, without a cookie and without a sign-in attempt, `/admin/login` and `/ADMIN/login` both answered 302 to the sign-in of Cloudflare Access. The Access application at the edge compares the path without regard to letter case, so no stranger reached the hotel's sign-in through `/ADMIN`; what the order of the routes had lost was the check of the token at the tunnel ([RC-331] finding 6).
- **Task 1, the change.** In the dashboard (Networking, Tunnels, the tunnel of the VM, Routes), the validating route got the path `(?i)^/admin` and was moved to the top. The log of `cloudflared` in Loki then showed version 4 of the configuration at 20:57:26 UTC: rule 0 the public name with the path `(?i)^/admin` and the Access check on, with the team and one Application Audience (AUD) tag, the tag of the Access application "NF Hotel back office"; rule 1 the public name without a path; rule 2 `http_status:404`; no warning and no error since 20:50 UTC.
- **Task 1, after the change (20:57:46 UTC).** `/admin/login`, `/ADMIN/login`, `/Admin/login`, `/aDmin/login`, `/%41DMIN/login` and `/admin` answered 302 to the sign-in of Access, and `/book` answered 200. The public probe, `probe_success{job="public_site"}`, read 1 on both series for the 20 minutes around 21:00 UTC (40 samples each), with HTTP status 200. With S01's own Access session (S01 typed the one-time code), `/admin/login` and `/ADMIN/login` showed the hotel's sign-in, "Sign in — NFHotel administration", at 21:36 UTC; no sign-in to the hotel was tried.
- **The pull request.** #649 (tasks 2 to 4) passed every required check. On `main`, CI run #564 of the merge f83859e first failed on a crash of the host of the API tests (exit 135, no test ran) and passed when the failed job ran again at 16:42 UTC; release run 43261566057 followed.
- **The deploy.** CD run 43262765251 of f83859e, approved by S01 ([ADR-0038]), deployed from 16:49 to 16:52 UTC on 2026-11-12. It pulled `nfhotel-nginx@sha256:0a653092a46b…` of the release, recreated `nfhotel-nginx-nginx-1` at 16:52:13 UTC, which turned healthy at 16:52:19 UTC, and ended with "Deployed: postgres ollama api web analysis benchmark nginx tunnel dbrelay alloy". The CD runs of 2026-11-13 deployed later commits of `main`, which keep the same guard.
- **From outside, 2026-11-13 at 22:35:45 UTC.** The same seven requests, without a cookie and without a sign-in attempt: the six spellings of `/admin` answered 302 to the sign-in of Access, and `/book` answered 200.
- **Inside the VM.** The request of criterion 4, to the public listener of nginx (port 8082, which is not published) from inside `nfhotel-nginx-nginx-1`, was not made. The AI's tooling refused the read-only command over Tailscale Secure Shell (SSH) on 2026-11-13 as a read of production, and the AI does not work around such a refusal. The command waits for S01 (action items).

## Checklist Results

| # | Criterion (MIL-072) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | After task 1, the configuration that `cloudflared` writes into its log has the validating route first, with the path `(?i)^/admin` and the Access check on, and the route without a path after it | Pass | Version 4 of the configuration at 20:57:26 UTC on 2026-11-11: rule 0 the validating route with `(?i)^/admin` and the Access check on, its AUD tag the one of the Access application; rule 1 the route without a path; rule 2 404 |
| 2 | After task 1, from outside the tailnet and without an Access session, five spellings of `/admin/login` answer with Cloudflare's sign-in page or 403, never with the hotel's sign-in page; signed in to Access, S01 reaches the hotel's sign-in at `/admin/login`; `/book` answers 200, and the public probe stays green | Pass | 2026-11-11 after task 1: the five spellings and `/admin` 302 to the sign-in of Access; S01 with an Access session reached the hotel's sign-in at `/admin/login` and `/ADMIN/login` at 21:36 UTC; `/book` 200; the probe 1 for 20 minutes. Repeated from outside on 2026-11-13 at 22:35:45 UTC with the same answers; the probe was not read again |
| 3 | The regression test fails with the template of 1c2726b and passes with the fix, and Ops CI runs it | Pass | [RC-331] finding 1: `/ADMIN/login`, `/Admin/login`, `/aDmin/login` and `/%41DMIN/login` reached the stand-in web application with the template of 1c2726b and got 404 with the fix; the job "Ops nginx routes" runs `test-nginx.sh` and passed on #649 and on `main` since, the last time on 611909b (Ops CI run 43518047226) |
| 4 | After the deploy, inside the VM, the public listener of nginx answers 404 to `/ADMIN/login` without the header, and the checks of criterion 2 still hold | Pending | The deploy: CD run 43262765251 recreated nginx with the image of f83859e. The checks of criterion 2 still hold from outside (2026-11-13). The request inside the VM has not been made; it waits for S01 |
| 5 | ADR-0029 and the wiki page Public-Access say what the deliverable says, and `test-wiki-coverage.sh` passes | Pass | [ADR-0029] decisions 2, 3 and 5 (the rows of 2026-11-11) and decision 1 with this record (finding 1); Public-Access says the same; the job "Ops wiki coverage" passed on 611909b and runs again on the pull request of this record |
| 6 | The code review against `QC-SH-001` is Go, `lint.sh` is clean, and every required check of the pull request is green | Pass | [RC-331] Go, accepted by S01; `lint.sh` clean ([RC-331] finding 5); every required check of #649 passed |

## Findings

1. **The session lasts one week.** Decision 1 of [ADR-0029] and the wiki page Public-Access said 24 hours, while the Access application is set to 1 week ([RC-331] finding 7). S01 decided in chat on 2026-11-11 that the records follow the setting ("Ret det med access-session så det passer med det aktuelle"). The edits were made that day but stayed out of #649. This record carries them: their rows in [ADR-0029] and [RC-331] become Accepted, with the AI as stand-in for S01, and the rows before them Deprecated. The setting itself is not changed.
2. **The recurring probe.** The plan leaves to S01, at the gate, a recurring probe that would notice a later change of the routes. The public probe (`public_site`) checks the site only, so a change of the routes or of the Access application in the dashboard would show only at the next check by hand. Recommendation: a probe that asks for `/ADMIN/login` on the public name without a session and expects the redirect to the sign-in of Access, in a milestone of its own, deployed with the monitoring. The decision is S01's; it is not a condition of this gate.
3. **Access at the edge was the first lock all along.** Before and after task 1 every spelling met Access at the edge. Task 1 and the guard of nginx restore the second and the third lock of decisions 2 and 3 of [ADR-0029]; criterion 4 checks the third on the VM itself.

## Overall Verdict

Go-with-conditions: criteria 1, 2, 3, 5 and 6 pass, and criterion 4 is Pending until the request inside the VM is made. #643 (task 1) and #648 (task 6) close at the merge of the pull request that carries this record. #647 (task 5), the Accepted row of [MIL-072] and the GitHub milestone #54 wait until the answers are recorded in a new row of this record. The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Close #643 and #648 | AI as stand-in for S01 | At the merge of this record |
| Run the check of criterion 4 on `nf-hotel`: inside `nfhotel-nginx-nginx-1`, `wget` of `http://127.0.0.1:8082` for `/ADMIN/login`, `/Admin/login`, `/aDmin/login`, `/%41DMIN/login`, `/admin/login` and `/admin` without the header (404 each) and for `/book` (200); or let the AI run the same read-only command | S01 | 2026-11-15, the target date of MIL-072 |
| Record the answers in a new row of this record, add the Accepted row of [MIL-072], and close #647 and the GitHub milestone #54 | AI | After the check |
| Decide on a recurring probe of `/ADMIN/login` on the public name (finding 2) | S01 | At the next planning |

---

[MIL-072]: ../../milestones/mil-072-the-access-check-on-every-admin-path.md
[RC-330]: ./rc-330-mil-072.md
[RC-331]: ./rc-331-mil-072-code.md
[ADR-0029]: ../../adr/adr-0029-back-office-behind-cloudflare-access.md
[ADR-0038]: ../../adr/adr-0038-approval-before-a-deploy.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
