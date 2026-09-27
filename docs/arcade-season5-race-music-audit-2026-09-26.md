# Season 5 race music audit — 26 September 2026

Source: the supplied **Initial D The Arcade Season 5 (3.10.00 Rev 1 +A)** installation. Original game files were read without modification. This audit does not add music to the game or publish a build.

## Result

- **74 named race songs have complete, decodable audio.** All 74 are stereo 48 kHz HCA streams with loop points.
- The race table has 85 rows: 75 named songs, six Dummy entries, and four special choices (Continue, Random, No BGM and automatic BGM).
- **テリトリーバトル — ツユ, Vo: 礼衣** remains named in the table, but both its race and preview cues have zero tracks. Its song audio is unavailable through those cues in this installation.
- The six Dummy entries (A_ONE_74–79) also have zero-track race/preview sequences. They are not additional recoverable songs.
- Two other audio cues, A_ONE_12 and A_ONE_47, exist and decode but are not registered race-menu songs. Their titles and intended use remain unverified.
- The existing project catalog has 117 records from earlier games. None of the 74 available Season 5 race-song titles matches an existing title after case/punctuation normalization. This is a catalog comparison, not audio fingerprinting.

The 74 available songs total **3 hours 54 minutes 13 seconds** before repeating their loops. The `_loop` cue variants reuse their corresponding song streams; they are not another 74 separate recordings. Their CRI cue/sequence controls remain recorded separately.

## Available music by source category

| Category | Songs |
|---|---:|
| The Arcade originals | 25 |
| Touhou Project | 25 |
| Dave Rodgers | 2 |
| 20th Anniversary | 1 |
| Hatsune Miku / Project DIVA | 12 |
| CHUNITHM | 7 |
| Hot-Version | 1 |
| MF Ghost anime | 1 |

## Complete named race-song list

Titles and artist credits below are taken directly from the game table. Duration is the playable stream length, not a soundtrack-album duration. The missing named entry is retained in the list.

