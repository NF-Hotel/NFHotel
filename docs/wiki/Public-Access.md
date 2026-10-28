# Public Access

NF Hotel is public at `https://nfhotel.devmatiker.com` through Cloudflare Tunnel: guests book and the mobile app works from anywhere, while no port is open in the home network and the back office sits behind Cloudflare Access.

**For:** S01, who owns the Cloudflare account and the domain, and anyone who answers `PublicSiteDown`.
**Decision:** [ADR-0028](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0028-public-access-through-cloudflare-tunnel.md), which explains why, with the options that were weighed. **Work:** MIL-040.

**Contents**
1. [What is public](#what-is-public)
2. [How a request travels](#how-a-request-travels)
3. [Set it up, step by step](#set-it-up-step-by-step)
4. [Check it](#check-it)
5. [Turn it off](#turn-it-off)
6. [When the public site is down](#when-the-public-site-is-down)
7. [Why it is built this way](#why-it-is-built-this-way)

---

## What is public

| Path on `nfhotel.devmatiker.com` | Public | Why |
| --- | --- | --- |
| `/`, `/book`, `/booking/...` | Yes | Guests look, book and sign in to their booking |
| `/mobile-api/api/auth/login`, `/mobile-api/api/rooms...` | Yes | The two routes of the mobile app (ADR-0015) |
| `/admin...` | Behind Cloudflare Access | Only listed staff get past the e-mail code to the hotel's sign-in (MIL-041); without it, 404 |
| Any other path of `/mobile-api/` | No, 404 | The API has no other public path |
| SSH, the monitoring, PostgreSQL | No | Not on the tunnel at all |

## How a request travels

```mermaid
flowchart LR
  G[Guest or phone] -->|HTTPS| CF[Cloudflare]
  CF -->|encrypted tunnel, started from the VM| T[cloudflared on nf-hotel]
  T -->|http://nginx:8082, inside the VM| N[nginx public listener]
  N --> W[Web application]
  N -->|two mobile routes| A[Database API]
  S[Staff on the tailnet] -->|HTTPS :443| N2[nginx tailnet server] --> W
```

No arrow points into the home network: `cloudflared` opens the connection from the inside, so the router keeps every port closed.

## Set it up, step by step

The order matters: Cloudflare adds the public host name only once the tunnel has a connected connector, so the token goes on the VM (Part B) before the name is added (Part C).

### Part A: Cloudflare (S01, in the browser)

1. **Create the tunnel.** In the [Cloudflare dashboard](https://dash.cloudflare.com), open **Networking → Tunnels → Create a tunnel**, choose **Cloudflared**, and name it `nf-hotel`. *Why this kind:* a tunnel managed in the dashboard needs only a token on the VM, no certificate file.
2. **Leave the install page.** It shows install commands with the token in them: do not run them and do not copy them anywhere. The token is fetched again in Part B.
3. **Force HTTPS.** In the domain's **SSL/TLS → Edge Certificates**, turn on **Always Use HTTPS**. *Why:* a guest who types `http://` is sent to `https://` by Cloudflare before anything reaches the hotel.
4. **Limit sign-in attempts.** In the domain's **Security → Security rules → Create rule → Rate limiting rules**, create one rule: when the URI path contains `/mobile-api/api/auth/login`, with the same IP, more than 5 requests in 10 seconds, **Block** for 10 seconds. *Why:* password guessing is stopped at Cloudflare; the API's own per-address limit stays as the second line.

### Part B: the token on `nf-hotel` (S01, over the tailnet)

5. **Fetch the token.** In **Networking → Tunnels → nf-hotel → Edit**, the install command ends with a long value starting with `eyJ`: that value alone is the token. It gives control of the tunnel, so it goes only into the command of step 8.
6. Open a shell on the VM: `tailscale ssh root@nf-hotel`.
7. Create the settings file from its example, readable by its owner only:
   ```bash
   sudo -u nfhotel bash -c 'umask 077 && cp /opt/nfhotel/ops/deploy/cloudflared/cloudflared.env.example /opt/nfhotel/ops/deploy/cloudflared/cloudflared.env'
   ```
8. Write the token without it reaching the screen, the shell history or the process list. The command asks for it; paste it and press Enter:
   ```bash
   sudo -u nfhotel bash -c 'f=/opt/nfhotel/ops/deploy/cloudflared/cloudflared.env; read -rs -p "Tunnel token: " t; echo; umask 077; { grep -v "^TUNNEL_TOKEN=" "$f"; printf "TUNNEL_TOKEN=%s\n" "$t"; } >"$f.new" && mv "$f.new" "$f"'
   ```
   *Why like this:* `read -s` does not echo, and `printf` is part of the shell, so the token is never a command argument that another user could see.
9. **Deploy:** re-run the latest successful `NF Hotel CD` run in GitHub Actions. The deploy writes the token to a secret file (ADR-0024), starts `cloudflared`, and checks that no secret is in its environment. In the dashboard the tunnel turns **Healthy**.

### Part C: the public host name (S01, in the browser)

10. In **Networking → Tunnels → nf-hotel → Edit → Routes → Add route → Published application**: subdomain `nfhotel`, domain `devmatiker.com`, path empty, service URL `http://nginx:8082`, HTTP Host Header empty, the other settings at their defaults. Cloudflare creates the DNS record itself. *Why HTTP:* the hop from `cloudflared` to nginx stays inside the VM on a Docker network; the tunnel itself and the guest's connection to Cloudflare are encrypted. *Why no Host Header:* the web application gets `nfhotel.devmatiker.com`, the name the guest typed.

### Rotate the token

If the token has been anywhere but step 8 (a chat, a screenshot, a file), rotate it: in the tunnel's settings, refresh (rotate) the token, then repeat steps 5 to 9 with the new one; the old token stops working. Where the dashboard offers no such action, delete the tunnel, create a new one with the same name, and repeat Parts B and C.

## The back office from the internet

**Decision:** [ADR-0029](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0029-back-office-behind-cloudflare-access.md). **Work:** MIL-041.

### Sign in as staff

1. Open `https://nfhotel.devmatiker.com/admin` in any browser, without Tailscale.
2. Cloudflare asks for your e-mail address. Type the one the team listed for you.
3. A 6-digit code arrives by e-mail (look in the spam folder the first time). Type it in.
4. The hotel's own sign-in appears: your user name and password, as before.

Cloudflare remembers the browser for 24 hours. An address that is not on the list gets no code and never sees the hotel's sign-in. On the tailnet, `https://nf-hotel.taildb84b5.ts.net/admin` works as before, without a code.

### Set it up (S01, in the browser, before the nginx change is deployed)

1. **The Access application.** In **Zero Trust → Access controls → Applications → Add an application → Self-hosted**: name `NF Hotel back office`, session duration **24 hours**, public hostname `nfhotel` · `devmatiker.com`, path `admin`. Login method: **One-time PIN** only.
2. **The policy.** Add a policy `Team`, action **Allow**, include **Emails**: the listed addresses. Save the application.
3. **Note two values.** In **Zero Trust → Settings**, the *team domain* (`<team>.cloudflareaccess.com`); on the application's page, the **Application Audience (AUD) Tag**. Neither is a secret.
4. **The validating route.** In **Networking → Tunnels → nf-hotel → Edit → Routes → Add route → Published application**: subdomain `nfhotel`, domain `devmatiker.com`, path `^/admin`, service URL `http://nginx:8082`. Under **Additional application settings → Access**, turn on **Validate the Access JWT at the tunnel** (or "Protect with Access") with the team name and the AUD tag of step 3. *Why:* a request for `/admin` that did not pass Access stops at the tunnel and never reaches the VM.
5. **Check before the deploy:** `https://nfhotel.devmatiker.com/admin/login` in a private window shows Cloudflare's e-mail page. Only then is the nginx change of MIL-041 deployed, which lets a request with the Access token through to the back office.

### Add or remove a member of staff

In **Zero Trust → Access controls → Applications → NF Hotel back office → Policies → Team**, add or remove the address under **Emails** and save. Removing an address stops new codes at once; a session already open ends within 24 hours. Their hotel account is managed in the back office as before (Manager → staff accounts).

## Check it

From a phone with Tailscale turned off, or any machine outside the tailnet:

- `https://nfhotel.devmatiker.com/` and `/book` open, with a valid certificate.
- `https://nfhotel.devmatiker.com/admin/login` shows Cloudflare's e-mail page, not the hotel's sign-in.
- In the Cloudflare dashboard the tunnel `nf-hotel` shows **Healthy**.
- In Grafana, the public probe (`job="public_site"`) shows success.

## Turn it off

- **At once, from the browser:** in **Networking → Tunnels**, open `nf-hotel` and delete its route `nfhotel.devmatiker.com`. The site leaves the internet; the tailnet is unchanged.
- **For good, on the VM:** delete `/opt/nfhotel/ops/deploy/cloudflared/cloudflared.env` and re-run CD; the deploy leaves the tunnel out. `PublicSiteDown` alerts, and stops 7 days after the last success.

## When the public site is down

`PublicSiteDown` in Discord means the public probe has failed for 3 minutes, so guests cannot reach the site.

1. Open `https://nf-hotel.taildb84b5.ts.net/` from the tailnet. If that fails too, the VM or the site is down: follow `SiteDown` in [Monitoring](Monitoring#runbook-when-an-alert-arrives).
2. If only the public name fails: look at the tunnel in the Cloudflare dashboard. **Down** or **Inactive** means `cloudflared` is not connected: on `nf-hotel`, `sudo docker logs --tail 50 nfhotel-cloudflared-cloudflared-1`.
3. An "unauthorized" or token error in that log means the token was revoked or mistyped: repeat Part B step 8 with the token from the dashboard, then re-run CD.
4. A 502 from Cloudflare with a healthy tunnel means nginx did not answer on port 8082: check `sudo docker ps` for the nginx container.

## Why it is built this way

- **A tunnel, not open ports.** The VM stands in a private home; with a tunnel the home address stays hidden and the router stays closed, and Cloudflare stops floods before they reach the home connection.
- **A separate listener on 8082.** The public rules (the back office only behind Access, the client's address from Cloudflare, HTTPS reported to the application) apply only to tunnel traffic; the tailnet server on 443 keeps working as before.
- **The back office behind Cloudflare Access.** Strangers cannot try passwords on a sign-in page they cannot see, and staff need no Tailscale (ADR-0029).
- **The token as a file.** Like every other secret since ADR-0024, it is never an environment variable, so `docker inspect` and the monitoring cannot read it.
- **Production on every release.** An error shows the error page, never a stack trace, to a guest.
