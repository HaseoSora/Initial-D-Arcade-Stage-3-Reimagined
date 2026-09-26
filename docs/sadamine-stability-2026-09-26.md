# Sadamine tree flicker and car/camera movement

The two September 26 clips show flickering foliage and vertical movement in both bumper and chase views. Source driving traces reproduce a repeating road-contact height residual on the descent. The local fixes address those two causes independently.

## Foliage

Sadamine's imported leaf cards include overlapping front/back faces with different diagonals and silhouette outlines. The existing exact-triangle matcher missed those faces. The importer now checks coplanar opposing triangles for area overlap and uses the existing facing rejection in the production geometry shader. Separate leaves and isolated one-sided cards keep their existing behavior.

All four Sadamine condition packs now mark 722 paired triangles per pack, versus 12 with exact matching. Hakone retains 3,600 paired triangles. Matching runs when the mesh loads; there is no per-frame CPU geometry search.

## Car and cameras

Sadamine now anchors the grounded actor presentation at road height plus the original model-origin offset. Its body uses road height plus the car's ride height directly, without any solver heave. Pitch and roll come from the queried road normal, with a short response for changes in road slope/banking; simulated pitch/roll chatter is excluded. The car and all three camera paths receive this stable pose. Hakone retains the earlier road-relative offset filter.

The original driving actor, collision solver and timing remain unchanged. Missing road contact, airborne separation, race reset and large position discontinuities bypass or reset the correction. Original Arcade Stage 3 courses retain their current presentation. The correction advances at physics ticks, with existing render interpolation between ticks. Actual road elevation and banking are preserved.

## Validation

Evidence is in `Verification/sadamine-bounce-20260926/` (ignored local files):

- `trees-final.log`: 570 checks, including synthetic different-diagonal and partial-silhouette cards, disjoint/separate cards, real meshes from all eight condition packs, and production-shader GPU front/back rejection.
- `imported_road_presentation_tests`: 1,808 checks for a descending road with the measured contact frequency, angle wrap, no-contact/airborne bypass and reset/teleport behavior. Residual RMS ratio: 0.252.
- `natural_chase_camera_tests`: 32,605 checks; worst 30/60/120 Hz difference 0.00542 m.
- `app-1/imported-road-presentation.txt`: 8,933 checks through actual App driving. Sadamine downhill/uphill road-relative frame-step RMS ratios are 0.210 and 0.200, approximately 79–80% attenuation. The sampled driving runs reach 148 and 145 km/h. Hakone is also exercised; original Akina remains outside the correction.
- Chase/bumper camera inputs and all three camera render paths preserve original driving words. `app-1/report.json` records the passing Unity player run, complete shutdown and nine captures, including every Sadamine condition/direction.

The evidence above records the first, partially filtered version. The follow-up ground anchoring supersedes it on Sadamine. Evidence for that version is in `Verification/sadamine-ground-anchor-20260926/`:

- 4,211 unit checks, including identical displayed height/pitch/roll with and without simulated chatter on a descending, banked road; airborne/no-contact and teleport handling.
- Actual App driving reaches the finish on the full 6,827.77 m route in each direction (30,782 and 30,778 physics ticks), with zero unanchored ticks. The automated driver limits speed to exercise every curve safely; diagnostic time extensions keep that slower test running without publishing a time.
- 81,942 collision samples cover the center and both sides of the road every 0.5 m, in both directions. Every sample has valid ground and excludes injected solver height/tilt chatter. The largest road height step is 0.120 m over a 0.5 m interval; there are no large height seams in that survey.
- Wet driving in both directions reaches approximately 139 km/h, alongside original Akina/Hakone regression checks, source-physics isolation and all three camera render paths.
- `app-2/report.json` and `app-2/imported-road-presentation.txt` record the final player checks and screenshots. At the floating-point precision used by the road coordinates, grounded heave from the solver is excluded rather than filtered.

These are scripted driving and graphics checks, not a replay of the user's exact inputs or a live multiplayer test. Genuine road elevation and airborne movement remain visible.

Local desktop deployment is recorded separately in `desktop-deployment.json`; this work does not create a GitHub release.

## Roadside foliage follow-up (13:04 clip)

The user's follow-up identifies the flickering trees. The first fix only tagged
instanced meshes whose manifest kind is `tree`. Sadamine also has fixed roadside
forest and bush panels under `crs_a`, which were still drawing their overlapping
front and back faces. An exact triangle audit found 8,185 opposite-winding pairs
in `bush_a1` and 553 in `forest_a` in the dry daytime pack.

The same facing correction now covers Sadamine's bush, forest, cherry-tree and
corner-grass materials, including wet weather's `forest_a3`. It tags 22,676,
22,677, 22,685 and 22,692 roadside triangles in the day dry, day wet, night dry
and night wet packs respectively. All 159 eligible meshes per pack are checked.
The 722 instanced-tree triangles on Sadamine and 3,600 on Hakone are preserved.
Buildings and other non-foliage materials retain their behavior.

The overlap search now sorts triangle bounds along the mesh's longest axis
before clipping potential pairs. This limits work for the larger roadside
meshes and runs only when loading them, not each frame.

Evidence is in `Verification/sadamine-clip-130440-20260926/`:

- `trees-check.log`: 1,527 passing geometry/GPU checks across all eight imported
  course packs, including opposite diagonals, partial silhouettes, separated
  cards, material selection and front/back production-shader rendering.
- `trees-final2/report.json`: scripted roadside contact in both steering
  directions, three contact durations, bumper/chase captures, and camera-pan
  and subpixel-motion sequences at the reported start area. The corresponding
  baseline diagnostic skips only the new roadside tags.
- Both player runs pass 149 checks with 62 captures. For the same subpixel
  camera movement, mean adjacent-frame RGB change in the nearby right-hand
  foliage falls from 1.366 to 0.162 (0–255 scale). Across the whole foliage band
  it falls from 0.296 to 0.120. The difference images show the disappearing
  speckled overlap on that roadside panel. This measures this controlled view,
  not a guarantee that all texture-edge shimmer is eliminated everywhere.

The investigation initially considered roadside collision and the black shadow
strip. Those experimental camera/shadow changes were discarded. This follow-up
changes foliage coverage; it does not change camera position, clipping planes,
shadow strength or driving physics.

The tested files were installed into `C:/Users/Chris/Desktop/Current`. The
deployment compared 18,044 approved game files, backed up and replaced three
changed files, and verified their hashes. The installed executable passed the
same 149 checks and 62 captures (`desktop-smoke/report.json`). Existing saves,
ROMs and custom music were excluded. No GitHub release was made.
