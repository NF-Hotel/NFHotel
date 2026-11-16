# RC-396 Gate review of MIL-075 The passport key out of the web container

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-396 |
| CrossReference | [MIL-075], [RC-335], [RC-360], [ADR-0004], [ADR-0024], [BC-001], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-16 | Accepted | mikemike | AI as stand-in for S01 | Initial version: the gate of MIL-075 after the deploy and the read of the web container; Go on criteria 1, 2, 3 and 5, criterion 4 waived by S01 in chat; mode autonomous, not independent | [553bec3] |

---

## Artifact Under Review

- Instance reviewed: [MIL-075] at its gate. It covers the five Go/No-Go criteria, with this evidence:
  - pull request #746, merged as 9bff1f1 on 2026-11-14 at 11:34 UTC, reviewed in [RC-360] (Go-with-conditions);
  - the CD run 43591370206 of that commit (2026-11-14, 11:43 to 11:50 UTC);
  - the CD run 43768542456 of 5dfc319 (2026-11-15, 22:18 to 22:24 UTC), the latest deploy, which started the web
    container that runs now;
  - the read of the web container and its secret folder inside the virtual machine (VM) on 2026-11-16.

  Before the gate: [RC-335] (the plan, Go) and [RC-360] (the code, Go-with-conditions).
- Checklist used: the Go/No-Go criteria of [MIL-075]. [QC-MIL-001] criterion 2 makes them the checklist of the gate.
- Scope: the gate (tasks 4 and 5). Criteria 1, 2 and 5 cite [RC-360] and the checks of #746. Criterion 3 rests on
  the evidence below. Criterion 4 is waived by S01 (F1).
- Language and domain: en / it (the Metadata rows of MIL-075). The gate adds only a row to its Version History. This
  record is a technical type.
