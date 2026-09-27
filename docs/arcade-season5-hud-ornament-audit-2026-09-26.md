# Season 5 tachometer and ornament audit — 26 September 2026

Audited the supplied **Initial D The Arcade Season 5 (3.10.00 Rev 1 +A)** installation, including its patch archive, against the Season 3 recovery and the current Unity catalogs. This is an asset/data audit. The Season 5 game was not executed, and the findings are not a claim of runtime parity or unlock availability. Outputs are isolated under `D:/Codex/GitHub/Initial-D-Arcade-Stage-3-Reimagined/Verification/arcade-s5-audit-20260926`; no assets were imported into the game and no release was published by this audit.

## Result

| Content | Current Unity catalog | Season 5 registry | Additional entries |
| --- | ---: | ---: | ---: |
| Tachometers | 87 | 115 | 28: **26 named designs and 2 dummy placeholders** |
| Hanging ornaments | 280 | 316 | 36: **34 named entries and 2 dummy placeholders** |
| Registered chain rigs | 4 | 4 | 0; the four mesh/rig source packages are byte-identical |

The 316 ornament rows reference **315 distinct models**, because two course keychains incorrectly share a model. There are eight additional unregistered mesh assets, inventoried separately below.

## Findings to address before importing

1. **Exclude placeholder meters 113 and 114, and ornaments 779 and 780.** This is more than an incomplete display label: the two meter base textures and two ornament diffuse textures visibly contain a pink `dummy` tile. Their widgets/meshes exist, but their presence should not be presented as finished artwork. See `placeholder-evidence.json` and the four original converted images.
2. **Correct the Manazuru black keychain mapping if imported.** Row **271**, `真鶴（黒プレート）`, points to `Key_chain_plate_UsuiSnow_Black`, also selected by row **272**, the Usui Snow black plate. The source contains the correctly named **`Key_chain_plate_coute103_Black`** mesh, with its dependencies and structurally valid exported geometry. The recommendation is to validate that model with row 271's icon and bind it explicitly; do not copy the erroneous table reference blindly. The source installation is unchanged.
3. **Review five updated gear-digit material bindings.** Racing **32**, Sirius **39**, Racing Pink **46**, Racing Orange **47**, and Racing Green **48** now bind `GearRate01` to their dedicated `MI_MeterNN_ShiftNum` material. The current import retains the older generic number/speed-number bindings. These are concrete source differences; a Unity render check is still needed before describing them as a fixed runtime defect.
4. **Review Flandre Scarlet meter 62's drift overlays.** Season 5 switches six green/orange/red lamp layers from meter **49** materials to the matching meter **60** silhouette-family materials. The current import still references 49. All referenced replacement resources are present.
5. **Preserve the additional effects when porting the 26 designs.** All 28 added registry widgets, including placeholders, contain drift-related controls. Their actual widget trees, layered resources, MovieScene tracks, and source parameter values are retained. Meter **104** has three special eye-loop animations; the chibi family introduces **`M_Meter98_Rotation`**. These need deliberate adapters and render checks, rather than importing just a flat frame. `M_Meter114_SlideGauge` belongs to a placeholder and should be kept out of a finished-content import.
6. **Retain attachment exceptions.** Source sockets are missing on 13 registered ornament rows: 7, 14, 21, 24, 28, 41, 42, 43, 45, 63, 83, 312, 780. New exceptions are **45 (FL5)** and **780 (placeholder)**. Mesh geometry being present does not prove a recovered attachment transform. Keep explicit attachment metadata and test the FL5 mount.

## Validation

- Both archive indices validated against their stored SHA-1. The patch mounts at `../../../GameProject/`, while the base mounts at `../../../`; normalization was performed before overriding entries. Without this, final patch rows and designs 113–117 would be missed.
- **4,168 packages / 8,351 source files** inspected, including registered content, all keychain mesh candidates, and recursively referenced visual dependencies. **Zero missing package dependencies and zero read errors** in this scope. Nonvisual gameplay/native dependencies are recorded as boundaries rather than expanding into unrelated cars/courses.
- Every extracted file passed the archive entry checksum and received a SHA-256 provenance record. Compared with the earlier source: **6,342 identical, 447 changed, 1,562 new files**. There are **565 patch-selected files** in scope.
- All **115 registered meter compositions** were inspected: no property read errors, and every serialized animation track binding resolved. **799 material packages** were inspected without tagged-property read errors. Counts include shared dependencies and are not a count of distinct visible effects.
- New/changed supported assets were converted with the existing UE Viewer tool. **689 PNGs** passed chunk CRC and full image decoding; **41 mesh exports** passed buffer bounds, finite coordinate, vertex attribute, and triangle-index checks. One HDR passed its Radiance header/dimension check; its pixels were not decoded by this audit.
- Model exports are genuine geometry. UE Viewer's glTF files do not automatically preserve the original shader/material appearance; material bindings, diffuse/specular textures, and raw packages are retained separately. Geometry validation does not imply that a flat glTF preview reproduces the original lighting.

