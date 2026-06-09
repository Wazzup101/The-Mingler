The Mingler — register-sync (push sync)
========================================

TRUST: Only run this if you trust The Mingler bot operator (@wazzup_101). Not affiliated
with Toontown Rewritten or Disney. Uses URLs and tokens only from '/register
link' — never use or share your token with others.

WHAT THIS TOOL DOES
-------------------
  GET   http://127.0.0.1:1547-1554/all.json
        Reads the TTR Companion API on THIS PC only (up to 8 account ports).

  POST  The URL you pass with -SyncUrl (from /register link)
        Uploads that JSON once, with your one-time -Token.

WHAT IT DOES NOT DO
-------------------
  - Install software, change the registry, or scan your disk
  - Download or run other scripts
  - Access Discord, passwords, or files outside this folder
  - Stay running in the background

The .ps1 file is short (~100 lines). You can inspect the code
before running if you're not sure about the script used. The
.cmd file only launches PowerShell with a one-time
execution-policy bypass for this script. That bypass applies only
to this one launch so Windows can run the script; it does not
change your system-wide PowerShell policy.

SETUP (same folder after you extract this ZIP)
--------------------------------------------
  1. Game open, logged in, Companion App Support ON, in-game consent accepted.
  2. In Discord: /register link — copy the step 4 command only.
  3. Open PowerShell in this folder, paste that line, press Enter.
  4. /register status to confirm (Optional).

BOT HOST SECURITY (for operators)
---------------------------------
  The bot API should listen on 127.0.0.1 behind HTTPS reverse proxy for
  internet use. Do not port-forward plain HTTP port 3847 to the internet.
  LAN play (home Wi-Fi) may use a private IP in public_base_url — see
  https://github.com/Wazzup101/The-Mingler/blob/main/docs/REGISTER_PUSH_SYNC.md
  You can also review register-sync.ps1 in the same repo before running.
  With lan_clients_only (default on), only home/LAN IPs can reach the API
  even if the port is bound on 0.0.0.0.
