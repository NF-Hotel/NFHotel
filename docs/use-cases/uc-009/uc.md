# UC-009 Run Time and Quality Benchmark

## Metadata
| Key | Value |
| --- | --- |
| ID | UC-009 |
| CrossReference | [UCD-001], [US-001], [SA-001], [UC-004], [UC-008], [MIL-020], [ADR-0005], [ADR-0013], [ADR-0014], [ADR-0031] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-14 | Accepted | mikemike | AI as stand-in for S01 | Initial version (MIL-020); accepted after RC-370 Go, at S01's word in chat ("Du godkender for mig"); mode autonomous, not independent | pending |

---

**Format:** Fully Dressed

## Fully Dressed

- **Scope:** NFHotel platform, hotel back office
- **Level:** user-goal
- **Primary Actor:** Manager (a member of hotel staff with the role Manager, which holds the permission `Benchmark.Run`)
- **Stakeholders and Interests:**
  - S01 — Team 2, the owner of the hotel: the model choice rests on measured results (US-001.04), and only a Manager starts a run, changes the installed models or sets a default AI model.
  - S02 — Other NFHotel 2.0 guilds: only the back office calls the benchmark service, with the token of that service; a refused call starts nothing and says why.
  - S03 — Proxmox VE administrator: a run loads the server for minutes, so the benchmark service runs one model at a time, keeps a short queue and adds one model at a time.
  - S04 — Valdamar: each run can be traced to the user name, the time and the choice that started it, and each default AI model to the user name and the time that set it.
  - S05 — Hotel staff: the other roles are not offered the function.
- **Preconditions:** The Manager is signed in (UC-004). The benchmark service is deployed, and the back office has its address and its token.
- **Postconditions (success guarantee):** The run is queued in the benchmark service with the Manager's choice and recorded with the user name and the time; the record follows each change of state that the system reads. When the run is done, its result is stored and can be compared (UC-008). A refused start starts nothing.

### Main Success Scenario

1. The Manager asks to start a benchmark run.
2. The system shows the installed models and the runs that the benchmark service knows: queued, running and recent, whoever started them.
3. The Manager may add a model by the name that the model server uses. The system shows the download until the model is installed, and then offers it.
4. The Manager chooses one to four models, the hotel cancellation analysis, the deterministic tasks or both, and one to five runs per model, and starts the run.
5. The system sends the choice to the benchmark service, which queues the run. The system records the run with the Manager's user name, the time and the choice, and shows it as queued.
6. The system follows the run from queued through running to done, and then until its result is stored. A change shows within a few seconds, also a change that another caller made.
7. The system shows the run as stored and leads the Manager to the stored runs (UC-008).
8. The Manager sets the default AI model of the website or of the market analysis, from a request of a stored run or by typing the name. The system saves it with the user name, the time and, when it came from a run, that run.
9. The Manager deletes a stored run that is no longer needed, after a confirmation. The system removes it for good, with its requests and their answers.
10. The Manager deletes an installed model that is no longer needed, after a confirmation.

### Extensions (Alternative / Exception Flows)

- 1a. A Receptionist or Housekeeping user asks to start a benchmark run, or for any other function of this use case:
  1. The system tells them that the role has no access (UC-004, 6a). The hotel system also refuses them the record of a run, the delete of a run and the change of a default AI model.
- 2a. The benchmark service cannot be reached, does not answer in time, or is not set up:
  1. The system says so, shows no model and starts nothing.
- 2b. No model is installed:
  1. The system says so; the Manager adds one (step 3).
- 2c. A run recorded in the last seven days is still queued or running:
  1. The system follows that run with its choice (step 6) and offers no new start until it has ended.
- 2d. A run recorded in the last seven days failed or was interrupted, and no run is being followed:
  1. The system shows that the newest of those runs did not finish, with its reason, and offers its choice again, keeping only the models that are still installed. A start makes a new run.
- 2e. The Manager asks to start a run again from the started runs listed apart (UC-008, step 2):
  1. The system takes the choice of that run, keeping only the models that are still installed. A start makes a new run.
- 3a. The text is not shaped like a model name, the model is already installed, another model is being added, or the download fails:
  1. The system adds nothing. It does not offer to add a text that is not shaped like a model name, and says why in the other cases.
- 5a. The benchmark service cannot be reached, refuses the token, has a full queue, or lacks a chosen model:
  1. The system says why and starts nothing.