| Cue | Song | Artist / vocalist | Length |
|---|---|---|---:|
| A_ONE_01 | Dive into the SPEED | A-One  Vo: AXEL.K | 3:58 |
| A_ONE_02 | Waiting for you | A-One  Vo: Shihori | 3:24 |
| A_ONE_03 | Starlight Spada | A-One  Vo: Rute | 2:25 |
| A_ONE_04 | Don't stop your way | A-One  Vo: あき | 2:29 |
| A_ONE_05 | Good Energy Flow | A-One  Vo: Nene | 2:23 |
| A_ONE_06 | Super Sonic | A-One  Vo: 光吉猛修 | 2:17 |
| A_ONE_07 | High Speed Live | A-One  Vo: さちぶどう | 2:14 |
| A_ONE_08 | Midnight Tournament | A-One  Vo: KEN BLAST | 2:36 |
| A_ONE_10 | Drive Your Ambition | A-One  Vo: みぃ | 2:21 |
| A_ONE_11 | Turn It Around | A-One  Vo: KEN BLAST | 2:35 |
| A_ONE_09 | SCRAMBLE EYES | MOTSU vs A-One | 2:15 |
| A_ONE_15 | DUAL ROZES | SOUND HOLIC vs Eurobeat Union  Vo: Nana Takahashi | 3:41 |
| A_ONE_16 | EDGE OF TIME | SOUND HOLIC  Vo: 709sec. | 3:17 |
| A_ONE_17 | God only knows | A-One  Vo: みぃ | 2:33 |
| A_ONE_19 | PANDORA ZONE | SOUND HOLIC  Vo:Nana Takahashi | 3:48 |
| A_ONE_18 | Crazy Hot | NJK Record  Vo: 坂上なち | 3:23 |
| A_ONE_20 | ANGEL WiNG (Crazy REMIX with DiGiTAL WiNG) | DiGiTAL WiNG / CrazyBeats  Vo: 花たん | 3:40 |
| A_ONE_21 | Endless, Sleepless Night | A-One  Vo: (V)・∀・(V) | 2:36 |
| A_ONE_22 | STARVING RAVEN | SOUND HOLIC  Vo: Nana Takahashi | 2:51 |
| A_ONE_13 | Horse Power Desire | Dave Rodgers  Vo: Dave Rodgers | 4:08 |
| A_ONE_14 | TOFU CONNECTION | Dave Rodgers  Vo: Dave Rodgers | 3:55 |
| A_ONE_23 | Fever on the Road | A-One  Vo: あやぽんず＊ / ytr | 3:12 |
| A_ONE_24 | NITRO RAINBOW | SOUND HOLIC vs Eurobeat Union  Vo: Nana Takahashi | 3:32 |
| A_ONE_25 | IGNITE THE POWER | SOUND HOLIC  Vo: STEVIE(44MAGNUM) | 3:29 |
| A_ONE_26 | Chase The Ghost | Sachi@SEGA | 1:40 |
| A_ONE_27 | BURN INSIDE | TAKENOBU & KUNOICHI @SEGA | 3:28 |
| A_ONE_28 | Endless Seeker | A-One  Vo: Rute / あき | 3:54 |
| A_ONE_29 | Fantastic World | A-One  Vo: Rute | 3:18 |
| A_ONE_36 | BEAT DOWN | A-One  Vo: Rute | 2:18 |
| A_ONE_30 | PRESERVED VAMPIRE | SOUND HOLIC  Vo: Nana Takahashi | 3:12 |
| A_ONE_31 | HEARTSTRINGS | DiGiTAL WiNG  Vo: 花たん | 3:55 |
| A_ONE_32 | No Life Queen (DJ Command Remix) | SOUND HOLIC Vs. Eurobeat Union  Vo: Nana Takahashi | 3:28 |
| A_ONE_33 | DARTH KILLER | SOUND HOLIC  Vo: Nana Takahashi / 709sec. | 4:11 |
| A_ONE_35 | Grip & Break down !! | SOUND HOLIC  Vo: Nana Takahashi | 1:56 |
| A_ONE_34 | Don't Break Me Down | A-One  Vo: AXEL.K | 2:32 |
| A_ONE_37 | DEEP IN THE HEATWAVE | SOUND HOLIC  Vo: 小寺可南子 | 3:02 |
| A_ONE_38 | ロミオとシンデレラ | doriko  Vo: 初音ミク | 4:29 |
| A_ONE_39 | ネトゲ廃人シュプレヒコール | さつき が てんこもり  Vo: 初音ミク | 4:21 |
| A_ONE_45 | テオ | Omoi  Vo: 初音ミク | 3:21 |
| A_ONE_40 | ロストワンの号哭 | Neru  Vo: 鏡音リン | 3:14 |
| A_ONE_41 | 孤独の果て | 光収容  Vo: 鏡音リン | 3:07 |
| A_ONE_46 | 右肩の蝶 | のりぴー  Vo: 鏡音レン | 4:22 |
| A_ONE_42 | ルカルカ★ナイトフィーバー | samfree  Vo: 巡音ルカ | 3:32 |
| A_ONE_43 | ワールズエンド・ダンスホール | wowaka  Vo: 初音ミク / 巡音ルカ | 3:21 |
| A_ONE_44 | アカツキアライヴァル | Last Note.  Vo: 初音ミク / 巡音ルカ | 4:15 |
| A_ONE_53 | Asterist | DiGiTAL WiNG  Vo: ふうか | 2:59 |
| A_ONE_52 | PARAMETERS | DiGiTAL WiNG  Vo: 沙 | 2:33 |
| A_ONE_48 | IMPACT | USAO feat.光吉猛修  Vo:光吉猛修 | 2:27 |
| A_ONE_49 | POTENTIAL | TAG | 2:15 |
| A_ONE_50 | We Gonna Journey | Queen P.A.L. | 2:08 |
| A_ONE_51 | テリトリーバトル | ツユ  Vo:礼衣 | **No audio: empty cues** |
| A_ONE_54 | Set Me Free Babe | Richard Cottle | 2:42 |
| A_ONE_55 | God Bless You!! | A-One  Vo: NAGISA | 3:37 |
| A_ONE_56 | Love Thief | Crazy Beats  Vo: 加藤ありさ | 4:04 |
| A_ONE_57 | Veiling Shooter | A-One  Vo: 花たん | 3:45 |
| A_ONE_58 | Twin Blade | A-One  Vo: Rute / あき | 1:26 |
| A_ONE_59 | GET IN THE GROOVE | DJ Command(Eurobeat Union)feat. Okogeeechann | 4:47 |
| A_ONE_60 | SOUL 2 DIVIDE | SOUND HOLIC Vs. ZYTOKINE　Vo: 星野奏子 | 3:42 |
| A_ONE_61 | 00 HEAVEN | SOUND HOLIC Vs. Eurobeat Union  Vo: Nana Takahashi / 709sec. | 3:56 |
| A_ONE_62 | Death Blossom | REDALiCE feat. 野宮あゆみ | 2:52 |
| A_ONE_63 | Necrophantasia | SOUND HOLIC　Vo: YURiCa / 花たん | 2:49 |
| A_ONE_64 | Wandering Soul | DiGiTAL WiNG with 空音 | 3:57 |
| A_ONE_65 | JUNGLE FIRE feat. MOTSU | 芹澤 優 | 3:08 |
| A_ONE_66 | New Starlight | DiGiTAL WiNG  Vo: 空音 | 2:52 |
| A_ONE_67 | Catch the Wave | livetune Vo: 初音ミク | 3:10 |
| A_ONE_68 | ロキ | みきとP Vo: 鏡音リン / みきとP | 3:35 |
| A_ONE_69 | ダブルラリアット | アゴアニキ Vo: 巡音ルカ | 3:15 |
| A_ONE_70 | VANTAGE POINT | SOUND HOLIC feat. Nana Takahashi | 2:47 |
| A_ONE_71 | LUNATIC DOLL | DJ Command (Eurobeat Union) Vo: Cocoa (Eurobeat Union) | 4:13 |
| A_ONE_72 | Tiara is gone | CrazyBeats Vo: KIHOW | 4:22 |
| A_ONE_73 | sweet little sister | Silver Forest Vo: さゆり (Silver Forest) | 3:16 |
| A_ONE_80 | Dark Diver | emon(Tes.)  Vo: ねんね | 2:41 |
| A_ONE_81 | C & B | PSYQUI  Vo: Such | 2:16 |
| A_ONE_82 | MUSIC PЯAYER | A-One  Vo: Rute / あき | 2:27 |
| A_ONE_83 | Elusive Enforcer | K-forest vs. Reku Mochizuki | 2:19 |

