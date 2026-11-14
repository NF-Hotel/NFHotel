# RC-342 Gate review of MIL-079 A booking opened only with its email

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-342 |
| CrossReference | [MIL-079], [RC-340], [RC-341], [ADR-0038], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-14 | Accepted | mikemike | AI as stand-in for S01 | Initial version: the gate of MIL-079 after the CD run of the merge and the check on the live site; Go; drafted by the AI, not independent<br>Accepted on 2026-11-14 by the AI as stand-in for S01, at S01's word in chat ("Du godkender for mig"); autonomous, not independent | [fc88265] |

---

## Artifact Under Review

- Instance reviewed: [MIL-079] at its gate. It covers the five Go/No-Go criteria, with this evidence:
  - pull request #651, merged as c25fff7 on 2026-11-12 at 16:03 UTC;
  - the release run 43255298062;
  - the CD run 43256465429 of that commit;
  - the check on the live site after the deploy.

  Before the gate: [RC-340] (the plan, accepted by S01 in chat) and [RC-341] (the code, Go).
- Checklist used: the Go/No-Go criteria of [MIL-079]. [QC-MIL-001] criterion 2 makes them the checklist of the gate.
- Scope: the gate. Criteria 1, 2, 3 and 5 cite RC-341 and the checks of #651. Criterion 4 rests on the check on the
  live site.
- Language and domain: en / it (the Metadata rows of MIL-079). The language of the document was reviewed in RC-340, and
  the gate adds only a row to its Version History. This record is a technical type.
- Language reviewer: none.
- Mode: supervised. The AI wrote the plan, the code and this record, so it is not independent. S01 accepts the gate
  in chat or in the review of the pull request.

**Evidence of 2026-11-12:**

- **The pull request.**
  - #651 on its last commit 8c93d2c: 52 checks passed. `DAST full scan` was skipped by design (it runs only by hand
    and on schedule), and `OWASP ZAP` was neutral. All 44 required checks of `main` passed.
  - SonarQube Cloud: 0 new issues and 100% coverage on new code.
  - S01 merged it at 16:03 UTC. The merge carried ae5d61e, the .NET images at 10.0.15 of #650, so GitHub marked #650
    as merged too.
- **The release.** Run 43255298062 of c25fff7 logged "PASS … no secret, no critical finding and no high finding with
  a fix" for each of the six release images between 16:09 and 16:11 UTC: `nfhotel-web`, `nfhotel-api`,
  `market-analysis`, `benchmark`, `nfhotel-nginx` and `nfhotel-socket-proxy`. `nfhotel-web` passed on the .NET
  runtime 10.0.15, which fixes CVE-2026-114146, CVE-2026-114147 and CVE-2026-114149. Those three had stopped the first
  release check of #651.
- **The CD queue.**
  - CD run 42966068447 of 78865e7 had waited for approval since 2026-11-11. Outside `docs/` it was the same as the
    live ec2d8a1. S01 approved it, and it deployed from 16:09 to 16:13 UTC.
  - Run 43002477847 of 1c2726b (the code of MIL-071) then waited in front of the run of c25fff7. S01 asked in chat to
    cancel it ("Ja, annuller #208"). The AI cancelled it at 16:17 UTC, while it still only waited for approval. One
    deploy then brings the code of MIL-071 and MIL-079 and the .NET 10.0.15 images.
- **The deploy.** CD run 43256465429 was approved by S01 in GitHub at 16:18 UTC and ended with success at 16:21:58 UTC.
  - Its verify step logged "build provenance and SBOM verified" for each of the six images.
  - The check of the containers found no secret in their environment.
  - The smoke test answered 301 on port 80 and 200 on port 443.
- **The live site after the deploy.**
  - 16:22:13 UTC, with curl (`after-receipt.txt`, `after-count.txt`):
    - the first HTML of `/booking/FLZ-000001` has no "Keep this number" and no `<dt>` or `<dd>`;
    - it holds the form "Find your booking", with the number filled in and an empty email field;
    - the count over FLZ-000001, FLZ-000002, FLZ-000003 and FLZ-999999 shows no booking. Before the fix it showed
      three (`before-count.txt`).
  - 16:22:45 UTC, in the headless Chromium of the Playwright image in Docker (`after-FLZ-000001.png`,
    `after-FLZ-999999.png`, `after-live-check.txt`): both pages show the form with the number and no booking card. A
    number that exists and one that does not look the same.
  - By 16:28:10 UTC, in the same browser:
    - the test booking FLZ-158424 (one night in a single room, 10 to 11 June 2030) was made through the public booking
      page with the email S01 gave in chat;
    - the booking page ended on its receipt step (`after-test-booking-receipt-step.png`);
    - looked up with another email, it answered "We couldn't find that booking." with no card
      (`after-test-booking-other-email.png`);
    - looked up with its own email, it showed the card of FLZ-158424, with no name, email or phone
      (`after-test-booking-own-email.png`).

    The email is in none of the files.

## Checklist Results

