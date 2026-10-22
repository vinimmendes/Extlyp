"""Package Blender upper-body renders and show the same held-art transform as Unity."""
from pathlib import Path
from PIL import Image, ImageDraw
import json, math, re, uuid, shutil

ROOT=Path(__file__).resolve().parents[2]
SOURCE=ROOT/'Prototypes/PlayerWalk3D/SwordAttack'
DEST=ROOT/'Assets/Resources/PlayerCharacter/Actions'
frames=[Image.open(SOURCE/'Upper'/f'{f:03}.png').convert('RGBA') for f in range(1,21)]
atlas=Image.new('RGBA',(2048,768))
for i,frame in enumerate(frames):
    if frame.size!=(256,256):raise ValueError('Unexpected render size')
    atlas.paste(frame,(i%8*256,i//8*256))
atlas.save(DEST/'SwordAttack_1.png')
shutil.copyfile(SOURCE/'SwordGrip.json',DEST/'SwordGrip.json')
template=(DEST.parent/'Pistol.png.meta').read_text(encoding='utf-8')
for name in ['SwordAttack_1.png','SwordGrip.json']:
    meta=DEST/(name+'.meta')
    if not meta.exists():
        guid=uuid.uuid4().hex
        meta.write_text(re.sub(r'^guid: \w+','guid: '+guid,template,flags=re.M) if name.endswith('.png') else
            'fileFormatVersion: 2\nguid: '+guid+'\nTextScriptImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n',encoding='utf-8')
script=ROOT/'Assets/Scripts/Players/PlayerMeleeActionArt.cs.meta'
if not script.exists():
    script.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n',encoding='utf-8')

grips=json.loads((SOURCE/'SwordGrip.json').read_text())['frames']
legs=Image.open(DEST.parent/'Legs.png').convert('RGBA').crop((0,256,256,512))
weapon=Image.open(ROOT/'Assets/Resources/Items/IconsV2/RustySword.png').convert('RGBA')
views=[]
size=896;ppu=512/3.4
for i,(frame,grip) in enumerate(zip(frames,grips)):
    body=Image.alpha_composite(legs,frame).resize((512,512),Image.Resampling.LANCZOS)
    layer=Image.new('RGBA',(size,size));layer.alpha_composite(body,((size-512)//2,(size-512)//2))
    # Full-sprite rect, central pivot, 1.55-unit longest side; handle UV (.22,.22).
    k=1.55*ppu/max(weapon.size)
    a=math.radians(grip['angle']-45);c=math.cos(a);s=math.sin(a)
    gx=size/2+grip['x']*ppu;gy=size/2-grip['y']*ppu
    sx=.22*weapon.width;sy=.78*weapon.height
    transform=(c/k,-s/k,sx+(-c*gx+s*gy)/k,s/k,c/k,sy+(-s*gx-c*gy)/k)
    sword=weapon.transform((size,size),Image.Transform.AFFINE,transform,Image.Resampling.BICUBIC)
    layer=Image.alpha_composite(layer,sword)
    bounds=layer.getbbox()
    if bounds[0]<=0 or bounds[1]<=0 or bounds[2]>=size or bounds[3]>=size:raise ValueError('Clipped preview frame '+str(i+1))
    view=Image.new('RGB',(size,size),(28,31,28));view.paste(layer,(0,0),layer)
    ImageDraw.Draw(view).text((18,18),f'Espada | quadro {i+1:02}/20',fill=(235,226,205))
    views.append(view)
durations=[round((i+1)*30/8)*10-round(i*30/8)*10 for i in range(8)]+[
    round((i+1)*50/12)*10-round(i*50/12)*10 for i in range(12)]
views[0].save(SOURCE/'ataque-espada.gif',save_all=True,append_images=views[1:],duration=durations,loop=0,disposal=2)
contact=Image.new('RGB',(384*5,384*2),(28,31,28))
for i,f in enumerate([0,3,6,7,8,9,11,14,17,19]):
    contact.paste(views[f].resize((384,384),Image.Resampling.LANCZOS),(i%5*384,i//5*384))
contact.save(SOURCE/'poses-espada.png')
print(json.dumps({'atlas':'SwordAttack_1.png','frames':20,'preview':str(SOURCE/'ataque-espada.gif')}))
