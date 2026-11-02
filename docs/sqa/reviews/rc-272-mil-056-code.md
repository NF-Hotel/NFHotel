# RC-272 Review of the code of MIL-056 (light local development stack)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-272 |
| CrossReference | [MIL-056], [ADR-0035], [QC-SH-001], [QC-WF-001], [QC-YML-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-02 | Accepted | Claude (AI) | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- **Instances reviewed:** the change of tasks 2 to 6 of MIL-056:
  - `ops/scripts/lib/stack.sh` (new), the shared steps of the two stacks, and `ops/scripts/e2e-stack.sh`, which now uses it;
  - `scripts/dev-lite.sh` (new), `scripts/dev-up.sh` (`--lite` and the hints of `--check`), `ops/deploy/dev-lite/nginx-port.yml` (new);
  - `ops/scripts/make-dev-certificate.sh` (the names through a configuration file instead of `-addext`);
  - `ops/tests/test-dev-lite.sh` and `ops/tests/test-dev-lite-portable.sh` (new), and the two jobs in `.github/workflows/ops-ci.yaml`;
  - `.gitignore`, `README.md`, the wiki pages Local Development Deploy and Workflows.
- **Checklists used:** [QC-SH-001], [QC-WF-001] and [QC-YML-001].
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).

## Evidence

| Check | Result |
| --- | --- |
| Light mode on Windows 11 (Git Bash, Docker Desktop) | `dev-up.sh --lite --seed` builds and starts the four services, creates the accounts, loads the seed and answers 200 on `https://localhost:8443/` |
| `ops/tests/test-dev-lite.sh` on Windows 11 | 16 passed, 0 failed: the site, the sign-in, the mobile sign-in and rooms through `/mobile-api/`, 401 for a wrong password, 404 for another API path, one port on the loopback, no privileged container, `--down` keeps the data, `--destroy` removes it |
| `ops/tests/test-dev-lite-portable.sh` on Windows 11 | 22 passed, 0 failed (the file modes and the bash 3.2 hint are skipped there, and run on the macOS runner) |
| `make-dev-certificate.sh` with LibreSSL 3.8.4 | The pair is made with `DNS:localhost` and `DNS:nginx`; LibreSSL is what macOS has |
| `e2e-stack.sh` with the library | Starts, and `ops/tests/check-e2e-stack.sh` passes every check (TLS, redirects, hardening, no host port, networks, three accounts, no secret in an environment) |
| Arm64 | Every image the light mode pulls or builds from (`dotnet/sdk`, `dotnet/aspnet`, `postgres:16-alpine`, `nginx:1.29-alpine`, at their pinned digests) has a `linux/arm64` manifest (`docker buildx imagetools inspect`) |
| Linters | `ops/tests/lint.sh` clean: actionlint 1.7.7, shellcheck 0.11.0, shfmt 3.14.1, yamllint 1.35.1; `scripts/dev-lite.sh` and `scripts/dev-up.sh` also pass shellcheck |
| On a Mac | Not run yet: task 7, by a developer of S02 |

## Checklist Results

### QC-SH-001

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Shebang and `set -euo pipefail` | Pass | The tests use `set -uo pipefail` and say why (every check runs and is counted), as the other tests do |
| 2 | Quoting, arrays, `[[ ]]`, `$(...)` | Pass | shellcheck clean |
| 3 | Names | Pass | `kebab-case.sh`, `snake_case` functions with the `stack_` prefix in the library, `UPPER_SNAKE` constants and environment variables |
| 4 | shellcheck and `bash -n` | Pass | shellcheck 0.11.0, no new `disable` except SC2015 in the tests, with its reason |
| 5 | Errors to standard error, usage on bad arguments | Pass | `die` of `lib/common.sh`; an unknown argument prints the usage and exits 2 |
| 6 | Temporary files | Pass | The tests use `mktemp -d` with a trap; the certificate's configuration file is written inside the target folder under `umask 077` and removed by a trap, because a native Windows openssl needs a native path |
| 7 | No secret written, echoed or on a command line | Pass, on condition | Secrets are made with `openssl rand`, written to 0600 files and mounted as files (ADR-0024); the test sends the password to curl in a file. The script prints the passwords of the three local development accounts, as the Proxmox mode does; criterion 6 of MIL-056 allows exactly that and nothing else |
| 8 | State outside its folder only with a flag | Pass | Starting the local stack is the purpose of `dev-lite.sh`, said in its header; `--check` starts nothing; `--destroy` is a flag |
| 9 | Header comment | Pass | Purpose, usage, options, environment and exit codes in each new script |
| 10 | Implements a cited task | Pass | MIL-056 tasks 2 to 5, ADR-0035 |
| 11 | Tested for success, failure and bypass | Pass | The two tests: start, sign-in, refusal, 404, `--down`, `--destroy`, `--check`, an old bash |
| 12 | shfmt | Pass | shfmt 3.14.1 clean on the new and changed files |
| 13 | Safe to re-run | Pass | The settings are made once and kept; the accounts are made once (a marker line); a second run starts from them (tested) |
| 14 | Bash version and tools stated; GNU-only options named | Pass | `Needs:` lines; the portability test names the GNU-only options it looks for |
| 15 | Defined once | Pass | The settings, secret files, compose calls and accounts are in `lib/stack.sh`, used by both stacks; `e2e-stack.sh` lost its own copy |

### QC-WF-001 and QC-YML-001 (`ops-ci.yaml`)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | YAML checklist | Pass | yamllint 1.35.1 clean |
| 5 | Names of jobs and steps | Pass | `Ops light development mode (MIL-056)` and `... on macOS (MIL-056)`, each step named |
| 6 | actionlint | Pass | 1.7.7 |
| 7 | Permissions | Pass | The workflow's `contents: read`; the new jobs need nothing more |
| 8 | Actions pinned | Pass | `actions/checkout` at its full commit SHA, as the other jobs |
| 9, 10 | No secret, no untrusted text in `run:` | Pass | The jobs use no secret and no event field |
| 14 | `runs-on` and `timeout-minutes` | Pass | `ubuntu-24.04`, 30 minutes; `macos-15`, 10 minutes |
| 17 | Tools at pinned versions | Pass, on condition | The macOS job installs bash from Homebrew only when the runner has none of 4 or later; that is what a developer does, and the test reads only the major version |
| 20 | Length of `run:` | Pass | Five lines; the logic is in the test script |

## Overall Verdict

Go: every Mandatory criterion passes; two pass on condition (QC-SH-001 criterion 7 for the printed development passwords, allowed by MIL-056 criterion 6, and QC-WF-001 criterion 17 for the Homebrew bash). Criterion 3 of MIL-056, the run on a Mac by S02, is not met yet, so task 7 stays open after the merge.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Run `bash scripts/dev-up.sh --lite --seed` on a Mac, sign in to the back office and from the mobile app, and add the result here | S02 | 2026-11-16 |

---

[MIL-056]: ../../milestones/mil-056-dev-stack-without-kvm.md
[ADR-0035]: ../../adr/adr-0035-light-local-development-stack.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
