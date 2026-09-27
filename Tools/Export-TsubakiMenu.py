"""Build the existing two-image menu bank from Tsubaki's own road and scenery."""
from pathlib import Path
import argparse,json,struct
from PIL import Image,ImageDraw,ImageFont
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--pack',type=Path,required=True)
parser.add_argument('--background',type=Path)
parser.add_argument('--preview',type=Path,required=True)
args=parser.parse_args();args.preview.mkdir(parents=True,exist_ok=True)
data=(args.pack/'road.bin').read_bytes();assert data[:4]==b'HKR1'
count=struct.unpack_from('<I',data,4)[0]
points=list(struct.iter_unpack('<3f',data[8:8+count*12]))
meta=json.loads((args.pack/'scene.json').read_text())
start,end=meta['checkpoints'][0],meta['checkpoints'][-1]
route=points[start:end+1:4]+[points[end]]
def font(size,bold=False):return ImageFont.truetype('C:/Windows/Fonts/'+('arialbd.ttf' if bold else 'timesbi.ttf'),size)
background=Image.open(args.background).convert('RGB') if args.background else Image.new('RGB',(1280,720),(20,29,36))
card=background.resize((640,360),Image.Resampling.LANCZOS).crop((0,0,640,312)).convert('RGBA')
card.alpha_composite(Image.new('RGBA',card.size,(0,0,0,40)))
draw=ImageDraw.Draw(card)
draw.text((16,26),'Tsubaki Line',font=font(57),fill='black',stroke_width=2,stroke_fill='white')
draw.line((0,102,398,102),fill='white',width=1)
# World X/Z retain the established course-menu orientation. Forward start is downhill.
loX=min(p[0] for p in route);hiX=max(p[0] for p in route)
loZ=min(p[2] for p in route);hiZ=max(p[2] for p in route)
scale=min(184/(hiX-loX),218/(hiZ-loZ))
line=[(423+(p[0]-loX)*scale,45+(p[2]-loZ)*scale) for p in route]
draw.line(line,fill=(0,0,0,190),width=6);draw.line(line,fill='white',width=2)
for position,label in ((line[0],'DOWNHILL'),(line[-1],'UPHILL')):
    x,y=position;draw.ellipse((x-3,y-3,x+3,y+3),fill='white')
    draw.text((min(548,max(416,x-28)),min(288,max(8,y-20 if label=='DOWNHILL' else y+7))),label,font=font(13,True),fill='white',stroke_width=1,stroke_fill='black')
tile=background.resize((96,64),Image.Resampling.LANCZOS).convert('RGBA');draw=ImageDraw.Draw(tile)
draw.text((2,40),'Tsubaki',font=font(21),fill='white',stroke_width=1,stroke_fill='black')
with (args.pack/'menu.idastex').open('wb') as output:
    output.write(b'IDAS3T1\0'+struct.pack('<II',1,2))
    for i,bitmap in enumerate((card,tile)):
        output.write(struct.pack('<IIII',i,*bitmap.size,bitmap.width*bitmap.height*4));output.write(bitmap.tobytes())
        bitmap.save(args.preview/('card.png' if i==0 else 'tile.png'))
