# Environment Files

This page explains every `*.env` file of the deployment: what it is for, who reads it, which keys matter and which ones
are secrets. The files live in `ops/` and `ops/deploy/<service>/`.

## How the files work

- Each file is made by copying its `*.env.example` to the name without `.example`. The example is in git, the copy is
  not: `ops/.gitignore` ignores `*.env`, so a secret never reaches a commit.
- Values are plain text. Do not quote them.
- `scripts/dev-up.sh` makes a missing file from its example, with fresh random secrets. **A file that exists is never
  changed.** So when an example changes (a new key, a new path), your copy keeps the old value. After you update the
  repository, compare them:

  ```bash
  diff ops/deploy/benchmark/benchmark.env ops/deploy/benchmark/benchmark.env.example
  ```

  Only the secrets and the commit in `BENCHMARK_REF` should differ. A stale copy is a common cause of errors such as
  "file not found" for a dataset path.
- `scripts/deploy-services.sh` copies the files to the VM with the working tree, and passes some values from one file
  to another service so that a secret is written in one place only (see the table below).
- A key written as `A__B` is the configuration key `A:B` of ASP.NET (`__` replaces the colon).
- Never commit an `*.env` file and never paste its content into a chat, an issue or a log.

## Overview

| File | Service | Used when | Holds secrets |
| --- | --- | --- | --- |
| `ops/pve.env` | Proxmox VE container | local development | yes: root password |
| `ops/deploy.env` | `deploy.sh` (the VM) | every deploy | no |
| `ops/deploy/postgres/postgres.env` | PostgreSQL | always | yes: database password |
| `ops/deploy/nfhotel-api/nfhotel-api.env` | database API | always | yes: four |
| `ops/deploy/nfhotel-web/nfhotel-web.env` | web application | always | yes: passport key |
| `ops/deploy/market-analysis/market-analysis.env` | market analysis | always | yes: API key |
| `ops/deploy/ollama/ollama.env` | Ollama (AI models) | always | no |
| `ops/deploy/benchmark/benchmark.env` | benchmark service | always | yes: API token |
| `ops/deploy/nginx/nginx.env` | nginx proxy | always | no |
| `ops/deploy/cloudflared/cloudflared.env` | Cloudflare Tunnel | public access only | yes: tunnel token |
| `ops/deploy/postgres-tailnet/postgres-tailnet.env` | database relay | team access only | no |
| `ops/deploy/alloy/alloy.env` | Alloy (telemetry) | monitoring only | no |
| `ops/deploy/monitoring/monitoring.env` | Grafana, Prometheus, Loki | monitoring host only | no |
| `ops/deploy/backup-target/backup-target.env` | backup target | backup host only | public key only |

`dev-up.sh` also writes `ops/deploy/nfhotel-api/dev-staff.env` (git-ignored) with the passwords of the three
development back office accounts. It is for development only and is never made for a release.

## The files

### `ops/pve.env`

The root password of the throwaway Proxmox VE container of local development. Key: `PVE_PASSWORD`. The API token is made
for each run by `scripts/pve-token.sh` and is never stored.

### `ops/deploy.env`

Tells `ops/scripts/deploy.sh` where the VM is and how large it is.

| Key | Meaning |
| --- | --- |
| `PVE_SSH` | The Proxmox node as `user@host` (key login). Required for a real server |
| `PVE_CONTAINER` | Development: the name of the local Proxmox container. Set it and leave `PVE_SSH` empty |
| `VM_NAME`, `VM_ID` | Name of the VM. Empty `VM_ID` takes the next free id; set it to adopt an existing VM |
| `VM_CORES`, `VM_MEMORY`, `VM_DISK` | Size: cores, memory in MiB (12288 = 12 GiB), disk in GiB |
| `VM_STORAGE`, `VM_SNIPPETS`, `VM_BRIDGE` | Proxmox storage, snippet storage and network bridge (`local` storage for the local container) |
| `SSH_KEY_FILE` | Public key installed for the VM user `nfhotel` |
| `VM_IP` | Set it when the guest agent cannot report the address |
| `REPO_URL`, `REPO_REF`, `APP_REPO_URL`, `APP_REF` | Without `--local`: the repositories and branches the VM builds from |

### `postgres/postgres.env`

`POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB` create the database. The password should be long and random without `;`
or quotes (`openssl rand -hex 24`). The API reads user, password and database from this file, so the password is written
here only. `NFHOTEL_DB_NETWORK` is the internal Docker network of the database.

### `nfhotel-api/nfhotel-api.env`

