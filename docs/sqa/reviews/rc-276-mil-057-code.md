# RC-276 Review of the code of MIL-057 (start from Visual Studio with F5)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-276 |
| CrossReference | [MIL-057], [ADR-0036], [QC-SH-001], [QC-CS-001], [QC-WF-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-02 | Accepted | Claude (AI) | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- **Instances reviewed:** the change of tasks 2 to 5 of MIL-057:
  - `scripts/dev-f5.sh` (new), `scripts/dev-f5.ps1` (new, a launcher through `dev-up.ps1 --f5`), and `scripts/dev-up.sh` (`--f5` beside `--lite`);
  - `NFHotel.slnLaunch` (new), `src/NFHotel.Api/NFHotel.Api.csproj` (`UserSecretsId`), and `src/NFHotel.Api/Properties/launchSettings.json` (no browser at start);
  - `ops/tests/test-dev-f5.sh` (new) and the job `Ops start from Visual Studio (MIL-057)` in `.github/workflows/ops-ci.yaml`;
  - `.gitignore`, `README.md`, the wiki pages Local Development Deploy and Workflows.
- **Checklists used:** [QC-SH-001], [QC-CS-001] (the project file only; no C# source changed) and [QC-WF-001].
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).

## Evidence

| Check | Result |
| --- | --- |
| `ops/tests/test-dev-f5.sh` on Windows 11 (Git Bash, Docker Desktop, .NET SDK 10.0.403) | 13 passed, 0 failed: nine API and four web secrets written; PostgreSQL on `127.0.0.1` only; no password in the container environment; no secret in a file of the repository; a second run writes nothing; the API's health check and the web sign-in page with their launch profiles; a sign-in through the API and through the web form to the dashboard; `--destroy` removes the container, the volume, the state and the secrets it wrote |
| First run of the test | Failed: `dotnet user-secrets` could not parse the JSON. A native Windows `openssl` ends its output with `\r\n`, and `$(...)` strips only the `\n`, so a `\r` was in every generated value. `secret()` and the passport key now strip `\r` too; reproduced and fixed |
| `NFHotel.slnLaunch` | The first draft had unescaped `\` in its paths, which is not valid JSON; the paths now use `\\` as Visual Studio writes them, and the file parses |
| `dev-f5.ps1 --check` from PowerShell | Finds Git Bash through `dev-up.ps1` and reports every need as ok |
| Linters | `ops/tests/lint.sh` clean (actionlint 1.7.7, shellcheck 0.11.0, shfmt 3.14.1, yamllint 1.35.1); shellcheck and shfmt also clean on `scripts/dev-f5.sh` and `ops/tests/test-dev-f5.sh`; `ops/tests/test-wiki-coverage.sh` 91 passed |
| F5 in Visual Studio 2026 on S01's machine | Not run by the AI: GUI tests never run on the host desktop (`framework/process/gui-testing.md`). Task 6 stays open for S01 to press F5 on the profile "API and web" |

## Checklist Results

### QC-SH-001

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Shebang and `set -euo pipefail` | Pass | `dev-f5.sh` has both; the test uses `set -uo pipefail` and says why, as the other tests do |
| 2 | Quoting, arrays, `[[ ]]`, `$(...)` | Pass | shellcheck clean |
| 3 | Names | Pass | `kebab-case.sh`, `snake_case` functions, `UPPER_SNAKE` constants and environment variables (`DEV_F5_*`) |
| 4 | shellcheck and `bash -n` | Pass | shellcheck 0.11.0; the only `disable` is SC2015 in the test, with its reason |
| 5 | Errors to standard error, usage on bad arguments | Pass | `die` of `lib/common.sh`; an unknown argument prints the usage and exits 2 |
| 6 | Temporary files | Pass | The test uses `mktemp -d` with a trap; the script's state is in `ops/.dev-f5/`, mode 0700 |
| 7 | No secret written, echoed or on a command line | Pass, on condition | The secrets go to `dotnet user-secrets` as JSON on standard input, and the database password to PostgreSQL as a file; the test sends passwords to curl in files. The script prints the development Manager's password, as the other development modes do (MIL-057 deliverable) |
| 8 | State outside its folder only with a flag | Pass | Writing the user secrets is the purpose of the script, said in its header; `--check` changes nothing; `--destroy` removes only the keys listed in `written-keys` |
| 9 | Header comment | Pass | Purpose, steps, usage, options, environment and exit codes |
| 10 | Implements a cited task | Pass | MIL-057 tasks 2 and 4, ADR-0036 |
| 11 | Tested for success, failure and bypass | Pass | The test: first run, second run, start, both sign-ins, `--destroy` |
| 12 | shfmt | Pass | shfmt 3.14.1 clean on the new files |
| 13 | Safe to re-run | Pass | A set key is kept; the container and the volume are reused (tested) |
| 14 | Bash version and tools stated | Pass | `Needs:` lines; bash 4 is checked before any bash 4 feature runs |
| 15 | Defined once | Pass | The PostgreSQL image is read from `ops/deploy/postgres`; the PowerShell launcher calls `dev-up.ps1` instead of copying its search for Git Bash; the API token and the passport key have one value for both projects |

### QC-CS-001 (`NFHotel.Api.csproj`)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 3 | Builds with no unexplained analyzer suppression | Pass | The API and the web application build in the test; no suppression added |
| 14 | Defined once | Pass | The ports are in the launch profiles; the API address the web application uses is written once, by `dev-f5.sh` |
| 18 | File and folder names | Pass | `NFHotel.slnLaunch` is the name Visual Studio reads; `dev-f5.ps1` and `dev-f5.sh` are `kebab-case` |
| 1, 2, 4 to 13, 15 to 17, 19 to 21 | Code criteria | N-A | No C# source changed |

### QC-WF-001 (`ops-ci.yaml`)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 5 | Names of jobs and steps | Pass | `Ops start from Visual Studio (MIL-057)`, each step named |
| 6 | actionlint | Pass | 1.7.7 |
| 7 | Permissions | Pass | The workflow's `contents: read`; the job needs nothing more |
| 8 | Actions pinned | Pass | `actions/checkout` and `actions/setup-dotnet` at their full commit SHAs, as the other jobs |
| 9, 10 | No secret, no untrusted text in `run:` | Pass | The job uses no secret and no event field |
| 14 | `runs-on` and `timeout-minutes` | Pass | `ubuntu-24.04`, 20 minutes |
| 17 | Tools at pinned versions | Pass | .NET from `global.json` |
| 20 | Length of `run:` | Pass | One line each; the logic is in the test script |

## Overall Verdict

Go: every Mandatory criterion that applies passes; QC-SH-001 criterion 7 passes on condition (the printed development password, which MIL-057 asks for). Criterion 1 of MIL-057, F5 in Visual Studio on S01's machine, is not met yet, so task 6 stays open after the merge.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Press F5 on the profile "API and web" in Visual Studio 2026 and sign in as the printed Manager | S01 | 2026-11-20 |

---

[MIL-057]: ../../milestones/mil-057-start-from-visual-studio.md
[ADR-0036]: ../../adr/adr-0036-start-from-visual-studio.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