## Chain and motion audit

All four registered chain `.uasset` / `.uexp` pairs and the strap table are identical to the earlier recovery. Every strap still selects `SwingType = 0`. The mirror actor retains native parent `/Script/IndRun.MirrorAccessory`; its inspected defaults are unchanged apart from the opaque compiled `UberGraphFrame` value. No new Blueprint function names were found. The separate race-mascot widget is unchanged.

This does **not** recover the original native pendulum equations, native attachment transform, or the meaning of the swing enum. The current game's reconstructed chain physics can be reused with appropriate mount testing; Season 5 does not provide a newly verified original physics implementation in these assets.

## Additional meter entries

English labels below are descriptive translations/identifications; the exact Japanese source names and class references are preserved in `meters.csv` and `meter-registry.json`.

| Source ID | Design | Image layers | Animation objects | Status |
| ---: | --- | ---: | ---: | --- |
| 90 | Pop Team Epic — quote design | 41 | 15 | Asset candidate; runtime adaptation pending |
| 91 | Pop Team Epic — Stay! Stay! | 41 | 12 | Asset candidate; runtime adaptation pending |
| 92 | Street | 36 | 13 | Asset candidate; runtime adaptation pending |
| 93 | The Quintessential Quintuplets — Ichika | 43 | 15 | Asset candidate; runtime adaptation pending |
| 94 | The Quintessential Quintuplets — Nino | 43 | 15 | Asset candidate; runtime adaptation pending |
| 95 | The Quintessential Quintuplets — Miku | 43 | 15 | Asset candidate; runtime adaptation pending |
| 96 | The Quintessential Quintuplets — Yotsuba | 43 | 15 | Asset candidate; runtime adaptation pending |
| 97 | The Quintessential Quintuplets — Itsuki | 43 | 15 | Asset candidate; runtime adaptation pending |
| 98 | Quintuplets chibi — Ichika | 40 | 12 | Asset candidate; runtime adaptation pending |
| 99 | Quintuplets chibi — Nino | 40 | 12 | Asset candidate; runtime adaptation pending |
| 100 | Quintuplets chibi — Miku | 40 | 12 | Asset candidate; runtime adaptation pending |
| 101 | Quintuplets chibi — Yotsuba | 40 | 12 | Asset candidate; runtime adaptation pending |
| 102 | Quintuplets chibi — Itsuki | 40 | 12 | Asset candidate; runtime adaptation pending |
| 103 | Retro Yankee | 23 | 16 | Asset candidate; runtime adaptation pending |
| 104 | Evil Eye | 44 | 17 | Asset candidate; runtime adaptation pending |
| 105 | MF Ghost — anime edition | 46 | 15 | Asset candidate; runtime adaptation pending |
| 106 | Infinity — 2nd tournament participation | 43 | 15 | Asset candidate; runtime adaptation pending |
| 107 | Touhou silhouette — Sakuya Izayoi | 41 | 15 | Asset candidate; runtime adaptation pending |
| 108 | Touhou silhouette — Remilia Scarlet | 41 | 15 | Asset candidate; runtime adaptation pending |
| 109 | Touhou silhouette — Flandre Scarlet | 41 | 15 | Asset candidate; runtime adaptation pending |
| 110 | Kaiji | 47 | 12 | Asset candidate; runtime adaptation pending |
| 111 | Teiai | 46 | 15 | Asset candidate; runtime adaptation pending |
| 112 | The Bog / Numa | 46 | 14 | Asset candidate; runtime adaptation pending |
| 113 | Dummy placeholder | 36 | 14 | Exclude: dummy art |
| 114 | Dummy placeholder | 53 | 13 | Exclude: dummy art |
| 115 | Kazuma Kiryu | 34 | 12 | Asset candidate; runtime adaptation pending |
| 116 | Goro Majima | 39 | 15 | Asset candidate; runtime adaptation pending |
| 117 | Infinity — Initial D 30th anniversary | 39 | 15 | Asset candidate; runtime adaptation pending |

## Additional ornament entries

These are table-selected 3D mesh references, not inventory pictures. Original labels are retained. IDs are source IDs and should remain stable if imported.

