# Contributing

## Where work belongs (ADR-0012)

This repository holds the NF Hotel **application**:
- the ASP.NET Core and Blazor code, the services and the API;
- the domain and business logic, the tests and the EF Core migrations;
- the application configuration templates (`appsettings*.json`);
- the project baseline (Business Case, Stakeholder Analysis, Project Plan, application ADRs and milestones).

All **DevOps and DevSecOps** work belongs in [NF-Hotel/DevOps](https://github.com/NF-Hotel/DevOps), by default and without exception unless an ADR there says why. That covers:
- pipeline logic, Docker Compose, deployment and rollback;
- PostgreSQL infrastructure, backups, Tailscale, nftables, nginx and TLS;
- secrets management, image scanning, SBOMs and attestations;
- monitoring, and the infrastructure ADRs, runbooks and evidence.

| Shared item | Lives in | Reviewed by |
| --- | --- | --- |
| `Dockerfile`, `Dockerfile.api` | here | DevOps code owners (`.github/CODEOWNERS`) |
| `.github/workflows/ci.yaml` | here | application maintainers |
| `.github/workflows/release.yaml`, `cd.yaml`, `release-check.yaml` | here: entry points only (triggers, permissions, environment), calling DevOps actions pinned by full commit SHA | DevOps code owners, S03 |
| The `production` environment and the Tailscale trust credential | settings of this repository | S03 |

**A change that needs both repositories** is two pull requests: the DevOps change first, then the SHA bump here.

**The deployment lives in NF-Hotel/DevOps only** (MIL-025 task 7): the compose files, the settings examples, the deploy and Proxmox scripts and the Proxmox tests. `scripts/dev-up.sh` (and `dev-up.ps1`) runs that deployment on your machine from a DevOps checkout beside this repository (`../DevOps`, or the folder in `NFHOTEL_DEVOPS_DIR`); see the wiki page Local Development Deploy.

## How to work

- Follow the SQA/QC framework in `framework/` (`git submodule update --init --recursive`).
- **Branches:** never commit on `main`. Open a pull request with one approving review from someone other than the author.
- **CI:** it must be green. The required checks come from `ci.yaml`; `release-check.yaml` builds the release images on every pull request.
- **IDs:** this repository allocates MIL, ADR and RC numbers below 500. NF-Hotel/DevOps allocates them from 500.
- **Formatting:** English, LF line endings (`.editorconfig`, `.gitattributes`).
- **Secrets:** they never enter git.
