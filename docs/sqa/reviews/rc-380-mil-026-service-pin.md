# RC-380 Review of the pin of the market analysis service after MIL-071

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-380 |
| CrossReference | [MIL-026], [MIL-071], [RC-329], [QC-YML-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-14 | Accepted | mikemike | AI as stand-in for S01 | Initial version: review of the pin of the market analysis service moved from 2868898 to 97ca890f, so the released service takes the pricing settings that the API sends since MIL-071; Go; mode autonomous, not independent | pending |

---

## Artifact Under Review

- Instance reviewed: the value of `MARKET_ANALYSIS_REF`, the commit of NF-Hotel/notebook-market-analysis that the image of the service is built from, moved from 286889844e9608452ca1971c9f6d69fc7fbabbc7 to 97ca890f58e01bb2270c45b59cee038dd3c7a9d2 in two places:
  - `ops/deploy/market-analysis/docker-compose.yml`, the default of the build argument;
  - `ops/deploy/market-analysis/market-analysis.env.example`, the template of the settings file.
- Checklist used: [QC-YML-001] for the compose file. The settings template is not YAML; it was checked for secrets and line endings only.
- Scope: a change of one value in each file. The pin was set in df900e3 ([MIL-026] task 15) and not moved since.
- Language and domain: n/a for a technical type.
- Language reviewer: none.
- Mode: autonomous, not independent. S01 gave the acceptance of the plan of 2026-11-14, which has the review records of MIL-026 in its third wave, to the AI in chat ("Du godkender for mig"). The review of the code of MIL-026 tasks 13 to 16, in progress for that wave, found the defect; the AI made the change and wrote this record.

**The defect the change fixes:**

- The release builds the service at the default of the compose file: `build-release-images.sh` runs `docker compose build --pull` in CI with no settings file, and the VM pulls the images by digest (`deploy-services.sh`, `--no-build`).
- At 2868898 the run API of the service has contract 1.1.0, and every request model refuses a field it does not know (`ConfigDict(extra="forbid", strict=True)` on the base class `_Strict` in `src/app/adapters/api/v1/market_analysis.py`). A refused request raises `InvalidRequest`.
- Since 3ebb51e ([MIL-071] task 2) `MarketAnalysisRunner` sends `pricing` with every run whenever `IPricingSettingsService` is registered, and the API registers it. So each run of the released service is expected to be refused, and the runner marks it failed; no report is made, and the page of price suggestions has no basis.
- The run of [RC-329] passed because the dev VM builds the service from the `MARKET_ANALYSIS_REF` of its own settings file, which named 8bf20104, not from the default of the compose file.
- Not seen on production: reading the VM needs a Tailscale check that S01 approves. The evidence is the code of both sides.

**Why 97ca890f:**

| Check | Result |
| --- | --- |
| What the commit is | The merge of the gate of MIL-002 of the service (2026-11-11). Compared with 8bf20104, the version of the run of [RC-329], it adds three commits that change only files under `docs/` (13 files) |
| Dependencies | Between 2868898 and 97ca890f no `pyproject.toml`, requirements file, `uv.lock` or Dockerfile of the service changed, so `requirements.lock` of this folder still pins the dependencies of the new commit; the rule of the Dockerfile to regenerate it applies when `pyproject.toml` changes. The image built from 8bf20104 with this Dockerfile and lock ran in [RC-329] |
| Settings | The only new setting is `OLLAMA_NUM_CTX`, with the default 16384 in `src/app/infrastructure/config.py` of the service; nothing new is required, so the settings file on the VM needs no change |
| Contract | The service has contract 1.2.0 with `pricing`; the copy of the client, `docs/test-data/market-contract.yaml`, is byte-identical to `openapi/market-contract.yaml` of the service at 97ca890f |
| Compose render | `docker compose config` (Docker Compose v5.5.0 in WSL) with the settings template: exit 0; the build argument and the environment show `MARKET_ANALYSIS_REF: 97ca890f58e01bb2270c45b59cee038dd3c7a9d2` |

## Checklist Results

| # | Criterion (QC-YML-001) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Parses with a safe loader; no duplicate keys | Pass | `docker compose config` parses and validates the file; the change replaces one value and adds no key |
| 2 | 2 spaces, LF, one final newline, no trailing spaces | Pass | Indentation unchanged; 0 carriage returns in both files |
| 3 | Passes `yamllint` with its committed configuration | Pass, read in CI | `yamllint` is not installed on this machine; Ops CI runs it with `.yamllint` on the pull request (step "Run actionlint, shellcheck, shfmt and yamllint"), and the result is read before the merge |
| 4 | A scalar that YAML would coerce is quoted | Pass | The value is part of `${MARKET_ANALYSIS_REF:-...}`, a plain string that starts with `$` |
| 5 | Only safe, plain types | Pass | Unchanged |
| 6 | No secret in the file | Pass | A commit hash of a public repository |
| 7 | Embedded program text of at most five lines | N-A | Unchanged |
| 8 | A repeated block or value is defined once | Pass with note | The commit is written in the default of the compose file and in the settings template, as before: the default serves the release build, which has no settings file, and the template documents the setting for the VM. The comment above the template line says to upgrade both with the lock file |
| 9 | Anchors and aliases only where supported | Pass | Unchanged (`x-analysis-image`) |
| 10 | Validates against the schema of the tool (Optional) | Pass | `docker compose config` |
| 11 | Header comment | Pass | Unchanged |
| 12 | Key names follow the tool | Pass | Unchanged |
| 13 | Block scalars (Optional) | N-A | None |
| 14 | File name (Optional) | Pass | Unchanged |
| 15 | Exercised by a check without the real target (Optional) | Pass | `docker compose config`; the release build of CI builds the image on the merge |

## Findings

- **F1, Major (functional): the released service refuses every run.** Described above; fixed by this change once the next release is deployed.
- **F2, note: the effect on production is not observed.** Neither the defect nor the fix was seen on the production VM. After the release, one market analysis run started by a Manager on production shows it: it should finish with a report and price suggestions.
- **F3, note: no check ties the pin to the contract.** Nothing in CI fails when the pinned service and the copy of the contract of the client differ, so this defect could not be caught. That belongs to the gate of [MIL-026] (its criterion on the sample and the contract), not to this change.

## Overall Verdict

Go. The change moves the release to the version of the service that the API has needed since [MIL-071], and that version was exercised end to end in [RC-329]. Every Mandatory criterion of [QC-YML-001] passes; criterion 3 is read in Ops CI before the merge. S01 gave the acceptance to the AI in chat on 2026-11-14 ("Du godkender for mig"), and the AI accepted this record as stand-in for S01. The reviewer is not independent: the AI made the change and wrote this record.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Read Ops CI (`yamllint`) and NF Hotel CI of the pull request before the merge | AI as stand-in for S01 | Before the merge |
| Start one market analysis run on production after the release, and confirm that it finishes with a report (F2) | S01 | The first working day after the release |
| A check that fails when the pinned service and the contract copy of the client differ (F3) | AI as stand-in for S01 | At the gate of MIL-026 |

---

[MIL-026]: ../../milestones/mil-026-market-analysis-report.md
[MIL-071]: ../../milestones/mil-071-price-suggestions-from-the-service.md
[RC-329]: ./rc-329-mil-071-gate.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
