MINGLER SYNC

Mingler Sync helps you register your own Toontown Rewritten toon data with
The Mingler Discord bot without opening PowerShell or a terminal.

HOW TO USE

1. Open Toontown Rewritten and log into the toon(s) you want to register.
2. Turn on Companion App Support in Settings > Gameplay > Miscellaneous.
3. Accept the Companion prompt shown by the game.
4. In Discord, run /register link and copy the entire private command block.
5. Open "Mingler Sync.exe" and paste that command.
6. Click Find my toons, review the names, then click Sync to Discord.
7. In Discord, run /register status to confirm if you want to double-check.

SAFE DEMO
Click Try safe demo to review fictional sample toons and finish a local-only
demonstration. The demo does not contact Discord, the game, or any server and
cannot change registration data.

PRIVACY AND SAFETY

- The app reads 127.0.0.1 ports 1547-1562 only after you click Find my toons (up to 16 concurrent toon sessions).
- It reads Companion data only. It does not control or automate the game.
- Your pairing token is kept in memory, hidden on screen, and cleared after use.
- The app does not collect passwords, chat, telemetry, or unrelated device data.
- If an unexpected app fault occurs while a valid pairing link is present, the
  app may send its phase, version, and bounded error type/message through the
  authenticated sync service. It does not send the token, raw Companion data,
  password, file paths, or device inventory. Network failures may prevent delivery.
- Internet uploads require HTTPS. Plain HTTP is accepted only for a private
  home-network address supplied by /register link.
- Do not share your /register link command. It expires and is one-time use.
- Install only from the Microsoft Store or inspect/build the public source. A
  replaced or modified executable could misuse anything pasted into it.
- No software is guaranteed vulnerability-free. Keep Windows and Microsoft
  Defender current and use /tickets if you see suspicious behavior.

FALLBACK

If the app does not work, the existing /register script PowerShell ZIP is
still supported and has not been removed or changed.

Mingler Sync and The Mingler are unofficial third-party tools and are not
affiliated with Toontown Rewritten, Disney, or their staff. You must continue
to follow all Toontown Rewritten and Discord rules.
