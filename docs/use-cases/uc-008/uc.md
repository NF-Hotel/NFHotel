# UC-008 Compare Models by Time and Quality

## Metadata
| Key | Value |
| --- | --- |
| ID | UC-008 |
| CrossReference | [UCD-001], [US-001], [SA-001], [UC-004], [UC-009], [MIL-019], [ADR-0005], [ADR-0013] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-14 | Accepted | mikemike | AI as stand-in for S01 | Initial version (MIL-019); accepted after RC-369 Go, at S01's word in chat ("Du godkender for mig"); mode autonomous, not independent | [50f850d] |

---

**Format:** Fully Dressed

## Fully Dressed

- **Scope:** NFHotel platform, hotel back office
- **Level:** user-goal
- **Primary Actor:** Manager (a member of hotel staff with the role Manager, which holds the permission `Benchmark.View`)
- **Stakeholders and Interests:**
  - S01 — Team 2, the owner of the hotel: the local AI model is chosen on the response time and the answer quality measured on this machine, with the models side by side (US-001.02).
  - S02 — Other NFHotel 2.0 guilds: the benchmark service delivers a finished run once; a repeated delivery stores nothing new, and a delivery in an unknown form stores nothing.
  - S04 — Valdamar: every figure is the stored figure of one run, with the machine, the dataset and the prompt version it was measured with.
  - S05 — Hotel staff: only the Manager is offered the benchmark; the other roles see nothing of it.
- **Preconditions:** The Manager is signed in (UC-004). The benchmark service delivers each finished run to the hotel system (UC-009).
- **Postconditions (success guarantee):** The Manager has read the stored runs and their measurements as the benchmark service delivered them. Reading changes no stored run; only the records of the started runs listed apart are brought up to date with the benchmark service (UC-009, step 6).

### Main Success Scenario

1. The Manager asks for the benchmark runs.
2. The system lists the stored runs, newest first. Each shows when it started, its models, its number of requests, the machine (CPU, memory and GPU) and the dataset. Runs started in the last seven days that have no stored result are listed apart (UC-009).
3. The Manager opens a run.
4. The system shows the facts of the run: the machine and its cores, the operating system, the model server and its version, the dataset and its rows, the prompt version and the number of requests.
5. The system shows, for the hotel cancellation analysis, the mean time to the first token, the mean total time and the mean tokens per second of each model, cold and warm, as a table and as charts. The Manager may choose the deterministic tasks, or both, instead; the system then also shows, for each model and task, how many of the graded requests passed.
6. The system shows the mean memory in use on the machine and on the GPU before, after and at the peak of the requests of each model.
7. The Manager chooses up to three hotel answers. The system shows them side by side: the summary, the cancellation rates the model gave (overall, direct customers and agent bookings), the key findings and the limitations.
8. The Manager reads the measurements and the outcome of every request, and the answer of a request.
9. The Manager downloads the measurements of the run as CSV or as JSON.

### Extensions (Alternative / Exception Flows)

- 1a. A Receptionist or Housekeeping user asks for the benchmark runs:
  1. The system tells them that the role has no access (UC-004, 6a). Where the runs are served, the call is refused as well.
- 2a. No run has been stored yet:
  1. The system says so and lists none.
- 2b. The hotel system cannot be reached:
  1. The system says so and lists no run.
- 3a. The run is no longer stored, for example because another Manager deleted it (UC-009, step 9):
  1. The system says that the run was not found and opens none.
- 5a. No request of the chosen workload finished without an error:
  1. The system says so and shows no means.
- 5b. A request ended with an error:
  1. The system leaves it out of the means and shows it as an error among the requests.
- 6a. The GPU memory was not measured (the machine has no NVIDIA GPU, or the tool that measures it was not found):
  1. The system says so and shows the memory of the machine only.
- 7a. The Manager chooses a fourth answer:
  1. The system drops the answer chosen first and keeps the newest three.
- 7b. An answer does not have the requested structure, or its request ended with an error:
  1. The system shows why the answer was refused and the start of what the model wrote.
- 8a. The Manager wants a model of the run as the default AI model:
  1. The Manager sets it from the request (UC-009, step 8).

### Special Requirements / Business Rules

| Step | Rule |
| --- | --- |
| 2 | The benchmark service delivers a finished run with the service token. The system stores it once, under its run id: a repeated delivery stores nothing new, and a delivery with an unknown schema version or an invalid field stores nothing (ADR-0013) |
| 2, 4 | Each run names the machine it ran on, because the time depends on the hardware (US-001.02) |
| 5 | A request is cold when it is the first request of a model in its run, which may include loading the model; the later requests are warm. The means take only the requests that ended without an error |
| 5, 7 | No model is ranked. "Valid" says only that an answer had the requested structure. The rates that the benchmark service calculates from the dataset are not delivered, so they show as n/a |
| 6 | The memory is what the whole machine had in use, not what the model alone used |
| 7, 8 | The answer texts are not part of the list or of the run; each answer is read on its own (ADR-0013) |
| 9 | The download holds every request with its measurements and its outcome, without the answer texts |
| all | Only the Manager holds `Benchmark.View`. The role is checked where the runs are served and again where the function is offered; the first is the authority (ADR-0005) |

### Open Issues

- US-001.02 names the Owner (S01) as the actor of this goal; the back office gives it to the role Manager, and UCD-001, amended on 2026-11-14, names Hotel staff (Manager), the role through which the Owner works. Whether US-001.02 changes is for S01 to decide.
- US-001.02 asks that models that miss a quality threshold or a time limit are marked. The system marks none and ranks none; the threshold and the limit are for the Product Owner (S04) to set.
- The benchmark service does not yet deliver the rates it calculates from the dataset (action item of RC-111), so an answer cannot be checked against them.
- The Dictionary and the Domain Model have no benchmark terms yet (action item of RC-105).

---

[UCD-001]: ../../use-case-diagram.md
[US-001]: ../../user-stories.md
[SA-001]: ../../stakeholder-analysis.md
[UC-004]: ../uc-004/uc.md
[UC-009]: ../uc-009/uc.md
[MIL-019]: ../../milestones/mil-019-benchmark-results-api.md
[ADR-0005]: ../../adr/adr-0005-backoffice-login.md
[ADR-0013]: ../../adr/adr-0013-benchmark-results.md
[50f850d]: https://github.com/NF-Hotel/NFHotel/commit/50f850d890371ba0759cbba802c27c9b31cf5b69
