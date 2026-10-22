"""Package additional Blender renders; preserve approved Walk/Web atlases and GUIDs."""
from pathlib import Path
import json, uuid, re
from PIL import Image, ImageDraw

ROOT=Path(__file__).resolve().parents[2]
SOURCE=Path(__file__).resolve().parent/'Actions'
DEST=ROOT/'Assets/Resources/Monsters/Weaver'
template=(DEST/'Walk.png.meta').read_text(encoding='utf-8')
status=json.loads((SOURCE/'render-status.json').read_text())
if not status['finished'] or status['error'] or status['done']!=65:
    raise RuntimeError('Blender renders incomplete')
clips={}
for name,start in [('Dash',201),('Descend',301)]:
    frames=[]
    for frame in range(start,start+32):
        with Image.open(SOURCE/name/f'{frame}.png') as source:
            frames.append(source.convert('RGBA').transpose(Image.Transpose.ROTATE_270).resize((256,256),Image.Resampling.LANCZOS))
    clips[name]=frames
    atlas=Image.new('RGBA',(2048,1024))
    for i,frame in enumerate(frames): atlas.paste(frame,((i%8)*256,(i//8)*256))
    target=DEST/(name+'.png'); atlas.save(target)
    meta=Path(str(target)+'.meta')
    if not meta.exists(): meta.write_text(re.sub(r'guid: [0-9a-f]+','guid: '+uuid.uuid4().hex,template),encoding='utf-8')
with Image.open(SOURCE/'Nest/401.png') as source:
    nest=source.convert('RGBA').transpose(Image.Transpose.ROTATE_270).resize((256,256),Image.Resampling.LANCZOS)
target=DEST/'Nest.png'; nest.save(target)
meta=Path(str(target)+'.meta')
if not meta.exists(): meta.write_text(re.sub(r'guid: [0-9a-f]+','guid: '+uuid.uuid4().hex,template),encoding='utf-8')

sheet=Image.new('RGB',(1024,3*280),(31,35,32)); draw=ImageDraw.Draw(sheet)
for row,(name,frames) in enumerate([('Dash',clips['Dash']),('Descend',clips['Descend']),('Nest',[nest]*4)]):
    for col,index in enumerate([0,11,18,31]):
        frame=frames[index] if name!='Nest' else nest
        sheet.paste(frame,(col*256,row*280),frame)
        draw.text((col*256+8,row*280+257),name+' '+str(index+1),fill=(222,215,190))
sheet.save(SOURCE/'actions-preview.png')
for name,frames in clips.items():
    preview=[]
    for frame in frames:
        image=Image.new('RGB',(256,256),(31,35,32)); image.paste(frame,(0,0),frame); preview.append(image)
    preview[0].save(SOURCE/(name.lower()+'.gif'),save_all=True,append_images=preview[1:],duration=60 if name=='Descend' else 45,loop=0)

script=ROOT/'Assets/Scripts/Enemies/Monster.Weaver.cs'
meta=Path(str(script)+'.meta')
if not meta.exists(): meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n',encoding='utf-8')
print('Packaged Dash (32), Descend (32), Nest (1); alpha, +X, original 2.4-unit frame.')
