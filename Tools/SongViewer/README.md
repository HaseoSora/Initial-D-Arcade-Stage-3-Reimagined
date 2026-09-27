# Sound Room

The browser viewer previews the 74 playable named race songs from the audited
The Arcade Season 5 installation. Menu music, Additional BGM, Story music,
Unlisted songs and Movie audio are excluded. A_ONE_51 and dummy cues contain
no playable song audio. Original jackets and source provenance are retained.

The Unity Sound Room replaces the existing race music picker. It retains the
original game catalog, automatic music, custom MP3/OGG/WAV imports, folder scanning
and confirmed deletion, and adds the 74 Season 5 songs as full-length race music.
Preview playback is separate from race selection and ducks only the native music.
Favorites and preview volume are saved in userdata/sound-room.json. The chosen
Season 5 song is saved by stable key in the existing custom-music library.

## Rebuild

Run prepare_media.py to generate race previews and original artwork. Run
expand_library.py to reconstruct the approved race-only catalog, then build.py
for the web page and portable Sound Room.html. prepare_game.py packages original
song previews, Season 5 previews and full Season 5 OGGs into
Assets/StreamingAssets/SoundRoom. It prunes only obsolete files listed in its
previous generated manifest. Source game files remain unchanged.

serve.py hosts the browser viewer on loopback port 8769 with audio byte ranges.
check.cjs verifies playback, seeking, search, favorites, mobile layouts, offline
playback and native audio controls without JavaScript. Outputs and evidence are
under Verification/arcade-s5-music-audit-20260926/Viewer.

Unity verification uses the existing -idas3-race-music-smoke diagnostic with
-idas3-sound-room-check, and -idas3-custom-music-check for custom library regression.
