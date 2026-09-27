# Season 5 tachometers and ornaments

## Collection

The recovered Season 5 collection adds 26 finished tachometers and 34 actual 3D
ornaments. The settings now contain 113 imported tachometers plus Original,
and 314 ornaments plus Off. Existing saved selection IDs remain unchanged.

New tachometer source IDs are 90–112 and 115–117: Pop Team Epic, Street, both
Quintuplets sets, Retro Yankee, Evil Eye, MF Ghost, Infinity, the Touhou
silhouette set, Kaiji, Teiai, Bog, Kiryu, Majima and Initial D 30th Anniversary.
Names in the catalog retain the Japanese source names alongside display names.

The new ornaments include seven cars, six course plaques, ten quote charms,
Popuko and Pipimi, five Quintuplets charms, Nobuhiko, Kyoko, glasses and a phone.
The Manazuru black plaque uses the recovered Manazuru mesh; its original table
incorrectly references the Usui Snow mesh.

Source placeholder meters 113/114 and ornaments 779/780 are intentionally absent.
The four existing chain families cover the new ornaments; there is no fifth
chain to import. Menu thumbnails are not substituted for ornament geometry.

## Runtime work

- Original artwork, source layer geometry, texture dependencies, atlas cells,
  material parameters, animation key values and playback ranges are retained.
- Exact cooked frame and day/night needle registrations override template
  brushes. This includes Kiryu's frame filenames without the usual day suffix.
- RPM, speed, gear, AT/MT, accelerator/brake, shift warnings and graded drift
  lights use the existing live telemetry. Street's steady drift lights use
  their completed entry animation because that widget has no Stay track.
- Character sway, scrolling shine, rotating rings, eye motion, auras,
  distortion, animated masks and the gear-change dice run on the presentation
  clock. Pause and seek do not advance their time; motion is independent of FPS.
- The three original eye clips play as a deterministic playlist. This retains
  all three clips while avoiding random changes when recreating a HUD preview.
- The new ornaments use the existing flexible-chain simulation, rebound,
  lateral/forward swing, twist and display interpolation. Their recovered
  diffuse/specular/normal textures are bound to the production renderer.
  Additional material maps and original parameter names remain in the catalog.
- Updated Season 5 gear/drift bindings for existing meters 32, 39, 46–48 and 62
  are included. Shared source texture updates are included as well.

## Limits of the recovery

This is a Unity reconstruction, not execution of the original Unreal shaders.
Cooked graphs are incomplete. Procedural effects and lighting use the original
textures/parameters with reconstructed operators; exact arcade shader parity
has not been established. The original eye clip selection was randomized; the
port uses a stable playlist. Source spline tangents are not decoded, so retained
animation keys use linear interpolation.

Corner-speed readouts, the low-RPM/downshift recommendation and speed-event
bursts retain their artwork and key data but remain inactive: their original
activation inputs are not present in the port's HUD telemetry. These are not
claimed as working animations. Unknown packed ornament material channels are
retained without inventing their meaning. Native ornament mounting transforms
and pendulum behavior were not recovered; the existing fitted attachment and
chain solver are used.

## Reproduction and validation

Run `Tools/Import-ArcadeSeason5.py --audit
Verification/arcade-s5-audit-20260926` with the local recovered audit and original
S3 HUD recovery available. The incremental import checks for unresolved
textures, preserves unrelated entries and records private source-file evidence
under `Verification/arcade-s5-import-20260926`.

`Idas3Season5Build.VerifyOnly` runs the new source-layer behavior checks, the
complete four-grade drift suite, existing meter regressions, production GPU
rendering for every meter/ornament and flexible-chain checks.
`Idas3Season5Build.VerifyAndBuild` then rebuilds the local Windows player.

No game archive or ROM is added by this import. No release is published by
these entrypoints.

Verified on Windows/D3D11, 26 September 2026:

- 565 focused Season 5 behavior checks and 2,927 graded-drift checks passed.
- All 113 meters passed 1,190 GPU cases and 292,069 catalog checks, alongside
  the existing alignment, audio, Halloween, Miku and Steampunk regressions.
- All 314 ornaments passed 1,570 static poses and 1,502 flexible-chain GPU
  frames. Resource-lifetime, pause and interpolation checks passed.
- The standalone game passed 1,303 checks with isolated saves: menu scrolling,
  new selections, Apply/reload/Cancel, movement/resizing, and live native racing
  with Street, Chibi, Evil Eye, Kaiji, Bog and Anniversary meters plus four new
  ornaments. The original ROM gate was satisfied using an existing local ROM;
  its temporary test link was removed afterward.
- The local Windows player was rebuilt at `Builds/Current/InitialDUnity.exe`.

Private logs, manifests, original constructor evidence and actual gameplay
captures are in `Verification/arcade-s5-import-20260926`.
