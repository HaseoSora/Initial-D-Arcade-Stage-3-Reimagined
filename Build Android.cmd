@echo off
setlocal
cd /d "%~dp0"
call "%~dp0Build Android Native.cmd"
if errorlevel 1 exit /b %errorlevel%
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tools\Unity-Project.ps1" -Action AndroidBuild
if errorlevel 1 (
  echo.
  echo Android APK build failed. See Logs\AndroidBuild.log
  exit /b %errorlevel%
)
echo.
echo Android APK built successfully:
echo %~dp0Builds\Android\InitialDArcadeStage3-Reimagined.apk
exit /b 0
