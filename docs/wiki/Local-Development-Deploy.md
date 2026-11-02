# Local Development Deploy

This page explains how to run the whole NF Hotel deployment on your own machine, so you can see a change working. It is
the deployment of [ADR-0003](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0003-proxmox-vm-for-services.md)
in miniature: Proxmox VE runs in a Docker container, a virtual machine (VM) in it runs every service as a Docker
container, and one script does all the steps. The script is for development only. For the real server see
[Proxmox Deployment](Proxmox-Deployment).

```text
your machine ── Docker ── Proxmox VE container ── VM (Debian + Docker) ── postgres, ollama, api, web, analysis, benchmark, nginx
      │                                                                                                       │
      └──────────────────────── https://localhost:8443/  ◄────────────────────────────────────────────────────┘
```

**On a Mac, or without KVM?** Use the [light mode](#the-light-mode-macos-or-no-kvm): `bash scripts/dev-up.sh --lite`. It
runs the database, the API, the web application and nginx straight in Docker, at the same addresses, in about 3 GiB.

**Debugging in Visual Studio?** [Run from Visual Studio](#run-from-visual-studio-f5): prepare once with
`bash scripts/dev-up.sh --f5`, then press F5 on the launch profile "API and web".

## What you need

| Need | Why | Windows 11 | Linux |
| --- | --- | --- | --- |
| Docker | runs the Proxmox VE container | Docker Desktop with the WSL 2 backend | Docker Engine with the compose plugin |
| `/dev/kvm` inside Docker | Proxmox VE starts the VM with KVM | Virtualization on in the BIOS, and a WSL distribution that loads the KVM modules, for example `wsl -d Debian`, started before the script | the `kvm` module for your CPU, loaded |
| bash 4 or later, `curl`, `ssh`, `tar`, `git` | the script and its tools | Git for Windows (Git Bash) | the packages of your distribution |
| Memory | the VM has 12 GiB by default, and Docker needs about 2 GiB more | give Docker 16 GiB or more: `memory=16GB` in `%UserProfile%\.wslconfig`, then `wsl --shutdown` | 16 GiB or more of RAM |
| Disk | the images, the VM disk and the models | about 40 GiB free | about 40 GiB free |
| Internet | the Proxmox image, the Debian image, Docker images, the models | yes | yes |
| Free ports | the site and the Proxmox web page | 8443 and 8006 | 8443 and 8006 |

You do not need a Proxmox server, an account or a password to start. The GitHub clone of the repository is enough.

Not sure? Run the check. It tests every line of the table, tells you how to fix each problem, and starts nothing:

```bash
bash scripts/dev-up.sh --check
```

On Windows from PowerShell, `.\scripts\dev-up.ps1 --check` finds Git Bash for you and runs the same script.

## Run it

```bash
bash scripts/dev-up.sh
```

The first run takes ten minutes or more, because it downloads the images and installs Docker in the VM. The script
tells you each step it takes. When it is ready it prints the addresses and the development accounts. Later runs ship
your working tree to the VM and rebuild only what you name, in about 30 seconds.

| Command | What it does |
| --- | --- |
| `dev-up.sh` | start everything, then rebuild and restart `api` and `web` from your working tree |
| `dev-up.sh web` | rebuild only the web application |
| `dev-up.sh api` | rebuild only the database API |
| `dev-up.sh benchmark` | rebuild the benchmark service from the commit named in `ops/deploy/benchmark/benchmark.env` |
| `dev-up.sh --all` | rebuild `api` and `web` and start whatever is not running |
| `dev-up.sh --seed` | load the sample rooms, guests and bookings of `db/seed_dev.sql` |
| `dev-up.sh --open` | open the site in your browser when it is ready |
| `dev-up.sh --check` | only check the machine |
| `dev-up.sh --down` | stop the services; the database, the VM and Proxmox stay |
| `dev-up.sh --destroy` | remove the Proxmox container with the VM and everything in it, including the database |

Settings through the environment: `DEV_SITE_PORT` (default 8443), `DEV_PVE_PORT` (default 8006) and `DEV_MODELS` (the
Ollama models to pull, default `llama3.2:3b phi3.5`; an empty value pulls none) and `DEV_GPU_OLLAMA` (`off` keeps the
Ollama of the VM even when your machine has an NVIDIA GPU).

## The light mode (macOS, or no KVM)

The Proxmox mode needs `/dev/kvm` inside Docker and about 14 GiB of memory, and it starts an x86 VM. Docker Desktop for
Mac has no KVM, and an Apple Silicon Mac cannot start the VM, so on a Mac the check says
`PROBLEM /dev/kvm is not available to Docker`. The light mode ([ADR-0035](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0035-light-local-development-stack.md))
leaves Proxmox VE and the VM out and runs the containers of the deployed shape straight in your Docker:

```text
your machine ── Docker ── postgres, api, web, nginx
      │                                    │
      └── https://localhost:8443/ ◄────────┘
```

| Need | macOS | Windows 11 | Linux |
| --- | --- | --- | --- |
| Docker with compose 2.24.4 or later | Docker Desktop (Intel or Apple Silicon) | Docker Desktop | Docker Engine with the compose plugin |
| bash 4 or later | `brew install bash`, then open a new terminal (macOS has bash 3.2) | Git for Windows (Git Bash) | the packages of your distribution |
| `openssl`, `curl`, `git` | in macOS (`xcode-select --install` for git) | Git for Windows | the packages of your distribution |
| Memory for Docker | about 3 GiB | about 3 GiB | about 3 GiB |
| Free port | 8443 | 8443 | 8443 |

No `/dev/kvm`, no privileged container. Run:

```bash
bash scripts/dev-up.sh --lite --check   # does this machine meet the needs? starts nothing
bash scripts/dev-up.sh --lite --seed    # build, start and load the sample rooms, guests and bookings
```

The first run builds the API and the web application, a few minutes; later runs rebuild them from your working tree. At
the end the script prints the three development accounts (`manager`, `receptionist`, `housekeeping`).

| Command | What it does |
| --- | --- |
| `dev-up.sh --lite` | build the API and the web application from your working tree and start the four services |
| `dev-up.sh --lite --seed` | the same, then load `db/seed_dev.sql` (once) |
| `dev-up.sh --lite --open` | open the site in your browser when it is ready |
| `dev-up.sh --lite --down` | stop the services; the database and the settings stay |
| `dev-up.sh --lite --destroy` | remove the services, the database and `ops/.dev-lite/` |

| What | Where |
| --- | --- |
| The back office | `https://localhost:8443/admin/login` (accept the development certificate) |
| The API of the mobile app | `https://localhost:8443/mobile-api/`; from the Android emulator `https://10.0.2.2:8443/mobile-api/`, from the iOS simulator `https://localhost:8443/mobile-api/`. The debug build of the mobile app uses this address by default |
| Settings, secrets and the passwords of the accounts | `ops/.dev-lite/` (git-ignored, readable by you only); the passwords are in `ops/.dev-lite/dev-staff.env` |
| The database | the volume of the container `nfhotel-dev-postgres-postgres-1` |

Not in the light mode: Ollama, the market analysis and the benchmark service. Their pages say the service is not
reachable. Use the Proxmox mode for them. Run only one mode at a time: both use port 8443 (`dev-up.sh --down` stops the
Proxmox mode, `dev-up.sh --lite --down` the light mode), or set `DEV_SITE_PORT`.

## Run from Visual Studio (F5)

The two modes above run the application as containers, so a breakpoint in Visual Studio is never hit. To debug, the API
and the web application run as .NET processes from Visual Studio (or Rider, or `dotnet run`), and only PostgreSQL runs
in Docker ([ADR-0036](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0036-start-from-visual-studio.md)):

```text
Visual Studio ── NFHotel.Web  https://localhost:7060/ ──► NFHotel.Api  http://localhost:59103/ ──► Docker: postgres 127.0.0.1:55432
```

| Need | macOS | Windows 11 | Linux |
| --- | --- | --- | --- |
| The .NET SDK of `global.json` | the .NET SDK, or Rider | Visual Studio 2026, or the .NET SDK | the .NET SDK |
| Docker | Docker Desktop | Docker Desktop | Docker Engine |
| bash 4 or later, `openssl` | `brew install bash` | Git for Windows (Git Bash) | the packages of your distribution |
| A development certificate | `dotnet dev-certs https --trust` | `dotnet dev-certs https --trust` (Visual Studio offers it at the first F5) | `dotnet dev-certs https` |
| Free ports | 55432, 59102, 59103, 7060 | the same | the same |

Prepare the machine once:

```bash
bash scripts/dev-up.sh --f5 --check   # does this machine meet the needs? starts nothing
bash scripts/dev-up.sh --f5           # start PostgreSQL and write the user secrets
```

From PowerShell: `.\scripts\dev-f5.ps1 --check` and `.\scripts\dev-f5.ps1`. The script starts PostgreSQL 16 on
`127.0.0.1:55432` and writes the .NET user secrets of `NFHotel.Api` and `NFHotel.Web`:

- the connection string, the API token, the signing key and the passport key;
- the first Manager;
- the address of the API.

At the end it prints the account `manager` and its password. A second run changes nothing.

Then open `NFHotel.sln`, choose the launch profile **API and web** in the toolbar, and press F5. Visual Studio starts
both projects with the debugger; sign in at `https://localhost:7060/admin/login`. The API runs the migrations at its
start. Without Visual Studio, start both projects in two terminals:

```bash
dotnet run --project src/NFHotel.Api --launch-profile NFHotel.Api
dotnet run --project src/NFHotel.Web --launch-profile https
```

| Command | What it does |
| --- | --- |
| `dev-up.sh --f5` (or `dev-f5.ps1`) | start PostgreSQL and write the user secrets that are missing |
| `dev-up.sh --f5 --seed` | the same, then load the sample rooms, guests and bookings of `db/seed_dev.sql` (it replaces those tables; start the API once with F5 first, so the tables exist) |
| `dev-up.sh --f5 --down` | stop PostgreSQL; the database and the secrets stay |
| `dev-up.sh --f5 --destroy` | remove PostgreSQL, its volume, `ops/.dev-f5/` and the user secrets the script wrote (yours stay) |

| What | Where |
| --- | --- |
| The back office | `https://localhost:7060/admin/login` |
| The API | `http://localhost:59103/` and `https://localhost:59102/`; `GET /health` answers 200 |
| The API for the mobile app | `--dart-define=API_BASE_URL=http://10.0.2.2:59103` from the Android emulator, `http://localhost:59103` from the iOS simulator. The API serves `/api/auth/login` and `/api/rooms` itself, with no `/mobile-api` prefix and no nginx in front. Not yet tested with the app: the light mode stays the reference address |
| The settings and secrets | .NET user secrets on your machine (Visual Studio: right-click the project, **Manage User Secrets**); the database password and the Manager password also in `ops/.dev-f5/` (git-ignored, readable by you only) |
| The database | the container `nfhotel-f5-postgres` and its volume `nfhotel-f5-pgdata` |

Not under F5: nginx and its TLS, Ollama, the market analysis and the benchmark service. The F5 mode can run next to
the light mode or the Proxmox mode: their ports differ.

## Use the GPU of your machine

The VM cannot take your GPU: Proxmox VE runs in a container here, and the VM is nested in it (on Windows the GPU is shared
with containers as a paravirtual device, which a VM cannot use). To run the AI models on an NVIDIA GPU, run Ollama on your
own machine. `dev-up.sh` does the rest when it finds an NVIDIA GPU (`nvidia-smi`) and an Ollama that answers on
`http://localhost:11434`:

1. It checks that the VM reaches your Ollama through the address of `host.docker.internal`.
2. It pulls the `DEV_MODELS` into your Ollama.
3. It writes that address as `OLLAMA_BASE_URL` in `market-analysis.env` and as `NFBENCH_OLLAMA_URL` in `benchmark.env`, and
   redeploys the two services.

Install Ollama from ollama.com and start it (on Windows it starts with the tray program). Set `DEV_GPU_OLLAMA=off` to keep
the Ollama of the VM, which runs on the CPU; the next run writes `http://ollama:11434` back, but only over a value that
the script wrote itself. The models are then in two places, your Ollama and the VM, and the VM's copy is the one the
default-model check reads. For a real Proxmox VE host, where a GPU can be passed on to the VM, see
[GPU Passthrough](GPU-Passthrough).

## What it does, step by step

1. Checks the machine (the same check as `--check`).
2. Makes any missing settings file from its `.example` with new random secrets: `ops/pve.env`, `ops/deploy.env` and the
   `ops/deploy/*/*.env` files of postgres, the API, the web application and nginx, plus `ollama.env` and `benchmark.env`. A
   file that exists is never changed. All of them are git-ignored.
3. Starts the Proxmox VE container once (later runs only start it again) and sets its root password from `ops/pve.env`.
4. Runs `ops/scripts/deploy.sh --local`, which creates the VM when it is missing, copies your working tree into it and
   deploys the services in the right order with `ops/scripts/deploy-services.sh`: the web application waits for a healthy
   database and API, and nginx waits for the web application and the market analysis API.
5. Pulls the small Ollama models, creates the accounts and publishes the site on `https://localhost:8443/`.

## Sign in

Open `https://localhost:8443/admin/login`. The browser warns about the certificate, because the script makes a
development certificate; accept it. The script prints one account per role (Manager, Receptionist and Housekeeping) at
the end of each run, **only** when `ASPNETCORE_ENVIRONMENT` is `Development` in `ops/deploy/nfhotel-api/nfhotel-api.env`,
which is what the script writes. A release deployment never prints them. The passwords are in plain text in two
git-ignored files:

- the Manager: `Staff__InitialManager__Password` in `ops/deploy/nfhotel-api/nfhotel-api.env`;
- the Receptionist and the Housekeeping user: `ops/deploy/nfhotel-api/dev-staff.env`.

If you change a password in the application, these files no longer match it.

## Where things are

| What | Where |
| --- | --- |
| The site | `https://localhost:8443/` (an address that starts with `http://` is sent on to `https://`) |
| Proxmox VE | `https://localhost:8006/`, user `root`, the password is `PVE_PASSWORD` in `ops/pve.env` |
| Database, benchmark results, Ollama models | volumes inside the VM, which lives inside the `proxmox-node1` container (the name is `PVE_CONTAINER` in `ops/deploy.env`) |
| The code the VM runs | `/opt/nfhotel` in the VM, a copy of your working tree made at each run |

Everything that is stored lives in the VM. `--destroy` removes it; `--down` does not.

## Daily use

1. Change code, then run `bash scripts/dev-up.sh web` (or `api`).
2. Look at the result on `https://localhost:8443/`.
3. Run `--seed` once when you want sample data.
4. At the end of the day nothing needs to be done. If Docker was restarted, run `dev-up.sh`: it starts the VM again.

To see the benchmark: sign in as the Manager, open `/admin/benchmark/start`, pick a model and start a run. The results
and the default AI model for the website and the market analysis are on `/admin/benchmark`.

## When something goes wrong

| What you see | Why | What to do |
| --- | --- | --- |
| The address starts with `http://` (for example the link that Docker Desktop shows for the container) | Docker Desktop writes every published port as `http://`; the site speaks HTTPS only | nothing: nginx sends `http://localhost:8443/` on to `https://localhost:8443/`. The Proxmox page on port 8006 is not behind nginx and needs `https://localhost:8006/` typed by hand |
| The browser warns about the certificate | the certificate is made by the script | accept it for this site |
| `/dev/kvm is not available to Docker` | KVM is off or WSL has not loaded it; macOS has no KVM | on a Mac, use the light mode: `dev-up.sh --lite`; otherwise turn on virtualization in the BIOS, on Windows start `wsl -d Debian` first, and run `--check` |
| `bash 3.2 is too old` on a Mac | macOS comes with bash 3.2 | `brew install bash`, open a new terminal, and run the script again |
| `docker compose ... is older than 2.24.4` (light mode) | the override of nginx's port needs `!override` | update Docker Desktop |
| `the Docker daemon does not answer` | Docker Desktop is not running | start it and wait until it says running |
| `dev-f5.sh` in PowerShell says `this is the bash of WSL`, or that the SDK of `global.json` is not installed | `bash` in PowerShell is the bash of WSL, with the Linux `dotnet` and Linux user secrets that Visual Studio never sees | run `.\scripts\dev-f5.ps1` from PowerShell; it finds Git Bash. Or run `bash scripts/dev-f5.sh` in Git Bash |
| After a sign-in under F5, Visual Studio stops at `Navigation.NavigateTo` with `NavigationException` (Exception User-Unhandled), and the browser times out | Blazor sends a redirect by throwing `NavigationException`, which the framework catches; the debugger stops on it first and pauses every process, so the API and the web application do not answer | clear "Break when this exception type is user-unhandled" in the exception box and press Continue; Visual Studio remembers it |
| F5 stops with `'Api:BaseUrl' must be an absolute http or https address of the API` | the user secrets are not written | run `dev-up.sh --f5` (or `dev-f5.ps1`) once, then F5 again |
| Visual Studio: `Could not resolve SDK "Microsoft.NET.Sdk"`, while `dotnet build` in a terminal works | an update of Visual Studio failed halfway and left the .NET host without its SDK resolver (`Microsoft.DotNet.MSBuildSdkResolver`); the installer then fails with error 1714 on a missing cached `dotnet-host-*.msi` | in the Visual Studio Installer, **Repair** Visual Studio. If the repair stops on the missing MSI, download that exact .NET host version from `https://dotnet.microsoft.com/download/dotnet`, point the installer at its MSI, and repair again |
| After a build, `git status` shows `src/NFHotel.Web/packages.lock.json` changed | your SDK is a newer patch of the band of `global.json`, and it locks another version of the implicit ASP.NET packages | do not commit it: `git checkout -- src/NFHotel.Web/packages.lock.json`. CI restores in locked mode with the SDK of `global.json` and fails on a changed lock file |
| A port is in use | another program holds 8443 or 8006 | stop it, or set `DEV_SITE_PORT` or `DEV_PVE_PORT` |
| The first run stops at `waiting for ssh, cloud-init and Docker` | the VM is still installing Docker | run the script again; it continues |
| The Start page says the benchmark service is not set up | the service or the model is not ready | run `dev-up.sh benchmark`, then reload the page |
| `502 Bad Gateway` after a rebuild | nginx has not found the new container | run `dev-up.sh` again; it reloads nginx |
| The Docker build of the benchmark service fails on a submodule | the build must not fetch the private `framework` submodule | already handled with `?submodules=false` in `ops/deploy/benchmark/docker-compose.yml`; update the repository |
| The market analysis shows no AI executive summary | the model name in the settings is not exactly one that Ollama lists (`phi3.5` instead of `phi3.5:latest`), or the model is too slow for the timeout | set the default AI model on `/admin/benchmark` and run `dev-up.sh` or `deploy-services.sh analysis`: the exact name is found and written. Insights on a CPU-only VM take 5 to 10 minutes; an analysis that times out shows `TIMEOUT` as the reason, and `GUARDRAIL_REJECTED` means the model broke the wording rules |
| Out of memory, the VM is killed | Docker has too little memory | raise it (see the table at the top), then `wsl --shutdown` |

## Rules of the road

- Use it on your own machine only. The Proxmox VE image is a third-party image that runs privileged and needs
  `/dev/kvm`; never run it in CI or on a shared host.
- It is for development. Do not use its certificate, accounts or settings files for a real server.
- Never commit the `*.env` files or `ops/deploy.env`; they are git-ignored, and the script never prints a secret except the
  development accounts described above.
- To change the deployment itself, change `scripts/dev-up.sh`, `ops/scripts/deploy.sh` or `ops/scripts/deploy-services.sh` and
  run `bash scripts/dev-up.sh --check` first.
