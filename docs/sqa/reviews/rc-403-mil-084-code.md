# RC-403 Code review of MIL-084: the security.txt, its check and the weekly look

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-403 |
| CrossReference | [MIL-084], [ADR-0048], [RC-402], [RC-400], [RC-387], [ADR-0037], [ADR-0021], [ADR-0046], [QC-SH-001], [QC-YML-001], [QC-WF-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-19 | Accepted | mikemike | AI as stand-in for S01 | Initial version; prepared in supervised mode; Go-with-conditions: every required check of the pull request green. The same day S01 switched MIL-084 to autonomous mode in chat ("Automode og deploy"), and the AI accepted this review as stand-in for S01 | pending |

---

## Artifact Under Review

- Instance reviewed: the code and the documents of [MIL-084] tasks 2 to 6 in the worktree of the branch
  `mil-084-security-txt`, from `main` at 3b973c7, before any commit:
  - task 2: `SECURITY.md`, with the text of the plan word for word;
  - task 3: `ops/deploy/nginx/well-known/security.txt`, the two locations in
    `ops/deploy/nginx/templates/public.conf.template` and the read-only mount in `ops/deploy/nginx/docker-compose.yml`;
  - task 4: `ops/scripts/check-security-txt.sh`, `ops/tests/test-check-security-txt.sh` with its row among the script
    tests of `.github/workflows/ops-ci.yaml`, the step `Check security.txt (MIL-084)` of the job `Ops nginx routes`,
    and the section on `security.txt` of `ops/tests/test-nginx.sh`;
  - task 5: the job `Look at the public security.txt` of `.github/workflows/edge-comparison.yaml`, the subcommands
    `look` and `look-report` of `ops/scripts/edge-comparison-steps.sh` with the function `file_issue`, which `report`
    now shares, and their section of `ops/tests/test-edge-comparison.sh`;
  - task 6: the wiki pages Public-Access, Incidents, Security-Controls and Workflows.

  `.github/CODEOWNERS` is unchanged: `/ops/`, `/SECURITY.md`, `/.github/` and `/docs/adr/` already name the owner.
  ADR-0048 (task 1) is reviewed in [RC-402].
- Checklist used: [QC-SH-001] (`qc-programming-shell`), [QC-YML-001] (`qc-yaml`) and [QC-WF-001] (`qc-workflow`). The
  framework has no checklist for an nginx configuration, so the two locations, the mount and the file are read against
  the decisions of [ADR-0048] below. `SECURITY.md` and the wiki pages have no checklist of their own; they are read
  against the deliverable of [MIL-084].
- Scope: full review of the new and changed files. The job `compare` of the same workflow ([ADR-0046]) and the
  subcommands `check`, `compare` and `report` were reviewed in [RC-387]; of them only `report` changed, and only in
  that its issue logic moved into `file_issue`.
- Language and domain: n/a (technical type)
- Language reviewer: none
- Mode: autonomous, not independent. The artificial intelligence (AI) assistant wrote the code and prepared this
  record in supervised mode for S01, who accepted the amended plan of [MIL-084] in chat on 2026-11-18 ([RC-400], Go).
  On 2026-11-19 S01 switched [MIL-084] to autonomous mode in chat ("Automode og deploy"), so the reviewer is the AI as
  stand-in for S01; the AI also looked at the wording of `SECURITY.md` as stand-in (below).

## Checklist Results

### QC-SH-001: `check-security-txt.sh`, `edge-comparison-steps.sh` and the three tests

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | `#!/usr/bin/env bash` and `set -euo pipefail`, or a comment explains the exception | Pass | The two scripts keep both; the tests keep the convention of `ops/tests`, `set -uo pipefail` with each assertion counted |
| 2 | Expansions quoted, lists as arrays, `[[ ]]` and `$(...)` | Pass | The options of the look are the array `args`; the Canonical fields, the addresses, the dates and the warnings of the check are arrays; the expected warnings are read with `IFS=, read -ra`; shellcheck clean |
| 3 | Naming | Pass | `kebab-case.sh` files; snake_case functions (`fetch_file`, `check_file`, `read_lines`, `check_value`, `check_expires`, `check_counts`, `check_warnings`, `check_named`, `check_links`, `check_hsts`, `finish`, `file_issue`, `write_look_summary`, `look`, `look_report`) and variables; the limits, the registry, the patterns, the address, the warnings and the title in UPPER_SNAKE, read-only |
| 4 | shellcheck and `bash -n`, every `disable` explained | Pass | shellcheck at the version of `lint.sh` over the five files: clean. The one new `disable`, `SC2015` in `test-check-security-txt.sh`, has its reason, as in the other tests |
| 5 | Errors on standard error with `error:`, a usage line for bad arguments | Pass | The check writes a bad argument as `error: ...` with its usage to standard error and exits 2; its findings are its output, on standard output, each `FAIL` or `WARN` line naming its kind and its source, with exit 1. The steps write `::error::` lines to standard error, for a check that cannot run, a `FAILED` that is not `true` or `false`, and a missing summary |
| 6 | `mktemp` and `trap ... EXIT`, no fixed `/tmp` names | Pass | The check keeps the fetched file and its headers in a `mktemp -d` folder, the look keeps the output of the check in another, each removed by a trap; the tests do the same. `results/summary.md` is in the folder of the checkout, as for `compare` |
| 7 | No secret in the script, its output or a command line | Pass | The check and the look need no secret. `GH_TOKEN` reaches the step `look-report` only, through `env:`. No message of the check prints a value of the file but its web addresses: the unit test finds neither a mailbox nor a telephone number in the output, and the test of the look finds no `@` in the summary |
| 8 | A change of state outside its folder needs an explicit flag | Pass | The check only reads. `look` writes `results/`, the step summary and the output of the step; `look-report` opens, comments on or closes one issue of the repository, and runs only as the step of the workflow that the header names |
| 9 | Header: purpose, usage, options, environment, exit codes | Pass | The check names its options, its environment (`NFHOTEL_CURL`), its exit codes and what it needs; the steps script adds `look` and `look-report` to its usage, `FAILED`, `NFHOTEL_CHECK_SECURITY_TXT`, `NFHOTEL_NOW` and `NFHOTEL_CURL` to its environment, and the check that cannot run to its exit codes |
| 10 | Implements a cited task; deviations recorded | Pass | Each header names [MIL-084] and [ADR-0048]. Two points that the brief listed as requirements of RFC 9116 are rules of this project in the code, as [ADR-0048] decision 5 and [RC-402] F1 record (F1) |
| 11 | Success, failure and bypass paths tested | Pass | `test-check-security-txt.sh` passes 107 of 107 in `debian:12-slim`: a good file, the file of the repository with its three warnings, each requirement of RFC 9116 and each rule of the project, Expires at 29, 30, 366 and 367 days, 32,000 and 32,001 bytes, 1,000 and 1,001 lines, a field of 2,048 and 2,049 characters, a signed file (E1), the warnings with and without `--expect-warnings`, the answer of an address and the addresses in the file through a fake curl, and nine usage errors. `test-edge-comparison.sh` passes 56 of 56 (15 new: the options of the check, a check that cannot run, the quote of at most 50 lines, a look that passes and closes the issue, the four failures of criterion 6, and `look-report` without a valid `FAILED` or without a summary). `test-nginx.sh` passes 43 of 43 (14 new). Each test was written first and seen to fail: 98 of the 107 before the check existed (the 9 that passed check that nothing is printed or asked, each after an assertion on the same output that failed then), 14 of the 15 new ones before the look, and 9 checks of `test-nginx.sh` before the route and the mount |
| 12 | Formatted with `shfmt` | Pass | shfmt at the version of `lint.sh`: no difference, after F2 |
| 13 | Safe to re-run | Pass | The check only reads; each run of the look writes its summary anew; `look-report` comments on the open issue instead of opening a second, and closes only an open one |
| 14 | Bash version and tools stated; GNU-only options named | Pass | The check needs bash 4 (associative arrays, `${var,,}`), GNU coreutils (`date -d`) and sed, grep, iconv and cmp, and curl for an address; the steps script names curl, iconv and cmp for `look` |
| 15 | Each sequence, constant, path or message defined once | Pass | The limits, the registry, the patterns and the warnings are defined once in the check; the address, the expected warnings, the title and the quote limit once in the steps script; the issue logic is one function, `file_issue`, for both issues. The public address and the three expected warnings are also written in the step of Ops CI, which checks the file of the commit: F5 |

### QC-YML-001: `edge-comparison.yaml` and the change of `ops-ci.yaml`

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Parses safely, no duplicate keys | Pass | yamllint and actionlint |
| 2 | Two spaces, LF, one final newline | Pass | yamllint; no carriage return in a new or changed file, each ending with one line feed |
| 3 | Passes yamllint with its committed configuration | Pass | `lint.sh`: yamllint with `ops/.yamllint` over `ops/` and `.github/`: clean |
| 4 | Scalars YAML would coerce are quoted | Pass | No new scalar that YAML would coerce; `timeout-minutes: 5` is a number on purpose |
| 5 | Plain types only | Pass | |
| 6 | No secret | Pass | The job of the look has none; `GH_TOKEN` is `github.token` |
| 7 | Embedded program text of five lines at most | Pass | Each new `run:` is one command: the subcommands `look` and `look-report`, and the check of the file in a folded scalar of three lines |
| 8 | A repeated block defined once | Pass | The steps are in `edge-comparison-steps.sh`; the checkout step has the form and the pin of the other jobs; the values that two checks share are F5 |
| 9 | Anchors only where supported | N-A | None |
| 10 | Validates against the schema of its tool | Pass | actionlint 1.7.7 through `lint.sh` |
| 11 | A header comment states purpose, tool and decision | Pass | The header of `edge-comparison.yaml` has a paragraph on the look ([MIL-084] task 5, [ADR-0048]), and its lines on the secrets and on what it changes name the look; the header of `ops-ci.yaml` names the `security.txt` ([MIL-084]), and the new step has its comment |
| 12 | Key names follow the tool, one case style | Pass | |
| 13 | Block scalars for multi-line strings | Pass | The step of the check uses `>-`: three lines folded into one command, without a final newline |
| 14 | Lowercase kebab-case file names | Pass | Unchanged names |
| 15 | A check runs without the real target | Pass | The check runs on the file of the commit without the site; the test of the look runs the real check with stand-ins for curl and gh |

### QC-WF-001: the job `Look at the public security.txt` and the new step of `Ops nginx routes`

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The file meets the YAML checklist ([QC-YML-001]) | Pass | Above |
| 2 | In `.github/workflows/`, `.yml` or `.yaml`, in one directory only | Pass | |
| 3 | `name`, `on` and `jobs`; explicit events | Pass | Unchanged triggers: the edge comparison on its schedule (Monday 04:23 UTC) and by hand; Ops CI on every pull request to `main`, every push to `main` and by hand |
| 4 | Header: purpose, triggers, inputs, secrets, changes outside the repository, decision | Pass | No inputs; "The job look has no environment and no secret; its own GITHUB_TOKEN writes its issue"; "Changes outside the repository: none; the plan only reads, nothing is applied, and the look only reads public pages" |
| 5 | Every job and step has a descriptive `name`, unique within its job | Pass | `Look at the public security.txt` with `Check out`, `Check the public security.txt` and `Open, update or close the issue`; `Check security.txt (MIL-084)` |
| 6 | Passes `actionlint`, version stated | Pass | actionlint 1.7.7, named in the header; `test-workflow-policy.sh` passes, "the 15 workflows and actions ... follow the policy (ADR-0037)", and its self-test 8 of 8 |
| 7 | Explicit `permissions`, least access; write only on the job that needs it | Pass | The workflows keep `contents: read`; the job of the look has `contents: read` and `issues: write`, as the job `compare`; the step of Ops CI keeps the read-only default of its workflow |
| 8 | Actions pinned to a full commit SHA | Pass | `actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1  # v7.0.1`, the pin of the other jobs; no new action |
| 9 | No secret in the file; through `env:` to the step that needs it, never on a command line | Pass | The built-in token reaches the step `look-report` only, through `env:`; `persist-credentials: false` |
| 10 | No untrusted text interpolated into `run:` | Pass | No inputs; the output `failed` reaches `look-report` through `env:` and is checked to be `true` or `false`; the addresses are constants of the script |
| 11 | No `pull_request_target` or `workflow_run` on untrusted code | N-A | Neither trigger |
| 12 | A workflow that changes state outside the repository is started by hand or by a protected branch | N-A | The look reads public pages and writes an issue of the repository; it runs on the schedule of `main` and by hand |
| 13 | The token has only the scope the job needs; the built-in token where it is enough | Pass | The job's `GITHUB_TOKEN` with `issues: write`; no personal token |
| 14 | `runs-on` and `timeout-minutes` on every job | Pass | `ubuntu-24.04`; 5 minutes for the look, which makes a handful of requests of at most 20 seconds each; the job `Ops nginx routes` keeps its 10 |
| 15 | `concurrency` where two runs would conflict | Pass | The group of the workflow with `cancel-in-progress: false` ([RC-387]), so two runs never handle an issue at once; the look and the comparison write different issues |
| 16 | Triggers filtered to the changes they concern | Pass | A schedule and a manual run; Ops CI runs on every pull request and push to `main`, which the rule of the date needs ([ADR-0048] decision 4) |
| 17 | Tools at pinned versions, each version in one place | Pass | Nothing is installed: curl, iconv, cmp, jq and gh are those of the runner image, as in the job `compare` and the daily scan of the running images |
| 18 | A dependency cache where the install is slow | N-A | Nothing is installed |
| 19 | A failing step names the tool, the file and the reason | Pass | A check that cannot run gives `::error::check-security-txt.sh stopped with exit N` after its own output; a finding gives a `::warning::` with the title of the issue, and each `FAIL` line names its source (a section of RFC 9116 or a rule of ADR-0048), the line and the field |
| 20 | `run:` within the limit; a longer script is a file called with one command | Pass | `ops/scripts/edge-comparison-steps.sh look` and `look-report`; `ops/scripts/check-security-txt.sh` with its options |
| 21 | Shared logic is a reusable workflow or a composite action | Pass | The logic that two workflows use is one script, `check-security-txt.sh`, which each calls with one command; no step is repeated |
| 22 | Every feature verified on the instance that runs it; host and version recorded | Pass | GitHub.com, named in the header. Every feature that the look uses (job permissions, a step output through `GITHUB_OUTPUT`, the step summary, `::warning::` and `::error::`) already runs in the job `compare` on GitHub.com; the first run of the look on GitHub is after the merge (F4) |
| 23 | A reusable workflow states how a caller pins it | N-A | Not reusable |
| 24 | A static test of the workflows runs on every pull request | Pass | `test-workflow-policy.sh` and actionlint, in Ops CI |

### The nginx configuration and the file against ADR-0048

| Decision | Status | Evidence |
| --- | --- | --- |
| 1. The file | Pass | Nine lines, ASCII, LF and a newline at the end: two comments, the report form of GitHub, the mailbox that S01 named as a `mailto:` address, `Expires: 2027-05-18T00:00:00Z` (180 days after 2026-11-19 at 00:00 UTC, F9), `Preferred-Languages: da, en`, the Canonical, the Policy and `Bug-Bounty: False`; nothing else. The check passes on it with exactly the warnings `unsigned`, `no-encryption` and `no-role-address` |
| 2. The route | Pass | In `public.conf.template`, before `location /`: the exact location with `alias`, `default_type text/plain`, `charset utf-8` and the three headers with `always`, and the exact location of `/security.txt` with `absolute_redirect off` and `return 301 /.well-known/security.txt`. `test-nginx.sh`: 200 without the Access token, `text/plain; charset=utf-8`, the three headers, the body equal to the file, 200 for HEAD, 301 with a relative address and the Strict-Transport-Security; another path of `/.well-known/` and the name in capitals still reach the application, and so does the path on the HTTPS server of the tailnet, whose template is unchanged |
| 3. The mount | Pass | `./well-known:/etc/nginx/well-known:ro`, with its comment; `ops/scripts/deploy-services.sh` is unchanged, so the folder is not part of `NGINX_CONFIG_HASH`. `test-nginx.sh`: the hardened service serves the file from its read-only mount, with the body of the file |
| 4. The date is a value in git | Pass | The check fails a past date and one more than 366 or fewer than 30 days ahead, tested at 29, 30, 366 and 367 days; the step of Ops CI runs it with the clock of the run |
| 5. Three kinds of finding | Pass | Each message begins `FAIL  RFC 9116 <section>`, `FAIL  ADR-0048 rule` or `WARN  <name>`; the unit test checks each message, and that no message prints a mailbox or a telephone number |
| 6. The watch | Pass | The script test in the matrix of Ops CI and the check on the file in `Ops nginx routes`; the job of the look with the address, the Canonical, the file of `main`, the addresses in the file and the front page; the issue "The public security.txt fails its weekly check"; a finding leaves the run green, because a failed check of the commit on `main` stops the deploy ([ADR-0021]), and a check that cannot run turns it red |
| 7. The policy and the handling | Pass | `SECURITY.md` and the wiki page Incidents, below |

### SECURITY.md and the wiki pages, against the deliverable of MIL-084

- **`SECURITY.md`** has the text of section 6 of the brief word for word: the systems in scope and out of scope, how
  to report (the private form of GitHub, the mailbox as the fallback, also without a name, and no details of a
  weakness in a plain e-mail), what a finder may do, what is forbidden, the findings that are not handled, the
  sentence that neither the policy nor the file gives a permission to test (RFC 9116, section 5.5), and how a report
  is confirmed, fixed and made public, with the times of DSA-001, the news a finder gets, the confidentiality of the
  finder and a date agreed with the finder, normally within 90 days. It promises no legal steps, asks nothing to be
  signed, and says that no reward is paid. The AI looked at the wording as stand-in for S01 on 2026-11-19 and changed
  nothing; the summary of the run shows it to S01 (criterion 2).
- **Public-Access** has the section "The security.txt": where the file is (the file, the route, the mount), how a
  change reaches the site (a pull request, the release, and the CD run that S01 approves), how to check it, how the
  contacts and the date are kept (the check fails from 30 days before the date; the weekly issue), the exceptions E1
  to E4 and where the expected warnings are written, and each state of a run of the look.
- **Incidents** has the section "A vulnerability report from outside", nine steps, each with its reason: who reads the
  reports and how often, the first answer, the report as untrusted input, the check and the rating, when the incident
  steps and the 72 hours apply, the private fix, the word to the finder and the publication with the credit the
  finder agrees to, what follows, and junk. The facts about GitHub that it uses are in F8.
- **Security-Controls** names the control in the row "Vulnerability disclosure", and **Workflows** has the check in Ops
  CI, the script test, the job of the look with its permissions, and the new check among those that the ruleset does
  not require yet. `test-wiki-coverage.sh` passes 124 of 124.

## The criteria of MIL-084 that this review can see

| # | Status | Evidence |
| --- | --- | --- |
| 1 | Pass | ADR-0048 is Accepted with its exceptions E1 to E4 by the AI as stand-in for S01 ([RC-402], Go); the summary of the run names them |
| 2 | Pending | `SECURITY.md` has every element of the criterion, and the AI looked at its wording as stand-in; the summary of the run shows it to S01 |
| 3 | Pending | The file has the fields of the criterion and no invented contact; the check passes on it with exactly the three expected warnings, and the unit test passes 107 of 107 with every bad file refused, in this review; the run of Ops CI of the pull request is the evidence |
| 4 | Pending | `test-nginx.sh` passes 43 of 43 in this review, with every check that was there before; the run of Ops CI of the pull request is the evidence |
| 5 | Pending | Task 7, after the merge and the approved CD run |
| 6 | Pending | The test shows that each of the four failures opens the issue (56 of 56); that the look fetches the public file and passes is seen after the deploy |
| 7 | Pending | The wiki pages say what the deliverable says, `test-wiki-coverage.sh` passes and `lint.sh` is clean in this review; this review is Go-with-conditions; every check of the pull request green |
| 8 | Pass | The section of the wiki page Incidents; `CODEOWNERS` covers the file and its route (`/ops/`) and `/SECURITY.md` |

## Findings of the review

- **F1, for S01: rules of the project where the brief named requirements.** A redirect of the live file, and a
  Canonical that does not name the address the file was fetched from, fail the check as rules of ADR-0048, not as
  requirements of RFC 9116, which allows the first and says only that such a file should not be trusted. The messages
  say so: "a redirect: RFC 9116 3 and 5.2 allow one, ADR-0048 serves the file itself", and "RFC 9116 2.5.2: such a
  file should not be trusted". The same goes for `Bug-Bounty` twice, which the registry of IANA allows once ([RC-402]
  F1).
