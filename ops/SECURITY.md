# Security Policy

## Scope

This repository holds the deployment, the pipeline actions and the server configuration of NF Hotel. A weakness here can reach production, so report it privately.

## Reporting a vulnerability

Do not open a public issue. Open the **Security** tab of this repository and choose **Report a vulnerability**. If you cannot use that, contact a maintainer of the `NF-Hotel` organization directly.

Please include what is affected (an action, a script, a compose file, a setting), the steps to reproduce it, and the impact you expect.

We confirm a report within 5 working days. A fix for a critical issue is aimed at within 7 days, and for a high one within 30 days. These are the thresholds of the quality gates in `docs/sqa/devsecops-assessment.md`.

## Secrets

No secret belongs in this repository. Passwords, keys and connection strings live on the server (`/etc/nfhotel/`, mode 0600), or in GitHub only where a step needs them. If a secret is ever committed, it is rotated at once; removing it from git is not enough.
