# RC-206 Gate review of MIL-036

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-206 |
| CrossReference | [MIL-036], [RC-199], [RC-204], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-24 | Accepted | mikemike | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- **Instance reviewed:** the five Go/No-Go criteria of [MIL-036], after its pull request #307 was merged (`34f651e`), the monitoring LXC was deployed at that commit, and the workflows were run by hand on main.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).

## Criteria

| # | Criterion | Result | Evidence |
| --- | --- | --- | --- |
| 1 | A daily workflow scans the running release and opens an issue on a finding; a known vulnerable image proves it | Go | Run 40132355560 with `nginx:1.20.0` by digest: HIGH findings in libssl and libexpat, issue #309 opened. Run 40132463957 on the release in production (`5bcda24`): nothing the gate refuses, and #309 was closed by the workflow |
| 2 | A burst of failed sign-ins or SSH logins raises an alert in Alertmanager, tested by a rule test | Go | The stack test of #307: "failed SSH logins in the journal reach Alertmanager"; the rule test of StaffSignInFailures. In production after the deploy, Loki's ruler holds SshLoginFailures and Prometheus StaffSignInFailures |
| 3 | The k6 test runs in CI against the throwaway stack and fails when the 95th percentile passes its threshold | Go | Run 40132358259: 1830 requests, p95 2.05 ms against the threshold of 1000 ms, 0 % failed, every check 200. A broken threshold makes k6 exit non-zero (99), which fails the step; this was not forced in a run |
| 4 | The wiki has the incident procedure, the postmortem template, and a log policy that names the forbidden fields | Go | The wiki page Incidents: severities, six steps with the reason for each, the GDPR 72-hour rule, the template, and the table of what may and must never be logged; the end-to-end run checks it ("no personal data of the test guest in the logs of 4 containers") |
| 5 | CI green and reviews Go | Go | #307: 32 checks green; RC-204 Go |

## Findings of the check

| # | Finding | What was done |
| --- | --- | --- |
| 1 | The test scan of criterion 1 ran on main at `34f651e` and failed by design; the CD gate (ADR-0021) then refused to deploy the release of `34f651e`, because a check of that commit was red (CD run 40132477812). Production stayed on `5bcda24`, and the next release (`df162f1`) was not affected | The workflow now reports a finding with the issue and a warning, and fails only on an error, so a finding can never block the release that fixes it |
| 2 | `deploy-monitoring.sh` refused to deploy once, saying the LXC's tailnet address was not its own: `ip … \| grep -q` under `pipefail` fails when `ip` gets SIGPIPE | That check and five others of the same form now let `grep` read all of its input |

## Overall Verdict

Go. MIL-036 is complete once this pull request closes #293.

## Action Items

None.

---

[MIL-036]: ../../milestones/mil-036-security-operations.md
[RC-199]: rc-199-mil-036.md
[RC-204]: rc-204-mil-036-code.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
