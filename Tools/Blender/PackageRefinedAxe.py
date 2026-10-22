"""Export the refined weapon atlas/world sprite and compose the actual game resources."""
from pathlib import Path
from PIL import Image, ImageDraw
import json, re, uuid, shutil

ROOT=Path(__file__).resolve().parents[2]
SOURCE=ROOT/'Prototypes/PlayerWalk3D/AxeRefined'
DEST=ROOT/'Assets/Resources/PlayerCharacter/Actions'
WORLD=ROOT/'Assets/Resources/Items/Weapons'
WORLD.mkdir(exist_ok=True)

frames=[Image.open(SOURCE/'Weapon'/f'{i:03}.png').convert('RGBA') for i in range(1,27)]
atlas=Image.new('RGBA',(2048,1024))
for i,frame in enumerate(frames):
    bounds=frame.getbbox()
    if frame.size!=(256,256) or bounds is None:raise ValueError('Invalid weapon render '+str(i+1))
    if min(bounds[:2])<=0 or max(bounds[2:])>=256:raise ValueError('Clipped weapon render '+str(i+1))
    atlas.paste(frame,(i%8*256,i//8*256))
atlas.save(DEST/'AxeWeapon_1.png')
meta=DEST/'AxeWeapon_1.png.meta'
if not meta.exists():
    template=(DEST.parent/'Pistol.png.meta').read_text(encoding='utf-8')
    meta.write_text(re.sub(r'^guid: \w+','guid: '+uuid.uuid4().hex,template,flags=re.M),encoding='utf-8')

worldAsset=WORLD/'BattleAxeWorld.png'
shutil.copyfile(SOURCE/'machado-chao.png',worldAsset)
meta=Path(str(worldAsset)+'.meta')
if not meta.exists():
    template=(ROOT/'Assets/Resources/Items/IconsV2/BattleAxe.png.meta').read_text(encoding='utf-8')
    template=re.sub(r'^guid: \w+','guid: '+uuid.uuid4().hex,template,flags=re.M)
    template=re.sub(r'(?m)^(\s+spriteID:) \w+',r'\1 '+uuid.uuid4().hex,template)
    meta.write_text(template,encoding='utf-8')
worldGuid=re.search(r'^guid: (\w+)',meta.read_text(),re.M).group(1)
folderMeta=Path(str(WORLD)+'.meta')
if not folderMeta.exists():folderMeta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n',encoding='utf-8')

# Compose straight from the exported atlases, including their distinct world framing.
bodyAtlas=Image.open(DEST/'AxeAttack_1.png').convert('RGBA')
weaponAtlas=Image.open(DEST/'AxeWeapon_1.png').convert('RGBA')
legs=Image.open(DEST.parent/'Legs.png').convert('RGBA').crop((0,256,256,512))
views=[];size=896
for i in range(26):
    rect=(i%8*256,i//8*256,i%8*256+256,i//8*256+256)
    upper=bodyAtlas.crop(rect);weapon=weaponAtlas.crop(rect)
    body=Image.alpha_composite(legs,upper).resize((512,512),Image.Resampling.LANCZOS)
    # Weapon spans 5.1 units; body spans 3.4, so 768 vs 512 pixels at equal scale.
    art=weapon.resize((768,768),Image.Resampling.LANCZOS)
    layer=Image.new('RGBA',(size,size));layer.alpha_composite(art,(64,64));layer.alpha_composite(body,(192,192))
    if i in [0,7,10,13,25]:layer.save(SOURCE/f'pose-{i+1:02}.png')
    view=Image.new('RGB',(size,size),(28,31,28));view.paste(layer,(0,0),layer)
    phase='preparacao' if i<10 else 'impacto' if i==10 else 'recuperacao'
    ImageDraw.Draw(view).text((18,18),f'Machado refinado | {i+1:02}/26 | {phase}',fill=(235,226,205))
    views.append(view)
durations=[40]*10+[round((i+1)*65/16)*10-round(i*65/16)*10 for i in range(16)]
views[0].save(SOURCE/'ataque-machado-refinado.gif',save_all=True,append_images=views[1:],duration=durations,loop=0,disposal=2)
poses=Image.new('RGB',(384*5,384*2),(28,31,28))
for i,f in enumerate([0,3,6,7,9,10,13,17,21,25]):
    poses.paste(views[f].resize((384,384),Image.Resampling.LANCZOS),(i%5*384,i//5*384))
poses.save(SOURCE/'poses-machado-refinado.png')

# Near-game-size comparison on light/dark surroundings, using the same composite.
readability=Image.new('RGB',(720,400),(28,31,28));draw=ImageDraw.Draw(readability)
draw.rectangle((360,0,720,400),fill=(113,111,91))
for offset in [0,360]:
    draw.text((offset+16,16),'Guarda / recuo / impacto / retorno',fill=(235,226,205))
    for n,f in enumerate([0,7,10,25]):
        pose=Image.open(SOURCE/f'pose-{f+1:02}.png').convert('RGBA').resize((176,176),Image.Resampling.LANCZOS)
        readability.paste(pose,(offset+(n%2)*180,36+(n//2)*160),pose)
readability.save(SOURCE/'leitura-tamanho-jogo.png')
manifest={'bodyAtlas':'AxeAttack_1.png','weaponAtlas':'AxeWeapon_1.png','frames':26,'impactFrame':11,
    'cell':256,'bodyWorldSize':3.4,'weaponWorldSize':5.1,'weaponPixelsPerUnit':256/5.1,
    'worldAsset':str(worldAsset.relative_to(ROOT)),'worldGuid':worldGuid,
    'timing':{'windup':.4,'recovery':.65},'applyWeaponGripAgain':False}
(SOURCE/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8')
print(json.dumps(manifest))
