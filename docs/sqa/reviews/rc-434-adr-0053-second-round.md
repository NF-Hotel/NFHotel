# RC-434 Review of ADR-0053 Mail from the application through a relay, second round

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-434 |
| CrossReference | [ADR-0053], [QC-ADR-001], [MIL-088], [ADR-0052], [ADR-0034], [MIL-027], [ADR-0035], [RC-432], [RC-433] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-19 | Accepted | mikemike | AI as stand-in for S01 | Initial version: the second review of ADR-0053 by a second AI, after the rework that RC-432 asked for; Go-with-conditions, the conditions closed in the text accepted here; mode autonomous, not independent | pending |

---

## Artifact Under Review

- Instance reviewed: [ADR-0053], reworked after [RC-432], in the worktree of the branch
  `mil-088-booking-verified-by-email` (from `main` at 6f7bb80), before any code of [MIL-088].
- Checklist used: [QC-ADR-001], with the decisions of [MIL-088], the code and the files under `ops/` at 6f7bb80.
- Scope: delta re-review. The second AI checked each finding of [RC-432] against the reworked text and every criterion
  over the whole record; the earlier record is [RC-432] (Go-with-conditions).
- Language and domain: n/a (a technical type, IT Professional English)
- Language reviewer: none
- Mode: autonomous, not independent. S01 chose autonomous mode in chat on 2026-11-19 ("Når din plan er lavet skal
  Opus 5.5 lave det hele i automode") and handed the decisions to the AI ("Du skal tage beslutningerne for mig"). An
  AI wrote the ADR, [RC-432] and this record. The second AI of [RC-432] reviewed the reworked text, together with
  [ADR-0052] ([RC-433]), read the files under `ops/` and the outside sources below, and changed nothing. The AI that
  wrote the ADR closed the conditions below and checked them as stand-in for S01; the second AI did not review the
  text a third time.

**The findings of [RC-432]**, as the second AI found them in the reworked text:

- Closed: 3 (the fuse under the lock, the record before the send), 5, 6, 10, 11, 13, 14 and 15. Condition 6 is closed
  by the risks in the consequences and the choice of the account left to S01 before task 9 of [MIL-088]: no mail of a
  guest flows before then.
- Closed with a gap left: 12 (see conditions 1 and 2).

**Outside sources the second AI read:** the commit of HAProxy "DOC: resolve-prefer default value and default-server
update"; the release notes of Mailpit v1.15.0; the Gmail Help pages on the limits of sending, on dots and on aliases;
the page of MailKit 5.0.0 on NuGet; chapter V12 of ASVS 5.0.

## Checklist Results

The results of the text accepted here, after the conditions below were closed.

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The ID (`ADR-NNNN`, 4-digit) matches the filename number and is unique and sequential | Pass | Unchanged |
| 2 | Context states the problem, the forces (cost, risk, constraints) and the options evaluated | Pass | The texts now add the option `resolve-prefer` of HAProxy and the TLS options of Mailpit, and give the two limits of Gmail apart |
| 3 | At least two options were considered, or the absence of alternatives is justified | Pass | Unchanged: four options for the sending, three for the way out |
| 4 | Decision is stated in one or two unhedged sentences and matches one of the evaluated options | Pass | Options D and G, in eight numbered decisions |
| 5 | Consequences list both Positive and Negative outcomes | Pass | The negative list holds the secret that opens the mailbox, the copies in Sent, the replies, the revoked app password, the fuse that a sender can use up, the block after bounces, and the two new moving parts |
| 6 | Quality-attribute impacts (performance, security, portability, etc.) are named where the decision affects them | Pass | Security, reliability, flexibility and maintainability, with the requirements of ASVS each rests on |
| 7 | Affected Artifacts are listed as links, and each is also in CrossReference | Pass | Nine artifacts, each a link and in the CrossReference; the wiki pages and the alert rules are named in prose with the task that changes them |
| 8 | Status changes are recorded as new Version History rows with a Change summary and Commit link; retained rows are never rewritten | Pass | One row, set to `Accepted` with this review; it cites [RC-432] and this record; its commit `pending` until the commit |
| 9 | A superseded or deprecated ADR names its successor, and the successor links back | N-A | Nothing is superseded |
| 10 | Written in business or technical language of the domain, with no unexplained abbreviations | Pass | After condition 5: OWASP, HTTP, SHA-256, UTF-8 and IPv6 are spelled out on first use, besides TLS, SSL, STARTTLS, TCP, SMTP, SPF, DKIM, DMARC, OAuth, VM and AI |
| 11 | Status is one of `Proposed`, `Accepted`, `Rejected`, `Deprecated` or `Superseded by ADR-NNNN`, and every Version History row uses only these | Pass | `Accepted` |

## Findings

The conditions of the second AI, numbered as it numbered them, and how each is closed in the text accepted here:

- **1. The forwarder would choose an address it cannot reach.** HAProxy takes the IPv6 address of a name unless told
  otherwise, the relay of Gmail has IPv6 addresses, and Docker on the virtual machine has no IPv6
  (`ops/deploy/host/docker/daemon.json` does not turn it on). A copy of the database relay of [ADR-0034], which looks
  up a container with an IPv4 address only, would send every mail to an address it cannot reach, and the booking flow
  would stop. Closed: decision 4 says `resolve-prefer ipv4`, with the reason.
- **2. "The forwarder holds no secret"** was true only if it gets the two values alone, because `nfhotel-api.env`
  holds every secret of the API until `write-secret-files.sh` splits them out. Closed: decision 4 says that
  `deploy-services.sh` reads the host and the port from that file and passes only them to the forwarder, as the
  variables of its compose file, and that the forwarder never gets the file.
- **3. The stacks did not test the TLS of production.** The catcher asked for STARTTLS, while production uses
  `SslOnConnect` on port 465. Closed: decision 7 has the catcher speak TLS from the first byte
  (`--smtp-require-tls`, since Mailpit v1.15.0) and the stacks set `SslOnConnect`; the tests of the adapter cover
  `StartTls` with a server of the test that offers no STARTTLS.
- **4. Two wording faults about Gmail.** The help page names two limits, more than 500 mails a day (sending again
  within 1 to 24 hours) and many bounces (sending again after 24 hours), and the draft merged them; "a few dozen
  bounces may be enough" had no source; one line was not wrapped. Closed: the context and the force name the two
  limits apart, the consequence says that Google gives no number, and the lines are wrapped.
- **5. Abbreviations (optional).** OWASP, HTTP, SHA-256 and UTF-8 were not explained. Closed, with IPv6.
- **11. No record of the first review.** Closed: [RC-432] and this record exist, and the Version History row cites
  both.

## Overall Verdict

Go-with-conditions, with the conditions closed. The second AI found the reworked decisions able to hold, criteria 1
to 8 and 11 passed and criterion 10 open; conditions 1 to 4 and 11, and the optional 5, are closed in the text that
this record accepts, as the findings above show. The AI accepted [ADR-0053] as stand-in for S01 in autonomous mode,
at S01's word in chat on 2026-11-19, and set its row to `Accepted`. The choice of the account stays S01's before
task 9 ([RC-432]). The reviewer is not independent: an AI wrote the ADR and this record and closed the conditions,
and a second AI gave the findings.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Close conditions 1 to 5 and 11 in the text of [ADR-0053] | mikemike; done on 2026-11-19 | Before the acceptance |
| Build the forwarder with `resolve-prefer ipv4` and only the host and the port of the relay, the catcher with TLS from the first byte in the throwaway stack ([MIL-027]) and the light development mode ([ADR-0035]), and the tests of the adapter for `StartTls` | mikemike | The code of [MIL-088], in its code review |

---

[ADR-0053]: ../../adr/adr-0053-mail-from-the-application-through-a-relay.md
[QC-ADR-001]: ../../../framework/qc/artifacts/qc-adr.md
[MIL-088]: ../../milestones/mil-088-a-booking-verified-by-email.md
[ADR-0052]: ../../adr/adr-0052-a-booking-takes-a-room-once-its-email-is-verified.md
[ADR-0034]: ../../adr/adr-0034-team-database-access.md
[MIL-027]: ../../milestones/mil-027-dast-and-end-to-end-tests.md
[ADR-0035]: ../../adr/adr-0035-light-local-development-stack.md
[RC-432]: ./rc-432-adr-0053.md
[RC-433]: ./rc-433-adr-0052-second-round.md
