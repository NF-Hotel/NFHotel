# RC-430 Gate review of MIL-087 Limits on what one sender can ask of the booking flow

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-430 |
| CrossReference | [MIL-087], [ADR-0051], [RC-412], [RC-426], [RC-427], [RC-428], [RC-429], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-20 | Accepted | mikemike | AI as stand-in for S01 | Initial version: the gate of MIL-087 after the deploy of 6f7bb80 (CD run 44624470820) and the check of criterion 8 on the live site; Go on criteria 1 to 8; mode autonomous, not independent | pending |

---

## Artifact Under Review

- Instance reviewed: [MIL-087] at its gate. It covers the eight Go/No-Go criteria, with this evidence:
  - the documents and the code of tasks 1 to 7 on `main` since 6f7bb80 (pull request 855, merged on 2026-11-19 at
    21:50:02 UTC), reviewed in [RC-426] and [RC-429] ([ADR-0051], both Go), [RC-427] (the documents, Go) and
    [RC-428] (the code, Go-with-conditions);
  - the checks of pull request 855 on its last head (420e1d8): 60 passed and 2 skipped, which skip by design (OWASP
    ZAP and DAST full scan). The runs of that head all passed: NF Hotel CI 44621130751, NF Hotel E2E and DAST
    44621130754, NF Hotel Ops CI 44621130781, NF Hotel Release check 44621130705, and the runs of CodeQL
    (44621118864) and of code quality (44621119036);
  - the eight runs of the merge commit 6f7bb80 on `main`, which all passed: NF Hotel CI 44622713061, NF Hotel E2E and
    DAST 44622712934, NF Hotel Ops CI 44622713029, NF Hotel Wiki 44622712986, NF Hotel Scorecard 44622713033, NF Hotel
    Release 44623509247, CodeQL 44622711750 and code quality 44622711929;
  - the run 44619538138 of NF Hotel load test on the branch, at 7edc508 (below);
  - the run 44624470820 of NF Hotel CD at 6f7bb80, which deployed MIL-087 (below);
  - the check of criterion 8 on the live site (below, and `docs/sqa/evidence/mil-087/live-check.txt`), and the check
    of the throwaway stack with the numbers of production (`docs/sqa/evidence/mil-087/stack-check.txt`).

  Before the gate: [RC-412] (the plan, Go).
- Checklist used: the Go/No-Go criteria of [MIL-087]. [QC-MIL-001] criterion 2 makes them the checklist of the gate.
- Scope: the gate (task 8).
- Language and domain: en / it (the Metadata rows of MIL-087). The gate adds only a row to its Version History. This
  record is a technical type.
- Language reviewer: none.
- Mode: autonomous, not independent. S01 chose autonomous mode in chat on 2026-11-19 ("Når din plan er lavet skal
  Opus 5.5 lave det hele i automode") and handed the decisions to the AI ("Du skal tage beslutningerne for mig"). An
  AI built the milestone and wrote this record. The planning session, another session of the AI, read the deploy on
  the VM; its facts are quoted here.

**The run of CD.** The AI that built this milestone approves no run of CD; S01 approves each one.

- Run 44624470820, at 6f7bb80, the merge of pull request 855 (parents cd29074 and 420e1d8), created at 22:00:59 UTC
  by the event `workflow_run`. Before it, the run 44619360629 of cd29074 (the merge of pull
  request 854, the framework update) waited; S01 approved it on GitHub, with an empty comment, and it ended in success
  at 22:47:12 UTC. The run of 6f7bb80 then became the waiting one.
- At 22:47 UTC all eight workflow runs of 6f7bb80 had passed, every check run of the commit was a success, skipped or
  neutral, and no other deploy ran or waited. On the VM: no line of the mail password in the environment file of the
  API, a pull as the user `nfhotel` of the running API image answered "Image is up to date", and no container was
  unhealthy. The planning session approved the run from S01's account at 22:48:04 UTC, at S01's word in chat on
  2026-11-19 ("du må gerne godkende deploy."), with the comment "Deploy of pull request #855 (MIL-087, limits on what
  one sender can ask): every check of the merge commit 6f7bb80 is green." The job "Deploy to production" ran from
  22:48:07 to 22:50:57 UTC and passed. The merge holds one migration, which only adds the column
  `made_on_public_site` with false for every existing booking.
- After it, on the VM at 22:52 UTC: the API, the web application, nginx, the market analysis and the benchmark are
  healthy, and the running nginx has the two lines with `X-NFHotel-Client-Address`. From outside: `/`, `/book`,
  `/booking` and `/.well-known/security.txt` answer 200.
