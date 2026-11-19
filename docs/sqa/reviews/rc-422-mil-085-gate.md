# RC-422 Gate review of MIL-085 A booking reference that cannot be guessed

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-422 |
| CrossReference | [MIL-085], [ADR-0049], [RC-410], [RC-416], [RC-417], [RC-418], [RC-419], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-19 | Accepted | mikemike | AI as stand-in for S01 | Initial version: the gate of MIL-085 after the deploy of 7fd5bea (CD run 44514175991) and the check of criterion 8 on the live site; Go on criteria 1 to 8; mode autonomous, not independent | pending |

---

## Artifact Under Review

- Instance reviewed: [MIL-085] at its gate. It covers the eight Go/No-Go criteria, with this evidence:
  - the documents and the code of tasks 1 to 5 on `main` since 0b1350b (pull request 837, merged on 2026-11-19 at
    10:45:22 UTC), reviewed in [RC-416] ([ADR-0049], Go), [RC-417] (the documents, Go), [RC-418] (the code,
    Go-with-conditions) and [RC-419] (the amendment of [ADR-0049] after the code review, Go);
  - the checks of pull request 837 on its last head (06ea8ba): 60 passed and 2 skipped, which skip by design (OWASP
    ZAP and DAST full scan). The runs of that head all passed: NF Hotel CI 44506929146, NF Hotel E2E and DAST
    44506929165, NF Hotel Ops CI 44506929267, NF Hotel Release check 44506929105, and the two runs of CodeQL;
  - the runs of the merge commit 0b1350b on `main`, which all passed: NF Hotel CI 44508593134, NF Hotel E2E and DAST
    44508593152, NF Hotel Ops CI 44508593200, NF Hotel Wiki 44508593808, NF Hotel Scorecard 44508593444, NF Hotel
    Release 44509384119 and the two runs of CodeQL. The run 44510422356 of NF Hotel CD failed (below);
  - the run 44514175991 of NF Hotel CD at 7fd5bea, which deployed MIL-085 (below);
  - the check of criterion 8 on the live site (below, and `docs/sqa/evidence/mil-085/live-check.txt`).

  Before the gate: [RC-410] (the plan, Go).
- Checklist used: the Go/No-Go criteria of [MIL-085]. [QC-MIL-001] criterion 2 makes them the checklist of the gate.
- Scope: the gate (task 6).
- Language and domain: en / it (the Metadata rows of MIL-085). The gate adds only a row to its Version History. This
  record is a technical type.
