# ADR-0039 Python services on their own Docker network

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0039 |
| CrossReference | [MIL-061], [ADR-0003], [ADR-0004], [ADR-0006], [ADR-0015], [ADR-0028] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-05 | Accepted | Claude (AI) | AI as stand-in for S01 | Initial version; the design of the wiki page Network Isolation by a developer of S01, which S01 asked in chat on 2026-11-05 to build (MIL-061); accepted after RC-288 Go | [cfc1eec] |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

Two Python services run beside the .NET applications on the VM: the market analysis
(`market-analysis-api`, from `NF-Hotel/notebook-market-analysis`) and the benchmark
(`benchmark`, from `NF-Hotel/notebook-benchmark`). The browser never talks to them. The web
application calls the market analysis for the holiday calendar and the benchmark to start a
run; the API starts market analysis runs and reads their result, and the benchmark delivers
its results to the API. Both call Ollama on `nfhotel-llm`.

Until this decision the market analysis was on `nfhotel-proxy`, `nfhotel-api` and
`nfhotel-llm`, and the benchmark on `nfhotel-api` and `nfhotel-llm`. nginx, the container
that faces the internet through the Cloudflare tunnel ([ADR-0028]), is on `nfhotel-proxy`
and, for the two mobile routes of [ADR-0015], on `nfhotel-api`: it shared two networks with
the market analysis and one with the benchmark, and it published a second listener on
`127.0.0.1:8081` of the VM (`templates/market-analysis.conf.template`) that proxied to the
market analysis API and to a notebook container (`market-analysis-notebook:2718`) that this
repository never deployed. A developer of S01 drew the calls and the networks on the wiki page
Network Isolation (pull request #501, 2026-11-05) and designed the layout below; S01 asked in
chat the same day to build it.

Forces: nginx is the most exposed container, so every network it is on is a network an
attacker who takes it over stands on; the Python services run the most third-party code and
pass text to and from AI models, a wider attack surface than a Blazor page; the database must
stay reachable from the API only ([ADR-0004]); the sign-in, the roles and the rate limits are
checked in the .NET applications, so a second path to the Python services around them is a
hole, not a feature; the change must fit one deploy without a change of the git-ignored
environment files on the VM; the end-to-end stack and the light development mode start the
compose files of the web application and the API and must keep working.

Options considered:

1. **Keep the layout.** No work, but nginx keeps a route to the market analysis on two networks and the listener on 8081 stays, for a notebook that does not exist here.
2. **Take the Python services off `nfhotel-proxy` only** and leave them on `nfhotel-api`. nginx is on `nfhotel-api` for the mobile routes, so it still reaches both services, and the listener on 8081 would have to go anyway, because nginx could no longer resolve the market analysis on the proxy network.
3. **A fourth internal network, `nfhotel-python`, for every call to and from the Python services.** The web application and the API join it; the market analysis and the benchmark leave `nfhotel-proxy` and `nfhotel-api`; nginx is not on it, and the listener on 8081 goes.
4. **A first draft of the developer** put the API on the proxy network and renamed the networks. Rejected in chat on 2026-11-05: the database API would stand on the most exposed network, and the rename touched about thirty files for no gain in isolation.

## Decision

We follow option 3. The Python services are reached only over `nfhotel-python`, a network
nginx cannot see.

- **The network.** `nfhotel-python` is an internal Docker network (`--internal`: no route out, no host port), like `nfhotel-api` and `nfhotel-db`. Its name is `NFHOTEL_PYTHON_NETWORK` beside the four existing names; every compose file gives it the default `nfhotel-python`. `ops/scripts/deploy-services.sh` creates it on the VM when it is missing; `ops/scripts/lib/stack.sh` creates and removes it for the end-to-end stack and the light development mode.
- **Who is on it.** The web application (`nfhotel-proxy`, `nfhotel-api`, `nfhotel-python`), the API (`nfhotel-api`, `nfhotel-db`, `nfhotel-python`), the market analysis (`nfhotel-python`, `nfhotel-llm`) and the benchmark (`nfhotel-python`, `nfhotel-llm`). nginx stays on `nfhotel-proxy` and `nfhotel-api`; Ollama, PostgreSQL, the relay, cloudflared and Alloy do not change.
- **What goes.** The listener of nginx on `127.0.0.1:8081`, its template, the port in the compose file and the variables `ANALYSIS_*` and `NGINX_INTERNAL_PORT`, with the lines of them in the scripts and the tests. The route to the notebook page goes with it: the developer called it a leftover ("Det er et levn der ikke skal med", 2026-11-05), and no notebook container is deployed here. Nothing replaces the route: a person reads the Python services with `docker logs` or a one-off client on the network, on the VM.
- **What stays.** The two mobile routes of nginx to the API on `nfhotel-api` ([ADR-0015]); the public listener on 8082 for the tunnel on `nfhotel-proxy` ([ADR-0028]); Ollama on `nfhotel-llm` with the two AI services only; the addresses the services call (`market-analysis-api:8000`, `benchmark:8000`, `nfhotel-api:8080`, `ollama:11434`), because the names resolve on the new network as they did on the old.
- **Guard.** `ops/tests/test-service-networks.sh`, in Ops CI, reads the networks of every service from `docker compose config` of every compose file the deploy uses and compares them with the table above, checks that only nginx (80, 443) and the relay (5432) publish a host port, and proves with stand-ins that the proxy network reaches none of the Python services while the python network reaches them.
- **Rules for a new service** (from the wiki page): start with no network and add one only for a call you can name; `nfhotel-proxy` only when nginx must route to the service; a service that only answers the .NET applications goes on `nfhotel-python` or a network of its own, never on `nfhotel-api` or `nfhotel-proxy`; no published host port (the tailnet is the way for a person, [ADR-0034] is the example); the picture, the call table, the wiki page Containers and Images and the table of the test change in the same pull request.
- **The move.** One deploy (the CD run of the merge, approved by S01) recreates the four containers and nginx; the environment files on the VM are not changed, because the compose files carry the default name. The lines of the old listener in `nginx.env` on the VM become unused.

## Consequences

**Positive:**

- A taken-over nginx can open no connection to the Python services; the only path to them is through a signed-in user of the .NET applications, where the roles and the rate limits are checked.
- The Python services, the code we control least, are not reachable from the front door, and a flaw in them is not reachable from the internet.
- The model server stays behind the two AI services; nothing else can ask it for answers or tell it to download or delete models.
- The question "what can this container reach?" has a short answer per service, read from its compose file and kept true by a test in Ops CI.
- The database stays reachable from the API and the relay only ([ADR-0004]); the Python services get the bookings in the request and never join `nfhotel-db`.

**Negative:**

- One more network to create and name, and one more row in every table that lists them.
- The market analysis, the benchmark, the web application, the API and nginx are recreated once, in one deploy.
- There is no longer a way to open the market analysis API from a developer's machine through an SSH tunnel; the back office, `docker logs` and a one-off client on the network on the VM take its place.
- A Python service that one day needs a route from the browser needs a new decision, not a line in nginx.
- The network tables of the compose files, the scripts, the wiki and the test must move together; the test fails the pull request that forgets one.

Quality attributes: security improves (a smaller reach for a compromise of nginx, defence in depth around the model server); maintainability improves (one table, tested); operability is neutral (one network more, created by the deploy); performance is unchanged (the same hops, on another network).

## Affected Artifacts

- [MIL-061] — decided here; its tasks build and deploy it
- [ADR-0004] — the network bullet is amended: the API joins three networks, and nginx routes the two mobile routes to it
- [ADR-0006] — the internal-listener bullet is amended: the listener on 8081 is removed
- [ADR-0003] — the service picture has one more network and one published listener less
- [ADR-0015], [ADR-0028] — unchanged; they say what nginx keeps

---

[MIL-061]: ../milestones/mil-061-python-services-network.md
[ADR-0003]: ./adr-0003-proxmox-vm-for-services.md
[ADR-0004]: ./adr-0004-database-api.md
[ADR-0006]: ./adr-0006-tls.md
[ADR-0015]: ./adr-0015-mobile-app-staff-token-access.md
[ADR-0028]: ./adr-0028-public-access-through-cloudflare-tunnel.md
[ADR-0034]: ./adr-0034-team-database-access.md
[cfc1eec]: https://github.com/NF-Hotel/NFHotel/commit/cfc1eeccada36dd1404eab566b161b4ee95c1ceb
