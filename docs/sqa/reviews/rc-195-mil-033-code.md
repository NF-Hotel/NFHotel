# RC-195 Review of the code of MIL-033 (security headers, antiforgery cookie, nginx version)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-195 |
| CrossReference | [MIL-033], [ADR-0006], [QC-CS-001], [QC-SH-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-24 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [fc3da26] |

---

## Artifact Under Review

- **Instances reviewed:** the change of tasks 1 to 6 of MIL-033 (#269 to #274):
  - `src/NFHotel.Web/Security/SecurityHeaders.cs` and `AntiforgeryCookie.cs` (new), and `Program.cs`;
  - `Components/App.razor` and `Components/Shared/ReconnectModal.razor` (new);
  - `wwwroot/app.css` and `wwwroot/select-arrow.svg` (new);
  - `ops/deploy/nginx/templates/00-server-tokens.conf.template` (new);
  - the tests: `SecurityHeadersTests.cs` (new), `HttpsRequirementTests.cs`, the end-to-end `BrowserTest.cs` and `ops/tests/test-nginx.sh`.
- **Checklists used:** [QC-CS-001] and [QC-SH-001].
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).

## Evidence

| Check | Result |
| --- | --- |
| Web tests | 241 passed locally, among them the new tests: headers on a page, a static file and a redirect; one policy; no inline code on `/book` and `/admin/login`; the antiforgery cookie with and without `Tls:Require` |
| CI of the pull request | 32 checks green, among them the build with warnings as errors, the test rules, the end-to-end tests, the DAST baseline, the nginx routes, CodeQL and SonarCloud |
| DAST baseline (run 40111975590) | Signed out and signed in: only Informational alerts (10109, 10015, 10112, 10111, 10031). None of rules 10055, 10021, 10063, 90004, 10011 or 10036 (criterion 1) |
| DAST full scan by hand (run 40112354112) | Only Informational alerts (10109, 10015, 10104). Neither 10036 nor 40025 (criterion 2). No WARN line was needed in `ops/security/zap-rules.tsv` (task 6) |
| End-to-end | Every test passed with the console check on: no page broke the policy (criterion 5) |
| nginx | `Server: nginx` with no version on port 80 and on HTTPS (criterion 6) |

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| CS 1, 2 | Names, one type per file | Pass | `SecurityHeaders`, `AntiforgeryCookie`, `UseSecurityHeaders`, `AddSecureAntiforgery` |
| CS 3 | Formatting, analyzers | Pass | `dotnet format --verify-no-changes` clean; the build has no warning with warnings as errors |
| CS 7 | Arguments validated at public boundaries | Pass | `ArgumentNullException.ThrowIfNull` in both extension methods |
| CS 8 | XML documentation | Pass | Every public type and member, with the reason behind each header and the cookie flag |
| CS 9, 14 | Immutable state, knowledge in one place | Pass | The policy and the header list are constants of `SecurityHeaders`, and the tests read them from there instead of repeating the values |
| CS 12 | Tests cover the new behaviour, named in three parts | Pass | Positive (a page), equivalence (a static file, the cookie without TLS), edge case (a redirect), negative (no inline code), regression (one policy). A two-part name was caught by the test rules and renamed |
| CS 15, 17 | Layers | Pass | The middleware and the cookie are delivery concerns, in `NFHotel.Web/Security` beside the staff sign-in |
| CS 16 | Single responsibility | Pass | Headers, cookie and reconnect dialog are separate types |
| SH | `test-nginx.sh` | Pass | The new checks follow the file's `check` helper; the linters are clean |
| Behaviour change | A test client behind the proxy | Noted | The antiforgery cookie is now `Secure`, so a client that speaks HTTP behind the proxy no longer sends it. The sign-in test now speaks HTTPS as a browser does. Production is HTTPS only (MIL-017), so no real client is affected |

## Overall Verdict

Go.

## Action Items

| # | Action | Owner | Due |
| --- | --- | --- | --- |
| 1 | After the CD run, read the headers of production (criterion 7) and close MIL-033 with its gate review | AI | After the merge |

---

[MIL-033]: ../../milestones/mil-033-security-headers.md
[ADR-0006]: ../../adr/adr-0006-tls.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[fc3da26]: https://github.com/NF-Hotel/NFHotel/commit/fc3da26f3435d60fa0ab8b8af72592cf534bccc3
