# RC-437 Gate review of MIL-088 A booking verified by email

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-437 |
| CrossReference | [MIL-088], [ADR-0052], [ADR-0053], [ADR-0041], [UC-011], [RC-413], [RC-431], [RC-432], [RC-433], [RC-434], [RC-435], [RC-436], [RC-441], [QC-MIL-001], [QC-WIKI-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-27 | Accepted | mikemike | AI as stand-in for S01 | Initial version: the gate of MIL-088 after the three attempts of CD run 44730514648 at 3cfe3ab5, the relay that S01 set (task 9) and S01's check of criterion 10 on the live site; the conditions of RC-435 and RC-436 closed; the wiki page Mail-Relay corrected; Go on criteria 1 to 10, with one deviation of criterion 10 accepted as stand-in; mode autonomous, not independent | pending |

---

## Artifact Under Review

- Instance reviewed: [MIL-088] at its gate. The gate covers the ten Go/No-Go criteria, with this evidence:
  - the documents and the code of tasks 1 to 8 on `main` since 3cfe3ab5 (pull request 857, merged on 2026-11-20 at
    07:05:02 UTC), reviewed in [RC-431] to [RC-434] (the two decision records), [RC-435] (the documents) and [RC-436]
    (the code);
  - the checks of pull request 857 on its last head (ccefbd9f): 60 passed and 2 skipped, which skip by design (OWASP
    ZAP and DAST full scan). The runs of that head all passed: NF Hotel CI 44690972493, NF Hotel E2E and DAST
    44690972423, NF Hotel Ops CI 44690972415, NF Hotel Release check 44690972328, CodeQL 44690970925 and code
    quality 44690970924;
  - the runs of the merge commit 3cfe3ab5 on `main`, which all passed: NF Hotel CI 44692344259, NF Hotel Ops CI
    44692344336, NF Hotel Wiki 44692344201, CodeQL 44692343647, code quality 44692343901, NF Hotel Release
    44694949711 (on its second attempt; the first failed on a passing answer 403 of GitHub to the image of the market
    analysis) and NF Hotel E2E and DAST 44730118249 with the full scan, started by hand;
  - the test results that the runs of the last head keep: the artifact `test-results-44690972493-1` of NF Hotel CI
    (nine result files) and `e2e-results` of NF Hotel E2E and DAST (the browser tests, 16 with the verification off
    and 3 with it on);
  - the run 44730514648 of NF Hotel CD at 3cfe3ab5 and its three attempts, with the relay that S01 set (task 9),
    below;
  - S01's check of criterion 10 on the live site and the counters of the API, below and in
    `docs/sqa/evidence/mil-088/live-check.txt`;
  - the published wiki, which [RC-441] checked at dcbc88ce with every page of MIL-088 on it, and the page Mail-Relay
    as this gate changes it (F2).

  Before the gate: [RC-413] (the plan, Go).
- Checklist used: the Go/No-Go criteria of [MIL-088]; [QC-MIL-001] criterion 2 makes them the checklist of the gate.
  For the page Mail-Relay that the gate changes, the criteria of [QC-WIKI-001] that a change of the content of one
  page can touch: 11, 13, 14 and 16.
- Scope: the gate (task 10), the conditions that the reviews of tasks 1 to 8 left to it, and the relay of task 9,
  which S01 set.
- Language and domain: en / it (the Metadata rows of MIL-088). The gate adds a row to the Version History of MIL-088.
  This record is a technical type.
- Language reviewer: none.
- Mode: autonomous, not independent. S01 chose autonomous mode in chat on 2026-11-19 ("Når din plan er lavet skal
  Opus 5.5 lave det hele i automode") and handed the decisions to the AI ("Du skal tage beslutningerne for mig"). The
  AI that built the milestone made the checks of the gate and wrote this record; no other reviewer read it.

**The runs of CD.** The AI that built this milestone approves no run of CD. S01 approves each one, or the main
session, the session that plans this work, approves it from S01's account at S01's word in chat. Run 44730514648 at
3cfe3ab5 was created on 2026-11-20 by the event `workflow_run` after NF Hotel Release 44694949711.

- Attempt 1, the deploy with the verification off: approved by the main session at 17:04:42 UTC on 2026-11-20, at
  S01's word of 2026-11-19 ("du må gerne godkende deploy."). The job "Deploy to production" ran from 17:04:46 to
  17:08:03 UTC and passed. The main session saw the migration `AddBookingRequests` applied, every container healthy,
  and the front page, the booking page and `security.txt` answer 200. The env file of the API held no mail setting
  yet, so the verification was off and the forwarder `mail-relay` was not started. The process of the API that
  started then counted one booking on the public site at about 17:34 UTC, made at once and without a mail, as before
  this milestone (the first check of task 10).
- Task 9, round 1, on 2026-11-22: S01, as root on the VM, wrote the mail settings and the app password into the env
  file of the API with `runuser -u nfhotel` and the verification off, checked the file without its values, and sent a
  test mail with `curl` from the VM, which arrived at 10:05 UTC. S01 then wrote: "Du må nu fortsætte med trin 5 efter
  den aftalte plan: kør den angivne CD-kørsel igen, gennemfør den nødvendige godkendelse". The main session re-ran the
  run and approved attempt 2 at 10:11:04 UTC, with a comment that names pull request 857 and S01's word. The job ran
  from 10:11:08 to 10:13:40 UTC and passed: 12 containers passed the container check, and `check-secret-env.sh`
  passed for each. The main session then saw, without reading the value, the secret file `Smtp__Password` of 16 bytes
  mounted read-only in the API and readable by its user, no secret in the environment of a container, `mail-relay`
  healthy with only its host and its port, and `/book` answering 200.
- Task 9, round 2: S01 switched the verification on and wrote "done" with the output of the command. The main session
  approved attempt 3 at 10:19:35 UTC, with the comment "Approved on S01's word in chat, 22 Nov 2026: re-run of the
  MIL-088 deploy (PR #857), round 2: BookingVerification__Enabled=true after the test mail and round 1.". The job ran
  from 10:19:39 to 10:22:30 UTC and passed. The API started at 10:22:10 UTC with the verification on and is healthy,
  so it took every mail setting at its start; the secret as in round 1.
- That process of the API ran until the deploy of MIL-089 on 2026-11-27: the run 45374815906 that S01 started by hand
  on 2026-11-24 at 3cfe3ab5 did not restart it, as the label of its instance in Prometheus shows. After the deploy of
  MIL-089 the verification is still on ([RC-441]).

**The check of criterion 10.** S01 made the bookings of the check on the live site and pressed their buttons; the AI
made no booking and sent no mail. The counters are those of the one process of the API from 2026-11-22 10:22:10 UTC
to the deploy of MIL-089, read through Grafana on 2026-11-27; the minutes are those of the first sample that shows a
change.

- 2026-11-22: a request and its link mail at 10:29 UTC, the booking and its receipt at 10:31 UTC. S01 wrote "det
  virker" in chat at 10:43:54 UTC.
- 2026-11-24, at midday: a request at 12:55 UTC and its booking at 12:59 UTC.
- 2026-11-24, the evening: requests at 16:46 and 17:07 UTC; a request and a refused link in the same minute, at 18:16
  UTC; and the third booking at 18:17 UTC, with the link of the last request, the only one still open then. S01
  answered "Ja, min test" on 2026-11-26 to the question whether the requests of that evening, 17 to 20 Danish time,
  were S01's test of the live check.
- Five requests made five link mails and three bookings with three receipts; no mail failed, the fuse never blew, and
  no alert of the mail or of the booking flow was pending or firing from 2026-11-22 to 2026-11-27.
- The main session read the three newest bookings made on the public site on 2026-11-26 at 10:09 UTC, read only,
  their status and dates and nothing about the guest: all three are `Pending`. The stay of each ended on or before
  2026-11-27.
- Step 5 of the check, the cancellation of the test bookings and the deletion of the copies of the test mails, is not
  done. S01 wrote on 2026-11-27 at 04:12 UTC, asked whether the three test bookings were cancelled or would stay: "De
  må gerne blive stående, jeg sletter dem måske senere. Ingen betydning" (F4).

**The page Mail-Relay.** Its Part B said that the relay is deployed first with the verification off, "so a wrong
setting shows while the public site still books as before". But the API uses the app password only when it sends a
mail, and with the verification off it sends none: a wrong app password showed only after the second deploy, when
guests wait for their links. The main session saw this before S01 wrote the password, and the plan it made for S01
sent a test mail from the VM before any deploy and wrote the file with `runuser` in place of `sudo`; S01 ran it so on
2026-11-22. The page did not say so (F2).

## Checklist Results

| # | Criterion (MIL-088) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | ADR-0052 and ADR-0053 are Accepted after their reviews: the eleven decisions with any changed number and its reason, and the alternatives with the reason each lost | Pass | [ADR-0052] (ten decisions) and [ADR-0053] (eight) carry the eleven decisions of the milestone. ADR-0052: [RC-431] No-Go, then [RC-433] Go-with-conditions with its conditions closed; ADR-0053: [RC-432] Go-with-conditions, then [RC-434] with its conditions closed; their second versions after the code in [RC-435]. A changed number names its reason: the fuse is 400 mails a day and not the planned 1,000, because Gmail limits an account to 500 (ADR-0053). Each record names its alternatives and why each lost. ADR-0052 is Accepted in its row of 2026-11-21 (decision 10 after MIL-089, [RC-439]), ADR-0053 in its row of 2026-11-20 |
| 2 | UC-011 and US-001.14 are Accepted after their reviews, with the artifacts they affect, and the dictionary has the two terms | Pass | [UC-011] with SSD-002, OC-002 and SD-001, and US-001.14 with the amended artifacts, Go in [RC-435]; each is Accepted in its latest row (OC-002 and SD-001 in their rows of 2026-11-21, after [RC-439]). The dictionary has Booking request (`BookingRequest`) and Verify (`Verify`) in its row of 2026-11-20, Accepted |
| 3 | A request stores no booking and takes no room: the same room is still offered to the next guest. The mail in the catcher holds the dates, the room size, the link and its time, and no text that was typed into the form. A request for the link that only reads changes nothing; the button verifies once; a second use, a wrong token and a lapsed one get the same answer | Pass | The eight tests that [RC-436] names for it passed on the last head (F1), among them `AskAsync_LastRoomAskedFor_IsStillOfferedToTheNextGuest`, `AskAsync_Mail_HoldsTheStayAndTheLinkAndNoTypedText`, `Get_Opened_ShowsTheDisabledFormAndCallsNothing` and `VerifyAsync_UsedWrongMalformedOrLapsedLink_GetsTheSameAnswer`, with the checks of the mail on the throwaway stack (`stack-check.txt`). On the live site no booking came before its button: each came one to four minutes after its request, and two of the five requests never became one |
| 4 | After the verification the booking is `Pending` with a reference, its guest was stored in the same transaction, the guest is signed in, and one mail carries the reference. Of 20 verifications at once for the last room of a size exactly one gets the booking, the other 19 are told that the room is gone, and no guest is left without a booking | Pass | The three tests that [RC-436] names passed on the last head, `VerifyAsync_TwentyAtOnceForTheLastRoom_OneBooksAndNineteenAreToldTheRoomIsGone` in PostgreSQL among them, and the browser test `Verify_LinkFromTheMail_BooksTheRoomAndFindsItWithTheReference`. On the live site each of the three bookings is `Pending` and got its receipt, the mail with its reference |
| 5 | With a fixed clock: a new request of one email makes the link of the old one useless; the fourth verification mail to one email within an hour and the sixth within a day are refused; a request is gone, with its personal data, within one hour after its link has lapsed | Pass | The seven names that [RC-436] gives passed on the last head with the fixed clock, among them `AskAsync_SecondRequestOfTheMailbox_MakesTheFirstLinkUseless`, `AskAsync_FourthMailInAnHour_IsRefusedAndKeepsTheThirdRequest`, `AskAsync_SixthMailInADay_IsRefused` and `RunAsync_LapsedRequestsAndOldRecords_AreDeleted`, and the five tests of `BookingRequestCleanupWorkerTests` |
| 6 | With the relay down the guest is told, no request is left and the counter counts; a relay without an encrypted connection is refused at the start; a line break in an email is refused before any mail is built; after a browser run no token, no verification address and no unmasked email is in a log of the test stack | Pass | The eight tests that [RC-436] names passed on the last head, among them `AskAsync_RelayDown_TellsTheGuestAndLeavesNothing`, `Start_RelayWithoutEncryption_IsRefusedWithTheNameOfTheSetting` and `AskAsync_LineBreakInTheEmail_IsRefusedBeforeAnyMail`; the checks of the logs of the throwaway stack after both runs of the browser tests are in `stack-check.txt`, and NF Hotel E2E and DAST runs them on every pull request |
| 7 | The browser tests pass on the throwaway stack with the mail catcher: ask, read the mail, verify, see the receipt with a reference, find the booking again; a lapsed link; send again. The rule tests show the alerts for mails that fail and for 200 mails in a day | Pass | NF Hotel E2E and DAST 44690972423 on the last head: 16 browser tests with the verification off and 3 with it on, all passed: `Verify_LinkFromTheMail_BooksTheRoomAndFindsItWithTheReference`, `Verify_UsedOrUnknownLink_GetsTheOneAnswer` and `SendAgain_FromTheInbox_SendsANewLinkAndTheFirstStopsWorking`. The rule tests of `MailNotSent`, `MailFuseBlown` and `MailVolumeHigh` passed in the job "Ops monitoring configuration (MIL-023)" of NF Hotel Ops CI 44690972415, which runs `promtool test rules`. A lapsed link is not waited for in a browser: its answer is that of a used or unknown link, and the lapse is held with the fixed clock and at the claim in PostgreSQL (`ClaimOpenAsync_AtTheMomentTheLinkLapses_ClaimsNothing`, passed), the deviation that [RC-436] accepted as stand-in; it stands |
| 8 | With the setting off, the public booking works as before this milestone and the tests of that path pass; with it on and no relay set, the API refuses to start and says why | Pass | The four tests that [RC-436] names passed on the last head, among them `RequestAndVerify_VerificationOff_AreNotFoundAndThePublicBookingBooks` and `Start_VerificationOnWithoutARelay_IsRefusedWithTheNameOfTheSetting`, and the 16 browser tests with the verification off. On the live site, after attempt 1 with the setting off, one booking was made at once and without a mail. The refusal to start without a relay was not tried on the live site, where it would take the site down; the API started there with the setting on, so it had every setting |
| 9 | The documents say what the deliverable says; the code review against `QC-CS-001`, and against `QC-SH-001` and `QC-YML-001` for what changes under `ops/`, is Go; the new package for mail is pinned, locked and free of known vulnerabilities; the coverage floor holds; every required check of the pull request is green | Pass | The documents: [RC-435], Go, its one Pending criterion, 14 of QC-WIKI-001, met on the published wiki in [RC-441]; one page that said more than the milestone did is corrected here (F2). The code: [RC-436], Go-with-conditions against QC-CS-001, QC-SH-001 and QC-YML-001, its condition met (F1). MailKit 5.0.0 is pinned in `NFHotel.Infrastructure.csproj` and locked with MimeKit.Core 5.0.0 in the lock files; the step "Restore and NuGet vulnerability check" of NF Hotel CI passed on the last head and on the merge, and no alert of Dependabot is open on either package at this gate. The coverage floor of NF Hotel CI passed on the last head and on the merge. Every required check of pull request 857 was green: 60 passed and 2 skipped by design |
| 10 | On the live site, once S01 has set the relay and the setting is on: a booking that S01 asks for the test arrives as a mail in S01's mailbox, takes no room before the button is pressed, is `Pending` with a reference after it, and a second request of the same email makes the first link useless; the evidence is in the evidence folder without personal data | Pass, with a deviation accepted as stand-in (F3) | S01 set the relay on 2026-11-22 and switched the verification on (above). The mail arrived in S01's mailbox: S01 wrote "det virker", and each of the three bookings came through the link of its mail, which only the mail carries, because the database keeps only the hash of the token. No room before the button: each booking came only at its button, and two requests never became a booking. Each booking is `Pending` after it, with its reference in its receipt. The second request: in S01's run of the check on the evening of 2026-11-24 ("Ja, min test") a link was refused after newer requests, but that link was also older than 30 minutes, so the live site shows the one refusal and not which rule gave it (F3). The evidence is `docs/sqa/evidence/mil-088/live-check.txt`, with no email, name, link, token, reference or date of a stay |

The page Mail-Relay as the gate changes it, against [QC-WIKI-001]: criterion 11 Pass, the language line of the page
is unchanged; 13 Pass, the commands and their prompts are in English; 16 Pass, the page stays in the flat folder
`docs/wiki`; 14 Pending, the page is checked on the published wiki after the merge of this gate (an action item).

## Findings

- **F1, note: the conditions of the reviews are met.**
  - [RC-436] gave the code Go-with-conditions: every required check of the pull request green. All 62 checks of pull
    request 857 ended green, 60 passed and 2 skipped by design. Every test that RC-436 names for criteria 3 to 8, 32
    names in all, passed on the last head ccefbd9f: 31 in the results of NF Hotel CI 44690972493, one of them the
    class `BookingRequestCleanupWorkerTests` with its five tests, and the browser test
    `Verify_UsedOrUnknownLink_GetsTheOneAnswer` in those of NF Hotel E2E and DAST 44690972423 (NF Hotel CI lists the
    browser tests as not run, because they need the stack). So RC-436 counts as Go.
  - Its other actions are done in [MIL-089]: the route of the verification, which has no limit per sender, was read
    against ASVS in SAR-002 and in decision 10 of [ADR-0052]; and the alerts 677 to 681 of CodeQL on the masked email
    were closed through the code, none dismissed ([RC-441] F5).
  - [RC-435] gave the documents Go, with criterion 14 of QC-WIKI-001 left to this gate: [RC-441] checked the published
    wiki at dcbc88ce, with every page and link of the pages of MIL-088 on it, and found it the same as `docs/wiki`.
- **F2, minor, fixed here: the page Mail-Relay.** Its Part B claimed that a wrong setting shows before the verification
  is on, which does not hold for the app password (above). The page now:
  - runs the commands of steps 4 to 6 with `runuser -u nfhotel` and not `sudo`, because S01 is root on the VM: then
    no setting of sudo can keep what is typed. The sudo of the repository keeps no input log ([ADR-0041]), but the VM
    could not be checked when the plan was made;
  - in step 5, drops every space and the no-break spaces that a copy from the browser can bring, and refuses anything
    but 16 small letters before the file is written;
  - in a new step 6, checks the file without its values and sends a test mail from the VM with the account and the
    app password of the file, before any deploy; the steps after it are numbered on, with the references to them;
  - says in step 3 of the live check that the first link is tried within its 30 minutes (F3).

  The commands are those that S01 ran on 2026-11-22, with the prompts in English and a test mail that does not say
  whether the verification is on. They were run against a fake env file in WSL with `curl` as a stub that sends
  nothing: a wrong value and capitals are refused and nothing is written; the four groups with a space and a no-break
  space give one line of 16 letters, mode 600, and a second run still one line; the check shows the ten names once
  each; the test mail hands the password to `curl` on its standard input and in no argument
  (`docs/sqa/evidence/mil-088/mail-relay-steps.txt`). [ADR-0053] decision 8 names the steps of S01 without their
  order or numbers, so it does not change.
- **F3, deviation accepted as stand-in: the second request on the live site.** The link that was refused on
  2026-11-24 at 18:16 UTC belonged to the request of 16:46 or of 17:07, so it was past its 30 minutes either way. The
  refusal is the one answer of an unknown, a used and a lapsed link, so the live site does not show that the second
  request made the first link useless. The rule is held by `AskAsync_SecondRequestOfTheMailbox_MakesTheFirstLinkUseless`
  with the fixed clock, and by the browser test `SendAgain_FromTheInbox_SendsANewLinkAndTheFirstStopsWorking` on the
  throwaway stack, with the code of production and the mail catcher; both passed. Seeing it on the live site would take
  a new test booking by S01, and the AI makes none. The AI, as stand-in for S01, accepts the deviation; S01 may
  challenge it, and step 3 of the page now says how to see it.
- **F4, note: the test bookings stay `Pending`, by S01's choice.** S01 decided that the three bookings of the check
  stay, and may delete them later. Their stays ended by 2026-11-27, so none holds a room for a night after the gate.
  They, and the copies of their mails in the folder Sent of S01's account, hold S01's own test data. No AI session
  cancels, deletes or uses them.

## Overall Verdict

Go on criteria 1 to 10, by the AI as stand-in for S01 in autonomous mode. Every criterion passes with the evidence
above: the two decision records and the use case accepted after their reviews, the tests of the request, the
verification, the mails and the relay, the browser tests on the throwaway stack, the code and documents reviewed with
their conditions closed, the relay set by S01 and the verification on in production, and S01's bookings of the check
on the live site. One part of criterion 10, the second request seen on the live site, rests on the tests instead, a
deviation accepted as stand-in (F3). The reviewer is not independent, and each stand-in decision is named in the
summary of the run.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept this record and the gate row of MIL-088, or return them | S01; done on 2026-11-27 by the AI as stand-in, at S01's word in chat | At the review of the gate pull request |
| Read the checks of the gate pull request before the merge | AI as stand-in for S01 | Before the merge |
| Check the page Mail-Relay on the published wiki (QC-WIKI-001 14) | mikemike | After the merge of the gate pull request |
| Step 5 of the live check: cancel the test bookings and delete the copies of the test mails (F4) | S01 | When S01 decides |
| Challenge the deviation of F3, or have step 3 of the live check run again within 30 minutes | S01 | When S01 decides |

---

[MIL-088]: ../../milestones/mil-088-a-booking-verified-by-email.md
[ADR-0052]: ../../adr/adr-0052-a-booking-takes-a-room-once-its-email-is-verified.md
[ADR-0053]: ../../adr/adr-0053-mail-from-the-application-through-a-relay.md
[ADR-0041]: ../../adr/adr-0041-connection-string-over-standard-input.md
[UC-011]: ../../use-cases/uc-011/uc.md
[RC-413]: ./rc-413-mil-088.md
[RC-431]: ./rc-431-adr-0052.md
[RC-432]: ./rc-432-adr-0053.md
[RC-433]: ./rc-433-adr-0052-second-round.md
[RC-434]: ./rc-434-adr-0053-second-round.md
[RC-435]: ./rc-435-mil-088-documents.md
[RC-436]: ./rc-436-mil-088-code.md
[RC-439]: ./rc-439-mil-089-documents.md
[RC-441]: ./rc-441-mil-089-gate.md
[MIL-089]: ../../milestones/mil-089-the-booking-flow-attacked-and-measured.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[QC-WIKI-001]: ../../../framework/qc/artifacts/qc-wiki.md
