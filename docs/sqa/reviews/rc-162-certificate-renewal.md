# RC-162 Review of the certificate renewal in git (RC-161 action 2)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-162 |
| CrossReference | [ADR-0009], [ADR-0018], [RC-161], [QC-SH-001], [QC-WF-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-21 | Accepted | mikemike | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- **Instances reviewed:**
  - `ops/scripts/renew-certificate.sh` (new);
  - `ops/deploy/systemd/nfhotel-cert-renew.service` and `.timer` (new);
  - `update` in `ops/scripts/deploy-release.sh`, which now installs them and enables the timer;
  - the `deploy` step of `ops/scripts/cd-steps.sh`, which runs `update` once more after a new deploy command;
  - `ops/tests/test-renew-certificate.sh` (new) and its job in `ops-ci.yaml`;
  - ADR-0009 and ADR-0018.
- **Checklists used:** [QC-SH-001], [QC-WF-001].
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`). S01 asked for RC-161 action 2 in chat on 2026-10-21.
- **Evidence from `nf-hotel`** (read-only, run by S03 on 2026-10-21):
  - the hand-made `nfhotel-cert-renew.service` ran `tailscale cert --cert-file=/etc/nginx/certs/nf-hotel.crt --key-file=/etc/nginx/certs/nf-hotel.key nf-hotel.taildb84b5.ts.net`, then `docker exec nfhotel-nginx nginx -t` and `nginx -s reload`;
  - its run of 2026-10-21 00:48 failed with `container ... is not running`. `nfhotel-nginx` is the stopped container from before the reconciliation;
  - `/etc/nginx/certs/nf-hotel.crt` and `/opt/nfhotel/ops/deploy/nginx/tls/fullchain.pem` have the same SHA-256 fingerprint and expire on 2027-01-04. Nothing has expired yet. But the running nginx reads the second file, which no renewal reached.

## Design

The script reads the host name from `NGINX_SERVER_NAME` and refuses anything that is not `<host>.<tailnet>.ts.net`. It finds the running nginx by its compose labels (project `nfhotel-nginx`, service `nginx`), not by a container name, and writes into the folder that this nginx mounts as `/etc/nginx/tls`. `tailscale cert` writes into a temporary folder. When the pair is equal to the installed one, nothing changes and nothing reloads. Otherwise the old pair is kept as `.previous` and the new one is installed: certificate 0644, key 0600, owned as the folder. Then `nginx -t` runs, and the old pair comes back if nginx refuses the new one; only then does nginx reload. The units keep the schedule of the hand-made timer (daily, `RandomizedDelaySec=1h`, `Persistent=true`) and its `UMask=0077`.

`update` installs the script and the units with `.previous` and the hash log of ADR-0018, and enables the timer with `enable --now`, so a new VM gets it from its first CD. A deploy command already on the VM does not know these files. So when `update` installs a new deploy command, CD runs `update` once more, and the new command installs them in the same deploy.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| SH 1–4, 12 | Header, quoting, names, shellcheck, shfmt | Pass | `ops/tests/lint.sh`: 0 findings |
| SH 5 | Errors and usage | Pass | Each failure says what failed. A failing `docker ps`, `docker inspect` or settings read gives an error message, not a silent `set -e` exit; the test caught one such silent exit before it was fixed |
| SH 6 | Temporary files | Pass | One `mktemp -d` folder with `trap ... EXIT`; tested that none is left |
| SH 7 | No secret | Pass | The key is written with `umask 077` and installed 0600; neither file is printed |
| SH 8 | Explicit change | Pass | Runs only from its timer or by hand as root; it changes files only when the certificate changed |
| SH 9 | Header | Pass | Purpose, steps, environment, exit codes and needs |
| SH 11 | Success, failure, bypass tested | Pass | `test-renew-certificate.sh` (11 checks): a refused host, an install with modes and owner, `.previous`, check-then-reload, unchanged, refused by nginx with the old pair restored, failed tailscale, no running nginx. `test-deploy-release.sh` (52): the renewal installed and its timer enabled, 7 files logged. `test-cd-steps.sh` (52): update runs twice after a new deploy command and once otherwise |
| SH 13 | Safe to re-run | Pass | An unchanged certificate changes and reloads nothing |
| WF 1, 6 | YAML and actionlint | Pass | One matrix entry added to `ops-ci.yaml` |
| WF 22 | Verified on the instance | Not yet | The first CD after the merge installs and enables the units on `nf-hotel`. S03 then checks `systemctl status nfhotel-cert-renew.service` after its next run |

## Findings

- **The renewal had been failing silently since 2026-10-21 00:48.** There was no alert. It would have stayed unnoticed until the certificate expired, because nginx kept serving the still-valid certificate.
- **The hand-made units on the VM are replaced by the next CD.** `update` keeps them as `.previous` in `/etc/systemd/system/`.
- **No alert on a failed timer run.** This applies to the backup and the renewal alike. Monitoring is outside this change.

## Overall Verdict

Go. QC-WF-001 criterion 22 closes with the first CD after the merge.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| After the first CD of this change, check that `nfhotel-cert-renew.timer` is active and that the next run of `nfhotel-cert-renew.service` succeeds | S03 | The day after that CD |
| Add the job "Ops script tests (test-renew-certificate.sh)" to the required checks of the ruleset | S01 | After it has run on this pull request |

---

[ADR-0009]: ../../adr/adr-0009-tls-certificates.md
[ADR-0018]: ../../adr/adr-0018-deploy-command-updates-itself.md
[RC-161]: ./rc-161-adr-refresh.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
