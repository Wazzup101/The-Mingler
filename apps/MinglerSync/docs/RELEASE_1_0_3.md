# Mingler Sync 1.0.3 release candidate

This is candidate source, not a signed or published Microsoft Store update.

## Changes

- Optional, default-off Discord Rich Presence with a fixed app icon and elapsed
  session timer. The preference is independent of manual game-data sync.
- Three-topic About & Help with policy, source and Store links.
- Strict registration-response validation: uncertain replies are not reported as
  success and uploads are never automatically repeated.
- Diagnostic reports omit raw exception text and use fixed categories.
- Scrollable main window preserves the Toon-review area at smaller display sizes.
- Coordinated 1.0.3 executable/package metadata and expanded .NET 10 Windows CI.
- Terms and Privacy explicitly cover Mingler Sync and optional Discord activity.

## Qualification

- 23 core/presence/privacy checks, 32 synthetic HTTP checks and 7 help/layout checks pass.
- The actual candidate's startup, review, safe-demo completion and About screen were checked.
- An explicitly approved live Discord check showed the app name, matching icon and
  elapsed timer, then visibly cleared the activity on stop. Saved consent was unchanged.
- Authenticated Companion upload, signed installation and Store publication are
  not confirmed by these results. Do not distribute an unsigned candidate as the
  normal player download. Existing Store installations are not updated by a source push.

The Microsoft Store signs accepted MSIX submissions; no self-signed certificate
or private signing material is included in this repository.