## Extra audio and exclusions

| Cue | Stream | Length | Finding |
|---|---:|---:|---|
| A_ONE_12 | 92 | 1:38 | Decodes; not in the race selection table; title unverified |
| A_ONE_47 | 51 | 3:28 | Decodes; not in the race selection table; title unverified |

The BGM bank has 118 streamed entries overall. The other audio includes menu, drama, finish and game-over cues. The embedded memory bank has two entries: the finish lead-in and a short A_ONE_12 prefetch segment. These are not counted as separate race songs. `non-race-cues.json` retains the menu/drama table associations.

## Validation and limits

- Both archive indexes were verified; extracted cue/table packages retain source hashes and base/patch provenance.
- All 118 streamed-bank entries have valid bounded AFS2 offsets and individual SHA-256 hashes. None are byte-identical duplicates. All use unencrypted HCA.
- The BGM.awb MD5 matches the hash stored in the original ACB cue sheet: `abeaca286fc22f61e6c9316f73242607`.
- All 74 available race streams decoded end to end with looping disabled, no output files, zero decoder errors and no warnings. The two unlisted candidate streams also passed.
- Exact sample counts, loop start/end samples, race/preview cue associations, artwork references and raw cue/sequence metadata are retained in the audit artifacts.
- This is an offline audit. Every song was not listened to in full; audible loop seams, original-game unlock rules, preview timing and in-game playback have not been tested.

Audio metadata and decode validation used [vgmstream r2117](https://github.com/vgmstream/vgmstream/releases/tag/r2117). The independent CRI table inspection was checked against its [ACB implementation](https://github.com/vgmstream/vgmstream/blob/master/src/meta/acb.c) and [UTF reader](https://github.com/vgmstream/vgmstream/blob/master/src/util/cri_utf.c).

## Local evidence

All private evidence is under `Verification/arcade-s5-music-audit-20260926`:

- `race-songs.csv`: complete 81-entry music-row inventory, including unavailable entries, with titles, artists, categories, duration and loop samples.
- `race-songs.json`: fuller song records and jacket-image references.
- `stream-metadata.json`, `memory-stream-metadata.json`, `awb-directory.json`: original audio-bank contents and hashes.
- `acb-tables.json`: preserved cue, sequence, track, waveform and preview-control metadata.
- `decode-results.json`, `unregistered-decode-results.json`: full decode results.
- `summary.json`, `source-files.json`: findings and package provenance.
