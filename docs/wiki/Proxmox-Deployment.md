# Proxmox Deployment

The development environment is one virtual machine (VM) on Proxmox VE. Every service runs as a
Docker container inside that VM (decision: ADR-0003).

## What runs in the VM

| Container | Source | Reachable from |
| --- | --- | --- |
| `nginx` | `nginx:1.27-alpine`, `deploy/nginx` | Internet (port 80), serves only NFHotel |
| `nfhotel-web` | this repository, `deploy/nfhotel-web` | through nginx only |
| `postgres` | `postgres:16-alpine`, `deploy/postgres` | `nfhotel-web` only, internal network |
| `ollama` | `ollama/ollama`, `deploy/ollama` | benchmark and market analysis only, network `nfhotel-llm` |
| `market-analysis-api`, `market-analysis-notebook` | `NF-Hotel/Notebook_Market_Analysis`, `deploy/market-analysis` | VM loopback only |
| `benchmark` | `NF-Hotel/notebook-benchmark`, `deploy/benchmark` | VM loopback only |

### Network exposure

- Only nginx publishes a port to the outside, and on that port it serves only the NFHotel web
  application. Any host name sent to port 80 reaches NFHotel.
- The analysis and benchmark notebooks are served by a second nginx listener on `127.0.0.1:8081`
  inside the VM. Reach them with an SSH tunnel (below).
- PostgreSQL sits on the Docker network `nfhotel-db`, created with `--internal`: it has no route
  out and no published port. Only `nfhotel-web` joins it.
- Ollama sits on the network `nfhotel-llm`, joined only by the benchmark and the analysis service.
  It publishes no port and nginx does not route to it. The network has outbound access so Ollama
  can download models.
- The proxy network `nfhotel-proxy` joins nginx, the web application, the analysis service and
  the benchmark.

## VM size

12 GiB memory (12288 MiB), 4 vCPU, 60 GiB disk, Debian 12 with Docker and the compose plugin,
built from the official cloud image by cloud-init. Change the size in `deploy.env`.

## Deploy everything

One command creates the VM if it is missing, then builds and starts every container in it.

1. Copy `deploy.env.example` to `deploy.env` and set `PVE_SSH` (`user@host` of the Proxmox node).
   The node and the VM need key login.
2. Copy each `deploy/<service>/*.env.example` to the matching `.env` file and fill it in:
   `postgres.env`, `ollama.env`, `nfhotel-web.env`, `market-analysis.env`, `benchmark.env`, `nginx.env`.
   For analysis also copy `market-analysis.toml.example` to `market-analysis.toml`.
3. Put the database user, password and name in `postgres.env` only. Use a password without `;`
   or quotes (`openssl rand -hex 24`). The web service builds its connection string from that
   file, so the password is never repeated in `nfhotel-web.env`.
4. Run:

```bash
bash scripts/deploy.sh
```

| Option | Effect |
| --- | --- |
| `--dry-run` | print the Proxmox commands that create the VM and stop |
| `--skip-vm` | deploy to the existing VM, do not create or start it |
| `--no-build` | reuse existing images |
| `--down` | stop and remove the containers; data volumes are kept |
| `postgres`, `ollama`, `web`, `analysis`, `benchmark`, `nginx` | deploy only the named services |

The script is safe to run again. It finds the VM by name, waits for its address, ssh, cloud-init
and Docker, updates the repository in `/opt/nfhotel` inside the VM, copies the git-ignored
settings files over ssh, and runs `scripts/deploy-services.sh` there.

## Environment and connection string

The VM is the development environment, so `nfhotel-web.env` sets
`ASPNETCORE_ENVIRONMENT=Development`. The application reads its database from
`ConnectionStrings__HotelDatabase`. `deploy/nfhotel-web/docker-compose.yml` builds it from
the `POSTGRES_USER`, `POSTGRES_PASSWORD` and `POSTGRES_DB` values of `deploy/postgres/postgres.env`
(`scripts/deploy-services.sh` passes them to compose) and points at the PostgreSQL container by
name:

```text
Host=postgres;Port=5432;Database=<POSTGRES_DB>;Username=<POSTGRES_USER>;Password=<from postgres.env>
```

Settings files are git-ignored and never committed.

## Models (Ollama)

List the models to download in `OLLAMA_PULL_MODELS` of `ollama.env` (space separated); the deploy
pulls them after Ollama is healthy. The VM has 12 GiB of memory and no GPU by default, so use small
models, for example `llama3.2:3b`. To add one later:

```bash
docker compose --env-file ollama.env exec ollama ollama pull <model>
```

Run that in `/opt/nfhotel/deploy/ollama` in the VM. Both services find Ollama at `http://ollama:11434`.
The market analysis configuration (`market-analysis.toml`) points there. The benchmark notebook has
`localhost:11434` built in, so its container forwards that port to the `ollama` container. A GPU
passed through to the VM is described in the comments of `deploy/ollama/docker-compose.yml`.

## Reach the internal services

```bash
ssh -L 8081:127.0.0.1:8081 nfhotel@<vm address>
```

Then open `http://analysis.localhost:8081` (market analysis notebook),
`http://analysis-api.localhost:8081` (analysis API) or `http://benchmark.localhost:8081`
(benchmark notebook).

## Known limits

- Ollama runs on the CPU, so answers are slow. LM Studio is not part of the VM; the benchmark
  shows it as unavailable.
- Making port 80 of the VM reachable from the internet (router, firewall, DNS) is outside these
  scripts.
- The deploy applies no database migrations and the application does not run them at start.
  Apply the Entity Framework migrations of `NFHotel.Infrastructure` to the new database yourself
  before first use.
