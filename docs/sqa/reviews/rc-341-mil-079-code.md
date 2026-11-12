# RC-341 Review of the code of MIL-079 tasks 1 to 4

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-341 |
| CrossReference | [MIL-079], [QC-CS-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-12 | Proposed | mikemike | S01 | Initial version; pre-review by the AI, not independent; S01 reviews the pull request | [127a7df] |

---

## Artifact Under Review

- Instance reviewed: the code of [MIL-079] tasks 1 to 4 on the branch `mil-079-booking-read-only-with-its-email`:
  - the API and the use case: `Bookings.View` on `GetByIdAsync` in `BookingsController` and in `BookingService`; the
    lookup `LookUpReceiptAsync` in `IBookingService`, `BookingService` (with `IsSameEmail` and the throttle key per
    booking), `BookingsController` (`POST api/bookings/public/receipt`) and `ApiBookingService`; the new records
    `BookingReceiptDto` and `BookingReceiptRequest`; the codes `booking.receipt_email_required` and
    `booking.too_many_lookups`, the second answered with 429 by `ResultResponses`;
  - the throttle: `InMemoryLoginThrottle` sweeps the windows that have run out every 256 failures, and shows its count
    in `TrackedKeyCount`;
  - the web: the lookup page `BookingReceipt.razor` (`/booking` and `/booking/{number}`) with
    `BookingReceiptViewModel`; `BookingReceiptCard`, which takes the receipt only; the booking page `Book.razor`, which
    keeps the receipt as step 4 instead of moving on to `/booking/{number}`, and `BookingFlowViewModel`, which keeps
    only the receipt; two texts in `ErrorMessages`; a footer link in `PublicLayout.razor`;
  - the tests: `BookingReadProtectionTests` (new), `EndpointProtectionTests`, `ContractParityTests`, `ApiFactory`,
    `ServiceRequestTests`, `BookingServiceTests`, `InMemoryLoginThrottleTests`, `BookingPriceTests`, `ServiceWorld`,
    `BookingPageTests`, `BookingFlowViewModelTests` and the browser tests `GuestBookingTests`
- Checklist used: [QC-CS-001] (`qc-programming-csharp`)
- Scope: full review of the changed files and the new tests
- Language and domain: n/a (technical type)
- Language reviewer: none
- Mode: supervised (the AI wrote the code and this pre-review; S01 reads the pull request)

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Casing | Pass | The solution builds with 0 warnings and 0 errors |
| 2 | `Async` suffix, one type per file | Pass | `LookUpReceiptAsync`, `LookUpAsync`; the two new records have a file each |
| 3 | Formatter, no suppressions, explicit local types | Pass | `dotnet format NFHotel.sln --verify-no-changes` reports only the `IDE0005` in `BookingCalendarViewModelTests.cs`, a file this change does not touch; no suppression is added. The new and changed lines under `src/` declare their types (no `var`); the tests keep `var` as the test projects do ([RC-311]) |
| 4 | Nullable references, `!` justified | Pass | `BookingReceiptRequest.Email` is `string?` on purpose: MVC refuses a null in a non-nullable string with its own 400, so a missing email would get another answer through the API than from the service; nullable, it reaches the use case on both paths, and the parity test shows the same code. No `!` in `src/`; the tests use `null!` only to check the argument guards |
| 5 | Disposal | N-A | No new disposable object |
| 6 | Async throughout | Pass | `await` throughout, `ConfigureAwait(false)` as the files do; no `.Result`, `.Wait()` or `async void` |
| 7 | Specific exceptions, arguments validated | Pass | `ArgumentNullException` for a missing request, in the use case and the client before any call. A key below one is a not-found in the lookup, because there it is a guess, not a programming error; in `GetByIdAsync` it stays an `ArgumentOutOfRangeException`. No new `catch` |
| 8 | XML documentation | Pass | Every new public type and member; the remarks say why the email, why the same answer, why the sweep |
| 9 | Properties, immutability | Pass | Records; the view model has private setters for its state |
| 10 | `decimal` for money, `DateTimeOffset` for points in time | Pass | No money; the receipt has dates (`DateOnly`); the throttle keeps `DateTimeOffset` |
| 11 | `CancellationToken` | Pass | `LookUpReceiptAsync` takes and passes it; the page passes its component token |
| 12 | Tests | Pass | Named `Method_Condition_Expected`; a fake clock and the in-memory world; no sleep and no network. Domain 485, Application 585, ApiClient 105, Api 230 and Web 783 tests pass, and the 8 throttle tests ([after-tests-all.txt]). Of the 17 API tests of task 1, 10 failed on the code before the fix ([before-tests.txt]); all 17 pass after it ([after-tests.txt]). The browser tests and the PostgreSQL integration tests run in the CI of the pull request, in containers |
| 13 | Trace to the DCD | Pass | No class of the domain design changes; the new records and the use case are of the application layer, which the class diagram does not draw |
| 14 | One place for each piece of knowledge | Pass | The receipt fields in `BookingReceiptDto.From` only; the email comparison in `IsSameEmail` only; the limit of 5 failures in 15 minutes comes from `StaffSecurityOptions`, the throttle of [ADR-0005], not a second constant; the texts in `ErrorMessages` |
| 15 | References point inward | Pass | The use case depends on the port `ILoginThrottle`; Infrastructure implements it; the web knows only the use case interface |
| 16 | SOLID | Pass | `BookingService` gains one more read of the same aggregate; the throttle is injected; the card takes a narrow record that has no guest fields at all |
| 17 | Layers and folders | Pass | The records in `Application/Dto/Bookings`, the page in `Web/Features/Booking`, the tests beside the tests of the same classes |
| 18 | File names | Pass | |
| 19 | Observer pattern | N-A | |
| 20 | SIMD and CUDA | N-A | |
| 21 | Regions in types of `src/` | Pass, on condition | No file under `src/` has regions; the changed files keep their layout, and the risk exception of [RC-278] for this criterion runs to 2026-12-31 |

## Findings of the review

- **F1, fixed: the throttle kept every window until its key came back.** `InMemoryLoginThrottle` dropped a window only
  when the same key was checked again after the window. For staff sign-in that is bounded by the names people type;
  with the lookup, a script that tries many numbers would leave one entry per number for as long as the API runs. The
  throttle now sweeps the windows that have run out every 256 failures, so the memory is bounded by the failures of
  one window. Two tests: 1,000 keys of an old window are swept, and a block inside its window survives a sweep.
- **F2, accepted: a guesser can block the lookup of one number.** Five wrong emails for a number block its lookup for
  15 minutes, also for its own guest: the key is the booking, because the API cannot see the guest's address (the web
  application calls it). The guest still sees the receipt right after the booking and can ask reception. The wiki
  page Security Controls says so under Rate limits.
