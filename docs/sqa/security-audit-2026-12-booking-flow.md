# SAR-002 Security audit of the booking flow against ASVS 5.0 level 2, November 2026

## Metadata
| Key | Value |
| --- | --- |
| ID | SAR-002 |
| CrossReference | [MIL-089], [MIL-085], [MIL-086], [MIL-087], [MIL-088], [ADR-0047], [ADR-0049], [ADR-0050], [ADR-0051], [ADR-0052], [ADR-0053], [ADR-0004], [ADR-0005], [ADR-0013], [ADR-0014], [ADR-0015], [ADR-0028], [ADR-0029], [SAR-001] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-21 | Deprecated | mikemike | AI as stand-in for S01 | Initial version: the security audit of the booking flow, [MIL-089] task 1 (RC-438)<br>Reworked after the No-Go of RC-438: the scale of severity (the OWASP Risk Rating, a stand-in choice), BF-02 fixed for one address with case 11 and its tests, 3.4.3 open in BF-12 and 15.1.3 in BF-02, 6.5.5 and 6.6.3 on decisions 2 and 10 of [ADR-0052], BF-12 with four observations, four statements corrected<br>After the delta review of RC-438: BF-01 High while the verification was off and Medium since it went on in production on 2026-11-22, cases 4 and 5 live, the connections of nginx in the remaining risks, decision 10 of [ADR-0052] among the actions | [00d609b] |
| 2026-11-27 | Accepted | mikemike | AI as stand-in for S01 | Accepted at the gate of [MIL-089] (RC-441), with C1, C2 and C4 of RC-438 closed<br>Requirement 3.4.6: the antiforgery of ASP.NET Core writes X-Frame-Options SAMEORIGIN on the answer that sets a new antiforgery cookie, as the check on the live site showed; the verdict is unchanged | pending |

## 1. Scope and method

**What was audited.** The booking flow as it stands after [MIL-085] to [MIL-088], on main at `3cfe3ab` with the changes
of [MIL-089]:

- the public pages of the web application that a guest uses to book: the search and the booking page (`/book`), the
  page of the verification link (`/booking/verify`), the guest sign-in and the page of the own booking;
- the routes of the API that those pages call: the search for free rooms, the public booking and the booking request,
  the verification of a link, the guest sign-in and the three guest routes;
- the mail of the verification and the relay settings ([ADR-0052], [ADR-0053]);
- the limits and the caller address ([ADR-0051]), the reference ([ADR-0049]), the input rules ([ADR-0050]) and the
  guest token ([ADR-0047]);
- the nginx listener in front of the pages, the security events of the flow and their alert rules.

The back office, the routes of the mobile app, the benchmark and AI services, the monitoring stack and the host lie
outside the flow. They appear here only where a requirement or one of findings 3 to 6 of [MIL-089] reaches them.

**Against what.** OWASP ASVS 5.0 at level 2 (the requirements of levels 1 and 2), as [MIL-089] decision 1 sets out:
chapters V1, V2, V4, V8, V15 and V16 in full, and of V3, V6, V7, V11 and V14 the requirements that the flow touches:
179 requirements. Each has one verdict:

- **met**: a test that fails without the control, or a file and line of this commit, shows it;
- **n/a**: the flow has nothing the requirement is about, with the reason;
- **accepted**: a gap that an accepted decision record takes on, with the record;
- **open**: a gap with a finding of section 8.

The report cites a requirement by its ID and gives the names of the chapters and sections; the text of each requirement
is in ASVS 5.0 itself.

**How.** Five helper drafts read the code chapter by chapter (groups A to D) and the abuse cases with findings 3 to 6
(group E). Every verdict was then read again against the code. A script checked every citation of the report against
the files of this commit: each path and line exists, and each test `Project/Class.Method` is declared in its project
(448 citations, 0 failed). The script that builds the tables compared every note with the text of its requirement and
found no run of seven or more words in common.

**Not independent.** The AI that built [MIL-085] to [MIL-088] wrote this audit, in autonomous mode, as stand-in for
S01 (`Mode: autonomous, not independent`). So the evidence that carries the conclusion comes from what does not depend
on the reviewer's judgement: the scans of section 5, the load test of section 6, and the tests that fail when their
control is taken out (section 4).

**What was fixed here.** [MIL-089] decision 3: a defect of the booking flow is fixed in this milestone with its test,
when the fix stays inside the code, the documents or the monitoring of the flow and changes no accepted decision
record. A gap against ASVS that no decision asked for is read as a finding, not as a defect, even where its fix would
stay inside the flow, such as the headers of the answers of nginx and the charsets (BF-12) or the check of the origin
of `/_blazor` (BF-09). The one exception is a finding of severity High in the flow, which criterion 1 of [MIL-089]
does not allow open: BF-02, fixed here for one address. BF-01 was High too while the verification by email was off;
S01 turned it on in production on 2026-11-22, with the relay of [MIL-088], which brings BF-01 to Medium (section 8).
Two decision records changed, as stand-in decisions that S01 can overrule: [ADR-0051] decisions 6 and 8, and
[ADR-0052] decisions 2 and 10 (section 9). Everything else is a finding for S01 with its severity, owner and issue
(section 8), and is not built here.

**Severity.** Each finding is rated by the OWASP Risk Rating Methodology. Its likelihood and its impact are each Low,
Medium or High, and the pair gives the severity: High and High is Critical; High and Medium, in either order, is
High; High and Low, in either order, or Medium and Medium, is Medium; Medium and Low, in either order, is Low; Low and
Low is Informational (the Note of the methodology). The likelihood weighs how easily an outsider finds and uses the
gap on the site as it runs today, and whether an alert sees it; the impact weighs what it costs the hotel and its
guests, in data and in availability, and how soon it is undone. Section 8 gives both factors of each finding, and its
issue says why. [SAR-001] named the levels without a method; this scale is the choice of this audit, as stand-in for
S01, who can overrule it.

## 2. Conclusion

- No open finding of severity Critical or High, inside or outside the booking flow. BF-02, many slow requests, was
  High as the flow stood (likelihood High, impact Medium); this milestone fixes it for one address, and what remains
  is Medium (section 9). BF-01, the rooms held by one sender, was High while the verification by email was off
  (likelihood High, impact Medium); since S01 turned it on on 2026-11-22, every booking needs a mailbox that gets
  its link, and BF-01 is Medium.
- Of the 179 requirements: 92 met, 49 n/a, 6 accepted and 32 open. The 32 open ones lead to eleven findings (BF-01,
  BF-02, BF-04 to BF-12); BF-03 comes from finding 4 of [MIL-089].
- Twelve findings in all, rated by the scale of section 1: five Medium (BF-01 to BF-05, of which BF-03 and BF-05 lie
  outside the flow), six Low (BF-06 to BF-11) and one Informational (BF-12). Each has an issue and the owner S01.
- Of the eleven abuse cases, nine are fully controlled and two partly (3, taking the rooms; 11, many slow requests,
  now held for one address). Each names tests that fail without its control. Cases 4 and 5 are live in production
  since 2026-11-22, when S01 turned the verification of a booking by email on.
- The largest risk in production today is BF-01: one home connection, an IPv4 address and an IPv6 network of 64
  bits, may make 10 public bookings an hour and 40 a day, while the alert `PublicBookingSurge` reports only more
  than 10 an hour and the back office does not mark public bookings. Since the verification is on, every three of
  them need a mailbox whose link a script opens.
- The scans on the commit of [MIL-085] to [MIL-088] report no High alert; the five alerts of the code scan on the
  security events close through the code of this milestone.
- The load test with the booking flow keeps its thresholds with the verification off and on, and a burst over the
  limits gets only refusals of the limits.

## 3. Verdicts per requirement

Columns: the requirement, its ASVS level, the verdict (with the finding when open), the evidence (a test as
`Project/Class.Method`, or a file and line of this commit), and a note.

### V1 Encoding and Sanitization

Sections: 1.1 Encoding and Sanitization Architecture; 1.2 Injection Prevention; 1.3 Sanitization; 1.4 Memory, String, and Unmanaged Code; 1.5 Safe Deserialization.

| Requirement | Level | Verdict | Evidence | Note |
| --- | --- | --- | --- | --- |
| 1.1.1 | 2 | met | `NFHotel.Domain.Tests/GuestValidationTests.Create_TextsWithWhiteSpaceAndDecomposedLetters_StoresTheNormalForm`; `src/NFHotel.Domain/Entities/Guest.cs:153` | ASP.NET Core decodes JSON, query and form values once, and Guest.Apply trims each text and brings it to normal form C before EntityValidation checks it (Guest.cs:153-163). No code in the flow decodes or unescapes a value a second time. |
| 1.1.2 | 2 | met | `src/NFHotel.Infrastructure/Mail/SmtpMailSender.cs:201`; `NFHotel.Web.Tests/BookingPageTests.Details_HostileStrings_StayTextInTheFields`; `NFHotel.Infrastructure.Tests/GuestAndRoomReadTests.Guest_HostileStringsThatMeetTheRules_AreStoredAndReadUnchanged` | Values are stored as typed and encoded only by the interpreter that uses them: Razor when it renders, MimeKit when SmtpMailSender.Build makes the mail, and EF Core as query parameters. |
| 1.2.1 | 1 | met | `NFHotel.Web.Tests/BookingPageTests.Details_HostileStrings_StayTextInTheFields` | The booking pages write guest values only through Razor, which encodes them for the element or attribute, and no page under Features/Booking uses MarkupString. The mail headers hold only the checked To address and a fixed subject. |
| 1.2.2 | 1 | met | `src/NFHotel.Application/UseCases/Bookings/BookingMails.cs:41`; `NFHotel.Infrastructure.Tests/MailSettingsTests.AddInfrastructure_VerificationOnWithAnHttpSiteAddress_StopsTheStart` | The only URL built with request data is the verification link: the site address from the settings, which must be https, and a base64url token in the fragment (BookingMails.LinkOf). The web application sends dates and room sizes to the API as typed values in the invariant format. |
| 1.2.3 | 1 | met | `src/NFHotel.Web/wwwroot/js/booking-verify.js:37`; `NFHotel.Web.Tests/SecurityHeadersTests.ContentSecurityPolicy_Directives_AllowNoInlineCodeOrForeignSource` | No script is built from data: booking-verify.js sets the token as the value of a form field, and JSON is written by System.Text.Json. The CSP allows scripts only from the site itself. |
| 1.2.4 | 1 | met | `NFHotel.Infrastructure.Tests/GuestAndRoomReadTests.Guest_HostileStringsThatMeetTheRules_AreStoredAndReadUnchanged`; `src/NFHotel.Infrastructure/Repositories/BookingRequestRepository.cs:49`; `src/NFHotel.Infrastructure/Persistence/UnitOfWork.cs:103` | The flow queries through EF Core LINQ, and its two raw statements use FromSql and ExecuteSqlAsync with interpolated values, which EF Core sends as parameters. |
| 1.2.5 | 1 | n/a | no `Process.Start`, `ProcessStartInfo` in `src` (`git grep`) | The flow starts no operating system process. |
| 1.2.6 | 2 | n/a | no `ldap` in `src` (`git grep`) | The flow builds no LDAP query and uses no directory service. |
| 1.2.7 | 2 | n/a | no `XPath`, `XmlDocument`, `XmlReader`, `XDocument` in `src` (`git grep`) | The flow builds no XPath query and reads no XML. |
| 1.2.8 | 2 | n/a | no `latex` in `src` (`git grep`) | The flow runs no LaTeX processor. |
| 1.2.9 | 2 | met | `NFHotel.Infrastructure.Tests/BookingPublicCountTests.CountComingPublicBookingsAsync_EmailWithAPatternCharacter_MatchesOnlyItself`; `src/NFHotel.Infrastructure/Repositories/SqlPattern.cs:29` | No regular expression is built from input. The one pattern built from input, the ILIKE match on the email, escapes backslash, percent and underscore first (SqlPattern.Exactly). |
| 1.3.1 | 1 | n/a | `src/NFHotel.Domain/Attributes/PersonNameAttribute.cs:59`; `src/NFHotel.Domain/Attributes/EmailFormatAttribute.cs:50` | The flow takes no HTML: it has no rich text field, and the name and email rules refuse angle brackets. |
| 1.3.2 | 1 | met | `NFHotel.Web.Tests/SecurityHeadersTests.ContentSecurityPolicy_Directives_AllowNoInlineCodeOrForeignSource`; `src/NFHotel.Web/Security/SecurityHeaders.cs:18` | The flow uses no eval, expression language or other dynamic code; booking-verify.js is its only own script, and the CSP allows neither unsafe-eval nor inline script. |
| 1.3.3 | 2 | met | `NFHotel.Api.Tests/SecurityEventValuesTests.Events_EveryLogMessage_TakesNoEmail`; `NFHotel.Api.Tests/BookingInputApiTests.Refusal_OfAField_WritesOneSecurityEventWithoutTheValue` | Since MIL-089 the security events write no typed value at all: no event has a field for an email, and the refusal of a field names the field, not its value. The email passes its allowlist and the one-line guard before it reaches the mail relay, and the country must be on a fixed list. |
| 1.3.4 | 2 | n/a | - | The flow takes no SVG and no file upload. |
| 1.3.5 | 2 | n/a | `src/NFHotel.Web/Features/Admin/Shared/MarkdownConverter.cs:20` | The flow takes no Markdown, CSS, XSL or BBCode from a guest. Outside the booking flow: the staff pages render Markdown with Markdig and DisableHtml. |
| 1.3.6 | 2 | n/a | `src/NFHotel.Infrastructure/Config/MailSettings.cs:179` | The flow calls no address taken from a request: the API address, the mail relay and the site address of the link are settings. The guest's email is only the recipient handed to the fixed relay. |
| 1.3.7 | 2 | met | `NFHotel.Application.Tests/BookingRequestServiceTests.AskAsync_Mail_HoldsTheStayAndTheLinkAndNoTypedText`; `src/NFHotel.Application/UseCases/Bookings/BookingMails.cs:54` | The mail texts are fixed strings in BookingMails filled with FormattableString.Invariant from dates, numbers and the link, never from typed text, and the Razor pages are compiled, so no template is built from input. |
| 1.3.8 | 2 | n/a | - | The flow runs on .NET and makes no JNDI lookup. |
| 1.3.9 | 2 | n/a | no `memcache`, `IDistributedCache`, `Redis` in `src` (`git grep`) | The flow sends nothing to memcache or another cache server; the limit counts live in the memory of the API (InMemorySenderLimits). |
| 1.3.10 | 2 | met | `NFHotel.Api.Tests/BookingInputApiTests.GuestForm_HostileString_IsRefusedByTheFieldOrStoredUnchanged`; `src/NFHotel.Api/Security/SecurityEvents.cs:133`; `src/NFHotel.Infrastructure/Logging/LoggerWarningLog.cs:38` | No typed value is used as a format string: logs use fixed LoggerMessage templates with values as arguments, and the hostile-input test sends %s%s%n and {0} and gets a refusal or the value stored unchanged. |
| 1.3.11 | 2 | met | `NFHotel.Infrastructure.Tests/SmtpMailSenderTests.SendAsync_LineBreakInTheAddressOrTheSubject_IsRefusedBeforeAnyConnection`; `NFHotel.Application.Tests/BookingRequestServiceTests.AskAsync_LineBreakInTheEmail_IsRefusedBeforeAnyMail` | Only the To address comes from the guest; it must pass EmailFormatAttribute, and SmtpMailSender.GuardOneLine refuses a line break in the address or the subject before MimeKit builds the message. |
| 1.4.1 | 2 | met | no `unsafe`, `stackalloc`, `Marshal`, `DllImport`, `AllowUnsafeBlocks` in `src` and `Directory.Build.props` (`git grep`) | The flow is managed C# with bounds-checked strings and arrays, and no project has unsafe code, stackalloc, Marshal or DllImport. |
| 1.4.2 | 2 | met | `NFHotel.Api.Tests/HostileInputTests.EveryEndpoint_HostileInput_IsRefusedWithoutAFaultOrALeak`; `NFHotel.Domain.Tests/StayLimitTests.CheckPublicParty_Guests_AcceptsOneToFour`; `src/NFHotel.Api/Security/InputRules.cs:80` | Numbers and dates are range-checked before use: a party of 1 to 4 (Booking.CheckPublicParty), dates from 1900 to 2199 (InputRules.IsAcceptedDate), and a number beyond Int32 is refused when the body is read. |
| 1.4.3 | 2 | met | `src/NFHotel.Infrastructure/Mail/SmtpMailSender.cs:101`; `src/NFHotel.Infrastructure/Persistence/UnitOfWork.cs:100` | Memory is managed by the .NET garbage collector, and the flow releases its resources with using: the SMTP client and its timeout source (SmtpMailSender.cs:101-103) and the database transaction (UnitOfWork.cs:100). |
| 1.5.1 | 1 | n/a | no `XmlDocument`, `XmlReader`, `XDocument`, `XmlSerializer`, `DataContractSerializer` in `src` (`git grep`) | The flow parses no XML: requests are JSON or form posts. |
| 1.5.2 | 2 | met | `NFHotel.Api.Tests/BookingInputApiTests.PublicBooking_UnknownField_IsRefused`; `NFHotel.Api.Tests/ValidationRulesTableTests.FlowTypes_WhenInspected_RefuseUnknownFieldsAndBoundTheirLists` | Request bodies are read by System.Text.Json into fixed record types that refuse unknown fields (JsonUnmappedMemberHandling.Disallow); there is no type name handling, polymorphic type or BinaryFormatter. |

