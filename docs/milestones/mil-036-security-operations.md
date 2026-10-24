# MIL-036 Security operations: image scans, security alerts, load test and incidents

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-036 |
| CrossReference | [BC-001], [ADR-0020], [MIL-022], [MIL-023] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-24 | Accepted | mikemike | S01 | Initial version; S01 asked in chat on 2026-10-24 for every gap of the DevOps review to be closed in autonomous mode ("Lav allesammen i automode on"), which accepts this plan; accepted after RC-199 Go | [737399d] |

---

## Purpose

Decide whether NF Hotel may claim that it notices a new vulnerability in what runs, an attack on the sign-in or on SSH, and a loss of speed under load, and that it handles an incident by a written procedure. Today the images are scanned when they are built, so a vulnerability published after a deploy waits for the next release; no alert watches failed sign-ins; nothing measures the site under load; and there is no incident procedure.

## Deliverable

- A scheduled workflow that scans the images of the release that runs in production, daily, and opens an issue for a new critical or high finding with a fix.
- Alerts on security events: failed staff sign-ins and failed SSH logins above a rate, from the logs in Loki, through Alertmanager to Discord.
- A load test (k6) of the public pages and the sign-in against the throwaway stack, with thresholds, run weekly and by hand.
- An incident procedure and a postmortem template in the wiki, and a log policy: no personal data in the logs, checked by a rule.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | A daily workflow scans the digests of the running release and opens an issue on a new finding; a test with a known vulnerable image proves it | Yes | No |
| 2 | A burst of failed sign-ins or SSH logins on the throwaway stack raises an alert in Alertmanager, tested by a rule test | Yes | No |
| 3 | The k6 test runs in CI against the throwaway stack and fails when the 95th percentile of the page duration passes its threshold | Yes | No |
| 4 | The wiki has the incident procedure and the postmortem template, and the log policy names the fields that must not be logged | Yes | No |
| 5 | CI is green and the reviews are Go | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-022 | The scan gate and the release manifest with the digests |
| MIL-023 | Loki, Alertmanager and the throwaway stack |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| Success criterion 21 (security) and 26 (operations) | Tasks 1 to 4 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-28.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Scan of the running images | A scheduled workflow reads the release manifest of production, scans each digest, and opens or updates an issue on a new finding. | No | MIL-022 |
| 2 | Security event alerts | Loki ruler rules for failed sign-ins and SSH failures, sent to Alertmanager; rule tests. | No | ADR-0020 |
| 3 | Load test | A k6 script with thresholds, run against the throwaway stack in a weekly workflow and by hand. | No |  |
| 4 | Incidents and log policy | The incident procedure, the postmortem template and the log policy in the wiki; a check that no e-mail address or passport number reaches the logs of the end-to-end run. | No |  |
| 5 | Check and review | Prove the criteria; reviews. | No |  |

---

[BC-001]: ../business-case.md
[ADR-0020]: ../adr/adr-0020-monitoring-stack.md
[MIL-022]: ./mil-022-supply-chain-security.md
[MIL-023]: ./mil-023-operational-quality.md
[737399d]: https://github.com/NF-Hotel/NFHotel/commit/737399d58692d9cf086cedcd8641a2d5f352e7d7
