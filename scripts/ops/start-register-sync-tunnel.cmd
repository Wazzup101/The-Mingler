@echo off
REM Start Cloudflare Tunnel for mingler.cc register sync (manual mode).
REM Normally the bot starts this automatically when register_sync.tunnel_auto_start is true.
REM Use this script only if tunnel_auto_start is false or you are testing cloudflared alone.
setlocal
cd /d "%~dp0"
echo Starting tunnel: sync.mingler.cc -^> http://127.0.0.1:3847
echo Edit cloudflared-mingler.cc.yml credentials-file before first use.
echo.
cloudflared tunnel --config "%~dp0cloudflared-mingler.cc.yml" run mingler-register-sync
endlocal
