# Proxmox Deployment

To try this deployment on your own machine, see [Local Development Deploy](https://github.com/NF-Hotel/NFHotel/wiki/Local-Development-Deploy) in NFHotel.

The development environment is one virtual machine (VM) on Proxmox VE. Every service runs as a
Docker container inside that VM (decision: ADR-0003).

## What runs in the VM

| Container | Source | Reachable from |
| --- | --- | --- |
| `nginx` | `nginx:1.27-alpine`, `ops/deploy/nginx` | Internet (port 443 over TLS; port 80 only redirects), serves only NFHotel |
| `nfhotel-web` | this repository, `ops/deploy/nfhotel-web` | through nginx only; holds only an API address and a token |
| `nfhotel-api` | this repository, `Dockerfile.api`, `ops/deploy/nfhotel-api` | `nfhotel-web` only, internal network |
| `postgres` | `postgres:16-alpine`, `ops/deploy/postgres` | `nfhotel-api` only, internal network |
| `ollama` | `ollama/ollama`, `ops/deploy/ollama` | benchmark and market analysis only, network `nfhotel-llm` |
| `market-analysis-api`, `market-analysis-notebook` | `NF-Hotel/Notebook_Market_Analysis`, `ops/deploy/market-analysis` | VM loopback only |
| `benchmark` | `NF-Hotel/notebook-benchmark` (the API service), `ops/deploy/benchmark` | internal networks only, no published port |

### Network exposure

- Only nginx publishes ports to the outside: port 443 serves only the NFHotel web application over
  TLS 1.2 and 1.3, and port 80 answers only a redirect to HTTPS (ADR-0006). Any host name sent to
  these ports reaches NFHotel.
- The analysis notebook and API are served by a second nginx listener on `127.0.0.1:8081`
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

- **Certificate.** nginx reads `ops/deploy/nginx/tls/fullchain.pem` and `privkey.pem` (git-ignored,
  mounted read-only). `ops/scripts/deploy-services.sh` runs `ops/scripts/make-dev-certificate.sh` when the
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

The production VM `nf-hotel` (Proxmox VMID 100) has 4 vCPU, 20 GiB memory, a 111 GB disk and Debian 13. These values were read on the VM on 2026-10-21 (ADR-0010 decision 6). A new VM made by these scripts gets the default below.

12 GiB memory (12288 MiB), 4 vCPU, 60 GiB disk, Debian 12 with Docker and the compose plugin,
built from the official cloud image by cloud-init. Change the size in `ops/deploy.env`.

The same node also runs the VM `sports-club` (101) and, for the monitoring of NF Hotel (ADR-0020), the unprivileged LXC `nf-monitoring` (102): 2 cores, 4 GiB memory, a 32 GiB disk on `local-lvm`, on `vmbr0`, started at boot. Its setup is in [Monitoring](Monitoring).

## Deploy everything

One command creates the VM if it is missing, then builds and starts every container in it.

1. Copy `ops/deploy.env.example` to `ops/deploy.env` and set `PVE_SSH` (`user@host` of the Proxmox node).
   The node and the VM need key login.
2. Copy each `ops/deploy/<service>/*.env.example` to the matching `.env` file and fill it in:
   `postgres.env`, `ollama.env`, `nfhotel-api.env`, `nfhotel-web.env`, `market-analysis.env`, `benchmark.env`, `nginx.env`.
   For analysis also copy `market-analysis.toml.example` to `market-analysis.toml`.
3. Put the database user, password and name in `postgres.env` only. Use a password without `;`
   or quotes (`openssl rand -hex 24`). The API builds its connection string from that file, so the
   password is never repeated. Put the API token (`Api__Token`, at least 32 characters,
   `openssl rand -base64 32`) and the passport key in `nfhotel-api.env` only; the web service takes
   the token from there.
4. Run:

```bash
bash ops/scripts/deploy.sh
```

| Option | Effect |
| --- | --- |
| `--dry-run` | print the Proxmox commands that create the VM and stop |
| `--local` | ship this working tree and an NFHotel working tree instead of cloning them (unpushed work) |
| `--skip-vm` | deploy to the existing VM, do not create or start it |
| `--no-build` | reuse existing images |
| `--down` | stop and remove the containers; data volumes are kept |
| `postgres`, `ollama`, `api`, `web`, `analysis`, `benchmark`, `nginx` | deploy only the named services |

The script is safe to run again. It finds the VM by name, waits for its address, ssh, cloud-init
and Docker, updates this repository in `/opt/nfhotel` inside the VM (`REPO_URL` and
`REPO_REF` in `ops/deploy.env`), copies the git-ignored settings files over ssh, and runs
`ops/scripts/deploy-services.sh` there, which builds web and api from `/opt/nfhotel`. The
production VM is deployed by the CD from released images instead (CI-CD).

## Environment, database and API

The VM is the development environment, so `nfhotel-api.env` and `nfhotel-web.env` set
`ASPNETCORE_ENVIRONMENT=Development`.

- **Database.** Only `nfhotel-api` reads `ConnectionStrings__HotelDatabase`.
  `ops/scripts/write-secret-files.sh` builds it from the `POSTGRES_USER`, `POSTGRES_PASSWORD`
  and `POSTGRES_DB` values of `ops/deploy/postgres/postgres.env` (or takes the login of `nfhotel_app`,
  ADR-0011) and points at the PostgreSQL container by name:

```text
Host=postgres;Port=5432;Database=<POSTGRES_DB>;Username=<POSTGRES_USER>;Password=<from postgres.env>
```

- **Web application.** It has `Api__BaseUrl=http://nfhotel-api:8080` from
  `ops/deploy/nfhotel-web/docker-compose.yml`, and `Api__Token` as a secret file. It has no database
  setting, and its container is not on `nfhotel-db`. Its program holds no Npgsql or EF Core code.
- **Check it.** In the VM: `docker inspect` of the web container shows no `nfhotel-db` network,
  `sudo bash /opt/nfhotel/ops/scripts/check-secret-env.sh` shows no secret in any environment, and a
  call to the API without the token answers 401.

- **Schema.** With `Database__MigrateOnStart=true` in `nfhotel-api.env` the API applies the pending
  Entity Framework migrations when it starts. Running it again changes nothing.

Settings files are git-ignored and never committed.

## Secrets and their rotation

**Decision:** [ADR-0024](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0024-secrets-as-files.md). **Work:** MIL-035.

No secret reaches a container as an environment variable. You write each secret once, in the settings file of its service, as before. At every deploy, `ops/scripts/write-secret-files.sh` splits the file into `secrets/settings.env` (everything except the secrets) and one file per secret in `ops/deploy/<service>/secrets/`, and compose mounts each file at `/run/secrets/<name>`.

*Why files:* an environment variable is shown by `docker inspect`, by `/proc/1/environ` and by the Docker socket proxy of the monitoring; a mounted file is not. *Why written by the deploy:* you keep one place to edit, and a changed value reaches the container at the next deploy.

| Secret | Written in | Read by |
| --- | --- | --- |
| `POSTGRES_PASSWORD` | `postgres/postgres.env` | PostgreSQL (first start only), the API's connection string |
| `Api__Token` | `nfhotel-api/nfhotel-api.env` | the API and the web application (and the benchmark service, from its environment) |
| `Staff__Token__SigningKey` | `nfhotel-api/nfhotel-api.env` | the API |
| `Staff__InitialManager__Password` | `nfhotel-api/nfhotel-api.env` | the API, only while no staff account exists |
| `Encryption__PassportKey` | `nfhotel-api.env` and `nfhotel-web.env` | the API and the web application |
| `MarketAnalysisService__Token`, `HolidayApi__ApiKey`, `BenchmarkService__Token` | their service's settings | the API or the web application |

The login of `nfhotel_app` (ADR-0011) stays in root's `/etc/nfhotel/secrets/nfhotel-api-database.env`; the deploy passes it to the script, which writes it as the connection string.

The personal database logins of the team (ADR-0034) are in root's `/etc/nfhotel/secrets/team/`, one file per person; see [Database Access](Database-Access).

### Rotate a secret

The same steps for every secret below, except where a step says otherwise. CI runs them on a throwaway stack (`ops/tests/test-secret-rotation.sh`), so they are known to work.

1. **Make a new value** in a shell on `nf-hotel`: `openssl rand -hex 24` for a password or a token, `openssl rand -hex 32` for the signing key. *Why hex:* it has no `;` or quote, which would break the connection string.
2. **Write it** in the settings file of its service, replacing the old line: `sudo -u nfhotel nano /opt/nfhotel/ops/deploy/<service>/<service>.env`.
3. **Deploy:** re-run the latest successful `NF Hotel CD` run in GitHub Actions. The deploy writes the files again, and compose recreates the container because the hash of its secret files changed. *Why not `deploy-services.sh` by hand:* only the CD passes the login of `nfhotel_app`, so a deploy by hand would log the API in as the superuser.
4. **Check** that the site and the back office sign-in work, and that `sudo bash /opt/nfhotel/ops/scripts/check-secret-env.sh` passes.

What is special per secret:

- **The API token.** The CD deploys the API, the web application and the benchmark service together, so all three get the new value; for the seconds in between, the web application gets 401 from the API. *Why it is safe:* the old token stops working the moment the API restarts, which is what a rotation is for.
- **The signing key.** Every member of staff is signed out and signs in again. *Why:* the old sessions were signed with the old key, which is exactly what makes a stolen session useless.
- **The database password.** Change the role first, then the settings, or the API cannot log in in between:
  ```bash
  sudo docker exec -i nfhotel-postgres-postgres-1 psql -U nfhotel -d nfhotel
  ```
  and type `ALTER ROLE nfhotel PASSWORD '<new value>';` at the prompt (*why at the prompt:* a password on the command line ends up in the shell history and in the process list). Then write it in `postgres.env` and re-run CD. PostgreSQL itself is not recreated: it reads the file only to create the database the first time. For the roles of ADR-0011, change the role with the same `ALTER ROLE`, then the matching file in `/etc/nfhotel/secrets`, and re-run CD.
- **The first Manager's password** is read only while no staff account exists. Once the first Manager has signed in and chosen a new password (ADR-0017), delete the line from `nfhotel-api.env` and re-run CD.
- **The passport key cannot be rotated yet.** It encrypts the passport numbers already stored, and the application has one key, so a new key would make them unreadable. If it leaks, treat it as an incident ([Incidents](Incidents#when-personal-data-may-have-leaked)); re-encryption is planned as later work (ADR-0024 decision 7).

## Staff sign-in (back office)

The pages under `/admin` need a signed-in member of staff (ADR-0005). The public booking pages do not.

- **Secrets, in `nfhotel-api.env` only.** `Staff__Token__SigningKey` signs the staff tokens (at least
  32 characters, `openssl rand -base64 48`). `Staff__InitialManager__UserName` and
  `Staff__InitialManager__Password` (at least 12 characters, `openssl rand -base64 18`) make the
  first Manager when no staff account exists. The web application never holds them.
- **First sign-in.** Open `http://<vm>/admin`: it redirects to `/admin/login`. Sign in with the
  first Manager. A session lasts 8 hours. The first Manager must choose a new password before
  anything else (`Staff__InitialManager__MustChangePassword`, true by default).
- **More accounts.** Use the Staff page of the back office (see "Staff accounts and passwords" below), or run the command in the API container, with the password in the environment so it
  never appears on a command line:

```bash
docker exec -e STAFF_PASSWORD='<at least 12 characters>' nfhotel-api-nfhotel-api-1   dotnet NFHotel.Api.dll create-staff <user name> <Receptionist|Manager|Housekeeping>
```

- **Roles.** Receptionist: bookings, guests, rooms to read. Manager: everything, including rooms,
  cleaning status and revenue. Housekeeping: rooms and their cleaning status only.
- **Wrong passwords.** Five failures of a user name in fifteen minutes stop that name until the
  window has passed (HTTP 429). Restarting the API clears the count.

### Staff accounts and passwords (MIL-030, ADR-0017)

Everything below is done on the pages of the back office, not with commands on the server.

- **Change your own password.** "Change password" in the side menu, for every role. It needs the current
  password; the new one has at least 12 characters, is not your user name or the old password, and is not a
  common password. Your other sessions end at once.
- **Manage accounts (Manager).** "Staff" lists the accounts. A Manager creates an account with a temporary
  password, disables an account (its sessions end at once), enables it again, and sets a new temporary
  password. The person must choose their own password at the next sign-in. A Manager cannot disable their own
  account, and the last active Manager cannot be disabled.
- **Audit trail (Manager).** "Audit" shows sign-ins, failed sign-ins, lockouts and every change to an account,
  with who did it and from which address. It never holds a password.
- **The password in the environment file.** `Staff__InitialManager__Password` only seeds an empty database.
  After the first Manager has chosen a new password, delete that line from `nfhotel-api.env` and restart the
  API; changing the line has no effect on an existing account.
- **A lost Manager password (break-glass).** If no Manager can sign in, set a temporary password on the server;
  the account must change it at the next sign-in and its old sessions end:

```bash
docker exec -e STAFF_PASSWORD='<at least 12 characters>' nfhotel-api-nfhotel-api-1   dotnet NFHotel.Api.dll reset-staff-password <user name>
```
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
The market analysis configuration (`market-analysis.toml`) points there. The benchmark service reads
`NFBENCH_OLLAMA_URL` from `ops/deploy/benchmark/benchmark.env`. A GPU
passed through to the VM is described in the comments of `ops/deploy/ollama/docker-compose.yml`.

## Reach the internal services

```bash
ssh -L 8081:127.0.0.1:8081 nfhotel@<vm address>
```

Then open `http://analysis.localhost:8081` (market analysis notebook)
or `http://analysis-api.localhost:8081` (analysis API).

## Known limits

- Ollama runs on the CPU, so answers are slow. LM Studio is not part of the VM; the benchmark
  shows it as unavailable.
- Making ports 80 and 443 of the VM reachable from the internet (router, firewall, DNS) is outside
  these scripts.
- The deploy runs no migration itself; the API applies them at start when
  `Database__MigrateOnStart=true` (see Staff sign-in).

## The benchmark service (MIL-019, MIL-020)

The benchmark service is a separate repository, `notebook-benchmark`, with its own compose file and env files. It
joins the internal network `nfhotel-api` and publishes no host port.

- The web application starts runs at `http://benchmark:8000` with the bearer token in
  `BenchmarkService__Token`, which the deploy takes from the `NFBENCH_API_TOKEN` of the benchmark service's
  `benchmark.env` and mounts as a secret file (ADR-0024).
- The benchmark service delivers the finished result to `http://nfhotel-api:8080/api/benchmark/results` with the
  `Api__Token` of `ops/deploy/nfhotel-api/nfhotel-api.env`. That value is its `NFBENCH_WEB_TOKEN`.
- A Manager starts a run at `/admin/benchmark/start` and reads the stored runs at `/admin/benchmark`.
- Every run a Manager starts is recorded in the API (`/api/benchmark/requests`, ADR-0031), with its choice, who started it
  and its last state. The benchmark service forgets its runs when it restarts, for example at a deploy: a run that it no
  longer knows is then shown as interrupted, on `/admin/benchmark` under "Started runs without a stored result" and on
  the start page, with "Start again" or "Use the same choice". Reopening the start page follows a run that is still going.
- Opening a run on `/admin/benchmark` shows it like the notebook of the benchmark repository: the mean per model and
  phase (cold is the first request of a model, warm the later ones) with three bar charts, the pass and fail of the
  deterministic tasks, up to three answers side by side, the memory charts, and a CSV and JSON download of the rows
  without the answer texts. The figures that the benchmark service calculates from the dataset are not delivered yet, so
  the Calculated column shows n/a.
- A Manager adds a model (for example `phi4-mini`) and deletes an installed one in the Installed models panel of
  `/admin/benchmark/start`. The web application asks the benchmark service, which pulls into and deletes from the
  Ollama of the VM (ADR-0005 of notebook-benchmark): one download at a time, with progress; a model that a queued or
  running benchmark uses cannot be deleted, and a default AI model is warned about before it is deleted. The service
  does not check free disk; Ollama fails a download that does not fit and the reason is shown.
- A Manager sets the default AI model of the website and of the market analysis on `/admin/benchmark` (ADR-0014): type
  or choose a model, or press "Use for the website" or "Use for the market analysis" on a result row. `GET /api/ai-defaults`
  gives the defaults to a caller with the service token. `ops/scripts/deploy-services.sh` writes the model of the market
  analysis as `model` in the `[llm]` section of `ops/deploy/market-analysis/market-analysis.toml` before it starts the analysis
  service, so a new default applies at the next deployment of that service.
- Until `BenchmarkService__Token` is set, the start page says that the service is not set up.

## The market analysis report (MIL-026)

The report is made by the markets API of `Notebook_Market_Analysis` (`ops/deploy/market-analysis/`, MIL-004). The NFHotel API,
not the web application, calls it.

- A Manager starts a run at `/admin/market-analysis`. The API answers at once and runs the analysis in the background: it
  reads every booking of the database (with the guest country and the market details of the booking), sends them as a
  JSON array to `http://market-analysis-api:8000/analyze?insights=true`, and stores the result in the table
  `market_analysis_run`. One run at a time; the page asks the API for the state every few seconds. A run with AI insights
  takes up to 6 x `generation_timeout_seconds` of `market-analysis.toml`. A run is started at night on a machine without a
  GPU, so the example allows 4 hours for one request (14400) and `MarketAnalysisService__TimeoutSeconds` in
  `ops/deploy/nfhotel-api/nfhotel-api.env` is 90000 (25 hours); 240 and 1500 are enough for a short run. The time depends on
  the model and not on the number of bookings, because the model only sees the findings.
- The state of a run is kept in the memory of the API. A restart forgets it, the page then says that the API no longer
  knows the run, and the stored report is not lost.
- The page shows the last stored report: key figures, and for each analysis its charts and the AI executive summary and
  suggestions, or the reason there is none (for example `GUARDRAIL_REJECTED`). Without a stored run it says that no
  analysis has been run.
- The markets API joins the network `nfhotel-api` (`ops/deploy/market-analysis/docker-compose.yml`) so that the API reaches it;
  it publishes no host port and has no authentication, so keep that network internal.
- Receptionist and Housekeeping users get 403 on `api/market-analysis` and do not see the menu entry.
- The AI model of the analysis is the one in `ops/deploy/market-analysis/market-analysis.toml`, which `ops/scripts/deploy-services.sh`
  sets from the default AI model of the market analysis (ADR-0014).

### The fake bookings

The database has no real bookings yet, so the report is made from fake ones. The bookings have extra fields that the
analysis needs (table `booking_market_detail`: booking date, price per night, adults, children, babies, meal, market
segment, customer type, deposit type, room type, repeated guest, previous cancellations, booking changes, special
requests, parking spaces). A booking without these fields is analysed with them missing.

`nf_hotel_bookings.csv` (40,060 bookings from 2018 to 2020, in `src/NFHotel.DataAnalysis/data/` of an NFHotel checkout) is loaded with the
command `seed-market-data` of the API:

```bash
docker cp src/NFHotel.DataAnalysis/data/nf_hotel_bookings.csv nfhotel-api-nfhotel-api-1:/tmp/nf_hotel_bookings.csv
docker exec nfhotel-api-nfhotel-api-1 dotnet NFHotel.Api.dll seed-market-data /tmp/nf_hotel_bookings.csv --through 2026-12-31
```

It makes 187 rooms (the most bookings that are at the hotel on one night), about 500 guests with the countries of the file,
and the bookings placed in rooms so that the overlap rule of the database holds. The file has no price, so the price per
night is made from the room type, the month and the market segment: it is not real. The command refuses a database that has
rooms, guests or bookings; add `--replace` to delete them first. It loads the same data every time.

The file covers 26 months (2018-07 to 2020-08). With `--through 2026-12-31` the file is repeated back to back, 26 months
at a time, until that arrival date, and the bookings after it are left out: about 157,000 bookings in 294 rooms.
The status follows today's date: a booking that has left is checked out, one that is at the hotel is checked in, one that
arrives later is confirmed, and a cancelled one stays cancelled. The repeated bookings are not a forecast.

The market analysis page takes a first and a last arrival date; with no date it analyses every booking.
