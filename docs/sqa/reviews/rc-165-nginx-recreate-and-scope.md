# RC-165 Review of the proxy recreate rule and the tailnet scope report (MIL-021 criteria 5 and 7)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-165 |
| CrossReference | [MIL-021], [RC-163], [QC-SH-001], [QC-YML-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-22 | Accepted | mikemike | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- **Instances reviewed:**
  - `ops/scripts/deploy-services.sh` and `ops/deploy/nginx/docker-compose.yml`: the proxy is recreated only when its configuration changes;
  - `ops/scripts/cd-steps.sh scope`: the peers seen are listed, without a warning;
  - their tests in `test-deploy-services-env.sh` and `test-cd-steps.sh`.
- **Checklists used:** [QC-SH-001], [QC-YML-001].
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`), MIL-021 in autonomous mode accepted by S01 on 2026-10-21.
- **Evidence:** the rollback drill, CD run 39650132524 (2026-10-22).

## Why

- **Criterion 5.** The drill log shows that `postgres` and `ollama` were kept (`Running`, then `Healthy`), but `nginx` was recreated on every deploy. `deploy-services.sh` passed `--force-recreate` to the proxy, because its templates are rendered only when it starts. Every deploy therefore dropped the open connections, and criterion 5 ("does not recreate postgres, nginx or ollama") could not pass. The proxy now gets `NGINX_CONFIG_HASH`, a hash of its templates, snippets and settings, as part of its environment. Compose recreates it exactly when one of them changes; a new certificate is loaded with a reload by `nfhotel-cert-renew`.
- **Criterion 7.** The scope step of the same run found the other ports of the VM closed: "Ports 80 443 5432 8081 of nf-hotel do not answer". It also warned that the CI node sees seven other peers. A peer is seen when either side may reach the other. The first grant of the tailnet policy lets `autogroup:member` reach every node, the ephemeral CI node too. What the CI node itself may reach is fixed by its one grant (`tcp:22` of `tag:nfhotel-vm`). The policy tests prove it: `tag:ci` is denied the backup target, the Proxmox node and the Sports Club VM, and the policy cannot be saved if that changes. The peers are therefore listed for the record, with no warning on every deploy. The breadth of the member grant is a finding for MIL-033 ("Stram tailnet"), not for this criterion.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| SH 1–4, 12 | Quoting, shellcheck, shfmt | Pass | `ops/tests/lint.sh` clean (one SC2034/SC2016 and one SC2015 placement fixed on the way) |
| SH 5 | Errors | Pass | A failed hash stops the deploy of the proxy with its reason |
| SH 11 | Tested | Pass | `test-deploy-services-env.sh`, 7 checks: a 16-hex hash; the same configuration gives the same hash; no `--force-recreate`; a changed template and changed settings change the hash. `test-cd-steps.sh`, 63 checks: the peers are listed without a warning, and an open port still warns |
| YML 1–3 | yamllint | Pass | |

## Overall Verdict

Go. The first deploy after the merge shows `nfhotel-nginx-nginx-1 Running`, unless the proxy configuration changed in that release.

## Action Items

None.

---

[MIL-021]: ../../milestones/mil-021-secure-cicd-foundation.md
[RC-163]: ./rc-163-mil-021-finish-code.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
