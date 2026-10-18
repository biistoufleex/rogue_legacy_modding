@echo off
rem Rogue Legacy (unofficial mod) - double-click installer.
rem Windows blocks PowerShell scripts downloaded from the Internet by default, so this runs
rem install.ps1 with a one-time execution policy bypass (nothing is changed system-wide).
title Rogue Legacy mod - install
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0install.ps1" %*
echo.
pause
