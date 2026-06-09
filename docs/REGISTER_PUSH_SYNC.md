# Push sync for `/register`

When the bot runs on a **different computer** than the player’s game, use **push sync** (`/register script` + `/register link`).

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

In `JSON Storage/Configuration/config.json`:

```json
"register_sync": {
  "enabled": true,
  "lan_bind": false,
  "listen_host": "127.0.0.1",
  "listen_port": 3847,
  "public_base_url": "https://your-public-hostname",
  "token_ttl_minutes": 15,
  "max_body_bytes": 2097152
}
```

| Field | Purpose |
|--------|---------|
| `listen_host` / `listen_port` | Where the Node HTTP server binds (default `127.0.0.1:3847`). |
| `lan_bind` | Must be **`true`** to bind `listen_host` `0.0.0.0` (home LAN). If `false` and host is `0.0.0.0`, the bot binds **`127.0.0.1` only**. |
| `lan_clients_only` | Default **`true`** with LAN bind: only private/LAN client IPs may call sync/health (helps if port 3847 is accidentally forwarded). |
| `public_base_url` | URL players use in `/register link` (HTTPS for internet; `http://192.168.x.x` OK for home LAN only). |
| `public_base_url_alt` | Optional second LAN URL (e.g. hotspot). |
| `token_ttl_minutes` | Link expiry. |
| `max_body_bytes` | Max POST body size. |

**Recommended (internet-facing):** `listen_host` `127.0.0.1`, `lan_bind` `false`, HTTPS reverse proxy to port 3847, `public_base_url` `https://…`.

**Home LAN only:** `lan_bind` `true`, `listen_host` `0.0.0.0`, `public_base_url` `http://<bot-LAN-IP>:3847`. **Do not** port-forward plain HTTP to the internet.

Environment overrides: `REGISTER_SYNC_PUBLIC_URL`, `REGISTER_SYNC_LISTEN_HOST`, `REGISTER_SYNC_LAN_BIND`, `REGISTER_SYNC_ENABLED`, etc. (see `utils/register/registerSyncConfig.js`).

## Reverse proxy (example)

Expose the API behind nginx/Caddy on the same machine as the bot:

```
https://api.example.com/api/register/sync  →  http://127.0.0.1:3847/api/register/sync
```

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
