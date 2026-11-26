# Environment Files

**English**

This page explains every `*.env` file of the deployment: what it is for, who reads it, which keys matter and which ones
are secrets. The files live in `ops/` and `ops/deploy/<service>/`, and the settings of the public edge in
`ops/infra/cloudflare/`.

## How the files work

- Each file is made by copying its `*.env.example` to the name without `.example`. The example is in git, the copy is
  not: `ops/.gitignore` ignores `*.env`, so a secret never reaches a commit.
- Values are plain text. Do not quote them.
- **Production and development are separate.** An `*.env.example` is the production template: it holds no development
  value (`ASPNETCORE_ENVIRONMENT=Production`, a placeholder site address in `CORS_ALLOWED_ORIGINS`, no development
  proxy route). The development values are in `<name>.env.dev` next to it (`nfhotel-api.env.dev`, `nfhotel-web.env.dev`,
  `market-analysis.env.dev`, `nginx.env.dev`). Only `scripts/dev-up.sh` reads them, and only when it makes a file; a
  production deploy never does. On the production host you write each `*.env` by hand from the example and set the
  values marked there, for example `API_TOKEN` and the site address of `market-analysis.env`.
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
| `ops/deploy/nfhotel-api/nfhotel-api.env` | database API | always | yes: four, and the app password of the mail relay once it is set up |
| `ops/deploy/nfhotel-web/nfhotel-web.env` | web application | always | yes: two copies, of the analysis key and the benchmark token |
| `ops/deploy/market-analysis/market-analysis.env` | market analysis | always | yes: API key |
| `ops/deploy/ollama/ollama.env` | Ollama (AI models) | always | no |
| `ops/deploy/benchmark/benchmark.env` | benchmark service | always | yes: API token |
| `ops/deploy/nginx/nginx.env` | nginx proxy | always | no |
| `ops/deploy/cloudflared/cloudflared.env` | Cloudflare Tunnel | public access only | yes: tunnel token |
| `ops/deploy/postgres-tailnet/postgres-tailnet.env` | database relay | team access only | no |
| `ops/deploy/alloy/alloy.env` | Alloy (telemetry) | monitoring only | no |
| `ops/deploy/monitoring/monitoring.env` | Grafana, Prometheus, Loki | monitoring host only | no |
| `ops/deploy/backup-target/backup-target.env` | backup target | backup host only | public key only |
| `ops/infra/cloudflare/edge.env` | `edge-tofu.sh` (the public edge in OpenTofu) | on the machine of S01 or S03 only | no token; the identifiers of the Cloudflare account and the addresses of the staff, kept out of git |

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
| `Encryption__PassportKey` | Key that encrypts passport numbers: 32 random bytes, base64 (`openssl rand -base64 32`). **Secret.** Losing it makes the stored numbers unreadable. Only the API holds it: never write it in the web file (ADR-0004) |
| `HotelTime__TimeZoneId` | Time zone of the hotel, `Europe/Copenhagen` |
| `Api__Token` | Token the web application and the benchmark service present to the API, at least 32 characters. **Secret.** The deploy passes it to the other services, so it is written here only |
| `Staff__Token__SigningKey` | Key that signs staff sign-in tokens, at least 32 characters. **Secret.** Only the API holds it |
| `Staff__InitialManager__*` | The first Manager, made at start when no staff account exists. The password has at least 12 characters and must be changed at the first sign-in. Delete the password line afterwards |
| `Database__MigrateOnStart` | Apply the pending migrations when the API starts. The deploy relies on it |
| `MarketAnalysisService__BaseUrl`, `__ApiKey`, `__TimeoutSeconds`, `__PollIntervalSeconds`, `__RunDeadlineSeconds` | Where the API starts market analysis runs (`market-analysis-api:8000`) and the key it sends as `X-API-Key` (**Secret**, never logged). The timeout (10 seconds, at most 60) is for one call, because the API follows a run by its id and keeps no request open; it asks for the state every `PollIntervalSeconds` (5) and gives up a run after `RunDeadlineSeconds` (3600). An empty address makes the analysis page say the service is not set up |
| `Smtp__Host`, `Smtp__Port`, `Smtp__Security` | The SMTP relay of the mails of the booking flow ([ADR-0053](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0053-mail-from-the-application-through-a-relay.md)): `smtp.gmail.com`, `465` and `SslOnConnect` (TLS from the first byte), or port 587 with `StartTls`. Any other value of `Smtp__Security`, `None` included, stops the API at its start. The forwarder `mail-relay` gets the host and the port from this file, and nothing else of it |
| `Smtp__UserName`, `Smtp__Password` | The account at the relay and its app password. **Secret:** the deploy writes the password into a secret file of the API (ADR-0024); write it as [Mail Relay](Mail-Relay#set-it-up-step-by-step) says, so it reaches no screen and no shell history |
| `Smtp__FromAddress`, `Smtp__FromName` | The address and the name that the mails come from; the name is `NF Hotel` by default. Gmail sends only from the address of the account, and a reply of a guest lands in its mailbox |
| `Smtp__Forwarder` | `mail-relay:1465`: the API reaches the relay through the forwarder, because it has no route out ([Network Isolation](Network-Isolation#the-way-of-the-mail)). When it is set, `deploy-services.sh` starts the forwarder before the API, and refuses the deploy when `Smtp__Host` or `Smtp__Port` is missing. Empty: no forwarder, and the API reaches the relay itself |
| `Smtp__CertificateThumbprint` | Empty in production, where the certificate of the relay is checked against the store of the system. The throwaway stacks set it to the SHA-256 of the certificate of their mail catcher (64 lower-case hex digits over its DER form), the one certificate they trust instead |
| `Smtp__MaxMailsPerDay` | The fuse: at most this many mails in 24 hours, 400 by default (Gmail allows an account 500 a day). At the fuse the public booking refuses new requests and `MailFuseBlown` fires ([Alerts](Alerts#mail-prometheusrulessecurityyml)) |
| `BookingVerification__Enabled` | `false` by default: the public booking books at once. `true`: a booking takes a room only once the guest has pressed the button of the link in the mail ([ADR-0052](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0052-a-booking-takes-a-room-once-its-email-is-verified.md)). With `true`, the API refuses to start, and names the setting, when a setting of the relay is missing, and `deploy-services.sh` refuses the deploy without `Smtp__Forwarder` |
| `BookingVerification__SiteAddress` | The address of the site in the link of the mail, an absolute `https` address: `https://nfhotel.devmatiker.com`. The link never takes the address of the request |
| `NFHOTEL_API_NETWORK`, `NFHOTEL_DB_NETWORK`, `NFHOTEL_PYTHON_NETWORK`, `NFHOTEL_MAIL_NETWORK` | Docker networks shared with the web service and nginx, with PostgreSQL, with the web service and the Python services (ADR-0039), and with the forwarder of the mail (ADR-0053) |

### `nfhotel-web/nfhotel-web.env`

The web application. It has no database login and no passport key: it calls the API, and only the API decrypts a
passport number (ADR-0004). An older file may still hold the line `Encryption__PassportKey`: delete it. Until then each
deploy leaves the line out of the web's settings with a warning that names the key, never the value, and deletes the
web's old secret file of the key (MIL-075).

| Key | Meaning |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Development` in the development VM |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | Trust the `X-Forwarded-*` headers set by nginx |
| `Tls__Require` | Redirect HTTP to HTTPS and mark the staff cookie Secure. True behind the proxy |
| `HotelTime__TimeZoneId` | Time zone of the hotel |
| `HolidayApi__BaseUrl`, `__TimeoutSeconds`, `__ApiKey` | The holiday calendar of the analysis service. The key must equal `API_TOKEN` of `market-analysis.env` |
| `BenchmarkService__BaseUrl`, `__TimeoutSeconds`, `__Token` | The benchmark service. The token is `NFBENCH_API_TOKEN` of `benchmark.env`. Empty means the Start page says the service is not set up |
| `NFHOTEL_PROXY_NETWORK`, `NFHOTEL_API_NETWORK`, `NFHOTEL_PYTHON_NETWORK` | Docker networks shared with nginx, with the API, and with the API and the Python services (ADR-0039) |

### `market-analysis/market-analysis.env`

The market analysis service, built from a pinned commit of `NF-Hotel/notebook-market-analysis`.

| Key | Meaning |
| --- | --- |
| `APP_ENV` | `production` requires `API_TOKEN` and explicit CORS origins |
| `API_TOKEN` | The key callers send in `X-API-Key`, at least 32 characters (`openssl rand -hex 32`). **Secret.** The web application sends the same value as `HolidayApi__ApiKey`; `dev-up.sh` writes both |
| `CORS_ALLOWED_ORIGINS` | Only satisfies the production rule: the web application calls from the server, not from a browser |
| `DATA_SOURCE` | `csv`: the service serves holidays for now and reads no market data |
| `AI_PROVIDER`, `OLLAMA_BASE_URL`, `OLLAMA_MODEL`, `AI_TIMEOUT_SECONDS` | The AI model of a run. The deploy writes `OLLAMA_MODEL` from the default AI model chosen in the back office. Empty means no AI narrative |
| `NFHOTEL_PYTHON_NETWORK`, `NFHOTEL_LLM_NETWORK` | Docker networks shared with the web application and the API, and with Ollama; nginx is on neither (ADR-0039) |
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
| `NFHOTEL_PYTHON_NETWORK`, `NFHOTEL_LLM_NETWORK` | Docker networks made by NFHotel: the Python services with the web application and the API (ADR-0039), and Ollama |
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

### `ops/infra/cloudflare/edge.env`

Only on the machine of S01 or S03, for `ops/scripts/edge-tofu.sh`, which plans and applies the routes of the tunnel and
the Access application (ADR-0046). It holds the values of `ops/infra/cloudflare/variables.tf`: `TF_VAR_account_id`,
`TF_VAR_tunnel_id`, `TF_VAR_access_application_id`, `TF_VAR_access_policy_id`, `TF_VAR_access_team_name`, and
`TF_VAR_team_emails`, the addresses of the policy `Team` as a JSON list. The token is never in it: `edge-tofu.sh` reads
`CLOUDFLARE_API_TOKEN`, or asks for it without echo. Docker reads the file, so a value has no quotes around it. The same
values are the secrets of the environment `cloudflare-read` of the weekly comparison: change both together, or the
comparison reports a difference after the next apply. See [Public Access](Public-Access#the-settings-outside-git);
`dev-up.sh` never makes this file.

## Secrets that must match

| Secret | Written in | Also used by |
| --- | --- | --- |
| `Api__Token` | `nfhotel-api.env` | web and benchmark, passed by the deploy |
| `API_TOKEN` | `market-analysis.env` | `HolidayApi__ApiKey` in `nfhotel-web.env`, copy by hand |
| `NFBENCH_API_TOKEN` | `benchmark.env` | `BenchmarkService__Token` in `nfhotel-web.env`, copy by hand |
| `POSTGRES_PASSWORD` | `postgres.env` | the API, built into its connection string by the deploy |

Back up the secret files (the passport key above all) somewhere safe and apart from the server. See
[Disaster Recovery](Disaster-Recovery).
