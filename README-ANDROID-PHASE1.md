# Initial D Arcade Stage 3 Reimagined — Android Phase 1

This branch contains the first native Android bring-up changes.

## What it changes

- Adds the missing `Native/android/scene_matrix.h` referenced by `unity_scene_capture.cpp`.
- Implements DirectXMath-compatible RH/LH look-at, perspective, and matrix multiplication without DirectXMath.
- Makes `Native/unity_plugin.cmake` able to create `Idas3Unity` when the Win32 `InitialDRemake` executable target does not exist.
- Defines `IDAS3_PORTABLE_SCENE` for the Android native plugin and Unity scene-capture sources.
- Removes D3D11/Win32 libraries from the Android link path.
- Keeps the existing Windows plugin path and D3D11 smoke tests intact.
- Writes the Android shared library to `Assets/Plugins/Android/arm64-v8a/libIdas3Unity.so`.
- Explicitly disables fast-math for the Android plugin because the game simulation is tied to fixed 60 Hz behavior.

## First Android target

Build only `Idas3Unity` through the Android NDK first.

Expected output:

`Assets/Plugins/Android/arm64-v8a/libIdas3Unity.so`

Recommended initial ABI: `arm64-v8a`.

## Known next blockers

1. `Application.streamingAssetsPath` is not a normal filesystem directory on Android. The native code needs a real app-private filesystem path.
2. `Idas3RomGate` currently assumes a desktop `rom` folder beside the executable. Android needs a persistent-storage CHD import/validation flow.
3. Windows-only updater/integration paths need to be bypassed on Android.
4. Unity Android settings still need ARM64 + Vulkan + fixed 60 FPS configuration.

Do not package `gds-0033.chd` in the APK. It remains user-supplied and is validated locally by the existing SHA-256 gate.
