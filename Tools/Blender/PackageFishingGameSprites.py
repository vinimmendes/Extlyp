"""Package Blender passes, register the worm icon and preserve all existing IDs."""
from pathlib import Path
from PIL import Image
import shutil, uuid, re
ROOT=Path(__file__).resolve().parents[2]
src=ROOT/'Art/Fishing/GameSprites-v01'; dest=ROOT/'Assets/Resources/Fishing'
dest.mkdir(parents=True,exist_ok=True)
for name,prefix,w,h in [('FishSwim','fish',256,128),('Bobber','bobber',96,128)]:
    atlas=Image.new('RGBA',(w*4,h*3))
    for i in range(12):
        with Image.open(src/f'{prefix}_{i:02}.png') as im:
            if im.size!=(w,h) or im.getbbox() is None: raise ValueError(f'Invalid isolated render {prefix}/{i}')
            atlas.paste(im,(i%4*w,i//4*h))
    atlas.save(dest/f'{name}.png')
for name,target in [('water-pier.png','WaterPier.png'),('spool.png','Spool.png'),('worm.png','Worm.png'),('ripple.png','Ripple.png')]:
    shutil.copyfile(src/name,dest/target)
template=(ROOT/'Assets/Resources/Items/IconsV2/CrossbowBolt.png.meta').read_text(encoding='utf-8')
for file in dest.glob('*.png'):
    meta=Path(str(file)+'.meta')
    if not meta.exists():
        text=re.sub(r'^guid: \w+', 'guid: '+uuid.uuid4().hex,template,flags=re.M)
        text=text.replace('maxTextureSize: 512','maxTextureSize: 2048')
        meta.write_text(text,encoding='utf-8')
for file in [dest,*list((ROOT/'Assets/Scripts').rglob('Fishing*.cs'))]:
    meta=Path(str(file)+'.meta')
    if not meta.exists(): meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n'+('folderAsset: yes\n' if file.is_dir() else ''),encoding='utf-8')
cat=ROOT/'Assets/Resources/ItemVisualCatalog.asset'
text=cat.read_text(encoding='utf-8')
guid=re.search(r'^guid: (\w+)',(dest/'Worm.png.meta').read_text(),re.M).group(1)
if '\n  - Id: 62\n' not in text:
    text+=f'  - Id: 62\n    InterfaceIcon: {{fileID: 21300000, guid: {guid}, type: 3}}\n    WorldIcon: {{fileID: 21300000, guid: {guid}, type: 3}}\n'
    cat.write_text(text,encoding='utf-8')
catalog=ROOT/'Assets/Scripts/Items/ItemCatalog.cs'
text=catalog.read_text(encoding='utf-8')
text=text.replace('RustySword, CrossbowBolt }','RustySword, CrossbowBolt, Worm }')
text=text.replace('Count => (int)ItemId.CrossbowBolt+1','Count => (int)ItemId.Worm+1')
text=text.replace('Icon(ItemId item) => item==ItemId.ImprovisedHandCrossbow','Icon(ItemId item) => item==ItemId.Worm ? "MI" : item==ItemId.ImprovisedHandCrossbow')
text=text.replace('Name(ItemId item) => item==ItemId.ImprovisedHandCrossbow','Name(ItemId item) => item==ItemId.Worm ? "Minhoca" : item==ItemId.ImprovisedHandCrossbow')
catalog.write_text(text,encoding='utf-8')
for name in ['Forest_CommonContainer','Dungeon_CommonContainer']:
    table=ROOT/f'Assets/Resources/Items/Loot/Tables/{name}.asset'; text=table.read_text(encoding='utf-8')
    if '\n  - Item: 62\n' not in text:
        text=text.replace('  Entries:\n','  Entries:\n  - Item: 62\n    Category: 1\n    Weight: 10\n    Minimum: 1\n    Maximum: 3\n    Themes: 15\n',1)
        table.write_text(text,encoding='utf-8')
print('Fishing atlases, worm catalog and common-container loot packaged.')
definition=dest/'Definition.asset'
if not definition.exists():
    guid=re.search(r'^guid: (\w+)',(ROOT/'Assets/Scripts/Items/FishingDefinition.cs.meta').read_text(),re.M).group(1)
    definition.write_text(f'''%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {guid}, type: 3}}
  m_Name: Definition
  m_EditorClassIdentifier:
  CastSeconds: 0.6
  WaitMin: 1
  WaitMax: 2
  ApproachSeconds: 1
  InvestigateMin: 0.35
  InvestigateMax: 0.75
  LeaveSeconds: 0.65
  HookSeconds: 1.6
  CaptureSeconds: 20
  AttemptSeconds: 90
  Loss: 0.12
  BitePercent: 70
  RarePercent: 10
  BaitRarePercent: 25
  LargePercent: 15
  Common:
    Zone: 0.28
    Frequency: 1.25
    Speed: 0.45
    Gain: 0.19
  Large:
    Zone: 0.23
    Frequency: 1.4
    Speed: 0.62
    Gain: 0.11
  Rare:
    Zone: 0.20
    Frequency: 2.2
    Speed: 0.60
    Gain: 0.105
''',encoding='utf-8')
    Path(str(definition)+'.meta').write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n',encoding='utf-8')
