# RC-187 Review of how renew-certificate.sh finds the host name

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-187 |
| CrossReference | [ADR-0009], [ADR-0020], [RC-162], [QC-SH-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-24 | Accepted | mikemike | AI as stand-in for S01 | Initial version; the cause was NGINX_SERVER_NAME=_ on nf-hotel, so the host name now comes from tailscale CertDomains | [f81cb46] |

---

## Artifact Under Review

- **Instances reviewed:**
  - how `ops/scripts/renew-certificate.sh` finds the host name: `NGINX_SERVER_NAME`, then tailscale's `CertDomains`;
  - six new cases in `ops/tests/test-renew-certificate.sh`.
- **Checklists used:** [QC-SH-001], the rows that the change touches.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).

## Why

On 2026-10-24, the monitoring raised `TaskReportMissing` for `cert_renew`. S03's journal of `nfhotel-cert-renew.service` showed that the renewal failed on 2026-10-23 and on 2026-10-24. Both times it stopped with:

`NGINX_SERVER_NAME in /opt/nfhotel/ops/deploy/nginx/nginx.env is not a tailnet name (<host>.<tailnet>.ts.net): ''`

**Cause.** S03 read the line on `nf-hotel`: `NGINX_SERVER_NAME=_`. That is nginx's catch-all, and it is deliberate, the value of `nginx.env.example` too. The renewal took its host name only from that line, and `_` is not a name that `tailscale cert` can issue, so it failed every night.

**First fix, and why it was not enough.** The first commit of this pull request made the reader tolerate a carriage return, spaces and several names, the ways in which `deploy-services.sh` reads the same line. Those are real differences between the two readers, but `_` holds no tailnet name in any form. That commit alone would not have fixed `nf-hotel`.

**Fix.**
- The renewal takes the first tailnet name in `NGINX_SERVER_NAME` when there is one.
- Otherwise it takes the first of `CertDomains` in `tailscale status --json`. That list holds exactly the names `tailscale cert` can issue for this machine, so it is the right source when nginx answers any name.
- The name is checked as strictly as before.
- When there is no name, the message shows the line and says that tailscale lists no CertDomains.

Without the monitoring, the failure would have shown only as `CertificateExpiresSoon`, 21 days before the certificate expired. RC-162 moved the renewal into git and CD, but checked only that the timer was installed, not that a run succeeded on `nf-hotel`.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| SH 11 | Behaviour is tested | Pass | `test-renew-certificate.sh` passes 19. **Production's case:** `_` with a CRLF ending takes the name from CertDomains. No name anywhere is refused, with the reason. A CertDomain that is not a tailnet name is refused. A CRLF line, a trailing space and several names each find the tailnet name in `NGINX_SERVER_NAME`. The fake prints `status --json` in tailscale's pretty-printed form |
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
[f81cb46]: https://github.com/NF-Hotel/NFHotel/commit/f81cb467aef99d0d377b4381e45d6f655b613242
