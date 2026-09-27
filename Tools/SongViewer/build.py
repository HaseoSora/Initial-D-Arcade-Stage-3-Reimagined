"""Build a self-contained music viewer and a smaller local-server version."""
from pathlib import Path
import base64, collections, html, json, re

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[1]
OUT=ROOT/'Verification/arcade-s5-music-audit-20260926/Viewer'
tracks=json.loads((OUT/'tracks.json').read_text('utf-8'))
counts=collections.Counter(t['category'] for t in tracks)
assert all(t.get('game') == 'The Arcade · Season 5' for t in tracks), 'Only Season 5 belongs in this viewer'
e=html.escape
def clock(seconds):
    n=round(seconds);return f'{n//60}:{n%60:02d}'
def icon(name):return f'<svg aria-hidden="true"><use href="#i-{name}"/></svg>'
def uri(path):
    p=OUT/path;kind={'.mp3':'audio/mpeg','.png':'image/png','.svg':'image/svg+xml'}[p.suffix]
    return f'data:{kind};base64,'+base64.b64encode(p.read_bytes()).decode('ascii')
def build(portable):
    media={}
    def source(path):
        if not portable:return path
        if path not in media:media[path]=uri(path)
        return media[path]
    rows=[]
    for index,t in enumerate(tracks):
        rows.append(f'''<article class="track-row" data-id="{e(t['id'])}">
          <button class="row-play" type="button" aria-label="Preview {e(t['title'])}"><span class="row-number">{index+1:02}</span>{icon('play')}</button>
          <img id="art-{e(t['id'])}" src="{source(t['art'])}" alt="" loading="lazy" width="42" height="49">
          <button type="button" class="row-info" aria-pressed="false"><strong>{e(t['title'])}</strong><span>{e(t['artist'] or t['category'])}</span></button>
          <span class="row-duration" aria-label="Full song length {clock(t['duration'])}">{clock(t['duration'])}</span>
          <button type="button" class="row-favorite" aria-label="Add {e(t['title'])} to favorites" aria-pressed="false">{icon('heart')}</button>
          <audio id="audio-{e(t['id'])}" class="fallback-audio" controls preload="none" aria-label="Preview of {e(t['title'])}" src="{source(t['audio'])}"></audio>
        </article>''')
    collections_markup=''.join(f'<button type="button" class="collection" data-category="{e(c)}" aria-pressed="false"><span>{e(c)}</span><b>{n}</b></button>' for c,n in counts.items())
    options='<option value="all">All collections</option>'+''.join(f'<option value="{e(c)}">{e(c)} · {n}</option>' for c,n in counts.items())
    replacements={'TRACK_COUNT':str(len(tracks)),'COLLECTION_COUNT':str(len(counts)),
        'STYLE':(HERE/'viewer.css').read_text('utf-8'),'SCRIPT':(HERE/'viewer.js').read_text('utf-8'),
        'TRACKS':json.dumps([{k:v for k,v in t.items() if k in ('id','title','artist','category','game','duration','previewDuration','waveform','sourceCueNames')} for t in tracks],ensure_ascii=False,separators=(',',':')).replace('<','\\u003c'),
        'ROWS':'\n'.join(rows),'COLLECTIONS':collections_markup,'MOBILE_OPTIONS':options,'INITIAL_ART':source(tracks[0]['art'])}
    text=(HERE/'index.html').read_text('utf-8')
    for key,value in replacements.items():text=text.replace('__'+key+'__',value)
    assert not re.search(r'__[A-Z_]+__',text)
    destination=OUT/('Sound Room.html' if portable else 'index.html')
    destination.write_text(text,'utf-8')
    print(destination,'bytes',destination.stat().st_size)
build(False)
build(True)
