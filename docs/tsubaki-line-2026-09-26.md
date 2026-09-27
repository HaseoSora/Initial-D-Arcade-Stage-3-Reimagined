# Tsubaki Line (Arcade Stage 8)

Tsubaki Line is appended as course 15, with condition codes 30 (downhill) and
31 (uphill). Existing course IDs, records and replay identities remain stable.
It supports day/night and dry/wet, with separate directional gates/barriers.
Selection artwork is generated from its own road outline and an actual rendered
course capture. The pause menu, start title, records, replays, Discord presence
and multiplayer use its own name.
Sponsor panels use their own atlas dimensions so lettering reads correctly from
both sides without reversing ordinary scenery textures.

## Source and conversion

The source is the local `Initial D Arcade Stage 8` installation's
`data/COURSE.xaf` and `data/COURSE/tsubaki`, selected by `COURSE_DATA.ini`.
`Tools/Import-TsubakiCourse.py` imports all four scenery variants, their textures,
1,361 configured tree placements with three LODs, 24 spectators and 15 lamps.
It uses both configured roadside tree paths, excluding an unselected third path.
The importer requires the existing local Yabukita decoder helper and the original
installation; the game does not depend on that installation after conversion.

The source paths begin with ten disconnected centre-line samples far from the
actual approach. The conversion trims those ten samples from all three strips
and remaps checkpoint and lighting-event indices together. All playable road,
approach and runout points remain. The resulting 4,112-point path has checkpoints
164, 1108, 2076, 2996 and 3820.

`Tools/Convert-Stage8Road.py` produces the existing D3 path/contact formats.
Every road edge sample is retained. Coarse contact cells use the actual road
plane with a two-metre wheel-probe allowance; this distinguishes the upper and
lower road decks at the overpass. The output has 24,666 contact triangles.

Handling uses Akina's Stage 3 parameters (conditions 6/7), with normal per-car
tuning and dry/wet behavior. This is a scenery/route port using the existing D3
driving solver, not a port of Stage 8 physics. The original Stage 8 TA timer
tables are retained: 120 seconds initially, then 70 seconds per section.

Rebuild the original race-title bank with `Tools/Export-ImportedStartTitles.ps1`
and stage it along with `RuntimeAssets/TSUBAKI`. Full player packaging enumerates
`Idas3CourseCatalog.Packs`; the faster `RebuildWindowsPlayer` entry point still
requires changed external runtime assets to be staged separately.

## Leaderboard

The local website and worker sources include Tsubaki Line. Migration
`0007_tsubaki_line.sql` widens the accepted condition range to 0–31 while
preserving runs, replay chunks, moderation, device records and foreign keys.
The migration and replay upload/download tests pass locally. These source changes
do not deploy the public leaderboard or create a GitHub release.

## Validation

Evidence is under `Verification/tsubaki-20260926` (local, ignored).

- Native full-route checks: 3,657 centre surface queries and 14,616 outward wall
  sweeps pass. Driving contact, gearbox and checkpoint tests pass in both
  directions for dry/wet and automatic/manual, covering 6,240 solver ticks.
- Native multiplayer configuration tests: 725 checks pass, including Tsubaki
  daytime acceptance and unchanged night-only Special Stage restrictions.
- Leaderboard: 41 tests pass, including the migration and both Tsubaki directions.
- Unity visual fixtures cover three route positions in both directions across
  day/night and dry/wet, course selection and pause labels. These are controlled
  camera fixtures, not eight completed human-driven laps.
- The separate TA smoke exercises native finish/timeout, points/tuning, saved
  records, all coaching maps, rankings, Continue and normal menu relaunch.
  Its final evidence is `ta-3/PASS.txt`; the earlier `ta-1` failure records a stale
  six-course title bank in staging, corrected by staging the new title asset.
- Two-process LAN checks passed in `online3-host` and `online3-join`, including
  native authority, moving peers, geometry, course identity, detailed replay
  recording and clean shutdown. Hidden-window IMGUI pixel tests were omitted;
  the earlier `online2` attempt could not receive a menu Repaint in batch mode.
  This does not establish internet/Steam relay quality or compatibility with
  the existing public build.

No full human-driven lap or exact Stage 8 rendering/handling parity is claimed.
No original ROM is included in the course pack or public package.

## Road-sign follow-up

The initial lettering fix covered sponsor logos. The follow-up also corrects
blue direction signs, speed-limit numerals, route numbers, park/bus-stop notices
and vertically mounted warning boards. `Idas8TsubakiSigns.cs` identifies their
individual rectangles in each of the four source atlases. Sideways packed text
uses the V reflection axis. Per-triangle tags prevent text corrections from
changing adjacent stone, guardrail or road textures; arrow-only warning graphics
retain their authored orientation.

`Verification/tsubaki-signs-20260926/PASS.txt` records actual mesh preservation
checks and GPU comparisons using the production fragment shader. Six sign types
in four variants reproduce mirrored back faces with correction disabled and
match their front view with correction enabled (zero differing pixels at the
test's three-channel tolerance). Existing Hakone/Sadamine sign checks also pass.
The rebuilt player passes the isolated 25-capture Tsubaki course fixture sequence
covering both directions and all four variants. Road/physics assets are unchanged.

## Scenery overlap correction

The three player clips recorded at 22:41, 22:42 and 22:43 on September 26 show
triangular terrain patches and unstable roadside surfaces. Tsubaki's static
course meshes author separate front/back polygons on exactly the same plane,
often with different UVs or vertex colors. The initial loader applied facing
rejection to instanced trees only, so these static pairs competed for depth.

Static Tsubaki course and mountain meshes now use the existing paired-face
classifier and world-space facing rejection. Unpaired polygons remain two-sided;
both authored sides, original positions, normals, UVs and colors are retained.
Sign-reflection tags survive the per-triangle vertex split, preserving the
previous lettering correction. This is restricted to Tsubaki static surfaces;
existing foliage handling on other imported courses is unchanged.

`Idas3TsubakiSceneryChecks` audits the real mesh attributes and sign tags across
all four variants and captures eight positions in both directions with the
production loader/shader. Local evidence and matched before/after captures are
under `Verification/tsubaki-clips-20260926`. The captured hillside at path point
3500 reproduces the triangular patches before the correction and a continuous
rock surface afterward. No physics parameters or collision data are changed.
