# RC-187 Review of the host name reader of renew-certificate.sh

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-187 |
| CrossReference | [ADR-0009], [ADR-0020], [RC-162], [QC-SH-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-24 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [1461731] |

---

## Artifact Under Review

- **Instances reviewed:**
  - the reading of `NGINX_SERVER_NAME` in `ops/scripts/renew-certificate.sh`;
  - three new cases in `ops/tests/test-renew-certificate.sh`.
- **Checklists used:** [QC-SH-001], the rows that the change touches.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).

## Why

On 2026-10-24, the monitoring raised `TaskReportMissing` for `cert_renew`. S03's journal of `nfhotel-cert-renew.service` showed that the renewal failed on 2026-10-23 and on 2026-10-24. Both times it stopped with:

`NGINX_SERVER_NAME in /opt/nfhotel/ops/deploy/nginx/nginx.env is not a tailnet name (<host>.<tailnet>.ts.net): ''`

**Cause.** The renewal and the deploy read the same line in different ways.

- The renewal accepted only a line that ended right after one name.
- `deploy-services.sh` reads the same line and drops a carriage return, which is why the proxy deploys with that file.
- It also splits the value into names, as nginx's `server_name` may list several.

So a file saved on Windows, a space after the name, or a second name made the renewal find nothing, while the site kept working.

**Fix.**

- The renewal reads the line as the deploy does: it drops the carriage return and splits the value into names.
- It takes the first name that is a tailnet name.
- The check of that name is as strict as before.
- When no name matches, the message lists what the line holds.

Without the monitoring, the failure would have shown only as `CertificateExpiresSoon`, 21 days before the certificate expired. RC-162 moved the renewal into git and CD, but checked only that the timer was installed, not that a run succeeded on `nf-hotel`.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| SH 11 | Behaviour is tested | Pass | `test-renew-certificate.sh` passes 17. New cases: a CRLF line, a trailing space, and several names (`localhost nf-hotel.taildb84b5.ts.net 100.72.194.127`); each finds the tailnet name. `_` is still refused |
| SH 5 | Errors | Pass | The message shows the names it found, so the next failure needs no guessing |
| SH 12, 4 | shfmt, shellcheck | Pass | `ops/tests/lint.sh` is clean |

## Overall Verdict

Go.

## Action Items

| # | Action | Owner | Due |
| --- | --- | --- | --- |
| 1 | After the CD run, start one renewal on `nf-hotel` and check its journal and `/var/lib/nfhotel-metrics/cert_renew.prom`; `TaskReportMissing` then resolves | S03 with the AI | After the merge |

---

[ADR-0009]: ../../adr/adr-0009-tls-certificates.md
[ADR-0020]: ../../adr/adr-0020-monitoring-stack.md
[RC-162]: rc-162-certificate-renewal.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[1461731]: https://github.com/NF-Hotel/NFHotel/commit/14617310d9ac6b9e4e18519fff67af567d78ae21
