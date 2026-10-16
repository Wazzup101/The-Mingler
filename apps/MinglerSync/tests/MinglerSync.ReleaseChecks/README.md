# Backend response release checks

Run separately from the existing core checks:

```powershell
dotnet run --project tests/MinglerSync.ReleaseChecks/MinglerSync.ReleaseChecks.csproj --configuration Release -p:UseSharedCompilation=false
```

This console harness uses the real `RegisterSyncClient` against temporary
loopback-only TCP servers. All payloads and bearer values are synthetic. It
does not connect to a real game, Discord, the public sync service, or a database.

Current qualification on 2026-10-15: **32 checks, 32 passed, 0 failed** after
repairing the production response validator. These checks are included in the
solution and Windows CI. They do not prove a live authenticated sync.

Confirmed pass: valid success parsing, POST/Bearer/Companion request contract,
empty-review rejection, and safe failure for HTTP 302/400/401/403/413/422/429/500/503.

Historical receipt before the repair: **17 checks, 11 passed, 6 failed**.
HTTP 200 with HTML, `{}`, explicit `ok:false`, a negative success count, an
invalid count type, and HTTP 204 without confirmation were the six failures.
All six now return safe unconfirmed results instead of success or an exception.

Additional coverage checks null/array roots, schema types, count/list mismatch,
missing receipt fields, overflow/fractional counts, malformed Toon receipts,
oversized bodies, JSON media types and explicit zero counts. The app neither
invents confirmation from submitted profiles nor automatically retries a POST.
