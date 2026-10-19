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
| `dev-up.sh benchmark` | rebuild the benchmark service from the commit named in `deploy/benchmark/benchmark.env` |
| `dev-up.sh --all` | rebuild `api` and `web` and start whatever is not running |
| `dev-up.sh --seed` | load the sample rooms, guests and bookings of `db/seed_dev.sql` |
| `dev-up.sh --open` | open the site in your browser when it is ready |
| `dev-up.sh --check` | only check the machine |
| `dev-up.sh --down` | stop the services; the database, the VM and Proxmox stay |
| `dev-up.sh --destroy` | remove the Proxmox container with the VM and everything in it, including the database |

Settings through the environment: `DEV_SITE_PORT` (default 8443), `DEV_PVE_PORT` (default 8006) and `DEV_MODELS` (the
Ollama models to pull, default `llama3.2:3b phi3.5`; an empty value pulls none).

## What it does, step by step

1. Checks the machine (the same check as `--check`).
2. Makes any missing settings file from its `.example` with new random secrets: `pve.env`, `deploy.env` and the
   `deploy/*/*.env` files of postgres, the API, the web application and nginx, plus `ollama.env` and `benchmark.env`. A
   file that exists is never changed. All of them are git-ignored.
3. Starts the Proxmox VE container once (later runs only start it again) and sets its root password from `pve.env`.
4. Runs `scripts/deploy.sh --local`, which creates the VM when it is missing, copies your working tree into it and
   deploys the services in the right order with `scripts/deploy-services.sh`: the web application waits for a healthy
   database and API, and nginx waits for the web application and the market analysis API.
5. Pulls the small Ollama models, creates the accounts and publishes the site on `https://localhost:8443/`.

## Sign in

Open `https://localhost:8443/admin/login`. The browser warns about the certificate, because the script makes a
development certificate; accept it. The script prints one account per role (Manager, Receptionist and Housekeeping) at
the end of each run, **only** when `ASPNETCORE_ENVIRONMENT` is `Development` in `deploy/nfhotel-api/nfhotel-api.env`,
which is what the script writes. A release deployment never prints them. The passwords are in plain text in two
git-ignored files:

- the Manager: `Staff__InitialManager__Password` in `deploy/nfhotel-api/nfhotel-api.env`;
- the Receptionist and the Housekeeping user: `deploy/nfhotel-api/dev-staff.env`.

If you change a password in the application, these files no longer match it.

## Where things are

| What | Where |
| --- | --- |
| The site | `https://localhost:8443/` (an address that starts with `http://` answers 400, use `https://`) |
| Proxmox VE | `https://localhost:8006/`, user `root`, the password is `PVE_PASSWORD` in `pve.env` |
| Database, benchmark results, Ollama models | volumes inside the VM, which lives inside the `proxmox-node1` container (the name is `PVE_CONTAINER` in `deploy.env`) |
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
| `400 Bad Request: The plain HTTP request was sent to HTTPS port` | the address starts with `http://` | use `https://localhost:8443/` |
| The browser warns about the certificate | the certificate is made by the script | accept it for this site |
| `/dev/kvm is not available to Docker` | KVM is off or WSL has not loaded it | turn on virtualization in the BIOS; on Windows start `wsl -d Debian` first; run `--check` |
| `the Docker daemon does not answer` | Docker Desktop is not running | start it and wait until it says running |
| A port is in use | another program holds 8443 or 8006 | stop it, or set `DEV_SITE_PORT` or `DEV_PVE_PORT` |
| The first run stops at `waiting for ssh, cloud-init and Docker` | the VM is still installing Docker | run the script again; it continues |
| The Start page says the benchmark service is not set up | the service or the model is not ready | run `dev-up.sh benchmark`, then reload the page |
| `502 Bad Gateway` after a rebuild | nginx has not found the new container | run `dev-up.sh` again; it reloads nginx |
| The Docker build of the benchmark service fails on a submodule | the build must not fetch the private `framework` submodule | already handled with `?submodules=false` in `deploy/benchmark/docker-compose.yml`; update the repository |
| Out of memory, the VM is killed | Docker has too little memory | raise it (see the table at the top), then `wsl --shutdown` |

## Rules of the road

- Use it on your own machine only. The Proxmox VE image is a third-party image that runs privileged and needs
  `/dev/kvm`; never run it in CI or on a shared host.
- It is for development. Do not use its certificate, accounts or settings files for a real server.
- Never commit the `*.env` files or `deploy.env`; they are git-ignored, and the script never prints a secret except the
  development accounts described above.
- To change the deployment itself, change `scripts/dev-up.sh`, `scripts/deploy.sh` or `scripts/deploy-services.sh` and
  run `bash scripts/dev-up.sh --check` first.
