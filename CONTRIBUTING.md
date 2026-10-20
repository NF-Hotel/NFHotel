# Contributing

## Where work belongs (ADR-0016)

This repository holds NF Hotel: the **application** and its **operations**.

| Area | Where | Reviewed by |
| --- | --- | --- |
| The ASP.NET Core and Blazor code, the services, the API, the domain, the tests and the EF Core migrations | `src/`, `tests/`, `db/` | application maintainers |
| The application configuration templates | `appsettings*.json` | application maintainers |
| `Dockerfile`, `Dockerfile.api` | the root | code owners of the operations (`.github/CODEOWNERS`) |
| Compose files, deploy, backup and database scripts, nginx, TLS, Tailscale, scanning, the throwaway test stack, the Proxmox tests | `ops/` | code owners of the operations, S03 for what runs on the VM |
| The composite actions and the workflows | `.github/actions/`, `.github/workflows/` | code owners of the operations |
| The `production` environment and the Tailscale trust credential | settings of this repository | S03 |
| The project baseline, the ADRs, the milestones, the DevSecOps assessment and the review records | `docs/` | the reviewer named in each document |

**A change to the application and its deployment is one pull request.** The workflows read the actions from the same commit, so nothing has to be pinned or bumped. A script that runs as root on the VM takes effect only after S03 installs it again (wiki page CI-CD).

NF-Hotel/DevOps, where the operations lived under ADR-0012, is archived. Its history is part of this repository: `git log -- ops/`.

## How to work

- Follow the SQA/QC framework in `framework/` (`git submodule update --init --recursive`).
- **Branches:** never commit on `main`. Open a pull request with one approving review from someone other than the author.
- **CI:** it must be green. The required checks come from `ci.yaml` (the application), `ops-ci.yaml` (the operations), `release-check.yaml` (the release images) and `e2e-dast.yaml` (the browser tests and the DAST baseline).
- **IDs:** MIL, ADR and RC numbers continue the sequences of `docs/artifact-registry.md`. The IDs from 500 that NF-Hotel/DevOps allocated (ADR-0500, RC-500 to RC-507) keep their numbers.
- **Formatting:** English, LF line endings (`.editorconfig`, `.gitattributes`).
- **Secrets:** they never enter git.
