# RC-172 Review of MIL-023 task 12 (code findings that block the deploy gate)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-172 |
| CrossReference | [MIL-023], [ADR-0021], [QC-MIL-001], [QC-CS-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-23 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [caaf6b8] |

---

## Artifact Under Review

- **Instances reviewed:**
  - the amendment of [MIL-023] (task 12);
  - the change it plans:
    - `HotelDbContextFactory.cs`;
    - `ops/deploy/backup-target/Dockerfile`;
    - `NFHotel.Domain.csproj`.
- **Checklists used:** [QC-MIL-001], [QC-CS-001].
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`). S01 accepted the plan for the remaining milestones on 2026-10-23.

## Why

The first deploy under ADR-0021 (CD run 39923290209, commit `429fdc3`) was refused by the gate: "SonarCloud Code Analysis (failure)". The quality gate rated the security of new code D because of two findings:

- **csharpsquid:S2068, `HotelDbContextFactory.cs` line 27.** "password" in a hard-coded connection string. It is the design-time placeholder that EF reads to generate and script migrations, and it never connects. It came into the new-code period with the translation commit `11032d1`. The placeholder needs no password, so the password is removed: the finding is fixed, not hidden.
- **docker:S6473, the backup target Dockerfile line 15.** `EXPOSE 22`, from this session's MIL-021 work. `EXPOSE` only documents a port. The compose file publishes ssh on the tailnet address of the laptop only, so the line is removed.

Until the findings are gone, every commit of `main` fails the SonarCloud check and nothing deploys. That is the gate working, and it needs this fix. The Danish comment of `NFHotel.Domain.csproj` is translated too (RC-171 finding).

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| MIL 1–2 | Deliverable and checkable result | Pass | The task names the findings, and the next SonarCloud check of `main` shows the result |
| MIL 3–6 | Dependencies, traceability, owner, date | Pass | ADR-0021; unchanged owner and date |
| CS | Builds without warnings; no hard-coded credential | Pass | `dotnet build` of Infrastructure and Domain is clean, with warnings as errors. The CI release build scripts the migrations from the placeholder |

## Overall Verdict

Go.

## Action Items

None.

---

[MIL-023]: ../../milestones/mil-023-operational-quality.md
[ADR-0021]: ../../adr/adr-0021-automatic-deploy-when-green.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[caaf6b8]: https://github.com/NF-Hotel/NFHotel/commit/caaf6b8c9ed143ecaab254a3af2d9b3729240d73
