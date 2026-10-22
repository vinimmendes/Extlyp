"""Pack only the new Blender crossbow renders, preserving existing resources."""
from pathlib import Path
from PIL import Image, ImageDraw
import uuid,re,json

ROOT=Path(__file__).resolve().parents[2]
SOURCE=ROOT/'Prototypes/PlayerWalk3D/HandCrossbow'
DEST=ROOT/'Assets/Resources/PlayerCharacter'
template=(DEST/'Pistol.png.meta').read_text(encoding='utf-8')
clips=[('Walk',1,32,DEST/'HandCrossbow.png'),
       ('EmptyWalk',1,32,DEST/'HandCrossbowEmpty.png'),
       ('Reload',41,49,DEST/'Actions/ReloadCrossbow_1.png'),
       ('Shot',97,8,DEST/'Actions/ShotCrossbow_1.png')]
report=[];preview=[]
legs=Image.open(DEST/'Legs.png').convert('RGBA').crop((0,256,256,512))
for name,first,count,target in clips:
    frames=[]
    for f in range(first,first+count):
        image=Image.open(SOURCE/name/f'{f:03}.png').convert('RGBA')
        if image.size!=(256,256) or image.getchannel('A').getextrema()[0]!=0:
            raise ValueError('Invalid frame '+str(f))
        frames.append(image)
    atlas=Image.new('RGBA',(2048,((count+7)//8)*256))
    for i,image in enumerate(frames):atlas.paste(image,(i%8*256,i//8*256))
    atlas.save(target)
    meta=Path(str(target)+'.meta')
    if not meta.exists():meta.write_text(re.sub(r'^guid: \w+','guid: '+uuid.uuid4().hex,template,flags=re.M),encoding='utf-8')
    views=[]
    for image in frames:
        composed=Image.alpha_composite(legs,image) if name not in ('Walk','EmptyWalk') else image
        view=Image.new('RGB',(512,544),(27,29,26))
        scaled=composed.resize((512,512),Image.Resampling.LANCZOS)
        view.paste(scaled,(0,32),scaled)
        ImageDraw.Draw(view).text((12,10),'Besta de mao - '+name,fill=(230,220,195))
        views.append(view)
    views[0].save(SOURCE/(name+'.gif'),save_all=True,append_images=views[1:],duration=42,loop=0,disposal=2)
    for index in ([8] if name in ('Walk','EmptyWalk') else [0,count//3,2*count//3,count-1]):preview.append((name,index,views[index]))
    report.append({'clip':name,'frames':count,'atlas':str(target.relative_to(ROOT)),'pixels':list(atlas.size)})
contact=Image.new('RGB',(512*3,544*((len(preview)+2)//3)),(27,29,26))
for i,(_,_,image) in enumerate(preview):contact.paste(image,(i%3*512,i//3*544))
contact.save(SOURCE/'poses.png')
(SOURCE/'manifest.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(json.dumps(report))
