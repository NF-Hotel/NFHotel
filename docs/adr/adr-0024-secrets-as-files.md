# ADR-0024 Secrets as files mounted by Compose, written from the settings files at each deploy

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0024 |
| CrossReference | [ADR-0004], [ADR-0011], [ADR-0020], [MIL-035], [RC-185] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-24 | Accepted | mikemike | AI as stand-in for S01 | Initial version; written by the AI in autonomous mode under MIL-035, which S01 accepted in chat on 2026-10-24 | [9090645] |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

**Who wrote what.** S01 asked on 2026-10-24 for the gaps of the DevOps review to be closed. One gap was that the secrets of NF Hotel reach the containers as environment variables. The options and the decision below were written by the AI assistant.

The database password (inside the connection string), the API token, the staff token signing key, the passport encryption key and the first Manager's password were environment variables of the API, the web application and PostgreSQL. An environment variable can be read by anyone who can run `docker inspect`, by any process in the container (`/proc/1/environ`), and by the Docker socket proxy of Alloy, whose read access RC-185 recorded. It is also copied into crash dumps and into the output of diagnostics that print the environment.

The operator writes the secrets in one settings file per service (`ops/deploy/<service>/<service>.env`, git-ignored, 0600, owned by the user `nfhotel`), and the deploy runs as that user, not as root (scripts/deploy-release.sh, ADR-0016). The API and the web application already read `/run/secrets` with `AddKeyPerFile`, and the PostgreSQL image reads `POSTGRES_PASSWORD_FILE`.

Options considered:

1. **Keep environment variables.** Nothing to build, but every reader above keeps the values.
2. **Compose secrets: files mounted at `/run/secrets`.** Each secret is a file on the host, bind-mounted read-only into the container that needs it. It is not in `Config.Env`, not in `/proc/1/environ`, and not visible to the socket proxy. It needs no new service, and both .NET and PostgreSQL read it as they are.
3. **A vault (OpenBao, HashiCorp Vault, Infisical).** Secrets are leased at start, audited and rotated centrally. But the vault itself needs storage, unsealing after each restart of the VM, a backup, and its own secret to log in with; with one VM and one team, it moves the problem rather than removing it.
4. **Docker Swarm secrets.** Encrypted at rest in the Raft log, with owner and mode per secret. But it needs Swarm mode for the whole stack, which changes every compose file, the deploy and the health waits.

## Decision

We follow option 2.

1. **The settings files stay the one place where a secret is written.** The operator keeps writing each secret once, in the settings file of its own service; nothing new to learn and nothing new to back up.
2. **The deploy writes the files.** `scripts/write-secret-files.sh` splits the settings of PostgreSQL, the API and the web application into `secrets/settings.env` (every setting except the secrets) and one file per secret, in `ops/deploy/<service>/secrets/`. `scripts/deploy-services.sh` runs it at every deploy, and the throwaway stacks of CI run it too. The script holds the one list of the secrets.
3. **Where the files live.** In a 0700 folder owned by `nfhotel`, next to the settings files, not in root's `/etc/nfhotel/secrets` as MIL-035 first proposed. The deploy runs as `nfhotel` and cannot write there, and the files would protect nothing more: the settings files they come from are already in the same place with the same owner. Root's folder keeps the logins that only root's deploy step reads (ADR-0011).
4. **Modes.** Each file is 0444. The container users (`app`, `postgres`) are not `nfhotel`, and Compose without Swarm cannot change the owner of a mounted secret, so the file must be readable by others; the 0700 folder keeps everyone but `nfhotel` and root out on the host, and inside the container only the one file is mounted.
5. **Rotation reaches the container.** Compose does not recreate a container when only a secret file changed, so the deploy puts a hash of the secret files of the API and the web application in their environment (`NFHOTEL_SETTINGS_HASH`), the same way the proxy has its configuration hash. A digest of high-entropy values reveals none of them.
6. **A check.** `scripts/check-secret-env.sh` reads `Config.Env` of the three containers after every deploy and at the end of every throwaway stack, and fails on a name of the list or a name that says password, token, secret or key (except `*_FILE`). It prints names, never values. A failure in production rolls the release back.
7. **Not rotated by the drill:** the passport key. It encrypts the passport numbers in the database, and the application holds one key, so a new key needs every stored value re-encrypted first. That needs a key ring in `AesGcmStringEncryptor` and a re-encryption command, which is new work outside MIL-035.

## Consequences

**Positive:**

- No secret of PostgreSQL, the API or the web application is in the environment of its container, so `docker inspect`, the socket proxy and a dump of the environment show none.
- Each secret can be rotated by editing one settings file and deploying, and CI does that for the database password, the API token, the signing key and the first Manager's password on every run of the drill.

**Negative:**

- The secrets are still plain text on the disk of the VM, as before; the protection is the owner and the mode, not encryption. Disk encryption or a vault would be the next step.
- The benchmark and market analysis services are built from other repositories and are not changed: the benchmark service still takes the API token from its environment.
- A compose command run by hand needs `scripts/write-secret-files.sh` first, or the deploy script.
- The passport key cannot be rotated yet (decision 7).

## Affected Artifacts

- [ADR-0004] and [ADR-0011]: the API token and the logins of the API are now files.
- [ADR-0020]: the socket proxy of Alloy no longer sees a secret (RC-185).
- [MIL-035]: tasks 2 to 4.

---

[ADR-0004]: ./adr-0004-database-api.md
[ADR-0011]: ./adr-0011-postgresql-access-for-the-api.md
[ADR-0020]: ./adr-0020-monitoring-stack.md
[MIL-035]: ../milestones/mil-035-secrets-as-files.md
[RC-185]: ../sqa/reviews/rc-185-alloy-docker-discovery.md
[9090645]: https://github.com/NF-Hotel/NFHotel/commit/90906459f40317be0fe2ac6e25862924cdccfee9