- 5b. The run started, but the hotel system could not record it:
  1. The system follows the run all the same and shows why the hotel system did not record it.
- 6a. The run fails, or it is done but its result could not be stored:
  1. The system says so and stops following the run. A failed run is recorded as failed, with its reason; a result that could not be stored stays with the benchmark service.
- 6b. The benchmark service no longer knows the run, because it was restarted:
  1. If the result of the run is stored, the system records the run as done. Otherwise it records the run as interrupted, says that the service was restarted before the run ended, and keeps the choice for a new start.
- 6c. A read of the runs or of the installed models fails:
  1. The system keeps the last state it read, says that the live updates are paused, and reads again.
- 8a. The model is not installed, or the typed name is not a model name or was used by no stored run:
  1. For a model that is not installed, the system adds it first and saves the default when the download is done; if the download fails, the default does not change.
  2. For a name that is not a model name, the system says so and changes nothing. For a name that no stored run used, the system notes that a service uses a model only under the exact name that the model server lists, tag included (phi3.5:latest), and still saves the default.
- 9a. The run is no longer stored:
  1. The system says that the run was not found and keeps the list.
- 10a. A queued or running run uses the model, or the model is a default AI model:
  1. If a run uses the model, the benchmark service refuses the delete, and the system says to delete it when the run is done.
  2. If the model is a default AI model, the system warns before the delete that the default stays set, so that the Manager chooses another default afterwards.

### Special Requirements / Business Rules

| Step | Rule |
| --- | --- |
| all | Only the Manager holds `Benchmark.Run`, `Benchmark.Delete`, `AiDefaults.Manage` and `Benchmark.ModelsManage`. The hotel system checks the record of a run, the delete of a run and the change of a default AI model. The back office checks the start of a run and the change of the models, because it calls the benchmark service itself (ADR-0005, ADR-0013) |
| 4 | A run has one to four models, at least one of the two analyses and one to five runs per model; the benchmark service checks the choice again. A start is offered only when the choice is complete and no run is being followed |
| 5 | Only the back office calls the benchmark service, with the token of that service. The benchmark service and the model server publish no port on the host |
| 5, 6 | The record of a run holds its choice, the user name, the time, and the latest state that the system read, with its reason (ADR-0031) |
| 6 | A run moves from queued to running, done, failed or interrupted, and from running to done, failed or interrupted. An interrupted run becomes done only when its stored result proves that it finished. Done and failed are final; any other change is refused (ADR-0031) |
| 2, 6 | While the function is open, the system reads the runs, the downloads and the installed models again every three seconds. A run without a stored result stays in view for seven days |
| 3 | A model name has the form that the model server uses and at most 100 characters. One model is added at a time |
| 8 | There is one default per purpose. The market analysis takes its default at its next deployment, under the exact name that the model server lists; a default that is not installed is not written (ADR-0014); when the model server does not answer at the deployment, the name is written unchecked. When the installed models cannot be read, the default is saved without the check |
| 9, 10 | A delete is for good and needs a confirmation |

### Open Issues

- US-001.04 names DevOps (S02) as the actor of this goal; UCD-001, amended on 2026-11-14, names DevOps, who runs the service, and Hotel staff (Manager), who starts a run. Setting the default AI model belongs to the UCD-001 goal "Choose Local AI Model" (actor Owner), which has no use case of its own; the back office gives it to the role Manager, and this use case covers the default as far as MIL-020 does. Whether US-001.04 and that goal change is for S01 to decide.
- The website default is stored, but the website makes no AI call, so nothing reads it yet (ADR-0014).
- When only the read of the downloads fails, the system does not say that the live updates are paused (MIL-020, criterion 12).
- The Dictionary and the Domain Model have no benchmark terms yet (action item of RC-105).

---

[UCD-001]: ../../use-case-diagram.md
[US-001]: ../../user-stories.md
[SA-001]: ../../stakeholder-analysis.md
[UC-004]: ../uc-004/uc.md
[UC-008]: ../uc-008/uc.md
[MIL-020]: ../../milestones/mil-020-start-benchmark-run.md
[ADR-0005]: ../../adr/adr-0005-backoffice-login.md
[ADR-0013]: ../../adr/adr-0013-benchmark-results.md
[ADR-0014]: ../../adr/adr-0014-default-ai-model.md
[ADR-0031]: ../../adr/adr-0031-state-that-survives-a-deploy.md
