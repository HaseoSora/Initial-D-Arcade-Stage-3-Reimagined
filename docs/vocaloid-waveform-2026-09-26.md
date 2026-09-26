# Vocaloid meter exterior waveform

The player reference shows audio bars outside the left dial on the Hatsune
Miku, Kagamine Rin & Len, and Megurine Luka meters (source IDs 68–70).
Their imported spectrum canvas is 308 pixels square, centered at (175,208).
The shared reconstructed audio shader previously began the bars at radius
0.235, or about 72 pixels. Most of the bars were therefore covered by the
later dial artwork, whose outer rim is about 132 pixels from the center.

These three meters now pass a 132-pixel starting radius and 20-pixel outward
range to the shared live/preview shader. Spectrum sampling, audio smoothing,
colors, source transforms, and draw order are retained. Other audio meters
keep their previous radii. The canvas and layout bounds have not changed.
This corrects placement in the reconstructed effect; the original cooked
material's executable graph is not available for exact visual parity.

Verification uses `Idas3MeterCatalogBuild.VerifyVocaloidWaveform` in Unity
batch mode with D3D11. `Idas3MikuMeterChecks` compares GPU images at silence,
moderate audio, and peak audio, in day/night at 75% and 150% HUD sizes. It
checks pixels beyond the exposed rim, outward growth, and the canvas edge,
as well as the existing alignment and preview geometry checks.

- Before: all three meters failed exterior visibility at moderate audio;
  each sampled exterior contained zero changed pixels.
- After: 376 checks passed across 60 GPU frames; 770 audio processor checks
  also passed. Miku and Luka captures were visually inspected.
- Before/after logs: `Verification/vocaloid-waveform-20260926/`.
- Before report: `Verification/miku-meter-alignment/gpu-20260926-115939-f541a4de/report.json`.
- After report: `Verification/miku-meter-alignment/gpu-20260926-120036-fed8d36c/report.json`.

IMGUI preview geometry and material parameters are checked; interactive
preview rasterization was not separately exercised. The common shader is
used in both rendering paths.

The full Windows player build succeeded (`build.log`) and was installed into
`C:/Users/Chris/Desktop/Current`. Three changed game files were backed up,
replaced, and hash-verified against staging; saves, settings, ROMs, and custom
music were excluded. `Verification/vocaloid-waveform-20260926/desktop-deployment.json`
records the installation. The earlier Hakone/Sadamine fixes remain included.
No GitHub release was made.
