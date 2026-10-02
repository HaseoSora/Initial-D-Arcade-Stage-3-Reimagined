param(
    [string]$AndroidSdk = $env:ANDROID_HOME,
    [string]$AndroidNdk = $env:ANDROID_NDK_HOME,
    [string]$NdkVersion = "25.2.9519653",
    [string]$Abi = "arm64-v8a",
    [int]$Api = 26
)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

function Find-UnityAndroidToolchain {
    $editors = @()
    if ($env:IDAS3_UNITY_EDITOR -and (Test-Path -LiteralPath $env:IDAS3_UNITY_EDITOR)) {
        $editorDir = Split-Path -Parent $env:IDAS3_UNITY_EDITOR
        $versionDir = Split-Path -Parent $editorDir
        $editors += $versionDir
    }
    foreach ($hubRoot in @("C:\Program Files\Unity\Hub\Editor","D:\Program Files\Unity\Hub\Editor")) {
        if (Test-Path -LiteralPath $hubRoot) {
            $editors += Get-ChildItem -LiteralPath $hubRoot -Directory | Sort-Object Name -Descending | Select-Object -ExpandProperty FullName
        }
    }
    # Standalone UnitySetup64 installs commonly use Program Files\Unity\Editor
    # instead of the Hub's versioned directory layout.
    foreach ($standalone in @("C:\Program Files\Unity","D:\Program Files\Unity")) {
        if (Test-Path -LiteralPath (Join-Path $standalone "Editor\Unity.exe")) {
            $editors += $standalone
        }
        if (Test-Path -LiteralPath $standalone) {
            $editors += Get-ChildItem -LiteralPath $standalone -Directory -ErrorAction SilentlyContinue |
                Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName "Editor\Unity.exe") } |
                Select-Object -ExpandProperty FullName
        }
    }
    foreach ($editor in $editors | Select-Object -Unique) {
        $android = Join-Path $editor "Editor\Data\PlaybackEngines\AndroidPlayer"
        $sdk = Join-Path $android "SDK"
        $ndk = Join-Path $android "NDK"
        if ((Test-Path -LiteralPath $sdk) -and (Test-Path -LiteralPath (Join-Path $ndk "build\cmake\android.toolchain.cmake"))) {
            return @{ Sdk=$sdk; Ndk=$ndk; Editor=$editor }
        }
    }
    return $null
}

$unityToolchain = Find-UnityAndroidToolchain

if (-not $AndroidSdk) {
    $candidate = Join-Path $env:LOCALAPPDATA "Android\Sdk"
    if (Test-Path -LiteralPath $candidate) { $AndroidSdk = $candidate }
}
if ((-not $AndroidSdk -or -not (Test-Path -LiteralPath $AndroidSdk)) -and $unityToolchain) {
    $AndroidSdk = $unityToolchain.Sdk
}
if (-not $AndroidSdk -or -not (Test-Path -LiteralPath $AndroidSdk)) {
    throw "Android SDK not found. Install Android Build Support (SDK/NDK Tools + OpenJDK) for Unity 6000.6 in Unity Hub, or set ANDROID_HOME."
}

