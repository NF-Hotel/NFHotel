# RC-095 Review of the MIL-021 workflows and YAML files

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-095 |
| CrossReference | [MIL-021], [ADR-0007], [QC-WF-001], [QC-YML-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-18 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [064c27c] |

---

## Artifact Under Review

- Instance reviewed: `.github/workflows/release.yaml`, `.github/workflows/cd.yaml`, and the image variables in `deploy/{nfhotel-web,nfhotel-api,market-analysis,benchmark}/docker-compose.yml`, at commit 6cd89c1
- Checklist used: [QC-WF-001] for the workflows; [QC-YML-001] for all six files
- Scope: the changes of MIL-021
- Language and domain: n/a (technical type)
- Language reviewer: none
- Mode: autonomous, not independent (reviewer is `AI as stand-in for S01`); an independent review is requested from Tirsvad on PR #117
- Tools: `rhysd/actionlint` 1.7.7 with shellcheck, yamllint 1.35.1 (`-s`, committed `.yamllint`), `docker compose config`

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The file meets the YAML checklist ([QC-YML-001]) | Pass | See the YAML table below |
| 2 | The file is in `.gitea/workflows/` or `.github/workflows/` with the extension `.yml` or `.yaml`, and the same workflow does not exist in both directories | Pass | `.github/workflows/release.yaml`, `cd.yaml`; no `.gitea/` |
| 3 | The file declares `name`, `on` and `jobs`; `on` lists explicit events and, where it matters, `branches` or `paths` | Pass | `workflow_run` with `types` and `branches`; `workflow_dispatch` with typed inputs |
| 4 | A header comment states the purpose, the triggers, the inputs, the secrets it needs, what it changes outside the repository, and the decision or milestone it implements | Pass | Both headers, rewritten in 6cd89c1 |
| 5 | Every job and step has a descriptive `name`, unique within its job | Pass | All steps named |
| 6 | The file passes `actionlint` (or the project's workflow validation test) and the tool version is stated | Pass | `rhysd/actionlint:1.7.7` with its bundled shellcheck: 0 findings; stated in the headers |
| 7 | Every job declares `permissions` explicitly with the least access that works (`contents: read` or none by default); write access only on the job that needs it | Pass | Top level `contents: read`; Release job adds `packages: write`; CD job adds `actions: read` and `id-token: write` |
| 8 | Every action used is pinned to a full commit SHA or an exact release tag, never to a branch; a moving major tag (`@v1`) is accepted only for an action published by the same organisation under a stated release policy | Pass | All four actions pinned to commit SHAs with the release in a comment (6cd89c1) |
| 9 | No secret is written in the file; a secret is read as `secrets.NAME`, passed to a step through `env:`, never placed on a command line or echoed | Pass | `GITHUB_TOKEN` through `env:` and `--password-stdin`; no other secret exists |
| 10 | Untrusted text (`inputs.*`, event fields such as a title, branch name or label, step outputs derived from them) is never interpolated into a `run:` script; it is passed through `env:` and used as a quoted variable | Pass | `inputs.release_run_id`, `head_sha` and the manifest outputs go through `env:`; every `run:` is one fixed command |
| 11 | A trigger that gives a run secrets or a write token on code from a fork or an untrusted branch (`pull_request_target`, `workflow_run`) does not check out or execute that code | Pass | Release runs only for a push event on `main` of this repository and checks out that tested commit; CD checks out `main` only and never the release code |
| 12 | A workflow that changes state outside the repository (release, tag, publish, sync, deploy) is started by hand or by a protected branch, and a manual run defaults to a dry run with an explicit input to apply | Pass | Release: protected `main` only; CD: after a release on `main`, or by hand with `apply: false` by default, and always behind the `production` approval |
| 13 | The token the workflow uses has only the scope the job needs, and where the host's built-in token is enough it is used instead of a personal token | Pass | Only `GITHUB_TOKEN`; Tailscale through OIDC |
| 14 | Every job sets `runs-on` to an explicit label and `timeout-minutes` | Pass | `ubuntu-24.04`, 30 minutes |
| 15 | `concurrency` is set where two runs would conflict: a check cancels the older run (`cancel-in-progress: true`); a publishing job queues (`cancel-in-progress: false`) | Pass | `release` and `production`, both queueing |
| 16 | Triggers are filtered (`branches`, `paths`, `types`) so the workflow runs only for the changes it concerns | Pass | `types: [completed]`, `branches: [main]`, and a job `if:` on the conclusion and event |
| 17 | Tools and dependencies are installed at pinned versions; a version is a workflow input with a default or a value in one place | Pass | Tailscale `version: 1.102.4`; Docker, jq and gh come from the pinned runner image `ubuntu-24.04` |
| 18 | A dependency cache is used where the install is slow enough to matter | Fail | No layer cache for the image builds yet (optional); action item for MIL-022 |
| 19 | A failing step names the tool, the file and the reason in its log (annotation or `::error::` line) | Pass | `cd-steps.sh` writes `::error::` lines; the build script prints `error:` with the image |
| 20 | A `run:` block is within the limit on embedded program text of criterion 7 of [QC-YML-001]; a longer script is a file (`tools/`, or a shared repository fetched at a pinned ref) that the step calls with one command, and the workflow passes its settings through `env:` | Pass | Every `run:` is one command; the logic is in `scripts/cd-steps.sh` and `scripts/build-release-images.sh` |
| 21 | Logic used by more than one workflow or repository is a reusable workflow (`workflow_call`) or a composite action; each input has a `description`, a `type` and a `default` where one is sensible | Pass | No logic is shared between the two workflows; the inputs have descriptions, types and a default for `apply` |
| 22 | Every workflow feature used is verified on the instance that runs it, and the host and version are recorded; a feature the target hosts differ on is noted in the header | Fail | The `workflow_run` chain, the `production` approval, the GHCR push and the Tailscale OIDC login have not run on github.com yet: they need the environment, the Tailscale setup and the merge of PR #22 (MIL-021 task 8) |
| 23 | A reusable workflow states, in its header, how a caller pins it and the script it fetches, so the workflow and its script are the same release | N-A | No reusable workflow |
| 24 | A static test of the repository's workflows exists and runs on every pull request: valid YAML, required keys, explicit permissions and runner, no inline script above the limit | Fail | Optional; no such test yet. Action item: run actionlint and yamllint in CI (MIL-022) |

## YAML Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The file parses with a safe YAML loader and has no duplicate keys in any mapping | Pass | yamllint (strict) parses all six files; `docker compose config` loads the four compose files |
| 2 | Indentation is 2 spaces everywhere, never tabs; lines end with LF; the file ends with one newline and has no trailing spaces | Pass | yamllint `-s`: 0 findings on the two workflows and the four changed compose files |
| 3 | Passes `yamllint` (or the project's YAML linter) with its configuration committed next to the code and no unexplained `disable` comments | Pass | yamllint 1.35.1 with the committed `.yamllint` |
| 4 | A scalar that YAML would coerce to another type is quoted: `yes`, `no`, `on`, `off`, `null`, `~`, numbers with a leading zero, versions such as `1.10`, and values that contain `: ` or ` #` | Pass | `version: 1.102.4` parses as a string (two dots); no coercible scalars |
| 5 | Only safe, plain types are used: no language-specific tags (`!!python/object`, `!ruby/object`) and no `!include`-style extension unless the consuming tool documents it | Pass | None |
| 6 | No secret is written in the file: no password, token, key or private connection string; a secret is referenced by name and supplied by the consuming tool | Pass | Only `secrets.GITHUB_TOKEN` and `vars.*` by name |
| 7 | Embedded program text (shell, Python, SQL, `jq`, and so on) in a value is at most five lines; a longer one is moved to a file of its own, in its own language, and the YAML calls it | Pass | The longest `run:` is three folded lines of one command |
| 8 | A block of keys or values repeated in the file or across files is defined once (an anchor, a template, an include or a reusable unit the consumer supports) | Pass | The image variable of market-analysis stays in its existing anchor; the CD step settings are passed once through `env:` |
| 9 | Anchors, aliases and merge keys (`&`, `*`, `<<`) are used only where the consuming tool is known to support them | Pass | Only the existing anchor in a compose file, supported by Docker Compose and checked with `docker compose config` |
| 10 | The file validates against the schema of the consuming tool where one exists, and the schema or tool version is stated | Pass | actionlint 1.7.7 checks the GitHub Actions schema; Docker Compose v2 `config` checks the compose files |
| 11 | A header comment states what the file is for, which tool reads it, and where its decision or design is recorded | Pass | Both workflows; the compose files keep their headers |
| 12 | Key names follow the convention of the consuming tool; one case style is used within the file (kebab-case or snake_case, never mixed) | Pass | GitHub Actions keys; the input names `release_run_id` and `apply` are snake_case |
| 13 | Multi-line strings use a block scalar (`|` or `>`) and state the chomping indicator | Pass | `>-` on every multi-line value |
| 14 | The file name is lowercase kebab-case with one extension style (`.yml` or `.yaml`) used throughout the repository | Pass | `.yaml` like `ci.yaml` |
| 15 | A change is exercised by a check that runs without the real target: a parse and schema test, or a dry run of the consuming tool | Pass | actionlint, yamllint and `docker compose config` with an image variable |

## Overall Verdict

Go-with-conditions — every Mandatory criterion passes except QC-WF-001 criterion 22: the workflow features have not yet run on github.com, because they need the `production` environment, the Tailscale setup and the merge of PR #22. The two optional criteria 18 and 24 fail and are planned for MIL-022.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Run the Release and CD workflows on github.com and record the run links (closes criterion 22) | S01 | MIL-021 task 8 |
| Add a build cache for the image builds (criterion 18) | S01 | MIL-022 |
| Run actionlint and yamllint on every pull request (criterion 24) | S01 | MIL-022 |
| Independent review of the workflows on PR #117 | Tirsvad (S01) | Before the merge of PR #117 |

---

[MIL-021]: ../../milestones/mil-021-secure-cicd-foundation.md
[ADR-0007]: ../../adr/adr-0007-cicd-and-supply-chain.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[064c27c]: https://github.com/NF-Hotel/NFHotel/commit/064c27c1d90793a6f3782e61cf4a91a4b96eba84
