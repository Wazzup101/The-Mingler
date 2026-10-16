# Mingler Sync: publisher Rich Presence setup

This is a desktop-only, opt-in feature. It does not change The Mingler bot's
presence, require a bot invite, or change registration/sync behavior. The public
configuration uses the separate **Mingler Sync** application, not The Mingler bot:
Discord uses the application's name for the activity title.

## Configured publisher application

- Public Application ID: `1560434306336432270` (Personal ownership).
- Fixed large-image asset key: `mingler_sync`.
- The application icon and Rich Presence asset use `packaging/mingler-bot-icon.png`.
- The presence preference remains off by default; configuration is not user consent.
- This is a development build, not an installed or Microsoft Store update.
- An owner-approved live test on October 15, 2026 confirmed Discord accepted the
  activity and visibly displayed Mingler Sync with the matching icon and elapsed
  timer. The activity visibly disappeared after the bounded test ended. No saved
  preference was changed. This does not replace authenticated game-sync or Store
  installation qualification.

## One-time owner setup

1. Open the [Discord Developer Portal](https://discord.com/developers/applications)
   and create an application named exactly **Mingler Sync**. A new bot is not needed.
2. Under **General Information**, set its app icon using
   `packaging/mingler-bot-icon.png`, the existing source for this Windows app's icon.
3. Under **Rich Presence / Art Assets**, upload that same image with the key
   **mingler_sync**. Use a fixed key, not alternating or map-specific images.
4. Copy the public **Application ID** into `discord-presence.json`:

   ```json
   {
     "applicationId": "1560434306336432270",
     "largeImageKey": "mingler_sync"
   }
   ```

   Never put a bot token, client secret, account token, or pairing token here.
   This file is copied beside the executable in build and publish output. All
   players use the publisher's configured application; players do not create apps.
5. Rebuild and verify using Discord desktop before creating a signed public
   release. No release, Store upload, installation, or bot deployment is performed
   by these steps automatically. Application ID and image alone do not prove
   Discord accepted or rendered the presence.

## Behavior and privacy

- Fresh install, missing preference, unknown schema, or invalid preference: **off**.
- Saved opt-in: reconnect while the window is open, without starting Discord.
- Timer: Unix start timestamp at enable/relaunch, preserved over reconnects.
- Activity: Playing (`type: 0`), fixed large image `mingler_sync`, hover text
  `Mingler Sync`, and elapsed time. No details/state or gameplay/user data.
- Disable/close: send a null activity, wait briefly for acknowledgement, and close
  the IPC connection. If Discord is gone, closing the connection is best-effort cleanup.
- Uses local named pipes `discord-ipc-0` through `discord-ipc-9`. No listener,
  HTTP request, firewall change, OAuth, user token, or new dependency is needed.
- When off, no Discord connection/retry loop is started. Companion scanning and
  uploads remain explicit button actions, unrelated to this preference.
- User preference contains only schema version and consent boolean. It contains no
  credentials or account/player data. Save failures are shown; a failed opt-out save
  applies only to the current session and the user is told to check next launch.

## Verification checklist

Run `dotnet build MinglerSync.sln --configuration Release`, then
`dotnet run --project tests/MinglerSync.Core.Tests --configuration Release`.
Tests use unique fake pipe names, never a real Discord connection. They cover
default-off/invalid settings, opt-out persistence, public configuration validation,
minimal payload, framing, ping/pong, acknowledgements, rejection, reconnect timer,
clear-on-disable/close, and oversized frames.

Only when the user chooses to opt in, manually:

1. Use a fresh Windows app preference: checkbox off, no app-initiated activity.
2. Open Discord desktop and opt in. Confirm **Playing Mingler Sync**, matching
   main icon, and a running elapsed timer. Check from another account if visibility matters.
3. Restart Discord with the app still open. Confirm reconnect and original timer.
4. Disable the option, then close/reopen the app. Confirm it remains off.
5. Enable, close the app, and confirm its activity disappears. Reopen with the saved
   opt-in and confirm a new timer. Registration and safe demo must still work unchanged.

Reference: [Discord's local RPC protocol and SET_ACTIVITY](https://docs.discord.com/developers/topics/rpc).
