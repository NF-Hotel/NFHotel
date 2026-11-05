# RC-289 Gate review of MIL-061 Python services on their own Docker network

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-289 |
| CrossReference | [MIL-061], [ADR-0039], [RC-287], [RC-288], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-05 | Accepted | Claude (AI) | AI as stand-in for S01 | Initial version; mode autonomous, not independent; the gate of MIL-061 after the deploy of 2026-11-05 | pending |

---

## Artifact Under Review

- Instance reviewed: [MIL-061] at its gate: the eight Go/No-Go criteria, with the evidence of the pull request #514 (merged as 90ef30a on 2026-11-05 at 16:29 UTC), the CD run 42066748612 and two read-only checks of `nf-hotel`, before and after the deploy. Before the gate: [RC-287] (the plan, Go) and [RC-288] (the code and ADR-0039, Go).
- Checklist used: the Go/No-Go criteria of [MIL-061]; [QC-MIL-001] criterion 2 is what makes them the checklist of the gate.
- Scope: the gate; criteria 7 and 8 cite RC-288.
- Language and domain: en / it (the Metadata rows of MIL-061); this record is a technical type.
- Language reviewer: none.
- Mode: autonomous, not independent. The AI wrote the milestone, the code, RC-287, RC-288 and this record; S01 accepted the plan in chat on 2026-11-05 ("Accepter planen"), said when the checks were green, merged through the AI ("Ja, merge"), approved the CD run under "Review deployments" (ADR-0038) and approved the read-only check of the VM in chat ("Ja, efter deploy"). The reviewer is the AI as stand-in for S01, so every decision stays open to S01's challenge.

**Evidence read on 2026-11-05:**

