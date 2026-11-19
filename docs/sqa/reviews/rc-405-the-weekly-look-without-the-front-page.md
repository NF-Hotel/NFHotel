# RC-405 Delta review of MIL-084: the weekly look without the front page

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-405 |
| CrossReference | [MIL-084], [ADR-0048], [RC-403], [RC-402], [ADR-0037], [ADR-0046], [MIL-066], [QC-SH-001], [QC-YML-001], [QC-WF-001], [QC-ADR-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-19 | Accepted | mikemike | AI as stand-in for S01 | Initial version; Go; mode autonomous, not independent | pending |

---

## Artifact Under Review

- Instance reviewed: the change of the branch `mil-084-look-fix`, cut from `main` at 46b29fb (the merge of the pull
  request of [MIL-084] tasks 1 to 6), before its commit:
  - `ops/scripts/edge-comparison-steps.sh`: the subcommand `look` no longer gives the check the front page
    (`--hsts-like`); the constant with its address is gone, and the comment above the constants says why;
  - `ops/tests/test-edge-comparison.sh`: the fake site answers the front page as the edge answered the runner, by
    itself and without the headers of the site; the test of the arguments expects no `--hsts-like`; a new test asks
    that the look fetches each address that the file names and not the front page; the test of a different
    Strict-Transport-Security, which the look no longer sees, is removed;
  - `.github/workflows/edge-comparison.yaml`: the paragraph of the header on the look;
  - [ADR-0048]: decisions 2, 5 and 6, two consequences, the CrossReference and a row of its Version History;
  - the wiki pages Public-Access (the check by hand keeps the comparison and gets the line that the weekly issue
    loses) and Workflows.
- Why: the first run of the look on GitHub, run 44498395515 of `NF Hotel edge comparison`, started by hand on `main`
  at 46b29fb after the deploy of [MIL-084], found one failure in both of its attempts (09:39 and 09:41 UTC on
  2026-11-19): "ADR-0048 rule: Strict-Transport-Security is 'max-age=2592000', and https://nfhotel.devmatiker.com/
  sends ''". It opened issue 835 and then commented on it. The run stayed green, as a finding does ([ADR-0048],
  decision 6).
- Checklist used: [QC-SH-001], [QC-YML-001] and [QC-WF-001] for the code, as in [RC-403], and [QC-ADR-001] for the
  change of [ADR-0048], as in [RC-402]. Only the criteria that the change touches are listed below; every other
  criterion keeps its result of [RC-403] or [RC-402], because the change does not reach what it checks.
