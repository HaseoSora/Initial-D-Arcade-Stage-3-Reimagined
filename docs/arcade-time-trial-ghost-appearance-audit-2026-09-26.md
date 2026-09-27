# The Arcade Time Trial ghost appearance

Scope: the blue ghost visual in the supplied Season 5 (3.10.00 Rev 1 +A) files. Read-only inspection; this audit does not add a ghost to the Unity game or publish a release.

## Confirmed implementation

The race Blueprint creates a separate, extremely simple blue ghost mesh. This is a dedicated visual, rather than a full normally painted opponent car.

- `BP_CarInRace.BuildCarParts` checks `GetPlayMode() == 1 && IsGhostCar` and skips the normal parts-building path for that case. The executable's serialized enum table identifies `EPlayMode::TimeAttackMode` as 1 (adjacent entries identify StoryMode as 0 and LanBattleMode as 4).
- `BP_CarInRace.SetGhostVisibility` creates a static-mesh component when needed, stores it in `ghostCarComp`, and calls `SetStaticMesh(SM_GhostCar_00)`. Its visibility logic calls `SetHiddenInGame` on that component.
- `ToggleGhost` passes the inverse of `bVisibleGhost` into `SetGhostVisibility`.
- These three Blueprint functions were decoded through their complete serialized script buffers: 1,169, 2,800 and 35 bytes respectively. This confirms the stored logic; it is not a live execution trace.

