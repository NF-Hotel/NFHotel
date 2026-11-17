# RC-397 Review of the fix of update for an older release against QC-SH-001

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-397 |
| CrossReference | [ADR-0018], [MIL-032], [MIL-021], [ADR-0038], [QC-SH-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-17 | Accepted | mikemike | AI as stand-in for S01 | Initial version: review of the fix of `update` in `ops/scripts/deploy-release.sh` for a release older than the shallow checkout of the VM against QC-SH-001; F1 fixed; Go; mode autonomous, not independent | [72dba2f] |

---

## Artifact Under Review

- Instance reviewed: the branch `deploy-update-older-release` (from `main` at d1b2af3), before its pull request:
  - `ops/scripts/deploy-release.sh`: `update` fetches main back to a day before a release that is not in the shallow
    history of main, and checks again. The new helpers `fetch_main` and `on_main` and the constants `MAIN_REF` and
    `DEEPEN_MARGIN_SECONDS` hold the fetch, the check and the margin once. The `update:` paragraph of the header says
    when main is fetched back.
  - `ops/tests/test-deploy-release.sh`: three checks on a new shallow clone (F1).
  - the section "Roll back" of the wiki page CI-CD: the rollback to any earlier release, and the drill (F2).
- Checklist used: [QC-SH-001] (`framework/qc/code/qc-programming-shell.md`), all 15 criteria, for the changed lines
  of both scripts.
- Scope: the changed lines. The rest of `update` stands as the records of [MIL-032] left it.
- Language and domain: n/a for a technical type.
- Language reviewer: none.
- Mode: autonomous, not independent. S01 answered "Ja" in chat on 2026-11-17 to "Skal jeg undersøge og rette
  rollback-øvelsen i CD?", and earlier asked that the AI make the choices as S01 would. The AI wrote the fix, ran the
  checks below and wrote this record.

### Checks that were run

| Check | Result |
| --- | --- |
| `ops/tests/test-deploy-release.sh` in `debian:12-slim`, the image of NF Hotel Ops CI (Docker in WSL) | 109 passed, 0 failed |
| The same in `debian:13-slim`, the release of the VM (git 2.47) | 109 passed, 0 failed |
| The same test against `ops/scripts/deploy-release.sh` of `main` (d1b2af3), without the fix | 106 passed, 3 failed: the older release is refused with the message of CD run 43879990197 ("is not on main of origin"), and the two checks after it fail because it installed nothing |
| shellcheck v0.11.0 (`-x`) and shfmt v3.14.1 (`-i 2 -ci -d`), the versions and options of `ops/tests/lint.sh`, on both scripts | clean |
| `bash -n` of both scripts | no syntax error |
| Not run here | a CD run on the VM with an earlier release. The fixed command reaches the VM only with the next deploy, and the run needs S01's approval ([ADR-0038]). The migrate step of an earlier release, which such a run now reaches, was not observed; its script is idempotent, so it should apply nothing |

## Checklist Results: QC-SH-001

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Starts with `#!/usr/bin/env bash` and `set -euo pipefail` | Pass | Unchanged in both scripts |
| 2 | Every expansion is quoted; lists are arrays; tests use `[[ ]]` and `$(...)` | Pass | `"$sha"`, `"$1"`, `"$MAIN_REF"`, `"$@"`, `[[ "$(as_app git rev-parse --is-shallow-repository)" == true ]]`; the date is read with `"$(...)"` and the arithmetic sits inside a quoted option |
| 3 | Names follow the conventions | Pass | `fetch_main`, `on_main` and the local `committed` are `snake_case`; `MAIN_REF` and `DEEPEN_MARGIN_SECONDS` are `UPPER_SNAKE` and `readonly` beside the other constants |
| 4 | Passes `shellcheck` and `bash -n` with no unexplained `disable` comments | Pass | shellcheck v0.11.0 clean; no `disable` added |
| 5 | Errors go to standard error with an `error:` message and a non-zero exit code | Pass | A failed read of the date or a failed fetch back ends in `die` ("cannot read the date of …", "cannot fetch the history of main from origin"), exit 1; a commit that is not on main is still refused with `refuse`, exit 2 |
| 6 | Temporary files use `mktemp` with a `trap ... EXIT` cleanup | N-A | No temporary file added; `update_dir` and its trap are unchanged |
| 7 | No secret is written in the script, echoed, or put on a command line | Pass | The new commands carry a commit and a date only; `as_app` still passes the connection string on standard input ([ADR-0018] is unchanged in that) |
| 8 | A script that changes state outside its own directory defaults to a dry run or needs an explicit flag | Pass | The fetch back runs only inside the command `update`, which CD names explicitly, and changes only the history of the checkout; the header says so |
| 9 | A header comment states purpose, usage, options, environment variables and exit codes | Pass | The `update:` paragraph now says that main is fetched back for a release older than the shallow checkout |
| 10 | The script implements a task or design it cites; deviations are recorded | Pass | [ADR-0018] says that the VM "must fetch `main` with enough history for the ancestry check"; the code fetched less (F1). The fix makes it fetch enough, and keeps the refusal of a commit that is not on main |
| 11 | Behaviour is tested for success, failure and any disabled or bypass path | Pass | Success: an older main commit is installed from a shallow clone. Failure: a commit that is only on a pull-request ref is refused after the fetch back. The second update of CD needs no fetch back. Without the fix the first check fails (the regression) |
| 12 | Formatted with `shfmt` | Pass | shfmt v3.14.1 `-i 2 -ci` clean |
| 13 | Safe to re-run | Pass | The history fetched back stays, so a second update fetches nothing more (the check "the history fetched back stays for the second update of CD"), and a second update with equal files changes and logs nothing (the existing check) |
| 14 | Bash version and external tools it needs are stated; GNU-only options are named | Pass | git needs `--shallow-since` (git 2.11) and `rev-parse --is-shallow-repository` (git 2.15); Debian 12 has 2.39 and the VM's Debian 13 has 2.47. The date is git's own raw form `@<seconds> +0000`, so no GNU `date -d` is needed |
| 15 | A command sequence, constant, path or message used more than once is defined once | Pass | The fetch of main is `fetch_main` and the ancestry check is `on_main`, each used twice in `update`; the branch is `MAIN_REF` and the margin `DEEPEN_MARGIN_SECONDS` |

## Findings

- **F1, defect (fixed): a CD run with an earlier release stops at the update step.** CD run 43879990197, the rollback
  drill that S01 started on 2026-11-16 with the release of c8ad347, stopped at 14:46Z with "refused: c8ad347… is not
  on main of origin". Nothing was migrated or deployed. c8ad347 is on main, and it was the release before the one
  that ran (ba1ddf2).
  - Cause: `apply` fetches each release with depth 1, so every deploy leaves its commit as a shallow boundary of the
    checkout. `update` fetched main without a depth, which brings only the commits above what the checkout has, so
    `git merge-base --is-ancestor` could not reach a release below the boundary.
  - Since `update` came ([MIL-032], 2026-10-21), every CD run with an earlier release stopped there: both the rollback
    "To any earlier release" of the wiki page CI-CD and a drill with an earlier release. The manual
    `nfhotel-deploy rollback` on the VM has no such check and worked (2026-11-16).
  - Fix: when the commit is not in the fetched history of main and the checkout is shallow, `update` fetches main
    back to a day before the date of the commit (`git fetch --shallow-since`), and checks again. A commit that is
    still not on main is refused as before. A normal deploy is newer than the checkout and fetches nothing more.
- **F2, note: a drill leaves the commands of the drilled release installed.** `update` installs the commands of the
  drilled release before the deploy, and the rollback of the drill does not install those of the release it returns
  to. This is unchanged. The wiki page now says to run a normal deploy of the latest release afterwards (it used to
  say "of the release", which for an earlier release would be a real rollback), and that a drill may name the release
  before the latest, so it needs no new release.
- **F3, note: the fix acts once it is on the VM.** `update` runs as the command that the VM has, so a CD run with an
  earlier release passes only after a deploy has installed this fix. A rollback through CD to a release from before
  this fix installs the old `update`: the second update of that run passes, because the history fetched back stays,
  but a further rollback through CD from there to a still older release would stop again. Deploying the latest release
  first, or the manual rollback on the VM, avoids it.

## Overall Verdict

Go. Every applicable criterion of [QC-SH-001] passes; 6 does not apply. F1 is fixed, with a regression test that
fails without the fix; F2 and F3 are notes. The AI accepted this record as stand-in for S01, at S01's "Ja" in chat on
2026-11-17. Not verified here: a CD run with an earlier release on the VM, which needs the deploy of this fix and
S01's approval of each run. The reviewer is not independent: the AI wrote the fix and this record.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept this record, or return it | S01; done on 2026-11-17 by the AI as stand-in, at S01's word in chat | At the review of the pull request of `deploy-update-older-release` |
| Read NF Hotel Ops CI and the review threads of the pull request | AI as stand-in for S01 | Before the merge |
| Approve the CD run of the merge, so the VM gets the fixed `update` ([ADR-0038]) | S01 | After the merge |
| A drill with the release before the latest, if S01 wants the drill of [MIL-021] criterion 6 shown again | S01 | After that deploy |

---

[ADR-0018]: ../../adr/adr-0018-deploy-command-updates-itself.md
[MIL-032]: ../../milestones/mil-032-self-updating-deploy-command.md
[MIL-021]: ../../milestones/mil-021-secure-cicd-foundation.md
[ADR-0038]: ../../adr/adr-0038-approval-before-a-deploy.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[72dba2f]: https://github.com/NF-Hotel/NFHotel/commit/72dba2f2ce6f64d2d4d394b3dab851739eac8fb1
