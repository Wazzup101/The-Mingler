# Register sync — deployment modes

> **Companion doc:** [REGISTER_PUSH_SYNC.md](./REGISTER_PUSH_SYNC.md) (player flow, API, security).  
> **Bot config keys:** `register_sync` in the bot’s `config.json` (see the live bot’s `utils/register/registerSyncConfig.js`).

Choose **one** deployment mode. Do not port-forward plain HTTP port **3847** to the internet.

---

## Quick comparison

| | **Home LAN** | **Internet (HTTPS)** |
|---|--------------|----------------------|
| Who can sync | Devices on your Wi‑Fi / LAN | Anyone with a valid `/register link` token |
| `listen_host` | `0.0.0.0` | `127.0.0.1` |
| `lan_bind` | `true` | `false` |
| `public_base_url` | `http://192.168.x.x:3847` | `https://sync.yourdomain.com` |
| `lan_clients_only` | `true` (recommended) | Ignored (see below) |
| Reverse proxy | None | Cloudflare Tunnel, Caddy, or nginx |
| Router port-forward | Not required | Not required (tunnel) or 443 only (proxy on VPS) |

Example JSON templates: [`docs/examples/register-sync-home-lan.config.json`](./examples/register-sync-home-lan.config.json), [`docs/examples/register-sync-internet.config.json`](./examples/register-sync-internet.config.json), [`docs/examples/register-sync-mingler.cc.config.json`](./examples/register-sync-mingler.cc.config.json) (production hostname).

### mingler.cc (operator)

**Domain:** `mingler.cc` · **Sync hostname:** `https://sync.mingler.cc`  
**Tunnel config:** [`scripts/ops/cloudflared-mingler.cc.yml`](../scripts/ops/cloudflared-mingler.cc.yml)  
**Runbook:** [`scripts/ops/README-mingler.cc.md`](../scripts/ops/README-mingler.cc.md)

Set `public_base_url` to `https://sync.mingler.cc`. Run the tunnel on the bot PC (or use `tunnel_auto_start` in bot config). Verify with `scripts/ops/verify-register-sync-health.ps1`.

---

## Mode A — Home LAN (same-house testers)

Use when every tester’s game PC can reach the bot PC on your private network.

### Bot config

Copy from [`register-sync-home-lan.config.json`](./examples/register-sync-home-lan.config.json) into the bot `config.json` → `register_sync`:

```json
"register_sync": {
  "enabled": true,
  "lan_bind": true,
  "lan_clients_only": true,
  "listen_host": "0.0.0.0",
  "listen_port": 3847,
  "public_base_url": "http://192.168.1.152:3847",
  "public_base_url_alt": "http://172.20.10.2:3847",
  "token_ttl_minutes": 15,
  "max_body_bytes": 2097152
}
```

Replace `192.168.1.152` with the bot PC’s LAN IP (`ipconfig` on Windows).  
`public_base_url_alt` is optional (e.g. iPhone hotspot IP when the bot runs on that network).

### Windows Firewall (bot PC)

Allow **inbound TCP 3847** from **Private** networks only:

```powershell
New-NetFirewallRule -DisplayName "Mingler Register Sync (LAN)" `
  -Direction Inbound -Protocol TCP -LocalPort 3847 `
  -Action Allow -Profile Private
```

### Verify

On a **game PC** on the same LAN:

```powershell
Invoke-WebRequest -Uri "http://192.168.1.152:3847/api/register/health" -UseBasicParsing
```

Expect `{"ok":true,"service":"register-sync"}`.

---

## Mode B — Internet beta (recommended: Cloudflare Tunnel)

Use when testers are **not** on your home network. The bot still listens on **localhost only**; Cloudflare terminates HTTPS and forwards to `127.0.0.1:3847`.

**Why Tunnel:** No router port-forward, free TLS, works behind CGNAT, fits a home Windows bot host.

### 1. Prerequisites

