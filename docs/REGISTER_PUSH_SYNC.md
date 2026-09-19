# Push sync for `/register`

Use the **Mingler Sync Windows app** with `/register link` when The Mingler and
your game run on different computers. The inspectable PowerShell package from
`/register script` remains a supported alternative.

## Recommended app flow

1. In any Discord server where The Mingler is installed, run **`/register link`**.
   - Accept the registration terms if prompted.
   - The bot replies privately with a short-lived, one-time pairing command.
2. On the game PC, open Toontown Rewritten, log into the toon(s) to sync, enable
   **Companion App Support**, and accept the in-game prompt.
3. Install or open [Mingler Sync](https://apps.microsoft.com/detail/9PP2WVDVMX9L).
4. Paste the entire private pairing command into the app.
5. Select **Find my toons**, review the names, then select **Sync to Discord**.
6. Run **`/register status`** in Discord if you want to confirm the last sync.

The app supports up to 16 concurrent local Companion sessions. It checks only
the local Companion service after you start a scan and uploads only after you
confirm. It does not control the game or run background syncs.

## PowerShell alternative

1. Run **`/register script`** and download `register-sync.zip`.
2. Extract the ZIP into a new folder and read its `README.txt`.
3. Run **`/register link`** and copy only the command shown in its final step.
4. Open PowerShell in the extracted folder, paste the command, and press Enter.
5. Run **`/register status`** in Discord if you want to confirm the last sync.

The package contains `README.txt`, `register-sync.cmd`, and
`register-sync.ps1`. The readable copies in this repository match the files
distributed by the bot.

## Response visibility

If your response visibility setting is public when the link is created, a
successful toon summary may appear in the channel where you ran the command.
With private response visibility, no channel summary is posted. Status and
pairing details remain private.

## Privacy and safety

- Never share a `/register link` command. It is tied to your Discord account,
  expires quickly, and can be used only once.
- Install Mingler Sync from the Microsoft Store, or inspect and build the source
  under [`apps/MinglerSync`](../apps/MinglerSync).
- The app keeps pairing information and Companion responses in memory only for
  the active session and clears pairing information after an upload attempt.
- Unexpected faults may send the limited diagnostic described in the
  [Privacy Policy](./PRIVACY_POLICY.md). Pairing tokens, raw Companion data,
  passwords, file paths, and device inventory are excluded.
- Plain HTTP is accepted only for a private home-network address supplied by
  `/register link`; internet uploads require HTTPS.
- Modified or impersonated downloads may be unsafe. Keep Windows and Microsoft
  Defender current and use `/tickets` if you notice suspicious behavior.

Mingler Sync and The Mingler are unofficial third-party tools and are not
affiliated with Toontown Rewritten, Disney, or their staff. Continue to follow
all Toontown Rewritten and Discord rules.
