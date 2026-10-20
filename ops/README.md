# NF Hotel DevOps

The authoritative repository for NF Hotel infrastructure and operations engineering (ADR-0012 in [NF-Hotel/NFHotel](https://github.com/NF-Hotel/NFHotel)). The application, its tests, its Dockerfiles and the workflow entry points live in NFHotel; everything that builds, releases, deploys, secures and runs it lives here.

## Layout

| Path | What it is |
| --- | --- |
| `.github/actions/release-images` | Composite action: builds the four images from an NFHotel checkout, pushes them to GHCR, writes the release manifest |
| `.github/actions/deploy-release` | Composite action: checks a release, joins the tailnet, runs the deploy command on the VM |
| `.github/workflows/ci.yaml` | CI of this repository: linters, script tests, the Proxmox tests without a server, a cross-repository build without push, the PostgreSQL test |
| `deploy/` | Docker Compose files per service, nginx, systemd units; settings examples (`*.env.example`) |
| `scripts/` | Deploy, rollback, backup, restore, database preparation, Proxmox and VM setup scripts |
| `tests/` | Tests of the scripts and the configuration; `tests/lint.sh` runs every linter; `tests/NFHotel.Proxmox.Tests` (a .NET solution of its own) tests the Proxmox API client, its live tests through `scripts/run-proxmox-tests.sh` |
| `docs/` | DSA-001, ADR-0007 to ADR-0011, MIL-021 to MIL-024, their review records, the wiki pages |
| `framework/` | The SQA/QC framework (submodule) |
| `MIGRATION.md` | Where every file came from in NFHotel |

## Using the actions from NFHotel

```yaml
- uses: NF-Hotel/DevOps/.github/actions/release-images@<full commit SHA>
  with:
    app-path: .
    app-sha: ${{ github.event.workflow_run.head_sha }}
    push: "true"
```

The actions refuse to run unless they are pinned by a full commit SHA. The calling job keeps its own permissions, environment and concurrency.

## Contributing

See `CONTRIBUTING.md`, including the ownership policy and the shared items. Security reports go through `SECURITY.md`.
