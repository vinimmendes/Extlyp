"""Pack Blender's transparent renders into game atlases, with aligned cell pivots."""
from pathlib import Path
from PIL import Image
import uuid

BASE=Path(__file__).resolve().parent
ROOT=BASE.parents[1]
DEST=ROOT/'Assets/Resources/PlayerCharacter';DEST.mkdir(parents=True,exist_ok=True)
CELL=256;COLS=8;ROWS=4
for variant in ['Unarmed','Pistol','Mosin','Shotgun']:
    source=BASE/('VisualV1/topo' if variant=='Pistol' else 'GameExport/'+variant)
    sheet=Image.new('RGBA',(COLS*CELL,ROWS*CELL))
    for index in range(32):
        frame=Image.open(source/f'{index+1:03}.png').convert('RGBA')
        if frame.size!=(CELL,CELL):frame=frame.resize((CELL,CELL),Image.Resampling.LANCZOS)
        sheet.paste(frame,((index%COLS)*CELL,(index//COLS)*CELL))
    path=DEST/f'{variant}.png';sheet.save(path)
    meta=path.with_suffix('.png.meta')
    if not meta.exists():
        meta.write_text(f'''fileFormatVersion: 2
guid: {uuid.uuid4().hex}
TextureImporter:
  serializedVersion: 13
  mipmaps:
    enableMipMap: 0
    sRGBTexture: 1
  isReadable: 0
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  textureType: 0
  textureShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    textureFormat: -1
    textureCompression: 0
    overridden: 0
''',encoding='utf-8')
    print(variant,path,sheet.size)
folder_meta=DEST.with_suffix('.meta')
if not folder_meta.exists():folder_meta.write_text(f'fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',encoding='utf-8')
