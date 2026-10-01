param(
    [string]$AndroidSdk = $env:ANDROID_HOME,
    [string]$NdkVersion = "25.2.9519653",
    [string]$Abi = "arm64-v8a",
    [int]$Api = 26
)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
if (-not $AndroidSdk) {
    $candidate = Join-Path $env:LOCALAPPDATA "Android\Sdk"
    if (Test-Path $candidate) { $AndroidSdk = $candidate }
}
if (-not $AndroidSdk -or -not (Test-Path $AndroidSdk)) {
    throw "Android SDK not found. Set ANDROID_HOME or install it under %LOCALAPPDATA%\Android\Sdk."
}
$ndk = Join-Path $AndroidSdk "ndk\$NdkVersion"
$toolchain = Join-Path $ndk "build\cmake\android.toolchain.cmake"
if (-not (Test-Path $toolchain)) { throw "Android NDK $NdkVersion not found at $ndk" }

$cmakeCommand = Get-Command cmake -ErrorAction SilentlyContinue
if ($cmakeCommand) {
    $cmake = $cmakeCommand.Source
} else {
    $cmake = Join-Path $AndroidSdk "cmake\3.22.1\bin\cmake.exe"
    if (-not (Test-Path $cmake)) { throw "CMake not found. Install Android SDK CMake 3.22.1 or add cmake to PATH." }
}

$build = Join-Path $root ("Native\build-android-" + $Abi)
& $cmake -S (Join-Path $root "Native") -B $build ("-DCMAKE_TOOLCHAIN_FILE=" + $toolchain) ("-DANDROID_ABI=" + $Abi) ("-DANDROID_PLATFORM=android-" + $Api) "-DCMAKE_BUILD_TYPE=Release"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& $cmake --build $build --target Idas3Unity --parallel 2
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$output = Join-Path $root "Assets\Plugins\Android\arm64-v8a\libIdas3Unity.so"
if (-not (Test-Path $output)) { throw "Build completed but $output was not created." }
Write-Host "Android ARM64 native plugin built successfully:"
Write-Host $output
