# ADR-0032 Code scanning shows the findings someone can act on

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0032 |
| CrossReference | [MIL-044], [MIL-022], [MIL-036], [ADR-0019], [DSA-001] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-26 | Accepted | mikemike | AI as stand-in for S01 | Initial version; written by the AI under MIL-044, which S01 accepted in chat on 2026-10-26; accepted after RC-233 Go | pending |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

**Who decided what.** S01 asked on 2026-10-26 for a security overview that does not show red, worked out as a GitHub and security professional would. The policy of this record was written by the AI assistant.

On 2026-10-26 the code scanning tab of NFHotel had 565 open alerts. 470 were Trivy findings in operating system packages of the four images (Debian and Ubuntu) for which no fixed version exists yet. The release gate of DSA-001 section 8 already accepts them: it stops a release for a critical finding, or a high one with a fix. Between them, the 25 alerts that need a decision are hard to see, and an alert list that nobody can empty is soon not read at all. The same holds for informational ZAP rules and for Scorecard checks that score decisions the team made on purpose.

Options considered:

1. **Every finding in code scanning.** Complete, but 470 of the alerts cannot be acted on until a vendor ships a fix.
2. **Dismiss the findings without a fix by hand.** Each dismissal has to be repeated for every new image digest, and a dismissed alert stays dismissed when a fix arrives.
3. **Code scanning gets the actionable findings; the rest stays in the record.** Trivy uploads the vulnerabilities with a fixed version and every secret. The full JSON report and the SBOM of each image stay with the release. A finding becomes an alert on the day its fix exists, because each release and the daily scan use that day's database.

## Decision

We follow option 3.

1. **Trivy.** `scan-image.sh` writes the SARIF for code scanning from `scan-gate.sh --fixable`: the vulnerabilities with a fixed version and every secret. The JSON report (all findings), the SBOM and its attestation are unchanged, and so is the gate.
2. **ZAP.** A rule whose alert is informational, or a finding that the team accepts, gets a line in `ops/security/zap-rules.tsv` with its reason and an expiry of at most 30 days (MIL-027); a rule that points at a real gap is fixed.
3. **Scorecard.** A check that scores a decision is dismissed in code scanning with the reason and the decision record: Code-Review (ADR-0019: no approving review is required), Fuzzing (no parser of untrusted binary input, which is where fuzzing pays), CII-Best-Practices (DSA-001 is the assessment of this project). Every other check is fixed.
4. **Stale configurations.** An analysis configuration that no longer runs, such as the CodeQL configuration for Python after ADR-0016, is deleted, so its last error does not stay on the overview.

## Consequences

**Positive:**

- Every open alert is one someone can act on, so the list is read.
- A vulnerability that gets a fix appears the same day, without anyone reopening a dismissed alert.

**Negative:**

- The tab does not show the full exposure of an image; that is in the JSON report and the SBOM of the release, which the security review has to read.
- An IGNORE line of ZAP expires after 30 days and must be renewed by a pull request.

**Quality:** security improves, because the actionable findings are visible; maintainability of the triage improves (ISO/IEC 25010:2023).

## Affected Artifacts

- [DSA-001]: section 8, where the gate is, now names where the findings are shown.
- [MIL-044]: tasks 1 to 5.

---

[MIL-044]: ../milestones/mil-044-security-overview.md
[MIL-022]: ../milestones/mil-022-supply-chain-security.md
[MIL-036]: ../milestones/mil-036-security-operations.md
[ADR-0019]: ./adr-0019-approvals-by-the-owner.md
[DSA-001]: ../sqa/devsecops-assessment.md