- **The pull request.** #514, head 794f369: Ops CI run 42062747074 with the new job `Ops service networks (MIL-061)` (48 seconds), `Ops nginx routes`, `Ops benchmark results volume (MIL-045)`, `Ops light development mode (MIL-056)` and `Ops secret rotation drill (MIL-035)` (2 minutes 19 seconds, after the fix of 794f369), E2E and DAST run 42062747194 (the end-to-end stack with `check-e2e-stack.sh`), CI 42062747056 and Release check 42062747038, all green; 49 checks passed and 2 skipped, `mergeStateStatus CLEAN`. Merge commit 90ef30a; on `main` the same workflows passed again (Ops CI 42065028300, E2E 42065028219, CI 42065028383), the Wiki run 42065028195 published the five pages, and the Release run 42065800344 built the images.
- **The deploy.** CD run 42066748612 (#164), approved by S01, ran from 16:50:28 to 16:54:10 UTC: it verified the provenance and SBOM of the four digests, backed up the database and the settings twice (before the migrations and before the deploy), applied the migration script, checked out 90ef30a in `/opt/nfhotel`, created the internal network `nfhotel-python`, and recreated the API, the web application, the market analysis, the benchmark with its one-shot service, nginx, the tunnel and Alloy, every one healthy; the smoke test answered 301 on 80 and 200 on 443; "deployed 90ef30a"; no rollback. The older waiting run #146 (41855022250, the release of 2026-11-04) was approved by S01 by mistake at 16:45 UTC and stopped at its own gate step ("not every check of 8b42cff is green; nothing is deployed"), so it changed nothing on the VM.
- **The VM, before.** Read over Tailscale SSH in check mode as `root@nf-hotel` at 16:50:41 UTC, thirteen seconds after the CD job started and before its deploy step reached the VM: no network `nfhotel-python`; the market analysis on `nfhotel-api`, `nfhotel-llm` and `nfhotel-proxy`; the benchmark on `nfhotel-api` and `nfhotel-llm`; the web application on `nfhotel-api` and `nfhotel-proxy`; the API on `nfhotel-api` and `nfhotel-db`; nginx publishing 80 and 443 on the tailnet address and 8081 on `127.0.0.1`, with `listen 8081` twice in its running configuration.
- **The VM, after.** Read the same way at 16:59:45 UTC: `nfhotel-python` exists, `internal=true`, with the benchmark, the API, the web application and the market analysis on it; `nfhotel-proxy` holds nginx, the tunnel and the web application; `nfhotel-api` holds the API, nginx, the web application and Alloy; `nfhotel-llm` holds the benchmark, the market analysis and Ollama; `nfhotel-db` holds the API, PostgreSQL and the relay. Per container: benchmark `llm python`; market analysis `llm python`; API `api db python`; web `api proxy python`; nginx `api proxy`, publishing 80 and 443 only; Alloy `egress internal api`; the socket proxy `internal`; cloudflared `proxy`; PostgreSQL `db`; the relay `db` and its tailnet network, publishing 5432 on the tailnet address; Ollama `llm`. Every container with a health check is healthy (the socket proxy has none, by design). In the nginx container, `nginx -T` holds no `listen 8081`; `getent hosts` does not resolve `market-analysis-api` or `benchmark` and a `wget` to their health paths gets no answer, while `nfhotel-api` resolves and answers 200 and `nfhotel-web` resolves and answers 308 (the controls). From the web container, `GET /health` opened with bash to `market-analysis-api:8000` and to `benchmark:8000` answers `200 OK`; from the API container to `market-analysis-api:8000`, `200 OK`; from the benchmark container to `nfhotel-api:8080`, `200 OK`. The logs of the two Python services hold `GET /health` lines only (their health checks and these probes); no page of the back office had been opened since the deploy. The settings files on the VM were last changed on 2026-10-21 (`nginx.env`, `benchmark.env`), 2026-11-01 (`nfhotel-web.env`, `nfhotel-api.env`) and 2026-11-04 (`market-analysis.env`), all before the deploy. No secret was read and no device address is copied here.

## Checklist Results

| # | Criterion (MIL-061) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The networks of every service equal the target table, read from `docker compose config`, and `nfhotel-python` is created `--internal`; checked by `test-service-networks.sh` in Ops CI | Pass | The job `Ops service networks (MIL-061)` green on 794f369 (run 42062747074) and on `main` at 90ef30a (run 42065028300); on the VM, `docker inspect` shows the table for every container and the network is internal |
| 2 | nginx publishes 80 and 443 only; no `listen 8081`, `NGINX_INTERNAL_PORT` or `ANALYSIS_*` under `ops/`; the mobile routes and the public listener unchanged; `test-nginx.sh` passes | Pass | `docker port` of nginx on the VM: 80 and 443 on the tailnet address, nothing else; `listen 8081` zero times in the running configuration (twice before the deploy); the three `grep` checks of the test green in Ops CI; `Ops nginx routes` green on both runs; the smoke test of the deploy answered 301 and 200 |
| 3 | The isolation is proven in Ops CI with stand-ins | Pass | The ten live checks of `test-service-networks.sh` green on both runs; on the VM the same picture with the real containers: nginx does not resolve the Python services and gets no answer from them, and reaches the API and the web application; the web application, the API and the benchmark reach what they call over `nfhotel-python` |
| 4 | The existing stacks still work: `check-e2e-stack.sh` with the new checks, `test-dev-lite.sh`, `test-benchmark-volume.sh`, `test-deploy-services-env.sh`, `lint.sh`, `test-wiki-coverage.sh` | Pass | E2E and DAST runs 42062747194 and 42065028219 (the end-to-end stack and `check-e2e-stack.sh`), Ops CI runs 42062747074 and 42065028300 (`Ops light development mode`, `Ops benchmark results volume`, the script tests, `Ops linters`, `Ops wiki coverage`), all green; the secret rotation drill green after the fix of 794f369 (RC-288 was written before that fix; the fix names the python network of the stack in the drill, nothing else) |
| 5 | On `nf-hotel` after the deploy: the networks of criterion 1, `docker port` of nginx 80 and 443 only, `market-analysis-api` not resolved from nginx, the two pages work (S01 in the browser, or requests of the web application and the API in the logs of the Python services) | Pass | The read of 16:59 UTC, above. The requests in the logs are the health checks and the probes opened from the web, API and benchmark containers (`200 OK` each); no page was opened yet, so S01 opens the market analysis page and the benchmark page once (action item); the probes already prove that the calls of the two pages reach their services over `nfhotel-python` |
| 6 | One CD run after the merge, approved by S01; smoke test passed, no rollback; the environment files on the VM not changed | Pass | Run 42066748612, 16:50 to 16:54 UTC; the settings files on the VM are dated 2026-10-21, 2026-11-01 and 2026-11-04; the compose defaults named the new network |
| 7 | The wiki pages describe the running design, the open points are closed, ADR-0039 with the amendments is Accepted after a Go against `QC-ADR-001` | Pass | RC-288 (Go, the ADR part), the rows of ADR-0039, ADR-0004 and ADR-0006 Accepted at cfc1eec; the Wiki run 42065028195 published Network Isolation, Containers and Images, Environment Files, Proxmox Deployment and Workflows |
| 8 | The code review against `QC-SH-001`, `QC-YML-001` and `QC-WF-001` is Go | Pass | RC-288 (Go); its two "not yet" items (WF 22, criterion 4) closed with the green runs above |

## Findings

- **The move was one deploy, as designed.** The CD run created the network and recreated the five containers of the move in 90 seconds, between 16:51:49 and 16:53:15 UTC; the tunnel and Alloy were recreated too, because the release carried their new images, not because of the move. The before and after reads show the difference the wiki page describes, and nothing else changed.
- **No page was opened since the deploy.** The gate stands on the probes from the real containers over `nfhotel-python` and on the health checks; the two pages of the back office are the user's check, asked for below. Nothing in the reads suggests they would fail: the web application and the API answer, and they reach the two Python services by the names their settings hold.
- **Two empty networks from before the reconciliation remain**, `nfhotel-backend` and `nfhotel-database` (seen on 2026-11-03 too). They carry no container and are outside this milestone; removing them is housekeeping for S01.
- **The unused lines in the settings files on the VM.** `nginx.env` still holds `ANALYSIS_*` and `NGINX_INTERNAL_PORT`, and `market-analysis.env` and `benchmark.env` the old network names; nothing reads them. Housekeeping for S01, as RC-287 said.
- **A tooling note.** `nslookup` of busybox inside the nginx container does not use Docker's embedded resolver and reported every name as unresolved in the first read; the after read uses `getent` and `wget`, which resolve as nginx does, with the API and the web application as controls.

## Overall Verdict

Go: the eight criteria pass on the green runs of the pull request and of `main`, the CD run of 90ef30a and the read of `nf-hotel` after the deploy. The milestone closes: the issues #512 and #513 and the GitHub milestone #43 close through the pull request of this record. The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Open the market analysis page and the benchmark page of the back office once, and say in chat that they work (criterion 5, the user's check) | S01 | 2026-11-06 |
| Add `Ops service networks (MIL-061)` to the required checks of the ruleset "Approve merges", and change the count on the Workflows wiki page | S01 | Open |
| Remove the unused `ANALYSIS_*` and `NGINX_INTERNAL_PORT` lines from `nginx.env` and the old network names from `market-analysis.env` and `benchmark.env` on the VM; remove the empty networks `nfhotel-backend` and `nfhotel-database`, when convenient | S01 | Open |

---

[MIL-061]: ../../milestones/mil-061-python-services-network.md
[ADR-0039]: ../../adr/adr-0039-python-services-network.md
[RC-287]: ./rc-287-mil-061.md
[RC-288]: ./rc-288-mil-061-code.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
