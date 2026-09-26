# Graded tachometer drift lamps

The previous implementation selected only the green switcher branch and disabled 711 blue/orange/red layers. Version 3 HUD telemetry now carries a four-level drift grade, while retaining the 40-byte structure and the version 2 green fallback. All 82 designs with authored drift lamps use the recovered color groups; the other five designs receive no invented indicator.

The native detector remains presentation-only. Actual world motion versus vehicle heading determines slip, with the existing speed/ground/contact checks and spin exclusion. Entry requires 120 ms at eight degrees; deeper grades use 10, 13 and 17 degrees. Grade changes require 80 ms, with 1.5 degrees of release hysteresis. A qualified initial slide selects its current grade immediately. These are D3 adaptations, not recovered The Arcade thresholds. Physics, handling, leaderboard rules and save formats are unchanged.

The renderer preserves source switchers, brush tints, glow stacks and alpha. InOut channels track the native fade across each channel's own keys. Lesser grades play recovered Stay animation; red plays recovered Blink animation. Halloween also uses its stronger authored lantern motion. A shared elapsed presentation clock makes these independent of FPS and freezes them on pause. Layout bounds are baked over all grades and their animation envelopes so the HUD does not shift or resize when the grade changes.

Verification is under `Verification/drift-grades-20260926`:

- Native unit checks cover left/right slip, all grades, hysteresis, short spikes, pause, contact fade, reset and 30/60/120/240 Hz behavior.
- Managed checks cover all 87 designs, all four colors, release fading, blinking, pause, seek and texture ownership. Existing Halloween and meter-signal regressions also run.
- GPU previews show Stuttgart, The Arcade, Steampunk, Miku and Halloween at all four levels. These previews use synthetic telemetry to exercise every grade.
- The isolated player fixture drives FR/FF/AWD cars through the original physics in dry/wet conditions, checks physics immutability and captures a real detected drift. It separately checks lamp fade pixels. It does not claim all four grades were reached in the scripted driving trajectory.

Replay drift detection remains unavailable because recorded replays do not contain the required authoritative contact/ground state. No new source artwork is fabricated. Included in release .36; release verification is recorded in `Verification/release-36-20260926/`.
