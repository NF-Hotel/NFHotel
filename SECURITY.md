# Security Policy

## Supported versions

Only the release that runs on the NF Hotel server, built from the latest commit of `main`, receives security fixes.

## Scope

This policy covers:

- the public site `https://nfhotel.devmatiker.com`: the guest pages, the booking and the sign-in pages;
- the code and the workflows of this repository.

It does not cover:

- the services of other parties that NF Hotel uses, such as GitHub, Cloudflare and Tailscale: report a weakness in one
  of them to that party;
- anything that cannot be reached from the internet;
- other people's systems, also when they link to NF Hotel.

The contact and this policy are published for the site at
<https://nfhotel.devmatiker.com/.well-known/security.txt> (RFC 9116). That file speaks for that name only.

## Reporting a vulnerability

Do not open a public issue for a security problem.

Report it privately through GitHub: <https://github.com/NF-Hotel/NFHotel/security/advisories/new> (the **Security**
tab, **Report a vulnerability**). The form needs a GitHub account; the report travels over HTTPS, and only the
maintainers see it.

If you cannot use the form, write to <mailto:mikr71394@edu.ucl.dk> and ask for a private way to send the details.
Please do not put the details of a weakness into a plain e-mail: it is not encrypted.

You can write in Danish or English. You do not have to give your name: a report under a pseudonym is handled in the
same way.

Please include:

- what is affected (a page, an endpoint, a container, a workflow);
- the steps to reproduce it, or a proof of concept;
- the impact you expect;
- whether you plan to publish what you found, and when.

## What this policy allows, and what it does not

This policy invites you to report what you find. It is not a permission to test: neither this policy nor the
`security.txt` of the site authorises access to our systems or data, and neither replaces the law or a written
agreement with us (RFC 9116, section 5.5). A penetration test needs our written agreement beforehand.

If you look into a weakness in good faith:

- use only your own bookings and accounts, or test data that you created yourself;
- stop as soon as you can show that the weakness exists, and report it;
- keep what you found to yourself until we have agreed a date of publication with you.

Do not:

- slow down or interrupt the service: no denial of service, no load or stress tests, no scans that send many
  requests;
- destroy, change or delete data, and place no code or files on our systems;
- read, copy or keep data of guests, staff or anybody else; if you come across such data, stop, and tell us what you
  saw without keeping it;
- try to get into the back office, the server or other people's accounts: no guessed passwords, no phishing or other
  social engineering, no physical access;
- use a weakness further than is needed to show that it exists.

## What we do not handle as a vulnerability

- the output of a scanner without a weakness that you have shown to work;
- a missing header, flag or DNS record without a way to use it against the site or its guests;
- a version number that the site shows, without a weakness in that version;
- anything that needs physical access, or a device or an account that is already taken over;
- weaknesses in the services of other parties (see Scope).

## What happens to a report

- We confirm a report within 5 working days and tell you whether we could reproduce it.
- We write to you again when the state of the report changes, and at least every 30 days until it is closed.
- A fix for a critical issue is aimed at within 7 days, and for a high issue within 30 days. These are the thresholds
  of the quality gates in [DSA-001](docs/sqa/devsecops-assessment.md).
- We agree a date of publication with you: normally the day the fix is released, and normally within 90 days of your
  report. Please do not publish before it. If we need more time, we ask you and say why.
- When the fix is released we tell you. For a weakness in the code of this repository we publish a security advisory
  on GitHub, and we name you in it if you want that.
- We keep your name and your contact details to ourselves and use them only to handle the report.
- We do not ask you to sign anything.
- We pay no reward for reports.

## Secrets

Settings files with secrets (`ops/deploy/*/*.env`, `ops/deploy/nginx/tls/`) are git-ignored and stay on the server. If a secret is ever committed or exposed, it is rotated at once; removing it from git alone is not enough.