- Language reviewer: none.
- Mode: autonomous, not independent. S01 gave the acceptance of the plan of 2026-11-14 to the AI in chat ("Du
  godkender for mig"), and on 2026-11-16 asked it to finish the open milestones in autonomous mode and to close each
  when it is done ("du skal i automode lave alle disse milestones", "færdiggør dem alle og luk dem når de er 100%
  done"). The waiver of criterion 4 is S01's own, in chat. The AI wrote the plan, the change and this record.

**Evidence:**

- **The deploy of 2026-11-14.** The CD run 43591370206 of 9bff1f1 deployed the change. Its log:
  - at 11:49:34 UTC, the warning that task 1 added: "`nfhotel-web.env` holds Encryption__PassportKey, a secret of
    another service; it is left out of the settings of web";
  - at 11:50:46 UTC, from `check-secret-env.sh`: "PASS nfhotel-web-nfhotel-web-1 has no secret in its environment";
  - at 11:50:50 UTC: the container check service passed.
- **The latest deploy.** The CD run 43768542456 of 5dfc319 on 2026-11-15 wrote the same warning at 22:22:42 UTC,
  started the web container that runs now at 22:22:59 UTC, and passed the same two checks at 22:23:59 and 22:24:03
  UTC.
- **The web container on 2026-11-16.** Read over Tailscale Secure Shell (SSH) at 08:03:42 UTC with `docker inspect`
  and a listing of file names, without printing a value:
  - `nfhotel-web-nfhotel-web-1` mounts three secret files in `/run/secrets`: `Api__Token`,
    `BenchmarkService__Token` and `HolidayApi__ApiKey`, and no `Encryption__PassportKey`;
  - inside the container, `/run/secrets` holds the same three files;
  - no variable of its environment is named `Encryption__PassportKey`, and no name has passport or encryption in it;
  - its secret folder on the VM, `deploy/nfhotel-web/secrets`, holds those three files and `settings.env`, and no
    key file;
  - control: the API container `nfhotel-api-nfhotel-api-1` mounts `/run/secrets/Encryption__PassportKey`, so the
    API keeps the key.
- **S01's word in chat (2026-11-16).** S01 asked the AI to close the milestone without the rest of task 4: "luk
  milestonen, jeg kigger ikke ydeligere ind i dette". The line `Encryption__PassportKey` stays in the web's settings
  file on the VM, and no guest with a passport number was opened in the back office (F1).

## Checklist Results

| # | Criterion (MIL-075) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The tests in `ops/tests/test-write-secret-files.sh` show that the web gets no file and no settings line `Encryption__PassportKey`, also when its settings file holds the line; that a settings line whose key is the secret of another service is left out with a warning, while every setting a service needs stays; that a secret file a service no longer lists is deleted; and that the compose file of the web names no passport key. Each test fails without the change, and Ops CI runs them | Pass | [RC-360] criterion 1: `test-write-secret-files.sh` 22 of 22; seven checks are new, and five of them failed on the script before the change. The job Ops script tests (test-write-secret-files.sh) passed on #746 |
| 2 | A test through `WebFactory` starts the web application with no passport key in its configuration and gets the front page and the sign-in page | Pass | [RC-360] criterion 2: `PassportKeyTests`, the front page and the sign-in page with an empty passport key. The job Build, tests and security passed on #746 |
| 3 | After the deploy, inside the VM: the web container has no file `/run/secrets/Encryption__PassportKey` and no such variable (`docker inspect`), its secret folder holds no key file, and the check of the environment after the deploy passed | Pass | The CD runs 43591370206 and 43768542456 passed the check of the environment for the web container. On 2026-11-16 the web container had no file `/run/secrets/Encryption__PassportKey`, no such variable, and its secret folder no key file, while the API container keeps the key (above) |
| 4 | After the deploy, S01 opens a guest with a passport number in the back office and reads it as before, and the log of the next deploy shows no warning for the web's settings | Waived by S01 | Not done: no guest with a passport number was opened in the back office, and the line stays in `nfhotel-web.env` on the VM, so every deploy writes the warning for the web's settings. S01 waived the criterion in chat on 2026-11-16 (F1) |
| 5 | The example file, Environment-Files, Proxmox-Deployment and ADR-0024 say what the deliverable says, `test-wiki-coverage.sh` passes, the code review against `QC-CS-001` and `QC-SH-001` is Go, `lint.sh` is clean, and every required check of the pull request is green | Pass | [RC-360] read the example file, Environment-Files, Proxmox-Deployment and the row of 2026-11-14 of [ADR-0024] against the deliverable; `test-wiki-coverage.sh` passed 116 of 116. On #746 the checks Ops wiki coverage and Ops linters (`lint.sh`) passed, with 52 more; DAST full scan and OWASP ZAP were skipped, by design. [RC-360] is Go-with-conditions; its conditions are met (F2) |

## Findings

- **F1, decision: criterion 4 waived by S01.** S01 is the owner and the approving reviewer of [MIL-075]. On
  2026-11-16 S01 asked in chat to close the milestone, and said that they would not look further into it: "luk
  milestonen, jeg kigger ikke ydeligere ind i dette". So two steps of task 4 are not done, and the AI does not do
  them:
  - The line `Encryption__PassportKey` stays in `nfhotel-web.env` on the VM. That file is the web's settings file on
    the host, not a file of the web container. The deploy leaves the line out of the web's settings and secret files
    (criterion 3), and names it in a warning at each deploy. Remove it only after a guest with a stored passport
    number has been opened in the back office and read as before: that read is what shows that the key of the API
    opens the stored numbers.
  - No guest with a stored passport number was opened in the back office after the deploy, so this gate does not
    show the read of a stored number through the API.
- **F2, note: the conditions of the code review are met.** [RC-360] gave Go-with-conditions: every check of the pull
  request green before the merge, and task 4 done after the deploy. #746 merged with every check green. Of task 4,
  the CD run and the read of the web container are done (criterion 3), and the rest is waived by S01 (F1). The review
  counts as Go for criterion 5.
- **F3, note: `scripts/dev-f5.sh` stays as it is.** [RC-360] F2 left it to S01 to decide whether the script of the
  development run in the IDE stops giving the web the key. It is not a criterion of [MIL-075], and S01 closed the
  milestone without deciding it, so it stays a note. The web does not read the key there either.

## Overall Verdict

Go. Criteria 1, 2, 3 and 5 of [MIL-075] pass, and criterion 4 is waived by S01, the owner and approving reviewer of
the milestone, in chat on 2026-11-16 (F1). The web container holds no passport key and the API keeps it, as objective
18 of [BC-001] says. The line in the web's settings file on the VM stays as it is. The row of 2026-11-16 of [MIL-075]
is Accepted and its row of 2026-11-12 Deprecated. The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept this record and the gate row of MIL-075, or return them | S01; done on 2026-11-16 by the AI as stand-in, at S01's word in chat | At the review of the gate pull request |
| Read NF Hotel CI of the gate pull request before the merge | AI as stand-in for S01 | Before the merge |
| Remove the line `Encryption__PassportKey` from `nfhotel-web.env` on the VM, after a guest with a stored passport number has been opened in the back office and read as before (F1) | S01 or S03 | Open; not part of the gate |
| Decide whether `scripts/dev-f5.sh` stops giving the web the key ([RC-360] F2, F3) | S01 | Open; not part of the gate |

---

[MIL-075]: ../../milestones/mil-075-the-passport-key-out-of-the-web-container.md
[RC-335]: ./rc-335-mil-075.md
[RC-360]: ./rc-360-mil-075-code.md
[ADR-0004]: ../../adr/adr-0004-database-api.md
[ADR-0024]: ../../adr/adr-0024-secrets-as-files.md
[BC-001]: ../../business-case.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[553bec3]: https://github.com/NF-Hotel/NFHotel/commit/553bec398d57a3bbf6d96b595b11a0bd85d2f926
