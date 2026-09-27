# Sound Room

Sound Room replaces the race music picker reached from opponent selection and
the online lobby's Music button. Hold View Change to open it as before.

- Search titles or artists, browse game collections, sort titles and save favorites.
- Preview songs with play/pause, a seekable waveform, next/previous and volume.
- Select **Use for race** (Enter / controller A) to commit the full song.
- Keep automatic music, the existing 116 selectable original tracks and custom
  MP3/OGG/WAV imports, folder scanning and deletion with No selected by default.
- Add the 74 available named Season 5 race songs, with original artwork and full
  recordings. Menu music, Additional BGM, Story music, Unlisted songs and Movie
  audio are excluded. The empty A_ONE_51 cue supplies no playable song.

Previews use a separate Unity audio source and temporarily duck native music.
They stop on selection changes or closing the picker. A cancelled full-song load
cannot commit after the picker is reopened. Full Season 5 songs use the existing
native custom PCM mixer, including countdown timing, looping and volume. The
chosen song and favorites persist by stable catalog key. Custom music remains local.

## Validation

- Unity Windows build succeeded.
- All 74 full-length packaged songs match their decoded source checksums; all
  191 packaged preview records have media and waveform data.
- Browser playback, seeking, search, favorites, mobile sizing, offline playback
  and playback without JavaScript pass with the race-only catalog.
- Real Unity/native diagnostics verify preview pause/seek/resume, music ducking,
  cancellation, selection persistence and full-song countdown/race playback.
- Existing custom MP3/OGG/WAV import, replacement, validation and confirmed
  deletion regression checks pass using isolated test saves.
- In-game visual review verifies original jackets, Japanese text, search and
  playback controls in the local lobby. Evidence is under Verification/sound-room*.

Windows was tested locally. Linux playback and a two-player network session have
not been exercised for this change. No GitHub release was made.
