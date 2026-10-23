# DORA metrics: definitions and the monthly report

How the five DevOps Research and Assessment (DORA) metrics of NF-Hotel/NFHotel are measured (MIL-023 task 5), and how the monthly report is made. The script is [`ops/scripts/dora-metrics.sh`](../../scripts/dora-metrics.sh); its tests and their hand-made month are `ops/tests/test-dora-metrics.sh` and `ops/tests/fixtures/dora/`.

## Data

Everything comes from the GitHub API of NF-Hotel/NFHotel, read-only:

| Data | API | Used for |
| --- | --- | --- |
| Merged pull requests into `main` in the month, with the author date of their first commit and their labels | `pulls`, `pulls/{n}/commits` | Change lead time; hotfixes |
| Commits of `main` (from 31 days before the month) | `commits?sha=main` | The order of the commits, to know which deployment shipped a change |
| Runs of `NF Hotel Release` | `actions/workflows/{id}/runs` | The commit of each release; releases that never reached production |
| Runs of `NF Hotel CD`, and the deployments to the environment `production` with their statuses and job logs | `actions/workflows/{id}/runs`, `deployments`, `deployments/{id}/statuses`, `actions/jobs/{id}/logs` | Deployments, failures and recovery |

**A deployment** is a CD run that reached `production` with the input `apply` true, and that succeeded or failed; a dry run (`apply` false) and a cancelled run are not deployments. The job log gives `release-run-id` and `apply`, which the deploy-release action prints at the start of its step; a run started by `workflow_run` deploys with `apply` true, and the release is the run that triggered it. **The deployed commit** is the head commit of that release run, not the commit of the CD run: a manual run of CD always runs on the head of `main`, whatever release it deploys. Job logs are kept for 90 days, so a month must be fetched within 90 days; a manual run whose log is gone counts as a dry run.

## Definitions

| Metric | Definition here | DORA |
| --- | --- | --- |
| Change lead time | Per pull request merged in the month: from the author date of its first commit to the end of the first successful deployment of a commit at or after its merge commit on `main`. The median, in hours. A pull request not deployed by the end of the window is listed, not counted | Time from code committed to code running in production |
| Deployment frequency | The number of successful deployments in the month; per week, and the number of days with one | How often code is deployed to production |
| Change failure rate | Failed deployments divided by all deployments. A deployment failed when its CD run failed, or when the next successful deployment rolled it back (deployed an older commit of `main`) | Share of deployments that need immediate intervention |
| Failed deployment recovery time | Consecutive failed deployments are one incident. From the end of its first failed deployment to the end of the next successful deployment. The median, in hours; an incident not recovered by the end of the window is counted as open | Time to recover from a failed deployment |
| Deployment rework rate | Unplanned deployments divided by all deployments. A deployment is unplanned when it recovers an incident, or when it ships a pull request labelled `hotfix` | Share of deployments that are unplanned and made because of an incident in production |

Why a failed CD run counts as a failed deployment: the deploy step changes the services on the VM and can fail halfway; the run has then needed an intervention, whether or not production broke. The report lists every deployment, so a reader can see what kind of failure each was.

## The monthly report

On the first working day of a month, for the month before (the token needs read access to the actions and deployments of NFHotel; `gh auth token` of a maintainer is enough):

```bash
bash ops/scripts/dora-metrics.sh fetch --month 2026-10 --out /tmp/dora-2026-10
bash ops/scripts/dora-metrics.sh report --month 2026-10 --data /tmp/dora-2026-10
```

Paste the output into a new report `docs/sqa/dora/dora-NNN-YYYY-MM.md` (the next DORA number in `docs/artifact-registry.md`), with the observations and actions of the month. The data files are not committed: they can be fetched again within the 90 days of the job logs.

## Sources

- DORA, *Accelerate State of DevOps Report 2024* (the five metrics, with deployment rework rate), and [dora.dev/guides/dora-metrics-four-keys](https://dora.dev/guides/dora-metrics-four-keys/).
