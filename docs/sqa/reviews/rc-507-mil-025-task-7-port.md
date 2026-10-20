# RC-507 Review of the port of the NFHotel deploy changes and the move of the Proxmox tests (MIL-025 task 7)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-507 |
| CrossReference | [MIL-022], [DSA-001], [ADR-0500], [QC-SH-001], [QC-WF-001], [QC-YML-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [8670ad0] |

---

## Artifact Under Review

- **Instances reviewed:**
  - the three-way merge of the NFHotel changes made after `0f69a52` (NFHotel `main` at `7c6f348`) into `deploy/`, `scripts/deploy-services.sh`, `scripts/deploy.sh`, `scripts/test-compose-proxy.sh`, `scripts/e2e-stack.sh` and `docs/wiki/Proxmox-Deployment.md`;
  - the benchmark image (`deploy/benchmark/Dockerfile`, `docker-compose.yml`, `requirements.lock`) rebuilt for the benchmark API service of notebook-benchmark `409693d`;
  - `scripts/resolve-model-name.sh` and `tests/test-resolve-model-name.sh` (copied), the merge of NF-Hotel/DevOps#7 and its test;
  - `tests/NFHotel.Proxmox.Tests` (copied), its solution, `global.json`, the CI job and the Dependabot entry;
  - `MIGRATION.md`, `README.md`, `.gitignore`.
- **Checklists used:** [QC-SH-001], [QC-WF-001], [QC-YML-001], the rows that the change touches.
- **Scope:** delta review (earlier records RC-501, RC-503, RC-504).
- **Language and domain:** n/a (technical type).
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).
- **Tools:** `tests/lint.sh` (actionlint 1.7.7 with shellcheck, shellcheck 0.11.0, shfmt 3.14.1, yamllint 1.35.1); the seven script tests of the CI matrix in Debian 12; `tests/test-nginx.sh` in WSL Debian; `scripts/scan-image.sh` on a local build of the benchmark image; `scripts/e2e-stack.sh`, `tests/check-e2e-stack.sh` and `scripts/run-e2e-tests.sh` against NFHotel `7c6f348`; `dotnet test` of the Proxmox solution and of its Docker image.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| SH 1–2 | Shebang, strict mode, quoting, `[[ ]]` | Pass | The ported blocks of `deploy-services.sh` keep the style of the file; the settings are read whole lines and split at the first `=` (#7) |
| SH 4 | shellcheck and bash -n | Pass | 0 findings |
| SH 5 | Errors and usage | Pass | `require` names the container and the order to deploy in; a model that is not installed leaves `market-analysis.toml` unchanged with a warning |
| SH 7 | No secret on a command line or echoed | Pass | The benchmark token is read from `benchmark.env` into the environment of compose only; the database query of the default model runs inside the database container with its own variables |
| SH 11 | Behaviour is tested | Pass | `test-deploy-services-env.sh` 2/2 (its fake docker now answers the start order check), `test-resolve-model-name.sh` 14/14, `test-deploy-release.sh` 36/36, `test-cd-steps.sh` 44/44, `test-vm-setup.sh` 17/17, `test-scan-gate.sh` 17/17, `test-dast-rules.sh` 14/14, `test-nginx.sh` 10/10; end-to-end stack checks pass and the 9 end-to-end tests of NFHotel pass |
| SH 12 | shfmt | Pass | No diff |
| SH 15 | Defined once | Pass | The image variables, digest pins, connection string of `nfhotel_app` and the optional API route stay as they were here; NFHotel's removal of them (task 5) was not ported |
| WF 7 | Least permissions | Pass | The new job has the workflow's `contents: read` and no secret |
| WF 8 | Actions pinned by full SHA | Pass | The same checkout and setup-dotnet SHAs as the other jobs |
| WF 22 | Features verified on the instance that runs them | Fail | Not yet: the new job and matrix entries run on github.com only when this pull request runs CI |
| YML 1–3 | Parses, formatting, yamllint | Pass | 0 findings |

## Findings

- **The benchmark is a different service now.** NFHotel replaced the marimo notebook behind nginx with the benchmark API (MIL-020): no proxy route, networks `nfhotel-api` and `nfhotel-llm`, a token each way. NFHotel builds it from the repository's own Dockerfile, which installs with pip from version ranges. Here the Dockerfile keeps the digest pin, a hashed lock regenerated for the four runtime dependencies only, and the removal of pip. The local build has 101 packages in its SBOM and passes the gate; the container answers `/health` read-only, with all capabilities dropped.
- **Production needs new settings before the next deploy.** `deploy-services.sh` deploys every service, and the benchmark service now reads `NFBENCH_API_TOKEN`, `NFBENCH_WEB_URL` and `NFBENCH_OLLAMA_URL` from `benchmark.env` and the API token from `nfhotel-api.env`. The `benchmark.env` on the VM is the old one, so it must be updated from the new example before this change is deployed. The market analysis settings (`MarketAnalysisService__*` in `nfhotel-api.env`, the timeout in `market-analysis.toml`) are optional: without them the page says that the service is not set up.
- **`scripts/test-compose-proxy.sh` was not run.** It builds the market-analysis image from its source repository, which no longer exists (ADR-0500, R17). The end-to-end stack covers the proxy instead.
- **The frozen market-analysis image now joins `nfhotel-api`.** The NFHotel API calls it there (MIL-026). It has no authentication, so that network must stay internal, which `deploy-services.sh` ensures by creating it with `--internal`.

## Overall Verdict

Go-with-conditions. Every Mandatory criterion that the change touches passes except QC-WF-001 criterion 22, which closes when CI of this pull request is green on github.com.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Update `deploy/benchmark/benchmark.env` on nf-hotel from the new example (a new `NFBENCH_API_TOKEN`), and set `BenchmarkService__*` and `MarketAnalysisService__*` if wanted | S01 | Before the first deploy that contains this change |
| Merge after NF-Hotel/DevOps#8 (stacked on it); the NFHotel pull request that closes NF-Hotel/NFHotel#159 merges after NF-Hotel/DevOps#1 | S01 | With the review of the stack |

---

[MIL-022]: ../../milestones/mil-022-supply-chain-security.md
[DSA-001]: ../devsecops-assessment.md
[ADR-0500]: ../../adr/adr-0500-freeze-market-analysis-image.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[8670ad0]: https://github.com/NF-Hotel/DevOps/commit/8670ad067a55f3efd6b83a3393038c5e1c6d46ee
