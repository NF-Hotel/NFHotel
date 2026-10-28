# Database Access

**Decision:** [ADR-0034](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0034-team-database-access.md). **Work:** MIL-048.

Each member of the team opens the production database from DBeaver with their own login. The port can be reached from the tailnet only. **This is the production database, with real guests:** you can read and change rows, and every change is logged with your name.

## Connect from DBeaver

1. Be connected to the tailnet (Tailscale on, your own account).
2. In DBeaver, choose Database → New Database Connection → PostgreSQL, and fill in:

   | Field | Value |
   | --- | --- |
   | Host | `100.72.194.127` |
   | Port | `5432` |
   | Database | `nfhotel` |
   | Username | your role, `dev_<name>` |
   | Password | the one S01 gave you |

3. Choose Test Connection, then Finish. DBeaver offers to download the PostgreSQL driver the first time.

The roles today:

| Role | Tailscale login |
| --- | --- |
| `dev_jakob` | `ZanjiOfficial@github` |
| `dev_william` | `lordwhitboy@gmail.com` |
| `dev_foxxy` | `outlashed@gmail.com` |
| `dev_mike` | `syneuro@gmail.com` |

## What you can do

- **You can:** read, insert, update and delete rows in every table.
- **You cannot:** create, truncate or drop a table, or change the migration history. The schema comes from the migrations only, so change it in code and deploy it.
- **Timeouts:**
  - A transaction left open for 10 minutes is ended.
  - A statement that waits more than 5 seconds for a lock gives up.

  This is so DBeaver cannot hold up the hotel. Use auto-commit, which is DBeaver's default.
- **Logging:** every change (`INSERT`, `UPDATE`, `DELETE`) is logged with your role. The log stays in Loki for 14 days. It holds the values you wrote, so it holds personal data.
- **A mistake:** a wrong `UPDATE` or `DELETE` is restored from the nightly backup ([Disaster Recovery](Disaster-Recovery)). Rows changed since then are lost. Ask S01 first, before you try to repair it by hand.

## Your password

- S01 gives it to you in private: never in a chat, an issue or a commit.
- Keep it in DBeaver's saved password or in a password manager.
- **Do not change it with SQL** (`ALTER ROLE … PASSWORD`). Every statement of your role is logged, so the new password would be in the log. Ask S01 for a new one instead.

## For S01: add, reset and remove a person

Run the commands on `nf-hotel` as root in `/opt/nfhotel/ops`. Without `--apply`, each one only shows what it would do.

```bash
sudo bash scripts/manage-team-db-users.sh --apply --add jakob
```
```bash
sudo cat /etc/nfhotel/secrets/team/dev_jakob.password
```

| Command | What it does |
| --- | --- |
| `--add <name>` | Creates `dev_<name>` with a new password in `/etc/nfhotel/secrets/team/dev_<name>.password` (root, 0600) |
| `--reset <name>` | Gives a new password and ends the person's sessions |
| `--remove <name>` | Ends the sessions, drops the role and deletes its file |
| `--list` | Shows the roles of the team |

A name is 2 to 20 small letters and digits.

The relay starts with the deploy once `ops/deploy/postgres-tailnet/postgres-tailnet.env` exists. Copy it from `postgres-tailnet.env.example`. Nothing in it is a secret.

## For S01: the Tailscale grant

Port 5432 is accepted on `tailscale0` only. The firewall closes it to the LAN and to the internet. Who on the tailnet may reach it is set in the tailnet policy, under Access controls in the Tailscale admin console. Add this to the `grants` list:

```json
{
  "src": ["ZanjiOfficial@github", "lordwhitboy@gmail.com", "outlashed@gmail.com", "syneuro@gmail.com"],
  "dst": ["100.72.194.127"],
  "ip":  ["tcp:5432"]
}
```

While the policy still has the default rule that lets every device reach every device, the grant adds nothing. Every device on the tailnet can then reach the port, and the password is the only lock. Removing the default rule changes all access on the tailnet, so give the other rules first.

## Without the relay: an SSH tunnel

S01, as the administrator, can still use a tunnel through Tailscale SSH:

```bash
ssh -N -L 15432:<address of the container>:5432 nfadmin@100.72.194.127
```

Then connect DBeaver to `localhost:15432`. Find the address with:

```bash
sudo docker inspect -f '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' nfhotel-postgres-postgres-1
```