- **F3, accepted: the comparison time shows whether the lengths differ.** `CryptographicOperations.FixedTimeEquals`
  returns at once for inputs of different length. The difference is nanoseconds behind a network round trip and a
  database read, and the throttle stops the guessing of an email after five tries.
- **F4, not in this change: two older sentences.** UC-004 says that the receipt page is open by its number until the
  guest sign-in, and [ADR-0005] lists the read of a booking by its number among the public calls. The ADR records the
  decision of its day and MIL-079 records the change; the note in UC-004 belongs with the guest sign-in of MIL-018.

## Overall Verdict

Go: every Mandatory criterion passes, criterion 21 on the condition of the risk exception of [RC-278]. Criterion 5 of
[MIL-079] also needs every required check of the pull request green; the browser tests that this change rewrites run
there. Left for task 5: the check on the live site after S01's merge and approval of the CD run.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Review and accept the code with the pull request | S01 | with the pull request |
| Update the note in UC-004 with the guest sign-in | S01 | MIL-018 |

---

[MIL-079]: ../../milestones/mil-079-a-booking-opened-only-with-its-email.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[ADR-0005]: ../../adr/adr-0005-backoffice-login.md
[RC-278]: ./rc-278-mil-058-code.md
[RC-311]: ./rc-311-mil-067-code.md
[before-tests.txt]: ../evidence/mil-079/before-tests.txt
[after-tests.txt]: ../evidence/mil-079/after-tests.txt
[after-tests-all.txt]: ../evidence/mil-079/after-tests-all.txt
[127a7df]: https://github.com/NF-Hotel/NFHotel/commit/127a7df34b573e8f8efca3b39e010b236516334f
