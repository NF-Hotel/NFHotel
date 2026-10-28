# RC-176 Review of the CIS selection, container and Docker hardening, firewall and patching (MIL-023 tasks 1 to 4)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-176 |
| CrossReference | [MIL-023], [CIS-001], [DSA-001], [QC-SH-001], [QC-WF-001], [QC-YML-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Deprecated | mikemike | AI as stand-in for S01 | Initial version | [717dc18] |
| 2026-10-23 | Accepted | mikemike | AI as stand-in for S01 | Ported from NF-Hotel/DevOps#12 (was RC-507 there); paths under ops/; its condition closes with the green Ops CI of the port (RC-178) | [717dc18] |

---

## Artifact Under Review

- **Instances reviewed:**
  - task 1 (NF-Hotel/NFHotel#134): `docs/sqa/cis-checklist.md` (CIS-001, new) and `scripts/cis-evidence.sh` (new, read-only);
  - task 2 (NF-Hotel/NFHotel#135): `deploy/host/docker/daemon.json` and `scripts/install-docker-daemon.sh` (new); the hardening of every service in `deploy/*/docker-compose.yml` except `deploy/proxmox`; `tests/lib/hardening.sh`, `tests/test-optional-services.sh` (new) and the hardening check in `tests/check-e2e-stack.sh`;
  - task 3 (NF-Hotel/NFHotel#136): `deploy/host/nftables/nfhotel.nft`, `deploy/systemd/nfhotel-firewall.service`, `scripts/apply-firewall.sh` and `tests/test-nftables.sh` (new);
  - task 4 (NF-Hotel/NFHotel#137): `deploy/host/apt/*`, `deploy/host/journald/nfhotel.conf`, `scripts/install-patching.sh` and `tests/test-host-setup.sh` (new); the monthly rebuild and reboot plan in the wiki page `docs/wiki/Host-Hardening.md` (new);
  - the new CI job "Host hardening" in `.github/workflows/ci.yaml`.
- **Checklists used:** [QC-SH-001], [QC-WF-001], [QC-YML-001], the rows that the change touches; MIL-023 criteria 1 to 4 for the content.
- **Scope:** full review of the new files, delta review of the compose files, `check-e2e-stack.sh` and `ci.yaml`.
- **Language and domain:** n/a (technical type).
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`). No change was made on `nf-hotel`.
- **Tools:** `tests/lint.sh` (actionlint 1.7.7 with shellcheck, shellcheck 0.11.0, shfmt 3.14.1, yamllint 1.35.1); Docker Desktop on the developer machine; NF-Hotel/NFHotel at `7c6f348` for the throwaway stack and the end-to-end tests.

## Test Runs

| Test | Where | Result |
| --- | --- | --- |
| `scripts/e2e-stack.sh up` with the hardened compose files, then `tests/check-e2e-stack.sh` | Local, stack `mil023-e2e` | 14 of 14 checks pass, among them the hardening of nginx, web, API and PostgreSQL; every container healthy, again after `docker restart` of each |
| `scripts/run-e2e-tests.sh` against that stack | Local, Playwright 1.62.0 | 9 of 9 end-to-end tests pass, three times (before and after the restarts, and after the nginx health check was added) |
| `tests/test-optional-services.sh` | Local; ollama 0.40.6, market-analysis `sha-96d036b`, benchmark `sha-1d50f50` | 14 of 14: each service has the hardening, is healthy, and is healthy after a restart; Ollama and the analysis API answer on their networks |
| `tests/test-nftables.sh` | Debian 13 container with `NET_ADMIN`, `SYS_ADMIN` | 28 of 28 |
| `tests/test-host-setup.sh` | Debian 13 container | 16 of 16 |
| `tests/test-nginx.sh`, `test-deploy-release.sh`, `test-cd-steps.sh`, `test-vm-setup.sh`, `test-scan-gate.sh`, `test-dast-rules.sh` | Local, unchanged tests | All pass |
| `tests/lint.sh` | Local | Clean |

Measured capability needs, by starting the official images with `--cap-drop ALL` and one capability less each time: nginx stops at `chown("/var/cache/nginx/client_temp")` without `CHOWN`; PostgreSQL cannot read or fix its data folder without `DAC_OVERRIDE` or `FOWNER`.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| SH 1–2 | Shebang, `set -euo pipefail`, quoting, arrays, `[[ ]]` | Pass | Every new script; the tests use `set -uo pipefail` so they reach every check. The nftables table name is an array, so it is passed as two words without an unquoted expansion |
| SH 3 | Names and constants | Pass | Paths and the table are readonly; the environment overrides (`NFHOTEL_*`) are upper case |
| SH 4 | shellcheck and bash -n | Pass | 0 findings. Two `disable=SC2016` with their reason: the inner script of the revert timer, and the awk programs and format strings of `cis-evidence.sh` |
| SH 5 | Errors and usage | Pass | `error:` and exit 1 on every failed step; usage on exit 2; `--restart` without `--apply`, a timeout outside 10 to 3600 s and a `--confirm` with nothing pending are refused |
| SH 6 | Temporary files | Pass | `test-optional-services.sh` uses `mktemp -d` with a trap; the firewall state is in a fixed root-only folder (0700) by design, so `--confirm` from another session finds it |
| SH 7 | No secret | Pass | `cis-evidence.sh` prints no container environment and no settings file; the other scripts handle no secret |
| SH 8 | Explicit flag for external changes | Pass | `install-docker-daemon.sh`, `install-patching.sh` and `apply-firewall.sh` are dry runs by default; the Docker restart needs `--restart` on top of `--apply`; the firewall reverts unless confirmed |
| SH 9 | Header comment | Pass | Every new script and test |
| SH 10 | Implements a cited task | Pass | MIL-023 tasks 1 to 4, CIS-001 rows; the deviations are below |
| SH 11 | Behaviour is tested | Pass | Dry run, apply, second run, rollback of an invalid `daemon.json` (by `dockerd --validate`, on the VM only), automatic and manual revert of the firewall, refusals; the revert of an invalid `daemon.json` is not tested, because no `dockerd` runs in the test container |
| SH 12 | shfmt | Pass | No diff |
| SH 13 | Safe to re-run | Pass | A second `--apply` of each script changes nothing; the ruleset replaces its own table in one transaction, and loading it twice keeps one table |
| SH 14 | Tools stated | Pass | In each header |
| SH 15 | Defined once | Pass | The hardening check is one function (`tests/lib/hardening.sh`) used by both stack tests; the host files live once in `deploy/host/` and the scripts install them |
| WF 4 | Header comment | Pass | The CI header names the host hardening tests |
| WF 7 | Least permissions | Pass | The job adds no permission; the extra capabilities are given to the test's own container, stated in a comment |
| WF 8 | Actions pinned by SHA | Pass | Only `actions/checkout`, at the existing pin |
| WF 22 | Verified on the instance that runs it | Fail | Until the CI run of this pull request is green |
| YML 1–3 | Parses, formatting, yamllint | Pass | 0 findings |
| YML 8 | Repetition defined once | Pass | The two market-analysis entries share an anchor; the other compose files are separate projects and cannot share one, so each states its own lines with its reason |
| YML 11 | Header comment | Pass | Every hardening block says why its service needs what it adds |

## Findings

- **No evidence from the VM.** The read-only evidence run on `nf-hotel` was blocked by the session's permission policy, so CIS-001 records the selection and the repository evidence, and every VM item is "Pending VM". `scripts/cis-evidence.sh` collects that evidence in one read-only run. MIL-023 criterion 1 is not met until it has run before and after the apply steps.
- **`DOCKER-USER` is a native chain.** MIL-023 asks for Docker traffic to be filtered "in `DOCKER-USER`". Docker owns that chain in its iptables-nft table and rebuilds the table when it restarts. The ruleset therefore has a chain `docker-user` in its own table, on the forward hook just before Docker's chains (priority `filter - 1`). It does the same job, it is not touched by a Docker restart, and it covers IPv6. The test replaces a Docker-like nat table under it; a real Docker restart is checked on the VM (wiki step 4, criterion 2).
- **How the internet reaches the VM is not verified.** It may be a port forward on the router (ADR-0006) or Tailscale Funnel. The ruleset allows both: 80 and 443 on every interface, and the loopback. The scan from outside the tailnet in the apply steps settles it.
- **Root login stays.** S01 and S03 administer as root over Tailscale. Port 22 answers only on `tailscale0` after the firewall. Turning off root login needs a named administrator account first (CIS-001, Debian 5.1.20, Deferred).
- **The nginx master process stays root.** `nginxinc/nginx-unprivileged` would change the ports inside the container, the templates and the tests; it is left for a change of its own (CIS-001, Docker 4.1).
- **Data Protection keys.** With a read-only root filesystem, the web and the API keep their ASP.NET Core Data Protection keys in memory. A restart of the container now signs staff out, as a deploy already did.
- **The limits are first estimates.** They are well above what the test stack used, but the VM's real use is not known. They are checked with `docker stats` after the deploy.
- **The monthly rebuild needs a trigger in NFHotel.** `NF Hotel Release` starts only after CI on `main`. A `workflow_dispatch` and a monthly `schedule` on `release.yaml` would let the plan run without a merge.
- **Item numbers.** The Docker item numbers are those cited by DSA-001 (v1.6.0 numbering); v1.9.0 may number some of section 5 differently. The rows name their titles.
- **A shared Docker host.** A first run of the throwaway stack with the default name `nfhotel-e2e` was removed during the review by something else on the same Docker host, most likely another run with the same default name. The later runs used `--name mil023-e2e`. Nothing to change in the scripts; a shared Docker host needs a name per run.

## Overall Verdict

Go-with-conditions for merging the code and the documents. Every Mandatory criterion that the change touches passes except QC-WF-001 criterion 22, which closes with the green CI of this pull request. MIL-023 criteria 1 to 4 are not met by this change alone: they need the apply steps on `nf-hotel`, which S01 decides.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Run `scripts/cis-evidence.sh` on `nf-hotel` before the apply steps and record it | S01 | Before the apply steps |
| Decide and run the apply steps of the wiki page Host-Hardening (patching, firewall with S03, Docker daemon in a window, deploy) and record them in an RC, with the scan from outside the tailnet before and after a Docker restart | S01, S03 | MIL-023 target date |
| Map the Docker item numbers of CIS-001 against the v1.9.0 document | S01 | Before criterion 1 is checked |
| Add `workflow_dispatch` and a monthly `schedule` to `release.yaml` in NF-Hotel/NFHotel | S01 | Before the first monthly rebuild, December 2026 |
| Follow-ups of CIS-001: `sysctl.d` (Debian 1.5, 3.3), `modprobe.d` (1.1.1), `/tmp` and `/dev/shm` options, an `sshd_config.d` file and a named administrator account | S01, S03 | After the first evidence run |

---

[MIL-023]: ../../milestones/mil-023-operational-quality.md
[CIS-001]: ../cis-checklist.md
[DSA-001]: ../devsecops-assessment.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[717dc18]: https://github.com/NF-Hotel/NFHotel/commit/717dc18e7bd3a6064272ef8fc9eb9ae123b89e55