| ID | Source display name | Mesh | Note |
| ---: | --- | --- | --- |
| 16 | ＭＸＷＨ６１ | `Key_chain_MXWH61` |  |
| 31 | Ｒ３５ ＭＹ２５ | `Key_chain_car_R35T` |  |
| 45 | ＦＬ５ | `Key_chain_car_FL5` | No serialized attachment socket |
| 85 | ＺＤ８ | `Key_chain_car_ZD8` |  |
| 122 | ＤＣ５１Ｔ | `Key_chain_car_DC51T` |  |
| 142 | ９９１ | `Key_chain_car_P991` |  |
| 150 | ９６０１８ | `Key_chain_car_AF4C` |  |
| 221 | 真鶴(木) | `Key_chain_plate_coute103_Wood` |  |
| 222 | 碓氷（雪）（木） | `Key_chain_plate_UsuiSnow_Wood` |  |
| 246 | 真鶴（プレート） | `Key_chain_plate_coute103_Red` |  |
| 247 | 碓氷（雪）（プレート） | `Key_chain_plate_UsuiSnow_Red` |  |
| 271 | 真鶴（黒プレート） | `Key_chain_plate_UsuiSnow_Black` | Correct source mapping before use |
| 272 | 碓氷（雪）（黒プレート） | `Key_chain_plate_UsuiSnow_Black` |  |
| 337 | 「まさに・・」 | `Key_chain_Word_37` |  |
| 338 | 「速くなければ・・」 | `Key_chain_Word_38` |  |
| 339 | 「来てみろや‼」 | `Key_chain_Word_39` |  |
| 340 | 「まんまと・・」 | `Key_chain_Word_40` |  |
| 341 | 「ず」 | `Key_chain_Word_41` |  |
| 342 | 「オレにはわかる」 | `Key_chain_Word_42` |  |
| 343 | 「必要なのは無（ゼロ）の心だ・・!!」 | `Key_chain_Word_43` |  |
| 344 | 「いよいよオレらの出番ってわけか・・」 | `Key_chain_Word_44` |  |
| 345 | 「くそったれ・・!!」 | `Key_chain_Word_45` |  |
| 346 | 「今度こそ本音で選ぶんだぜ・・」 | `Key_chain_Word_46` |  |
| 742 | ポプ子（バール） | `Key_chain_popteam_742` |  |
| 743 | ピピ美（プライヤー） | `Key_chain_popteam_743` |  |
| 774 | 一花（SD） | `Key_chain_Hanayome_ichika_00` |  |
| 775 | 二乃（SD） | `Key_chain_Hanayome_nino_00` |  |
| 776 | 三玖（SD） | `Key_chain_Hanayome_miku_00` |  |
| 777 | 四葉（SD） | `Key_chain_Hanayome_yotsuba_00` |  |
| 778 | 五月（SD） | `Key_chain_Hanayome_itsuki_00` |  |
| 779 | dummy | `Key_chain_maimai_779` | Exclude: dummy diffuse texture |
| 780 | dummy | `Key_chain_maimai_780` | Exclude: dummy diffuse texture |
| 1016 | 秋山延彦 | `key_chain_Theory_1016_akiyama` |  |
| 1017 | 岩瀬恭子 | `key_chain_Theory_1017_kyoko` |  |
| 1116 | 眼鏡 | `key_chain_Theory_1116_glasses` |  |
| 1117 | 携帯電話 | `key_chain_Theory_1117_keitai` |  |

Existing ornament rows 27 and 723–728 only change their display names in the registry; their mesh/icon/strap mappings remain the same. The MF Ghost group now explicitly says manga edition.

## Unregistered mesh assets

These files are not additional selectable items in the inspected table. Alternate/dummy meshes, unused character variants, and cord variants must not be counted as additional registered ornaments.

- `Key_chain_0779`
- `Key_chain_780`
- `Key_chain_plate_coute103_Black`
- `Key_chain_popteam_737`
- `Key_chain_popteam_739`
- `key_chain_Himo_00_Brack`
- `key_chain_Himo_00_Red`
- `key_chain_Himo_00_White`

## Import order

1. Apply and render-test the six existing-meter binding changes.
2. Port the 26 non-placeholder meter widgets with their shared materials, digit/needle families, drift/rev/gear effects, eye loops and rotation materials. Exercise day/night, all gear digits, AT/MT, all four drift grades, scaling, placement and frame-rate changes.
3. Import the 34 named ornament entries with genuine mesh/material resources, correct row 271's mapping after a visual comparison, and test FL5's mount. Retain the existing four rig definitions and motion adapter.
4. Keep 113/114 and 779/780 recorded for provenance but unavailable in the player-facing selectors. Do not expose unrelated unregistered mesh files as new items automatically.

## Evidence files

The audit folder contains `summary.json`, `source-inventory.json`, `dependencies.json`, `meter-registry.json`, `meter-details.json`, `widgets-details/`, `materials.json`, `ornament-registry.json`, `ornament-details.json`, `motion-comparison.json`, `unregistered-models.json`, `placeholder-evidence.json`, `export-verification.json`, `meters.csv`, and `ornaments.csv`. `Raw/` preserves scoped cooked files; `Exports/` holds conversions. Private archive-access material is not part of this report or any release.

Remaining limits: cooked/stripped shader graphs, original-only corner-speed events, native runtime rules, exact animation activation chains, dynamic asset loads, and unlock conditions are not proven by static import-table analysis. No exact arcade behavior or running-game visual parity is claimed.
