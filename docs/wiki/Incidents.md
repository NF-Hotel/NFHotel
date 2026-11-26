# Incidents

**English**

This page says what the team does when NF Hotel is down, slow or attacked, how it writes up what happened afterwards, and what the logs may and may not contain.

**For:** anyone in the team (S01) who answers an alert in Discord, and the owner of the Proxmox server (S03) when the node is involved.
**Work:** MIL-036 task 4; MIL-084 for a report from outside. **Related:** [Monitoring](Monitoring) (the alerts and their first steps), [Host Hardening](Host-Hardening) (console recovery), [CI-CD](CI-CD) (roll back a release), [Public Access](Public-Access#the-securitytxt) (the `security.txt` of the site).

**Contents**
1. [What counts as an incident](#what-counts-as-an-incident)
2. [Step by step: handle an incident](#step-by-step-handle-an-incident)
3. [A vulnerability report from outside](#a-vulnerability-report-from-outside)
4. [When personal data may have leaked](#when-personal-data-may-have-leaked)
5. [Write the postmortem](#write-the-postmortem)
6. [Log policy](#log-policy)
7. [Postmortems](#postmortems)
8. [Why it is done this way](#why-it-is-done-this-way)

---

## What counts as an incident

An incident is anything that stops guests from booking, stops the staff from working, or may expose data. Every `critical` alert in Discord is one until it is shown not to be. A `warning` is one only when it lasts longer than its first step in the [runbook](Monitoring#runbook-when-an-alert-arrives) takes to fix it.

| Severity | Means | Examples | Answer within |
| --- | --- | --- | --- |
| SEV1 | Guests cannot book, or data may have leaked | `SiteDown`, `ApplicationErrors` above 50 %, an unknown sign-in to the VM | At once, whoever sees it first |
| SEV2 | The site works but badly, or a safety net is gone | `ApplicationSlow`, `BackupStale`, `SshLoginFailures`, `StaffSignInFailures` | The same day |
| SEV3 | Nothing is broken yet | `CertificateExpiresSoon`, `DiskAlmostFull`, a new finding of the image scan | Within the week, as an issue |

## Step by step: handle an incident

1. **Claim it.** Answer the alert in Discord #nf-hotel-alerts with "I have it" and the severity. *Why:* two people fixing the same thing at the same time undo each other's work, and the others then know they can stay away.
2. **Open an issue** titled `Incident: <what is broken>` with the label `incident`, and paste the alert. Write each step you take as a comment, with the time. *Why:* the comments are the timeline of the postmortem; nobody remembers the times afterwards.
3. **Stop the harm first, find the cause later.** In order of preference:
   - the last release broke it: `sudo /usr/local/sbin/nfhotel-deploy rollback` on `nf-hotel` ([CI-CD](CI-CD#roll-back));
   - one container is unhealthy: find it with `sudo docker ps -a`, then `sudo docker restart <container>`;
   - an attack: close the door (take the site off by stopping the nginx container, `sudo docker stop <nginx container>`) and keep the logs;
   - the VM or the node is gone: follow the console recovery in [Host Hardening](Host-Hardening).

   *Why:* guests waiting while the team reads logs is the costliest part of an incident; a rollback switches to the release before, which is kept on the VM, and takes seconds.
4. **Keep the evidence.** Before a restart or a rebuild of a suspected attack, save `sudo docker logs <container> > ~/incident-<date>-<container>.log` and note the time window for Loki. *Why:* a restart throws the container log away, and Loki keeps only 14 days.
5. **Check that it is fixed** the way the alert checks it: wait for *RESOLVED* in Discord, and open the site yourself. *Why:* the alert is the definition of "working"; a fix that the alert does not see is not proven.
6. **Close the issue** with one line on the cause and a link to the postmortem (SEV1 and SEV2), or the fix (SEV3).

## A vulnerability report from outside

`SECURITY.md` and the `security.txt` of the site ask someone who finds a weakness to report it privately: through the report form of GitHub (private vulnerability reporting), or by e-mail to the mailbox that the file names second. This is how the team keeps what the policy promises (MIL-084, [ADR-0048](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0048-where-security-txt-is-served.md)). A report is not an incident by itself; step 5 says when it becomes one.

1. **Who reads, and how often.** S01 gets an e-mail from GitHub for each new private report, and reads the mailbox at least twice a week; the confirmation is due within 5 working days. GitHub sends that e-mail only to an administrator who watches the repository with **Security alerts** (or **All Activity**) and has e-mail turned on for watching, so S01 keeps both settings (**Watch → Custom** on the repository, and **Settings → Notifications** of the account). *Why:* the policy promises an answer in 5 working days, and a report that nobody sees breaks the promise without anyone noticing.
2. **Answer first.** Thank the finder and confirm the report, in Danish or in English. A report through the form shows under **Security and quality → Advisories** of the repository, with the status *Triage*: answer there with an ordinary comment, which the finder sees; a confidential comment is for the team only. Ask politely for what is missing. Ask for nothing to be signed. To a demand for payment before the details: no reward is paid, and the form is open to everyone. *Why:* a quick, polite answer keeps the finder on the private channel; a finder who hears nothing may publish.
3. **The report is untrusted.** Open no attachment and follow no link of a report on a machine that is signed in to the accounts of the project. Reproduce a proof of concept in the development environment or in a throwaway container, never against production and never with real data. *Why:* a report can itself be phishing, or a script that attacks whoever runs it.
4. **Check and rate.** Reproduce the weakness. Then **Accept and open as draft** (for a report by e-mail, **New draft security advisory** on the same page), and rate it with the CVSS calculator of the draft. The rating gives the time of DSA-001: a fix within 7 days for a critical weakness and within 30 days for a high one. A report that is no weakness is closed with **Close security advisory** and a comment that says why. *Why:* the score is a measure that the finder and the advisory databases share, and it sets the deadline instead of a feeling.
5. **Is it an incident?** If the report shows that data was reached, or that the weakness is being used, follow [Step by step: handle an incident](#step-by-step-handle-an-incident), and [When personal data may have leaked](#when-personal-data-may-have-leaked), with its 72 hours, when personal data may have leaked. *Why:* a weakness that someone used is harm that has happened, not only a risk, and the 72 hours run from when the team knew.
6. **Fix in private.** The repository is public, so an ordinary pull request shows the weakness before the fix is live. Prepare the fix in the temporary private fork of the draft advisory (**Start a temporary private fork**). No status check runs on a pull request there, and GitHub does not enforce the protection rules of `main` when the advisory merges it: run the tests on your own machine before the merge, and see the checks of the merge commit on `main` turn green before S01 approves the CD run. When the checks must run before the merge, open an ordinary pull request with a neutral title instead, and deploy as soon as it is merged. S01 merges and approves the CD run. *Why:* the time between a public fix and its deploy is the time in which anyone can read how to attack the live site.
7. **Tell the finder, then publish.** Tell the finder when the fix is live. Publish the advisory on the agreed date. When others may run the affected code, ask GitHub for a CVE through the advisory before it is published. Credit the finder only with their consent: GitHub shows a credit only after the person accepts it. *Why:* the advisory tells the users of the code what to update, and a finder who is treated fairly reports again.
8. **Afterwards.** Add a regression test that fails without the fix. A weakness in the service of another party (a library, Cloudflare, GitHub) is passed on to that party, and the finder is told. *Why:* a weakness that came back would be found by the next finder, and the other party's users have the same weakness.
9. **Junk.** Output of a scanner without a proof, and reports outside the scope of `SECURITY.md`, get one short answer that points to the policy, and are closed. *Why:* such reports are many and cheap to send; one polite answer is enough.

## When personal data may have leaked

NF Hotel holds the names, e-mail addresses, phone numbers and passport numbers of guests. If any of it may have reached someone it should not (a stolen database copy, a backup on a lost laptop, guest data in a public log), it is a personal data breach under GDPR article 33:

1. Treat it as SEV1, and tell S01 the same day.
2. Write down what data, about how many guests, since when, and how it got out.
3. S01 decides with the hotel whether to notify Datatilsynet, **within 72 hours** of learning about it, and whether to tell the guests (article 34). When in doubt, notify.
4. Rotate every secret that may have been read (database passwords, the JWT key, tokens): write the new value in `/etc/nfhotel` on `nf-hotel` and redeploy, and revoke the old token where it was issued.

*Why:* the 72 hours run from when the team knew, not from when it finished investigating; a late notification is a breach of its own.

## Write the postmortem

Every SEV1 and SEV2 gets a postmortem within a week, as a comment on the incident issue or a page linked from it. It is blameless: it names what the system allowed, not who made the mistake. *Why:* people hide mistakes they are blamed for, and a hidden mistake happens again.

Copy this template:

```markdown
## Postmortem: <title>

**Severity:** SEV1 / SEV2   **Duration:** <start> to <end> (UTC)   **Written by:** <name>

### Summary
Two or three sentences: what guests or staff saw, and for how long.

### Timeline (UTC)
- hh:mm alert <name> fired
- hh:mm claimed by <name>
- hh:mm <what was done>
- hh:mm resolved

### Cause
What broke, and why the system let it break. Ask "why" until the answer is something the team can change.

### What went well, and what did not
- Detection: did an alert find it, or a guest?
- Response: what slowed the fix down?

### Actions
- [ ] <change that stops it from happening again> (issue #N)
- [ ] <change that finds it sooner> (issue #N)
```

Each action becomes an issue; the postmortem is done when they are planned, not when they are written.

## Log policy

The logs of every container and of the VM's journal go to Loki and are kept 14 days ([ADR-0020](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0020-monitoring-stack.md)). Everyone with Grafana access can read them, so they hold what is needed to find a fault and nothing more.

| May be logged | Must never be logged |
| --- | --- |
| The time, the route, the status code, the duration | A guest's name, e-mail address, phone number or passport number |
| Ids (booking id, room id, staff user id) | A password, a token, a cookie or an `Authorization` header |
| The type and message of an exception | A request or response body |
| The client address in the nginx access log | The connection string or any other secret |
| In the security events of the booking flow (category `NFHotel.Security`): the caller address ([ADR-0051](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0051-limits-of-the-booking-flow-and-the-caller-address.md) decision 6) | An e-mail address in any form, whole, masked or hashed; the booking reference; the token of a verification link; any other field that a guest filled in |

How it is enforced:

- **Code:** log an id, never the entity. Write `logger.LogWarning("Booking {BookingId} could not be saved", booking.Id)`, never the guest or the DTO. *Why:* an object written to the log carries every field it has, including the ones added later.
- **CI:** after the end-to-end tests, `ops/scripts/check-log-privacy.sh` reads the logs of every container of the throwaway stack and fails the run if the test guest's e-mail address, phone number or last name appears in one. It names the container but does not print the line. *Why:* the end-to-end tests book like a real guest, so a log line that leaks their data would leak a real guest's data in production.
- **Retention:** 14 days in Loki, then deleted. The backups hold the database, not the logs.

If guest data is found in a log anyway, it is an incident: fix the code, then delete the lines from Loki (`/loki/api/v1/delete` with the time window), and judge it under [When personal data may have leaked](#when-personal-data-may-have-leaked).

## Postmortems

### Benchmark runs could not store their results (2026-10-20 to 2026-10-26)

- **Summary:** every benchmark run since 2026-10-20 ended `failed` with `[Errno 13] Permission denied: '/data/results.jsonl'`, so no result was stored. The container stayed healthy, and the back office only said that no run had been stored. MIL-043 made the failure visible on 2026-10-26.
- **Cause:** the volume `benchmark_results` was created on 2026-10-20 by an earlier benchmark image whose user had uid 1000. The image of MIL-025 runs as `app`, uid 10001. Docker sets the owner of a named volume only when it creates it (and copies the image's folder into it only while it is empty), so the new user could not write to the old volume.
- **Fix on the day:** `chown -R 10001:10001` of the volume, with the benchmark image as root.
- **Lasting fix (MIL-045):** the one-shot service `results-owner` in `ops/deploy/benchmark/docker-compose.yml` gives `/data` to the image's user at every start, before the service starts; the healthcheck also writes to `/data`, so a volume the service cannot write fails the deploy. `ops/tests/test-benchmark-volume.sh` proves both in the ops CI.
- **Lesson:** a healthcheck should prove what the service needs to do its job, not only that it answers.

## Why it is done this way

- **An issue per incident, not a chat thread.** Discord scrolls away and cannot be linked from a commit; an issue keeps the timeline, closes with the fix, and is found again when the same thing happens.
- **Roll back before debugging.** Every deploy keeps the previous release and the rollback is drilled (MIL-021), so rolling back is one tested action, while a fix under pressure is new, untested code.
- **Blameless postmortems.** The team is small and everyone deploys; the useful question is which check would have caught it.
- **A log check in CI, not only a rule in a wiki.** A rule nobody checks is broken by the first hurried change; the end-to-end run already books as a guest, so checking its logs costs seconds.
