# Proxmox Deployment

The development environment is one virtual machine (VM) on Proxmox VE. Every service runs as a
Docker container inside that VM (decision: ADR-0003).

## What runs in the VM

| Container | Source | Reachable from |
| --- | --- | --- |
| `nginx` | `nginx:1.27-alpine`, `deploy/nginx` | Internet (port 443 over TLS; port 80 only redirects), serves only NFHotel |
| `nfhotel-web` | this repository, `deploy/nfhotel-web` | through nginx only; holds only an API address and a token |
| `nfhotel-api` | this repository, `Dockerfile.api`, `deploy/nfhotel-api` | `nfhotel-web` only, internal network |
| `postgres` | `postgres:16-alpine`, `deploy/postgres` | `nfhotel-api` only, internal network |
| `ollama` | `ollama/ollama`, `deploy/ollama` | benchmark and market analysis only, network `nfhotel-llm` |
| `market-analysis-api`, `market-analysis-notebook` | `NF-Hotel/Notebook_Market_Analysis`, `deploy/market-analysis` | VM loopback only |
| `benchmark` | `NF-Hotel/notebook-benchmark`, `deploy/benchmark` | VM loopback only |

### Network exposure

- Only nginx publishes ports to the outside: port 443 serves only the NFHotel web application over
  TLS 1.2 and 1.3, and port 80 answers only a redirect to HTTPS (ADR-0006). Any host name sent to
  these ports reaches NFHotel.
- The analysis and benchmark notebooks are served by a second nginx listener on `127.0.0.1:8081`
  inside the VM. Reach them with an SSH tunnel (below).
- PostgreSQL sits on the Docker network `nfhotel-db`, created with `--internal`: it has no route
  out and no published port. Only the database API `nfhotel-api` joins it (ADR-0004).
- The database API is the only process with the database login and the passport key. It sits on
  `nfhotel-db` and on a second internal network, `nfhotel-api`, that it shares with the web
  application. It publishes no port and nginx has no route to it. The web application proves
  itself with a bearer token; a call without it gets 401.
- Ollama sits on the network `nfhotel-llm`, joined only by the benchmark and the analysis service.
  It publishes no port and nginx does not route to it. The network has outbound access so Ollama
  can download models.
- The proxy network `nfhotel-proxy` joins nginx, the web application, the analysis service and
  the benchmark.

## TLS

nginx ends TLS (MIL-017, ADR-0006). The web application behind it stays on plain HTTP inside the
Docker network `nfhotel-proxy`.

- **Certificate.** nginx reads `deploy/nginx/tls/fullchain.pem` and `privkey.pem` (git-ignored,
  mounted read-only). `scripts/deploy-services.sh` runs `scripts/make-dev-certificate.sh` when the
  pair does not exist: a self-signed certificate for `localhost`, the server name and the addresses
  of the VM, valid 365 days. A browser warns about it; trust it once or use a real certificate.
- **A real certificate.** Put the certificate chain and key from a public authority in the same two
  files and restart nginx. The configuration does not change. Issuing and renewing is not automated
  yet (it needs a public host name and DNS); an expired certificate stops the service.
- **HSTS.** Set `NGINX_HSTS=max-age=31536000` in `nginx.env` only with a real certificate.
- **Ports.** `NGINX_HTTP_PORT` (default 80) redirects; `NGINX_HTTPS_PORT` (default 443) serves. If
  the HTTPS port is not 443, set `NGINX_REDIRECT_PORT=:<port>` so the redirect keeps it.
- **Web application.** `Tls__Require=true` in `nfhotel-web.env` makes it redirect any request that
  still arrives as HTTP and mark the staff cookie `Secure`.
- **Check it.** `curl -kI https://<vm>/` answers 200; `curl -I http://<vm>/` answers 301 to HTTPS;
  `curl -k --tls-max 1.1 https://<vm>/` fails.

## VM size

12 GiB memory (12288 MiB), 4 vCPU, 60 GiB disk, Debian 12 with Docker and the compose plugin,
built from the official cloud image by cloud-init. Change the size in `deploy.env`.

## Deploy everything

One command creates the VM if it is missing, then builds and starts every container in it.

1. Copy `deploy.env.example` to `deploy.env` and set `PVE_SSH` (`user@host` of the Proxmox node).
   The node and the VM need key login.
