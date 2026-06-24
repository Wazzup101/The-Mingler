# Push sync for `/register`

When the bot runs on a **different computer** than the player's game, use **push sync** (`/register script` + `/register link`).

## Flow

1. In Discord (allowed guilds): **`/register link`**
   - Accept terms if prompted.
   - Bot replies **ephemerally** with a **one-time token** (15 minutes by default) and a PowerShell command.

2. On the **game PC** (game open, toon logged in, Companion App Support ON):
   - Download **`/register script`** → **`register-sync.zip`** (includes `README.txt`, `register-sync.cmd`, `register-sync.ps1`).
   - Extract the ZIP to a folder and read **`README.txt`**.
   - Run `/register link` and copy **only** the step 4 line (starts with `.\register-sync.cmd`).
   - Open **PowerShell in that same directory**, paste the line, press Enter.
   - The script reads **all** Companion account ports `1547–1554` (8s timeout per port, same as owner pull) and POSTs `{ "companions": [ ... ] }` to the bot.

3. In Discord: **`/register status`** — confirms last sync time.

Public source and docs: [github.com/Wazzup101/The-Mingler](https://github.com/Wazzup101/The-Mingler) (`scripts/register-sync-pack/`, `scripts/register-sync.ps1`, this file).

## Bot configuration

Full setup guide (home LAN vs internet HTTPS, Cloudflare Tunnel, Caddy, migration checklist):

**[REGISTER_SYNC_DEPLOYMENT.md](./REGISTER_SYNC_DEPLOYMENT.md)**

Example JSON snippets: [`docs/examples/register-sync-home-lan.config.json`](./examples/register-sync-home-lan.config.json), [`docs/examples/register-sync-internet.config.json`](./examples/register-sync-internet.config.json), [`docs/examples/register-sync-mingler.cc.config.json`](./examples/register-sync-mingler.cc.config.json) (production: `https://sync.mingler.cc`).

### Deployment modes (summary)

| Mode | `listen_host` | `lan_bind` | `public_base_url` | Reverse proxy |
|------|---------------|------------|---------------------|---------------|
| **Home LAN** | `0.0.0.0` | `true` | `http://<bot-LAN-IP>:3847` | None |
| **Internet** | `127.0.0.1` | `false` | `https://sync.yourdomain.com` | Cloudflare Tunnel, Caddy, or nginx |

In `JSON Storage/Configuration/config.json` → `register_sync` (internet template):

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

| Field | Purpose |
|--------|---------|
| `listen_host` / `listen_port` | Where the Node HTTP server binds (default `127.0.0.1:3847`). |
| `lan_bind` | Must be **`true`** to bind `listen_host` `0.0.0.0` (home LAN). If `false` and host is `0.0.0.0`, the bot binds **`127.0.0.1` only**. |
| `lan_clients_only` | With **LAN bind only**: only private/LAN client IPs may call sync/health. **Ignored** when using `127.0.0.1` + HTTPS proxy (internet mode). |
| `public_base_url` | URL players use in `/register link` (HTTPS for internet; `http://192.168.x.x` OK for home LAN only). |
| `public_base_url_alt` | Optional second LAN URL (e.g. hotspot). Usually omitted in internet mode. |
| `token_ttl_minutes` | Link expiry. |
| `max_body_bytes` | Max POST body size. |

**Do not** port-forward plain HTTP port **3847** to the internet.

Environment overrides: `REGISTER_SYNC_PUBLIC_URL`, `REGISTER_SYNC_LISTEN_HOST`, `REGISTER_SYNC_LAN_BIND`, `REGISTER_SYNC_ENABLED`, etc. (see bot `utils/register/registerSyncConfig.js`).

## Reverse proxy (example)

Expose the API behind Cloudflare Tunnel, Caddy, or nginx on the same machine as the bot:

```
https://sync.yourdomain.com/api/register/sync  →  http://127.0.0.1:3847/api/register/sync
```

Example configs: [`scripts/ops/cloudflared-register-sync.config.example.yml`](../scripts/ops/cloudflared-register-sync.config.example.yml), [`scripts/ops/Caddyfile.register-sync.example`](../scripts/ops/Caddyfile.register-sync.example).

Health check: `GET /api/register/health`

## API

**POST** `/api/register/sync`

- Header: `Authorization: Bearer <token from /register link>`
- Body: `{ "companion": { ... } }` for one toon, or `{ "companions": [ ... ] }` for multitoon (one `/all.json` per active port). Single-object root body still accepted.

**Responses**

| Code | Meaning |
|------|---------|
| 200 | Sync OK (`syncedToons` count) |
| 401 | Missing/invalid/expired token |
| 413 | Body too large |
| 422 | Import failed (no toons, game data issue) |

Tokens are **one-time** and stored **hashed** in SQLite (`register_sync_tokens`).

## Owner vs everyone else

| User | `/register sync` | Push sync (`link` / `script` / `status`) |
|------|------------------|------------------------------------------|
| Bot owner | Pull from localhost (game on same PC as bot) | Optional |
| Everyone else | Not available — use push sync | Required |

## Slash subcommands

- **`/register sync`** — owner-only pull from local Companion API (same PC as bot).
- **`/register link`** — push sync token + instructions.
- **`/register script`** — download `register-sync.zip` (ephemeral).
- **`/register status`** — last push sync.

## Security notes

- Never share your token; it binds to your Discord user only.
- Use **HTTPS** on `public_base_url` before exposing sync beyond a trusted LAN.
- Keep `listen_host` on **`127.0.0.1`** when using a reverse proxy; set **`lan_bind`** only for intentional home-LAN use.
- Do not port-forward port **3847** to the internet without TLS in front of the API.
