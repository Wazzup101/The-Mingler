@echo off
REM Launcher for register-sync.ps1 (avoids PowerShell execution-policy blocks on .ps1)
if "%~1"=="" (
  echo.
  echo  The Mingler - register-sync
  echo  ===========================
  echo.
  echo  Double-clicking this file does not work by itself.
  echo.
  echo  1. In Discord, run /register link
  echo  2. Copy the full command from step 4 ^(starts with .\register-sync.cmd^)
  echo  3. Shift+right-click this folder -^> Open in Terminal / PowerShell
  echo  4. Paste the command and press Enter
  echo.
  echo  See README.txt in this folder for more help.
  echo.
  pause
  exit /b 1
)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0register-sync.ps1" %*
exit /b %ERRORLEVEL%
