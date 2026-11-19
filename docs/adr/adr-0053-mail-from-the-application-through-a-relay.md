# ADR-0053 Mail from the application through a relay

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0053 |
| CrossReference | [MIL-088], [MIL-035], [MIL-027], [MIL-064], [ADR-0004], [ADR-0006], [ADR-0023], [ADR-0024], [ADR-0034], [ADR-0035], [ADR-0047], [ADR-0051], [ADR-0052], [BC-001], [RC-432], [RC-434] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-19 | Accepted | mikemike | AI as stand-in for S01 | Initial version; task 2 of MIL-088, written by the AI before any code<br>Reworked after the review by a second AI: Go with conditions in RC-432 (a network of its own for the forwarder, one source for the address of the relay, the risks of a private mailbox and of the fuse, the claims scoped), then Go with conditions in RC-434 (the forwarder on IPv4 and given only the host and the port, the catcher with TLS from the first byte, the two limits of Gmail), conditions closed; accepted by the AI as stand-in for S01 in autonomous mode, at S01's word in chat on 2026-11-19 ("Du skal tage beslutningerne for mig") | [baaf79b] |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

**Who decides.** [MIL-088] decision 8 and task 2 ask this record to fix the port and its adapter, the library and why,
the settings and the secret file, the encrypted connection, what happens when the relay is down, the fuse, the mail
catcher of the stacks, and what S01 has to bring, with the alternatives and the reason each lost. [ADR-0052] decides
which mails are sent and when. S01 handed the decisions to the artificial intelligence (AI) that builds the milestone
("Du skal tage beslutningerne for mig") and chose the relay in the chat of the planning session on 2026-11-19: the
mail service of Gmail with a Google account of S01 ("Kan du bruge min Google e-mail"). The planning session named three
drawbacks of a private account: the secret opens that mailbox, guests see the address and their replies land there,
and a change of the password of the account revokes the secret. S01 chose the private account all the same ("2. Men du
giver mig guide til opsætningen"). The review of this record found two more drawbacks that S01 has not heard; they are
in the consequences, and the choice of the account stays S01's in task 9 of [MIL-088].

**The problem**, as the code after [MIL-087] has it (read on 2026-11-19 at 6f7bb80):

- The application cannot send mail: no code, no package and no setting for it. The alerts reach a chat channel
  through Alertmanager, not mail.
- The API is on three internal networks, `nfhotel-api`, `nfhotel-db` and `nfhotel-python` (`docker network create
  --internal`), so it has no route out of the virtual machine (VM), and nothing it could send would reach a relay.
  `nfhotel-api` also carries nginx and the web application (the wiki page Network-Isolation), so a service put on it
  would be reachable from nginx too.
- The secrets of the API are files in `/run/secrets`, written from `nfhotel-api.env` by `write-secret-files.sh` and
  read with `AddKeyPerFile` ([ADR-0024]).
- Production has no relay: only S01 can make the secret of the account. The AI opens no account and sees no
  password.

**What the texts say** (read on 2026-11-19):

- Google, "Send email from a printer, scanner, or app": "The fully qualified domain name of the SMTP service is
  smtp.gmail.com"; port 465 for SSL and port 587 for TLS; the device signs in with the account and an app password.
- Gmail Help, "Limits for sending & getting mail": it names two limits. An account that sends "more than 500 emails"
  in a day is limited, and may send again within 1 to 24 hours. An account that sends a large number of mails that
  bounce or cannot be delivered because the address is invalid is limited too, and may send again after 24 hours; the
  page gives no number for that.
- Google, "Sign in with app passwords": an app password needs 2-Step Verification; "App passwords aren't
  recommended"; Google revokes the app passwords when the password of the account changes.
- Microsoft, the remarks of `System.Net.Mail.SmtpClient`: it is not recommended for new development; "Use MailKit or
  other libraries instead".
- MailKit (the README of the project): MIT licence, a project of the .NET Foundation; its client of the Simple Mail
  Transfer Protocol (SMTP) connects with Transport Layer Security (TLS; SSL is its old name) at once (`SslOnConnect`)
  or upgrades a plain connection with the command STARTTLS (`StartTls`).
- HAProxy, the documentation of the server option `resolve-prefer`: when a name has addresses of both kinds,
  HAProxy takes the one of Internet Protocol version 6 (IPv6) unless the option says `ipv4`.
- Mailpit, its options: `--smtp-require-tls` makes the server speak TLS from the first byte (since v1.15.0), and
  `--smtp-require-starttls` asks for STARTTLS instead.
- The Application Security Verification Standard (ASVS) 5.0 of the Open Worldwide Application Security Project
  (OWASP): 1.3.11 (input is made safe before it reaches a mail
  system), 12.3.1 (TLS for outbound connections, with no fall back to plain text), 12.3.2 (a TLS client checks the
  certificate of the server), 12.3.4 (where a certificate is made in-house, only that certificate is trusted), 13.3.1
  (secrets in a secrets-management solution), 16.3.4 (failures of a control, such as a failed TLS connection to a back
  end, are logged), 16.5.2 (the application stays secure when an external resource fails).

**Forces.**

- Mail is a small part of the flow: about one mail per request and one per booking, a few dozen a day.
- The repository is public: no address of the account, no password and no name of the host of production in it.
- The API should keep no route out; one more open way out of the VM is one more way for a compromised API to reach the
  internet.
- Gmail limits an account that sends more than 500 mails a day, and an account whose mails bounce in large numbers; a
  flood of requests must not use up the account that S01 also uses privately.
- A test must not need the internet or an account: the browser tests read the mail from a catcher.

**Options considered:**

- **A. The web interface of a mail provider**, over the Hypertext Transfer Protocol (HTTP) (an interface of a
  sending service, or the interface of Gmail with
  OAuth, the protocol that lets an application act for an account with a token instead of its password). Fine for
  volume and tracking, but it needs an account at a provider that S01 does not have, or a refresh token of OAuth that
  S01 must make and renew; the code would follow one provider. Lost.
- **B. A mail server on the VM** (Postfix sending straight to the mail servers of the receivers). Needs a domain with
  its records for the sender policy (SPF, Sender Policy Framework), the signature (DKIM, DomainKeys Identified Mail)
  and the reporting policy (DMARC, Domain-based Message Authentication, Reporting and Conformance), a reverse name of
  the address, and a reputation that an address of a home connection does not have; the mails would land in spam.
  Lost.
- **C. `System.Net.Mail.SmtpClient`** of .NET. No package, but Microsoft recommends against it for new development.
  Lost.
- **D. SMTP over an encrypted connection with MailKit**, behind a port of the application layer, to any relay with a
  user name and a password. Chosen; Gmail is the relay of production at S01's choice, and nothing of Gmail is in the
  code.

**Options for the way out of the VM:**

- **E. The API on a network with a route out.** One line in a compose file, but it opens every host and every port to
  the API. Lost.
- **F. A rule of the host firewall** that lets the API reach the ports of the relay only. The addresses of Gmail change,
  so the rule would open the port to every host; the firewall of the VM is not where the containers are described.
  Lost.
- **G. A forwarder.** HAProxy in mode TCP (Transmission Control Protocol: it passes the bytes of a connection on,
  without reading them), the image that the database relay of the team pins already ([ADR-0034]), forwards one port to
  the host and port of the relay, and finds the address of the relay by its name again every few seconds. The TLS of
  the mail runs from the API to the relay through it, so the forwarder sees only encrypted bytes and holds no secret.
  Chosen.

## Decision

The API sends its mails with SMTP over TLS to a relay, through MailKit behind a port of the application layer, and
reaches the relay only through a forwarder on a network that it shares with the API alone (options D and G). In
detail:

**1. The port and its adapter.** `IMailSender` in the application layer takes one mail (the address it goes to, the
subject and the text) and answers whether the relay took it, with the reason in a word when it did not; the use cases
of [ADR-0052] build the two mails. The adapter `SmtpMailSender` in the infrastructure uses MailKit 5.0.0 (released on
2026-11-10, after the cooldown of 7 days of the repository; it brings MimeKit.Core and System.Formats.Asn1, and not
the cryptographic library BouncyCastle that MailKit 4 brings), pinned in the lock files. A mail is plain text in
UTF-8, the Unicode encoding of eight-bit units. The adapter refuses an address or a subject with a line break before
it builds anything (ASVS 1.3.11), and a send that takes more than 30 seconds fails.

**2. The settings**, the section `Smtp` of the API:

| Setting | Meaning |
| --- | --- |
| `Smtp:Host` | The host name of the relay, for the connection and for the check of its certificate |
| `Smtp:Port` | Its port: 465 with `SslOnConnect`, 587 with `StartTls` |
| `Smtp:Security` | `SslOnConnect` or `StartTls` |
| `Smtp:UserName` | The account at the relay |
| `Smtp:Password` | Its password: a secret file, `Smtp__Password` in `nfhotel-api.env` ([ADR-0024]) |
| `Smtp:FromAddress` | The address that the mails come from |
| `Smtp:FromName` | The name that the mails come from; `NF Hotel` by default |
| `Smtp:Forwarder` | The host and port of the forwarder of decision 4, `mail-relay:1465`; empty when the API reaches the relay itself |
| `Smtp:CertificateThumbprint` | Empty in production. The SHA-256 hash (Secure Hash Algorithm, 256 bits) of the one certificate to trust instead of the store of the system, for the catcher of the stacks: 64 lower-case hex digits over its DER form |
| `Smtp:MaxMailsPerDay` | The fuse of decision 6: 400 by default |

A value of `Smtp:Security` other than the two, `None` included, stops every start of the API, whatever else is set.
With the verification of [ADR-0052] on, the API also refuses to start, and names the setting, when one of host, port,
user name, password or sender address is missing. The address of the account is a setting of the VM; the example
files of the repository use `example.com`.

**3. The encrypted connection** to the relay: TLS at once on port 465, or STARTTLS on 587, and no fall back to plain
text: with `StartTls` MailKit refuses a relay that does not offer STARTTLS (ASVS 12.3.1 for this connection). The
certificate of the relay is checked against the store of the system and the host name (ASVS 12.3.2). Only where
`Smtp:CertificateThumbprint` is set, in the stacks of decision 7, is the one certificate with that SHA-256 trusted
instead (ASVS 12.3.4). The connections inside the VM that [ADR-0006] and [ADR-0023] leave as plain HTTP stay as they
are; this record covers the way of the mail.

**4. The way out.** The forwarder `mail-relay` (folder `ops/deploy/mail-relay`) is HAProxy 3.2 in mode TCP, the
image of [ADR-0034] pinned by digest, with the hardening of the other services. Its two networks:

- `nfhotel-mail`, internal, which only the API and the forwarder join; the API reaches the forwarder at
  `mail-relay:1465`, and neither nginx nor the web application can;
- `nfhotel-mail-out`, which only the forwarder joins, with a route out of the VM.

It forwards to the host and port of the relay and finds the address of that host again through the name server of
Docker every 10 seconds, as the database relay of [ADR-0034] finds PostgreSQL, and it takes the IPv4 address of the
host (`resolve-prefer ipv4`): Docker on the VM has no IPv6 (`ops/deploy/host/docker/daemon.json`), and the relay of
Gmail has IPv6 addresses too, which HAProxy would take otherwise. The host and the port come from `Smtp__Host` and
`Smtp__Port` of `nfhotel-api.env`, the settings that the API checks the certificate against, so the forwarder and
the API cannot disagree about the relay; there is no other file for them. `deploy-services.sh` reads the two values
from that file and passes only them to the forwarder, as the variables of its compose file: the forwarder never gets
the file, which holds every secret of the API. `deploy-services.sh` starts the forwarder when `Smtp__Forwarder` is
set, and refuses the deploy, naming the setting, when `Smtp__Forwarder` is set
without `Smtp__Host` or `Smtp__Port`, or when `BookingVerification__Enabled` is true without `Smtp__Forwarder`: the API
on its internal networks could reach no relay. The API opens the connection to the forwarder and runs TLS with the host
name of the relay, so the check of the certificate is the same as without the forwarder. The API stays on internal
networks only.

**5. When the relay does not take a mail.** For the mail of a request, the guest is told that the mail could not be
sent and to try again later, the request is removed, and nothing is booked (ASVS 16.5.2). The API logs the event
`mail_not_sent` with the kind of the mail and the reason in a word, never the address, and counts it; the alert
`MailNotSent` fires on the first one in 10 minutes (ASVS 16.3.4). A second mail of [ADR-0052] decision 5 that fails is
logged and counted the same way and does not undo the booking.

**6. The fuse.** At most 400 mails in 24 hours, counted in the database from the records of the mails of [ADR-0052]
decision 6, under the same lock as the limits per mailbox, with the record written before the mail is sent, so a burst
of requests cannot pass the fuse. At 400 a new request is refused with 503 `booking.mail_paused`, and the guest is
told to try again later or to call the hotel; a second mail over the fuse is not sent. The API logs and counts the
event `mail_fuse_blown`. The alert `MailVolumeHigh` fires at 200 mails in 24 hours, and `MailFuseBlown` when the fuse
refuses. [MIL-088] planned a fuse at 1,000; Gmail limits the account at 500 mails a day, so the fuse is 400, with the
alert at 200 as planned. A relay with a higher limit can raise the setting.

**7. The mail catcher.** Mailpit (`axllent/mailpit` v1.33.1, pinned by digest; v1.34.0 was two days old) takes the
mail in the throwaway stack of the end-to-end tests ([MIL-027]) and in the light development mode ([ADR-0035]), never
on the VM: its compose file is `ops/deploy/mailpit/docker-compose.yml`, which the stacks start and
`deploy-services.sh` does not know. The daily scan of third-party images ([MIL-064]) reads every compose file of
`ops/deploy` and so scans the image of the catcher too. The catcher speaks TLS from the first byte
(`--smtp-require-tls`) with a certificate that the stack makes, and the stacks set `Smtp:Security` to `SslOnConnect`,
as production does; the API pins that certificate with `Smtp:CertificateThumbprint`. The catcher accepts any user
name and password, and its interface on port 8025 lets the browser tests read the link. The mail goes from the API
through the forwarder of decision 4 to the catcher, so the stack tests the way of production. The tests of the
adapter cover `StartTls`: a server of the test that offers no STARTTLS is refused.

**8. What S01 brings** (task 9 of [MIL-088]), written in the wiki page Mail-Relay, step by step:

- the account: a Google account with 2-Step Verification on. S01 chose the private account; an account made for the
  hotel keeps the guests' mails and the limits of Google away from S01's private mail (see the consequences), and the
  wiki page recommends it. S01 decides before the first step of task 9;
- an app password of that account, made by S01 and typed by S01 into `nfhotel-api.env` on the VM as `Smtp__Password`;
- the other settings of decision 2 and of [ADR-0052] decision 8 in `nfhotel-api.env`, with `Smtp__Host=smtp.gmail.com`,
  `Smtp__Port=465`, `Smtp__Security=SslOnConnect` and `Smtp__Forwarder=mail-relay:1465`;
- a deploy, then `BookingVerification__Enabled=true` and a second deploy, and then the check on the live site of
  [MIL-088] criterion 10.

A Gmail address needs no records in the name system from S01: Google signs the mail of `gmail.com` and publishes its
policies. A sender address of a domain of the hotel would need its records for SPF, DKIM and DMARC, which the wiki
page lists for later. The mails say that a reply is not read.

## Consequences

**Positive:**

- The application can send the mails of [ADR-0052] through any relay with an encrypted connection, a user name and a
  password; changing the relay is a change of settings.
- The API keeps no route out: one port of one forwarder, on a network of the two alone, reaches one relay, and the
  forwarder holds no secret.
- The flow fails closed when the relay fails: no request without its mail, no booking without its request.
- The browser tests run the whole way of production, through the forwarder, without the internet, and the daily scan
  covers the image of the catcher.

**Negative:**

- Whoever reads `Smtp__Password` can read and send from the mailbox of the account. It is a secret file of the API
  only, readable inside a folder that only its owner opens (0444 in 0700, [ADR-0024] decision 4), never in the
  repository or a log; S01 can revoke it in the Google account at any time. ASVS 13.3.1 asks for a secrets-management
  solution; the secret files of [ADR-0024] are not one, and [ADR-0024] names a vault as the next step.
- With the private account, the mails pass through S01's private mailbox: a bounce lands in its inbox, and Gmail may
  keep a copy of each mail sent in the folder Sent. Such a copy holds the email of a guest, the dates, a link that
  works for 30 minutes and the reference of the booking, which with the email opens the booking ([ADR-0047]); it is
  out of reach of the clean-up of [ADR-0052] decision 7. The check of task 9 looks for such copies after the first
  test mail, and the wiki page tells S01 how to delete them; an account made for the hotel keeps them out of S01's
  private mail.
- Guests see the address of the account as the sender, and a reply lands in that mailbox.
- A change of the password of the account revokes the app password: the mails fail, `MailNotSent` fires, and the
  public booking refuses requests until S01 sets a new one.
- At 400 mails in a day the public booking stops taking requests until the window has room again. A sender with 20
  addresses can reach that within a day under the limits per address of [ADR-0051] (20 requests a day each), with 80
  mailboxes under the limits per mailbox; IPv6 addresses are cheap. The alert `MailVolumeHigh` at 200 warns S01 first;
  the answer is a rule of Cloudflare or the puzzle of [ADR-0051], or fewer requests per address.
- Mails to addresses that do not exist bounce, and when many bounce Google may stop the account from sending for 24
  hours; Google gives no number. With the private account that stops S01's own mail too. The fuse counts mails, not
  bounces, so it does not prevent this; an account made for the hotel limits the harm to the mail of the hotel.
- The forwarder is one more container on the VM, and Mailpit one more image in the stacks of development.

Quality attributes: security (no route out for the API, TLS with a checked certificate to the relay, the secret as a
file, ASVS 1.3.11, 12.3.1, 12.3.2 and 12.3.4); reliability (a failure is told, counted and alerted, ASVS 16.3.4 and
16.5.2); flexibility (any SMTP relay); maintainability (the catcher makes the flow testable without the internet).

## Affected Artifacts

- [MIL-088] — tasks 6, 8 and 9 build and use this record.
- [ADR-0052] — its two mails go through the port of this record, and its records of the mails sent feed the fuse.
- [ADR-0024] — one more secret file, `Smtp__Password`, in the list of `write-secret-files.sh`.
- [ADR-0034] — its HAProxy image serves a second forwarder.
- [ADR-0035] — the light development mode runs the catcher and the forwarder.
- [ADR-0047] — a reference of a booking now travels by mail too.
- [ADR-0051] — its limits per address are what bounds the use of the fuse.
- [MIL-027] — the throwaway stack of the end-to-end tests runs the catcher and the forwarder.
- [MIL-064] — the daily scan covers the image of the catcher.

Task 8 of [MIL-088] updates the wiki pages Environment-Files, Security-Controls, Alerts, Containers-and-Images and
Network-Isolation with the check `ops/tests/test-service-networks.sh`, adds the page Mail-Relay, and adds the alerts
to the alert rules of the monitoring with their rule tests.

---

[MIL-088]: ../milestones/mil-088-a-booking-verified-by-email.md
[MIL-035]: ../milestones/mil-035-secrets-as-files.md
[MIL-027]: ../milestones/mil-027-dast-and-end-to-end-tests.md
[MIL-064]: ../milestones/mil-064-patched-nginx-and-third-party-scan.md
[ADR-0004]: ./adr-0004-database-api.md
[ADR-0006]: ./adr-0006-tls.md
[ADR-0023]: ./adr-0023-application-telemetry.md
[ADR-0024]: ./adr-0024-secrets-as-files.md
[ADR-0034]: ./adr-0034-team-database-access.md
[ADR-0035]: ./adr-0035-light-local-development-stack.md
[ADR-0047]: ./adr-0047-guest-login.md
[ADR-0051]: ./adr-0051-limits-of-the-booking-flow-and-the-caller-address.md
[ADR-0052]: ./adr-0052-a-booking-takes-a-room-once-its-email-is-verified.md
[BC-001]: ../business-case.md
[RC-432]: ../sqa/reviews/rc-432-adr-0053.md
[RC-434]: ../sqa/reviews/rc-434-adr-0053-second-round.md
[baaf79b]: https://github.com/NF-Hotel/NFHotel/commit/baaf79babbf7d3ff7c52b39b60f1c31def2fdccb
