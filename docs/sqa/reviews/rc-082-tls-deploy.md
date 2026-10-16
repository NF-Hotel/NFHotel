# RC-082 Review of the TLS deploy files and scripts (MIL-017)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-082 |
| CrossReference | [MIL-017], [RC-077], [QC-YML-001], [QC-SH-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-17 | Accepted | Claude (AI) | AI as stand-in for S01 | Initial version | [065c6e8] |

---

## Artifact Under Review

- Instance reviewed: `deploy/nginx/docker-compose.yml`, `deploy/nginx/templates/default.conf.template`, `deploy/nginx/snippets/tls.conf`, the env examples of nginx and the web service, `.gitignore`, and the scripts `scripts/make-dev-certificate.sh`, `scripts/deploy-services.sh` and `scripts/test-compose-proxy.sh`
- Checklist used: the criteria of [QC-YML-001] and [QC-SH-001] that apply to a compose file, an nginx template and a script (each line names the criterion; the numbering of [RC-077] is kept)
- Scope: the changes of this milestone
- Mode: autonomous, not independent (reviewer is `AI as stand-in for S01`)
- Tools: `yamllint` 1.38.0, `shellcheck` 0.11.0, `shfmt -i 2 -ci`, `nginx` start in the smoke test and in the VM, `openssl`, the smoke test, a deploy to a Proxmox VE container

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | YAML: parses, no duplicate keys, 2-space indentation, passes `yamllint` | Pass | yamllint with `.yamllint` prints nothing for `deploy/`; nginx accepted the rendered configuration in the smoke test and in the VM |
| 2 | YAML: no secret in the file | Pass | The compose file names a folder, not a key; `deploy/nginx/tls/` is git-ignored (`git check-ignore` true for both files) and mounted `:ro` |
| 3 | YAML: header comment states purpose, tool and decision | Pass | The compose and the template cite MIL-017 and ADR-0006 |
| 4 | YAML: exercised by a check that runs without the real target | Pass | The smoke test starts nginx with a throwaway certificate on other ports and checks the redirect, TLS 1.2, the refusal of TLS 1.1, the missing HSTS header and the 502 while the web service is stopped; 25 of 25 checks pass |
| 5 | Shell: `set -euo pipefail`, quoting, `shellcheck` | Pass | `shellcheck` clean for all scripts; the one `SC2086` in `deploy-services.sh` is disabled with a reason (the names are split on purpose) |
| 6 | Shell: no secret in script, echo or command line | Pass | The script prints the folder and the names in the certificate, never the key; the key is written with `umask 077` and then `chmod 644` for the container user, inside a git-ignored folder |
| 7 | Shell: tested for success, failure and bypass | Pass | Success: the script makes a pair with the names, a second run changes nothing, the SAN list was read back with `openssl x509`. Failure: a Windows `openssl` first failed on the `/CN=` subject (path conversion) and on an MSYS path, fixed with `MSYS_NO_PATHCONV` and `cygpath`. Bypass: an existing pair (a real certificate) is never overwritten |
| 8 | Shell: formatted with `shfmt` | Pass | `shfmt -i 2 -ci` clean |

## Overall Verdict

Go — the applicable criteria pass. The smoke test passes 25 of 25 checks. In the VM: port 80 answers 301 to HTTPS, HTTPS answers 200 with TLS 1.2 and 1.3, TLS 1.1 and 1.0 are refused, no HSTS header is sent with the development certificate, only ports 80, 443 and the loopback 8081 are published, and the Manager signed in over HTTPS and received a `Secure` cookie.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Open port 443 next to port 80 in the router or firewall when the VM goes on the internet | S03 | before production |

---

[MIL-017]: ../../milestones/mil-017-tls.md
[RC-077]: ./rc-077-backoffice-login-deploy.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[065c6e8]: https://github.com/NF-Hotel/NFHotel/commit/065c6e85054e7b24dc5848e241038ede6d747f49
