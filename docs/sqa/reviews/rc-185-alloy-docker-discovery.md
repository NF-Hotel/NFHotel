# RC-185 Review of the network read of the socket proxy, the log delivery test, and the CIS-001 exception (MIL-023 task 8)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-185 |
| CrossReference | [MIL-023], [ADR-0020], [CIS-001], [RC-184], [QC-SH-001], [QC-YML-001], [QC-ADR-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-23 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [4ff81cc] |

---

## Artifact Under Review

- **Instances reviewed:**
  - the socket proxy of `ops/deploy/alloy/docker-compose.yml`;
  - the log delivery and proxy checks of `ops/tests/test-alloy-start.sh`;
  - CIS-001 row 5.31;
  - the consequences of ADR-0020.
- **Checklists used:** [QC-SH-001], [QC-YML-001] and [QC-ADR-001], the rows that the change touches.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).

## Why

After #258, Alloy was healthy on `nf-hotel`, and the host metrics, the job reports and the journal reached the monitoring. But S03 verified that `discovery.docker.containers` failed every minute:

- the error was "error while computing network labels: Error response from daemon: 403 Forbidden";
- so no container log reached Loki.

**Cause.** The error comes from the Docker discovery of Prometheus, which Alloy uses.

- On every refresh, it lists the networks to label each target, and that call cannot be switched off.
- The socket proxy had `NETWORKS: "0"`.

**Fix.** `NETWORKS: "1"`, the smallest change: it adds `GET /networks`. `POST: "0"` still refuses every write, and Alloy still has no socket.

**Second finding.** CIS-001 row 5.31 said that no service mounts the Docker socket. That has not been true since the socket proxy came in (#253).

- The row now records the one exception and its limits.
- The `:ro` flag of the mount does not limit the API; the proxy's rules do.
- ADR-0020's consequence ("read-only access to the Docker socket") is corrected the same way.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| SH 11 | The test catches the defect | Pass | **Probe** with `NETWORKS: "0"`: the job failed 3 checks. No line of the test container reached Loki, discovery logged `403 Forbidden`, and the proxy refused the network list. **With the fix:** 13 of 13 pass |
| SH 11 | What the test proves | Pass | A real container's log line reaches a real Loki through the restricted proxy, with the labels `container` and `host`, which a healthy Alloy alone did not show. The Loki uses the pinned image and the configuration of `deploy/monitoring`. The proxy allows the container and network lists, and refuses network creation, stop, exec and the image list (403 each) |
| SH 11 | Diagnostics | Fixed | The failure message of the discovery check first matched `403` inside a timestamp. It now matches `403 Forbidden` |
| CIS | Hardening kept | Pass | The proxy keeps `read_only`, `no-new-privileges` and an internal network with no route out. Alloy keeps its user, its capabilities (none) and its read-only root |
| ADR | Accuracy | Pass | The consequence now says how Alloy reaches Docker and the journal, as built and tested |

## Overall Verdict

Go.

## Limits that stay

- The proxy lets Alloy read every container's inspect data, including its environment.
  - Some containers get secrets as environment variables, for example the database password of `postgres.env`.
  - A compromised Alloy could therefore read them.
  - This was already so before this change (`CONTAINERS: "1"`), and the network list adds no secret.
  - Reducing it needs those secrets passed as files instead. That is a follow-up, not part of this fix.
- Container logs go to Loki as they are. A log line that holds personal data is kept for 14 days (ADR-0020 decision 8).

## Action Items

| # | Action | Owner | Due |
| --- | --- | --- | --- |
| 1 | After the CD run, check in Grafana that `{container="nfhotel-web-nfhotel-web-1"}` shows log lines, and that Alloy's log has no 403 | AI with S03 | After the merge |

---

[MIL-023]: ../../milestones/mil-023-operational-quality.md
[ADR-0020]: ../../adr/adr-0020-monitoring-stack.md
[CIS-001]: ../cis-checklist.md
[RC-184]: rc-184-alloy-user-and-start-test.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[QC-ADR-001]: ../../../framework/qc/artifacts/qc-adr.md
[4ff81cc]: https://github.com/NF-Hotel/NFHotel/commit/4ff81ccd0f1d43a160f2f45d72845e942cf5728e
