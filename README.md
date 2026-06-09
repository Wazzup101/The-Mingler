# The Mingler

Public documentation and scripts for **The Mingler** — a third-party Discord bot for Toontown Rewritten (TTR) communities.

**Not affiliated** with Toontown Rewritten, Disney, or any official TTR staff.

**Operator:** @wazzup_101 on Discord (bot owner).

---

## What is published here

This repository is for **transparency and setup help** — not the full bot source code.

| Path | Purpose |
|------|---------|
| [docs/REGISTER_PUSH_SYNC.md](docs/REGISTER_PUSH_SYNC.md) | Push sync guide (`/register script`, `/register link`) for players and bot hosts |
| [scripts/register-sync.ps1](scripts/register-sync.ps1) | PowerShell script (readable source; also shipped in `register-sync.zip`) |
| [scripts/register-sync.cmd](scripts/register-sync.cmd) | Launcher for the PowerShell script |
| [scripts/register-sync-pack/README.txt](scripts/register-sync-pack/README.txt) | Plain-text guide included in the ZIP from `/register script` |

Download the packaged ZIP in Discord via **`/register script`** (ephemeral). You can inspect the same files here on GitHub before running anything on your game PC.

---

## Quick start (players)

1. Accept **`/register`** terms in Discord.
2. **`/register script`** — download and extract `register-sync.zip`; read `README.txt`.
3. Game open, logged in, **Companion App Support** ON (accept the in-game prompt).
4. **`/register link`** — copy **only** the PowerShell line from step 4; run it in the folder where you extracted the ZIP.
5. **`/register status`** — optional confirmation.

Full details: [docs/REGISTER_PUSH_SYNC.md](docs/REGISTER_PUSH_SYNC.md).

---

## Trust and safety

- Only run `register-sync` if you trust **The Mingler** operator (@wazzup_101).
- Use sync URLs and tokens **only** from your own **`/register link`** response — never use foreign tokens.
- The script reads the TTR **Companion API on your PC only** (`127.0.0.1`, ports 1547–1554) and POSTs JSON once to the bot URL in your link. It does not control the game.

While playing TTR you must still follow [TTR Terms of Service](https://www.toontownrewritten.com/terms) and [Privacy Policy](https://www.toontownrewritten.com/privacy).

---

## Bot hosts (operators)

If you run the bot yourself, see **Bot configuration** and **Security notes** in [docs/REGISTER_PUSH_SYNC.md](docs/REGISTER_PUSH_SYNC.md).

---

## Questions or issues

Use **`/tickets`** to send feedback, requests, and more to the bot operator (@wazzup_101).
