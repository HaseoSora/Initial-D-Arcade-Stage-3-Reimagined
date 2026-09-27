"""Rebuild the approved race-only library from audited preview records."""
from pathlib import Path
import collections, json
ROOT=Path(__file__).resolve().parents[2]
AUDIT=ROOT/'Verification/arcade-s5-music-audit-20260926'
OUT=AUDIT/'Viewer'
read=lambda p:json.loads(p.read_text('utf-8-sig'))
songs=read(AUDIT/'race-songs.json')
tracks=[dict(read(OUT/'previews'/(s['id']+'.json')),game='The Arcade · Season 5') for s in songs if not s['placeholder'] and s['audio']]
assert len(tracks)==74 and len({t['id'] for t in tracks})==74
assert all((OUT/t['audio']).is_file() for t in tracks)
(OUT/'tracks.json').write_text(json.dumps(tracks,ensure_ascii=False,separators=(',',':')),'utf8')
report=dict(tracks=len(tracks),categories=dict(collections.Counter(t['category'] for t in tracks)),
            scope='Playable named Season 5 race songs only',
            excluded=['Menu music','Additional BGM','Story music','Unlisted songs','Movie audio'],
            unavailableNamedSongs=[dict(id=s['id'],title=s['title'],reason='Empty source cue') for s in songs if not s['placeholder'] and not s['audio']])
(OUT/'library-coverage.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),'utf8')
print('Prepared',len(tracks),'Season 5 race songs.')