- The address that the limits count, read once by the planning session at 22:51:55 UTC from a client outside the
  tailnet, through the tunnel: one guest sign-in with a reference that no booking has and a made-up email at
  `example.invalid`. The page answered 200 with the refusal and set no cookie, and the one event
  `guest_lookup_failed` in the log of the API names a public IPv4 address, neither a private one nor the key
  `unknown`. So nginx, the web application and the API hand on the visitor's own address behind `cloudflared`.

**The check of criterion 8.** The AI that built this milestone made it on 2026-11-20 at 01:00 UTC; it booked nothing
and stored nothing on the live site but the event of the refusal:

- The counter `nfhotel_security_events_total`, read from Prometheus through Grafana at 01:00:23 UTC, had one series:
  `guest_lookup_failed` at 1, the sign-in of the planning session above. No series of `limit_refused`.
- 61 searches for free rooms on `/book` from one address, in Chromium without a window, driven by Playwright 1.62.0 in
  its container image (`mcr.microsoft.com/playwright/dotnet:v1.62.0-noble`), never on the desktop of the host
  (`framework/process/gui-testing.md`). The dates (2026-12-30 to 2027-01-01) are typed into the fields and each field
  is left with Tab, as a guest does; the button "Find available rooms" is pressed, and "Back to dates" after each
  answer. No room type was chosen and the form of the guest was never reached:
  - searches 1 to 60 each showed the room step, the 60th 13.1 seconds after the first press;
  - search 61, 13.3 seconds after the first press: refused, and the page stays on the dates with the one text of the
    limits, "Too many requests in a short time. Please wait a while and try again, or contact the hotel.";
  - one search 65.9 seconds after the first press, when the window of a minute was over: the room step again.
- The counter at 01:01:47 UTC: `guest_lookup_failed` at 1 and a new series `limit_refused` with `limit="search"` at 1,
  the refusal of search 61.
- The limit on booking requests is not tried on the live site, because a booking request that passes the limit makes a
  booking. The throwaway stack shows it with the numbers of production: the 6th booking request from one address is
  refused with 429, the code and the whole seconds of `Retry-After` (`stack-check.txt`).
- The output is `docs/sqa/evidence/mil-087/live-check.txt`; the site stays in a variable there, the address of the
  workstation is not printed, and it holds no personal data.

## Checklist Results

