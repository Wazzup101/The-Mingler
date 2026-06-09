@echo off
REM Launcher for register-sync.ps1 (avoids PowerShell execution-policy blocks on .ps1)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0register-sync.ps1" %*
exit /b %ERRORLEVEL%
