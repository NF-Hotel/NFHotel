# Secret files of the monitoring (never in git)

`scripts/deploy-monitoring.sh` expects these files in `ops/deploy/monitoring/secrets/`, one
value on one line, mode 0600. With `--apply` it gives each file to the user of the container
that reads it, mode 0400 (wiki page Monitoring, step C4):

| File | Value |
| --- | --- |
| `discord-webhook-url` | The webhook URL of the Discord channel `#nf-hotel-alerts` |
| `healthchecks-watchdog-url` | The ping URL of the Healthchecks.io check "NF Hotel monitoring watchdog" |
| `grafana-admin-password` | The password of the Grafana user `admin`, at least 16 characters |
| `github-scan-token` | Optional (MIL-053). A fine-grained GitHub token (`github_pat_...`) of S01 for the repository `NF-Hotel/NFHotel` only, read-only: Code scanning alerts, Actions and Metadata, with an expiry date. Without it the scan exporter reads no code scanning alerts, and the deploy makes the file empty |

Anyone who knows the webhook URL can post in the channel, so treat it as a password.
