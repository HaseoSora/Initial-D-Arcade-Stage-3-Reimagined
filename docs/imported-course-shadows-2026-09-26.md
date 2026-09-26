# Hakone and Sadamine shadows

The daytime type-6 textures store **light visibility** in alpha: tree/pole
silhouettes are dark and open space is white. Hakone uses black-RGB DXT5;
Sadamine uses A8. Both the separate road overlay and the secondary texture
path treated that channel as shadow opacity, reversing the lighting.

The imported shader now inverts visibility when blending a black overlay and
when evaluating the secondary shadow texture. The existing secondary-pass
ambient floor is retained. DDS bytes, mipmaps, texture orientation, scenery
colors and original Stage 3 shading are not changed. Materials also honor the
authored shadow UV-set index; several gutter/slope materials request UV0.

Sadamine's single weather-specific course pack contains separate uphill and
downhill fence/checkpoint groups. Both were rendered together. A transformed
triangle audit (positions rounded to 1 mm) found 12,596 overlapping triangles
between those groups in each weather variant. At night that total comprises
11,732 fence triangles, 440 checkpoint triangles and 424 lamp triangles.
The loader now uses only the selected direction's group, including on direction
changes. Common scenery stays active. This fixes those verified overlapping
groups; it does not assert that every possible flicker in the course has the
same cause.

Verification files are in the ignored directory
`Verification/imported-shadow-20260926/`. The opt-in standalone command is
`-idas3-mode-flow-smoke <new-output-folder> -idas3-imported-shadow-check`.
It runs production DDS decoding and GPU rendering for A8 and DXT5 shadow masks,
then inspects/captures both directions in all four weather/time variants of
both courses using isolated native fixtures. No ordinary saves or public
leaderboard submissions are used.

Results:

- `unity-final-build.log`: full staging player build succeeded.
- `final/report.json`: 27,782 checks passed with clean shutdown, including 32
  sampled GPU colors and material/direction checks across 16 course conditions.
  The shader test uses an isolated vertex stage with the actual production
  fragment function; the scene captures cover the complete native camera path.
- `final/`: 16 scene captures and eight GPU mask captures. Daytime before/after
  images and a sheet of every condition were visually inspected. Tree-shaped
  areas now shade the road rather than brighten it.
- `topology.json`: the four-condition Sadamine overlapping-triangle audit.

Early GPU fixture attempts inherited camera state and rendered no test quad;
the final fixture isolates its clip-space vertex stage. An early material
assertion also assumed names were unique; source materials with repeated names
are now checked by actual material identity. These diagnostic failures are
retained in the ignored proof directory, not reported as passing runs.

Puniru's submitted `Gunsai-course.patch` independently contains the same
visibility inversion correction. It was reviewed, not applied wholesale.
Gunsai, its assets, its DLLs and its unrelated map/network changes were excluded
as requested.

The changes are local and have not been released on GitHub.

At the user's request, the tested player was also installed into
`C:/Users/Chris/Desktop/Current`. The comparison covered 18,044 approved game
files; seven changed files were backed up and replaced, then hash-verified.
The desktop executable passed the same 27,782 checks and all 16 condition
captures (`desktop-smoke/report.json`). Saves, settings, ROMs and custom music
were excluded from deployment. `desktop-deployment.json` records the install
and backup paths. No GitHub release was made.
