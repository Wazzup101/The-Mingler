# mingler.cc — register sync (Cloudflare Tunnel)

**Domain:** [mingler.cc](https://mingler.cc) (Cloudflare)  
**Sync API:** `https://sync.mingler.cc/api/register/sync`  
**Health:** `https://sync.mingler.cc/api/register/health`

The bot binds **localhost only** (`127.0.0.1:3847`). Cloudflare Tunnel terminates HTTPS and forwards to the bot. No router port-forward for 3847.

---

## Phase 0 — Resolve the domain and add it to Cloudflare

**Exact hostname for register sync:** `sync.mingler.cc` (subdomain).  
**Root domain:** `mingler.cc` (apex). You onboard the **zone** `mingler.cc` once; the tunnel adds `sync` as a record later.

### Step 0A — Confirm whether you own `mingler.cc`

1. Open [https://dash.cloudflare.com](https://dash.cloudflare.com) → check **Websites** for `mingler.cc`.
2. If it is **not** listed, check your registrar (Namecheap, Google Domains, Porkbun, etc.) for an active registration.
3. Optional from PowerShell (once DNS exists): `nslookup mingler.cc` — should return nameservers or an IP, not “Non-existent domain”.

| Situation | What to do |
|-----------|------------|
| **Not registered anywhere** | Register `mingler.cc` (Phase 0B). |
| **Registered elsewhere, not on Cloudflare** | Add site to Cloudflare (Phase 0C). |
| **Already on Cloudflare** | Skip to [Phase 1 — Bot config](#1-bot-config-already-set-in-configjson). |

`PHX-themingler.cc-payment_intent` in Stripe is **payment metadata**, not DNS. It does not prove the domain is registered or on Cloudflare.

### Step 0B — Register `mingler.cc` (if you do not own it)

**Option 1 — Cloudflare Registrar (simplest long-term)**

1. [dash.cloudflare.com](https://dash.cloudflare.com) → **Domain registration** → **Register domains**.
2. Search **`mingler.cc`** → add to cart → complete purchase.
3. The zone is created automatically with Cloudflare nameservers — skip Phase 0C.

**Option 2 — Another registrar**

1. Buy `mingler.cc` at your preferred registrar.
2. Continue with Phase 0C to point DNS to Cloudflare.

`.cc` domains are usually paid yearly; availability and price vary by registrar.

### Step 0C — Add an existing domain to Cloudflare

1. [dash.cloudflare.com](https://dash.cloudflare.com) → **Add a site** (or **Websites** → **Onboard a domain**).
2. Enter **`mingler.cc`** (apex only, no `https://`, no `sync.`).
3. Pick a plan (**Free** is enough for tunnel + DNS).
4. Cloudflare scans existing DNS (may be empty if the domain was never configured).
5. Copy the **two Cloudflare nameservers** shown (e.g. `ada.ns.cloudflare.com`, `bob.ns.cloudflare.com`).
6. At your **registrar**, open `mingler.cc` → **Nameservers** → **Custom DNS** → paste Cloudflare’s pair → save.
7. Back in Cloudflare, click **Continue** / **Check nameservers**. Propagation often takes **15 minutes to 48 hours**; Cloudflare emails you when active.
8. When status is **Active**, the zone is ready for Phase 2 (tunnel DNS).

**Do not** change `public_base_url` in the bot until `sync.mingler.cc` resolves (Phase 4 verify).

### Step 0D — What you are *not* setting up yet

- No A record for `sync` manually (tunnel creates it in Phase 2).
- No port-forward on your router.
- Root `mingler.cc` can stay empty or point at a future site; register sync uses **`sync.mingler.cc` only**.

---

## Phase 1 — Bot config (already set in `config.json`)

```json
"register_sync": {
  "enabled": true,
  "lan_bind": false,
  "listen_host": "127.0.0.1",
  "listen_port": 3847,
  "public_base_url": "https://sync.mingler.cc",
  "tunnel_auto_start": true,
  "tunnel_name": "mingler-register-sync",
  "tunnel_config": "scripts/ops/cloudflared-mingler.cc.yml",
  "token_ttl_minutes": 15,
  "max_body_bytes": 2097152
}
```

With `tunnel_auto_start: true`, the bot spawns cloudflared in the same console when the sync API starts listening. Set `tunnel_auto_start: false` if you use the Windows cloudflared service instead.

---

## Phase 2 — Cloudflare Tunnel (one-time, bot PC)

On the **same PC that runs the bot**:

```powershell
# Install cloudflared if needed: https://developers.cloudflare.com/cloudflare-one/connections/connect-networks/downloads/

cloudflared tunnel login
cloudflared tunnel create mingler-register-sync
cloudflared tunnel route dns mingler-register-sync sync.mingler.cc
```

Edit [`cloudflared-mingler.cc.yml`](./cloudflared-mingler.cc.yml):

- Set `credentials-file` to the JSON path printed by `tunnel create` (usually `%USERPROFILE%\.cloudflared\<uuid>.json`).

### Existing tunnel (e.g. PHX-themingler)

If **mingler.cc** is already on Cloudflare and you have a tunnel from another project (dashboard name like `PHX-themingler`):

1. Open **Zero Trust → Networks → Tunnels** in Cloudflare.
2. Edit that tunnel → **Public Hostname** → add:
   - **Subdomain:** `sync`
   - **Domain:** `mingler.cc`
   - **Service:** `http://127.0.0.1:3847`
3. Or merge the `ingress` rule from `cloudflared-mingler.cc.yml` into that tunnel’s local config.

Do **not** point the root `mingler.cc` at the bot unless you intend the whole site to be the sync API.

---

## Phase 3 — Run the tunnel

**Default (one window):** with `tunnel_auto_start: true` in `register_sync` (see Phase 1), **starting the bot** also starts cloudflared in the same console (`[Cloudflared]` log prefix). No separate cmd window needed.

Manual test (optional):

```powershell
cd path\to\this\repo\scripts\ops
cloudflared tunnel --config cloudflared-mingler.cc.yml run mingler-register-sync
```

Or use [`start-register-sync-tunnel.cmd`](./start-register-sync-tunnel.cmd) only if `tunnel_auto_start` is `false`.

Leave the bot running whenever players need push sync (tunnel stops when the bot stops).

---

## Phase 4 — Verify and go live

```powershell
.\verify-register-sync-health.ps1
```

Or:

```powershell
Invoke-WebRequest -Uri "https://sync.mingler.cc/api/register/health" -UseBasicParsing
```

Expect: `{"ok":true,"service":"register-sync"}`

Then: **restart the bot** (if not since config change) → `/register link` in Discord → run script on a game PC (can be off your home Wi‑Fi) → `/register status`.

---

## Phase 5 — Windows service (optional)

After manual run works:

```powershell
# Point the service at your config (copy yml to %USERPROFILE%\.cloudflared\config.yml is the usual approach)
cloudflared service install
cloudflared service start
```

---

## Phase 6 — Public `/register` (when sync works)

Backend URL is ready after Phases 1–4. To let players outside test guilds use `/register`:

1. Expand guild list in `utils/bot/commandVisibility.js` (`register` key), or remove `register` from the map for global slash registration.
2. Restart bot (quick slash refresh runs on startup).
3. Post a short player guide: `/register script` → `/register link` → run script on game PC.

---

## Troubleshooting

| Issue | Fix |
|-------|-----|
| `mingler.cc` / `sync.mingler.cc` NXDOMAIN | Domain not registered or nameservers not pointed to Cloudflare yet (Phase 0) |
| Health fails on HTTPS | Tunnel not running; DNS not propagated; wrong `credentials-file` |
| `/register link` still shows `192.168.x.x` | Restart bot; check `REGISTER_SYNC_PUBLIC_URL` env override |
| 502 from Cloudflare | Bot not running or not listening on `127.0.0.1:3847` |
| Stripe / site on `mingler.cc` | Use **sync** subdomain only for register API |

**Note:** `PHX-themingler.cc-payment_intent` in Stripe is unrelated to register sync — it is payment metadata. Register sync only needs the `sync.mingler.cc` hostname on a tunnel.
