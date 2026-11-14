# RC-372 Gate review of MIL-053 Security scan results in Grafana

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-372 |
| CrossReference | [MIL-053], [RC-256], [RC-257], [RC-277], [RC-347], [RC-355], [RC-366], [ADR-0007], [ADR-0020], [MIL-051], [QC-MIL-001], [QC-PY-001], [QC-SH-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-15 | Accepted | mikemike | AI as stand-in for S01 | Initial version: the gate of MIL-053 on Ops CI of `main` at 5846e91, the deploys of the monitoring on `nf-monitoring` of 2026-11-14, and the numbers of Prometheus, GitHub code scanning and SonarQube Cloud read in the same minute; Go; mode autonomous, not independent | pending |

---

## Artifact Under Review

- Instance reviewed: [MIL-053] at its gate. It covers the seven Go/No-Go criteria, with this evidence:
  - Ops CI run 43645988924 of `main` at 5846e91 (2026-11-14), its jobs "Ops monitoring configuration (MIL-023)" and
    "Ops wiki coverage";
  - the two deploys of the monitoring on `nf-monitoring` on 2026-11-14, at 05:57 UTC ([RC-347]) and at 08:04 UTC
    ([RC-355]);
  - Prometheus of `nf-monitoring`, the code scanning alerts of GitHub and the measures of SonarQube Cloud, read between
    22:54 and 22:56 UTC on 2026-11-14, and the dashboard as S01's browser showed it at about 22:05 UTC;
  - the source on `main` at 5846e91: the exporter in `ops/deploy/monitoring/scan-exporter/`, the compose file,
    `ops/scripts/deploy-monitoring.sh`, and the rules `prometheus/rules/scans.yml` with their tests.

  Before the gate: [RC-256] (the plan, Go) and [RC-257] (the code, Go).
- Checklist used: the Go/No-Go criteria of [MIL-053]. [QC-MIL-001] criterion 2 makes them the checklist of the gate.
- Scope: the gate. Criteria 1 to 3 and 7 rest on Ops CI of 5846e91 and on [RC-257], criterion 4 on CI and on the
  deploys of 2026-11-14, criterion 5 on the source, the metrics and the logs of CI, and criterion 6 on the reading of
  2026-11-14 between 22:54 and 22:56 UTC.
- Language and domain: en / it (the Metadata rows of MIL-053). The language of the document was reviewed in RC-256, and
  the gate adds only a row to its Version History. This record is a technical type.
- Language reviewer: none.
- Mode: autonomous, not independent. S01 gave the acceptance of the plan of 2026-11-14, which has the gates of MIL-053,
  MIL-054 and MIL-055 in its third wave, to the AI in chat ("Du godkender for mig"). The AI wrote the code of MIL-053
  ([RC-257]) and this record.

**Evidence of 2026-11-14 (UTC):**

- **Ops CI of `main`.** Run 43645988924 of 5846e91 ended with success. Its job "Ops monitoring configuration
  (MIL-023)" logged:
  - for the unit tests of the exporter, "64 passed in 1.84s" and "Total coverage: 93.56%" (the floor is 80 %);
  - "PASS promtool check config" and "PASS promtool test rules";
  - for the stack test `ops/tests/test-monitoring-stack.sh`, which starts the stack with `deploy-monitoring.sh --apply`:
    "the six services run", "each secret belongs to the user of its container, 0400, in a 0711 folder", "the missing
    scan token becomes an empty file of the exporter's user, 0400", "the scan exporter can read its token file",
    "Prometheus scrapes the scan exporter" and "without a token the exporter reports code scanning as not read", each
    as PASS, and "RESULT: 24 passed, 0 failed";
  - inside the stack test, the check of the deploy over the six containers: "PASS nfhotel-monitoring-scan-exporter-1
    monitoring/scan-exporter - uid 10001, caps none, seccomp filter, apparmor docker-default (enforce), root ro, host
    paths none, writable mounts none, socket none, networks nfhotel-monitoring_default, ports none", and
    "6 container(s): 6 passed, 0 failed, 0 warned";
  - the check of every compose file against the profiles: "PASS monitoring/scan-exporter matches its profile" and
    "RESULT: 42 passed, 0 failed".

  Its job "Ops wiki coverage" logged "123 passed, 0 failed". Neither log holds a string of the shape of a GitHub token.
- **The unit tests.** In `scan-exporter/tests/unit/`: `test_open_alert_samples_counts_by_tool_category_and_severity`
  (the counting), `test_collect_follows_full_pages_of_alerts` (the pagination),
  `test_collect_when_a_source_fails_keeps_its_last_samples_and_marks_it_down` and
  `test_collect_when_an_answer_is_malformed_marks_the_source_down` (a failed source),
  `test_collect_without_token_skips_code_scanning_and_reads_the_rest` and `test_read_token_when_missing_or_empty_is_none`
  (a missing token), and `test_collect_sends_the_token_to_github_only`. The collector reads recorded answers through a
  fake client; `test_http_json_client.py` replaces `urlopen` and asserts that an error does not hold the token.
- **The rule tests.** `scans.yml` has five rules, and each has a case in `rules-test.yml` where it fires and one where
  it does not. `ScanFindingsHigh`, `ScanStale` and `SonarQualityGateFailed` have them in separate evaluations.
  `ScanWorkflowFailed` and `ScanSourceDown` have them in one evaluation each: a failed and a successful workflow, and a
  source that is read and one that is not. `promtool test rules` compares the alerts exactly, so an alert of the
  successful workflow or of the read source would fail the test.
- **The deploys on `nf-monitoring`.** `deploy-monitoring.sh --apply` ran from `main` over Tailscale SSH at 05:57 UTC
  ([RC-347]) and at 08:04 UTC ([RC-355]), and exited 0 both times. The script stops when the token file has a group or
  other bit or does not start with `github_pat_`; it gives the file to user 10001 with mode 0400, sets the folder to
  0711, and stops when a container of the monitoring breaks its profile in `ops/security/container-profiles.tsv`. At
  05:57 UTC the check printed "6 container(s): 6 passed, 0 failed, 0 warned", with user 10001, no capability, a
  read-only root, no host path, no writable mount and no socket for the scan exporter ([RC-347]).
- **Prometheus.** At 22:54:42 UTC Prometheus of `nf-monitoring` answered `up{job="scans"}` 1 and
  `nfhotel_scan_source_up` 1 for every source. The open findings: 17 of OWASP ZAP with severity warning and 1 of Trivy
  with severity medium, 18 in all, none critical or high. SonarQube Cloud: the quality gate passed; bugs 1,
  vulnerabilities 0, security hotspots 0, code smells 782, coverage 96.3 %, duplication 0 %. The five rules had health
  ok and were inactive. None of the 57 series of the exporter held a label or a value of the shape of a GitHub token.
  At 23:02 UTC `up{job="scans"}` was still 1.
- **GitHub and SonarQube Cloud.** At 22:55:09 UTC the API of GitHub code scanning listed 18 open alerts: 17 of
  OWASP ZAP (warning) and 1 of Trivy (medium). At 22:55:10 UTC the API of SonarQube Cloud gave the same six measures
  and the quality gate OK.
- **The dashboard.** At about 22:05 UTC the AI looked at the dashboard "Security scans" in S01's browser, signed in to
  Grafana: it showed the same findings and measures.
- **The scan workflows.** The latest runs on `main` of the five scan workflows ended with success: `ci.yaml`,
  `release.yaml`, `e2e-dast.yaml` and `scorecard.yaml` on 5846e91 between 22:03 and 22:14 UTC, and
  `running-image-scan.yaml` on 34c8a62 at 12:14 UTC.
- **The repository.** `git grep` over `main` at 5846e91 finds no string of the shape of a GitHub token (`github_pat_`
  with 20 or more characters after it, or `gh[pousr]_` with 30 or more). No workflow reads the token file; it exists
  only on `nf-monitoring`, in the git-ignored `secrets/github-scan-token`. The exporter writes four kinds of log line:
  the start, "no GitHub token file", "cannot read <source>: <error>", and, since [MIL-051], "report left out".
- **Not observed.** The owner and mode of the token file on `nf-monitoring` after 08:04 UTC, and the log lines of the
  exporter there. Reading either needs a Tailscale SSH session, which waits for S01 to approve its check, and Loki
  holds only the journal of `nf-hotel` (F1, F2).

## Checklist Results

| # | Criterion (MIL-053) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The unit tests of the exporter pass in Ops CI, with no network call, and cover the counting by tool and severity, pagination, a failed source and a missing token | Pass | Run 43645988924: 64 passed, coverage 93.56 %; the tests named above. The collector uses a fake client, and the test of the HTTP client replaces `urlopen` |
| 2 | `promtool check config` and `promtool test rules` pass with the new job and rules, and every new rule has a test that fires and one that does not | Pass | Both PASS in run 43645988924; each of the five rules has a case that fires and one that does not |
| 3 | The stack test starts six services, and Prometheus reports `up{job="scans"} == 1` | Pass | "the six services run", and "Prometheus scrapes the scan exporter", which waits for `up{job="scans"}` to be 1. On `nf-monitoring` it was 1 at 22:54:42 and at 23:02 UTC |
| 4 | The exporter runs read-only, as a user of its own, with every capability dropped and no published port; the token file is readable by the exporter's user only | Pass | CI: uid 10001, caps none, root ro, ports none, and each secret of its container's user, 0400, in a 0711 folder. `nf-monitoring`: the deploys of 05:57 and 08:04 UTC exited 0, so the token guard, the owner and mode of the file and the check of the profiles held then; the mode was not read after that (F1) |
| 5 | No token appears in the repository, a log line of the exporter, a metric or a workflow log | Pass | The repository: none. The metrics: none in 57 series. The logs of run 43645988924: none, and no workflow reads the token. The exporter: no log line carries the token or a header, and the test of the HTTP client asserts it for an error; the lines on `nf-monitoring` were not read (F2) |
| 6 | After the deploy on `nf-monitoring`, the dashboard "Security scans" shows the open findings and the SonarQube Cloud measures, and the numbers match GitHub code scanning and sonarcloud.io | Pass | 18 open findings (17 OWASP ZAP warning, 1 Trivy medium) and the six measures, the same in Prometheus, GitHub and SonarQube Cloud at 22:54 to 22:55 UTC; the dashboard showed them at about 22:05 UTC (F4) |
| 7 | `test-wiki-coverage.sh` passes (new service and alerts on their pages), and the code review against `QC-PY-001` and `QC-SH-001` is Go | Pass | "123 passed, 0 failed"; [RC-257]: Go. The later changes of the exporter are reviewed too (F3) |

## Findings

- **F1, note: the mode of the token file on `nf-monitoring` was not read after the deploy.** The deploy of 08:04 UTC
  exited 0, and the script stops when `chown 10001:10001` or `chmod 0400` of the file fails, so the file was the
  exporter's, with mode 0400, then. A change by hand since then would not show. Reading it needs a Tailscale SSH
  session, which waits for S01 to approve its check; the next such session is the install of [MIL-051] on
  `nf-monitoring`.
- **F2, Low: the logs of `nf-monitoring` do not reach Loki.** Loki has one host, `nf-hotel`, with the journal as its only
  source, so the log of the scan exporter, like that of every container of the monitoring, can be read only on
  `nf-monitoring` itself. Criterion 5 holds on the code: the exporter logs no token and no header, and an error of its
  HTTP client is tested not to hold the token. Whether `nf-monitoring` should send its own logs to Loki is for S01 to
  decide; MIL-053 did not plan it.
- **F3, note: the exporter changed after [RC-257].** b69a4ea and 791ab63 (2026-11-01, in #434: the line length of a
  rule, the coverage of the exporter, and `NOSONAR` on the plain HTTP of `/metrics`) are recorded in [RC-257] itself.
  5756e58 (2026-11-04, the documented `JsonClient.get_json`) is reviewed in [RC-277], and c86c0d3 (2026-11-14, the
  report folder of [MIL-051]) in [RC-366]. The last deploy recorded on `nf-monitoring`, at 08:04 UTC, came before
  c86c0d3; the report folder runs there after the install of [MIL-051].
- **F4, note: the dashboard and the numbers were read at two times.** The dashboard was seen at about 22:05 UTC, and the
  numbers were compared at 22:54 to 22:55 UTC. Its panels read the series of the exporter in Prometheus, which were
  compared.

## Overall Verdict

Go. The seven criteria pass on the evidence of 2026-11-14; two readings on `nf-monitoring` itself were not possible
(F1, F2), and they are action items. S01 gave the acceptance to the AI in chat on 2026-11-14 ("Du godkender for mig"),
and the AI accepted this record and the gate row of MIL-053 as stand-in for S01; the gate of MIL-053 has passed.
MIL-053 has no milestone or issues on GitHub, so its pull request closes none. The reviewer is not independent: the AI
wrote the code of MIL-053 and this record.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept this record and the gate row of MIL-053, or return them | S01; done on 2026-11-15 by the AI as stand-in, at S01's word in chat | At the review of the gate pull request |
| F1, F2: in the next Tailscale SSH session on `nf-monitoring`, read the owner and mode of `secrets/github-scan-token`, and count the lines of the exporter's log that have the shape of a token, without printing a line | AI as stand-in for S01 | At the install of [MIL-051] (target 2026-11-27) |
| F2: decide whether `nf-monitoring` sends its own logs to Loki | S01 | 2026-11-27, with the install of [MIL-051] |

---

[MIL-053]: ../../milestones/mil-053-security-scan-metrics.md
[RC-256]: ./rc-256-mil-053-plan.md
[RC-257]: ./rc-257-mil-053-code.md
[RC-277]: ./rc-277-mil-058.md
[RC-347]: ./rc-347-mil-080-gate.md
[RC-355]: ./rc-355-mil-083-gate.md
[RC-366]: ./rc-366-mil-051-code.md
[ADR-0007]: ../../adr/adr-0007-cicd-and-supply-chain.md
[ADR-0020]: ../../adr/adr-0020-monitoring-stack.md
[MIL-051]: ../../milestones/mil-051-monitoring-continuous-deployment.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[QC-PY-001]: ../../../framework/qc/code/qc-programming-python.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
