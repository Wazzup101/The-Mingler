# Mingler Sync

Mingler Sync is a native Windows client for The Mingler's optional `/register`
flow. It performs the same user-initiated, read-only Companion API sync as the
PowerShell fallback without requiring players to use a terminal.

## Player flow

1. Open Toontown Rewritten and enable **Companion App Support**.
2. In Discord, run `/register link` and copy the private command block.
3. Paste it into Mingler Sync and select **Find my toons**.
4. Review the names, then select **Sync to Discord**.

The pairing token stays in memory and is cleared after an upload attempt. The
app does not store toon payloads, passwords, or telemetry, and it never controls
the game or syncs in the background.

## Build and test

Requires the .NET 8 SDK on Windows.

```powershell
dotnet build .\MinglerSync.sln
dotnet run --project .\tests\MinglerSync.Core.Tests\MinglerSync.Core.Tests.csproj
```

## Microsoft Store package

The Store product is reserved as **Mingler Sync** (`9PP2WVDVMX9L`). Install the
Microsoft WinApp CLI, then run:

```powershell
.\packaging\build-msix.ps1
```

The script generates visual assets from the committed SVG and writes an
unsigned Store-submission MSIX under `artifacts/`. Microsoft signs accepted
production packages. Development certificates, passwords, generated packages,
and generated assets must not be committed.

This is a free, unofficial fan utility and is not affiliated with or endorsed
by Toontown Rewritten, Disney, Discord, or their staff.

