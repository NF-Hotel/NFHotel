# RC-331 Code review of MIL-072: the Access check on every spelling of /admin

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-331 |
| CrossReference | [MIL-072], [RC-330], [ADR-0029], [QC-ADR-001], [QC-SH-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-11 | Deprecated | mikemike | S01 | Initial version; mode supervised; the review of tasks 2 to 4 of MIL-072 before their pull request: `ops/tests/test-nginx.sh`, `public.conf.template`, ADR-0029 and the wiki page Public-Access; Go, accepted by S01 in chat ("2 ja", to "Accepterer du RC-331?") | [0e58535] |
| 2026-11-11 | Accepted | mikemike | AI as stand-in for S01 | Finding 7 and its action item: S01 decided in chat that the records follow the setting of 1 week; ADR-0029 decision 1 and the wiki say 1 week. Finding 3: the Sonar fix of `with_token` (ffce18c); accepted at the gate of MIL-072 (RC-339) | [3afb6a3] |

---

## Artifact Under Review

- **Instances reviewed** (the worktree of the branch `mil-072-access-check-on-every-admin-path` against `main` at 1c2726b; nothing is committed yet):
  - shell: the regression test in `ops/tests/test-nginx.sh` (task 2): four spellings of `/admin/login` without the Access token, `/ADMIN/login` with it, the function `with_token` and the header;
  - nginx: `location ~* ^/admin` and its comment in `ops/deploy/nginx/templates/public.conf.template` (task 3);
  - [ADR-0029] (task 3): the new Version History row, the paragraph of 2026-11-11 in the Context, decisions 2, 3 and 5, the new negative consequence and MIL-072 in Affected Artifacts;
  - wiki: Public-Access (task 4): the table "What is public", steps 4 and 5 of the setup of the back office, and "Check it".
- **Not in this record:** criteria 1 and 2 of [MIL-072] belong to task 1, the routes of the tunnel, which the AI changed in the Cloudflare dashboard in S01's browser on 2026-11-11 at 20:56 and 20:57 UTC. Criterion 1 and the checks without an Access session of criterion 2 passed at 20:57 UTC, and the check with S01's own Access session at 21:36 UTC: `/admin/login` and `/ADMIN/login` answered 200 with the hotel's sign-in. Criterion 4 needs the deploy (task 5). Criterion 6 needs this record and `lint.sh`, which pass below, and every required check of the pull request. The gate record of task 6 closes them.
- **Checklists used:** [QC-ADR-001] for the ADR; [QC-SH-001] for the test; criteria 3 and 5 of MIL-072 for the template and the wiki page, as task 4 of the plan says.
- **Scope:** delta review of the changed files against 1c2726b. The parts of ADR-0029 that MIL-072 does not change stand as accepted on 2026-10-25.
- **Language and domain:** n/a (technical types).
- **Language reviewer:** none.
- **Mode:** supervised. The AI wrote the test, the fix, the row and the text of ADR-0029, the wiki text and this record, so the walk is not independent of the author. S01 is the reviewer: S01 accepted the plan ("1ja", [RC-330]) and this record ("2 ja"), and asked in the same answer for the commit, the push and the pull request.

**Tools and runs of 2026-11-11, on the PC of S01:**

- `ops/tests/test-nginx.sh` in the native Docker of WSL 2, written first: against the template of 1c2726b, 25 passed and 4 failed (21:04 UTC); with the fix, 29 passed, 0 failed (21:06 UTC). After the change of finding 3, both again: 25 passed and 4 failed with a copy of `ops/deploy/nginx` that holds the template of 1c2726b (21:17 UTC), and 29 passed, 0 failed with the fix (21:18 UTC).
- `ops/tests/lint.sh` (layout, actionlint 1.7.7, shellcheck v0.11.0, shfmt v3.14.1, yamllint 1.35.1), from Git Bash against Docker Desktop: clean at 21:07 and at 21:18 UTC (finding 5).
- `ops/tests/test-wiki-coverage.sh`: 112 passed, 0 failed, at 21:06 and at 21:19 UTC.
- The images that the test builds and pulls were removed by name after each round; no container or network of the test is left.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| ADR 1 | The ID (`ADR-NNNN`, 4-digit) matches the filename number and is unique and sequential | Pass | ADR-0029, `adr-0029-back-office-behind-cloudflare-access.md`; unchanged |
| ADR 2 | Context states the problem, the forces (cost, risk, constraints) and the options evaluated | Pass | The paragraph of 2026-11-11 states the problem (the validating route second in the list; `^/admin` and `^~ /admin` compare letter case, the web application does not), what it meant (the check of the tunnel was not in effect, while Access at the edge stood in front of every spelling, finding 6) and which decisions it makes precise. The forces and the three options of 2026-10-25 stand |
| ADR 3 | At least two options were considered, or the absence of alternatives is justified | Pass | The three options stand. MIL-072 changes no choice among them: it makes option 3 work as decided, so it adds none |
| ADR 4 | Decision is stated in one or two unhedged sentences and matches one of the evaluated options | Pass | "We follow option 3. It changes ADR-0028 decision 3 for `/admin` only." is unchanged. Decisions 2, 3 and 5 now name the place of the route in the list, its path `(?i)^/admin`, `location ~* ^/admin` and the check in the log of `cloudflared`, without hedging |
| ADR 5 | Consequences list both Positive and Negative outcomes | Pass | Two positive and three negative; the new negative: the routes live in the dashboard, so no test of the repository sees their order (finding 6) |
| ADR 6 | Quality-attribute impacts are named where the decision affects them | Pass | Security: who reaches the staff sign-in, with two locks (decision 4); the delta puts the check of the tunnel in effect for every spelling. Maintainability: the routes outside git, in the new negative consequence |
| ADR 7 | Affected Artifacts are listed as links, and each is also in CrossReference | Pass | ADR-0028, MIL-041 and the new MIL-072, each in CrossReference and defined at the bottom |
| ADR 8 | Status changes are recorded as new Version History rows with a Change summary and Commit link; retained rows are never rewritten | Pass | One new row, reviewer S01, `pending` until the commit. With S01's acceptance of this record it is `Accepted`, and the row of 2026-10-25 is `Deprecated` with its text unchanged |
| ADR 9 | A superseded or deprecated ADR names its successor, and the successor links back | N-A | Nothing is superseded |
| ADR 10 | Written in business or technical language of the domain, with no unexplained abbreviations | Pass | After finding 4, VM and AUD are spelled out in decision 2. JWT stays inside the quoted name of the setting in the dashboard, and `Cf-Access-Jwt-Assertion` is the name of a header |
| ADR 11 | Status is one of the allowed values, and every Version History row uses only these | Pass | `Deprecated` and `Accepted` |
| SH 1 | Header line and `set` options | Pass | Unchanged: `#!/usr/bin/env bash` and `set -uo pipefail`, as every test of `ops/tests`, so that every check runs and is counted |
| SH 2 | Quoting, arrays, `[[ ]]` | Pass | `"$spelling"`, `"$PUBLIC$1"` and `"$(with_token …)"` are quoted; the loop runs over a fixed list of words, in which bash expands nothing, `/%41DMIN/login` included |
| SH 3 | Names | Pass | `spelling`, a variable, and `with_token`, a function, both `snake_case` |
| SH 4, 12 | shellcheck, `bash -n`, shfmt | Pass | `lint.sh` clean with shellcheck v0.11.0 and shfmt v3.14.1; no `disable` added |
| SH 5 | Errors | Pass | Unchanged: `check` counts a failure and prints what it got and what it expected; the script ends with 1 when a check failed |
| SH 6 | Temporary files | Pass | Unchanged: one folder, `tests/output/nginx`, removed by `trap cleanup EXIT`; the new checks write no file |
| SH 7 | Secrets | Pass | The token is the fake `eyJ.test.token`, as before: nginx checks only that the header is there. No real token, team name or tag is in the test |
| SH 8 | State outside its folder | Pass | Unchanged: the containers and networks that the header names, removed at the end |
| SH 9 | Header | Pass | The header now says "in any spelling of /admin" and names MIL-072 |
| SH 10 | Implements the task it cites | Pass | Tasks 2 and 3 of MIL-072; the commits are to carry their `Task:` trailers. No deviation |
| SH 11 | Tested for success, failure and the bypass path | Pass | The bypass path: four spellings without the token, each 404; success: `/ADMIN/login` with the token reaches the web application; the checks in lower case and of the mobile routes keep their results. With the template of 1c2726b the four fail |
| SH 13 | Safe to re-run | Pass | Unchanged: `cleanup` runs at the start and at the end; four runs on 2026-11-11 |
| SH 14 | Needs stated | Pass | Unchanged: bash 4+ and docker; the new checks use the image of curl that the test already names |
| SH 15 | Knowledge once | Pass | After finding 3, the request with the token is one function, `with_token`, for `/admin/login` and `/ADMIN/login`; the four spellings are one list |
| MIL-072 3 | The regression test fails with the template of 1c2726b and passes with the fix, and Ops CI runs it | Pass | 25 passed and 4 failed with the template of 1c2726b, 29 passed and 0 failed with the fix, before and after finding 3; `.github/workflows/ops-ci.yaml` runs `bash ops/tests/test-nginx.sh` |
| MIL-072 5 | ADR-0029 and the wiki page Public-Access say what the deliverable says, and `test-wiki-coverage.sh` passes | Pass | ADR-0029: the Context of 2026-11-11, decisions 2, 3 and 5, a negative consequence and MIL-072 in Affected Artifacts. Public-Access: `/admin...` in any letter case in the table; the path `(?i)^/admin` and **Move up** to **Order 1** in step 4; `/ADMIN/login` and **Order 1** in step 5 and in "Check it", with the newest log line `Updated to new configuration` of `cloudflared` in Grafana. 112 passed |

## Findings

1. **The test fails first, for the reason that was found.** With the template of 1c2726b, `/ADMIN/login`, `/Admin/login`, `/aDmin/login` and `/%41DMIN/login` without the token got 200 from the stand-in web application: `^~ /admin` is a prefix that compares letter case, so they took `location /`. The percent-encoded letter went the same way, because nginx decodes the path before it matches. With `location ~* ^/admin` all four get 404.
2. **The regular expression and the other locations.** nginx checks the regular expressions after the prefixes, in the order of the file. `~* ^/admin` comes before the regular expression of `mobile-api.conf` (`^/mobile-api…`), and neither can match the paths of the other; `location /` stays the fallback. The checks of the two mobile routes, of another API route and of `/mobile-api/` keep their results. Like `^~ /admin` before it, the location also takes paths such as `/administrator`; the validating route `(?i)^/admin` covers the same paths.
3. **One command for the request with the token.** The first version of task 2 repeated, for `/ADMIN/login`, the `docker run` of curl with the two headers that the check in lower case already had (SH 15). This review made it the function `with_token`, used by both checks. The runs after it gave the same results, and `lint.sh` is clean. On the pull request, SonarCloud then flagged the plain-HTTP hop of `with_token` (shell:S5332); ffce18c moved its two headers into local constants, so the command fits on one line with the same `NOSONAR` reason as `PUBLIC`. The requests are the same, and the quality gate passed.
4. **Two abbreviations in decision 2.** VM and AUD, both from the accepted text, were not spelled out. Decision 2 now says "the virtual machine (VM)" and "the Application Audience (AUD) tag", the name of the field in the dashboard. In the same pass the table of Public-Access got "in any letter case" on its row of `/admin...`.
5. **`lint.sh` runs from Git Bash for this worktree.** The worktree lies in the temporary folder of Windows, and its `.git` file names a Windows path, which git in WSL cannot read. In WSL every linter therefore got an empty list of files, and yamllint stopped on its usage line. From Git Bash, with `MSYS_NO_PATHCONV=1` and `REPO_HOST` set to the Windows path, every linter read its files and is clean. Ops CI runs `lint.sh` again on the pull request.
6. **The edge was not the gap.** At 20:54 UTC, before the change of the routes, `/admin/login` and `/ADMIN/login` both answered with a redirect to the sign-in of Cloudflare Access: the Access application does not compare letter case, which settles the third finding of MIL-072. What was not in effect was the check of the tunnel, which the place and the path of the route now put in effect. No test of the repository sees the routes, so ADR-0029 records this as a negative consequence, and decision 5 names the check in the log of `cloudflared`.
7. **The session of Access lasts one week.** The dashboard shows a session duration of 1 week for the application "NF Hotel back office" (seen during task 1), while decision 1 of ADR-0029 and three places of the wiki page said 24 hours. S01 first left the choice to the AI ("det må du vide"), which kept 24 hours; S01 then decided in chat that the records follow the setting ("Ret det med access-session så det passer med det aktuelle"). Decision 1 and the wiki now say 1 week. The cost: a browser left signed in, or an address taken off the list, can pass the first lock for up to a week, while taking an address off the list stops new codes at once; the hotel's own sign-in stays the second lock either way.

## Overall Verdict

Go. Every Mandatory criterion of QC-ADR-001 and QC-SH-001 that the change touches passes after findings 3 and 4, and so do criteria 3 and 5 of MIL-072. Criteria 1 and 2 belong to task 1 and passed, the second with S01's own Access session at 21:36 UTC; criterion 4 needs the deploy and criterion 6 the required checks of the pull request. The gate record of task 6 closes them. The walk is not independent of the author; S01 accepted the record in chat ("2 ja").

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Merge the pull request once every required check is green | S01 | 2026-11-15 |
| Approve the CD run of the merge (ADR-0038); then the checks of task 5 | S01 | At the merge |

---

[MIL-072]: ../../milestones/mil-072-the-access-check-on-every-admin-path.md
[RC-330]: ./rc-330-mil-072.md
[ADR-0029]: ../../adr/adr-0029-back-office-behind-cloudflare-access.md
[QC-ADR-001]: ../../../framework/qc/artifacts/qc-adr.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[0e58535]: https://github.com/NF-Hotel/NFHotel/commit/0e5853507a4d6ebea270a1048857a1c02fac8e73
[3afb6a3]: https://github.com/NF-Hotel/NFHotel/commit/3afb6a38797598a205e2c4c82dd19c352967ae89