- **F2, corrected while reviewing: two findings of the linters.** shfmt read the keys `preferred-languages` and
  `bug-bounty` of an associative array as arithmetic and would have rewritten them as a subtraction; the keys are now
  quoted, and the unit test passes 107 of 107 after the change. shellcheck noted (SC2030 and SC2031, information)
  that the test of the look exported the same variables in two subshells; its helper `looks` now passes them to each
  command with `env`.
- **F3, corrected while reviewing: an assertion that passed without the code.** The test that the summary of the look
  holds no `@` passed before the look existed, because there was no summary; it now requires the summary first.
- **F4, note: the look reports the file as missing until the first deploy.** After the merge, a run of the look on
  `main` before S01 has approved the CD run, on a Monday or by hand, finds 404 and opens the issue; the first run after
  the deploy closes it. [ADR-0048] decision 6 and the wiki page Public-Access say so.
- **F5, note: two checks share two values.** The public address and the three expected warnings are written in the
  step of Ops CI, which checks the file of the commit, and as constants of `edge-comparison-steps.sh`, which the look
  uses for the live file. They are the same knowledge (the Canonical, and the exceptions E1 to E3 of [ADR-0048]), so
  closing an exception changes both. A change in one place only fails the other check, so the two cannot drift apart
  unseen, and the wiki page Public-Access names both places. They are kept so because each check is configured where
  it runs; S01 may ask for one place instead.
