"""Package actual Blender outputs for Unity; preserve existing GUIDs and resources."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import uuid,sys
BASE=Path(__file__).resolve().parent; ROOT=BASE.parents[1]
RES=ROOT/'Assets/Resources'; RUNTIME=BASE/'PendingRuntimeExport'
TEMPLATE=(RES/'PlayerCharacter/Shotgun.png.meta').read_text(encoding='utf-8')
def texture_meta(path):
    meta=Path(str(path)+'.meta')
    if not meta.exists():
        lines=TEMPLATE.splitlines(); lines=[('guid: '+uuid.uuid4().hex) if x.startswith('guid: ') else x for x in lines]
        meta.write_text('\n'.join(lines)+'\n',encoding='utf-8')
def folder(path):
    path.mkdir(parents=True,exist_ok=True)
    meta=Path(str(path)+'.meta')
    if not meta.exists(): meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',encoding='utf-8')
def atlas(frames,path):
    result=Image.new('RGBA',(2048,((len(frames)+7)//8)*256))
    for i,f in enumerate(frames):
        result.paste(f.resize((256,256),Image.Resampling.LANCZOS),((i%8)*256,(i//8)*256))
    result.save(path); texture_meta(path)
folder(RES/'PlayerCharacter/Actions')
for source in sorted(RUNTIME.iterdir()):
    if not source.is_dir(): continue
    if '--rifle-partials-only' in sys.argv and not source.name.startswith('ReloadRifleRounds'): continue
    frames=[Image.open(p).convert('RGBA') for p in sorted(source.glob('*.png'))]
    if not frames: continue
    if source.name=='Mosin': atlas(frames,RES/'PlayerCharacter/Mosin.png'); continue
    for page in range((len(frames)+63)//64):
        atlas(frames[page*64:(page+1)*64],RES/'PlayerCharacter/Actions'/f'{source.name}_{page+1}.png')
if '--rifle-partials-only' in sys.argv:
    print('Corrected rifle partial atlases installed; GUIDs preserved.')
    raise SystemExit(0)
for monster,prototype,actions in [
    ('Stalker','StalkerCrawler',[('Walk','Walk',32,1),('Bite','Attack',20,101)]),
    ('Acid','CorrosivePouch',[('Walk','Walk',32,1),('Spit','Spit',45,101)])]:
    destination=RES/'Monsters'/monster; folder(destination)
    for name,source,count,start in actions:
        frames=[]
        for i in range(count):
            f=Image.open(ROOT/'Prototypes'/prototype/source/f'{start+i:03}.png').convert('RGBA')
            frames.append(f.transpose(Image.Transpose.ROTATE_270))
        atlas(frames,destination/(name+'.png'))
for script in ['Assets/Scripts/Players/PlayerActionArt.cs','Assets/Scripts/Enemies/MonsterAnimationVisual.cs']:
    meta=ROOT/(script+'.meta')
    if not meta.exists(): meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n',encoding='utf-8')
# Previews from the original full-body transition renders, never from AI images.
font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',22)
labels={'ShotgunToPistol':'Shotgun → pistola','RifleToPistol':'Rifle → pistola','ShotgunToRifle':'Shotgun → rifle','RifleToShotgun':'Rifle → shotgun'}
for name,label in labels.items():
    source=BASE/'WeaponTransitions'/name
    frames=[Image.open(source/'frames'/f'{i:03}.png').convert('RGBA') for i in range(1,37)]
    views=[]
    for f in frames:
        view=Image.new('RGB',(640,680),(27,29,26)); f=f.resize((640,640),Image.Resampling.LANCZOS); view.paste(f,(0,40),f)
        ImageDraw.Draw(view).text((18,8),label+' · Blender',font=font,fill=(224,224,213)); views.append(view)
    views[0].save(source/(name+'.gif'),save_all=True,append_images=views[1:],duration=[500]+[42]*34+[700],loop=0,disposal=2)
from package_weapon_transition_gallery import package
package(BASE/'WeaponTransitions')
print('Player actions, partial reloads and monster animations installed. GUIDs preserved.')
