# Mingler Sync release notes

This is the version history for the Mingler Sync desktop app. Windows releases are distributed through the [Microsoft Store](https://apps.microsoft.com/detail/9PP2WVDVMX9L). Public Linux previews are listed in [GitHub Releases](https://github.com/Wazzup101/The-Mingler/releases). Historical builds and candidates are labeled separately.

## Linux 1.1.0-beta.3 · Public preview · November 23, 2026

- Gives you up to 90 seconds to accept the in-game Companion prompt after selecting Find my toons.
- Explains how to accept the prompt when multiple game windows are open.
- Includes tighter checks on registration destinations and bounds on local Companion responses.

[Download the Linux x64 preview](https://github.com/Wazzup101/The-Mingler/releases/tag/mingler-sync-v1.1.0-beta.3-linux).

## Windows 1.0.5 · Store update prepared

- Gives you up to 90 seconds to accept the in-game Companion prompt after selecting Find my toons.
- Adds guidance for accepting the prompt in each open game window.
- Check the Microsoft Store listing for availability.

## Linux 1.1.0-beta.2 · Historical candidate

- Tightened validation of registration destinations and added size limits for Companion responses.
- Improved handling of malformed or interrupted Companion responses.
- These changes are included in beta.3. Beta.2 was not distributed as a public download.

## Linux 1.1.0-beta.1 · Public preview · November 8, 2026

- Added the first public Linux x64 preview with a graphical interface and a bundled runtime.
- Added the guided flow to find Toons, review profiles, and explicitly sync them to Discord.
- Included optional Discord activity sharing, off by default.

[View the original Linux x64 preview](https://github.com/Wazzup101/The-Mingler/releases/tag/mingler-sync-v1.1.0-beta.1-linux).

## Windows 1.0.4 · Microsoft Store

- Added optional Discord Rich Presence with an app icon and session timer. It starts off by default.
- Added About & Help and improved layout on displays using Windows scaling.
- Added instructions for syncing through the public Mingler Sync Link companion in Discord.
- Retired the fictional demo. The app now uses the same real registration flow for players and reviewers.

## Windows 1.0.3 · Historical candidate

- Introduced optional Discord Rich Presence and an expanded About & Help window.
- Improved handling of uncertain upload responses and removed raw error text from diagnostics.
- Added scrolling so the Toon review area remained reachable on smaller displays.

## Windows 1.0.2 · Microsoft Store

- Published the Windows app source and the player documentation.
- Included a local safe demo with fictional profiles, separate from real Discord registration.
- Kept real registration behind the player's scan, review, and upload actions.

## Windows 1.0.1 · Historical Store candidate

- Prepared an updated Windows MSIX and documented how to test the real Companion pairing and sync flow.
- Retained the explicit review step before uploading Toon information.

## Windows 1.0.0 · First Windows build

- Introduced the native Windows app as a graphical alternative to the PowerShell sync tool.
- Added a guided flow to paste a pairing command, find local Toons, review them, and sync to Discord.
