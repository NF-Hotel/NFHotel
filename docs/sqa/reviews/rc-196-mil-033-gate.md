# RC-196 Gate review of MIL-033

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-196 |
| CrossReference | [MIL-033], [RC-194], [RC-195], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-24 | Accepted | mikemike | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- **Instance reviewed:** the seven Go/No-Go criteria of [MIL-033], after its pull request #276 was merged (`a0dc2c5`) and deployed by CD (run 40113922955).
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).

## Criteria

| # | Criterion | Result | Evidence |
| --- | --- | --- | --- |
| 1 | The ZAP baseline reports none of rules 10055, 10021, 10063, 90004, 10011 and 10036 | Go | Run 40111975590: signed out and signed in, only Informational alerts |
| 2 | The ZAP full scan reports neither 10036 nor 40025 | Go | Run 40112354112, started by hand on the branch: only Informational alerts; no WARN line needed |
| 3 | An integration test reads the headers of a page and a static file, and the policy is strict | Go | `SecurityHeadersTests` (RC-195) |
| 4 | An integration test behind `Tls:Require` gets the antiforgery cookie with `Secure` and `HttpOnly` | Go | `HttpsRequirementTests.LoginPage_BehindTheProxy_SetsASecureAntiforgeryCookie` |
| 5 | The end-to-end tests are green and fail on a CSP violation | Go | Every test passed with the console check on, guest booking and back office pages included |
| 6 | The nginx test checks the `Server` header | Go | `test-nginx.sh`: `Server: nginx` on port 80 and HTTPS |
| 7 | CI green, production answers with the headers, reviews Go | Go | 32 checks green. Production on 2026-10-24 after CD: the policy, `X-Content-Type-Options`, `Permissions-Policy`, COOP, COEP and CORP on the page and on a static file, `Server: nginx`, and the antiforgery cookie `Secure`. RC-195 Go |

## Overall Verdict

Go. MIL-033 is complete; its issues #269 to #275 are closed.

## Action Items

None.

---

[MIL-033]: ../../milestones/mil-033-security-headers.md
[RC-194]: rc-194-mil-033.md
[RC-195]: rc-195-mil-033-code.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
