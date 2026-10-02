@echo off
setlocal
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tools\Build-Android-Native.ps1"
exit /b %errorlevel%
