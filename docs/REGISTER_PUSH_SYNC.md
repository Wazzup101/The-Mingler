# Push sync for `/register`

When the bot runs on a **different computer** than your game, use Mingler Sync
with `/register link`. The PowerShell `/register script` package remains a
supported fallback.

## Flow

1. In Discord (any server where The Mingler is installed): **`/register link`**
   - Accept terms if prompted.
   - Bot replies **ephemerally** with a **one-time token** (15 minutes by default) and a PowerShell command.

2. On the **game PC** (game open, toon logged in, Companion App Support ON):
   - Open Mingler Sync and paste the entire private command block.
   - Select **Find my toons**, review the names, then select **Sync to Discord**.

   PowerShell fallback:
   - Download **`/register script`** → **`register-sync.zip`** (includes `README.txt`, `register-sync.cmd`, `register-sync.ps1`).
   - Extract the ZIP to a folder and read **`README.txt`**.
   - Run `/register link` and copy **only** the step 4 line (starts with `.\register-sync.cmd`).
   - Open **PowerShell in that same directory**, paste the line, press Enter.
   - The script reads **all** Companion account ports `1547–1554` (8s timeout per port) and POSTs `{ "companions": [ ... ] }` to the bot.

3. In Discord: **`/register status`** — confirms last sync time.

Public source: [github.com/Wazzup101/The-Mingler](https://github.com/Wazzup101/The-Mingler) (`scripts/register-sync-pack/`, `scripts/register-sync.ps1`, this file).

## Slash subcommands (players)

- **`/register link`** — one-time token + PowerShell command for your game PC.
- **`/register script`** — download `register-sync.zip` (ephemeral).
- **`/register status`** — last push sync time and pending link.

`/register sync` is for the bot owner only (game on the same PC as the bot).

## What the sync tools do

| Step | Where | What |
|------|--------|------|
| Read | `127.0.0.1:1547–1554` on **your game PC** | TTR Companion `/all.json` (only while the game is open) |
| Upload | HTTPS URL from **`/register link`** | One POST after your confirmation; the one-time token is cleared after use |

Production upload host: **`https://sync.mingler.cc`** (embedded in your link command).

## Security notes

- Only run `register-sync` if you trust **The Mingler** operator (@wazzup_101).
- Use sync URLs and tokens **only** from your own **`/register link`** — never share your token.
- The app and script do not control gameplay, scan your disk, collect passwords or telemetry, or stay running in the background.
- Mingler Sync is free and provided for community information and convenience. It is unofficial and not affiliated with or endorsed by Toontown Rewritten, Disney, Discord, or their staff.
- While playing TTR you must still follow [TTR Terms of Service](https://www.toontownrewritten.com/terms) and [Privacy Policy](https://www.toontownrewritten.com/privacy).