| # | Criterion (MIL-079) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | A read of a booking by id with the API token and no staff token gets 401, with a staff token without `Bookings.View` 403, and with the permission 200; the review record shows the test failing on the code before the fix and passing after it, as transcripts in the evidence folder | Pass | `GetBooking_ApiTokenWithoutStaffToken_ReturnsUnauthorized`, `GetBooking_HousekeepingWithoutBookingsView_ReturnsForbidden` and `GetBooking_ReceptionistWithBookingsView_ReturnsOk` in `BookingReadProtectionTests`. Of the 17 tests of the command, 10 failed on the code of main 1c2726b (`before-tests.txt`) and 17 pass on the branch (`after-tests.txt`), [RC-341]. Green in `NF Hotel CI` on #651 |
| 2 | A lookup with the right number and email, in any letter case, gets 200 and a body without name, email, phone or passport number; a wrong email, an unknown number and an unreadable number get the same 404 body; the sixth failed lookup for one number within 15 minutes gets 429; creating a booking and listing free rooms still work without a staff token | Pass | The `LookUpReceipt_*` tests and `AvailableRooms_ApiTokenWithoutStaffToken_StillReturnsOk` in `BookingReadProtectionTests` ([RC-341]), green on #651. On the live site the test booking was created through the public page, and it opened with its own email and not with another |
| 3 | The receipt page without the email shows no booking, also in the first HTML that the server sends; with the right email it shows the card; right after a booking the guest sees the receipt; the tests of the view models pass | Pass | `BookingPageTests`, `BookingFlowViewModelTests` and `BookingReceiptTests`, and the browser tests `GuestBookingTests`, green on #651 ([RC-341]). On the live site the first HTML of `/booking/FLZ-000001` holds the form and no booking (`after-receipt.txt`), and the test booking ended on its receipt step (`after-test-booking-receipt-step.png`) |
| 4 | On the live site after the deploy, `/booking/FLZ-000001` without the email shows no booking data, and a booking that S01 makes for the test opens with its number and email; the screenshots and transcripts of the page before the fix and after the deploy are in the evidence folder | Pass | `after-receipt.txt`, `after-count.txt`, `after-FLZ-000001.png` and `after-FLZ-999999.png`. The test booking FLZ-158424 opened with its number and email and not with another email (`after-test-booking-*.png`, `after-live-check.txt`). The AI made it at S01's request in chat (first finding). The `before-*` files of #651 are in the same folder |
| 5 | The documents say what the deliverable says, the code review against `QC-CS-001` is Go, and every required check of the pull request passes | Pass | [RC-341] is Go. BR-C-06, the test plan and the wiki pages Security-Controls and Public-Access changed in #651. All 44 required checks of #651 passed on 8c93d2c. F4 of RC-341 (UC-004 and ADR-0005) lies outside the deliverable and stays an action item |

## Findings

- **The AI made the test booking, at S01's request.** Criterion 4 names a booking that S01 makes. S01 chose in chat to
  let the AI make it in Docker with S01's email ("Ja, lav den"). So the booking is S01's decision, but S01 did not
  type it. The check is the same: the booking went through the public booking page of the live site after the
  deploy.
- **The test booking stays in the database.** FLZ-158424 is a pending booking of a single room for 10 to 11 June 2030,
  named "Test MIL-079". This gate leaves it in place, so the evidence points at a booking that exists.
- **The numbers run high.** The test booking got number 158424, and the numbers are the ids of the bookings. Before
  MIL-079, a count from FLZ-000001 could try every number up to that one. Now each number needs the email of its own
  booking.
- **The milestone is not on GitHub.** MIL-079 was never published, so there is no GitHub milestone or issue to close.

## Overall Verdict

Go. The five criteria pass on the evidence of 2026-11-12. S01 gave the acceptance to the AI in chat on 2026-11-14 ("Du godkender for mig"), and the AI accepted this record and the gate row of MIL-079 as stand-in for S01; the gate of MIL-079 has passed.
MIL-079 is not published to GitHub, so no milestone or issue closes. The reviewer is not independent: the AI wrote the
plan, the code and this record.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept this record and the gate row of MIL-079, or return them | S01; done on 2026-11-14 by the AI as stand-in, at S01's word in chat | At the review of the gate pull request |
| Decide whether to publish MIL-079 to GitHub (`sync-project.sh --apply --milestone MIL-079`) | Decided 2026-11-14 in the plan accepted at S01's word: not published, because it is done; only milestones with open work are published | Closed |
| Bring the note in UC-004 and the list of public calls in ADR-0005 in line with the lookup by number and email (F4 of RC-341) | MIL-018, with the ADR of its task 2 | 2026-12-11 |
| Decline or remove the test booking FLZ-158424 when the evidence no longer needs it | S01 | Open |
| Resolve the code-quality review thread on #651; the fix is in 8c93d2c | S01 | Open |

---

[MIL-079]: ../../milestones/mil-079-a-booking-opened-only-with-its-email.md
[RC-340]: ./rc-340-mil-079.md
[RC-341]: ./rc-341-mil-079-code.md
[ADR-0038]: ../../adr/adr-0038-approval-before-a-deploy.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[fc88265]: https://github.com/NF-Hotel/NFHotel/commit/fc88265603804b527f97224c87d91dca53e09e69