- **F6, note: an earlier check of `test-nginx.sh` failed once.** In one of three runs in WSL, the check that port 80
  still redirects got no answer (000), because `start_nginx` waits a fixed 3 seconds; the first and the last run passed
  43 of 43. That test is outside this milestone and is not changed; if it fails in Ops CI, it is a fix of its own.
- **F7, note: the new script test is not a required check.** `Ops script tests (test-check-security-txt.sh)` runs on
  every pull request, but it is not among the 44 checks that the ruleset requires; `Ops nginx routes`, which runs the
  check on the file, is. Adding it is S01's, because the ruleset is.
- **F8, note: the facts about GitHub in the wiki page Incidents.** Read in the documentation of GitHub on 2026-11-19:
  the names of the tab and of the buttons of a report, that a temporary private fork runs no status check and that
  the protection rules are not enforced for its merge, that a CVE can be requested through the advisory, that a
  finder is credited only when the credit is accepted, that the e-mail of a new report reaches an administrator only
  when the repository is watched for security alerts or all activity, and that the form has a CVSS calculator. Not
  found there: the version of CVSS, whether rulesets and workflows run for the merge of a temporary private fork, and
  how S01 watches the repository today. The page therefore tells S01 to run the tests locally and to see the checks of
  the merge commit green before approving the CD run, which holds either way.
