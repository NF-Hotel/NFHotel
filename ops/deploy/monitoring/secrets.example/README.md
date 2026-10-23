# Secret files of the monitoring (never in git)

`scripts/deploy-monitoring.sh` expects these files in `ops/deploy/monitoring/secrets/`, one
value on one line, mode 0600:

| File | Value |
| --- | --- |
| `discord-webhook-url` | The webhook URL of the Discord channel `#nf-hotel-alerts` |
| `healthchecks-watchdog-url` | The ping URL of the Healthchecks.io check "NF Hotel monitoring watchdog" |
| `grafana-admin-password` | The password of the Grafana user `admin`, at least 16 characters |

Anyone who knows the webhook URL can post in the channel, so treat it as a password.