### V2 Validation and Business Logic

Sections: 2.1 Validation and Business Logic Documentation; 2.2 Input Validation; 2.3 Business Logic Security; 2.4 Anti-automation.

| Requirement | Level | Verdict | Evidence | Note |
| --- | --- | --- | --- | --- |
| 2.1.1 | 1 | met | `NFHotel.Api.Tests/ValidationRulesTableTests.Table_EveryFieldOfTheFlow_HasARow`; `docs/refactor/BusinessRules.md:248` | BusinessRules.md has a validation table with one row per field of the flow (length, characters, format), and a test fails when a field of a flow type has no row. |
| 2.1.2 | 2 | met | `docs/refactor/BusinessRules.md:277` | The validation table has a column for joined rules, such as departure after arrival, at most 30 nights on the public site and a room that sleeps the party. |
| 2.1.3 | 2 | met | `NFHotel.Api.Tests/LimitsTableTests.Table_Numbers_AreTheDefaultsOfTheirSettings`; `docs/refactor/BusinessRules.md:328` | The limits table names each limit with its scope (caller address, email, mailbox, reference) and the global fuse of 400 mails a day, and a test ties each number to the default of its setting. |
| 2.2.1 | 1 | met | `docs/refactor/BusinessRules.md:292`; `src/NFHotel.Api/Security/InputRules.cs:34`; `NFHotel.Api.Tests/ValidationRulesTableTests.Table_EveryRow_FillsEveryColumnAndGivesATextItsLength` | Each guest field has an allowlist rule in a domain attribute, and InputGuardFilter checks every text, enum and date. The email of the guest sign-in is only compared, so it has no length of its own beyond the bound of 1,000 characters of every text; the ledger said 1 to 100 characters and now says what the code does (fixed in MIL-089). |
| 2.2.2 | 1 | met | `NFHotel.Api.Tests/BookingInputApiTests.PublicBooking_GuestField_IsAcceptedOrRefusedWithItsCode`; `src/NFHotel.Application/UseCases/Bookings/BookingService.cs:467` | The API applies every rule itself (InputGuardFilter, PlanPublicAsync, Guest.Apply), and the tests call it without the page; the maxlength of the page and its local ValidateFields only help the guest. |
| 2.2.3 | 2 | met | `NFHotel.Application.Tests/BookingServiceTests.CreatePublicAsync_RoomType_BooksTheSmallestFreeRoomThatSleepsTheParty`; `NFHotel.Api.Tests/BookingInputApiTests.Nights_PublicThirtyAndStaffThirtyOne_AreBookedAndPublicThirtyOneIsRefused`; `NFHotel.Api.Tests/BookingInputApiTests.Departure_FiveYearsAheadAndADayLater_IsBookedOrRefused` | The API checks the joined rules itself: departure after arrival and at most five years ahead (Booking.CheckPeriod), at most 30 nights on the public site, and only rooms that sleep the party (BookingService.RoomsInOrder). |
| 2.3.1 | 1 | met | `NFHotel.Api.Tests/BookingRequestApiTests.PublicBooking_VerificationOn_IsRefusedAndStoresNothing`; `NFHotel.Api.Tests/BookingRequestApiTests.Verify_SameTokenTwice_SecondGetsTheAnswerOfALinkThatDoesNotWork` | With the verification on, the direct booking route answers verification_required and stores nothing, so a booking needs a request and then its link, and a link works once. The API picks the room and the price itself, so no step can be skipped by sending them. |
| 2.3.2 | 2 | met | `NFHotel.Api.Tests/LimitsTableTests.Table_Numbers_AreTheDefaultsOfTheirSettings`; `NFHotel.Api.Tests/SenderLimitApiTests.Booking_SixthInAnHour_IsRefusedWithTheCodeAndTheTimeToWait`; `NFHotel.Application.Tests/BookingRequestServiceTests.AskAsync_FourthMailInAnHour_IsRefusedAndKeepsTheThirdRequest`; `NFHotel.Api.Tests/GuestAccessTests.SignIn_SixthTryForOneReference_ReturnsTooManyRequests` | Each documented limit has a test at its edge: searches, booking requests an hour and a day, coming bookings per email, mails per mailbox, the fuse and sign-in tries. |
| 2.3.3 | 2 | accepted | `src/NFHotel.Application/UseCases/Bookings/BookingRequestService.cs:191`; `src/NFHotel.Infrastructure/Repositories/BookingRequestRepository.cs:49` | The guest and the booking are saved in one transaction, but the claim deletes the request in its own statement before it, so a fault in the booking step leaves neither the request nor a booking. Accepted by ADR-0052 decision 5: the claim is what makes a link work once; the guest asks again. |
| 2.3.4 | 2 | met | `NFHotel.Infrastructure.Tests/BookingVerificationRaceTests.VerifyAsync_TwentyAtOnceForTheLastRoom_OneBooksAndNineteenAreToldTheRoomIsGone`; `NFHotel.Infrastructure.Tests/BookingVerificationRaceTests.CreatePublicAsync_TwentyAtOnceForTheLastRoomWithTheVerificationOff_OneBooksAndNineteenAreToldTheRoomIsGone`; `NFHotel.Infrastructure.Tests/ConflictPathTests.SaveChangesAsync_TwoParallelCreationsOfTheSamePeriod_ExactlyOneSucceeds` | Public bookings are planned and saved under the advisory lock public_booking, and the exclusion constraint ex_booking_room_period refuses a second booking of a room for an overlapping period. |
| 2.4.1 | 2 | open, BF-01 and BF-02 | `src/NFHotel.Infrastructure/Config/SenderLimitOptions.cs:20`; `ops/deploy/monitoring/prometheus/rules/security.yml:40`; `ops/deploy/monitoring/prometheus/rules/security.yml:90`; `src/NFHotel.Web/Security/CircuitLimit.cs:22` | Limits count per caller address and per mailbox, so a few addresses can hold many rooms with public bookings, or use up the fuse of 400 mails a day, which MailVolumeHigh reports at 200 mails. Since MIL-089 one address holds at most 50 circuits of the pages, but many addresses can still hold them (BF-01 and BF-02). |

### V3 Web Frontend Security

Sections: 3.2 Unintended Content Interpretation; 3.3 Cookie Setup; 3.4 Browser Security Mechanism Headers; 3.5 Browser Origin Separation; 3.7 Other Browser Security Considerations.