The database API. It is the only service that holds the database login and the keys to the personal data.

| Key | Meaning |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Development` shows the development passwords at the end of `dev-up.sh`. Never use it on a release |
| `Encryption__PassportKey` | Key that encrypts passport numbers: 32 random bytes, base64 (`openssl rand -base64 32`). **Secret.** Losing it makes the stored numbers unreadable. It must be the same in the web file |
| `HotelTime__TimeZoneId` | Time zone of the hotel, `Europe/Copenhagen` |
| `Api__Token` | Token the web application and the benchmark service present to the API, at least 32 characters. **Secret.** The deploy passes it to the other services, so it is written here only |
| `Staff__Token__SigningKey` | Key that signs staff sign-in tokens, at least 32 characters. **Secret.** Only the API holds it |
| `Staff__InitialManager__*` | The first Manager, made at start when no staff account exists. The password has at least 12 characters and must be changed at the first sign-in. Delete the password line afterwards |
| `Database__MigrateOnStart` | Apply the pending migrations when the API starts. The deploy relies on it |
| `MarketAnalysisService__BaseUrl`, `__TimeoutSeconds`, `__Token` | Where the API sends bookings for analysis (`market-analysis-api:8000`). The timeout is long (90000 seconds) because the AI models are slow. The token is only for a gateway in front of the service. An empty address makes the analysis page say the service is not set up |
| `NFHOTEL_API_NETWORK`, `NFHOTEL_DB_NETWORK` | Docker networks shared with the web service and with PostgreSQL |

### `nfhotel-web/nfhotel-web.env`

The web application. It has no database login: it calls the API.

| Key | Meaning |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Development` in the development VM |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | Trust the `X-Forwarded-*` headers set by nginx |
| `Tls__Require` | Redirect HTTP to HTTPS and mark the staff cookie Secure. True behind the proxy |
| `Encryption__PassportKey` | Same value as in the API file. **Secret** |
| `HotelTime__TimeZoneId` | Time zone of the hotel |
| `HolidayApi__BaseUrl`, `__TimeoutSeconds`, `__ApiKey` | The holiday calendar of the analysis service. The key must equal `API_TOKEN` of `market-analysis.env` |
| `BenchmarkService__BaseUrl`, `__TimeoutSeconds`, `__Token` | The benchmark service. The token is `NFBENCH_API_TOKEN` of `benchmark.env`. Empty means the Start page says the service is not set up |
| `NFHOTEL_PROXY_NETWORK`, `NFHOTEL_API_NETWORK` | Docker networks shared with nginx and with the API |

### `market-analysis/market-analysis.env`

The market analysis service, built from a pinned commit of `NF-Hotel/notebook-market-analysis`.

| Key | Meaning |
| --- | --- |
| `APP_ENV` | `production` requires `API_TOKEN` and explicit CORS origins |
| `API_TOKEN` | The key callers send in `X-API-Key`, at least 32 characters (`openssl rand -hex 32`). **Secret.** The web application sends the same value as `HolidayApi__ApiKey`; `dev-up.sh` writes both |
| `CORS_ALLOWED_ORIGINS` | Only satisfies the production rule: the web application calls from the server, not from a browser |
| `DATA_SOURCE` | `csv`: the service serves holidays for now and reads no market data |
| `AI_PROVIDER`, `OLLAMA_BASE_URL`, `OLLAMA_MODEL`, `AI_TIMEOUT_SECONDS` | The AI model of a run. The deploy writes `OLLAMA_MODEL` from the default AI model chosen in the back office. Empty means no AI narrative |
| `NFHOTEL_PROXY_NETWORK`, `NFHOTEL_LLM_NETWORK`, `NFHOTEL_API_NETWORK` | Docker networks shared with nginx, with Ollama and with the web application and API |
| `MARKET_ANALYSIS_REPO`, `MARKET_ANALYSIS_REF` | Repository and commit the image is built from. Upgrade by changing the commit and regenerating `requirements.lock` (see the README of the folder) |

### `ollama/ollama.env`

No secret. `OLLAMA_KEEP_ALIVE` is how long a model stays in memory after its last request. `OLLAMA_MAX_LOADED_MODELS=1`
and `OLLAMA_NUM_PARALLEL=1` keep the CPU-only 12 GiB VM within memory, because the benchmark and the analysis share this
Ollama. `OLLAMA_PULL_MODELS` lists the models the deploy pulls, separated by spaces (empty pulls none).
`NFHOTEL_LLM_NETWORK` is the network shared with the benchmark and the analysis.

### `benchmark/benchmark.env`

