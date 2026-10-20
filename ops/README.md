# NF Hotel operations

Everything that builds, releases, deploys, secures and runs NF Hotel (ADR-0016). It was the repository NF-Hotel/DevOps under ADR-0012, and it was brought in here with its history on 2026-10-20: `git log -- ops/` shows its commits, and `MIGRATION.md` records where each file first came from.

## Layout

| Path | What it is |
| --- | --- |
| `deploy/` | Docker Compose files per service, nginx, systemd units, the frozen market-analysis image (ADR-0500); settings examples (`*.env.example`) |
| `scripts/` | Release build and scan, CD steps, the deploy command of the VM (`deploy-release.sh`), deploy, backup, restore, database preparation, Proxmox and VM setup, the throwaway E2E stack, the DAST scan |
| `security/` | `trivyignore.yaml` and `zap-rules.tsv`: accepted findings, each with a reason and an expiry |
| `tests/` | Tests of the scripts and the configuration; `tests/lint.sh` runs every linter of `ops/` and of `.github/`. `tests/NFHotel.Proxmox.Tests` is a .NET solution of its own; its live tests run through `scripts/run-proxmox-tests.sh` |
| `deploy.env.example`, `pve.env.example` | Settings of `scripts/deploy.sh` and of the Proxmox tests (copy without `.example`; git-ignored) |
| `artifact-registry-devops.md` | The registry of NF-Hotel/DevOps at the move, for the IDs from 500 |

The composite actions are in `../.github/actions/` and the CI of this folder is `../.github/workflows/ops-ci.yaml`. The documents (DSA-001, ADR-0007 to ADR-0011, ADR-0500, MIL-021 to MIL-025, their review records) are in `../docs/`, and the runbooks in the wiki: CI-CD, Database Preparation, Proxmox Deployment.

## Run the checks locally

```bash
bash ops/tests/lint.sh
docker run --rm -v "$PWD/ops:/src:ro" debian:12-slim bash /src/tests/test-deploy-release.sh
```

On Windows with Git Bash, set `MSYS_NO_PATHCONV=1` and give `REPO_HOST` the Windows path of the repository root (for example `C:/Users/you/NFHotel`) to `lint.sh`.
