# Mingler Sync

Mingler Sync is the native Windows client for The Mingler's `/register`
flow. It performs the same user-initiated, read-only Companion API sync as the
existing PowerShell package, but without requiring players to use a terminal.

Install from the [Microsoft Store](https://apps.microsoft.com/detail/9PP2WVDVMX9L).

## Player flow

1. Open Toontown Rewritten and log into the toon(s) to sync.
2. Enable **Companion App Support** and accept the in-game prompt.
3. In Discord, run `/register link` and copy the command shown by the bot.
4. Open Mingler Sync and paste the command.
5. Select **Find my toons**, review the names, then select **Sync to Discord**.

## Safe demonstration

Select **Try safe demo** to exercise the review and completion interface with
fictional sample toons. The demo does not require Discord or the game and does
not scan or upload anything. It cannot register or modify toon data. Optional
Rich Presence is independent of the demo and stays off unless enabled.

Each explicit scan checks only local Companion API ports `1547` through `1562`,
supporting up to 16 concurrent toon sessions.

The one-time pairing token is held only in memory and is cleared after an upload
attempt. The app does not store toon payloads, credentials, or telemetry.

When an unexpected fault occurs while a still-valid pairing token is present,
the app may send a bounded diagnostic through the authenticated sync service.
It contains the known phase, app version, fixed error category/message, and Discord user tied to
the token. It excludes the token, raw Companion response, passwords, file paths,
and device inventory. Reports are rate-limited and may not arrive when the
network or sync service is unavailable.
Raw exception text is omitted rather than redacted.

## Optional Discord Rich Presence

**Show Mingler Sync on Discord** is off by default. When enabled, the Windows
app uses the running Discord desktop client to show **Playing Mingler Sync**,
the same app icon, and elapsed time for the current enabled session. Reconnecting
Discord does not reset the timer; turning the option off and back on, or reopening
the app, starts a new timer. Turning it off or closing the app requests activity
removal on a best-effort basis; a disconnected Discord client may take time to clear it.
Discord's own activity/privacy settings still control its visibility.

Only the app identity, fixed icon, and session start time are shared. No Toon
names, pairing information, registration results, server names, or file paths
enter the activity. The app saves only the on/off preference in
`%LOCALAPPDATA%\MinglerSync\preferences.json`; missing or invalid preferences mean
off. This does not start Discord, enable Windows startup, or scan/sync game data.

The publisher application is configured in this development build; see
[Discord application setup and verification](DISCORD_RICH_PRESENCE.md).
This does not opt anyone in or update the installed/Store app. An owner-approved
live check confirmed the matching icon, elapsed timer and clear-on-stop behavior.
Authenticated game sync and Store installation still require separate qualification.
If publisher configuration is missing or invalid, the option is visibly unavailable.

## Developer build

Requires the .NET 10 SDK on Windows. This source is candidate **1.0.3-rc.2**,
not the installed Store release. **About & Help** provides three scrollable
topics, fixed policy/source/Store links and safe troubleshooting guidance.
Unconfirmed server replies never produce a registration-success screen; check
`/register status` before repeating an upload.

```powershell
dotnet build .\MinglerSync.sln --maxcpucount:1 -p:UseSharedCompilation=false
dotnet run --project .\tests\MinglerSync.Core.Tests\MinglerSync.Core.Tests.csproj
dotnet run --project .\tests\MinglerSync.ReleaseChecks\MinglerSync.ReleaseChecks.csproj
dotnet run --project .\tests\MinglerSync.WinForms.Tests\MinglerSync.WinForms.Tests.csproj
```

Run the application:

```powershell
dotnet run --project .\src\MinglerSync.WinForms\MinglerSync.WinForms.csproj
```

Create a portable self-contained build:

```powershell
dotnet publish .\src\MinglerSync.WinForms\MinglerSync.WinForms.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Do not distribute a development build as the default player download. Stage 2
requires release signing, checksums, and clear publisher/download information.

## Microsoft Store package

The Store product is **Mingler Sync** (`9PP2WVDVMX9L`):
[apps.microsoft.com/detail/9PP2WVDVMX9L](https://apps.microsoft.com/detail/9PP2WVDVMX9L).
Its public package identity is recorded in `packaging/Package.appxmanifest`.

Generate the Store assets from the bot's public application icon:

```powershell
$env:WINAPP_CLI_TELEMETRY_OPTOUT = '1'
winapp manifest update-assets .\packaging\mingler-bot-icon.png --manifest .\packaging\Package.appxmanifest
```

Build the unsigned Store-submission package:

```powershell
.\packaging\build-msix.ps1
```

The Store submission package is generated under `artifacts/`. Local
installation requires a trusted development signature; the Microsoft Store
signs accepted production packages. Never commit development certificate files
or passwords.