- Scope: delta review of the change above.
- Language and domain: n/a (technical type)
- Language reviewer: none
- Mode: autonomous, not independent. S01 switched [MIL-084] to autonomous mode in chat on 2026-11-19 ("Automode og
  deploy"); the artificial intelligence (AI) assistant wrote the change and reviews it as stand-in for S01.

## The cause

| Evidence | What it shows |
| --- | --- |
| The output of the check in the log of run 44498395515 | Only the comparison with the front page fails: the file answers, equals the file of `main`, has its type and its headers, and every address that it names answers |
| The access log of the proxy on the virtual machine, read over SSH after the run | The requests of the runner (`curl/8.5.0`) for `/.well-known/security.txt`, two in each attempt (09:39:21, 09:39:22, 09:41:39 and 09:41:40 UTC), each answered 200; no request of the runner for `/` |
| The look of task 7 from a workstation of the team, 09:38:53 to 09:38:56 UTC | The front page answers 200 with `strict-transport-security: max-age=2592000`, the value of the file, and the check with `--hsts-like` passes |

The edge answers the front page to a runner of GitHub by itself, without the headers of the site, and the request
never reaches the proxy. The check records no status or body of that answer, and the site is not at fault. The look
cannot see the header of the front page from a runner, so it stops asking for it, and the comparison stays with the
check by hand that the wiki page Public-Access describes and with task 7 of [MIL-084].

## Checklist Results

### QC-SH-001: `edge-comparison-steps.sh` and `test-edge-comparison.sh`

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 2 | Expansions quoted, lists as arrays, `[[ ]]` and `$(...)` | Pass | The options of `look` stay the array `args`, now without `--hsts-like` and its address |
| 4 | shellcheck and `bash -n`, every `disable` explained | Pass | `lint.sh`: "all linters are clean"; no new `disable` |
| 9 | Header: purpose, usage, options, environment, exit codes | Pass | The usage line of `look` names the Canonical and the addresses that the file names; nothing else in the header changed |
| 10 | Implements a cited task; deviations recorded | Pass | [MIL-084] task 5 and criterion 6, which never asked for the front page; the change of decision 6 is recorded in [ADR-0048] with its evidence |
| 11 | Success, failure and bypass paths tested | Pass | Test first, in `debian:12-slim` on the Docker engine of WSL: with the new test and the old script, 49 passed and 6 failed: the arguments, the look that passes (with the line of the runner), the addresses asked (the front page among them), the close of the issue, and the quotes of a different file and of a near date (each with the extra line). With the change, 55 passed, 0 failed; a missing file, a different file, a date fewer than 30 days ahead and a named address that fails or leaves its host still open the issue |
| 12 | Formatted with `shfmt` | Pass | `lint.sh` |
| 15 | Each sequence, constant, path or message defined once | Pass | The constant of the front page is gone with its last use, so no dead constant stays; the test keeps its own `FRONT_PAGE` for the fake site and for the assertion that the look does not ask it |

### QC-YML-001 and QC-WF-001: `edge-comparison.yaml`

Only the header comment changed.

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| QC-YML-001 2, 3 | Two spaces, LF, one final newline; yamllint | Pass | `lint.sh`; no carriage return in a changed file |
| QC-YML-001 11 | A header comment states purpose, tool and decision | Pass | The paragraph on the look says what it checks, what it leaves out and why |
| QC-WF-001 4 | Header: purpose, triggers, inputs, secrets, changes outside the repository, decision | Pass | As above; the triggers, the secrets and the changes outside the repository are unchanged |
| QC-WF-001 6 | Passes `actionlint`, version stated | Pass | actionlint 1.7.7 through `lint.sh`; `test-workflow-policy.sh`: "the 15 workflows and actions ... follow the policy (ADR-0037)"; its self-test passes |
| QC-WF-001 22 | Every feature verified on the instance that runs it | Pass | The look ran on GitHub.com in run 44498395515; the changed look uses no new feature, and it runs there by hand after the merge (F2) |

### QC-ADR-001: the change of ADR-0048

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 4 | Decision is stated in one or two unhedged sentences and matches one of the evaluated options | Pass | The sentence of the decision is unchanged; decision 6 now says what the watch leaves out and why, with the evidence of 2026-11-19, and decisions 2 and 5 say who compares the two values |
| 5 | Consequences list both Positive and Negative outcomes | Pass | The positive outcome of the look no longer names a different Strict-Transport-Security; the negative one on the value in two places says that only the check by hand and task 7 find a change of one alone |
| 7 | Affected Artifacts are listed as links, and each is also in CrossReference | Pass | Unchanged; this review joins the CrossReference |
| 8 | Status changes are recorded as new Version History rows with a Change summary and Commit link; retained rows are never rewritten | Pass | A new row of 2026-11-19, `Accepted` after this review, with `pending` until its commit; the row of 2026-11-18 becomes `Deprecated`, its text unchanged; two rows |
| 10 | Written in business or technical language of the domain, with no unexplained abbreviations | Pass | No new abbreviation |
| 11 | Status is one of the allowed values | Pass | `Deprecated` and `Accepted` |

### The wiki pages

The wiki pages have no checklist; they are read against the change. Public-Access: step 3 of "Change it" says that
the check by hand adds `--hsts-like`, which the weekly look cannot make, when to run it, and what
`Strict-Transport-Security is ...` means (moved there from the list of the weekly issue); "The weekly look" says what
it leaves out and why. Workflows: the row "Does" of the edge comparison says the same. `test-wiki-coverage.sh`
passes.

### The other tests

In WSL on a copy of the branch: the unit test of the check, 107 passed, 0 failed; `test-deploy-services-env.sh`, 17
passed, 0 failed; `test-nginx.sh`, 43 passed, 0 failed; the check on the file of the branch with the three expected
warnings passes.

## Findings

- **F1 (observation, for S01): the edge answers the front page to a runner of GitHub by itself.** The rule that does
  it is part of the Cloudflare account, which this milestone does not change; the code of [ADR-0046] holds the
  tunnel and Access, not such a rule. A monitor in a datacenter sees the front page only as the edge answers it.
  Nothing to do in this milestone.
- **F2 (minor, closed at the gate of [MIL-084]): the changed look has not run on GitHub yet.** It runs by hand on
  `main` after the merge; the gate review records its result, and a look that passes closes issue 835.
- **F3 (observation): the weekly look no longer watches the Strict-Transport-Security of the file.** A change of the
  value of the application alone is found by the check by hand and by task 7, as [ADR-0048] now says. The location
  names its value in one line, and the length of the policy stays S01's open decision ([MIL-066]).

## Overall Verdict

Go, by the AI as stand-in for S01 in autonomous mode. Every criterion that the change touches passes, the test that
failed without the change passes with it, and `lint.sh`, the workflow policy and its self-test and the wiki coverage
pass. With this change the look can meet criterion 6 of [MIL-084]; the gate checks it on GitHub after the merge. The
reviewer is not independent, and the decision is named in the summary of the run.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Merge with every required check of the pull request green; run `NF Hotel edge comparison` by hand on `main` and record the look in the gate review of MIL-084 (F2) | The AI, as stand-in for S01 | 2026-11-19 |
| Decide whether a monitor in a datacenter should see the front page of the site (F1) | S01 | When S01 looks at the security settings of the zone |
| Read the summary of the run and challenge this stand-in decision | S01 | When S01 reads the summary |

---

[MIL-084]: ../../milestones/mil-084-a-security-txt-and-a-disclosure-policy.md
[ADR-0048]: ../../adr/adr-0048-where-security-txt-is-served.md
[RC-403]: ./rc-403-mil-084-code.md
[RC-402]: ./rc-402-adr-0048.md
[ADR-0037]: ../../adr/adr-0037-github-actions-hardening-policy.md
[ADR-0046]: ../../adr/adr-0046-the-tunnel-routes-and-access-in-opentofu.md
[MIL-066]: ../../milestones/mil-066-findings-of-7-november.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[QC-ADR-001]: ../../../framework/qc/artifacts/qc-adr.md
