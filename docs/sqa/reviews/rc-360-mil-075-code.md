# RC-360 Code review of MIL-075: the passport key out of the web container

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-360 |
| CrossReference | [MIL-075], [RC-335], [ADR-0004], [ADR-0024], [MIL-035], [QC-SH-001], [QC-CS-001], [QC-YML-001], [QC-ADR-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-14 | Accepted | mikemike | AI as stand-in for S01 | Initial version; mode autonomous, not independent; Go-with-conditions: every check of the pull request green | [8ca858a] |

---

## Artifact Under Review

- Instance reviewed: the change of [MIL-075] tasks 1 to 3 on the branch `mil-075-passport-key-out-of-web`:
  - task 1: `ops/scripts/write-secret-files.sh` and its test `ops/tests/test-write-secret-files.sh`;
  - task 2: `ops/deploy/nfhotel-web/docker-compose.yml`, `ops/deploy/nfhotel-web/nfhotel-web.env.example`, the comment
    in `src/NFHotel.Web/Program.cs` and the new test `tests/NFHotel.Web.Tests/PassportKeyTests.cs`;
  - task 3: the wiki pages Environment-Files and Proxmox-Deployment, the row of 2026-11-14 of [ADR-0024], and
    `scripts/dev-up.sh` (F1).
- Checklist used: [QC-SH-001] (`qc-programming-shell`), [QC-CS-001] (`qc-programming-csharp`), [QC-YML-001]
  (`qc-yaml`) and, for the changed parts of [ADR-0024], [QC-ADR-001] (`qc-adr`). No workflow changed, so
  `qc-workflow` does not apply. The wiki pages and the example file have no checklist of their own; they are read
  against the deliverable of [MIL-075] below.
- Scope: full review of the changed files; partial review of [ADR-0024] (its row of 2026-11-14)
- Language and domain: n/a (technical type)
- Language reviewer: none
- Mode: autonomous, not independent (the reviewer is AI as stand-in for S01, who gave the acceptance of the plan of
  2026-11-14 to the AI in chat, "Du godkender for mig", with the merge and the approval of its CD run). The code was
  written on 2026-11-12, when S01 asked in chat for the build of [MIL-075] ("Ja, start MIL-075"), and moved unchanged
  onto the branch of [MIL-073] on 2026-11-14.

## Checklist Results

### QC-SH-001: `write-secret-files.sh`, `test-write-secret-files.sh` and `scripts/dev-up.sh`

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | `#!/usr/bin/env bash` and `set -euo pipefail`, or a comment explains the exception | Pass | The script keeps both; the test keeps the convention of `ops/tests`, `set -uo pipefail`, and now says why there is no `-e` |
| 2 | Expansions quoted, lists as arrays, `[[ ]]` and `$(...)` | Pass | The names of the other services' secrets are the keys of the associative array `foreign`; `${SECRETS[$other]}` is split on purpose, as the script splits the lists elsewhere, and the names hold only letters and `_`; shellcheck clean |
| 3 | Naming | Pass | `key_pattern`, `foreign`, `other` and `list` in snake_case; the files keep their names |
| 4 | shellcheck and `bash -n`, every `disable` explained | Pass | shellcheck v0.11.0 `-x` over the three files: clean; `bash -n` passes; no new `disable` |
| 5 | Errors on standard error with `error:`, a usage line for bad arguments | Pass | The new messages are a `warning:` that names the key of another service and the file, and a `deleted` line for a secret file the service no longer gets, both on standard error; they do not stop the deploy, because the secret is left out and nothing a service needs is lost; the usage errors are unchanged |
| 6 | `mktemp` and `trap ... EXIT`, no fixed `/tmp` names | Pass | The test works in its `mktemp -d` folder; the script writes temporary files only in its output folder, under names that start with `.` and are moved into place |
| 7 | No secret in the script, its output or a command line | Pass | `grep -Eo` gives the key and `=` only, so the warning never reads the value; the test asserts that neither the log nor the folder holds the value of the passport key of the web's settings, or of a tunnel token in the API's settings |
| 8 | A change of state outside its folder needs an explicit flag | Pass | The script writes and deletes only in the folder given with `--out`; it deletes a file only when its name is a secret of another service, and leaves any other file |
| 9 | Header: purpose, usage, options, environment, exit codes | Pass | The header says that a service gets only its own secrets, what is left out with a warning, and what `--out` now deletes |
| 10 | Implements a cited task; deviations recorded | Pass | [MIL-075] tasks 1 and 2; F1 records the change of `scripts/dev-up.sh`, which the tasks do not name |
| 11 | Success, failure and bypass paths tested | Pass | The test passes 22 of 22 in `debian:12-slim`; seven checks are new and five of them failed on the script before the change: the web gets no passport key when its settings hold the line, the warning names the key and the value is nowhere, the web keeps its settings and own secrets, the API leaves a tunnel token out with a warning and its own secrets give none, settings with only own secrets give no warning, a stale key file is deleted while another file stays, and the compose file and the example name no passport key |
| 12 | Formatted with `shfmt` | Pass | shfmt v3.14.1 `-i 2 -ci` over the two files of `ops/`: no difference. `scripts/dev-up.sh` is outside `lint.sh` and has four differences of its own, none in the lines this change touches (F4) |
| 13 | Safe to re-run | Pass | A second run writes the same files and deletes nothing more; the warning repeats while the line stays in the settings file |
| 14 | Bash version and tools stated; GNU-only options named | Pass | Unchanged `Needs:`; `grep -o` and `grep -E` are POSIX options of GNU and BusyBox grep |
| 15 | Each sequence, constant, path or message defined once | Pass | The list `SECRETS` stays the one list of the secrets: the pattern of the keys comes from it through `key_pattern`, which the warning and the filter share, and the test reads the web's list with `--list` |

### QC-CS-001: `src/NFHotel.Web/Program.cs` and `tests/NFHotel.Web.Tests/PassportKeyTests.cs`

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Casing | Pass | `PassportKeyTests`, `NoPassportKeyFactory`, `ExtraSettings` |
| 2 | `Async` suffix, one type per file | Pass | The test class with its nested factory, as the other tests of the folder have them |
| 3 | Formatter, no suppressions, explicit local types | Pass | `dotnet format NFHotel.sln --verify-no-changes --include` the two files: no change; explicit types (`NoPassportKeyFactory`, `HttpClient`, `HttpResponseMessage`) |
| 4 | Nullable references, `!` justified | Pass | No `!` |
| 5 | Disposal | Pass | The factory, the client and the response are disposed with `using` |
| 6 | Async throughout | Pass | `await` on the request; no `.Result` or `.Wait()` |
| 7 | Specific exceptions, arguments validated | N-A | No new code that throws or takes an argument from outside |
| 8 | XML documentation | Pass | The class, the test and the factory say why the web needs no key, and why the key is set empty rather than left out |
| 9 | Properties, immutability | N-A | No new state |
| 10 | `decimal` for money, `DateTimeOffset` for points in time | N-A | |
| 11 | `CancellationToken` | Pass | The request takes `TestContext.Current.CancellationToken` |
| 12 | Tests | Pass | `Get_PageWithoutPassportKey_IsServed` for the front page and the sign-in page, through `WebFactory`, with no network and no clock; `dotnet test` of the class: passed, 0 failed |
| 13 | Trace to the DCD | N-A | No class of the design changes; `Program.cs` changes a comment only |
| 14 | One place for each piece of knowledge | Pass | The comment now says once that only the API decrypts a passport number |
| 15 | References point inward | Pass | No reference changes: the web keeps only `AddApiClient`, and Infrastructure is not referenced by it |
| 16 | SOLID | N-A | No new class of `src/` |
| 17 | Layers and folders | Pass | The test beside the other tests of the web |
| 18 | File names | Pass | |
| 19 | Observer pattern | N-A | |
| 20 | SIMD and CUDA | N-A | |
| 21 | Regions in types of `src/` | N-A | No type of `src/` changes |

### QC-YML-001: `ops/deploy/nfhotel-web/docker-compose.yml`

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Parses safely, no duplicate keys | Pass | yamllint (`key-duplicates`) |
| 2 | Two spaces, LF, one final newline | Pass | yamllint; no CR byte |
| 3 | Passes yamllint with its committed configuration | Pass | yamllint 1.35.1 `-s -c ops/.yamllint` in `python:3.13-slim`, as `lint.sh` runs it: clean |
| 4 | Scalars YAML would coerce are quoted | Pass | No new scalar |
| 5 | Plain types only | Pass | |
| 6 | No secret | Pass | Names of secret files only |
| 7 | Embedded program text of five lines at most | N-A | |
| 8 | A repeated block defined once | Pass | The test holds the compose file to the list of the script: every secret of `--list web` is mounted, and no passport key |
| 9 | Anchors only where supported | N-A | |
| 10 | Validates against the schema of its tool | Pass | The stand-in stacks of Ops CI start the web from this file |
| 11 | A header comment states purpose, tool and decision | Pass | Unchanged |
| 12 | Key names follow the tool, one case style | Pass | |
| 13 | Block scalars for multi-line strings | N-A | |
| 14 | Lowercase kebab-case file names | Pass | |
| 15 | A check runs without the real target | Pass | `test-write-secret-files.sh` reads the file without a container |

### QC-ADR-001: the row of 2026-11-14 of [ADR-0024]

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The ID matches the filename number and is unique and sequential | Pass | Unchanged |
| 2 | Context states the problem, the forces and the options evaluated | Pass | Unchanged; the new row makes decision 2 precise and does not choose another option |
| 3 | At least two options were considered | Pass | Unchanged |
| 4 | Decision stated in one or two unhedged sentences | Pass | Decision 2 adds that each service gets only its own secrets |
| 5 | Consequences list Positive and Negative outcomes | Pass | A positive consequence is added: the web holds no passport key, and a secret written in the wrong file never reaches a container |
| 6 | Quality-attribute impacts named | Pass | Security (confidentiality) |
| 7 | Affected Artifacts as links, each also in CrossReference | Pass | [MIL-075] in both |
| 8 | Status changes as new Version History rows; retained rows never rewritten | Pass | The row of 2026-11-14 is new; the row of 2026-10-25 stays as it was and becomes Deprecated; the oldest row and its link left the table |
| 9 | A superseded ADR names its successor | N-A | |
| 10 | Business or technical language, no unexplained abbreviations | Pass | |
| 11 | Status values from the allowed list | Pass | `Accepted` and `Deprecated` |

### The documents against the deliverable of [MIL-075]

- **The example file** `nfhotel-web.env.example` has no line of the passport key, not even a commented one, and says
  that an older settings file may hold it and that the deploy leaves it out with a warning until it is deleted.
- **Environment-Files**: the table of the files counts the web's secrets as the two copies it holds; the row of the
  key in the API's table says that only the API holds it; the web's section says what the deploy does with an old
  line; the row of the key is gone from the web's table and from the table of the secrets that must match.
- **Proxmox-Deployment**: each service gets only its own secrets, and the table of the secrets names the API only.
- `test-wiki-coverage.sh` passes 116 of 116.

## The criteria of MIL-075 that this review can see

| # | Status | Evidence |
| --- | --- | --- |
| 1 | Pass | `test-write-secret-files.sh`, 22 of 22, with the seven new checks above |
| 2 | Pass | `PassportKeyTests`, the front page and the sign-in page with an empty passport key: passed |
| 3 | Pending | Task 4, after the deploy: the web container and its secret folder, read over Tailscale SSH |
| 4 | Pending | Task 4: the line removed from the web's settings file on the VM, a guest with a passport number opened in the back office, and the log of the next deploy |
| 5 | Pending | The documents and the linters above pass, and this review is Go-with-conditions; the checks of the pull request |

## Findings of the review

- **F1, deviation: `scripts/dev-up.sh` changed.** It wrote the passport key into a new `nfhotel-web.env` of the
  development VM. Once the example file had no such line, its `sed` changed nothing, so the two blocks were dead and
  are removed; the API's key is written as before.
- **F2, note: `scripts/dev-f5.sh` still gives the web its key.** The script of the development run in the IDE writes
  `Encryption:PassportKey` into the user secrets of both projects. The web does not read it, so nothing breaks;
  [MIL-075] does not name the file, so it is left as it is and reported to S01.
- **F3, note: no setting that a service reads is left out.** Read in `src/` on 2026-11-14: the API reads the database
  login, the passport key, its token, the signing key, the first Manager's password and the analysis key, all on its
  own list; the web reads its token, the holiday key and the benchmark token, all on its own list. The filter of
  secrets of other services therefore removes only lines that no service reads in a settings file it does not own.
- **F4, note: the formatter and `scripts/`.** `lint.sh` formats and checks the scripts of `ops/` and `.github/`.
  `scripts/dev-up.sh` has four shfmt differences that are older than this change; this change only removes lines.
- **F5, note: a rollback stays safe.** `deploy-release.sh` checks out the operations commit of the release it goes
  back to and runs that commit's own `write-secret-files.sh`, which writes the web's key file again for the old compose
  file; the old web does not read it either.
- **F6, note: the checks ran in WSL.** The script test and the linters ran in WSL from a copy of `ops/` and
  `scripts/`, with the images and versions of `lint.sh`; the job Ops linters runs `lint.sh` itself.

## Overall Verdict

Go-with-conditions. Every applicable criterion of [QC-SH-001], [QC-CS-001], [QC-YML-001] and [QC-ADR-001] passes,
and criteria 1 and 2 of [MIL-075] pass. The row of 2026-11-14 of [ADR-0024] is Accepted and its row of 2026-10-25
Deprecated. The conditions: every check of the pull request is green before the merge, and task 4 is done after the
deploy. The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Merge only with every check of the pull request green, after [MIL-073] (#693) | S01 (the AI as stand-in) | At the merge |
| Approve the CD run of the merge; read the secrets, the environment and the secret folder of the web container over Tailscale SSH without printing a secret | S01 (the AI as stand-in) | 2026-11-19 |
| Remove the line `Encryption__PassportKey` from `nfhotel-web.env` on the VM | S01 or S03; the AI tries it first over Tailscale SSH | 2026-11-19 |
| Open a guest with a passport number in the back office and read it as before | S01 | 2026-11-19 |
| Decide whether `scripts/dev-f5.sh` should stop giving the web the key (F2) | S01 | Open |

---

[MIL-075]: ../../milestones/mil-075-the-passport-key-out-of-the-web-container.md
[RC-335]: ./rc-335-mil-075.md
[ADR-0004]: ../../adr/adr-0004-database-api.md
[ADR-0024]: ../../adr/adr-0024-secrets-as-files.md
[MIL-035]: ../../milestones/mil-035-secrets-as-files.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[QC-ADR-001]: ../../../framework/qc/artifacts/qc-adr.md
[8ca858a]: https://github.com/NF-Hotel/NFHotel/commit/8ca858a67009163c8e76e707abf63986fa51e220
