# ADR-0031 Sign-in keys and benchmark runs that survive a deploy

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0031 |
| CrossReference | [ADR-0005], [ADR-0013], [ADR-0004], [ADR-0024], [MIL-020], [MIL-043] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-26 | Accepted | mikemike | AI as stand-in for S01 | Initial version; written by the AI under MIL-043, which S01 accepted in chat on 2026-10-26; accepted after RC-227 Go | pending |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

**Who decided what.** S01 asked on 2026-10-26 for sign-ins and benchmark runs that survive a deploy, after the deploy of MIL-042 broke a page and lost a queued run. The design of this record was written by the AI assistant.

Every release gives the four images new digests, so every deploy recreates the containers. Two kinds of state live only in a container's memory:

1. **The Data Protection keys of the web application.** ASP.NET Core protects the staff cookie (ADR-0005) and the antiforgery token with them. The container has a read-only root and no writable home folder, so the keys are kept in memory (`EphemeralXmlRepository`). After a restart every cookie and token is unreadable: staff are signed out, and a form that was open fails ("The key … was not found in the key ring").
2. **The runs of the benchmark service.** The web application starts a run on the benchmark service (MIL-020), which keeps its queue in memory; the API stores a run only when the service delivers the finished result (ADR-0013). A restart forgets a queued or running run, and the back office has nothing that says it existed.

Options for the keys:

1. **A Docker volume** for the key folder, owned by the user of the application.
2. **The database.** The web application has no database login, by design (ADR-0004, Business Case criterion 21).
3. **Keys encrypted with a certificate** in the volume. The certificate would be one more secret file on the same VM, beside the keys it protects, so it adds rotation work and little protection.
4. **No change.** Staff are signed out at every deploy.

Options for the runs:

1. **A run record in the API.** The web application tells the API about each run it starts and each state it sees. A run that the service no longer knows becomes `interrupted`, and the back office shows it and can start the same choice again.
2. **The deploy waits** until no run is going. A run takes minutes to hours on the CPU of the VM, so CD would wait for an unknown time, and a run that hangs would block every release.
3. **The benchmark service keeps its queue on disk.** That is a change in `NF-Hotel/notebook-benchmark`, its own decision, and a restart would still stop a running model; the hotel would still need to say what happened.

## Decision

We follow option 1 for both.

1. **Keys in a volume.** The web application calls `PersistKeysToFileSystem` with the folder from the setting `DataProtection:KeysPath` and `SetApplicationName("NFHotel.Web")`, so a new container reads the keys of the old one. The image creates `/app/keys` owned by the user `app`; `ops/deploy/nfhotel-web` mounts the volume `keys` of its compose project (`nfhotel-web_keys` on the VM) there, and the root stays read-only. The keys are not encrypted at rest: the volume is readable only by root and by `app` on the VM, as the secret files are ([ADR-0024]). They are not in the backup: losing them only signs staff out.
2. **A run record in the API.** The entity `BenchmarkRequest` (table `benchmark_request`) holds the run id of the service, the choice (models, tasks, repetitions), who started it, when, its state and the last error. The states are `queued`, `running`, `done`, `failed` and `interrupted`. `queued` may become any of the others; `running` may become `done`, `failed` or `interrupted`; the last three are final. A change to the same state is accepted and changes nothing; any other change is refused.
3. **Who writes it.** The web application, with the staff token and the permission `Benchmark.Run`: it records a run right after the service accepted it, and records each new state it reads. When the service answers that it does not know the run, the web application records `interrupted`. The pages that list or follow runs ask the service about every open run first, so a run that was lost while nobody looked is marked as soon as someone opens the page.
4. **What the back office shows.** The runs page lists the runs of the last 7 days that are not `done` (a done run is in the stored results), newest first. The start page follows a run that is still going when it opens. An interrupted or failed run can be started again with the same choice; that is a new run.

## Consequences

**Positive:**

- A deploy no longer signs staff out or breaks an open form.
- A run that a restart stops is shown as interrupted, with its choice, and can be started again with one click.

**Negative:**

- The keys are files on the VM. Whoever reads them can forge a staff cookie, which needs root on the VM, as reading the secret files does.
- A restart still stops a running benchmark; the hotel only records it. Keeping the queue across restarts is for the benchmark service to decide (option 3).
- A run whose state was never read again stays `queued` or `running` in the table until someone opens a benchmark page.

**Quality:** reliability and usability of the back office improve; security is unchanged, because the keys sit beside the existing secrets (ISO/IEC 25010:2023).

## Affected Artifacts

- [ADR-0005]: the staff cookie keeps its keys across a restart.
- [ADR-0013]: the API stores the runs that were started as well as the delivered results.
- [MIL-043]: tasks 1 to 5.

---

[ADR-0004]: ./adr-0004-database-api.md
[ADR-0005]: ./adr-0005-backoffice-login.md
[ADR-0013]: ./adr-0013-benchmark-results.md
[ADR-0024]: ./adr-0024-secrets-as-files.md
[MIL-020]: ../milestones/mil-020-start-benchmark-run.md
[MIL-043]: ../milestones/mil-043-state-across-deploys.md
