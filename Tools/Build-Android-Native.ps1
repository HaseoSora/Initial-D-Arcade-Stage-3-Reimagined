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

$cmakeCommand = Get-Command cmake -ErrorAction SilentlyContinue
if ($cmakeCommand) {
    $cmake = $cmakeCommand.Source
} else {
    $cmake = $null
    $cmakeRoot = Join-Path $AndroidSdk "cmake"
    if (Test-Path -LiteralPath $cmakeRoot) {
        $cmakeCandidate = Get-ChildItem -LiteralPath $cmakeRoot -Directory | Sort-Object Name -Descending | ForEach-Object { Join-Path $_.FullName "bin\cmake.exe" } | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
        if ($cmakeCandidate) { $cmake = $cmakeCandidate }
    }
    if (-not $cmake) { throw "CMake not found. Install Android SDK CMake or add cmake to PATH." }
}

Write-Host "Android SDK: $AndroidSdk"
Write-Host "Android NDK: $AndroidNdk"
Write-Host "CMake:      $cmake"

$build = Join-Path $root ("Native\build-android-" + $Abi)
& $cmake -S (Join-Path $root "Native") -B $build ("-DCMAKE_TOOLCHAIN_FILE=" + $toolchain) ("-DANDROID_ABI=" + $Abi) ("-DANDROID_PLATFORM=android-" + $Api) "-DCMAKE_BUILD_TYPE=Release"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& $cmake --build $build --target Idas3Unity --parallel 2
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$output = Join-Path $root "Assets\Plugins\Android\arm64-v8a\libIdas3Unity.so"
if (-not (Test-Path $output)) { throw "Build completed but $output was not created." }
Write-Host "Android ARM64 native plugin built successfully:"
Write-Host $output
