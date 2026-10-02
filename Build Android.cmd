@echo off
setlocal
cd /d "%~dp0"
call "%~dp0Build Android Native.cmd"
if errorlevel 1 exit /b %errorlevel%
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tools\Unity-Project.ps1" -Action AndroidBuild
if errorlevel 1 (
  set "BUILD_EXIT=%errorlevel%"
  echo.
  echo Android APK build failed. Showing the last 120 lines of Logs\AndroidBuild.log:
  echo ------------------------------------------------------------------------
  powershell.exe -NoProfile -Command "if (Test-Path -LiteralPath '%~dp0Logs\AndroidBuild.log') { Get-Content -LiteralPath '%~dp0Logs\AndroidBuild.log' -Tail 120 } else { Write-Host 'AndroidBuild.log was not created.' }"
  echo ------------------------------------------------------------------------
  exit /b %BUILD_EXIT%
)
echo.
echo Android APK built successfully:
echo %~dp0Builds\Android\InitialDArcadeStage3-Reimagined.apk
exit /b 0
