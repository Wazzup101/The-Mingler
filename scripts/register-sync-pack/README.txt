The Mingler — register-sync (push sync)
========================================

QUICK START (Windows, same PC as the game)
------------------------------------------
  1. Game open, logged in, Companion App Support ON, in-game consent accepted.
  2. Discord: /register script — extract register-sync.zip into a NEW folder
     (not inside the ZIP). Move register-sync.cmd, register-sync.ps1, and
     README.txt into that folder before continuing. Extractors: 7-Zip
     (recommended) https://www.7-zip.org/ , WinRAR https://www.win-rar.com/ ,
     or WinZip https://www.winzip.com/
  3. Discord: /register link — copy ONLY the step 4 command.
  4. Shift+right-click that folder -> Open in Terminal / PowerShell.
  5. Paste the command, press Enter. Wait — idle ports can take up to ~1 minute.
  6. The script lists what was added or updated (profile, beans, gags, etc.).
  7. Discord: /register status to confirm.

TRUST: Only run this if you trust The Mingler bot operator (@wazzup_101). Not affiliated
with Toontown Rewritten or Disney. Uses URLs and tokens only from '/register
link' — never use or share your token with others.

WHAT THIS TOOL DOES
-------------------
  GET   http://127.0.0.1:1547-1554/all.json
        Reads the TTR Companion API on THIS PC only (up to 8 account ports).

  POST  The HTTPS URL from /register link (-SyncUrl)
        Uploads that JSON once, with your one-time -Token. Your game PC
        does not need to be on the bot's Wi-Fi for internet sync.

WHAT IT DOES NOT DO
-------------------
  - Install software, change the registry, or scan your disk
  - Download or run other scripts
  - Access Discord, passwords, or files outside this folder
  - Stay running in the background

The register-sync.ps1 file is readable source (~300 lines). Inspect it before running if you are unsure.
The .cmd file only launches PowerShell with a one-time
execution-policy bypass for this script. That bypass applies only
to this launch so Windows can run the script; it does not
change your system-wide PowerShell policy.

EXTRACT THE ZIP (before you run anything)
-----------------------------------------
  Do not run the script from inside the ZIP. Extract register-sync.zip into a
  new folder first, then move register-sync.cmd, register-sync.ps1, and
  README.txt into that folder. Open Terminal / PowerShell in that folder for
  the /register link command.

  Need an extractor? Official download pages:
    1. 7-Zip (recommended): https://www.7-zip.org/
    2. WinRAR: https://www.win-rar.com/
    3. WinZip: https://www.winzip.com/

SETUP (same folder after you move the files out of the ZIP)
-----------------------------------------------------------
  1. Game open, logged in, Companion App Support ON, in-game consent accepted.
  2. In Discord: /register link — copy the step 4 command only.
  3. Open a terminal in THIS folder (see OPEN A TERMINAL below), paste that
     line, press Enter.
  4. /register status to confirm (optional).

OPEN A TERMINAL IN THIS FOLDER
------------------------------
  Windows: Hold SHIFT, right-click an empty spot inside this folder, and pick
           "Open PowerShell window here" (or "Open Terminal here"). Paste the
           register-sync.cmd command. Double-clicking register-sync.cmd alone
           shows instructions but cannot sync without the command from step 4.

  macOS:   In Finder, right-click the folder and choose "New Terminal at
           Folder" (enable it once via System Settings -> Keyboard -> Keyboard
           Shortcuts -> Services if you don't see it). PowerShell is required:
           install with  brew install --cask powershell , then run:
             pwsh ./register-sync.ps1 -SyncUrl "..." -Token "..."

  Linux:   In your file manager right-click inside the folder and choose
           "Open Terminal Here" (or cd into the folder). PowerShell is
           required: install "powershell" from your distro / Microsoft repo,
           then run:
             pwsh ./register-sync.ps1 -SyncUrl "..." -Token "..."

  Note: the register-sync.cmd launcher is Windows-only. On macOS/Linux run the
  .ps1 directly with pwsh as shown above.
