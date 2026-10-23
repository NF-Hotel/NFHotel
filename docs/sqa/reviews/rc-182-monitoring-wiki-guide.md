# RC-182 Review of the rewritten wiki page Monitoring

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-182 |
| CrossReference | [ADR-0020], [ADR-0022], [RC-180], [QC-SH-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-23 | Accepted | mikemike | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- **Instances reviewed:**
  - the wiki page `docs/wiki/Monitoring.md`;
  - the check of its silence command in `ops/tests/test-monitoring-stack.sh`.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).

## Why

S01 asked for the page to be written "the way it is really done". The earlier page mixed the decision (why) with the instructions (how), and the reasons repeated ADR-0020.

The page now follows the usual split of documentation ([Diátaxis](https://diataxis.fr/)):
- **How-to:** install, daily use, change;
- **Runbook and troubleshooting:** what to do when something fails;
- **Reference:** files, reports and checks.

The explanation is a short summary that links to the ADR, which holds the full reasoning. The decision records stay in `docs/adr/`, so the wiki and the ADRs do not repeat each other.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| Audience and purpose | Stated at the top | Pass | Who the page is for, the decision behind it, and a table of contents |
| Steps are runnable | Each step gives the command and the expected result | Pass | The commands were checked against the scripts and compose files. The compose project of Alloy is `nfhotel-alloy`, so its container is `nfhotel-alloy-alloy-1` |
| Silence command | It silences the probe alerts too | Pass | The probe alerts have no `host` label, so `host=nf-hotel` would not silence `SiteDown`. The page uses `alertname!=Watchdog`, and the stack test in CI runs that exact command |
| Runbook | One row per alert rule, with its severity | Pass | All 12 rules of `nfhotel.yml`, plus the Healthchecks.io alert |
| Links | Work on the GitHub wiki | Pass | Repository files are linked with full URLs (ADR-0022) |
| No secret | No value in the page | Pass | The secrets are read with `read -s` into files |

## Overall Verdict

Go.

## Action Items

None.

---

[ADR-0020]: ../../adr/adr-0020-monitoring-stack.md
[ADR-0022]: ../../adr/adr-0022-publish-the-wiki-from-docs.md
[RC-180]: rc-180-monitoring-install-guide.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
