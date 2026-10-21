# Market analysis service (MIL-004)

Runs `NF-Hotel/Notebook_Market_Analysis` as two containers of one image behind the
nginx proxy: the API (`serve`, port 8000) and the read-only history notebook
(marimo, port 2718). Neither publishes a host port.

## Start

```bash
cp market-analysis.env.example market-analysis.env
cp market-analysis.toml.example market-analysis.toml
docker network create nfhotel-proxy        # once, shared with the proxy
docker compose --env-file market-analysis.env up -d --build
```

Then start the proxy (`deploy/nginx`, `nginx.env` with the `ANALYSIS_*` names). The
notebook answers on `ANALYSIS_SERVER_NAME` and the API on `ANALYSIS_API_SERVER_NAME`.
Send bookings to `POST /analyze?insights=true` on the API host; the notebook shows the
retained results. `scripts/test-compose-proxy.sh` proves both through the proxy.

AI insights need a local model server (Ollama or LM Studio) that the container can
reach; `market-analysis.toml` points at `host.docker.internal`. Without one the result
says the insight is unavailable.

## Gaps against US-001.01 ("see the market analysis with diagrams and an AI analysis")

| Gap | Evidence | Owner | Action |
| --- | --- | --- | --- |
| No charts: the notebook shows tables | The source README says "Not built yet: charts"; the repository has no charting library | S02 | Raise an issue in `NF-Hotel/Notebook_Market_Analysis` for charts; this repository does not change its code |
| AI analysis needs a reachable local model | `[llm]` in the configuration; without one the insight is `unavailable` | S02 | Decide where the model runs; the services run in a VM on Proxmox (ADR-0003), the model on a host the VM can reach |
| No input is loaded by the service itself | A calling system posts the bookings | S02 | Decide who sends the bookings to `/analyze` |