2. Copy each `deploy/<service>/*.env.example` to the matching `.env` file and fill it in:
   `postgres.env`, `ollama.env`, `nfhotel-api.env`, `nfhotel-web.env`, `market-analysis.env`, `benchmark.env`, `nginx.env`.
   For analysis also copy `market-analysis.toml.example` to `market-analysis.toml`.
3. Put the database user, password and name in `postgres.env` only. Use a password without `;`
   or quotes (`openssl rand -hex 24`). The API builds its connection string from that file, so the
   password is never repeated. Put the API token (`Api__Token`, at least 32 characters,
   `openssl rand -base64 32`) and the passport key in `nfhotel-api.env` only; the web service takes
   the token from there.
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
| `postgres`, `ollama`, `api`, `web`, `analysis`, `benchmark`, `nginx` | deploy only the named services |

The script is safe to run again. It finds the VM by name, waits for its address, ssh, cloud-init
and Docker, updates the repository in `/opt/nfhotel` inside the VM, copies the git-ignored
settings files over ssh, and runs `scripts/deploy-services.sh` there.

## Environment, database and API

The VM is the development environment, so `nfhotel-api.env` and `nfhotel-web.env` set
`ASPNETCORE_ENVIRONMENT=Development`.

- **Database.** Only `nfhotel-api` reads `ConnectionStrings__HotelDatabase`.
  `deploy/nfhotel-api/docker-compose.yml` builds it from the `POSTGRES_USER`, `POSTGRES_PASSWORD`
  and `POSTGRES_DB` values of `deploy/postgres/postgres.env` (`scripts/deploy-services.sh` passes
  them to compose) and points at the PostgreSQL container by name:

```text
Host=postgres;Port=5432;Database=<POSTGRES_DB>;Username=<POSTGRES_USER>;Password=<from postgres.env>
```

- **Web application.** It has `Api__BaseUrl=http://nfhotel-api:8080` and `Api__Token`, both set by
  `deploy/nfhotel-web/docker-compose.yml`. It has no database setting and no passport key, and its
  container is not on `nfhotel-db`. Its program holds no Npgsql or EF Core code.
- **Check it.** In the VM: `docker inspect` of the web container shows no `nfhotel-db` network,
  `docker exec <web> printenv` shows no `ConnectionStrings`, and a call to the API without the token
  answers 401.

- **Schema.** With `Database__MigrateOnStart=true` in `nfhotel-api.env` the API applies the pending
  Entity Framework migrations when it starts. Running it again changes nothing.

Settings files are git-ignored and never committed.

## Staff sign-in (back office)

The pages under `/admin` need a signed-in member of staff (ADR-0005). The public booking pages do not.

- **Secrets, in `nfhotel-api.env` only.** `Staff__Token__SigningKey` signs the staff tokens (at least
  32 characters, `openssl rand -base64 48`). `Staff__InitialManager__UserName` and
  `Staff__InitialManager__Password` (at least 12 characters, `openssl rand -base64 18`) make the
  first Manager when no staff account exists. The web application never holds them.
- **First sign-in.** Open `http://<vm>/admin`: it redirects to `/admin/login`. Sign in with the
  first Manager. A session lasts 8 hours.
- **More accounts.** Run the command in the API container, with the password in the environment so it
  never appears on a command line:

```bash
docker exec -e STAFF_PASSWORD='<at least 12 characters>' nfhotel-api-nfhotel-api-1   dotnet NFHotel.Api.dll create-staff <user name> <Receptionist|Manager|Housekeeping>
```

- **Roles.** Receptionist: bookings, guests, rooms to read. Manager: everything, including rooms,
  cleaning status and revenue. Housekeeping: rooms and their cleaning status only.
- **Wrong passwords.** Five failures of a user name in fifteen minutes stop that name until the
  window has passed (HTTP 429). Restarting the API clears the count.
- **Limits.** A token cannot be revoked before it expires. Over TLS (above) the password and the
  cookie are private on the wire.

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
- Making ports 80 and 443 of the VM reachable from the internet (router, firewall, DNS) is outside
  these scripts.
- The deploy runs no migration itself; the API applies them at start when
  `Database__MigrateOnStart=true` (see Staff sign-in).