if (-not $AndroidNdk) {
    foreach ($candidate in @(
        (Join-Path $AndroidSdk ("ndk\" + $NdkVersion)),
        $env:ANDROID_NDK_ROOT,
        $(if ($unityToolchain) { $unityToolchain.Ndk } else { $null })
    )) {
        if ($candidate -and (Test-Path -LiteralPath (Join-Path $candidate "build\cmake\android.toolchain.cmake"))) {
            $AndroidNdk = $candidate
            break
        }
    }
}
if (-not $AndroidNdk) {
    throw "Android NDK not found. Install NDK $NdkVersion in your Android SDK or install Unity Android Build Support with Android SDK & NDK Tools."
}
$toolchain = Join-Path $AndroidNdk "build\cmake\android.toolchain.cmake"
if (-not (Test-Path -LiteralPath $toolchain)) { throw "Android NDK toolchain file not found at $toolchain" }

function Test-CMakeVersion([string]$Path) {
    if (-not $Path -or -not (Test-Path -LiteralPath $Path)) { return $false }
    try {
        $line = & $Path --version 2>$null | Select-Object -First 1
        if ($line -match 'cmake version ([0-9]+)\.([0-9]+)\.([0-9]+)') {
            $version = [Version]::new([int]$matches[1],[int]$matches[2],[int]$matches[3])
            return $version -ge [Version]::new(3,24,0)
        }
    } catch {}
    return $false
}

$cmake = $null
$cmakeCandidates = @()
$cmakeCommand = Get-Command cmake -ErrorAction SilentlyContinue
if ($cmakeCommand) { $cmakeCandidates += $cmakeCommand.Source }
$cmakeCandidates += @(
    "C:\Program Files\CMake\bin\cmake.exe",
    "C:\Program Files (x86)\CMake\bin\cmake.exe"
)
$cmakeRoot = Join-Path $AndroidSdk "cmake"
if (Test-Path -LiteralPath $cmakeRoot) {
    $cmakeCandidates += Get-ChildItem -LiteralPath $cmakeRoot -Directory |
        Sort-Object Name -Descending |
        ForEach-Object { Join-Path $_.FullName "bin\cmake.exe" }
}
foreach ($candidate in $cmakeCandidates | Select-Object -Unique) {
    if (Test-CMakeVersion $candidate) { $cmake = $candidate; break }
}
if (-not $cmake) {
    throw "CMake 3.24 or newer is required. Install current CMake (for example: winget install Kitware.CMake) and rerun Build Android.cmd."
}

# Android builds use Ninja. Avoid CMake's Windows default (NMake), which
# requires the Visual Studio nmake tool and is not part of the Android SDK.
$ninja = $null
$ninjaCommand = Get-Command ninja -ErrorAction SilentlyContinue
if ($ninjaCommand) { $ninja = $ninjaCommand.Source }
if (-not $ninja -and (Test-Path -LiteralPath $cmakeRoot)) {
    $ninja = Get-ChildItem -LiteralPath $cmakeRoot -Directory |
        Sort-Object Name -Descending |
        ForEach-Object { Join-Path $_.FullName "bin\ninja.exe" } |
        Where-Object { Test-Path -LiteralPath $_ } |
        Select-Object -First 1
}
if (-not $ninja) {
    throw "Ninja was not found. Unity Android SDK normally includes ninja.exe under SDK\cmake\<version>\bin."
}

Write-Host "Android SDK: $AndroidSdk"
Write-Host "Android NDK: $AndroidNdk"
Write-Host "CMake:      $cmake"
Write-Host "Ninja:      $ninja"

$build = Join-Path $root ("Native\build-android-" + $Abi)

# If an earlier configure used NMake (or any other generator), discard that
# cache so CMake can cleanly switch this build directory to Ninja.
$cache = Join-Path $build "CMakeCache.txt"
if (Test-Path -LiteralPath $cache) {
    $generatorLine = Select-String -LiteralPath $cache -Pattern '^CMAKE_GENERATOR:INTERNAL=' -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $generatorLine -or $generatorLine.Line -ne 'CMAKE_GENERATOR:INTERNAL=Ninja') {
        Write-Host "Removing stale non-Ninja CMake cache..."
        Remove-Item -LiteralPath $build -Recurse -Force
    }
}

& $cmake -G Ninja -S (Join-Path $root "Native") -B $build ("-DCMAKE_MAKE_PROGRAM=" + $ninja) ("-DCMAKE_TOOLCHAIN_FILE=" + $toolchain) ("-DANDROID_ABI=" + $Abi) ("-DANDROID_PLATFORM=android-" + $Api) "-DCMAKE_BUILD_TYPE=Release"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& $cmake --build $build --target Idas3Unity --parallel 2
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$output = Join-Path $root "Assets\Plugins\Android\arm64-v8a\libIdas3Unity.so"
if (-not (Test-Path $output)) { throw "Build completed but $output was not created." }
Write-Host "Android ARM64 native plugin built successfully:"
Write-Host $output
