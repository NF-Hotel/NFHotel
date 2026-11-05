# MIL-061 Python services on their own Docker network

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-061 |
| CrossReference | [BC-001], [ADR-0004], [ADR-0006], [ADR-0015], [ADR-0028], [ADR-0038], [MIL-015], [MIL-020], [MIL-026] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-05 | Accepted | mikemike | AI as stand-in for S01 | Initial version; the target design of the wiki page Network Isolation, written by a developer of S01 and merged on 2026-11-05 (pull request #501), which S01 asked in chat to build ("Nu må du gerne starte med at lave det som Jens ønsker det", now build it as Jens wants it); S01 accepted the plan in chat on 2026-11-05 ("Accepter planen") after RC-287 Go | pending |

---

## Purpose

Decide whether the two Python services, the market analysis (`market-analysis-api`) and the benchmark (`benchmark`), move to a Docker network of their own that nginx cannot see, as the wiki page Network Isolation designs it. Today the market analysis is on `nfhotel-proxy`, `nfhotel-api` and `nfhotel-llm`, and the benchmark on `nfhotel-api` and `nfhotel-llm`: nginx, the container that faces the internet through the Cloudflare tunnel ([ADR-0028]), shares two networks with the market analysis and one with the benchmark, and publishes a second listener on `127.0.0.1:8081` of the VM that proxies to the market analysis API and to a notebook container that this repository does not deploy. The browser never talks to the Python services; `nfhotel-web` and `nfhotel-api` do, for a signed-in user with the right role.

The target design: a new internal network `nfhotel-python` carries every call to and from the Python services; `nfhotel-web` and `nfhotel-api` join it; the market analysis and the benchmark leave `nfhotel-proxy` and `nfhotel-api`; nginx stays on `nfhotel-proxy` and `nfhotel-api` (the two mobile routes of [ADR-0015]) and loses the listener on 8081, so a taken-over nginx can open no connection to the Python services, and the model server Ollama stays reachable from the two AI services only (`nfhotel-llm`). The developer who designed it settled the open points of the page in chat on 2026-11-05: the notebook route is a leftover and goes ("Det er et levn der ikke skal med"); the market analysis reaches Ollama as `ollama:11434` on `nfhotel-llm`, as `market-analysis.env.example` already says; the services and the nginx change move in one deploy.

## Deliverable

- **ADR-0039**, which records the decision and its reasons (the seven of the wiki page), and amends the network bullet of [ADR-0004] (the API joins three networks; nginx routes the two mobile routes to it) and the internal-listener bullet of [ADR-0006] (the listener on 8081 is removed), each with a Version History row.
- **The network `nfhotel-python`**, internal like `nfhotel-api` and `nfhotel-db`, named by `NFHOTEL_PYTHON_NETWORK` beside the four existing names: `ops/scripts/deploy-services.sh` creates it on the VM when it is missing, and `ops/scripts/lib/stack.sh` creates and removes it for the end-to-end stack and the light development mode, which start the compose files of the web application and the API.
- **The compose files and their environment examples** moved to the target table of the wiki page: `nfhotel-web` on proxy, api, python; `nfhotel-api` on api, db, python; `market-analysis` on python, llm; `benchmark` on python, llm; nginx, Ollama, PostgreSQL, the relay, cloudflared and Alloy unchanged. The compose files keep a default for the new name, so the environment files on the VM need no change for the deploy.
- **nginx without the internal listener**: `templates/market-analysis.conf.template`, the port line `127.0.0.1:8081` and the variables `ANALYSIS_*` and `NGINX_INTERNAL_PORT` are removed, from the compose file, the environment example, the stack library, the smoke test `ops/scripts/test-compose-proxy.sh`, `ops/scripts/deploy.sh` and the tests. The ports 8081 and 2718 stay in the lists of ports that must not answer (`ops/scripts/check-tailnet-exposure.sh`, the scope step of `ops/scripts/cd-steps.sh`).
- **Tests**: a new `ops/tests/test-service-networks.sh` in its own Ops CI job, which reads `docker compose config` of every compose file under `ops/deploy/` and compares the networks of every service with the target table, checks that only nginx and the relay publish a host port (nginx 80 and 443 only), and proves the isolation live with stand-ins on throwaway networks; the existing tests follow the move.
- **Documentation**: the wiki pages Network Isolation (the design is the running one; the three open points closed), Containers and Images, Environment Files and Proxmox Deployment.
- **One deploy**: the CD run after the merge, approved by S01 ([ADR-0038]), recreates the four containers and nginx in one run; then the gate reads the VM.

Order: the review of this document first (QC-MIL-001 and QC-LANG-001), then tasks 1 to 6 on one branch, task 7 with the pull request and the deploy, then task 8.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | For every compose file under `ops/deploy/`, the networks of every service, read from `docker compose config`, equal the target table of the wiki page Network Isolation (nginx: proxy, api; `nfhotel-web`: proxy, api, python; `nfhotel-api`: api, db, python; market analysis: python, llm; benchmark: python, llm; Ollama: llm; PostgreSQL: db; the relay: db and its tailnet network; cloudflared: proxy; Alloy unchanged), and `nfhotel-python` is created `--internal`; checked by `ops/tests/test-service-networks.sh` in Ops CI | Yes | No |
| 2 | nginx publishes 80 and 443 only (the development ports of `docker-compose.dev.yml` and the light mode aside): no `listen 8081`, no `NGINX_INTERNAL_PORT` and no `ANALYSIS_*` variable remains under `ops/`; the two mobile routes on `nfhotel-api` ([ADR-0015]) and the public listener 8082 on `nfhotel-proxy` ([ADR-0028]) are unchanged, and `ops/tests/test-nginx.sh` passes | Yes | No |
| 3 | The isolation is proven in Ops CI: with stand-ins attached to the networks the compose files assign, a client on the proxy network cannot connect to `market-analysis-api:8000` or to `benchmark:8000`, and a client on the python network gets an answer from both and from `nfhotel-api:8080` | Yes | No |
| 4 | The existing stacks still work: the end-to-end stack (`check-e2e-stack.sh`, with the new check that nginx is not on the python network), the light development mode (`test-dev-lite.sh`), `test-benchmark-volume.sh`, `test-deploy-services-env.sh`, `lint.sh` and `test-wiki-coverage.sh` pass in Ops CI | Yes | No |
| 5 | On `nf-hotel` after the deploy, read in check mode: `docker inspect` shows for every container the networks of criterion 1; `docker port` of nginx lists 80 and 443 only; the name `market-analysis-api` does not resolve from the nginx container; the market analysis page and the benchmark page of the back office work (S01 in the browser, or requests of `nfhotel-web` and `nfhotel-api` dated after the deploy in the logs of the two Python services) | Yes | No |
| 6 | The move was one CD run after the merge, approved by S01 ([ADR-0038]): its smoke test passed and no rollback ran; the environment files on the VM were not changed for it | Yes | No |
| 7 | The wiki pages Network Isolation, Containers and Images, Environment Files and Proxmox Deployment describe the running design, the open points of Network Isolation are closed, and ADR-0039 with the amendments of [ADR-0004] and [ADR-0006] is Accepted after a Go against `QC-ADR-001` | Yes | No |
| 8 | The code review against `QC-SH-001` (the scripts and tests), `QC-YML-001` (the compose files, the environment examples) and `QC-WF-001` (the Ops CI job) is Go | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-015 | Built the API on the internal networks `nfhotel-api` and `nfhotel-db` ([ADR-0004]); the API now joins `nfhotel-python` too |
| MIL-020 | Put the benchmark on `nfhotel-api` so the web application and the API reach it; it moves to `nfhotel-python` |
| MIL-026 | Put the market analysis on `nfhotel-api` as `market-analysis-api`, with the listener of nginx on 8081; it moves to `nfhotel-python` and the listener goes |
| ADR-0015 | nginx stays on `nfhotel-api` for the two mobile routes; nothing else of nginx changes |
| ADR-0028 | The tunnel reaches nginx on `nfhotel-proxy`; the path of the public site is untouched |
| ADR-0038 | The deploy of the move waits for the approval of S01 |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001 objective 18 and success criteria 21, 23 and 28: the database only through an API that is not exposed; no host port for the API and PostgreSQL; only ports 80 and 443 of nginx answer from outside. The move narrows what a compromise of nginx can reach, in the same direction | Tasks 1 to 8 |
| The wiki page Network Isolation (the design, 2026-11-05) | Criteria 1, 3, 7 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |
| Design proposed by | S01 (a developer of Team 2, on the wiki page Network Isolation) |

## Target Date

2026-11-12. It changes deploy files, scripts, tests and documentation, so it does not move a date of the Business Case.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | ADR-0039 and the amendments of ADR-0004 and ADR-0006 | Record the decision of the wiki page Network Isolation as `docs/adr/adr-0039-python-services-network.md`: the network `nfhotel-python`, who is on it, the seven reasons, what is dropped (the listener on 8081 and the notebook route, a leftover by the designer's decision of 2026-11-05) and the consequences (one more network, one deploy that recreates four containers and nginx, the stale `ANALYSIS_*` and `NGINX_INTERNAL_PORT` lines of `nginx.env` on the VM become unused). Amend the network bullet of ADR-0004 (the API joins `nfhotel-api`, `nfhotel-db` and `nfhotel-python`; nginx routes the two mobile routes of ADR-0015 to it) and the internal-listener bullet of ADR-0006 (removed), each with a Version History row that names ADR-0039. | No | criterion 7 |
| 2 | The network `nfhotel-python` in the scripts | `ops/scripts/deploy-services.sh` reads `NFHOTEL_PYTHON_NETWORK` (default `nfhotel-python`) beside the four existing names and creates the network `--internal` when it is missing, and its header describes the networks as they are after the move. `ops/scripts/lib/stack.sh` gets `STACK_PYTHON_NET`: created internal by `stack_networks`, passed to the compose calls, removed by `stack_down`, because the compose files of the web application and the API now name a python network. `ops/scripts/create-docker-vm.sh` is unchanged: `deploy-services.sh` creates the networks on the VM. | No | criteria 1, 4 |
| 3 | The compose files and the environment examples | `ops/deploy/market-analysis/docker-compose.yml` from proxy, llm, api to python, llm; `ops/deploy/benchmark/docker-compose.yml` from llm, api to llm, python; `ops/deploy/nfhotel-web/docker-compose.yml` and `ops/deploy/nfhotel-api/docker-compose.yml` add python, each network block with `name: ${NFHOTEL_PYTHON_NETWORK:-nfhotel-python}` and `external: true`. The four `*.env.example` files get `NFHOTEL_PYTHON_NETWORK=nfhotel-python` and lose the names they no longer use; `NFBENCH_WEB_URL=http://nfhotel-api:8080` and `OLLAMA_BASE_URL=http://ollama:11434` stay. The header comments say which network carries which call, and the stale comment of the API's compose file ("shared with the web service only") goes. | No | criteria 1, 6 |
| 4 | nginx without the internal listener | Delete `ops/deploy/nginx/templates/market-analysis.conf.template`; remove the port line `127.0.0.1:${NGINX_INTERNAL_PORT:-8081}:8081` and its comment from `ops/deploy/nginx/docker-compose.yml`, the `ANALYSIS_*` and `NGINX_INTERNAL_PORT` lines from `nginx.env.example`, the `ANALYSIS_*` lines that `ops/scripts/lib/stack.sh` writes, the analysis part of `ops/scripts/test-compose-proxy.sh`, the tunnel lines of `ops/scripts/deploy.sh`, the comment in `ops/deploy/dev-lite/nginx-port.yml` and the sentence in `ops/deploy/market-analysis/README.md`. `ops/scripts/check-tailnet-exposure.sh` and the scope step of `ops/scripts/cd-steps.sh` keep 8081 and 2718 among the ports that must not answer. | No | criterion 2 |
| 5 | Tests of the networks | New `ops/tests/test-service-networks.sh`, run by a new job of `.github/workflows/ops-ci.yaml`: for every compose file under `ops/deploy/`, `docker compose config --format json` gives the networks per service, compared with the target table; only nginx and the relay publish a host port, nginx 80 and 443 only; no `listen 8081`, `NGINX_INTERNAL_PORT` or `ANALYSIS_` under `ops/`; then stand-ins (`hashicorp/http-echo`, as `test-nginx.sh` uses) named `market-analysis-api`, `benchmark` and `nfhotel-api` are attached to throwaway networks as the compose files assign them, and a curl client proves that the proxy network reaches none of them and the python network reaches all three. `ops/tests/test-nginx.sh` loses its 8081 part; `ops/tests/test-optional-services.sh` (manual, needs the images) and `ops/tests/test-benchmark-volume.sh` use a python network instead of the api network, the first with the negative check from the proxy network; `ops/tests/check-e2e-stack.sh` adds the check that nginx is not on the python network. | No | criteria 1, 2, 3, 4 |
| 6 | Documentation | Wiki page Network Isolation: "What changes from today" becomes what changed and when (MIL-061), the three open points are closed with the decisions of 2026-11-05 (the notebook route dropped, `ollama:11434`, one deploy), the rules for a new service stay. Containers and Images: the rows of nginx (no internal listener, no 8081), the web application, the API, the market analysis and the benchmark, and the networks table with `nfhotel-python`. Environment Files: `NFHOTEL_PYTHON_NETWORK` in the rows of the four files, the `ANALYSIS_*` row removed. Proxmox Deployment: the SSH tunnel to 8081 removed. `bash ops/tests/test-wiki-coverage.sh` passes. | No | criterion 7 |
| 7 | Code review, pull request and deploy | The review record of the code against `QC-SH-001` (`deploy-services.sh`, `stack.sh`, `test-compose-proxy.sh`, `deploy.sh`, the tests), `QC-YML-001` (the compose files, the environment examples) and `QC-WF-001` (the Ops CI job), and of ADR-0039 with the two amendments against `QC-ADR-001`, in one record before the pull request. The pull request closes one issue per task, carries the milestone and is merged in autonomous mode once every required check is green; the Release and the CD run follow, and S01 approves the deploy (ADR-0038). The deploy is one run: the four containers and nginx are recreated, the smoke test passes, nothing is changed in the environment files on the VM. | No | criteria 6, 8 |
| 8 | Gate: the read on nf-hotel and the review record | After the deploy, a read-only check on `nf-hotel` over Tailscale SSH in check mode, under an approval of S01 in chat: `docker inspect` of the networks of every container, `docker port` of nginx, the resolution of `market-analysis-api` from the nginx container, and the requests of the web application and the API in the logs of the two Python services, or S01's check of the market analysis page and the benchmark page in the browser. The gate review record against criteria 1 to 8, the row of MIL-061 in the traceability matrix, the Accepted row of this document, and the closing of the GitHub milestone. | No | criteria 1 to 8 |

---

[BC-001]: ../business-case.md
[ADR-0004]: ../adr/adr-0004-database-api.md
[ADR-0006]: ../adr/adr-0006-tls.md
[ADR-0015]: ../adr/adr-0015-mobile-app-staff-token-access.md
[ADR-0028]: ../adr/adr-0028-public-access-through-cloudflare-tunnel.md
[ADR-0038]: ../adr/adr-0038-approval-before-a-deploy.md
[MIL-015]: ./mil-015-database-api.md
[MIL-020]: ./mil-020-start-benchmark-run.md
[MIL-026]: ./mil-026-market-analysis-report.md
