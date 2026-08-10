# Mingler Sync

Mingler Sync is the Stage 1 native Windows client for The Mingler's `/register`
flow. It performs the same user-initiated, read-only Companion API sync as the
existing PowerShell package, but without requiring players to use a terminal.

## Player flow

1. Open Toontown Rewritten and log into the toon(s) to sync.
2. Enable **Companion App Support** and accept the in-game prompt.
3. In Discord, run `/register link` and copy the command shown by the bot.
4. Open Mingler Sync and paste the command.
5. Select **Find my toons**, review the names, then select **Sync to Discord**.

The one-time pairing token is held only in memory and is cleared after an upload
attempt. The app does not store toon payloads, credentials, or telemetry.

When an unexpected fault occurs while a still-valid pairing token is present,
the app may send a bounded diagnostic through the authenticated sync service.
It contains the phase, app version, error type/message, and Discord user tied to
the token. It excludes the token, raw Companion response, passwords, file paths,
and device inventory. Reports are rate-limited and may not arrive when the
network or sync service is unavailable.

## Developer build

```powershell
dotnet build .\MinglerSync.sln
dotnet run --project .\tests\MinglerSync.Core.Tests\MinglerSync.Core.Tests.csproj
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

The Store product is reserved as **Mingler Sync** (`9PP2WVDVMX9L`). Its public
package identity is recorded in `packaging/Package.appxmanifest`.

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
