"""Package the two-handed spear action and compose the same atlases used by Unity."""
from pathlib import Path
from PIL import Image, ImageDraw
import json, re, uuid, shutil

ROOT=Path(__file__).resolve().parents[2]
SOURCE=ROOT/'Prototypes/PlayerWalk3D/SpearAttack'
DEST=ROOT/'Assets/Resources/PlayerCharacter/Actions'
WORLD=ROOT/'Assets/Resources/Items/Weapons'
WORLD.mkdir(exist_ok=True)
template=(DEST.parent/'Pistol.png.meta').read_text(encoding='utf-8')
for kind,name in [('Upper','SpearAttack'),('Weapon','SpearWeapon')]:
    atlas=Image.new('RGBA',(2048,768))
    for i in range(20):
        frame=Image.open(SOURCE/kind/f'{i+1:03}.png').convert('RGBA');bounds=frame.getbbox()
        if frame.size!=(256,256) or bounds is None:raise ValueError('Invalid '+kind+' frame '+str(i+1))
        if min(bounds[:2])<=0 or max(bounds[2:])>=256:raise ValueError('Clipped '+kind+' frame '+str(i+1))
        atlas.paste(frame,(i%8*256,i//8*256))
    path=DEST/(name+'_1.png');atlas.save(path);meta=Path(str(path)+'.meta')
    if not meta.exists():meta.write_text(re.sub(r'^guid: \w+','guid: '+uuid.uuid4().hex,template,flags=re.M),encoding='utf-8')
shutil.copyfile(SOURCE/'SpearGrip.json',DEST/'SpearGrip.json')
meta=DEST/'SpearGrip.json.meta'
if not meta.exists():meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+
    '\nTextScriptImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n',encoding='utf-8')
worldAsset=WORLD/'SpearWorld.png';shutil.copyfile(SOURCE/'lanca-chao.png',worldAsset)
meta=Path(str(worldAsset)+'.meta')
if not meta.exists():
    spriteTemplate=(ROOT/'Assets/Resources/Items/IconsV2/Spear.png.meta').read_text(encoding='utf-8')
    spriteTemplate=re.sub(r'^guid: \w+','guid: '+uuid.uuid4().hex,spriteTemplate,flags=re.M)
    spriteTemplate=re.sub(r'(?m)^(\s+spriteID:) \w+',r'\1 '+uuid.uuid4().hex,spriteTemplate)
    meta.write_text(spriteTemplate,encoding='utf-8')
worldGuid=re.search(r'^guid: (\w+)',meta.read_text(),re.M).group(1)

bodyAtlas=Image.open(DEST/'SpearAttack_1.png').convert('RGBA')
weaponAtlas=Image.open(DEST/'SpearWeapon_1.png').convert('RGBA')
legs=Image.open(DEST.parent/'Legs.png').convert('RGBA').crop((0,256,256,512))
views=[];size=896
for i in range(20):
    rect=(i%8*256,i//8*256,i%8*256+256,i//8*256+256)
    body=Image.alpha_composite(legs,bodyAtlas.crop(rect)).resize((512,512),Image.Resampling.LANCZOS)
    weapon=weaponAtlas.crop(rect).resize((768,768),Image.Resampling.LANCZOS)
    layer=Image.new('RGBA',(size,size));layer.alpha_composite(weapon,(64,64));layer.alpha_composite(body,(192,192))
    if i in [0,7,8,11,19]:layer.save(SOURCE/f'pose-{i+1:02}.png')
    view=Image.new('RGB',(size,size),(28,31,28));view.paste(layer,(0,0),layer)
    phase='preparacao' if i<8 else 'impacto' if i==8 else 'recuperacao'
    ImageDraw.Draw(view).text((18,18),f'Lanca | quadro {i+1:02}/20 | {phase}',fill=(235,226,205));views.append(view)
durations=[round((i+1)*30/8)*10-round(i*30/8)*10 for i in range(8)]+[
    round((i+1)*50/12)*10-round(i*50/12)*10 for i in range(12)]
views[0].save(SOURCE/'estocada-lanca.gif',save_all=True,append_images=views[1:],duration=durations,loop=0,disposal=2)
poses=Image.new('RGB',(384*5,384*2),(28,31,28))
for i,f in enumerate([0,2,5,7,8,9,11,14,17,19]):
    poses.paste(views[f].resize((384,384),Image.Resampling.LANCZOS),(i%5*384,i//5*384))
poses.save(SOURCE/'poses-lanca.png')
readability=Image.new('RGB',(720,400),(28,31,28));draw=ImageDraw.Draw(readability)
draw.rectangle((360,0,720,400),fill=(113,111,91))
for offset in [0,360]:
    draw.text((offset+16,16),'Guarda / recuo / impacto / retorno',fill=(235,226,205))
    for n,f in enumerate([0,7,8,19]):
        pose=Image.open(SOURCE/f'pose-{f+1:02}.png').convert('RGBA').resize((176,176),Image.Resampling.LANCZOS)
        readability.paste(pose,(offset+(n%2)*180,36+(n//2)*160),pose)
readability.save(SOURCE/'leitura-tamanho-jogo.png')
grips=json.loads((SOURCE/'SpearGrip.json').read_text())['frames']
manifest={'frames':20,'impactFrame':9,'bodyAtlas':'SpearAttack_1.png','weaponAtlas':'SpearWeapon_1.png',
    'cell':256,'bodyWorldSize':3.4,'weaponWorldSize':5.1,'weaponPixelsPerUnit':256/5.1,
    'windup':.30,'recovery':.50,'worldGuid':worldGuid,'tipAtImpact':[grips[8]['tipX'],grips[8]['tipY']],
    'applyWeaponGripAgain':False}
(SOURCE/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8')
print(json.dumps(manifest))