- A domain on Cloudflare (DNS managed by Cloudflare).
- [cloudflared](https://developers.cloudflare.com/cloudflare-one/connections/connect-networks/downloads/) installed on the **bot PC**.

### 2. Create the tunnel (one-time)

```powershell
cloudflared tunnel login
cloudflared tunnel create mingler-register-sync
```

Note the tunnel UUID and credentials path (e.g. `%USERPROFILE%\.cloudflared\<uuid>.json`).

### 3. DNS

```powershell
cloudflared tunnel route dns mingler-register-sync sync.yourdomain.com
```

Use a dedicated hostname (e.g. `sync.yourdomain.com`), not your main site root.

### 4. Tunnel config

Copy [`scripts/ops/cloudflared-register-sync.config.example.yml`](../scripts/ops/cloudflared-register-sync.config.example.yml) or [`scripts/ops/cloudflared-mingler.cc.yml`](../scripts/ops/cloudflared-mingler.cc.yml) and edit:

- `tunnel` → your tunnel name or UUID
- `credentials-file` → path from step 2
- `hostname` → `sync.yourdomain.com`

Run manually to test:

```powershell
cloudflared tunnel run mingler-register-sync
```

Install as a Windows service when stable:

```powershell
cloudflared service install
```

### 5. Bot config (internet)

Update `register_sync` in `config.json` — see [`register-sync-internet.config.json`](./examples/register-sync-internet.config.json) or [`register-sync-mingler.cc.config.json`](./examples/register-sync-mingler.cc.config.json):

```json
"register_sync": {
  "enabled": true,
  "lan_bind": false,
  "listen_host": "127.0.0.1",
  "listen_port": 3847,
  "public_base_url": "https://sync.yourdomain.com",
  "token_ttl_minutes": 15,
  "max_body_bytes": 2097152
}
```

Remove `public_base_url_alt` unless you still need a separate LAN URL (usually you do not — LAN users can use the HTTPS URL too).

Restart the bot. Startup log should show:

```
[RegisterSync] Listening on http://127.0.0.1:3847 ... link URL(s): https://sync.yourdomain.com/api/register/sync
```

### 6. Verify (any network)

```powershell
Invoke-WebRequest -Uri "https://sync.yourdomain.com/api/register/health" -UseBasicParsing
```

Then run a full `/register link` → script test from a game PC **off** your home Wi‑Fi.

### `lan_clients_only` on internet mode

The IP filter runs **only** when `lan_bind` is `true` and `listen_host` is `0.0.0.0`. With `127.0.0.1` + a reverse proxy, all requests arrive from loopback — you do **not** need to set `lan_clients_only: false`. Security is: HTTPS + one-time Bearer tokens + no public listing of tokens.

---

## Mode C — Internet via Caddy or nginx (same machine or VPS)

Use if you already run Caddy/nginx with a public certificate, or the bot runs on a VPS.

Traffic path:

```
https://sync.yourdomain.com/api/register/*  →  http://127.0.0.1:3847/api/register/*
```

Examples:

- Caddy: [`scripts/ops/Caddyfile.register-sync.example`](../scripts/ops/Caddyfile.register-sync.example)
- Bot config: same as Mode B (`127.0.0.1`, `lan_bind: false`, `public_base_url: https://...`).

**Body size:** Default max POST is 2 MiB (`max_body_bytes`). If the proxy has a lower limit, raise it (Caddy has no default issue; nginx: `client_max_body_size 2m;`).

---

## Migrating LAN → Internet

1. Set up Mode B (or C) and verify health over HTTPS.
2. Change `register_sync` to internet template (`127.0.0.1`, `lan_bind: false`, `public_base_url: https://...`).
3. Restart the bot — `/register link` will embed the new URL automatically.
4. Tell testers to re-run `/register link` (old LAN URLs in copied commands will fail).
5. Optional: remove the Windows Firewall LAN rule for 3847 (nothing listens on LAN anymore).

---

## Environment overrides (no config file edit)

| Variable | Example |
|----------|---------|
| `REGISTER_SYNC_PUBLIC_URL` | `https://sync.yourdomain.com` |
| `REGISTER_SYNC_LISTEN_HOST` | `127.0.0.1` |
| `REGISTER_SYNC_LAN_BIND` | `false` |
| `REGISTER_SYNC_ENABLED` | `true` |

`REGISTER_SYNC_PUBLIC_URL` wins over `public_base_url` in JSON.

---

## Checklist before internet beta

- [ ] `public_base_url` uses **https://** (not `http://` on a public hostname).
- [ ] Bot binds **127.0.0.1:3847** only (`lan_bind: false`).
- [ ] Health check works from a phone on cellular (off Wi‑Fi).
- [ ] Full push sync works end-to-end (script + token + `/register status`).
- [ ] Router does **not** forward port 3847.
- [ ] Guild visibility for `/register` includes target servers (bot `commandVisibility.js`).

---

## Troubleshooting

| Symptom | Likely cause |
|---------|----------------|
| Script: connection refused / timeout to `192.168.x.x` | Wrong LAN IP, firewall, AP isolation, or VPN |
| Script: 403 Forbidden | `lan_clients_only` + client not on private IP (or wrong network) |
| Script: 401 on upload | Expired or reused token — new `/register link` |
| Health OK but sync fails | Game/Companion not running on game PC (script never POSTs) |
| Health fails on HTTPS | Tunnel/proxy down, DNS not propagated, wrong hostname in config |
| `/register link` still shows LAN URL | Bot not restarted after config change; check `REGISTER_SYNC_PUBLIC_URL` env override |