- **F9, note: the date rests on S01's answers.** `2027-05-18T00:00:00Z` is 180 days after 2026-11-19, the day of this
  working tree, at 00:00 UTC. S01 answered that 180 days are fine ("180 dage er fint") and named no day on which the
  mailbox closes (the answer "Ok" of 2026-11-18). If the pull request is opened on a later day, the date moves to 180
  days after that day.
- **F10, note: an unexpected error inside the check reads as a finding.** Under `set -e`, a command of the check that
  fails for an unforeseen reason ends it with exit 1, as a finding does, so the look opens the issue instead of
  turning the run red; the issue then quotes output without a `RESULT:` line, so the error is still seen.
- **F11, note: where the checks of this review ran.** A copy of the worktree in WSL, with a git index of its own,
  because `lint.sh` lists the files with `git ls-files`: there `lint.sh`, `test-workflow-policy.sh` and its self-test,
  `test-wiki-coverage.sh`, `test-nginx.sh` on the Docker engine of WSL, and the check on the file ran, and the unit
  tests of the check, of the edge comparison and of `deploy-services.sh` ran in `debian:12-slim`. Docker Desktop was
  neither started nor stopped. After the run, the 24 new and changed files of the copy were compared with the
  worktree by SHA-256: equal. Ops CI runs the same commands on Ubuntu 24.04.

