# ADR-0041 The connection string of the API crosses sudo on standard input

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0041 |
| CrossReference | [MIL-070], [ADR-0011], [ADR-0024], [MIL-023], [CIS-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-08 | Accepted | mikemike | AI as stand-in for S01 | Initial version; task 1 of MIL-070, which S01 accepted in chat on 2026-11-08 ("1 ja", RC-318 Go) with this choice and the ways it rejects; written by the AI; accepted after RC-320 Go | [6411a2a] |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

**Who decides.** S01 accepted [MIL-070] in chat on 2026-11-08 ("1 ja"). The plan names this decision, standard input with `sudo` kept, and the ways it rejects. The artificial intelligence (AI) assistant wrote this record as task 1 and reviewed it as a stand-in for S01 (RC-320); S01 can challenge it in the summary of the run.

**The problem.** `ops/scripts/deploy-release.sh` runs as root on the virtual machine (VM) and runs `git` and `deploy-services.sh` as the application user `nfhotel`, the owner of the checkout, through `as_app`. Until this record, `as_app` called `sudo -u nfhotel -H --preserve-env=NFHOTEL_API_CONNECTION_STRING,NFHOTEL_API_MIGRATE_ON_START bash -c ...`. Since the roles of [ADR-0011], `api_database_settings` sets the connection string of `nfhotel_app`, with its password, before `deploy-services.sh` writes the secret file of the API ([ADR-0024]). `sudo` writes every variable that it keeps into the `ENV=` field of its log line. Since [MIL-023] that line goes to `/var/log/sudo.log` and to the journal, and Alloy sends the journal to Loki. On 2026-11-08 the journal held 186 such lines, the first from 2026-10-21 (MIL-070, findings 1 to 3).

**Forces.**

- The change of user must stay in the log of `sudo`: rows 5.2.1 to 5.2.3 of [CIS-001] and the audit trail of [MIL-023].
- `deploy-services.sh` and `write-secret-files.sh` read the connection string from their environment, in the operations commit of every release that a rollback can check out, so the command must still find it there.
- The secret must go into no argument: the `COMMAND=` field of the same log line, and the process list. It must also go into no other file on disk; [ADR-0024] keeps secrets in files only in the secrets folder and where a container mounts them.
- Debian's `/etc/sudoers` sets `use_pty`, and no input log is set, so `sudo` does not record what it reads on standard input.

Options considered:

1. **`runuser` or `setpriv` instead of `sudo`.** They change the user and log nothing, so the secret leaves the log, and so does every step of the deploy that the audit trail of MIL-023 records.
2. **A temporary file for `nfhotel`** with the connection string, its name passed to the command. The secret goes onto the disk once more, outside the secrets folder, and a deploy that fails can leave it behind.
3. **No log of `sudo` for root** (`Defaults:root !logfile, !syslog`). It hides the secret, and with it every command that root runs as another user, which is the trail that [CIS-001] asks for.
4. **Standard input.** `as_app` writes the connection string as one line into a pipe to the shell that `sudo` starts. That shell reads it into the environment of the command and runs the command with nothing on its standard input. `sudo` keeps only the flag `NFHOTEL_API_MIGRATE_ON_START`, which holds no secret, and logs the change of user and the command as before.

## Decision

We follow option 4: the connection string of the API crosses `sudo` on standard input, and `sudo` keeps only variables that hold no secret.

1. **as_app.** `connection_string_line | sudo -u nfhotel -H --preserve-env=NFHOTEL_API_MIGRATE_ON_START bash -c "$APP_SHELL" _ <checkout> <command...>`. `connection_string_line` writes the value with the builtin `printf`, so it is in the arguments of no process. `APP_SHELL` reads one line, exports it as `NFHOTEL_API_CONNECTION_STRING`, changes to the checkout, and runs the command with `exec` and standard input from `/dev/null`. Without a connection string it reads nothing, and the variable stays unset, as before.
2. **The rule for every `sudo` call in `ops/`.** A variable crosses `sudo` in its environment only when its name is on a short list of names that hold no secret, today `NFHOTEL_API_MIGRATE_ON_START`; `--preserve-env` without a list, `-E` and `NAME=value` before the command are not used. A secret crosses on standard input. `ops/tests/test-sudo-log.sh` checks the rule, and proves the transfer in Debian 13 with the `sudo` settings of the VM.
3. **The exposed password** is replaced after the deploy of this change (MIL-070 task 5), and the old entries stay until their retention ends.

## Consequences

**Positive:**

- A deploy writes no secret into the log of `sudo`, the journal or Loki, and the log still records the change of user and every command.
- The commands find the connection string where they always did, so the operations commits of older releases still deploy and roll back.
- The rule and its test stop the next `--preserve-env` of a secret in continuous integration (CI), before it reaches the VM.

**Negative:**

- The command of every `as_app` call runs with nothing on its standard input; a later command that needs input must get it another way.
- The log line of every `as_app` call is longer, because it holds the text of the shell.
- The test of the rule reads the words of each `sudo` call, not their meaning: a secret in an argument, such as `env NAME=value` as the command, is outside it, and the code review is the check for that.
- The password stays exposed until task 5 of MIL-070 replaces it, and the old entries hold it until their retention ends.

**Quality:** security improves (confidentiality of the database login, accountability kept by the log of `sudo`); maintainability: one more rule for `sudo` calls, kept by a test.

## Affected Artifacts

- [MIL-070]: tasks 1 to 3 implement this record, and task 5 replaces the password.
- [MIL-023]: the log of `sudo` stays as it decided, without the secret.
- [ADR-0024]: the secret file of the API is still written by `deploy-services.sh` from the variable.

---

[MIL-070]: ../milestones/mil-070-the-database-password-out-of-the-logs.md
[ADR-0011]: ./adr-0011-postgresql-access-for-the-api.md
[ADR-0024]: ./adr-0024-secrets-as-files.md
[MIL-023]: ../milestones/mil-023-operational-quality.md
[CIS-001]: ../sqa/cis-checklist.md
[6411a2a]: https://github.com/NF-Hotel/NFHotel/commit/6411a2a500971d387634758b345a7fda9d127722
