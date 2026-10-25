# RC-218 Review of the code of MIL-040 (public access through Cloudflare Tunnel)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-218 |
| CrossReference | [MIL-040], [ADR-0028], [ADR-0024], [QC-SH-001], [QC-YML-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-25 | Accepted | mikemike | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- **Instances reviewed:** the change of tasks 1 to 6 of MIL-040 (#319 to #324), pull request #326:
  - `ops/deploy/cloudflared/` (new), and the tunnel in `deploy-services.sh`, `write-secret-files.sh` and `check-secret-env.sh`;
  - `templates/public.conf.template`, `templates/00-forwarded.conf.template` and `snippets/mobile-api.conf` (new), `snippets/proxy.conf`, `dev/mobile-api.conf` and the compose files of nginx;
  - `NFHOTEL_ENVIRONMENT=Production` in `deploy-release.sh`;
  - the probe and `PublicSiteDown` in the monitoring, with rule tests;
  - the tests `test-nginx.sh`, `test-write-secret-files.sh`, `test-check-secret-env.sh`, `test-deploy-services-env.sh` and `test-compose-proxy.sh`;
  - ADR-0028, BC-001, and the wiki pages Public-Access, Monitoring, Home and the sidebar.
- **Checklists used:** [QC-SH-001] and [QC-YML-001]. No C# changed.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).

## Evidence

| Check | Result |
| --- | --- |
| nginx test | The seven new checks pass: the public site reaches the web application as `https` with the client's address from `CF-Connecting-IP`; `/admin` and `/admin/login` answer 404; both mobile routes reach the API; any other `/mobile-api/` path answers 404; the tailnet server still reports its own scheme |
| Script tests | `test-write-secret-files.sh` 15, `test-check-secret-env.sh` 7, `test-deploy-services-env.sh` 14, `test-deploy-release.sh` 59 and `test-cd-steps.sh` 74 passed |
| Rule tests | `promtool test rules` passes, with a public site that never answered staying quiet and one that answered and failed firing after 3 minutes |
| CI of the pull request | Every check green, among them the end-to-end tests and the DAST baseline on the changed nginx, the smoke test of the proxy, and the hardened nginx service on the API network |
| Linters | `ops/tests/lint.sh` clean |

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| SH | Strict mode, quoting, usage and exit codes | Pass | The changed scripts keep their headers, and each new option and environment variable is documented there |
| SH | Input validated before use | Pass | An empty tunnel token and an environment name that is not a word are refused |
| SH | No secret printed or put on a command line | Pass | The token goes from the settings file to a 0444 file in a 0700 folder and to `--token-file`; the wiki writes it with `read -s` and `printf`, a shell builtin |
| SH | Failure does not take the hotel down | Pass | A tunnel that does not turn healthy warns, like Alloy; without `cloudflared.env` it is left out |
| YML | Compose | Pass | `cloudflared` pinned by digest, no port, `cap_drop: ALL`, read-only, limits and a health check; nginx joins the API network with a comment saying what it carries |
| YML | Alert | Pass | `for`, severity, a summary with the instance, and a runbook row |
| Security | Public surface | Pass | Port 8082 is not published; the back office and the API's other routes are refused on the public listener; the client address is trusted only from the Docker networks |
| Security | Production on every release | Pass | Set by the deploy of a release, tested; the development stack keeps Development |
| Noted | Trust inside the proxy network | Noted | Another container on the proxy network could send a false `CF-Connecting-IP` to port 8082; ADR-0028 names it |

## Overall Verdict

Go.

## Action Items

| # | Action | Owner | Due |
| --- | --- | --- | --- |
| 1 | Create the tunnel, the public host name, Always Use HTTPS and the rate-limiting rule in the Cloudflare dashboard, and write the token on the VM (wiki page Public-Access, parts A and B) | S01 | When the Proxmox node is back |
| 2 | Deploy, prove criteria 2 to 8 from outside the tailnet, and close MIL-040 with its gate review | AI | After action 1 |

---

[MIL-040]: ../../milestones/mil-040-public-access.md
[ADR-0028]: ../../adr/adr-0028-public-access-through-cloudflare-tunnel.md
[ADR-0024]: ../../adr/adr-0024-secrets-as-files.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