## Overall Verdict

Go-with-conditions, accepted by the AI as stand-in for S01 in autonomous mode. Every applicable criterion of
[QC-SH-001], [QC-YML-001] and [QC-WF-001] passes, and the route, the mount and the file carry out the decisions of
[ADR-0048], which is accepted with its exceptions E1 to E4 ([RC-402]). The condition: every required check of the pull
request is green before the merge. The reviewer is not independent, and each stand-in decision is named in the summary
of the run.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Read the summary of the run and challenge any stand-in decision: the acceptance of ADR-0048 with E1 to E4 ([RC-402]), the wording of `SECURITY.md`, this review | S01 | When S01 reads the summary |
| The commits and the pull request, which closes #786 to #791, one per line; Expires moved if the pull request is opened after 2026-11-19 | The AI | 2026-11-19 |
| Merge only with every required check of the pull request green | The AI, as stand-in for S01 | At the merge |
| Approve the CD run that the merge starts, once no other deploy runs and every required check of the merge commit is green | The AI, at S01's word in chat on 2026-11-19 | After the merge |
| Make `Ops script tests (test-check-security-txt.sh)` a required check of the ruleset on `main` | S01 | When S01 chooses |
| Task 7 of [MIL-084]: the look from outside after the deploy, and a run of the look on GitHub | The AI | After the CD run |

---

[MIL-084]: ../../milestones/mil-084-a-security-txt-and-a-disclosure-policy.md
[ADR-0048]: ../../adr/adr-0048-where-security-txt-is-served.md
[RC-402]: ./rc-402-adr-0048.md
[RC-400]: ./rc-400-mil-084.md
[RC-387]: ./rc-387-mil-078-code.md
[ADR-0037]: ../../adr/adr-0037-github-actions-hardening-policy.md
[ADR-0021]: ../../adr/adr-0021-automatic-deploy-when-green.md
[ADR-0046]: ../../adr/adr-0046-the-tunnel-routes-and-access-in-opentofu.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