| Key | Meaning |
| --- | --- |
| `NFBENCH_API_TOKEN` | Token the web application must send, at least 32 characters (`openssl rand -hex 32`). **Secret.** Copy it to `BenchmarkService__Token` of the web file |
| `NFBENCH_WEB_URL` | The API on the internal network. Its token is `Api__Token`, passed by the deploy as `NFBENCH_WEB_TOKEN` |
| `NFBENCH_OLLAMA_URL` | The Ollama of NFHotel |
| `NFBENCH_DATASET` | Path of the dataset **inside the image**. The built-in one is `/opt/benchmark/data/example/synthetic_hotel_bookings.csv` |
| `NFBENCH_DATA_DIR` | Where results and unsent results are kept (the volume `results`) |
| `NFHOTEL_API_NETWORK`, `NFHOTEL_LLM_NETWORK` | Docker networks made by NFHotel |
| `BENCHMARK_REPO`, `BENCHMARK_REF` | Repository and commit the image is built from. Upgrade by changing the commit |

### `nginx/nginx.env`

The proxy that publishes the site. No secret.

| Key | Meaning |
| --- | --- |
| `COMPOSE_FILE` | Development only: adds the `/mobile-api` route. `dev-up.sh` sets it; leave it unset on a release |
| `NGINX_SERVER_NAME` | Host name the proxy answers to; `_` matches any |
| `NGINX_REDIRECT_PORT`, `NGINX_HTTPS_PORT`, `NGINX_HTTP_PORT`, `NGINX_BIND` | The ports and address the proxy is published on. Set the redirect port to `:<port>` when the HTTPS port is not 443 |
| `NGINX_HSTS` | `Strict-Transport-Security` value. Empty with a self-signed certificate; `max-age=31536000` with a real one |
| `NGINX_TLS_DIR` | Folder with `fullchain.pem` and `privkey.pem` (git-ignored) |
| `UPSTREAM_HOST`, `UPSTREAM_PORT` | The web application |
| `ANALYSIS_*`, `ANALYSIS_API_*` | Host names and upstreams of the analysis notebook and API (`*.localhost` locally) |
| `NGINX_INTERNAL_PORT` | Loopback port of the internal listener for the analysis (not public) |
| `NFHOTEL_PROXY_NETWORK` | Docker network shared with the web service |

### `cloudflared/cloudflared.env`

Only for public access. `TUNNEL_TOKEN` is the token of the Cloudflare tunnel. **Secret.** The deploy writes it into a secret
file and never into the container's environment. Without this file the deploy leaves the tunnel out and the site stays on
the tailnet. Write the token as [Public Access](Public-Access) describes, so it does not reach the shell history.

### `postgres-tailnet/postgres-tailnet.env`

Only for team access to the database. `NFHOTEL_TAILNET_IP` is the tailnet address of the VM (`tailscale ip -4`); the relay
publishes 5432 on that address only. Without this file no database port is published. See
[Database Access](Database-Access).

### `alloy/alloy.env` and `monitoring/monitoring.env`

Only when the monitoring runs. Alloy sends metrics and logs to `PROMETHEUS_REMOTE_WRITE_URL` and `LOKI_PUSH_URL` (the
tailnet address of the monitoring host). `MONITORING_BIND` is the tailnet address on which Grafana, Prometheus and Loki
are published. See [Monitoring](Monitoring).

### `backup-target/backup-target.env`

On the backup host. `BACKUP_PUBLIC_KEY` is the public key of root on the server (not secret), `BACKUP_TARGET_BIND` and
`BACKUP_TARGET_PORT` are the tailnet address and port of ssh, and `BACKUP_TARGET_DIR` is where the encrypted backups land.
See [Disaster Recovery](Disaster-Recovery).

## Secrets that must match

| Secret | Written in | Also used by |
| --- | --- | --- |
| `Api__Token` | `nfhotel-api.env` | web and benchmark, passed by the deploy |
| `Encryption__PassportKey` | `nfhotel-api.env` | `nfhotel-web.env`, copy by hand: the two must be equal |
| `API_TOKEN` | `market-analysis.env` | `HolidayApi__ApiKey` in `nfhotel-web.env`, copy by hand |
| `NFBENCH_API_TOKEN` | `benchmark.env` | `BenchmarkService__Token` in `nfhotel-web.env`, copy by hand |
| `POSTGRES_PASSWORD` | `postgres.env` | the API, built into its connection string by the deploy |

Back up the secret files (the passport key above all) somewhere safe and apart from the server. See
[Disaster Recovery](Disaster-Recovery).
