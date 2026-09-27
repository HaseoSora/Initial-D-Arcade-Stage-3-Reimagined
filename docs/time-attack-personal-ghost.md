# Personal-best Time Attack ghost

Gameplay > Time Attack Ghost toggles the blue Arcade ghost. It defaults to On,
including when loading an older options file. Apply persists the choice; hiding
the ghost does not turn off best-run capture. Existing camera choices and replay
recording options retain their behavior.

Each save owns `driver_profiles_v1/ghosts_v1/course_<id>_<direction>_<weather>.idghost`.
The fastest completed Time Attack wins across cars and transmissions. Day/night
shares the record, matching the game's Time Attack record partitions. Original
and imported courses use distinct catalog IDs. Deleting a save removes its ghosts
with the rest of that save. Unslotted play has its own legacy profile directory.

Only completed, non-timeout production Time Attacks can update these files.
Ties, slower runs, incomplete/truncated recordings, battles and replay playback
cannot replace a ghost. Saves use a complete temporary file and replacement with
the previous file retained until replacement succeeds. Invalid ghost files are
ignored. Optional replay recording and community sharing are independent.

Old profile record times alone cannot reconstruct a driven trajectory. Previous
global CSV files have no save ownership, so they are not assigned to a selected
player. A first completed run creates a ghost; subsequent faster completed runs
replace it. "Best saved run" therefore means the fastest available trajectory for
that save, not a reconstructed run for a historical time with no recording.

The host draws a presentation-only mesh with no collider, lights, audio, AI or
physics owner. Poses come from the native 60 Hz race recording and interpolate
with the same fixed-step clock as the player's car. Pause, restart, scene exit and
the ghost's finish all use race state rather than an independent wall clock.

The mesh and two textures in `Assets/Resources/TimeAttackGhost` come from the
audited current Arcade asset `SM_GhostCar_00`, not its unused `_old` 3D variant.
Mesh.json retains the previously audited Unity coordinate/winding conversion.
Material 0 is the backing gradient, material 1 the car-shaped footprint. The
shader uses source RGB tints and the recovered opacity `1 - texture alpha`.
Lighting is an unlit Unity approximation; projection and occlusion share the
production scene's depth convention. A small surface offset avoids road overlap.

Verification: `time_attack_ghost_tests` covers best replacement, corruption,
matching and wrapped pose interpolation. The isolated mode-flow fixture `-15`
exercises actual native capture/finish/restart paths and imported-course loading.
The player diagnostic `-idas3-mode-flow-smoke <new-directory> -idas3-ghost-check`
checks runtime rendering, frame/pause independence, controller access and settings
apply/reload/cancel, and captures the ghost on/off. The additional flag
`-idas3-ghost-menu-capture` captures Gameplay when running interactively; hidden
Windows players do not receive the required OnGUI repaint events. Diagnostics use
private saves and do not submit times.

The local Windows build passed 22 storage/interpolation checks, 20 native race
checks, and 21 built-player checks. See
`Verification/time-attack-ghost-20260926/validation.json` for the build hashes and
capture comparison. The temporary user-owned test ROM was removed from the build
after verification.
