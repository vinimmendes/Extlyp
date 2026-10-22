"""Pack axe upper-body frames and compose the existing generated weapon for delivery."""
from pathlib import Path
from PIL import Image, ImageDraw
import json, math, re, uuid, shutil

ROOT=Path(__file__).resolve().parents[2]
SOURCE=ROOT/'Prototypes/PlayerWalk3D/AxeAttack'
DEST=ROOT/'Assets/Resources/PlayerCharacter/Actions'
frames=[Image.open(SOURCE/'Upper'/f'{f:03}.png').convert('RGBA') for f in range(1,27)]
atlas=Image.new('RGBA',(2048,1024))
for i,frame in enumerate(frames):
    if frame.size!=(256,256):raise ValueError('Unexpected frame size')
    box=frame.getbbox()
    if box is None or min(box[:2])<=0 or max(box[2:])>=256:raise ValueError('Clipped upper-body frame '+str(i+1))
    atlas.paste(frame,(i%8*256,i//8*256))
atlas.save(DEST/'AxeAttack_1.png')
shutil.copyfile(SOURCE/'AxeGrip.json',DEST/'AxeGrip.json')
template=(DEST.parent/'Pistol.png.meta').read_text(encoding='utf-8')
for name in ['AxeAttack_1.png','AxeGrip.json']:
    meta=DEST/(name+'.meta')
    if not meta.exists():
        guid=uuid.uuid4().hex
        meta.write_text(re.sub(r'^guid: \w+','guid: '+guid,template,flags=re.M) if name.endswith('.png') else
            'fileFormatVersion: 2\nguid: '+guid+'\nTextScriptImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n',encoding='utf-8')

# Same pixel coordinates, widths, colors and compositing as MeleeWeaponArt.Get(BattleAxe).
pixels=[(0,0,0,0)]*(128*128)
def line(a,b,width,color):
    dx=b[0]-a[0];dy=b[1]-a[1];length=dx*dx+dy*dy
    for y in range(128):
        for x in range(128):
            t=max(0,min(1,((x+.5-a[0])*dx+(y+.5-a[1])*dy)/max(1,length)))
            alpha=max(0,min(1,width*.5+.5-math.hypot(x+.5-a[0]-dx*t,y+.5-a[1]-dy*t)))
            if alpha>0:
                old=pixels[y*128+x];pixels[y*128+x]=tuple(old[c]+(color[c]-old[c])*alpha for c in range(4))
line((14,64),(106,64),8,(.4,.27,.15,1))
line((94,43),(94,86),16,(.73,.77,.73,1))
line((103,38),(103,91),9,(.92,.91,.82,1))
line((28,59),(28,69),4,(.64,.49,.27,1))
line((37,59),(37,69),4,(.64,.49,.27,1))
weapon=Image.new('RGBA',(128,128))
weapon.putdata([tuple(round(c*255) for c in pixels[(127-y)*128+x]) for y in range(128) for x in range(128)])
weapon.save(SOURCE/'machado-arte-atual.png')
grips=json.loads((SOURCE/'AxeGrip.json').read_text())['frames']
legs=Image.open(DEST.parent/'Legs.png').convert('RGBA').crop((0,256,256,512))
views=[];size=896;ppu=512/3.4
for i,(frame,g) in enumerate(zip(frames,grips)):
    body=Image.alpha_composite(legs,frame).resize((512,512),Image.Resampling.LANCZOS)
    layer=Image.new('RGBA',(size,size));layer.alpha_composite(body,((size-512)//2,(size-512)//2))
    k=1.55*ppu/128;kx=k*g['scaleX'];ky=k*g['scaleY']
    a=math.radians(g['angle']);c=math.cos(a);s=math.sin(a)
    gx=size/2+g['x']*ppu;gy=size/2-g['y']*ppu
    sx=50;sy=64
    transform=(c/kx,-s/kx,sx+(-c*gx+s*gy)/kx,s/ky,c/ky,sy+(-s*gx-c*gy)/ky)
    art=weapon.transform((size,size),Image.Transform.AFFINE,transform,Image.Resampling.BICUBIC)
    # The upper body is above the axe in Unity so fingers occlude the handle.
    layer=Image.alpha_composite(art,layer)
    bounds=layer.getbbox()
    if bounds[0]<=0 or bounds[1]<=0 or bounds[2]>=size or bounds[3]>=size:raise ValueError('Clipped composed frame '+str(i+1))
    view=Image.new('RGB',(size,size),(28,31,28));view.paste(layer,(0,0),layer)
    ImageDraw.Draw(view).text((18,18),f'Machado | quadro {i+1:02}/26',fill=(235,226,205))
    views.append(view)
durations=[40]*10+[round((i+1)*65/16)*10-round(i*65/16)*10 for i in range(16)]
views[0].save(SOURCE/'ataque-machado.gif',save_all=True,append_images=views[1:],duration=durations,loop=0,disposal=2)
shutil.copyfile(SOURCE/'ataque-machado.gif',SOURCE/'ataque-machado-recuo.gif')
contact=Image.new('RGB',(384*5,384*2),(28,31,28))
for i,f in enumerate([0,3,6,7,9,10,13,17,21,25]):
    contact.paste(views[f].resize((384,384),Image.Resampling.LANCZOS),(i%5*384,i//5*384))
contact.save(SOURCE/'poses-machado.png')
print(json.dumps({'frames':26,'atlas':'AxeAttack_1.png','preview':str(SOURCE/'ataque-machado.gif')}))