- Language reviewer: none.
- Mode: autonomous, not independent. S01 chose autonomous mode in chat on 2026-11-19 ("Når din plan er lavet skal
  Opus 5.5 lave det hele i automode") and handed the decisions to the AI ("Du skal tage beslutningerne for mig"). An
  AI built the milestone and wrote this record. The planning session, another session of the AI, read the deploy on
  the VM, made the test booking and ran the check; its facts are quoted here.

**The runs of CD.** The AI that built this milestone approves no run of CD; S01 approves each one.

- Run 44510422356, at 0b1350b, created at 10:57:35 UTC. The planning session approved it from S01's account at
  10:59:06 UTC, at S01's word in chat on 2026-11-19 ("du må gerne godkende deploy."). It installed the commands of
  0b1350b, backed up the database (11:00:26 UTC), applied the migration of MIL-085 as `nfhotel_migrator`, backed up
  again (11:00:33 UTC), and failed at "deploying api": the registry of GitHub answered `denied`, because the read
  credential of the VM for that registry had run out. The deploy rolled back to 46b29fb: the runtime check passed 11
  of 11, the smoke test passed, and the run ended with exit code 1. The database kept the migration (the column
  `reference` and the index `ux_booking_reference`), and the release of 46b29fb ran on it without a fault (F1).
- S01 renewed the credential. A first token, at 11:42 UTC, lacked `read:packages`; the second, at 11:48 UTC, works. At
  11:51 UTC the planning session pulled the running API image as the user `nfhotel` and found all six images of the
  release readable, without reading the token.
- Run 44514175991, at 7fd5bea, created at 11:22:04 UTC: the merge of pull request 836 (MIL-084), whose first parent
  is 0b1350b, so this run deployed MIL-085 together with the change of the weekly look of MIL-084. It held the one
  concurrency group of CD, so the failed run was not run again. It was approved from S01's account at about 11:50:33
  UTC, with no comment, by S01. The job ran from 11:50:33 to 11:53:15 UTC and passed. Before it, every workflow run of
  7fd5bea had passed and no other deploy ran.
- After it, on the VM at 11:53:53 UTC: the API and the web application run their new images, and both are healthy, as
  are nginx, the market analysis and the benchmark. The log of the API has no line with fail, error or exception
  since its start. From outside: `/`, `/book` and `/.well-known/security.txt` answer 200, and `/booking/FLZ-000001`
  answers 302 to `/booking`.

**The check of criterion 8.** The planning session made it at S01's word in chat on 2026-11-19 ("lav gerne
prøvebookinger på det rigtige system også"):

- At 11:54 UTC it booked on `/book` one night (2027-02-02 to 2027-02-03) in a single room for one guest, with a
  made-up name, phone number and country, no passport number, and S01's mailbox as the email. The receipt showed
  the status Pending and the reference 25082319-9db9-435f-866f-9f7674f44256. That is version 4 (the first digit of its
  third group) and the variant of RFC 9562 (the first digit of its fourth group is 8).
- At 11:55:20 UTC it ran `check-guest-login.sh --cancel` from the script of 7fd5bea in WSL (curl 8.14.1, bash 5.2.37).
  The email came from a local file in the variable `GUEST_EMAIL` and was never on a command line. 21 of 21 checks
  passed, with exit code 0, and the booking was cancelled, so it holds no room. The output is
  `docs/sqa/evidence/mil-085/live-check.txt`; the site stays in a variable there, and it holds no email address.
- A second test booking, made at 12:10 UTC for a picture and cancelled by the same check, is not evidence for the
  gate.

## Checklist Results

| # | Criterion (MIL-085) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | ADR-0049 is Accepted after its review: decisions 1 to 8, the four alternatives with the reason each lost, and the texts it rests on | Pass | [ADR-0049] is Accepted. [RC-416] is Go against QC-ADR-001: the eight decisions, the alternatives with the reason each lost (a shorter random code, a UUID as the key of the table, a UUID of version 7, an encrypted number), and the texts it rests on (RFC 9562, the ASVS). Its amendment after the code review, which states decisions 2 and 6 as the code does them, is Go in [RC-419] |
| 2 | Every booking has a unique reference that is a UUID of version 4: a test reads the version and the variant, finds 10,000 references made in a row all different, and names the generator; after the migration on PostgreSQL every booking that existed has a reference, the number of bookings is unchanged, and the release before this one passes its smoke test against the migrated database | Pass | `BookingReferenceTests` reads the version and the variant, makes 10,000 references in a row and finds them all different, and names the generator (`Guid.NewGuid`, the random generator of the operating system). `BookingReferenceMigrationTests` passed on PostgreSQL in NF Hotel CI on the pull request and on the merge: every booking that existed has a reference, and the count is unchanged. The release before this one ran on the migrated database of the live site: the rollback of run 44510422356 to 46b29fb passed its runtime check and its smoke test after the migration, and that release served the site until 11:50 UTC without a fault (F1) |
| 3 | No page (public, guest or staff) and no address shows the counting key or the text `FLZ-`, and no public or guest operation of the API takes or returns the key: a test walks the pages and the contracts, and the test of the public endpoints knows the new list | Pass | The tests of the pages (`GuestPagesTests`, `MyBookingViewModelTests` and the pages of the back office) and of the contracts (`GuestAccessTests`, `BookingReadProtectionTests`, `GuestGatewayTests`) passed on the pull request and on the merge. `EndpointProtectionTests` knows the new list of the public endpoints. On the live site, the sign-in page names no booking number, and an old address with a number goes to the empty sign-in (the evidence) |
| 4 | With the right reference and email, in any letter case of the email, a lookup and the guest sign-in succeed; a number of the old kind, an unknown reference, an unreadable one and a wrong email get the same status and the same body; the sixth wrong try for one reference within 15 minutes gets 429, and so does the try over the limit per address that ADR-0049 sets; 100,000 tries with different references leave the store within its bound | Pass | `GuestAccessServiceTests` and `GuestAccessTests` hold the sign-in with the email in any letter case, the same status and body for the four refusals, the sixth wrong try per reference and the try over 20 per address with 429. `InMemoryGuestSignInLimitsTests` makes 100,000 tries with different references and finds both stores within their bound of 10,000. On the live site, a wrong email and an old number with the right email were refused with the same refusal, and the reference with the email signed the guest in (the evidence) |
| 5 | The public and guest answers hold only the fields of decision 7, shown by a contract test; a guest token for one booking still gets 403 for another | Pass | The contract test of `GuestAccessTests` holds every answer to a guest to the receipt of decision 7 and nothing more, and `GuestOwnershipTests` refuses each guest operation on another booking with 403 before anything changes. On the live site, the own booking shows its reference and no email (the evidence) |
| 6 | The browser test passes on the throwaway stack: book, see a receipt with a reference in UUID form, find the booking with reference and email, sign out, sign in, cancel; an unknown reference gets no answer of the 500 kind | Pass | `GuestBookingTests` of the end-to-end suite passed in NF Hotel E2E and DAST on the last head of the pull request (44506929165) and on the merge (44508593152). That closes criterion 11 of QC-SH-001, which [RC-418] left pending for the end-to-end job and for this task |
| 7 | The documents say what the deliverable says; the code review against `QC-CS-001` is Go; the coverage floor holds; every required check of the pull request is green | Pass | [RC-417] is Go on the documents. [RC-418] is Go-with-conditions, and its condition is met: every required check of the pull request was green before the merge, with the tests on PostgreSQL, the browser tests and the coverage floor (F2). The coverage floor step of NF Hotel CI passed; 60 checks passed and 2 skipped by design |
| 8 | On the live site after the deploy: a number of the old kind shows no booking, a booking that S01 makes for the test, or that the AI makes on S01's word, has a reference of version 4 and opens with its email only, and the evidence is in the evidence folder | Pass | The check above. The AI made the booking at S01's word in chat, and it has a reference of version 4. 21 of 21 checks pass: an old address with a number goes to the empty sign-in, an old number with the right email is refused, a wrong email is refused and sets no cookie, and the reference with the email opens the booking. The booking is cancelled. The evidence is `docs/sqa/evidence/mil-085/live-check.txt` |

## Findings

- **F1, note: the rollback ran on the migrated database.** Decision 4 of [ADR-0049] lets the migration only add, so
  that the release before it keeps working on the migrated database. The failed run 44510422356 put that to the test
  on the live site: the migration was applied, the image of the new release could not be pulled, and the release of
  46b29fb ran on the migrated database from 11:00 to 11:50 UTC, with its runtime check and smoke test passed and every
  container healthy. That is the last clause of criterion 2, seen in production and not only in the test.
- **F2, note: the condition of the code review is met.** [RC-418] gave Go-with-conditions: every required check of the
  pull request green before the merge. Pull request 837 merged with every check green, so the review counts as Go for
  criterion 7.
- **F3, fixed elsewhere: the check needs Linux or WSL.** A first run of `check-guest-login.sh` from Git Bash on Windows
  failed because of the tool, not the site. The curl of Git Bash cannot open the files that the script hands it as
  `name@/tmp/...`, so it sent no form with the email, and that run made no failed tries. It is not evidence. The line
  "Needs" of the script now says that it runs on Linux or in WSL and not in Git Bash on Windows. The pull request of
  [MIL-086] carries that line, because it deploys in any case, and this pull request stays documents only.
- **F4, note: two milestones in one deploy.** The run that deployed MIL-085 was the run of the merge of pull request
  836. The failed run was not run again, because the one concurrency group of CD was held by the later run, and that
  later run deploys `main` with everything before it. The look of MIL-084 and the reference of MIL-085 changed
  different files, and the check above concerns only MIL-085.
- **F5, note: the four notes of SonarCloud on pull request 837.** SonarCloud passed its quality gate on pull request
  837 and noted four new issues, all of the lowest severity, Info: a return type that could be narrower
  (`GuestBookingController.Answer`), a primary constructor (`RequiresGuestAttribute`), the type `System.Threading.Lock`
  (`InMemoryGuestSignInLimits`), and a file-scoped namespace in the migration that EF Core generated. None is a
  criterion. The first three are taken up when [MIL-087] edits those files; the migration stays as it was generated.

## Overall Verdict

Go on criteria 1 to 8, by the AI as stand-in for S01 in autonomous mode. Every criterion passes with the evidence above:
the decision record and its amendment, the reference and its migration, the pages and the contracts without the
counting key, the one lookup with its limits, the answers of decision 7, the browser test, the reviews and the checks,
and the check on the live site after the deploy. The reviewer is not independent, and each stand-in decision is named
in the summary of the run.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept this record and the gate row of MIL-085, or return them | S01; done on 2026-11-19 by the AI as stand-in, at S01's word in chat | At the review of the gate pull request |
| Read the checks of the gate pull request before the merge | AI as stand-in for S01 | Before the merge |
| Say in the line "Needs" of `check-guest-login.sh` that it runs on Linux or in WSL (F3) | The AI | In the pull request of MIL-086 |
| Take up the three notes of SonarCloud on code (F5) | The AI | With MIL-087 |
| Count the address that nginx saw in the limits per address, so that a caller cannot pick the key of another (the note of the planning session, and [RC-418] F5 and F13) | The AI | With MIL-087 |

---

[MIL-085]: ../../milestones/mil-085-a-booking-reference-that-cannot-be-guessed.md
[ADR-0049]: ../../adr/adr-0049-booking-reference.md
[RC-410]: ./rc-410-mil-085.md
[RC-416]: ./rc-416-adr-0049.md
[RC-417]: ./rc-417-mil-085-documents.md
[RC-418]: ./rc-418-mil-085-code.md
[RC-419]: ./rc-419-adr-0049-after-the-code-review.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[MIL-086]: ../../milestones/mil-086-booking-input-checked-by-one-set-of-rules.md
[MIL-087]: ../../milestones/mil-087-limits-on-what-one-sender-can-ask.md
