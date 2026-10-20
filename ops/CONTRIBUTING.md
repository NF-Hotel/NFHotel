# Contributing

## What belongs here

This repository is the authoritative home of all NF Hotel infrastructure and operations engineering (ADR-0012 in NF-Hotel/NFHotel). That means:

- **Pipelines:** the CI/CD composite actions and their scripts; GHCR releases, SBOMs, attestations and image scanning.
- **Deployment:** Docker Compose, the deploy, rollback and Proxmox scripts.
- **Platform services:** PostgreSQL roles, backup, retention and restore; Tailscale and nftables configuration; nginx and TLS automation.
- **Operations:** secrets management design, monitoring, logging, alerting and infrastructure security tests.
- **Records:** the infrastructure ADRs, milestones, runbooks, compliance matrices, risk assessments and review records.

The application belongs in [NF-Hotel/NFHotel](https://github.com/NF-Hotel/NFHotel): the code, the tests, EF Core migrations and application configuration templates. So do the `Dockerfile`s and the workflow entry points (`ci.yaml`, `release.yaml`, `cd.yaml`), which call the actions of this repository pinned by full commit SHA.

| Shared item | Owner | Who reviews |
| --- | --- | --- |
| `Dockerfile`, `Dockerfile.api` | NFHotel | DevOps code owners |
| `release.yaml`, `cd.yaml` (permissions, environment, triggers) | NFHotel | DevOps code owners, S03 |
| The pinned SHA of this repository in NFHotel | NFHotel | a pull request per bump |
| The `production` environment and the Tailscale trust credential | NFHotel settings | S03 |

## Policy

- New DevOps and DevSecOps work starts here by default.
- An exception (DevOps work in NFHotel) needs an ADR here that states why, reviewed like any other ADR.
- A change that both repositories need is two pull requests: this repository first, then the SHA bump in NFHotel.

## How to work

- Follow the SQA/QC framework in `framework/` (`git submodule update --init`).
- **Branches:** never commit on `main`. Work on a branch, open a pull request, and get one approving review from someone other than the author.
- **CI:** it must be green. It runs actionlint, yamllint, shellcheck, shfmt and the script tests.
- **IDs:** new MIL, ADR and RC numbers start at 500 (`docs/artifact-registry.md`).
- **Formatting:** every file uses LF line endings and English; shell scripts are formatted with `shfmt -i 2 -ci`.
- **Secrets:** they never enter git. Settings files with secrets stay on the server (`/etc/nfhotel/`, `deploy/*/*.env`, all git-ignored).
- **Production:** it changes only through the approved release procedure (ADR-0007): a release from NFHotel, an approval in `production`, then the deploy command.
