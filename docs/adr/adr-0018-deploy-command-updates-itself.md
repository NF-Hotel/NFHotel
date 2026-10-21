# ADR-0018 The deploy command updates itself from the reviewed main commit

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0018 |
| CrossReference | [BC-001], [SA-001], [ADR-0007], [ADR-0008], [ADR-0016], [MIL-021], [MIL-032], [RC-162], [ADR-0009] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-21 | Deprecated | mikemike | AI as stand-in for S01 | Accepted after RC-157 Go (accepted by S01 in chat) | [819a1d9] |
| 2026-10-21 | Accepted | mikemike | AI as stand-in for S01 | The certificate renewal is installed too, and CD runs update again after a new deploy command (RC-162) | [e2e8a90] |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

**Who wrote what.** The goal is S01's, given in chat on 2026-10-21 ("I shall NOT watch anything or do anything by hand; it must be automated and secured so it runs by itself when everything is green", translated from Danish). The design and the reasons below were written by the AI assistant. They are not a quote of S01.

ADR-0007 and MIL-021 task 5 made the deploy command a root-owned copy. `ops/scripts/install-deploy-user.sh` installs `ops/scripts/deploy-release.sh` as `/usr/local/sbin/nfhotel-deploy`. The user `deploy`, which CD uses over Tailscale SSH, may run only that command through sudo. `ops/scripts/install-backup.sh` installs the backup command `/usr/local/sbin/nfhotel-backup` and its systemd timer in the same way. A change to these files in git takes effect only when S03 runs the install scripts again by hand. The reason was that CD must not be able to change what runs as root.

On 2026-10-21 that rule cost a production deploy:

- After the VM switch of ADR-0016, the installed backup command still used the old path, so CD stopped before the migrations.
- The fix (NFHotel#220) was merged, but the VM kept the old copies until S03 ran both install scripts again. The step was run twice too early and was easy to miss.

Since ADR-0007, the path from git to the VM has gained the checks that the manual step was meant to give:

- the ruleset on `main` (required checks, a review by someone other than the author, last push approved), with no direct push and no bypass;
- the `ops` CI, which runs shellcheck, shfmt and the script tests on every change to these files;
- the environment `production`, whose approver must be Tirsvad or codemikemike, cannot be the person who started the run, and cannot be bypassed by an admin.

A change to the deploy command has therefore passed two people and its tests before CD can carry it. A third, manual step adds little and is skipped in practice.

Options considered:

1. **Keep the manual install** (ADR-0007). Safe in principle, but it depends on S03 remembering, and it failed on 2026-10-21.
2. **A warning only.** CD marks the run when the installed commands differ from the release commit. S03 still installs by hand.
3. **The command updates itself during CD,** only from a commit that is on `main`.
4. **A root timer on the VM that pulls `main` and installs.** Rejected: it runs outside CD, with no approval, no log in the run and no link to a release.

## Decision

We follow option 3: during each CD run, before the migrations, the deploy command installs the operations commands of the release commit. It does so only when that commit is on `origin/main`.

1. **A new subcommand, `update <sha>`.** CD calls `sudo -n /usr/local/sbin/nfhotel-deploy update <sha>` before `migrate` and `deploy`. The sudo rule does not change, because `deploy` still runs only `nfhotel-deploy`.
2. **Only reviewed commits.** The command fetches `main` from `origin` and refuses any commit that is not reachable from `origin/main`, the branch that the ruleset protects. GitHub serves commits of open pull requests by SHA, so this check is what keeps an unreviewed commit out.
3. **What it installs.** It reads the files from that commit with `git show`, so a modified working tree cannot be installed:
   - `ops/scripts/deploy-release.sh` as `/usr/local/sbin/nfhotel-deploy`;
   - `ops/scripts/backup-database.sh` as `/usr/local/sbin/nfhotel-backup`;
   - `ops/deploy/systemd/nfhotel-backup.{service,timer}`, followed by `systemctl daemon-reload`;
   - `ops/scripts/renew-certificate.sh` as `/usr/local/sbin/nfhotel-cert-renew`, and `ops/deploy/systemd/nfhotel-cert-renew.{service,timer}`, whose timer it enables (added by [RC-162], [ADR-0009]).

   When `update` installs a new deploy command, CD runs `update` once more. The new command then installs the files that the old one did not know, in the same deploy.

   Each file is installed with `install` (root, the same mode as today), and only when its content differs.
4. **A way back and a record.** The copy it replaces is kept as `<file>.previous`. Each update appends a line to the release log with the commit and the old and new SHA-256 of every file it changed. A run that changes nothing writes nothing.
5. **What stays manual, once.** The first install of the user, the sudo rule, `/etc/nfhotel/backup.env`, the age key and Tailscale do not change. S03 runs `install-deploy-user.sh` once more after this milestone merges, so that the VM has a deploy command that knows `update`. After that, no manual install is needed.
6. **Human gates that stay.** The pull-request review and the `production` approval stay. They are the controls this decision relies on.

## Consequences

**Positive:**

- A merged change to the deploy or backup command reaches the VM with the next approved CD. S03 does nothing by hand.
- The VM can no longer run an old command against a new layout, the fault of 2026-10-21.
- Every change to what runs as root is in git, has passed review and the `ops` CI, and is logged with its hashes on the VM.

**Negative:**

- What runs as root is now set by `main` plus an approved CD, with no third person. A wrong change that passes review and CI reaches the VM at the next deploy. The way back is `<file>.previous`, or a revert on `main`.
- The VM must fetch `main` with enough history for the ancestry check. That is a small extra fetch on each deploy.
- One last manual install (decision 5) is needed to bring in the first command that knows `update`.

## Affected Artifacts

- [ADR-0007] — amended: the deploy command is no longer installed only by hand; CD installs it from a reviewed `main` commit.
- [ADR-0008] — the backup command and its timer are installed in the same way.
- [MIL-021] — task 5's rule "S03 installs it again" is replaced by the `update` step.
- [MIL-032] — the milestone that carries out this decision.

---

[BC-001]: ../business-case.md
[SA-001]: ../stakeholder-analysis.md
[ADR-0007]: ./adr-0007-cicd-and-supply-chain.md
[ADR-0008]: ./adr-0008-database-migration-and-backup.md
[ADR-0016]: ./adr-0016-one-repository-for-application-and-operations.md
[MIL-021]: ../milestones/mil-021-secure-cicd-foundation.md
[MIL-032]: ../milestones/mil-032-self-updating-deploy-command.md
[819a1d9]: https://github.com/NF-Hotel/NFHotel/commit/819a1d9d1bd8492f9164aac8416d56192d514e24
[RC-162]: ../sqa/reviews/rc-162-certificate-renewal.md
[ADR-0009]: ./adr-0009-tls-certificates.md
[e2e8a90]: https://github.com/NF-Hotel/NFHotel/commit/e2e8a90e0082aa329e4d2da499e8b1e853c26ca8
