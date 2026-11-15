# RC-387 Code review of MIL-078: the tunnel routes and Access in OpenTofu, and their weekly comparison

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-387 |
| CrossReference | [MIL-078], [ADR-0046], [RC-386], [RC-338], [ADR-0037], [ADR-0021], [RC-354], [QC-SH-001], [QC-YML-001], [QC-WF-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-15 | Accepted | mikemike | AI as stand-in for S01 | Initial version; mode autonomous, not independent; Go-with-conditions: every check of the pull request green | pending |

---

## Artifact Under Review

- Instance reviewed: the code and the documents of [MIL-078] tasks 2 to 4 on the branch `mil-078-edge-as-code`:
  - task 2: `ops/infra/cloudflare/` (`versions.tf`, `variables.tf`, `main.tf`, `.terraform.lock.hcl`,
    `edge.env.example` and `.gitignore`), `ops/scripts/edge-tofu.sh`, and the job `Ops edge code (MIL-078)` of
    `.github/workflows/ops-ci.yaml`;
  - task 3: `.github/workflows/edge-comparison.yaml`, `ops/scripts/edge-comparison-steps.sh`, and
    `ops/tests/test-edge-comparison.sh` with its row among the script tests of `ops-ci.yaml`;
  - task 4: the wiki pages Public-Access, Technology-Choices, Workflows, CI-CD and Environment-Files, and
    `ops/README.md`. The three ADR rows of task 4 are reviewed in [RC-386].
- Checklist used: [QC-SH-001] (`qc-programming-shell`), [QC-YML-001] (`qc-yaml`) and [QC-WF-001] (`qc-workflow`). The
  framework has no checklist for OpenTofu code, so that code is read against the decisions of [ADR-0046] below, as
  criterion 6 of [MIL-078] says. The wiki pages have no checklist of their own; they are read against the deliverable
  of [MIL-078].
- Scope: full review of the new and changed files
- Language and domain: n/a (technical type)
- Language reviewer: none
- Mode: autonomous, not independent, as in [RC-354]. S01 accepted the plan of [MIL-078] in chat on 2026-11-12
  ([RC-338], Go), and on 2026-11-15 asked that the open milestones be finished without a decision of theirs ("uden at
  jeg skal ta stilling til noget"), with the merge. The reviewer is the AI as stand-in for S01, and the code was
  written by an AI agent that the reviewer started.

## Checklist Results

### QC-SH-001: `edge-tofu.sh`, `edge-comparison-steps.sh` and `test-edge-comparison.sh`

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | `#!/usr/bin/env bash` and `set -euo pipefail`, or a comment explains the exception | Pass | The two scripts keep both; the test keeps the convention of `ops/tests`, `set -uo pipefail` with each assertion counted |
| 2 | Expansions quoted, lists as arrays, `[[ ]]` and `$(...)` | Pass | The docker options are the arrays `opts`, `values` and `STATE`; the values are read with `mapfile`, the status of the pipe from `PIPESTATUS` into an array; shellcheck clean |
| 3 | Naming | Pass | snake_case functions (`run_tofu`, `init_tofu`, `need_token`, `value_options`, `mask_addresses`, `write_summary`, `join_names`) and variables; `CODE`, `IMAGE`, `STATE`, `STATE_FILE`, `SUMMARY`, `ISSUE_TITLE`, `SECRETS`, `JQ_REDACT` and `JQ_REDUCE` in UPPER_SNAKE, read-only |
| 4 | shellcheck and `bash -n`, every `disable` explained | Pass | shellcheck at the version of `lint.sh` over the three files: clean; the two `SC2016` in the steps script and the `SC2015` of the test each have their reason |
| 5 | Errors on standard error with `error:`, a usage line for bad arguments | Pass | `edge-tofu.sh` stops with `die` (`error: ...`) without a certificate bundle, without a token, or for `apply` without a terminal; the steps write `::error::` lines to standard error; a bad argument prints the usage and exits 2 |
| 6 | `mktemp` and `trap ... EXIT`, no fixed `/tmp` names | Pass | The work folder of the provider and the folder of the plan are `mktemp -d`, removed by a trap; the test checks that the work folder is gone after a run |
| 7 | No secret in the script, its output or a command line | Pass | The token is read from `CLOUDFLARE_API_TOKEN` or with `read -s`, and reaches docker as `-e CLOUDFLARE_API_TOKEN`, the values as `-e TF_VAR_*` or `--env-file`, by name only; the test finds none of its made-up values on a command line. The plan stream holds the identifiers and is reduced by `jq` before anything is printed, and every message of tofu and docker is shown with identifiers and addresses replaced |
| 8 | A change of state outside its folder needs an explicit flag | Pass | Only `apply` changes the account: it needs a terminal and the `yes` at the prompt of tofu. `format` and `lock` rewrite files of `ops/infra/cloudflare/` only when asked by name; the step `report` writes the issue of the workflow only |
| 9 | Header: purpose, usage, options, environment, exit codes | Pass | Each script names its subcommands, its environment, its exit codes and what it needs; `edge-tofu.sh` says which subcommand changes anything outside the repository, and for whom |
| 10 | Implements a cited task; deviations recorded | Pass | Each header names [MIL-078] and [ADR-0046]; the commit names its tasks (`Task: MIL-078#N`); no deviation from tasks 2 and 3 |
| 11 | Success, failure and bypass paths tested | Pass | `test-edge-comparison.sh` passes 41 of 41 in `debian:12-slim`, with stand-ins for docker, gh and `edge-tofu.sh`: the options of each subcommand, the token and the values by name, the refusals, the removal of the work folder; the steps with every secret, with missing ones and with an unset one; a clean plan, a difference, a failed plan, a plan without a summary and addresses that are not a list, each without a made-up value in the output; the issue opened, commented on, closed and left alone |
| 12 | Formatted with `shfmt` | Pass | shfmt at the version of `lint.sh`: no difference |
| 13 | Safe to re-run | Pass | `fmt`, `validate` and `plan` only read; each run has a new work folder and state; `report` comments on the open issue instead of opening a second |
| 14 | Bash version and tools stated; GNU-only options named | Pass | `edge-tofu.sh` needs bash 4.4 (an empty array under `set -u`), the steps bash 4; both name docker, jq and gh where they use them |
| 15 | Each sequence, constant, path or message defined once | Pass | The image and its digest are in `edge-tofu.sh` only; the redaction is one jq function, used for the plan and for the other output; the names of the secrets are in the steps script, and the workflow passes one yes or no per name |

### QC-YML-001: `edge-comparison.yaml` and the change of `ops-ci.yaml`

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Parses safely, no duplicate keys | Pass | yamllint and actionlint |
| 2 | Two spaces, LF, one final newline | Pass | yamllint; no carriage return in a new or changed file |
| 3 | Passes yamllint with its committed configuration | Pass | `lint.sh`: yamllint with `ops/.yamllint` over `ops/` and `.github/`: clean |
| 4 | Scalars YAML would coerce are quoted | Pass | The cron expression `"23 4 * * 1"` |
| 5 | Plain types only | Pass | |
| 6 | No secret | Pass | Only the names of the secrets |
| 7 | Embedded program text of five lines at most | Pass | Every `run:` is one line that calls a subcommand of a script |
| 8 | A repeated block defined once | Pass | The steps are in `edge-comparison-steps.sh`; the checkout step has the form and the pin of the other workflows |
| 9 | Anchors only where supported | N-A | None |
| 10 | Validates against the schema of its tool | Pass | actionlint 1.7.7 through `lint.sh` |
| 11 | A header comment states purpose, tool and decision | Pass | The workflow states its purpose, triggers, secrets, what it changes and [ADR-0046]; the new job of `ops-ci.yaml` has its comment, and the header of the file names [MIL-078] |
| 12 | Key names follow the tool, one case style | Pass | |
| 13 | Block scalars for multi-line strings | N-A | None |
| 14 | Lowercase kebab-case file names | Pass | `edge-comparison.yaml` |
| 15 | A check runs without the real target | Pass | The test of the steps runs with stand-ins; the job `Ops edge code (MIL-078)` validates without a token; the workflow without its secrets compares nothing and stays green |

### QC-WF-001: `edge-comparison.yaml` and the job `Ops edge code (MIL-078)`

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The file meets the YAML checklist ([QC-YML-001]) | Pass | Above |
| 2 | In `.github/workflows/`, `.yml` or `.yaml`, in one directory only | Pass | |
| 3 | `name`, `on` and `jobs`; explicit events | Pass | `schedule` (Monday 04:23 UTC) and `workflow_dispatch` |
| 4 | Header: purpose, triggers, inputs, secrets, changes outside the repository, decision | Pass | No inputs; the seven secrets of the environment `cloudflare-read`; "Changes outside the repository: none" |
| 5 | Every job and step has a descriptive `name`, unique within its job | Pass | |
| 6 | Passes `actionlint`, version stated | Pass | actionlint 1.7.7, named in the header; `test-workflow-policy.sh` passes with the new workflow: "the 15 workflows and actions ... follow the policy (ADR-0037)" |
| 7 | Explicit `permissions`, least access; write only on the job that needs it | Pass | `contents: read` for the workflow; `contents: read` and `issues: write` for its one job; the new job of Ops CI keeps the read-only default of that workflow |
| 8 | Actions pinned to a full commit SHA | Pass | `actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1  # v7.0.1`, the pin of the other workflows; tofu runs from an image pinned by digest, so no new action and no new publisher |
| 9 | No secret in the file; through `env:` to the step that needs it, never on a command line | Pass | The token and the values reach the step of the plan only, through `env:`, and tofu through `docker -e NAME`; the step of the check gets only `secrets.X != ''`, a yes or a no |
| 10 | No untrusted text interpolated into `run:` | Pass | No inputs; the output `differs` reaches the step `report` through `env:` and is checked to be `true` or `false` |
| 11 | No `pull_request_target` or `workflow_run` on untrusted code | N-A | Neither trigger |
| 12 | A workflow that changes state outside the repository is started by hand or by a protected branch | N-A | It changes nothing outside the repository: the token only reads, and nothing is applied. It runs on the schedule of `main` and by hand, and its environment is limited to `main` |
| 13 | The token has only the scope the job needs; the built-in token where it is enough | Pass | The job's `GITHUB_TOKEN` writes the issue; the Cloudflare token can read the tunnel and Access and nothing else ([ADR-0046] decision 5) |
| 14 | `runs-on` and `timeout-minutes` on every job | Pass | `ubuntu-24.04`; 15 minutes for the comparison, 10 for `Ops edge code (MIL-078)` |
| 15 | `concurrency` where two runs would conflict | Pass | One group per workflow with `cancel-in-progress: false`, so two runs never handle the issue at once |
| 16 | Triggers filtered to the changes they concern | Pass | A schedule and a manual run only; the new job of Ops CI runs under the filters of that workflow |
| 17 | Tools at pinned versions, each version in one place | Pass | OpenTofu 1.13.9 by digest in `edge-tofu.sh`; the provider 5.29.0 in `versions.tf` and by its checksums in the lock file; `jq` and `gh` are those of the runner image, as in the daily scan of the running images |
| 18 | A dependency cache where the install is slow | N-A | Optional; the provider, about 200 MB, is downloaded once a week |
| 19 | A failing step names the tool, the file and the reason | Pass | `::error::` lines with the summary of each error of tofu and its object, redacted |
| 20 | `run:` within the limit; a longer script is a file called with one command | Pass | `ops/scripts/edge-comparison-steps.sh check`, `compare` and `report` |
| 21 | Shared logic is a reusable workflow or a composite action | N-A | The steps are used by this workflow only |
| 22 | Every feature verified on the instance that runs it; host and version recorded | Pass | GitHub.com, named in the header. `environment:` runs in `cd.yaml` and `secrets.X != ''` in `ci.yaml` on GitHub.com; `::add-mask::` is new here and is a documented command of the runner. The first run on GitHub, the schedule of 2026-11-16 or a manual run, shows the path without secrets; the masking of the addresses is seen in the first run with them (task 5) |
| 23 | A reusable workflow states how a caller pins it | N-A | Not reusable |
| 24 | A static test of the workflows runs on every pull request | Pass | `test-workflow-policy.sh` and actionlint, in Ops CI |

### The OpenTofu code against ADR-0046

| Decision | Status | Evidence |
| --- | --- | --- |
| 1. What is in code | Pass | `main.tf`: the configuration of the tunnel `nf-hotel` with its routes in order, first the public name with the path `(?i)^/admin` and `access` required with the team name and the AUD tag read from the application, then the public name without a path, both to `http://nginx:8082`, then `http_status:404`; the application `NF Hotel back office`, self-hosted, `168h`, for the public name and `/admin`, with the policy `Team` at precedence 1; the policy `Team`, `allow`, one e-mail rule per address. The tunnel, its token, the DNS record and the zone settings are not in code |
| 2. The tool and its pins | Pass | The image `ghcr.io/opentofu/opentofu:1.13.9-minimal` by the digest of its index, run as the calling user with `--cap-drop ALL`, `no-new-privileges`, a read-only root, a `/tmp` in memory, the certificate authorities read-only and the code read-only except for `format` and `lock`; `required_version = "~> 1.13.0"`; the provider exactly 5.29.0, with checksums for several platforms in the lock file; every init but `lock` reads the lock file read-only |
| 3. No stored state | Pass | No backend block; `init -backend=false`; the state of a plan or an apply in `/state`, a file system in memory of its container; one `import` block per object, with its identifiers from variables |
| 4. The identifiers and the addresses outside git | Pass | Six variables, each with a validation (32 hexadecimal characters, a UUID, a team name, a list of addresses), `team_emails` sensitive; `edge.env` ignored by git, next to the GitHub template for Terraform, and `edge.env.example` without a value; no zone identifier |
| 5. Two tokens | Pass | `edge-tofu.sh` takes the token from the environment or asks for it without echo, and refuses it as an argument; the workflow gives the token that reads to the step of the plan only |
| 6. The weekly comparison | Pass | QC-WF-001 above, and the test of the steps |
| 7. Changing the edge | Pass | `apply` runs only in a terminal and stops at the prompt of tofu; nothing in CI can apply |
| 8. The checks | Pass | `tofu fmt -check` and `tofu validate` in the job `Ops edge code (MIL-078)`, and `test-edge-comparison.sh` among the script tests |

### The wiki pages, against the deliverable of [MIL-078]

- **Public-Access** has the section "The edge as code": what is in code and what stays in the dashboard, the settings
  outside git with where each value is found, a change through a pull request and an apply by S01 or S03, the token
  that writes, the weekly comparison with each state of the run, the reading of the issue, the set-up of the
  environment with its branch rule before its secrets, the renewal of the token and the emergency. The earlier steps
  of the page say where the code now applies.
- **Technology-Choices** adds OpenTofu among the chosen tools, Terraform and Pulumi among the rejected ones, and a
  section on OpenTofu; the four threads it cites exist with the titles it gives them.
- **Workflows** has the workflow, the environment `cloudflare-read`, the secrets of the edge and the pins that
  Dependabot does not update; **CI-CD**, **Environment-Files** and `ops/README.md` name the new job, `edge.env` and
  `ops/infra/cloudflare/`. `test-wiki-coverage.sh` passes 125 of 125.

## The criteria of MIL-078 that this review can see

| # | Status | Evidence |
| --- | --- | --- |
| 1 | Pass | ADR-0046 is Accepted with [RC-386] Go |
| 2 | Pending | In this review `edge-tofu.sh fmt` exited 0 and `edge-tofu.sh validate` said "The configuration is valid" in the pinned image, and `test-workflow-policy.sh` passes with the new workflow; the job `Ops edge code (MIL-078)` of the pull request is the evidence |
| 3 | Pending | Task 5: the first plan with the token that reads, which S01 creates |
| 4 | Pending | Task 5: the drill, on S01's yes |
| 5 | Pending | Task 5: a change applied by S01 with the token that writes |
| 6 | Pending | The wiki pages and the three ADR rows above and in [RC-386]; `test-wiki-coverage.sh` 125 of 125; this review; every required check of the pull request |

## Findings of the review

- **F1, note: the plan path, checked in this review.** With made-up identifiers, a made-up address and a made-up
  token, and a stand-in for docker that ran only the call of the plan with `--network none`, `edge-tofu.sh plan-json`
  installed the provider, handed the values on by name, kept its state in memory, and stopped at its first call to the
  API of Cloudflare ("failed to make http request ... dial tcp: lookup api..."), with a summary of the changes in the
  stream. So the init without a backend, `-state` in memory and the import identifiers work, and nothing reached
  Cloudflare. The first plan against the account is task 5.
- **F2, note: the reduction of the plan reads the stream of OpenTofu.** It keeps the messages `change_summary`,
  `planned_change` and `diagnostic`; the test feeds it a stream written in that form, and the plan of F1 wrote the
  same types. The first real stream is read at task 5.
- **F3, note: the environment `cloudflare-read`.** GitHub makes an environment the first time a job names it, without
  a branch rule. Until S01 opens it and limits it to `main` (step 2 of "Set up the weekly comparison"), it holds no
  secret, and a run warns "not compared" and stays green. The first scheduled run is on 2026-11-16 at 04:23 UTC.
- **F4, note: two new checks that are not required.** `Ops edge code (MIL-078)` and `Ops script tests
  (test-edge-comparison.sh)` run on every pull request but are not among the 44 checks that the ruleset requires;
  adding them is S01's, because the ruleset is.
- **F5, note: the pins are kept by hand.** Dependabot reads neither the digest in `edge-tofu.sh` nor the provider, and
  its `opentofu` ecosystem is not configured; [ADR-0046] decision 8 and the Dependabot row of the wiki page Workflows
  say so.
- **F6, note: where the checks of this review ran.** `lint.sh` ran from Git Bash against Docker Desktop with the path
  conversion of MSYS turned off; the script test, `fmt`, `validate` and the plan of F1 ran from WSL, with an empty
  Docker configuration, because the credential helper of Docker Desktop failed in WSL for a public image. Ops CI runs
  the same commands on Ubuntu.

## Overall Verdict

Go-with-conditions. Every applicable criterion of [QC-SH-001], [QC-YML-001] and [QC-WF-001] passes, and the OpenTofu
code carries out the decisions of [ADR-0046]. The conditions: every check of the pull request is green before the
merge, and the gate of [MIL-078] reads the first run of the workflow on GitHub. The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Merge only with every check of the pull request green | S01 (the AI as stand-in) | At the merge |
| Make `Ops edge code (MIL-078)` and `Ops script tests (test-edge-comparison.sh)` required checks of the ruleset on `main` | S01 | When S01 chooses |
| Read the first run of `NF Hotel edge comparison` on GitHub: green, with the warning "not compared" while the secrets are missing | S01 (the AI as stand-in) | After 2026-11-16 04:23 UTC |
| Task 5 of [MIL-078]: the tokens, the environment and its secrets, the first plan, the drill and the applied change ([RC-386]) | S01 | Before the gate of [MIL-078] |

---

[MIL-078]: ../../milestones/mil-078-the-tunnel-routes-and-access-as-code.md
[ADR-0046]: ../../adr/adr-0046-the-tunnel-routes-and-access-in-opentofu.md
[RC-386]: ./rc-386-adr-0046.md
[RC-338]: ./rc-338-mil-078.md
[ADR-0037]: ../../adr/adr-0037-github-actions-hardening-policy.md
[ADR-0021]: ../../adr/adr-0021-automatic-deploy-when-green.md
[RC-354]: ./rc-354-mil-083-code.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
