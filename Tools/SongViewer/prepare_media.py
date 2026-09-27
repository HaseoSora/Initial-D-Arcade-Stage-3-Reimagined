"""Prepare portable previews from audited local Season 5 audio, without changing the game."""
from pathlib import Path
import array, concurrent.futures, hashlib, importlib.util, json, math, shutil, subprocess, sys, wave

ROOT=Path(__file__).resolve().parents[2]
AUDIT=ROOT/'Verification/arcade-s5-music-audit-20260926'
OUT=AUDIT/'Viewer'
OUT.mkdir(exist_ok=True)
FFMPEG=next((AUDIT/'Tools/imageio-ffmpeg-0.6.0').glob('ffmpeg*.exe'))
VGM=AUDIT/'Tools/vgmstream/vgmstream-cli.exe'
read=lambda p:json.loads(p.read_text('utf-8-sig'))
songs=[s for s in read(AUDIT/'race-songs.json') if s['audio'] and not s['placeholder']]
cat_names={'頭文字DAC\\nオリジナル':'The Arcade originals','東方Project':'Touhou Project','Dave Rodgers':'Dave Rodgers',
    '20th\\nanniversary':'20th Anniversary','初音ミク\\n-Project DIVA-':'Project DIVA','CHUNITHM':'CHUNITHM','Hot-Version':'Hot-Version','アニメ版MFゴースト':'MF Ghost'}
def category(s):
    value=next(c for c in s['categories'] if c!='ALL')
    return cat_names.get(value,value.replace('\\\\n',' / ').replace('\\n',' / '))

# Extract only the eight source album jackets and retain unmodified PNG exports.
sys.path.insert(0,str(ROOT/'Verification/arcade-s3-ornaments-20260923'))
from extract_ornaments import Pak
old=ROOT/'Verification/arcade-s5-audit-20260926'
active={}
for filename in ('base-index.json','patch-index.json'):
    record=read(old/filename);pak=Pak(record['pak'],old/'patch-index-decrypted.bin' if filename.startswith('patch') else None)
    for name,entry in pak.entries.items():active[pak.mount.removeprefix('../../../')+name]=(pak,name,entry)
jackets=sorted({s['jacketLarge'].split('.')[0].replace('/Game/','GameProject/Content/') for s in songs})
art_root=AUDIT/'Jackets'
art_proof=[]
for stem in jackets:
    for ext in ('.uasset','.uexp','.ubulk'):
        key=stem+ext
        if key not in active:continue
        pak,name,entry=active[key];assert not entry['flags']&2
        data=pak.extract(name);dest=art_root/'Raw'/key;dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(data)
        art_proof.append(dict(path=key,sha256=hashlib.sha256(data).hexdigest(),source=pak.path.name))
args=['-export','-game=ue4.24','-png','-nooverwrite',f'-path={art_root/"Raw"}',f'-out={art_root/"Exports"}']
args += ['-pkg='+s+'.uasset' for s in jackets[1:]]+[jackets[0]+'.uasset']
rsp=art_root/'export.rsp';rsp.write_text('\n'.join('"'+a+'"' for a in args),'utf-8')
with (art_root/'export.log').open('w') as log:
    subprocess.run(['D:/Codex/tools/ueviewer/umodel_64.exe','@'+str(rsp)],stdout=log,stderr=subprocess.STDOUT,check=True,creationflags=subprocess.CREATE_NO_WINDOW)
(art_root/'provenance.json').write_text(json.dumps(art_proof,indent=2),'utf-8')
(OUT/'artwork').mkdir(exist_ok=True);(OUT/'previews').mkdir(exist_ok=True)
for stem in jackets:
    source=art_root/'Exports'/(stem.removeprefix('GameProject/Content/')+'.png')
    shutil.copyfile(source,OUT/'artwork'/source.name)

def prepare(song):
    key=song['id'];audio=song['audio'][0];cached=OUT/'previews'/(key+'.json')
    if cached.exists() and (OUT/'previews'/(key+'.mp3')).exists():return read(cached)
    temp=AUDIT/('viewer-preview-'+key+'.wav')
    subprocess.run([str(VGM),'-i','-s',str(audio['index']),'-o',str(temp),str(AUDIT/'BGM.awb')],check=True,capture_output=True,creationflags=subprocess.CREATE_NO_WINDOW)
    try:
        start=min(audio['loops']['start']/audio['sampleRate'],max(0,audio['durationSeconds']-30))
        with wave.open(str(temp),'rb') as wav:
            wav.setpos(round(start*wav.getframerate()));raw=wav.readframes(round(30*wav.getframerate()));channels=wav.getnchannels()
            assert wav.getsampwidth()==2 and channels==2
        pcm=array.array('h',raw)
        if sys.byteorder!='little':pcm.byteswap()
        values=[]
        for i in range(96):
            a=len(pcm)*i//96;b=len(pcm)*(i+1)//96
            values.append(math.sqrt(sum(float(v)*v for v in pcm[a:b:12])/max(1,len(pcm[a:b:12]))))
        peak=max(values);waveform=[round(max(.055,v/peak),3) for v in values] if peak else [.055]*96
        destination=OUT/'previews'/(key+'.mp3')
        subprocess.run([str(FFMPEG),'-hide_banner','-loglevel','error','-y','-ss',str(start),'-i',str(temp),'-t','30',
            '-af','afade=t=in:st=0:d=0.08,afade=t=out:st=29.5:d=0.5','-ar','44100','-ac','2',
            '-codec:a','libmp3lame','-b:a','96k','-map_metadata','-1',str(destination)],check=True,capture_output=True,creationflags=subprocess.CREATE_NO_WINDOW)
        result=dict(id=key,title=song['title'].strip(),artist=song['artist'].strip(),category=category(song),
            duration=round(audio['durationSeconds'],3),previewDuration=30,previewStart=round(start,3),
            audio='previews/'+key+'.mp3',art='artwork/'+song['jacketLarge'].split('.')[0].rsplit('/',1)[-1]+'.png',
            waveform=waveform,sourceStreamIndex=audio['index'],previewSha256=hashlib.sha256(destination.read_bytes()).hexdigest())
        cached.write_text(json.dumps(result,ensure_ascii=False),'utf-8')
        return result
    finally:
        assert temp.resolve().parent==AUDIT.resolve()
        if temp.exists():temp.unlink()

with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
    tracks=[]
    for track in pool.map(prepare,songs):
        track['game']='The Arcade · Season 5'
        tracks.append(track)
        if len(tracks)%10==0:print('Prepared previews',len(tracks),'/',len(songs),flush=True)
(OUT/'tracks.json').write_text(json.dumps(tracks,ensure_ascii=False,separators=(',',':')),'utf-8')
print('Prepared',len(tracks),'30-second previews and',len(jackets),'original album jackets.',flush=True)
