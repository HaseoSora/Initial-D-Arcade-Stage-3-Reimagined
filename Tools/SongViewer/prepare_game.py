"""Package Sound Room previews and full Season 5 songs for the Unity player."""
from pathlib import Path
import concurrent.futures, hashlib, json, shutil, subprocess

ROOT=Path(__file__).resolve().parents[2]
AUDIT=ROOT/'Verification/arcade-s5-music-audit-20260926'
VIEWER=AUDIT/'Viewer'
OUT=ROOT/'Assets/StreamingAssets/SoundRoom'
WORK=VIEWER/'work'
VGM=AUDIT/'Tools/vgmstream/vgmstream-cli.exe'
FFMPEG=next((AUDIT/'Tools/imageio-ffmpeg-0.6.0').glob('ffmpeg*.exe'))
read=lambda p:json.loads(p.read_text('utf-8-sig'))
source_metadata={t['streamInfo']['index']:t for t in read(AUDIT/'stream-metadata.json')}
for name in ('previews','artwork','songs'):(OUT/name).mkdir(parents=True,exist_ok=True)

def run(args):
    r=subprocess.run([str(a) for a in args],capture_output=True,creationflags=subprocess.CREATE_NO_WINDOW)
    if r.returncode or b'failed opening' in r.stderr.lower():raise RuntimeError(r.stderr.decode('utf8','replace'))

def package(item):
    t,index=item
    key=t['id'];preview=VIEWER/t['audio'];cover=VIEWER/t['art']
    assert hashlib.sha256(preview.read_bytes()).hexdigest()==t['previewSha256']
    shutil.copyfile(preview,OUT/'previews'/preview.name)
    shutil.copyfile(cover,OUT/'artwork'/cover.name)
    full=OUT/'songs'/(key+'.ogg')
    evidence=VIEWER/'previews'/(key+'.full.json')
    source=Path(t.get('source',AUDIT/'BGM.awb'))
    stream=t['sourceStreamIndex']
    authored=source_metadata[stream];loop=authored['loopingInfo']
    assert 0<=loop['start']<loop['end']<=authored['numberOfSamples']
    fingerprint=dict(source=str(source),bytes=source.stat().st_size,modified=source.stat().st_mtime_ns,stream=stream)
    cached=read(evidence) if evidence.exists() else {}
    if not full.exists() or cached.get('fingerprint')!=fingerprint or hashlib.sha256(full.read_bytes()).hexdigest()!=cached.get('sha256'):
        wav=WORK/(key+'-game.wav')
        try:
            run([VGM,'-i','-s',stream,'-o',wav,source])
            assert wav.is_file()
            run([FFMPEG,'-hide_banner','-loglevel','error','-y','-i',wav,'-vn','-ac','2','-ar','48000','-c:a','libvorbis','-q:a','4','-map_metadata','-1',full])
            run([FFMPEG,'-hide_banner','-loglevel','error','-i',full,'-f','null','-'])
            evidence.write_text(json.dumps(dict(fingerprint=fingerprint,sha256=hashlib.sha256(full.read_bytes()).hexdigest())),'utf8')
        finally:
            assert wav.resolve().parent==WORK.resolve();wav.unlink(missing_ok=True)
    return dict(key='arcade5.'+key,id=2000+index,title=t['title'],artist=t['artist'],stage=11,
                collection=t['category'],duration=t['duration'],preview='previews/'+preview.name,
                cover='artwork/'+cover.name,audio='songs/'+full.name,waveform=t['waveform'],
                loopStart=loop['start'],loopEnd=loop['end'],loopSampleRate=authored['sampleRate'])

tracks=[]
previous=read(OUT/'catalog.json')['tracks'] if (OUT/'catalog.json').exists() else []
native=read(ROOT/'Native/data/original_audio/streams/music_catalog.json')['tracks']
for song in native:
    t=read(VIEWER/'previews'/(song['id']+'.json'));p=VIEWER/t['audio']
    assert hashlib.sha256(p.read_bytes()).hexdigest()==t['previewSha256']
    shutil.copyfile(p,OUT/'previews'/p.name)
    tracks.append(dict(key=song['id'],id=song['index'],title=song['title'],artist=song['artist'],stage=song['sourceStage'],
                       collection='Special Stage' if song['sourceStage']==10 else 'Arcade Stage '+str(song['sourceStage']),
                       duration=song['durationSeconds'],preview='previews/'+p.name,cover='',audio='',waveform=t['waveform']))
s5=read(VIEWER/'tracks.json');assert len(s5)==74 and all(t['game']=='The Arcade · Season 5' for t in s5)
with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
    for i,t in enumerate(pool.map(package,((t,i) for i,t in enumerate(s5))),1):
        tracks.append(t)
        if i%10==0:print('Packaged full songs',i,'/',len(s5),flush=True)
assert len({t['key'] for t in tracks})==len(tracks)
(OUT/'catalog.json').write_text(json.dumps(dict(version=1,tracks=tracks),ensure_ascii=False,separators=(',',':')),'utf8')
# Remove only media owned by the previous generated manifest, never user music.
used={t.get(field) for t in tracks for field in ('preview','cover','audio') if t.get(field)}
old={t.get(field) for t in previous for field in ('preview','cover','audio') if t.get(field)}
for relative in sorted(old-used):
    target=(OUT/relative).resolve()
    assert target.is_relative_to(OUT.resolve()) and target.parent.name in ('previews','artwork','songs')
    target.unlink(missing_ok=True)
    Path(str(target)+'.meta').unlink(missing_ok=True)
print('Packaged',len(tracks),'preview records and',len(s5),'full-length Season 5 tracks.',flush=True)
