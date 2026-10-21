# RC-159 Review of the self-updating deploy command (MIL-032)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-159 |
| CrossReference | [MIL-032], [ADR-0018], [QC-SH-001], [QC-WF-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-21 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [f485ba7] |

---

## Artifact Under Review

- **Instances reviewed (MIL-032 tasks 2 to 5):**
  - `nfhotel-deploy update` and `install_from` in `ops/scripts/deploy-release.sh`;
  - the release log that counts only release lines;
  - the `deploy` step of `ops/scripts/cd-steps.sh`, with update before migrate and deploy;
  - the headers of `install-deploy-user.sh` and `install-backup.sh`;
  - the wiki page CI-CD and CONTRIBUTING;
  - the first run on `nf-hotel`.

  The code was merged in NFHotel#227. The two defects found here are fixed in the same pull request as this record.
- **Checklists used:** [QC-SH-001], [QC-WF-001].
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`). S01 asked for this review in chat on 2026-10-21.
- **Tools:**
  - `ops/tests/lint.sh` (check-layout, actionlint, shellcheck, shfmt, yamllint);
  - `test-deploy-release.sh` and `test-cd-steps.sh` in Debian 12;
  - the log of CD run 39561453009.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| SH 1–4, 12 | Header, quoting, names, shellcheck, shfmt | Pass | 0 findings |
| SH 5 | Errors and usage | Pass | A short commit exits 2. A commit that is not on `origin/main` exits 2 with the reason. A failed fetch, `git show`, `cp` or `install` exits 1 with `error:` and the file |
| SH 6 | Temporary files | Pass after fix | `install_from` used `mktemp` and removed the file only on its own paths. A failure in between left the file in `/tmp`. Now `update` makes one folder with `mktemp -d`, and a `trap ... EXIT` removes it. Test: "update removes its temporary folder" |
| SH 7 | No secret | Pass | Only paths and SHA-256 hashes are printed and logged |
| SH 8 | Explicit change | Pass | `update` is an explicit subcommand, called only by the approved CD job. The header now lists it with deploy and rollback as a command that changes the VM |
| SH 9 | Header | Pass | `update`, its refusal, `.previous`, the hash log, and the three test variables are documented |
| SH 10 | Implements its design | Pass | ADR-0018, decisions 1 to 4 |
| SH 11 | Success, failure and bypass tested | Pass after fix | `install_from` runs in a condition (`&& units=1`, `\|\| true`), where `set -e` does not apply. A failed `install` or `cp` was ignored, and update reported success. This is proven by the new test "a failed install fails update with its reason", which fails on the merged code. Every step now ends in `\|\| die`. 51 checks in `test-deploy-release.sh` and 48 in `test-cd-steps.sh` |
| SH 13 | Safe to re-run | Pass | A second update with equal files changes and logs nothing (tested) |
| SH 15 | Defined once | Pass | One `install_from` for the four files |
| WF 22 | Verified on the instance | Pass | CD run 39561453009 (8a077b0) on `nf-hotel` logged `update: installing ...`, installed `nfhotel-backup` from the release commit, then logged `the commands match 8a077b0`. The migrations, the deploy and the smoke test passed |

## MIL-032 Go/No-Go Criteria

| # | Criterion | Result | Evidence |
| --- | --- | --- | --- |
| 1 | update installs from main, refuses a pull-request commit, changes nothing when equal, keeps `.previous` | Go | `test-deploy-release.sh`, including a shallow checkout as on the VM |
| 2 | CD runs update, migrate, deploy, and stops when update fails | Go | `test-cd-steps.sh` |
| 3 | The sudo rule is unchanged | Go | `install-deploy-user.sh` writes the same rule (only its header changed). `deploy` runs only `/usr/local/sbin/nfhotel-deploy` |
| 4 | A CD after the last manual install logs an update, and the commands equal the release commit | Go | CD run 39561453009 |
| 5 | No document tells S03 to install again after a change | Go | A search of `ops/scripts`, `docs/wiki` and CONTRIBUTING finds none |
| 6 | Reviews `Go` | Go | RC-157 (qc-adr), RC-158 (qc-milestones-gateways), this record |

## Findings

- **Fixed: a failed install was silent** (SH 11). It is fixed in this record's pull request. Until that pull request is deployed, the command on the VM keeps the defect. The next approved CD installs the fix by itself, which is the mechanism of ADR-0018.
- **Fixed: a temporary file could be left** (SH 6).

## Overall Verdict

Go.

## Action Items

None.

---

[MIL-032]: ../../milestones/mil-032-self-updating-deploy-command.md
[ADR-0018]: ../../adr/adr-0018-deploy-command-updates-itself.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[f485ba7]: https://github.com/NF-Hotel/NFHotel/commit/f485ba76f3a61cce643390c206872a0ee2c29860