The official [Time Trial guide](https://initiald.sega.jp/inidac/guide/gamemode/) describes showing the player's earlier run with Up. The Down-button line guide is a separate feature.

## Mesh and appearance

Asset paths below are relative to `GameProject/Content/IND/Assets/CAR/`.

| Asset | Inspected content |
|---|---|
| `Common/Mesh/SM_GhostCar_00` | Selected by the race Blueprint. 8 triangles form a flat car-shaped silhouette, about 1.62 m wide and 4.02 m long, approximately 1.52 cm above a separate backing layer. |
| `Common/Mesh/SM_GhostCar_00_old` | Retained older alternative with an 82-triangle 3D car body. Not the mesh selected by the inspected creation call. |
| Both meshes' backing layer | 12 triangles, about 2.24 m by 4.86 m, with a separate material slot. |
| `COMMON_TEX/GhostCar/T_GhostCar_00_C` | 32×32 RGBA cyan-to-blue texture: RGB ranges from (0,255,255) to (0,170,255), with alpha 92/255 throughout. |
| `COMMON_TEX/GhostCar/T_GhostCar_00_Shadow` | 32×64 RGBA blue backing texture: RGB (0,127,192), alpha ranging from 89 to 255. |

The two material instances are `MI_GhostCar_00` and `MI_GhostCar_Shadow_00`, under `CAR_Material/Instance_Material/MI_Common/`. Both inherit `CAR_Material/Master_Material/M_CAR_ClearParts_00`, whose saved blend mode is `BLEND_Translucent` and lighting mode is `TLM_Surface`.

The instances explicitly set metallic and specular intensity to zero. They also contain saved `Opacity = 0` and `FallOffSoftness = 0` values. The follow-up below recovers their meaning from the compiled shader: for those saved parameters, final material opacity is **one minus texture alpha**. The material export from UE Viewer does not reproduce that shader. Exact original lighting and any runtime overrides remain unverified.

The selected mesh is genuinely flat in its exported geometry. The older `_old` asset is three-dimensional. Therefore an assertion that the selected Time Trial effect uses a full 3D car with an edge-detection or Fresnel outline is not supported by this audit. A live comparison is still needed to establish exact blending, brightness and any additional native rendering behavior.

## Equivalent for our game

For the requested similar **blue outlined car model**, use a simplified car mesh with a transparent cyan body and a brighter blue silhouette/edge material. Keep it as a separate visual object, without vehicle physics, car collision, engine audio, headlights or normal vehicle shadows. Its transform can come from the selected recorded run.

Use depth testing so scenery can occlude it. A gentle fade near the player's car would keep the road visible when the two overlap. The outline treatment and proximity fade are proposed behavior for our version, not verified properties of the source game. A flat blue footprint would follow the inspected Season 5 asset more literally; a 3D outline better matches the model appearance requested here.

Playback storage, record selection and interpolation were outside this narrowed visual audit. No new binding or gameplay behavior has been installed.

## Evidence

Local source: `D:/Initial D games/Initial D The Arcade Season 5 (3.10.00 Rev 1 +A)(2025-09-20)(JPN,EXP)[Sega ALLS][TP]`.

Audit output: `Verification/arcade-ta-ghost-audit-20260926/`:

- `inventory.json`: source archive, extracted-file sizes and SHA-256 hashes; archive payload SHA-1 validation passed.
- `assets.json`: decoded mesh bounds, materials, texture references, Blueprint names and tagged properties.
- `ghost-functions.json`: decoded Blueprint instruction trees, fully consumed buffers and no unresolved instructions for the three scoped functions.
- `play-mode-enum-evidence.json`: executable enum-table offsets and values.
- `Export/`: original decoded PNG textures and glTF meshes for inspection; exported glTF material colors are placeholders, not an authoritative in-game rendering.
- `asset-errors.json`: empty for the eight scoped packages.

The game archives and executables were read only. No source game was run and no desktop build was changed for this audit.

## Raw import preview

`Verification/arcade-ta-ghost-audit-20260926/Preview/` now contains driving and detail captures of both meshes rendered in Unity with the project's real Tsubaki course loader and course shader. The original geometry, UVs, texture RGB/alpha and saved RGB tint were used. glTF handedness and texture-coordinate orientation were converted for Unity.

The temporary ghost material uses unlit source-alpha blending. This is an inspection approximation, not a reconstruction of `M_CAR_ClearParts_00`. In particular, the prominent blue rectangle from the backing layer is not evidence that The Arcade's finished ghost looks like that. The original material's alpha interpretation must be recovered before promising an exact visual match. Neither a new outline effect nor a glow was invented for these captures.

All four initial captures completed successfully with Unity exit code 0. Preview-only editor scripts and shader were moved out of `Assets` into `Preview/Source` afterward. These initial images have the wrong alpha interpretation and are superseded by `Preview/RecoveredMaterial/`. No game build, runtime feature, save, desktop installation or GitHub release was changed.

## Follow-up: why two meshes are present

The evidence supports a retained legacy 3D asset plus a deliberately selected flat Time Trial marker. It does **not** establish the designers' exact reason for choosing the flatter design.

1. Both source installations contain byte-for-byte identical copies of the two meshes, their two material instances, their two textures and the parent material: 14 matching package/payload files. This is not a change introduced between the supplied Season 3 and Season 5 versions. `version-comparison.json` records the hashes.
2. The Season 3 race Blueprint also references `SM_GhostCar_00`. Its `BuildCarParts`, `SetGhostVisibility` and `ToggleGhost` functions were fully decoded (1,128, 2,777 and 35 serialized bytes). Both versions create the flat mesh in the Time Trial ghost path. The visibility functions differ in other details; they are not being claimed byte-identical.
3. A scan of **59,379 active package headers**, merging base and patch archives and excluding deleted entries, found only three packages containing the ghost mesh name: the two mesh packages themselves and `BP_CarInRace`. The race Blueprint imports the current mesh only. No other package header imported or named the `_old` mesh. There were no extraction/parser errors. This is a package-reference audit, not proof against arbitrary dynamically constructed native asset paths.
4. Neither `GameProject-Win64-Shipping.exe` nor `IndRun.dll` contained a literal reference to either mesh name in UTF-8 or UTF-16. The shipped Season 3 asset-manager configuration scans the entire common-car mesh folder recursively as `CarCommon`; folder inclusion is therefore not evidence of a mesh being selected for races. This configuration is a packaging clue, not a recovered build history.
5. Both meshes have an identical 12-triangle backing layer. The body/silhouette was separately authored: the older body has 82 triangles and actual height; the current one has 8 triangles at a single height. The current triangulation is not merely the older mesh with its height scaled to zero.

**Best-supported interpretation:** the older 3D ghost was superseded, and its asset remained in the common mesh package. The flat version offers a less obstructive marker of the earlier run's road position. Improved visibility is a design inference from the geometry, not a confirmed Sega statement. The inspected files and official guide do not document when or why that choice was made. A particular performance motivation, automatic detail-level switch, or intentional selectable alternative should not be asserted.

## Recovered transparency and corrected preview

`read_ghost_shader.py` recovered and disassembled **43 compressed DXBC programs**, including 20 pixel shaders, from the parent material. It also decoded the material's inline uniform-expression table, which maps the fourth and fifth scalar parameters to `Opacity` and `FallOffSoftness` (packed float4 index 5, components x and y). All 20 pixel variants contain the same inverse-alpha opacity calculation.

The relevant equation is:

```
materialOpacity = saturate(
    (1 + textureAlpha * (Opacity - 1))
    / pow(max(dot(viewDirection, surfaceNormal), 0), FallOffSoftness)
)
```

For the saved ghost instances (`Opacity=0`, `FallOffSoftness=0`) and a visible front-facing surface, this simplifies to `1 - textureAlpha`. The body texture's alpha 92/255 becomes opacity 163/255, approximately 64%. The backing texture's alpha-255 border becomes fully transparent. The solid blue rectangle shown in the first preview came from the inspection shader using texture alpha directly; it was not a faithful representation of the original material.

The corrected inspection material implements this equation and the parent's back-face culling. Four new captures in `Preview/RecoveredMaterial/` completed with Unity exit code 0. They still use approximate unlit RGB shading rather than the original UE environment lighting, and do not claim complete visual parity or live source-game verification. Original geometry and textures were retained. Updated preview helpers are archived in `Preview/RecoveredSource/` outside the game's Assets folder.

Reproduction evidence includes `Shaders/uniform-expressions.json`, `Shaders/recovered-shaders.json`, `Shaders/opacity-cross-check.json` and the `.asm` disassemblies. For example, `decompressed-161072.asm` samples texture alpha into `r4.w`, evaluates `1 + r4.w * (cb3[5].x - 1)`, divides by the view/normal power controlled by `cb3[5].y`, and writes that result to output alpha. Static local-vertex-factory shader `decompressed-53397.asm` transforms source positions through the instance and view matrices without a material height/extrusion term.