| Requirement | Level | Verdict | Evidence | Note |
| --- | --- | --- | --- | --- |
| 3.2.1 | 1 | met | `ops/tests/test-nginx.sh:150`; `NFHotel.Api.Tests/ApiSecurityHeadersTests.Get_AnyAnswer_TellsTheBrowserNotToSniff`; `src/NFHotel.Api/Security/ApiSecurityHeaders.cs:29` | A browser cannot request the API routes of the flow: nginx passes only the two mobile routes (snippets/mobile-api.conf) and answers 404 to any other /mobile-api/ path, and every API answer is JSON with nosniff. The flow takes no upload and offers no download, and its pages are HTML meant to be rendered. |
| 3.2.2 | 1 | met | `NFHotel.Web.Tests/BookingPageTests.Details_HostileStrings_StayTextInTheFields`; `src/NFHotel.Web/wwwroot/js/booking-verify.js:37` | Razor writes every value of the booking pages as encoded text, and no booking page uses MarkupString (only the admin charts and MarkdownText do). The one script of the flow sets field.value and hidden, never innerHTML. |
| 3.3.1 | 1 | open, BF-09 | `src/NFHotel.Web/Security/GuestClaims.cs:21`; `src/NFHotel.Web/Security/AntiforgeryCookie.cs:18`; `NFHotel.Web.Tests/GuestPagesTests.SignIn_BehindTheProxy_SetsASecureHttpOnlyGuestCookie` | Both cookies of the flow are Secure behind the proxy (Tls__Require=true), but neither name has a prefix: the guest cookie is nfhotel.guest and the antiforgery cookie keeps the default name .AspNetCore.Antiforgery.&lt;suffix&gt; (BF-09). |
| 3.3.2 | 2 | met | `NFHotel.Web.Tests/GuestPagesTests.SignIn_RightReferenceAndEmail_SetsTheGuestCookieAndGoesToTheOwnBooking`; `src/NFHotel.Web/Security/GuestSignInRegistration.cs:63` | The guest cookie is SameSite=Lax and goes only to /my-booking, whose posts also need the antiforgery token; the antiforgery cookie keeps the framework default SameSite=Strict. |
| 3.3.3 | 2 | open, BF-09 | `src/NFHotel.Web/Security/GuestSignInRegistration.cs:61`; `src/NFHotel.Web/Security/GuestSignInRegistration.cs:67`; `src/NFHotel.Web/Security/AntiforgeryCookie.cs:18` | No cookie of the flow uses the __Host- prefix, and none is meant for another host. The guest cookie's Path=/my-booking also rules that prefix out, because it needs Path=/ (BF-09). |
| 3.3.4 | 2 | met | `NFHotel.Web.Tests/GuestPagesTests.SignIn_RightReferenceAndEmail_SetsTheGuestCookieAndGoesToTheOwnBooking`; `NFHotel.Web.Tests/HttpsRequirementTests.LoginPage_BehindTheProxy_SetsASecureAntiforgeryCookie`; `src/NFHotel.Web/Security/GuestSessionTickets.cs:17` | Both cookies are HttpOnly, and the guest token reaches the browser only inside the protected guest cookie (the test checks that the cookie does not show it). The booking page hands the browser a one-time ticket for /my-booking/start, never the token. |
| 3.4.1 | 1 | open, BF-10 | `src/NFHotel.Web/Program.cs:77`; `ops/deploy/nginx/templates/public.conf.template:58`; `docs/adr/adr-0048-where-security-txt-is-served.md:54` | The public site sends Strict-Transport-Security: max-age=2592000 (30 days, the default of app.UseHsts(); there is no AddHsts) without includeSubDomains, and the answers that nginx makes itself carry none. Nothing in the repository sets HSTS at Cloudflare (BF-10). |
| 3.4.2 | 1 | n/a | - | The flow uses no CORS: neither application calls AddCors or UseCors and nginx adds no Access-Control header, so a browser refuses every cross-origin read. |
| 3.4.3 | 2 | open, BF-12 | `src/NFHotel.Web/Security/SecurityHeaders.cs:20`; `src/NFHotel.Web/Security/SecurityHeaders.cs:14`; `docs/milestones/mil-033-security-headers.md:40`; `NFHotel.Web.Tests/SecurityHeadersTests.ContentSecurityPolicy_Directives_AllowNoInlineCodeOrForeignSource` | Every answer of the web application carries one CSP with object-src 'none' and a 'self' allowlist without inline code. base-uri is 'self', not 'none', because Blazor needs the base element of App.razor, as MIL-033 chose and the comment of the code says; a base element that markup planted could only point at another path of this site. The test pins 'self' since MIL-089. The answers that nginx makes itself carry no CSP (BF-12). |
| 3.4.4 | 2 | open, BF-12 | `NFHotel.Web.Tests/SecurityHeadersTests.Get_Page_HasEveryHeader`; `NFHotel.Api.Tests/ApiSecurityHeadersTests.Get_AnyAnswer_TellsTheBrowserNotToSniff`; `ops/deploy/nginx/templates/public.conf.template:23` | The web application and the API send nosniff on every answer, but the answers that nginx makes itself on the public listener (413 for a body over 64 KB, 404 on /admin without the Access token and on other /mobile-api/ paths, 502 while the web application is down) carry none (BF-12). |
| 3.4.5 | 2 | met | `src/NFHotel.Web/Security/SecurityHeaders.cs:28`; `NFHotel.Web.Tests/SecurityHeadersTests.ReferrerPolicy_Value_SendsNoAddressToAnotherSite`; `NFHotel.Web.Tests/SecurityHeadersTests.Get_Page_HasEveryHeader`; `NFHotel.Web.Tests/BookingVerifyPageTests.Answers_OfThePage_CarryNoReferrerAndNoStore` | Every answer of the web application sends Referrer-Policy same-origin, so no address of the flow goes to another site; the page of the verification link keeps no-referrer, because its headers are written after the others (fixed in MIL-089). |
| 3.4.6 | 2 | open, BF-12 | `src/NFHotel.Web/Security/SecurityHeaders.cs:20`; `NFHotel.Web.Tests/SecurityHeadersTests.Get_Redirect_HasEveryHeader`; `src/NFHotel.Web/Program.cs:96` | Every answer of the web application carries frame-ancestors 'none' (Blazor's own frame-ancestors 'self' is turned off). The application sets no X-Frame-Options of its own; the antiforgery of ASP.NET Core writes X-Frame-Options: SAMEORIGIN on the answer that sets a new antiforgery cookie, and a browser that knows frame-ancestors obeys it and ignores X-Frame-Options (`docs/sqa/evidence/mil-089/live-check.txt`). The answers that nginx makes itself carry no CSP, so they could be framed; they hold no content of the site (BF-12). |
| 3.5.1 | 1 | met | `NFHotel.Web.Tests/GuestPagesTests.GuestEndpoint_PostWithoutAntiforgery_ReturnsBadRequest`; `NFHotel.Web.Tests/BookingVerifyPageTests.Post_WithoutAntiforgery_IsRefusedAndCallsNothing`; `src/NFHotel.Web/Program.cs:88` | Every form post of the flow (the sign-in, the verification, the cancel and the move on /my-booking, /my-booking/start and the sign-out) needs the antiforgery token. The steps of /book run over the web socket, whose missing origin check is 4.4.2. |
| 3.5.2 | 1 | n/a | - | The flow does not rely on CORS preflight: it sends no CORS header, and its posts are checked with antiforgery tokens (3.5.1). |
| 3.5.3 | 1 | met | `NFHotel.Web.Tests/BookingVerifyPageTests.Get_Opened_ShowsTheDisabledFormAndCallsNothing`; `src/NFHotel.Web/Security/GuestSignInRegistration.cs:96`; `src/NFHotel.Api/Controllers/GuestBookingController.cs:54` | Every change of the flow is a POST: the sign-in, the verification, the session start and the sign-out, the cancel and the move, and the API routes that book, ask, verify, sign in, cancel and move. Opening the link of the mail changes nothing. |
| 3.5.4 | 2 | open, BF-09 | `ops/deploy/nginx/templates/public.conf.template:38`; `ops/deploy/nginx/templates/public.conf.template:47`; `src/NFHotel.Web/Security/StaffSecurityRegistration.cs:35` | The public name serves the booking pages, the back office under /admin and two routes of the separate API under /mobile-api on one origin, so the same-origin policy does not separate them. The staff cookie (default Path=/) goes with every request of the booking flow to the web application (BF-09). |
| 3.5.5 | 2 | n/a | - | The pages of the flow neither send nor receive postMessage: the site's own code in src/NFHotel.Web (outside the Bootstrap files, which no page loads) has no postMessage call and no message listener. |
| 3.7.1 | 2 | met | `src/NFHotel.Web/Components/App.razor:21`; `NFHotel.Web.Tests/SecurityHeadersTests.ContentSecurityPolicy_Directives_AllowNoInlineCodeOrForeignSource` | The pages use HTML, CSS, Blazor's blazor.web.js and one script of the site's own (booking-verify.js), and object-src 'none' refuses every plugin. |
| 3.7.2 | 2 | met | `NFHotel.Web.Tests/GuestPagesTests.SignInPage_AddressWithANumberOrAReference_GoesToTheEmptySignIn`; `src/NFHotel.Application/UseCases/Bookings/BookingMails.cs:41` | Every redirect of the flow goes to a path of the site (/booking, /my-booking) or keeps the host of the request (the HTTPS redirect), and the link in the mail is built from the site address of the settings, not from the request. |

### V4 API and Web Service

Sections: 4.1 Generic Web Service Security; 4.2 HTTP Message Structure Validation; 4.3 GraphQL; 4.4 WebSocket.

| Requirement | Level | Verdict | Evidence | Note |
| --- | --- | --- | --- | --- |
| 4.1.1 | 1 | open, BF-12 | `src/NFHotel.Web/Program.cs:90`; `ops/deploy/nginx/templates/public.conf.template:55` | Pages are text/html; charset=utf-8 and the API answers application/json; charset=utf-8, but the static text files (app.css, the scoped CSS, booking-verify.js, blazor.web.js, the SVG images) come from MapStaticAssets without a charset, and nginx's own error pages are text/html without one; no test checks the charset (BF-12). |
| 4.1.2 | 2 | met | `NFHotel.Web.Tests/HttpsRequirementTests.Get_RequestArrivingAsHttp_RedirectsToHttps`; `src/NFHotel.Web/Program.cs:81`; `docs/wiki/Public-Access.md:59` | Only the pages redirect HTTP to HTTPS (Always Use HTTPS at Cloudflare, port 80 of nginx, Tls:Require in the web application). The API has no HTTPS redirect, and its routes of the flow are reached only by the web application inside the stack. |
| 4.1.3 | 2 | met | `ops/tests/test-nginx.sh:240`; `NFHotel.Api.Tests/HttpCallerAddressTests.Of_CallWithoutTheServiceToken_IgnoresTheHeader`; `NFHotel.Web.Tests/ClientAddressMiddlewareTests.UseClientAddress_PeerOutsideTheStackWithTheHeader_TakesTheConnection` | nginx replaces X-Real-IP, X-NFHotel-Client-Address and X-Forwarded-Proto with its own values, and only the last X-Forwarded-For entry, the one nginx adds, counts. The web application takes the address header only from a peer of the stack and the API only on a call with the service token (the other holder of that token is 8.2.1). |
| 4.2.1 | 2 | met | `ops/deploy/nginx/Dockerfile:8`; `ops/deploy/nginx/templates/00-upgrade.conf.template:5`; `ops/deploy/nginx/snippets/proxy-headers.conf:12` | nginx 1.29 refuses a request with both Content-Length and Transfer-Encoding (since 1.21.1) and sends each request to the application with Connection: close unless it is an upgrade, so no smuggled request can ride a shared connection to Kestrel. No test sends such a request. |
| 4.3.1 | 2 | n/a | - | The flow has no GraphQL: no GraphQL library or endpoint is in src. |
| 4.3.2 | 2 | n/a | - | The flow has no GraphQL, so there is no introspection to turn off. |
| 4.4.1 | 1 | met | `src/NFHotel.Web/Security/SecurityHeaders.cs:19`; `docs/wiki/Public-Access.md:59`; `ops/deploy/nginx/templates/default.conf.template:10` | blazor.web.js opens /_blazor on the origin of the page, which is served over HTTPS only (Always Use HTTPS at Cloudflare; port 80 of nginx only redirects), and connect-src 'self' on an HTTPS page allows wss but not ws. From cloudflared on, the socket runs as plain ws on the Docker networks of the VM, like every request (V12). |
| 4.4.2 | 2 | open, BF-09 | `ops/deploy/nginx/snippets/blazor.conf:4`; `src/NFHotel.Web/Program.cs:95` | Neither nginx (blazor.conf) nor the web application checks the Origin of the handshake on /_blazor: there is no WebSocketOptions.AllowedOrigins and no check of $http_origin, so a page on any site can open a circuit of /book from a visitor's browser (BF-09). |
| 4.4.3 | 2 | n/a | `src/NFHotel.Web/Features/Booking/MyBooking.razor:13` | The flow needs no session on the socket: /book runs without one, and the guest's own booking is a static page with the guest cookie, so no dedicated WebSocket token is used. |
| 4.4.4 | 2 | n/a | - | No dedicated WebSocket token exists (4.4.3). |

### V6 Authentication

Sections: 6.1 Authentication Documentation; 6.2 Password Security; 6.3 General Authentication Security; 6.4 Authentication Factor Lifecycle and Recovery; 6.5 General Multi-factor authentication requirements; 6.6 Out-of-Band authentication mechanisms; 6.8 Authentication with an Identity Provider.

| Requirement | Level | Verdict | Evidence | Note |
| --- | --- | --- | --- | --- |
| 6.1.1 | 1 | met | `docs/adr/adr-0049-booking-reference.md:145-169`; `docs/refactor/BusinessRules.md:351-352`; `docs/wiki/Security-Controls.md:95`; `NFHotel.Api.Tests/LimitsTableTests.Table_Numbers_AreTheDefaultsOfTheirSettings` | ADR-0049 decision 6, the table Limits of the booking flow and the wiki row Rate limits define the two limits of the guest sign-in (5 failed tries per reference, 20 per caller address in 15 minutes), their settings GuestLookup:*, the alert BookingFlowRefusals and the lockouts they leave: whoever holds a reference can block it for 15 minutes, and about 50,000 failed sign-ins fill the store of references, which then fails closed. LimitsTableTests holds the table to the defaults of the settings. |
| 6.1.2 | 2 | n/a | `docs/adr/adr-0047-guest-login.md:65-66` | Outside the booking flow: passwords exist only for staff accounts (ADR-0005, ADR-0017); a guest has no password and signs in with the booking reference and the email. |
| 6.1.3 | 2 | met | `docs/adr/adr-0047-guest-login.md:67-74`; `docs/adr/adr-0052-a-booking-takes-a-room-once-its-email-is-verified.md:165-166`; `docs/wiki/Security-Controls.md:96,99` | A guest gets a session three ways, each documented with its controls: the sign-in with reference and email behind the limits (ADR-0047, ADR-0049), the public booking with the verification off (ADR-0047 decision 2) and the button of the mail link with it on (ADR-0052 decision 5); all three issue the same 2-hour token for one booking. No single page lists the three side by side; the wiki row Guest authorization names the sign-in only. |
| 6.2.1 | 1 | n/a | `docs/adr/adr-0047-guest-login.md:65-66` | Outside the booking flow: passwords exist only for staff accounts (ADR-0005, ADR-0017); a guest has no password. |
| 6.2.2 | 1 | n/a | `docs/adr/adr-0047-guest-login.md:65-66` | Outside the booking flow: passwords exist only for staff accounts (ADR-0017); a guest has no password to change. |
| 6.2.3 | 1 | n/a | `docs/adr/adr-0047-guest-login.md:65-66` | Outside the booking flow: passwords exist only for staff accounts (ADR-0017); a guest has no password to change. |
| 6.2.4 | 1 | n/a | `docs/adr/adr-0047-guest-login.md:65-66` | Outside the booking flow: passwords exist only for staff accounts, whose policy checks a list of common passwords (ADR-0017); a guest sets no password. |
| 6.2.5 | 1 | n/a | `docs/adr/adr-0047-guest-login.md:65-66` | Outside the booking flow: passwords exist only for staff accounts (ADR-0017); a guest sets no password. |
| 6.2.6 | 1 | n/a | `src/NFHotel.Web/Features/Booking/GuestSignIn.razor.cs:69-76` | Outside the booking flow: passwords exist only for staff accounts; the guest sign-in has no password field, only the reference and the email. |
| 6.2.7 | 1 | n/a | `src/NFHotel.Web/Features/Booking/GuestSignIn.razor.cs:69-76` | Outside the booking flow: passwords exist only for staff accounts; the guest sign-in has no password field. |
| 6.2.8 | 1 | n/a | `docs/adr/adr-0047-guest-login.md:65-66` | Outside the booking flow: passwords exist only for staff accounts. The guest sign-in compares the email trimmed, in normal form C and without letter case on purpose (ADR-0047 decision 3); the email is no password. |
| 6.2.9 | 2 | n/a | `docs/adr/adr-0047-guest-login.md:65-66` | Outside the booking flow: passwords exist only for staff accounts (12 to 256 characters, ADR-0017); a guest has no password. |
| 6.2.10 | 2 | n/a | `docs/adr/adr-0047-guest-login.md:65-66` | Outside the booking flow: passwords exist only for staff accounts; a guest has no password to rotate. |
| 6.2.11 | 2 | n/a | `docs/adr/adr-0047-guest-login.md:65-66` | Outside the booking flow: passwords exist only for staff accounts; a guest sets no password. |
| 6.2.12 | 2 | n/a | `docs/adr/adr-0047-guest-login.md:65-66` | Outside the booking flow: passwords exist only for staff accounts; a guest sets no password. |
| 6.3.1 | 1 | met | `NFHotel.Api.Tests/GuestAccessTests.SignIn_SixthTryForOneReference_ReturnsTooManyRequests`; `NFHotel.Api.Tests/GuestAccessTests.SignIn_TwentyFirstFailedTryFromOneAddress_ReturnsTooManyRequests`; `NFHotel.Infrastructure.Tests/InMemoryGuestSignInLimitsTests.TryCount_ManyThreadsAtOnce_CountsNoMoreThanTheLimits` | GuestAccessService counts every try against the reference and the caller address before the booking is read (GuestAccessService.cs:96) and refuses the sixth failure per reference and the 21st per address in 15 minutes with 429, as ADR-0049 decision 6 says. The address is the one nginx saw, taken only from a call with the service token (ADR-0051 decision 4). |
| 6.3.2 | 1 | n/a | `docs/adr/adr-0047-guest-login.md:65-66` | Outside the booking flow: the flow has no user accounts; the one seeded account is the staff initial manager (Staff__InitialManager__*, ADR-0017). |
| 6.3.3 | 2 | met | `docs/adr/adr-0047-guest-login.md:50-58`; `docs/adr/adr-0047-guest-login.md:131-137`; `docs/wiki/Security-Controls.md:96` | The guest sign-in is single-factor (the reference with the email of the booking); ADR-0047 documents why (options 1 to 3) and the mitigating controls: limits per reference and per address, a 2-hour token for one booking, answers with the receipt only, no room change, no passport number. That is the documented relaxation that 6.3.3 allows; its reason against a mail link (needs a mail service) no longer holds since MIL-088. |
| 6.3.4 | 2 | met | `NFHotel.Api.Tests/EndpointProtectionTests.Controllers_EveryAction_RequiresAPermissionOrIsPublic`; `NFHotel.Api.Tests/GuestAccessTests.GuestEndpoints_TheThreeGuestRoutes_AreTheOnlyOnesOpenToAGuestAndTakeNoKey`; `NFHotel.Api.Tests/BookingReadProtectionTests.OldLookup_AnyCall_IsNoEndpointAnyMore` | Only the three documented ways issue a guest token (GuestAccessService.cs:117 and 132, BookingRequestService.cs:211), the older receipt lookup is gone, and a test fails on any action that is neither public, staff nor guest. Every way gives the same 2-hour token for one booking, and the staff routes refuse it with 403. |
| 6.4.1 | 1 | met | `NFHotel.Application.Tests/BookingRequestServiceTests.VerifyAsync_UsedWrongMalformedOrLapsedLink_GetsTheSameAnswer`; `NFHotel.Infrastructure.Tests/BookingRequestPersistenceTests.ClaimOpenAsync_AtTheMomentTheLinkLapses_ClaimsNothing`; `src/NFHotel.Application/UseCases/Bookings/VerificationTokens.cs:36` | The one activation code of the flow, the token of the mail link, is 32 bytes from RandomNumberGenerator, works once and lapses after 30 minutes, and never becomes a lasting credential: after the button the guest holds the usual 2-hour token. The booking reference, which the system also makes and mails, is the lasting half of the sign-in and cannot be changed (see 11.5.1). |
| 6.4.2 | 1 | met | `src/NFHotel.Web/Features/Booking/GuestSignIn.razor.cs:69-76`; `src/NFHotel.Application/UseCases/Guests/GuestAccessService.cs:81-118` | The guest sign-in asks for the reference and the email of the booking and nothing else; there are no password hints and no secret questions. The email is the booking's own data, not a chosen answer, and ADR-0047 treats it as a weak secret (see 6.3.3). |
| 6.4.3 | 2 | n/a | `docs/adr/adr-0049-booking-reference.md:142-144` | The flow has no password to reset. A guest who lost the reference contacts the hotel and staff give it (ADR-0049 decision 5); how staff check the caller is not written down (a note of BF-12). |
| 6.4.4 | 2 | n/a | - | The flow has no multi-factor authentication, so no factor can be lost; the staff sign-in is outside the flow. |
| 6.5.1 | 2 | met | `NFHotel.Infrastructure.Tests/BookingRequestPersistenceTests.ClaimOpenAsync_TenPressesAtOnce_ClaimTheRequestOnce`; `NFHotel.Api.Tests/BookingRequestApiTests.Verify_SameTokenTwice_SecondGetsTheAnswerOfALinkThatDoesNotWork`; `NFHotel.Web.Tests/GuestSessionTicketsTests.Redeem_ManyThreadsAtOnce_GivesTheSessionOnce` | The token of the mail link is claimed by one DELETE ... RETURNING (BookingRequestRepository.cs:49), so of ten presses at once one claims the request, and a second press gets 422 booking.verification_link_invalid. The one-time ticket of the web application is redeemed once as well. |
| 6.5.2 | 2 | n/a | `NFHotel.Application.Tests/BookingRequestServiceTests.AskAsync_Token_IsStoredOnlyAsItsHash` | The flow has no lookup secrets. The nearest stored secret, the 256-bit token of the mail link, is kept only as its SHA-256 (VerificationTokens.cs:55), which the requirement allows from 112 bits. |
| 6.5.3 | 2 | met | `src/NFHotel.Application/UseCases/Bookings/VerificationTokens.cs:36`; `NFHotel.Application.Tests/BookingRequestServiceTests.AskAsync_TwoRequests_GetDifferentTokens` | The token of the mail link is RandomNumberGenerator.GetBytes(32) written as base64url. The test shows that two requests get different tokens; that the source is the CSPRNG shows only in the code. |
| 6.5.4 | 2 | met | `src/NFHotel.Application/UseCases/Bookings/VerificationTokens.cs:19`; `NFHotel.Application.Tests/BookingRequestServiceTests.AskAsync_Token_IsStoredOnlyAsItsHash` | The token has 256 random bits (43 base64url characters, which the test matches), far above 20 bits. |
| 6.5.5 | 2 | accepted | `src/NFHotel.Domain/Entities/BookingRequest.cs:41`; `src/NFHotel.Application/UseCases/Bookings/BookingRequestService.cs:211`; `docs/adr/adr-0052-a-booking-takes-a-room-once-its-email-is-verified.md:121`; `docs/adr/adr-0052-a-booking-takes-a-room-once-its-email-is-verified.md:252`; `NFHotel.Application.Tests/BookingRequestServiceTests.VerifyAsync_JustBeforeTheLinkLapses_Books` | The mail link is an out-of-band token that also signs the guest in, and it lives 30 minutes, three times the bound of the requirement. Accepted by ADR-0052 decision 2 and its first negative consequence: a mail can take minutes to arrive or wait in a spam folder, and the token has 256 random bits, works once and books only what the guest asked for. The record weighs the bound since MIL-089, a stand-in judgement that S01 can overrule. |
| 6.6.1 | 2 | n/a | - | The flow sends no codes by phone or SMS; its only out-of-band channel is the mail link. |
| 6.6.2 | 2 | met | `NFHotel.Application.Tests/BookingRequestServiceTests.AskAsync_SecondRequestOfTheMailbox_MakesTheFirstLinkUseless`; `NFHotel.Infrastructure.Tests/BookingRequestPersistenceTests.Renew_Request_MakesTheOldLinkUseless`; `NFHotel.E2E.Tests/BookingVerificationTests.SendAgain_FromTheInbox_SendsANewLinkAndTheFirstStopsWorking` | The hash of each token sits on its one booking request and the claim books only that request; a new request of the mailbox replaces the hash, so an earlier link stops working. The session goes to whichever browser presses the button, not necessarily the one that asked, by design (ADR-0052). |
| 6.6.3 | 2 | accepted | `src/NFHotel.Api/Controllers/BookingsController.cs:154-158`; `docs/refactor/BusinessRules.md:348`; `docs/adr/adr-0052-a-booking-takes-a-room-once-its-email-is-verified.md:229` | The verification route has no limit per caller, and a failed press writes verification_failed, which no alert watches. Accepted by ADR-0052 decision 10 since MIL-089: the token has 256 random bits, so guessing cannot succeed, and the requests that make links are limited per address and per mailbox. |
| 6.8.1 | 2 | n/a | - | The flow uses no identity provider: the API issues and checks its own guest token. |
| 6.8.2 | 2 | n/a | `NFHotel.Api.Tests/GuestAccessTests.Refusal_EveryKindOfFailedSignIn_HasTheStatusAndTextOfAWrongEmail` | The flow uses no identity provider. The API's own guest token is checked for its signature all the same: unsigned, tampered and foreign-key tokens get the one 401 in the test. |
| 6.8.3 | 2 | n/a | - | The flow uses no identity provider and no SAML. |
| 6.8.4 | 2 | n/a | - | The flow uses no identity provider, so there are no acr, amr or auth_time claims to check. |

### V7 Session Management

Sections: 7.1 Session Management Documentation; 7.2 Fundamental Session Management Security; 7.3 Session Timeout; 7.4 Session Termination; 7.5 Defenses Against Session Abuse; 7.6 Federated Re-authentication.

| Requirement | Level | Verdict | Evidence | Note |
| --- | --- | --- | --- | --- |
| 7.1.1 | 2 | open, BF-06 | `docs/adr/adr-0047-guest-login.md:36`; `docs/adr/adr-0047-guest-login.md:61-63` | ADR-0047 documents the absolute life of the guest session (2 hours, no renewal) and why it is short, but no inactivity timeout (there is none) and no comparison with the re-authentication of NIST SP 800-63B (BF-06). |
| 7.1.2 | 2 | open, BF-06 | `src/NFHotel.Application/UseCases/Guests/GuestAccessService.cs:115-117` | No document says how many guest sessions one booking may have, and the code sets no bound: every successful sign-in issues one more token (BF-06). |
| 7.1.3 | 2 | n/a | - | No federation or single sign-on: the API issues its own tokens and trusts the sessions of no other system. |
| 7.2.1 | 1 | met | `NFHotel.Api.Tests/GuestAccessTests.Refusal_EveryKindOfFailedSignIn_HasTheStatusAndTextOfAWrongEmail`; `NFHotel.Infrastructure.Tests/JwtGuestTokenIssuerTests.Issue_Booking_IsNotValidAsAStaffToken`; `src/NFHotel.Api/Security/ApiTokenValidation.cs:34-47` | The API checks the guest token on every call (issuer, audience nfhotel-guest, HS256 only, expiry, role Guest and a booking claim), and the web application reads the guest cookie on the server through ASP.NET Core Data Protection; the browser never holds the token in readable form. |
| 7.2.2 | 1 | met | `src/NFHotel.Infrastructure/Security/JwtGuestTokenIssuer.cs:59-81`; `NFHotel.Infrastructure.Tests/JwtGuestTokenIssuerTests.Issue_Booking_ProducesATokenWithTheGuestRoleAndTheBooking` | Each sign-in, public booking or verification issues a new signed JWT with its own iat and exp, held in a data-protected cookie; the static service token Api__Token authenticates the web application to the API, not a guest. The token has no jti, so two sign-ins to one booking in the same second get the same string. |
| 7.2.3 | 1 | met | `src/NFHotel.Web/Security/GuestSessionTickets.cs:61`; `NFHotel.Web.Tests/GuestSessionTicketsTests.Issue_TwoSessions_GivesTwoDifferentRandomTickets` | The guest session is self-contained; the one reference token of the flow, the ticket that carries a new booking's session to the guest cookie, is 32 bytes from RandomNumberGenerator and works once within 10 minutes. |
| 7.2.4 | 1 | met | `NFHotel.Web.Tests/GuestPagesTests.SignIn_RightReferenceAndEmail_SetsTheGuestCookieAndGoesToTheOwnBooking`; `src/NFHotel.Web/Security/GuestSignInRegistration.cs:132-141` | Every sign-in gets a new token from the API and writes a new guest cookie over any old one, and there is no guest session before the sign-in that could be fixed. An old token stays valid at the API until its own end (see 7.4.1). |
| 7.3.1 | 2 | open, BF-06 | `src/NFHotel.Web/Security/GuestSignInRegistration.cs:72`; `src/NFHotel.Web/Security/GuestSignInRegistration.cs:140`; `src/NFHotel.Infrastructure/Security/JwtGuestTokenIssuer.cs:29` | The guest session has only the absolute 2 hours: no inactivity timeout, a cookie that does not slide and is persistent (IsPersistent = true), so an idle session on a shared computer stays open for up to 2 hours, also after the browser is closed (BF-06). |
| 7.3.2 | 2 | met | `NFHotel.Infrastructure.Tests/JwtGuestTokenIssuerTests.Issue_Booking_ExpiresTwoHoursAfterTheClock`; `NFHotel.Api.Tests/GuestAccessTests.SignIn_RightReferenceAndEmailInAnotherCase_ReturnsASessionOfTwoHours`; `src/NFHotel.Web/Security/GuestSignInRegistration.cs:140` | The token ends 2 hours after the sign-in (ADR-0047 decision 2) and the API refuses it after that (the expired token of GuestAccessTests); the cookie ends with the token and is never renewed (AllowRefresh = false, SlidingExpiration = false). |
| 7.4.1 | 1 | accepted | `src/NFHotel.Web/Security/GuestSignInRegistration.cs:115-124`; `docs/adr/adr-0047-guest-login.md:137-138` | Sign-out deletes the guest cookie in the browser only; the token inside is a JWT without a list of revoked tokens, so a copy stays usable for the rest of its 2 hours. Accepted by ADR-0047, whose negative consequence names it; BF-06 proposes a stamp per booking that would end every token at once. |
| 7.4.2 | 1 | accepted | `docs/adr/adr-0047-guest-login.md:104-107`; `src/NFHotel.Application/UseCases/Guests/GuestBookingService.cs:47-55`; `src/NFHotel.Application/UseCases/Guests/GuestService.cs:276-281` | A guest has no account. When staff cancel a booking, new sign-ins are refused, and a token issued before keeps working to its end only to read the cancelled receipt, because cancel and move need an open booking (ADR-0047 decision 7). |
| 7.4.3 | 2 | n/a | `src/NFHotel.Application/UseCases/Guests/GuestService.cs:194` | A guest cannot change the reference or the email. Staff can change the email of a guest in the back office (outside the flow), and guest tokens issued before stay valid to their end (see 7.4.1). |
| 7.4.4 | 2 | met | `src/NFHotel.Web/Features/Booking/MyBooking.razor:98-102`; `NFHotel.Web.Tests/GuestPagesTests.SignOut_GuestAndStaffInOneBrowser_EndsOnlyTheGuestSession`; `NFHotel.E2E.Tests/GuestBookingTests.GuestSession_BookSignOutSignInAndCancel_ReachesTheOwnBooking` | /my-booking, the one page that needs the guest session, always shows a Sign out button that posts to /my-booking/sign-out with an antiforgery token. |
| 7.4.5 | 2 | open, BF-06 | `docs/adr/adr-0047-guest-login.md:137-138`; `ops/tests/test-secret-rotation.sh:122` | Staff cannot end the session of one guest. The only way to end guest sessions is to rotate Staff__Token__SigningKey, which ends every guest and staff token at once and needs a deploy (BF-06). |
| 7.5.1 | 2 | n/a | `src/NFHotel.Api/Controllers/GuestBookingController.cs:46-66` | A guest cannot change any attribute that the sign-in uses: the guest routes read, cancel and move the booking only. |
| 7.5.2 | 2 | open, BF-06 | `src/NFHotel.Web/Features/Booking/MyBooking.razor:98-102`; `src/NFHotel.Web/Security/GuestSignInRegistration.cs:115-124` | A guest cannot see or end the other sessions of the booking: there is no list, and sign-out ends only the cookie of the browser it is pressed in (BF-06). |
| 7.6.1 | 2 | n/a | - | No relying party and no identity provider: the flow has no federated sessions. |
| 7.6.2 | 2 | met | `NFHotel.Web.Tests/BookingVerifyPageTests.Get_Opened_ShowsTheDisabledFormAndCallsNothing`; `NFHotel.Web.Tests/GuestPagesTests.GuestEndpoint_PostWithoutAntiforgery_ReturnsBadRequest` | A guest session starts only from an explicit post with an antiforgery token: the sign-in form, the confirmation of a booking (/my-booking/start) or the button of the mail link. Opening the link calls nothing, so a mail scanner that opens it starts no session. |

### V8 Authorization

Sections: 8.1 Authorization Documentation; 8.2 General Authorization Design; 8.3 Operation Level Authorization; 8.4 Other Authorization Considerations.

| Requirement | Level | Verdict | Evidence | Note |
| --- | --- | --- | --- | --- |
| 8.1.1 | 1 | met | `docs/refactor/Access-Control-Matrix.md:69`; `NFHotel.Application.Tests/AccessControlMatrixTests.HasPermission_RoleAndPermission_EqualsTheMatrix` | The access control matrix gives each role its permissions and defines a guest's own data as the booking in the guest token; the test reads the matrix and fails when it differs from RolePermissions. |
| 8.1.2 | 2 | met | `docs/adr/adr-0049-booking-reference.md:170`; `docs/refactor/Access-Control-Matrix.md:73` | ADR-0049 decision 7 names the fields a guest reads (reference, dates, nights, room size, status) and lets the status decide whether a move or a cancel is offered; the matrix says a guest moves only the dates in the same room and never sees a passport number. Field rules for the staff roles are still open questions in the matrix, outside the flow. |
| 8.2.1 | 1 | open, BF-11 | `NFHotel.Api.Tests/EndpointProtectionTests.Controllers_EveryAction_RequiresAPermissionOrIsPublic`; `ops/deploy/benchmark/docker-compose.yml:55`; `src/NFHotel.Api/Security/HttpCallerAddress.cs:54` | Every action needs a staff permission or a guest, or is on the public list of the test. But the service token that opens the public routes is also the token of the benchmark service, so that service can book, ask for and verify bookings, sign guests in and name any caller address without a permission of its own (BF-11). |
| 8.2.2 | 1 | met | `NFHotel.Api.Tests/GuestAccessTests.GuestEndpoints_TheThreeGuestRoutes_AreTheOnlyOnesOpenToAGuestAndTakeNoKey`; `NFHotel.Application.Tests/GuestOwnershipTests.GuestOperation_AnotherBooking_IsForbidden` | The guest routes take no key: the API reads the booking from the guest token, and the use cases check again with GuestOwnership that the call is about that booking. |
| 8.2.3 | 2 | met | `NFHotel.Api.Tests/GuestAccessTests.GuestAnswers_GuestWithAPassportNumber_HoldTheReceiptAndNothingMore`; `NFHotel.Api.Tests/GuestAccessTests.Reschedule_GuestRoute_KeepsTheRoomOfTheBooking`; `NFHotel.Api.Tests/BookingInputApiTests.PublicBooking_UnknownField_IsRefused` | A guest reads the six fields of the receipt and nothing more, a move that names a room is refused with 400, and a public booking with a field that its type does not have is refused, so no other field can be written. |
| 8.3.1 | 1 | met | `NFHotel.Api.Tests/GuestAccessTests.StaffEndpoint_GuestToken_ReturnsForbidden`; `NFHotel.Api.Tests/GuestAccessTests.GuestRoute_StaffToken_ReturnsForbidden`; `src/NFHotel.Application/UseCases/Bookings/BookingService.cs:896` | The API decides every access, with the filters RequiresGuest and RequiresPermission and again with GuestOwnership in the use cases; the web application only carries the tokens. A guest token on any staff endpoint gets 403, and so does a staff token on the guest routes. |
| 8.4.1 | 2 | n/a | - | NF Hotel is one hotel with one set of data; the application has no tenants. |

### V11 Cryptography

Sections: 11.1 Cryptographic Inventory and Documentation; 11.2 Secure Cryptography Implementation; 11.3 Encryption Algorithms; 11.4 Hashing and Hash-based Functions; 11.5 Random Values; 11.6 Public Key Cryptography.

| Requirement | Level | Verdict | Evidence | Note |
| --- | --- | --- | --- | --- |
| 11.1.1 | 2 | open, BF-08 | `docs/refactor/Encryption-Key-Management.md:4`; `docs/refactor/Encryption-Key-Management.md:142-143`; `docs/refactor/Encryption-Key-Management.md:198`; `docs/adr/adr-0024-secrets-as-files.md:46` | The one key-management document is a draft that cites NIST SP 800-57 but covers only the passport key, says its production storage is undecided (secret files since ADR-0024) and has no rotation procedure; the HS256 signing key, the service token, the Data Protection keys and the SMTP password have no written lifecycle. No key is shared by more than two parties (BF-08). |
| 11.1.2 | 2 | open, BF-08 | `docs/refactor/Encryption-Key-Management.md:120-145` | The key inventory lists one key, Encryption:PassportKey. It misses the HS256 key that signs staff and guest tokens, Api__Token, the Data Protection key ring of the web application, Smtp__Password, the trust of the relay certificate, and the uses of SHA-256 (link token, mailbox key, certificate pin) (BF-08). |
| 11.2.1 | 2 | met | `src/NFHotel.Infrastructure/Security/AesGcmStringEncryptor.cs:64-65`; `src/NFHotel.Application/UseCases/Bookings/VerificationTokens.cs:36`; `src/NFHotel.Application/UseCases/Bookings/VerificationTokens.cs:55`; `src/NFHotel.Infrastructure/Security/JwtGuestTokenIssuer.cs:80` | All cryptography of the flow comes from the .NET platform (AesGcm, SHA256, RandomNumberGenerator, HMAC through Microsoft.IdentityModel, ASP.NET Core Data Protection, TLS through MailKit on SslStream); no primitive is written in the project. |
| 11.2.2 | 2 | open, BF-08 | `docs/adr/adr-0024-secrets-as-files.md:46`; `src/NFHotel.Infrastructure/Security/AesGcmStringEncryptor.cs:12`; `src/NFHotel.Infrastructure/Security/AesGcmStringEncryptor.cs:27-32` | The passport numbers of guests and booking requests are encrypted with one key in a format without a key or algorithm id (nonce, tag, ciphertext), so neither key nor algorithm can be replaced without a re-encryption tool that does not exist (ADR-0024 decision 7). The token signing key can be rotated (test-secret-rotation.sh), at the cost of ending every session (BF-08). |
| 11.2.3 | 2 | met | `src/NFHotel.Infrastructure/Security/AesGcmStringEncryptor.cs:30`; `src/NFHotel.Infrastructure/DependencyInjection.cs:295`; `src/NFHotel.Application/UseCases/Bookings/VerificationTokens.cs:19` | AES-GCM with keys of 128 to 256 bits, HMAC-SHA256 with a key of at least 32 characters (made with openssl rand -base64 48, as the documents say), SHA-256 and the 256-bit tokens each reach a strength of 128 bits or more. The start checks the length of the signing key, not its randomness. |
| 11.3.1 | 1 | met | `src/NFHotel.Infrastructure/Security/AesGcmStringEncryptor.cs:64-65` | The flow encrypts with AES-GCM, and the guest cookie with the default of Data Protection (AES-256-CBC with HMAC-SHA256); git grep finds no ECB, DES or RSA padding in src. |
| 11.3.2 | 1 | met | `src/NFHotel.Infrastructure/Security/AesGcmStringEncryptor.cs:64-65` | AES-GCM for the passport numbers and AES-256-CBC with HMAC-SHA256 (Data Protection) for the guest cookie; both are approved. |
| 11.3.3 | 2 | met | `NFHotel.Infrastructure.Tests/AesGcmStringEncryptorTests.Decrypt_TamperedCipherText_ThrowsCryptographicException` | AES-GCM with a 16-byte tag refuses a changed ciphertext, and Data Protection authenticates the cookie with HMAC. No associated data binds a passport ciphertext to its row (a note of BF-12). |
| 11.4.1 | 1 | met | `src/NFHotel.Application/UseCases/Bookings/VerificationTokens.cs:55`; `src/NFHotel.Api/Security/ApiTokenValidation.cs:42`; `src/NFHotel.Infrastructure/Mail/SmtpMailSender.cs:181` | SHA-256 for the link token, the mailbox key and the certificate pin, HMAC-SHA256 for the tokens; git grep finds no MD5 or SHA-1 in src. |
| 11.4.2 | 2 | n/a | `docs/adr/adr-0047-guest-login.md:65-66` | Outside the booking flow: guests have no password; staff passwords use PBKDF2-HMAC-SHA256 with 600,000 iterations (Pbkdf2PasswordHasher, ADR-0005). |
| 11.4.3 | 2 | met | `src/NFHotel.Application/UseCases/Bookings/VerificationTokens.cs:55`; `src/NFHotel.Infrastructure/Mail/SmtpMailSender.cs:181` | Every hash of the flow is SHA-256 with 256 bits of output, and the token signature is HMAC-SHA256. |
| 11.4.4 | 2 | n/a | `src/NFHotel.Infrastructure/Security/JwtStaffTokenIssuer.cs:51` | No key of the flow is derived from a password: the HMAC key is the configured random string used as its bytes, and the passport key is 16 to 32 random bytes in base64. |
| 11.5.1 | 2 | accepted | `src/NFHotel.Domain/ValueObjects/BookingReference.cs:38`; `src/NFHotel.Infrastructure/Persistence/Configurations/BookingConfiguration.cs:55`; `docs/adr/adr-0049-booking-reference.md:123-126` | The booking reference is a version 4 UUID with 122 random bits from the generator of the system, and the requirement does not count a UUID. Accepted by ADR-0049: the reference opens a booking only with its email, and 5 failed tries per reference and 20 per address in 15 minutes stop a search. The link token and the ticket have 256 bits from RandomNumberGenerator. |
| 11.6.1 | 2 | n/a | `src/NFHotel.Infrastructure/Mail/SmtpMailSender.cs:190` | The flow makes no key pairs and signs only with HMAC; public-key cryptography occurs only inside TLS to the relay, where the platform checks the certificate against the system store. The mail sender turns the check of revocation off, without a comment (a note of BF-12). |

### V14 Data Protection

Sections: 14.1 Data Protection Documentation; 14.2 General Data Protection; 14.3 Client-side Data Protection.

| Requirement | Level | Verdict | Evidence | Note |
| --- | --- | --- | --- | --- |
| 14.1.1 | 2 | open, BF-04 | `docs/sqa/samm-baseline.md:137`; `docs/wiki/Incidents.md:63` | Incidents.md names the personal data the hotel holds (name, email, phone, passport number), but nothing classifies the data of the flow into protection levels. Encoded or derived data, such as the guest token with its booking id, the booking reference, the hash of the verification token and the caller address, is not classified at all (BF-04). |
| 14.1.2 | 2 | open, BF-04 | `docs/refactor/Decisions.md:48-51`; `docs/sqa/samm-baseline.md:92` | With no protection levels there are no requirements per level. D-04 asks for encryption and a delete policy, D-05 leaves the retention values to be set later, and log access and retention are written only in the Log policy of Incidents.md (BF-04). |
| 14.2.1 | 1 | met | `NFHotel.ApiClient.Tests/BookingRequestGatewayTests.VerifyAsync_Token_IsInTheBodyAndNotInTheAddress`; `NFHotel.Web.Tests/BookingVerifyPageTests.Script_OfThePage_IsServedAndTakesTheTokenOutOfTheAddress`; `src/NFHotel.Web/Security/GuestSignInRegistration.cs:94`; `src/NFHotel.ApiClient/ApiGateway.cs:118` | The verification token stands in the fragment of the link and goes in the body of a POST, the reference and the email of the sign-in go in a form body, and the guest token goes in the guest cookie and in a request header to the API. An old address with a booking number is redirected to the empty sign-in without being read. |
| 14.2.2 | 2 | met | `src/NFHotel.Web/Security/GuestSessionTickets.cs:71`; `src/NFHotel.Web/Security/GuestSessionTickets.cs:56-58` | nginx has no proxy cache, and the only server-side holder of a guest session is GuestSessionTickets in memory: a ticket is removed when it is redeemed, and lapsed tickets (10 minutes) are removed at the next issue. |
| 14.2.3 | 2 | met | `NFHotel.Web.Tests/SecurityHeadersTests.Get_Page_SendsOnePolicy`; `src/NFHotel.Web/Security/SecurityHeaders.cs:19` | The Content-Security-Policy allows scripts, styles, images, fonts and connections only from the site itself, so the pages load no tracker or code of another party. |
| 14.2.4 | 2 | open, BF-04 | `src/NFHotel.Application/UseCases/Bookings/BookingRequestCleanup.cs:52-53`; `docs/refactor/Decisions.md:45-51`; `docs/wiki/Incidents.md:112`; `docs/wiki/Incidents.md:122` | The clean-up deletes only lapsed booking requests and old mail records; guests and bookings are kept with no retention, though D-04 requires a delete policy. The Log policy promises 14 days, while the Docker local logs and the journal on the VM are bounded only by size. Since MIL-089 the Log policy names what the security events hold: the caller address, and no email (BF-04). |
| 14.3.1 | 1 | open, BF-06 | `src/NFHotel.Web/Security/GuestSignInRegistration.cs:115-124` | The sign-out deletes the guest cookie but sends no Clear-Site-Data, and an open page of the own booking keeps the booking in the DOM after the guest token expires, since the static page has nothing that clears it (BF-06). |
| 14.3.2 | 2 | met | `NFHotel.Web.Tests/BookingVerifyPageTests.Answers_OfThePage_CarryNoReferrerAndNoStore`; `src/NFHotel.Web/Security/VerifyPageHeaders.cs:20`; `ops/security/zap-rules.tsv:10` | The verify page answers Cache-Control no-store and Referrer-Policy no-referrer, and the pages with a form, the sign-in and the own booking among them, get no-cache, no-store from the antiforgery token. Only the verify page has a test of it. |
| 14.3.3 | 2 | met | `src/NFHotel.Web/Security/GuestSignInRegistration.cs:62-67`; `src/NFHotel.Web/wwwroot/js/booking-verify.js:22-37` | The only browser storage of the flow is the guest cookie, a session token that is HttpOnly and sent only under /my-booking. The one script of the flow writes the token into a form field, never into localStorage, sessionStorage or IndexedDB. |

### V15 Secure Coding and Architecture

Sections: 15.1 Secure Coding and Architecture Documentation; 15.2 Security Architecture and Dependencies; 15.3 Defensive Coding.

| Requirement | Level | Verdict | Evidence | Note |
| --- | --- | --- | --- | --- |
| 15.1.1 | 1 | met | `SECURITY.md:80-81`; `docs/sqa/devsecops-assessment.md:289`; `docs/sqa/devsecops-assessment.md:302-303` | SECURITY.md aims at a fix within 7 days for a critical and 30 days for a high finding. DSA-001 blocks any known NuGet vulnerability and any fixable high in an image, allows an exception for at most 30 days, and sets Dependabot updates and a monthly rebuild of the images. |
| 15.1.2 | 2 | met | `.github/workflows/ci.yaml:83-87`; `.github/actions/release-images/action.yml:135-141`; `.github/actions/release-images/action.yml:152-158`; `.github/dependabot.yml:11-18` | Restore runs in locked mode against the hashes of packages.lock.json with NuGetAudit on, Dependabot watches NuGet, images, compose files and Actions, and nfhotel-web and nfhotel-api get an SPDX SBOM attestation per release. There is no nuget.config, so the source is the default nuget.org. |
| 15.1.3 | 2 | open, BF-02 | `docs/adr/adr-0051-limits-of-the-booking-flow-and-the-caller-address.md:79-82`; `docs/adr/adr-0051-limits-of-the-booking-flow-and-the-caller-address.md:243-258`; `docs/adr/adr-0053-mail-from-the-application-through-a-relay.md:124-125`; `docs/adr/adr-0053-mail-from-the-application-through-a-relay.md:141` | ADR-0051 names the searches and booking requests over the web socket as the load to bound and sets limits per address and per mailbox; since MIL-089 its decision 8 bounds the circuits of the pages too, at most 50 open per caller, and nginx names its timeouts for a slow client. ADR-0053 bounds a send to 15 seconds, below the 30 seconds the web application waits for the API, and sets a fuse of 400 mails a day. No record says how the site stays up when many callers each hold circuits under the limit (BF-02). |
| 15.2.1 | 1 | met | `ops/security/trivyignore.yaml:54`; `docs/milestones/mil-090-scan-findings-of-19-november.md:20`; `docs/milestones/mil-090-scan-findings-of-19-november.md:26` | No component is past its time frame on 20 November: the Trivy exceptions lapse on 8 December, and CVE-2026-97054 in nine third-party images, found on 19 November, is planned in MIL-090. This holds only while MIL-090 keeps its dates. |
| 15.2.2 | 2 | met | `NFHotel.Api.Tests/BookingRequestApiTests.Ask_OverTheBookingRequestsOfTheAddress_IsRefusedByTheLimitPerAddress`; `NFHotel.Infrastructure.Tests/SmtpMailSenderTests.SendAsync_RelayNeverAnswersTheQuit_IsSentWithinTheBound`; `NFHotel.Api.Tests/BookingInputApiTests.PublicBooking_BodyAtAndOverTheLimit_IsReadOrRefusedWith413`; `NFHotel.Web.Tests/CircuitLimitTests.UseCircuitLimit_CallerWithFiftyOpen_RefusesTheNextWith429AndTheEvent`; `ops/tests/test-nginx.sh:172` | The limits per address and per mailbox, the fuse, the 15-second bound on a send and the 64 KB body limit are in the code and tested, and since MIL-089 so are the 50 open circuits per caller and the timeouts of nginx for a slow client. |
| 15.2.3 | 2 | open, BF-12 | `src/NFHotel.Web/Components/App.razor:8-9`; `src/NFHotel.Web/NFHotel.Web.csproj:39`; `src/NFHotel.Web/Program.cs:31` | The web image ships the 44 files of wwwroot/lib/bootstrap, source maps included, although App.razor says Bootstrap is not loaded. Program.cs registers the holiday calendar client, which nothing in src injects (BF-12). |
| 15.3.1 | 1 | met | `src/NFHotel.Application/Dto/Bookings/BookingReceiptDto.cs:16`; `src/NFHotel.Application/Dto/Bookings/BookingRequestReceiptDto.cs:8`; `src/NFHotel.Application/Dto/Guests/GuestSessionDto.cs:13` | The public routes return their own records: the receipt (reference, dates, nights, room size, status), the expiry of a booking request, and the guest token with its expiry. No entity and no guest data goes back. |
| 15.3.2 | 2 | n/a | - | The flow makes no backend call to an external URL: the API sends mail over SMTP to the relay, and the web application calls only the API at its configured address. |
| 15.3.3 | 2 | met | `NFHotel.Api.Tests/BookingInputApiTests.PublicBooking_UnknownField_IsRefused`; `NFHotel.Api.Tests/ValidationRulesTableTests.FlowTypes_WhenInspected_RefuseUnknownFieldsAndBoundTheirLists` | Every request type of the flow refuses unknown JSON fields (JsonUnmappedMemberHandling.Disallow), and the controllers pass request DTOs to the use cases and never bind an entity. |
| 15.3.4 | 2 | met | `NFHotel.Api.Tests/HttpCallerAddressTests.Of_CallWithoutTheServiceToken_IgnoresTheHeader`; `NFHotel.Web.Tests/ClientAddressMiddlewareTests.UseClientAddress_PeerOutsideTheStackWithTheHeader_TakesTheConnection`; `NFHotel.E2E.Tests/CallerAddressTests.CreateAccount_InACircuit_IsAuditedUnderTheAddressThatNginxSaw`; `ops/deploy/nginx/templates/public.conf.template:12-15` | nginx takes the address from CF-Connecting-IP on a listener that is not published on the host, the web application trusts X-Real-IP only from a peer inside the stack, and the API reads X-NFHotel-Client-Address only on a call with the service token. The limits and the security events use that address. |
| 15.3.5 | 2 | met | `NFHotel.Api.Tests/BookingInputApiTests.PublicBooking_RoomSizeMissingOrUnknown_IsRefused`; `NFHotel.Api.Tests/HostileInputTests.EveryEndpoint_HostileInput_IsRefusedWithoutAFaultOrALeak`; `src/NFHotel.Application/UseCases/Guests/GuestAccessService.cs:146-149` | Each field binds to a declared C# type, a missing or unknown enum value is refused, and the email of a sign-in is compared after normal form C and upper case with a fixed-time equality. JsonSerializerDefaults.Web still reads a number from a JSON string, which is lenient but not a type confusion. |
| 15.3.6 | 2 | met | `src/NFHotel.Web/wwwroot/js/booking-verify.js:22-37` | The only own script of the flow reads the token from the fragment into a string and writes it into a form field; it builds no object from input keys and merges nothing. |
| 15.3.7 | 2 | met | `src/NFHotel.Api/Controllers/BookingsController.cs:69-72`; `src/NFHotel.Api/Controllers/BookingsController.cs:111-112`; `src/NFHotel.Api/Controllers/BookingsController.cs:139-140`; `src/NFHotel.Api/Controllers/BookingsController.cs:155-156` | Each action of the flow names its source with FromQuery or FromBody, so a value in the query cannot stand in for one in the body. A JSON key given twice keeps its last value, as System.Text.Json does by default, and the rules check that same value. |

### V16 Security Logging and Error Handling

Sections: 16.1 Security Logging Documentation; 16.2 General Logging; 16.3 Security Events; 16.4 Log Protection; 16.5 Error Handling.

| Requirement | Level | Verdict | Evidence | Note |
| --- | --- | --- | --- | --- |
| 16.1.1 | 2 | open, BF-04 | `docs/wiki/Incidents.md:106-122`; `docs/wiki/Monitoring.md:70`; `ops/deploy/host/docker/daemon.json:4-8`; `ops/deploy/host/journald/nfhotel.conf:8-10` | The Log policy and Monitoring.md cover the container logs and Loki, but no inventory lists each layer (nginx access log, the console logs of the API and the web application, the journal, Loki) with its format, access and retention. The copies on the VM are bounded by size, not by the 14 days the documents name (BF-04). |
| 16.2.1 | 2 | open, BF-07 | `src/NFHotel.Api/Security/SecurityEvents.cs:133-135`; `src/NFHotel.Api/Security/InputRefusalEventFilter.cs:31-35` | The security events carry their name and, except input_refused, the caller address, but no request or trace id ties the lines of one request together, and input_refused has no address at all. The time of a line comes only from Docker, since the console format writes none (BF-07). |
| 16.2.2 | 2 | met | `docs/sqa/cis-checklist.md:164` | The VM keeps time with systemd-timesyncd, and the time of a log line is the UTC time that Docker records and Loki keeps; the applications write no time of their own. |
| 16.2.3 | 2 | met | `ops/deploy/alloy/config.alloy:74-79`; `ops/deploy/alloy/config.alloy:91-96`; `ops/deploy/alloy/config.alloy:106-110`; `docs/wiki/Monitoring.md:48` | The container logs and the journal stay on the VM and go to Loki through Alloy, as Monitoring.md and the Log policy say; Alloy sends logs nowhere else. |
| 16.2.4 | 2 | open, BF-07 | `ops/deploy/alloy/config.alloy:98-104`; `src/NFHotel.Api/appsettings.json:2-3` | The API and the web application write the default simple console format, two or more lines per entry, and Alloy has no multiline stage, so Loki stores the category and event id apart from the message. A security event can still be found by the name at the start of its message, but other entries, exceptions above all, are split over many lines (BF-07). |
| 16.2.5 | 2 | met | `NFHotel.Api.Tests/SecurityEventValuesTests.Events_EveryLogMessage_TakesNoEmail`; `NFHotel.Api.Tests/SenderLimitApiTests.Booking_Made_WritesOneEventWithTheAddressAndNoEmail`; `docs/adr/adr-0051-limits-of-the-booking-flow-and-the-caller-address.md:225` | No event writes a password, a token, a reference, a passport number or an email, whole, masked or hashed (ADR-0051 decision 6, third version, MIL-089). The caller address is the one personal value, kept for the 14 days of Loki. The five alerts of CodeQL on the masked email close with this change. |
| 16.3.1 | 2 | open, BF-07 | `src/NFHotel.Api/Security/BookingFlowEventFilter.cs:40-56`; `src/NFHotel.Application/UseCases/Guests/GuestAccessService.cs:117`; `src/NFHotel.Api/Security/GuestAuthentication.cs:141`; `src/NFHotel.Api/Security/ApiTokenMiddleware.cs:45-52` | A refused guest sign-in (401) and its limit (429) are logged, but a successful sign-in is not, a refused or expired guest token is only marked in HttpContext.Items, and a call with a wrong service token is answered 401 without a log line (BF-07). |
| 16.3.2 | 2 | open, BF-07 | `src/NFHotel.Api/Security/RequiresGuestAttribute.cs:36-50`; `src/NFHotel.Api/Security/GuestAuthentication.cs:109-110`; `src/NFHotel.Api/Security/RequiresPermissionAttribute.cs:79-80` | RequiresGuest refuses a missing or refused guest token with 401 and a staff token with 403, and RequiresPermission refuses staff calls the same way, all without a log line or a counter, so repeated tries leave no trace (BF-07). |
| 16.3.3 | 2 | met | `NFHotel.Api.Tests/BookingInputApiTests.Refusal_ByTheHandlerOfExceptions_WritesTheSecurityEvent`; `NFHotel.Api.Tests/SenderLimitApiTests.Booking_Made_WritesOneEventWithTheAddressAndNoEmail`; `NFHotel.Api.Tests/BookingRequestApiTests.Ask_RelayDown_Answers503LeavesNoRequestAndWritesTheEvent` | The events that ADR-0051 and ADR-0052 define (input_refused, limit_refused, guest_lookup_failed, verification_failed, public_booking_made, booking_request_made and the mail events) are written, and all but input_refused are counted in nfhotel_security_events_total; since MIL-089 the web application writes and counts its refusals of the circuits as well. The gaps in who and in authentication events are under 16.2.1, 16.3.1 and 16.3.2. |
| 16.3.4 | 2 | met | `NFHotel.Api.Tests/BookingRequestApiTests.Ask_RelayDown_Answers503LeavesNoRequestAndWritesTheEvent`; `NFHotel.Infrastructure.Tests/SmtpMailSenderTests.SendAsync_RelayWithoutStartTls_IsAFailureOfTlsBeforeTheSignIn`; `src/NFHotel.Api/Program.cs:91-113` | A mail that fails, a failure of TLS to the relay included, writes mail_not_sent with the reason, and the fuse writes mail_fuse_blown. An unexpected exception is logged by the exception handler middleware before the handler in Program.cs answers 500. |
| 16.4.1 | 2 | met | `src/NFHotel.Api/Security/SecurityEvents.cs:133-185`; `src/NFHotel.Api/Security/InputRefusalEventFilter.cs:31-35` | The events write only fixed names, the HTTP method, route templates, status and error codes and normalised addresses, none of which can carry a line break or a separator; since MIL-089 no email. The simple console formatter does not escape line breaks itself, so an event that adds free text would need the same care. |
| 16.4.2 | 2 | open, BF-05 | `ops/deploy/monitoring/loki/config.yml:3`; `ops/deploy/monitoring/docker-compose.yml:87`; `ops/deploy/monitoring/loki/config.yml:52-53`; `docs/wiki/Incidents.md:108` | Loki runs with auth_enabled false, so anything that reaches its port can query and push log lines, and ask to delete them, without a credential (BF-05). |
| 16.4.3 | 2 | open, BF-05 | `ops/deploy/host/nftables/nfhotel.nft:64`; `ops/deploy/alloy/docker-compose.yml:100-107`; `ops/deploy/monitoring/loki/config.yml:3` | The logs go to Loki on the monitoring host, a separate system, but a container of the VM with a route out can reach it, and Loki takes pushes and queries without a credential. A breach of the web container could therefore read or forge log lines and ask for their deletion (BF-05). |
| 16.5.1 | 2 | met | `NFHotel.Api.Tests/HostileInputTests.EveryEndpoint_HostileInput_IsRefusedWithoutAFaultOrALeak`; `src/NFHotel.Api/Program.cs:91-113`; `src/NFHotel.Web/Program.cs:76` | The API answers an unexpected error with 500 and the code api.unexpected_error only, and the web application shows its error page outside Development. The hostile-input test fails on any answer that names an exception, a stack frame, a .NET type, a setting or the token. |
| 16.5.2 | 2 | met | `NFHotel.Api.Tests/BookingRequestApiTests.Ask_RelayDown_Answers503LeavesNoRequestAndWritesTheEvent`; `NFHotel.Application.Tests/BookingRequestServiceTests.VerifyAsync_ReceiptMailRefused_KeepsTheBooking`; `NFHotel.Infrastructure.Tests/SmtpMailSenderTests.SendAsync_RelayNeverAnswersTheQuit_IsSentWithinTheBound` | When the relay is down a booking request is refused with 503 and nothing is stored, a refused receipt mail keeps the booking, and a relay that never answers is cut off within the 15-second bound. |
| 16.5.3 | 2 | met | `NFHotel.Application.Tests/BookingRequestServiceTests.AskAsync_RelayDown_TellsTheGuestAndLeavesNothing`; `NFHotel.Api.Tests/BookingRequestApiTests.Verify_SameTokenTwice_SecondGetsTheAnswerOfALinkThatDoesNotWork`; `NFHotel.Infrastructure.Tests/BookingVerificationRaceTests.VerifyAsync_TwentyAtOnceForTheLastRoom_OneBooksAndNineteenAreToldTheRoomIsGone`; `src/NFHotel.Api/Security/ApiToken.cs:17-19` | A booking request is stored only after its mail went, a link works once even under a race, and the API does not start without a service token of the minimum length. |

Verdicts: 92 met, 49 n/a, 6 accepted, 32 open, of 179.

## 4. Abuse cases

The eleven cases of [MIL-089] decision 2. "Tests" names tests that fail when the control is taken out; the evidence
folder holds the transcripts of five of them (see below).

| Case | Verdict | Controls | Tests | Residual |
| --- | --- | --- | --- | --- |
| 1 Working out a booking (guessing a reference) | Fully controlled | The reference is a random version 4 UUID; the guest sign-in counts each try per reference and per address before it looks anything up, and answers every failure with one refusal; the email is compared in fixed time ([ADR-0049]) | `NFHotel.Api.Tests/GuestAccessTests.SignIn_TwentyFirstFailedTryFromOneAddress_ReturnsTooManyRequests`; `NFHotel.Api.Tests/GuestAccessTests.Refusal_EveryKindOfFailedSignIn_HasTheStatusAndTextOfAWrongEmail`; `NFHotel.Domain.Tests/BookingReferenceTests.New_TenThousandInARow_AreAllDifferentVersion4UuidsWithTheRfcVariant`; `NFHotel.E2E.Tests/GuestBookingTests.SignIn_OldBookingNumberWithItsEmail_GetsTheOneRefusal` | The counts live in the memory of the API, so a restart resets them. The fixed-time compare has no test, because a test of timing would be flaky |
| 2 Trying emails against a reference | Fully controlled | Five failed tries per reference in 15 minutes, counted before the lookup; a full store refuses a new key instead of dropping a used-up one ([ADR-0049] decision 6); the one refusal | `NFHotel.Api.Tests/GuestAccessTests.SignIn_SixthTryForOneReference_ReturnsTooManyRequests` (transcript); `NFHotel.E2E.Tests/GuestBookingTests.FindBooking_ReferenceWithAnotherEmailThenItsOwn_SignsInOnlyWithItsOwn` | Anyone who knows a reference can keep its guest out for 15 minutes (availability only). After about 50,000 failed tries in 15 minutes from about 2,500 networks of /64, the full store refuses every new reference: it fails closed, as [ADR-0049] decision 6 decided |
| 3 Taking the rooms | Partly controlled, BF-01 | Five booking requests an hour and 20 a day per address (an IPv6 address by its /64); three coming public bookings per mailbox, counted under the lock `public_booking`; at most 30 nights; with the verification on, a request holds no room until its link is used; the alerts `PublicBookingSurge` and `BookingFlowRefusals` | `NFHotel.Api.Tests/SenderLimitApiTests.Booking_SixthInAnHour_IsRefusedWithTheCodeAndTheTimeToWait`; `NFHotel.Api.Tests/SenderLimitApiTests.Booking_TwentyFirstInADay_IsRefusedByTheLimitOfTheDay`; `NFHotel.Application.Tests/PublicBookingLimitTests.CreatePublicAsync_FourthComingBookingOfOneEmail_IsRefused`; the burst of the load test (section 6) | With the verification off, as in production until 2026-11-22, an invented email per booking passed the limit per mailbox. With it on, every three bookings need a mailbox that gets the link, so a script with throwaway mailboxes can still hold rooms from one home connection, under the alert (BF-01) |
| 4 Filling a stranger's mailbox | Fully controlled; live since 2026-11-22, when the verification was turned on | Three verification mails an hour and five a day per mailbox, counted from the stored mails under the lock `booking_request`; one open request per mailbox; the fuse of 400 mails a day; the mail holds no typed text | `NFHotel.Application.Tests/BookingRequestServiceTests.AskAsync_FourthMailInAnHour_IsRefusedAndKeepsTheThirdRequest`; `NFHotel.Application.Tests/BookingRequestServiceTests.AskAsync_SixthMailInADay_IsRefused`; `NFHotel.Application.Tests/BookingRequestServiceTests.AskAsync_Request_CountsAndWritesUnderTheLock`; `NFHotel.Api.Tests/BookingRequestApiTests.Ask_FuseUsedUp_Answers503AndTriesNoMail`; `NFHotel.Infrastructure.Tests/BookingRequestPersistenceTests.RunUnderLockAsync_TwoAtOnce_RunOneAfterTheOther` | About 20 addresses with 80 mailboxes use up the fuse, after which no guest gets a verification mail that day (BF-01; [ADR-0053] accepts it with the alert `MailVolumeHigh`) |
| 5 Using a verification link twice | Fully controlled; live since 2026-11-22, when the verification was turned on | The link is claimed by one `DELETE ... RETURNING` on the SHA-256 of its token; the token has 256 random bits, lapses after 30 minutes and travels in the fragment of the link | `NFHotel.Infrastructure.Tests/BookingRequestPersistenceTests.ClaimOpenAsync_OpenRequest_IsClaimedOnceAndDeleted`; `NFHotel.Infrastructure.Tests/BookingRequestPersistenceTests.ClaimOpenAsync_TenPressesAtOnce_ClaimTheRequestOnce`; `NFHotel.E2E.Tests/BookingVerificationTests.Verify_UsedOrUnknownLink_GetsTheOneAnswer` | None found |
| 6 The race for the last room | Fully controlled | The exclusion constraint `ex_booking_room_period` refuses two overlapping bookings of one room; the use case then tries the next free room; the choice and the save run under the lock `public_booking`, which also guards the count per mailbox | `NFHotel.Infrastructure.Tests/ConflictPathTests.SaveChangesAsync_TwoParallelCreationsOfTheSamePeriod_ExactlyOneSucceeds`; `NFHotel.Infrastructure.Tests/BookingVerificationRaceTests.CreatePublicAsync_TwentyAtOnceForTheLastRoomWithTheVerificationOff_OneBooksAndNineteenAreToldTheRoomIsGone`; `NFHotel.Application.Tests/BookingRequestServiceTests.CreatePublicAsync_FreeRoom_ChoosesAndSavesUnderThePublicBookingLock` (new in MIL-089, transcript); `NFHotel.Application.Tests/BookingRequestServiceTests.CreatePublicAsync_LockNotTaken_BooksAndSavesNothing` (new in MIL-089) | None found |
| 7 Writing the caller's address | Fully controlled from outside the stack; BF-11 inside it | nginx takes `CF-Connecting-IP` only from the docker ranges and overwrites `X-NFHotel-Client-Address`; the web application takes that header only from a peer in the stack networks; the API takes it only with the service token | `NFHotel.Api.Tests/HttpCallerAddressTests.Of_CallWithoutTheServiceToken_IgnoresTheHeader`; `NFHotel.Web.Tests/ClientAddressMiddlewareTests.UseClientAddress_PeerOutsideTheStackWithTheHeader_TakesTheConnection` (transcript); `ops/tests/test-nginx.sh` (the case on the address that the caller wrote); `NFHotel.E2E.Tests/CallerAddressTests.CreateAccount_InACircuit_IsAuditedUnderTheAddressThatNginxSaw` | A container inside the stack can name an address (BF-11); [ADR-0028] names the trust of the docker ranges as a negative consequence |
| 8 A field that the request type does not have | Fully controlled | Every request type of the flow refuses a JSON member it does not have; the booking of a guest and its room come from the token and the stored booking, never from the body | `NFHotel.Api.Tests/BookingInputApiTests.PublicBooking_UnknownField_IsRefused` (transcript); `NFHotel.Api.Tests/ValidationRulesTableTests.FlowTypes_WhenInspected_RefuseUnknownFieldsAndBoundTheirLists`; `NFHotel.Api.Tests/GuestAccessTests.Reschedule_GuestRoute_KeepsTheRoomOfTheBooking` | None found |
| 9 Markup and query text in a field | Fully controlled | The name rules of the domain; the input guard of the API; parameterised queries; Blazor encodes what it shows; the mails are plain text and hold no typed text | `NFHotel.Domain.Tests/GuestFieldFormatTests.PersonName_OtherCharacters_IsRefused`; `NFHotel.Web.Tests/BookingPageTests.Details_HostileStrings_StayTextInTheFields`; `NFHotel.Infrastructure.Tests/BookingPublicCountTests.CountComingPublicBookingsAsync_EmailWithAPatternCharacter_MatchesOnlyItself`; `NFHotel.Api.Tests/HostileInputTests.EveryEndpoint_HostileInput_IsRefusedWithoutAFaultOrALeak` | The scan of the site does not drive the form of `/book`, which runs in a circuit, so it is no evidence for those fields |
| 10 A body that is too large | Fully controlled | nginx refuses a body over 64 KB with 413; the API refuses a declared length over 64 KB before it reads the body, and bounds the read of a chunked one | `ops/tests/test-nginx.sh` (the case of 64 kilobytes and one byte); `NFHotel.Api.Tests/BookingInputApiTests.PublicBooking_BodyAtAndOverTheLimit_IsReadOrRefusedWith413`; `NFHotel.Api.Tests/BookingInputApiTests.PublicBooking_ChunkedBodyOverTheLimit_IsRefusedWith413ByTheServer` | The size of a message of a circuit is the default of SignalR (32 KB), which the code does not set (a note of BF-12) |
| 11 Many slow requests | Partly controlled, BF-02 | At most 50 open circuits per caller in the web application, an IPv6 address by its /64: the next request to `/_blazor` gets 429, the event `limit_refused` of the limit `circuits` and its count, which `BookingFlowRefusals` adds to the refusals of the API ([ADR-0051] decision 8, new in MIL-089); nginx times out a client that takes over 60 seconds for its headers or between two reads or writes, named in its configuration since MIL-089; the defaults of Kestrel; Cloudflare ends the connection of the caller, but how it treats slow clients is neither set nor tested in the repository | `NFHotel.Web.Tests/CircuitLimitTests.WebApplication_CallerAtTheLimit_GetsNoCircuitButThePage` (new in MIL-089, transcript); `NFHotel.Web.Tests/CircuitLimitTests.UseCircuitLimit_CallerWithFiftyOpen_RefusesTheNextWith429AndTheEvent`; `NFHotel.Web.Tests/CircuitLimitTests.TryOpen_ManyAtOnce_CountsExactlyTheLimitOfEachCaller`; `ops/tests/test-nginx.sh` (the case of the timeouts of the public listener) | Many addresses, or an IPv6 network of 56 bits with its 256 networks of 64 bits, can still hold 50 circuits each; a web socket stays open for up to an hour, and the alert sees only refusals (BF-02) |

**Transcripts (criterion 2).** `docs/sqa/evidence/mil-089/without-their-control.txt` holds five tests, each run with its
control taken out of the source and then with it in place: case 8, case 7 (at the web layer), case 2, case 6 and case
11. Each failed without its control and passed with it. The three that criterion 2 asks for are cases 8, 7 and 2, as
the review chose them (RC-438); cases 6 and 11 show controls that are new in MIL-089.

## 5. Scans

On main at `3cfe3ab`, the commit that holds [MIL-085] to [MIL-088]:

- **The full scan of the site** (ZAP, run 44730118249): 0 High and 1 Medium, rule 40025 (proxy disclosure, 5
  instances). It is not new: it was open before [MIL-085], and MIL-090 handles it. The rest is Informational (rules
  10015, 10027, 10031, 10049, 10058, 10109 and 90027).
- **The scan of the API** (the same run): 0 High, 0 Medium, 1 Low (rule 2, private address disclosure, kept at WARN
  in `ops/security/zap-rules.tsv` with its reason) and two Informational rules (100000, the scan's own refused input;
  10111, the sign-in found as intended).
- **The code scan** (CodeQL, run 44692343647): five open alerts, 677 to 681, `cs/exposure-of-sensitive-information`
  (Medium), all in `src/NFHotel.Api/Security/BookingFlowEvents.cs`: the email that the events of [MIL-087] and
  [MIL-088] wrote, masked. They are closed by the code of this milestone, not dismissed: no event takes an email any
  more, and the limit per email is named `mailbox` (section 9). The scan of the pull request, and of main after the
  merge, is read in the gate (RC-441).
- **The audit of the packages** (the restore of CI with the audit of every package, direct and transitive, at level
  low, warnings as errors, run 44692344259): passed.
- **The gate of the images** (the release, run 44694949711): passed for the six images of the release. The scan of
  the release in production (run 44749057042, 2026-11-20) passed too.
- **The rules of the scan**: nine rules that lapsed on 2026-12-08 were read again against the scan and renewed to
  2026-12-18 with their reasons ("checked again in MIL-089"). No rule lapses within 14 days.

## 6. Load test

`ops/load/nfhotel.js` and `ops/scripts/load-test.sh` now take the booking flow: searches for free rooms and booking
requests within the limits, each virtual guest from an address of its own, and then a burst from one address over the
limits. The workflow `.github/workflows/load-test.yaml` runs it twice, with the verification of a booking off and on.
The thresholds are those of today: 95 percent of the answers within 1,000 ms and fewer than 5 percent failed.

| Run | Requests of the guests | Failed | 95 percent within | The burst from one address |
| --- | --- | --- | --- | --- |
| Verification off, as production ran until 2026-11-22 | 3,445 | 0 | 15.3 ms | 65 answers (the 60 searches and the 5 bookings that the limits allow) and 23 refusals with 429 and the code of the limit; no answer of the 500 kind |
| Verification on, as production runs since 2026-11-22 | 3,445 | 0 | 19.9 ms | The same: 65 answers, 23 refusals, no answer of the 500 kind |

The runs were made on 2026-11-20 on a throwaway stack with the numbers of production; every mail went to the mail
catcher of that stack. Evidence: `docs/sqa/evidence/mil-089/load-verification-off.txt` and
`docs/sqa/evidence/mil-089/load-verification-on.txt`.

## 7. Findings 3 to 6 of MIL-089

| Finding of [MIL-089] | Verdict | Rests on | Reason and tests | Residual |
| --- | --- | --- | --- | --- |
| 3 The web socket path `/_blazor` is not behind the Access check of `/admin` | Accepted | [ADR-0029] decision 4, [ADR-0005] | `/_blazor` must stay public, because the booking page and the neighbourhood map run in a circuit. A circuit starts only the components whose markers the server rendered into a page, and the markers of the back office are rendered only under `/admin`, which needs Access and then the staff cookie. Every staff action is a call of the API with the staff token. Tests: `ops/tests/test-nginx.sh` (the back office without the Access token is refused); `NFHotel.E2E.Tests/BackOfficeAccessTests.AdminPage_WithoutSession_RedirectsToTheLogin` | An admin circuit that is already open outlives the Access session, bounded by the staff token and its stamp check. [ADR-0029] does not name `/_blazor` (BF-12) |
| 4 The two routes of the mobile app are open on the public listener | Finding for S01, Medium, outside the flow: BF-03 | [ADR-0015] | The routes are open by decision, but a staff password alone reaches more than the decision lists, and the throttle of the sign-in is shared with the back office | See BF-03 |
| 5 The guest token is signed with the staff key and stays valid after a cancellation | Accepted | [ADR-0047] decisions 2, 5 and 7 and its negative consequence | A guest token cannot pass a staff route, and after a cancellation it reads only the receipt and can neither move nor cancel the booking. Tests: `NFHotel.Api.Tests/GuestAccessTests.StaffEndpoint_GuestToken_ReturnsForbidden`; `NFHotel.Api.Tests/GuestAccessTests.GuestRoute_StaffToken_ReturnsForbidden`; `NFHotel.Application.Tests/GuestBookingServiceTests.GetAsync_CancelledBooking_ReadsTheReceiptWithTheStatusCancelled` and `NFHotel.Application.Tests/GuestBookingServiceTests.RescheduleAsync_CancelledBooking_KeepsTheCodeOfTheBookingAndTheDates` (both new in MIL-089); `NFHotel.Application.Tests/GuestBookingServiceTests.CancelAsync_CancelledBooking_KeepsTheCodeOfTheBooking` | One key signs both kinds of token (BF-08); a copied guest token cannot be ended before its 2 hours (BF-06) |
| 6 Two operations of the API need only the service token | Accepted | [ADR-0013], [ADR-0014], [ADR-0004] | Neither route is reachable from outside the stack, and what they give a holder of the service token is little beside what the token gives anyway. Tests: `NFHotel.Api.Tests/EndpointProtectionTests.Controllers_EveryAction_RequiresAPermissionOrIsPublic`; `NFHotel.Api.Tests/BenchmarkEndpointTests.PostResult_WithoutTheServiceToken_Returns401AndStoresNothing`; `ops/tests/test-nginx.sh` (another API route is not public) | The reason that [ADR-0014] gives for the open read is out of date: the market analysis reads the default from the database, and the route's only callers hold a staff token. The token shared with the benchmark service is BF-11 |

## 8. Findings

Severity: the scale of section 1, from the likelihood (L) and the impact (I) of each finding. Every finding is open,
its owner is S01, and its issue holds the proposed fix. None is built in [MIL-089] ([MIL-089] decision 3), but BF-02
for one address, which was High as it stood (section 9). BF-01 was High too while the verification by email was
off, and is rated here as production runs since 2026-11-22, with the verification on.

| Finding | L | I | Severity | In the flow | Requirements and abuse cases | Issue | Owner |
| --- | --- | --- | --- | --- | --- | --- | --- |
| BF-01 A few addresses can hold the rooms, or use up the mails of a day | Medium | Medium | Medium | Yes | 2.4.1; cases 3 and 4 | [#867] | S01 |
| BF-02 Many addresses can still hold the circuits of the pages; one address is held at 50 since MIL-089 | Medium | Medium | Medium | Yes | 2.4.1, 15.1.3; case 11 | [#868] | S01 |
| BF-03 The routes of the mobile app let a staff password alone reach every rooms action from the internet; the shared throttle lets anyone lock staff out; the throttle per address counts an IPv6 address in full | Low | High | Medium | No (finding 4) | None | [#869] | S01 |
| BF-04 Personal data of the booking flow has no retention, no classification and no log inventory; the log copies on the VM are bounded by size | Low | High | Medium | Yes | 14.1.1, 14.1.2, 14.2.4, 16.1.1 | [#870] | S01 |
| BF-05 The log store takes pushes, queries and deletions without a credential | Low | High | Medium | No (the monitoring stack), but it keeps the events of the flow | 16.4.2, 16.4.3 | [#871] | S01 |
| BF-06 A guest session cannot be ended early, and an idle one stays open | Low | Medium | Low | Yes | 7.1.1, 7.1.2, 7.3.1, 7.4.5, 7.5.2, 14.3.1 | [#872] | S01 |
| BF-07 The security log of the flow misses some events and cannot tie the lines of one request | Medium | Low | Low | Yes | 16.2.1, 16.2.4, 16.3.1, 16.3.2 | [#873] | S01 |
| BF-08 The keys have no written life cycle, and the passport key cannot be replaced | Low | Medium | Low | Partly | 11.1.1, 11.1.2, 11.2.2 | [#874] | S01 |
| BF-09 The booking pages share an origin with the back office, and the web socket does not check where it comes from | Low | Medium | Low | Yes | 3.3.1, 3.3.3, 3.5.4, 4.4.2 | [#875] | S01 |
| BF-10 HSTS lasts 30 days and does not cover the subdomains | Low | Medium | Low | Yes | 3.4.1 | [#876] | S01 |
| BF-11 Callers inside the stack can name the address of a caller | Low | Medium | Low | Yes | 8.2.1; case 7; finding 6 | [#877] | S01 |
| BF-12 Hardening notes: the headers of the answers of nginx, `base-uri 'self'`, charsets and unused files, and four observations | Low | Low | Informational | Yes | 3.4.3, 3.4.4, 3.4.6, 4.1.1, 15.2.3; case 10; the notes of 6.4.3, 11.3.3 and 11.6.1 | [#878] | S01 |

BF-12 also holds four observations that are no verdict of their own: how staff check a caller who lost the
reference is not written down (6.4.3); no associated data binds a passport ciphertext to its row (11.3.3); the size of
a message of a circuit is the default of SignalR, 32 KB, which the code does not set (case 10); and the mail sender
turns the check of the revocation of the certificate of the relay off without a comment
(`src/NFHotel.Infrastructure/Mail/SmtpMailSender.cs:190`, 11.6.1).

Counts: 5 Medium, 6 Low, 1 Informational; 10 in the flow or partly, 2 outside it. No open finding is High or
Critical; BF-02 was High before its fix in this milestone.

## 9. Fixed in MIL-089

- **The security events hold no email** ([ADR-0051] decision 6, third version). The events of the flow wrote the email
  masked to its first character and its domain; they now write the caller address and no email, whole, masked or
  hashed, and the limit per email is named `mailbox` after what it counts. This is a stand-in decision that the
  planning session made as S01 on 2026-11-20, so that alerts 677 to 681 of the code scan close through the code; S01
  can overrule it (RC-439). Files: `src/NFHotel.Api/Security/SecurityEvents.cs`,
  `src/NFHotel.Api/Security/BookingFlowEvents.cs`, `src/NFHotel.Api/Security/BookingFlowEventFilter.cs`,
  `src/NFHotel.Api/Security/ApiBookingRequestEvents.cs`, `src/NFHotel.Application/Ports/IBookingRequestEvents.cs` and
  `src/NFHotel.Application/Ports/LimitNames.cs`. Tests: `NFHotel.Api.Tests/SecurityEventValuesTests.Events_EveryLogMessage_TakesNoEmail`;
  `NFHotel.Api.Tests/SenderLimitApiTests.Booking_Made_WritesOneEventWithTheAddressAndNoEmail`; the check of the
  throwaway stack in `ops/tests/check-e2e-stack.sh` that no event of the flow holds an email. Requirements 1.3.3,
  16.2.5 and 16.4.1 are met by it.
- **The circuits of one caller** (abuse case 11 and BF-02 for one address; [ADR-0051] decision 8, a stand-in decision
  that S01 can overrule). The web application keeps at most 50 circuits of the pages open per caller address, an IPv6
  address by its /64, and refuses the next request to `/_blazor` with 429, the event `limit_refused` of the limit
  `circuits` and its count; the alert `BookingFlowRefusals` now counts the refusals of the web application with those
  of the API. nginx names its client timeouts of 60 seconds on the public listener, so a test holds them. As the flow
  stood, BF-02 was High by the scale of section 1: likelihood High, because one address with a script was enough and
  no alert saw it; impact Medium, because the pages and the back office stop for every guest and every member of
  staff until the circuits end, while no data is read or lost and a restart ends it. Criterion 1 of [MIL-089] does
  not allow that open. What remains, many addresses, is Medium. Files: `src/NFHotel.Web/Security/CircuitLimit.cs`,
  `src/NFHotel.Web/Security/CircuitLimitMiddleware.cs`, `src/NFHotel.Web/Program.cs`,
  `ops/deploy/nginx/templates/public.conf.template` and `ops/deploy/monitoring/prometheus/rules/security.yml`. Tests:
  the twelve of `NFHotel.Web.Tests/CircuitLimitTests`, the case of the timeouts in `ops/tests/test-nginx.sh`, and the
  test of the alert in `ops/deploy/monitoring/prometheus/rules-test.yml`.
- **The referrer of every page** (3.4.5): `Referrer-Policy: same-origin` on every answer of the web application, and
  `no-referrer` kept on `/booking/verify`, where the token is. Test: `NFHotel.Web.Tests/SecurityHeadersTests.ReferrerPolicy_Value_SendsNoAddressToAnotherSite`.
- **The lock of the public booking** (case 6): two new tests show that the choice of the room and every save run under
  the lock `public_booking`, which also guards the count per mailbox, and that nothing is booked or saved when the lock
  is not taken: `NFHotel.Application.Tests/BookingRequestServiceTests.CreatePublicAsync_FreeRoom_ChoosesAndSavesUnderThePublicBookingLock`
  and `NFHotel.Application.Tests/BookingRequestServiceTests.CreatePublicAsync_LockNotTaken_BooksAndSavesNothing`.
- **A guest token after the cancellation** (finding 5): the two tests of section 7.
- **The ledger of the business rules** (2.2.1): the row of the email of the guest sign-in now gives its real bound and
  its errors, and the limit per email is named `mailbox` (`docs/refactor/BusinessRules.md`). The design documents of
  the use case of the verification (its sequence diagram, its operation contracts and the class diagram) now give the
  events no email and name the limit `mailbox`.
- **The base of the page addresses** (3.4.3): the policy keeps `base-uri 'self'`, because the pages of Blazor need a
  `base` element; the code now says why and the test pins it. The requirement asks for `'none'`, so it stays open as
  part of BF-12.
- **Two weighings of [ADR-0052]** (6.5.5 and 6.6.3): decision 2 now weighs the 30 minutes of the link against the 10
  of the requirement, and decision 10 says why the route of the verification has no limit per caller. Both are
  stand-in judgements that S01 can overrule.
- **The load test** takes the booking flow (section 6), and **the rules of the scan** are renewed (section 5).
- **The wiki page** Booking-Flow-Protection tells the team how the flow is protected and what to do at each of its
  alerts.

## 10. Remaining risks

- One home connection with a script and throwaway mailboxes can make 40 public bookings a day, 10 an hour, which the
  alert `PublicBookingSurge` does not report (BF-01); since 2026-11-22 the verification makes every three of them
  need a mailbox whose link the script opens. With the verification off again, invented emails would do, and BF-01
  would be High.
- About 20 addresses can use up the mails of a day (BF-01), which [ADR-0053] accepts with the alert `MailVolumeHigh`.
- Many addresses, or an IPv6 network of 56 bits, can still hold the circuits of the pages, 50 for each address or
  network of 64 bits, and the alert sees only refusals (BF-02). nginx may run out first: its configuration keeps the
  default of the image, 1,024 connections per worker process, and a web socket through it holds two, so about ten
  callers at the limit fill one worker (worked out, not measured).
- The counts of the limits live in the memory of the API: a restart resets them ([ADR-0051]).
- Anyone who knows a reference can keep its guest out for 15 minutes, and a flood from thousands of networks fills the
  store of sign-in counts, which then refuses every new reference ([ADR-0049] decision 6). Both are availability only.
- A container inside the stack can name the address of a caller (BF-11).
- The log tells the refusals apart by the address and the time, not by the email, and a refused request is not
  stored: staff can list the mailboxes at their limit at the time, but cannot tie a refusal to one ([ADR-0051],
  consequences).
- The routes of the mobile app (BF-03) and the log store (BF-05) lie outside the flow and stay as they are.
- This audit is not independent (section 1).

## 11. Actions for S01

1. Approve the run of continuous deployment for the merge of [MIL-089]; the check on the live site of its criterion 8
   follows.
2. Keep the verification by email on, as it is since 2026-11-22: with it off, BF-01 is High again (section 8).
3. Decide each finding: a milestone, a later date or an acceptance, starting with the five Medium ones (BF-01 to BF-05).
4. BF-03: decide whether the routes of the mobile app stay on the public listener before the app is in use.
5. BF-04: set how long a guest and a booking are kept after the stay.
6. BF-10: set the length of HSTS, open since MIL-066.
7. Keep or overrule the stand-in decisions of this audit: no email in the security events ([ADR-0051] decision 6), at
   most 50 circuits per caller (decision 8), the 30 minutes of the link against the 10 of ASVS 6.5.5 ([ADR-0052]
   decision 2), no limit per caller on the route of the verification (decision 10, ASVS 6.6.3), and the scale of
   severity (section 1).
8. Publish the release v0.3.0 when wanted ([MIL-089] decision 6).

---

[MIL-089]: ../milestones/mil-089-the-booking-flow-attacked-and-measured.md
[MIL-085]: ../milestones/mil-085-a-booking-reference-that-cannot-be-guessed.md
[MIL-086]: ../milestones/mil-086-booking-input-checked-by-one-set-of-rules.md
[MIL-087]: ../milestones/mil-087-limits-on-what-one-sender-can-ask.md
[MIL-088]: ../milestones/mil-088-a-booking-verified-by-email.md
[ADR-0047]: ../adr/adr-0047-guest-login.md
[ADR-0049]: ../adr/adr-0049-booking-reference.md
[ADR-0050]: ../adr/adr-0050-where-the-rules-of-the-booking-input-live.md
[ADR-0051]: ../adr/adr-0051-limits-of-the-booking-flow-and-the-caller-address.md
[ADR-0052]: ../adr/adr-0052-a-booking-takes-a-room-once-its-email-is-verified.md
[ADR-0053]: ../adr/adr-0053-mail-from-the-application-through-a-relay.md
[ADR-0004]: ../adr/adr-0004-database-api.md
[ADR-0005]: ../adr/adr-0005-backoffice-login.md
[ADR-0013]: ../adr/adr-0013-benchmark-results.md
[ADR-0014]: ../adr/adr-0014-default-ai-model.md
[ADR-0015]: ../adr/adr-0015-mobile-app-staff-token-access.md
[ADR-0028]: ../adr/adr-0028-public-access-through-cloudflare-tunnel.md
[ADR-0029]: ../adr/adr-0029-back-office-behind-cloudflare-access.md
[SAR-001]: ./security-audit-2026-11.md
[#867]: https://github.com/NF-Hotel/NFHotel/issues/867
[#868]: https://github.com/NF-Hotel/NFHotel/issues/868
[#869]: https://github.com/NF-Hotel/NFHotel/issues/869
[#870]: https://github.com/NF-Hotel/NFHotel/issues/870
[#871]: https://github.com/NF-Hotel/NFHotel/issues/871
[#872]: https://github.com/NF-Hotel/NFHotel/issues/872
[#873]: https://github.com/NF-Hotel/NFHotel/issues/873
[#874]: https://github.com/NF-Hotel/NFHotel/issues/874
[#875]: https://github.com/NF-Hotel/NFHotel/issues/875
[#876]: https://github.com/NF-Hotel/NFHotel/issues/876
[#877]: https://github.com/NF-Hotel/NFHotel/issues/877
[#878]: https://github.com/NF-Hotel/NFHotel/issues/878
[00d609b]: https://github.com/NF-Hotel/NFHotel/commit/00d609b1450afdb2d73df7a8d5dd9b1de2e1c8da
