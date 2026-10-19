# Security Policy

## Supported versions

Only the release that runs on the NF Hotel server, built from the latest commit of `main`, receives security fixes.

## Reporting a vulnerability

Do not open a public issue for a security problem.

Report it privately through GitHub. Open the **Security** tab of this repository and choose **Report a vulnerability**. If you cannot use that, contact a maintainer of the `NF-Hotel` organization directly.

Please include:

- what is affected (a page, an endpoint, a container, a workflow);
- the steps to reproduce it, or a proof of concept;
- the impact you expect.

We confirm a report within 5 working days and agree a disclosure date with you. A fix for a critical issue is aimed at within 7 days, and for a high issue within 30 days. These are the thresholds of the quality gates in [DSA-001](docs/sqa/devsecops-assessment.md).

## Secrets

Settings files with secrets (`deploy/*/*.env`, `deploy/nginx/tls/`) are git-ignored and stay on the server. If a secret is ever committed or exposed, it is rotated at once; removing it from git alone is not enough.
