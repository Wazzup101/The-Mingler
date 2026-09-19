# The Mingler

Public documentation and scripts for **The Mingler** — a third-party Discord bot for Toontown Rewritten (TTR) communities.

**Not affiliated** with Toontown Rewritten, Disney, or any official TTR staff.

**Operator:** @wazzup_101 on Discord (bot owner).

---

## What is published here

This repository is for **transparency and setup help** — not the full bot source code. It documents **The Mingler** as a whole, while the files below are split into two groups: bot-wide policies, and the self-contained **Companion `/register` sync apparatus**.

### Bot (overall)

These apply to the entire bot, not just the sync feature.

| Path | Purpose |
|------|---------|
| [docs/TERMS_OF_SERVICE.md](docs/TERMS_OF_SERVICE.md) | Bot Terms of Service (Discord Developer Portal) |
| [docs/PRIVACY_POLICY.md](docs/PRIVACY_POLICY.md) | Bot Privacy Policy (Discord Developer Portal) |

### Companion `/register` sync tools

The optional Companion-client sync feature includes a beginner-friendly Windows
app and the existing PowerShell fallback.

| Path | Purpose |
|------|---------|
| [Mingler Sync (Microsoft Store)](https://apps.microsoft.com/detail/9PP2WVDVMX9L) | Recommended Windows app — install from the Microsoft Store |
| [apps/MinglerSync](apps/MinglerSync) | Inspectable Windows app source, tests, and Microsoft Store packaging |
| [docs/REGISTER_PUSH_SYNC.md](docs/REGISTER_PUSH_SYNC.md) | Push sync guide (`/register script`, `/register link`) for players |
| [scripts/register-sync.ps1](scripts/register-sync.ps1) | PowerShell script (readable source; also shipped in `register-sync.zip`) |
| [scripts/register-sync.cmd](scripts/register-sync.cmd) | Launcher for the PowerShell script |
| [scripts/register-sync-pack/README.txt](scripts/register-sync-pack/README.txt) | Plain-text guide included in the ZIP from `/register script` |

Download the packaged ZIP in Discord via **`/register script`** (ephemeral). You can inspect the same files here on GitHub before running anything on your game PC.

---

## Quick start (players)

Use these in **any Discord server** where The Mingler is installed.

1. Accept **`/register`** terms in Discord.
2. Install [Mingler Sync](https://apps.microsoft.com/detail/9PP2WVDVMX9L) from the Microsoft Store.
3. Open the game, log in, enable **Companion App Support**, and accept its prompt.
4. Run **`/register link`** and copy the private command block.
5. Paste it into Mingler Sync, find your toons, review the names, and confirm sync.
6. Use **`/register status`** for optional confirmation.

Prefer a terminal? Use **`/register script`** as the supported fallback. Extract the ZIP and follow its `README.txt`.

Mingler Sync uses The Mingler's current public Discord application artwork as
its Windows and Store icon.

Full details: [docs/REGISTER_PUSH_SYNC.md](docs/REGISTER_PUSH_SYNC.md).

---

## Trust and safety

- Only run `register-sync` if you trust **The Mingler** operator (@wazzup_101).
- Use sync URLs and tokens **only** from your own **`/register link`** response — never use foreign tokens.
- The app and fallback read the TTR **Companion API on your PC only** (`127.0.0.1`, ports 1547–1554) after you start a scan. They upload only after an explicit action, do not control the game, collect passwords or telemetry, or stay running in the background.
- Unexpected app faults may send only the bounded diagnostic documented in the [Privacy Policy](docs/PRIVACY_POLICY.md); tokens and raw Companion data are excluded.
- Install only from the [Microsoft Store](https://apps.microsoft.com/detail/9PP2WVDVMX9L) or inspect/build this public source. Modified or impersonated downloads may be unsafe.

While playing TTR you must still follow [TTR Terms of Service](https://www.toontownrewritten.com/terms) and [Privacy Policy](https://www.toontownrewritten.com/privacy).

---

## Questions or issues

Use **`/tickets`** to send feedback, requests, and more to the bot operator (@wazzup_101).
---

## Item Image Assets

Browseable copies of the item image collections (kept in sync from their own repos):

| Collection | Path in this repo |
|------------|-------------------|
| [Laser Beams](https://github.com/Wazzup101/Laser-Beams) | `assets/item-images/laser-beams/` |
| [Water Balloons](https://github.com/Wazzup101/Water-Balloons) | `assets/item-images/water-balloons/` |
| [Snowballs](https://github.com/Wazzup101/Snowballs) | `assets/item-images/snowballs/` |

Source repos stay independent. Edit them there, then re-run sync-mingler-assets.ps1 to update this mirror.