| # | Criterion (MIL-087) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | ADR-0051 is Accepted after its review: decisions 1 to 7 with any changed number and its reason, and the four alternatives with the reason each lost | Pass | [ADR-0051] is Accepted. [RC-426] is Go against QC-ADR-001 on its first version: the seven decisions with their numbers, where each count lives and what a restart does to it, how the address travels from nginx to the API, and the four alternatives that the milestone names with the reason each lost; no number of the milestone changed. [RC-429] is Go on the second version, which keeps every decision and says what a full store drops, caps a limit at 100,000, and names the forwarded headers of the web application and the sign-in limit of the mobile app |
| 2 | The table of the limits is in the rule ledger, and a test fails when a default of the settings differs from the table | Pass | The table "Limits of the booking flow" is in `docs/refactor/BusinessRules.md`. `Table_Numbers_AreTheDefaultsOfTheirSettings`, `Table_Windows_AreTheWindowsOfTheLimits`, `Table_Names_AreTheLimitsOfTheSecurityEvents`, `Table_Refusals_Are429WithACodeOfTheApi` and `Settings_MaxTrackedKeys_BoundEachStoreAtTenThousand` in `LimitsTableTests` compare it with the settings; they passed in NF Hotel CI on the last head and on the merge |
| 3 | For every limit, with a fixed clock: the request at the number is accepted, the next one gets 429 with the one text and the time to wait, and after the window the count starts again; the fourth coming booking of one email on the public site is refused and the same booking by staff is accepted | Pass | On the fixed clock of the world, `SenderLimitApiTests` takes the 60th search and refuses the 61st with the code and `Retry-After`, answers again after the minute, refuses the 6th booking request in an hour and the 21st in a day and books again after the hour, refuses the fourth coming booking of one email and books it by staff (`Booking_ByStaffForAnEmailAtItsLimit_IsMade`); `InMemorySenderLimitsTests`, `PublicBookingLimitTests` and `BookingPublicCountTests` on PostgreSQL hold the same at the store, the use case and the database, and `InMemoryGuestSignInLimitsTests` holds the sign-in limits of MIL-085 in the shared store. They passed in NF Hotel CI on the last head and on the merge; the page shows the one text (`BookingFlowViewModelTests`) |
| 4 | Through nginx in the test stack, a request that writes its own `X-Forwarded-For` and `X-Real-IP` is counted and audited under the address that nginx saw; a call to the API without the service token cannot set the address; forwarded headers from outside the networks of the stack are ignored | Pass | `ops/tests/test-nginx.sh` shows that nginx writes the address it saw into `X-NFHotel-Client-Address` over the headers that the caller wrote; `CreateAccount_InACircuit_IsAuditedUnderTheAddressThatNginxSaw` in the browser tests and the check of the throwaway stack (failed guest sign-ins with forged headers counted under the address of nginx, `stack-check.txt`) show it through the stack; `StaffSignIn_WithoutTheServiceToken_CannotNameTheAddress`, `Of_CallWithoutTheServiceToken_IgnoresTheHeader`, `StaffSignIn_ForwardedFromOutsideTheStack_IsAuditedUnderThePeer` and `ClientAddressMiddlewareTests` hold the API and the web application. They passed in NF Hotel CI, NF Hotel E2E and DAST and NF Hotel Ops CI on the last head and on the merge |
| 5 | After 100,000 requests from different addresses the stores are within their bound, and the requests of one address do not use up the count of another | Pass | `TryCount_HundredThousandAddresses_StaysWithinTheBound`, `TryCount_OtherAddress_IsCountedApart`, `TryCount_ManyAtTheSameTime_CountsExactlyTheLimit` and the two tests of a full store in `InMemorySenderLimitsTests`, with `Search_OfAnotherAddress_IsNotStopped` and `Search_HundredAtTheSameTime_AnswersSixty` at the API, passed in NF Hotel CI on the last head and on the merge |
| 6 | Each event kind is written once, with a masked email and without another typed value, and the check of the logs of the test stack passes; the counters exist; the rule tests show both alerts firing at their numbers; the wiki page Alerts names them | Pass | `Booking_Made_WritesOneEventWithTheAddressAndTheMaskedEmail`, `GuestSignIn_WithAWrongEmail_WritesOneEventWithoutTheReference`, `Refusals_OfEveryLimit_WriteOneEventEachWithTheName`, `Events_OfTheBookingFlow_AreCountedOnceWithTheirKindAndLimit` and `SecurityEventValuesTests` passed in NF Hotel CI; the check of the logs of the throwaway stack found the events with the address and a masked email and no email of the check in any log (`stack-check.txt`, and NF Hotel E2E and DAST on the merge); the rule tests in `ops/deploy/monitoring/prometheus/rules-test.yml` fire `BookingFlowRefusals` above 20 refusals in 10 minutes and `PublicBookingSurge` above 10 public bookings in an hour, in NF Hotel Ops CI; the wiki pages Alerts and Security-Controls name both. On the live site the counter has the series of `guest_lookup_failed` and of `limit_refused` (above) |
| 7 | The browser tests and the weekly load test pass with the numbers of production; the documents say what the deliverable says; the code review against `QC-CS-001`, and against `QC-SH-001` and `QC-YML-001` for what changes under `ops/`, is Go; the coverage floor holds; every required check of the pull request is green | Pass | The browser tests passed in NF Hotel E2E and DAST on the last head (44621130754) and on the merge (44622712934). The load test of a throwaway stack with the default settings, which are the numbers of production (run 44619538138, at 7edc508), passed: k6 opened `/`, `/book` and `/admin/login` with up to 20 virtual users, 1,830 requests, none failed, 95% within 2.57 milliseconds. It sends no search and no booking. The last commit after it (420e1d8) changed comments and the subnet of the stack check, and the stack check passed on that head. [RC-427] is Go on the documents. [RC-428] is Go-with-conditions against QC-CS-001, QC-SH-001 and QC-YML-001, and both conditions are met (F1). The step "Coverage floor" of NF Hotel CI passed on the last head, and SonarCloud passed its quality gate with 99.7% coverage on the new code. 60 checks passed and 2 skipped by design |
| 8 | On the live site after the deploy, without a booking being made: the search for free rooms over its limit from one address gets the refusal, and the refusals show in the counter; the limit on booking requests is shown on the throwaway stack with the numbers of production; the evidence is in the evidence folder | Pass | The check above. The 61st search within a minute from one address was refused with the one text of the limits, a search after the minute was answered, and the counter went from no series of `limit_refused` to 1 for the limit `search`. The 6th booking request is refused on the throwaway stack with the numbers of production. Nothing was booked. The evidence is `docs/sqa/evidence/mil-087/live-check.txt` and `stack-check.txt` |

## Findings

- **F1, note: the conditions of the code review are met.** [RC-428] gave Go-with-conditions: every required check of
  the pull request green, and the load test passed on the branch with the numbers of production. Pull request 855
  merged with every required check green, and the load test passed on the branch (criterion 7), so the review counts
  as Go.
- **F2, note: four alerts of CodeQL on the security events.** The check CodeQL of the last head passed and named four
  new alerts of medium severity, `cs/exposure-of-sensitive-information` 673 to 676, in
  `src/NFHotel.Api/Security/BookingFlowEvents.cs` (lines 28, 37 and twice 49); they are open on `main` since the
  merge. What reaches the log there is the address of the caller and the email masked by `SecurityEvents.MaskEmail`
  (its first character and its domain), which decision 6 of [ADR-0051] asks for (ASVS V16.3); CodeQL reads the
  parameters `address` and `email` as private data and does not know the mask. Not a criterion of MIL-087. Criterion
  3 of [MIL-089] asks for no open alert in the changed code, so its task 3 takes them up: a change that CodeQL can
  follow, or a dismissal with its reason.
- **F3, note: one line about a GSSAPI library in the log of the API.** The planning session found "Error:
  libgssapi_krb5.so.2: cannot open shared object file: No such file or directory" once in the log of the API since
  its start. It is not new with this release: the merge changed no package, project file, lock file or Dockerfile
  (886c8e8 to 6f7bb80), and the throwaway stack of CI shows the same line when a process of the API image first opens
  the database, on 2026-11-15 (run 43697204070 at b6d3f43) and at the merge of MIL-086 (run 44567142139). It comes
  from the GSS session encryption of Npgsql 10 (the setting `GSS Encryption Mode`, which Npgsql 9.0.3 did not have):
  the .NET runtime tries to load the GSSAPI library, which the image of the API does not hold, writes the line, and
  Npgsql connects without GSS encryption; the API stays healthy and reads the database. The security review of
  [MIL-089] (SAR-002) records a verdict on it; a change of the setting would be a task of its own.
- **F4, note: the check used up the search limit of one address for the rest of a minute.** From search 61 until 60
  seconds after the first press, any other visitor behind the public address of the workstation would have been
  refused a search too; the check ran at 01:00 UTC and the search at 65.9 seconds was answered.
- **F5, note: three notes of SonarCloud.** SonarCloud passed its quality gate on pull request 855 and noted three new
  issues of the lowest severity, Info: CA1873 (a log call that builds its values before the logger is asked whether
  it writes) in `BookingFlowEvents` (line 28), IDE0161 in the generated migration, and CA1862 in `ServiceWorld` (line
  311). None is a criterion. [MIL-088] takes up the first and the third when it edits those files; the generated
  migration stays as the tool wrote it.

## Overall Verdict

Go on criteria 1 to 8, by the AI as stand-in for S01 in autonomous mode. Every criterion passes with the evidence above:
the decision record in its second version, the table with its test against the settings, every limit at its number on
a fixed clock, the address that nginx saw as the only address of a caller, the bounded stores, the events with their
counter and alerts, the reviews, the load test and the checks, and the check on the live site after the deploy. The
reviewer is not independent, and each stand-in decision is named in the summary of the run.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept this record and the gate row of MIL-087, or return them | S01; done on 2026-11-20 by the AI as stand-in, at S01's word in chat | At the review of the gate pull request |
| Read the checks of the gate pull request before the merge | AI as stand-in for S01 | Before the merge |
| Take up the four alerts of CodeQL (F2) and give the GSSAPI line a verdict (F3) | The AI | With MIL-089 |
| Take up the notes CA1873 and CA1862 of SonarCloud (F5) | The AI | With MIL-088 |

---

[MIL-087]: ../../milestones/mil-087-limits-on-what-one-sender-can-ask.md
[ADR-0051]: ../../adr/adr-0051-limits-of-the-booking-flow-and-the-caller-address.md
[RC-412]: ./rc-412-mil-087.md
[RC-426]: ./rc-426-adr-0051.md
[RC-427]: ./rc-427-mil-087-documents.md
[RC-428]: ./rc-428-mil-087-code.md
[RC-429]: ./rc-429-adr-0051-second-version.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[MIL-088]: ../../milestones/mil-088-a-booking-verified-by-email.md
[MIL-089]: ../../milestones/mil-089-the-booking-flow-attacked-and-measured.md
