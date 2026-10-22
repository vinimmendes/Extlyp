"""Add the approved art to the existing view without rebuilding its hierarchy."""
from pathlib import Path
import re,uuid
ROOT=Path(__file__).resolve().parents[2]
art=ROOT/'Assets/Resources/UI/Art/EnvironmentSelectionApproved.png'
guid=uuid.uuid5(uuid.NAMESPACE_URL,'cyber-extraction-ui/approved-environment-selection-v1').hex
meta=Path(str(art)+'.meta')
if not meta.exists():
    meta.write_text('fileFormatVersion: 2\nguid: '+guid+'''\nTextureImporter:
  serializedVersion: 13
  mipmaps:
    enableMipMap: 0
    sRGBTexture: 1
  isReadable: 0
  textureType: 0
  textureShape: 1
  alphaIsTransparency: 0
  nPOTScale: 0
  maxTextureSize: 2048
  textureSettings:
    filterMode: 1
    wrapU: 1
    wrapV: 1
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    textureCompression: 0
    overridden: 0
''',encoding='utf-8')
guid=re.search(r'^guid: (\w+)',meta.read_text(),re.M).group(1)
path=ROOT/'Assets/Resources/UI/Prefabs/Expedition.prefab'
source=path.read_text(encoding='utf-8')
if '  EnvironmentArtwork:' in source:raise SystemExit('Artwork already authored; preserving current prefab.')
blocks={int(m.group(2)):m.group(0) for m in re.finditer(r'^--- !u!(\d+) &(\d+)\n.*?(?=^--- !u!|\Z)',source,re.M|re.S)}
mapping={1017:9200,1018:9201,1019:9202,1020:9203}
new=''
for old,new_id in mapping.items():
    block=blocks[old]
    block=re.sub(r'^(--- !u!\d+ &)\d+',lambda m:m.group(1)+str(new_id),block,flags=re.M)
    block=re.sub(r'\{fileID: (1017|1018|1019|1020)\}',lambda m:'{fileID: '+str(mapping[int(m.group(1))])+'}',block)
    if old==1017:block=block.replace('m_Name: ForestPortrait','m_Name: ApprovedEnvironmentArtwork')
    if old==1020:
        block=block.replace('m_Texture: {fileID: 0}','m_Texture: {fileID: 2800000, guid: '+guid+', type: 3}')
        block=re.sub(r'm_RaycastTarget: \d+','m_RaycastTarget: 0',block)
    new+=block
root=blocks[1002].replace('  m_Children:\n','  m_Children:\n  - {fileID: 9201}\n',1)
source=source.replace(blocks[1002],root)
source=source.replace('  EnvironmentCards:','  EnvironmentArtwork: {fileID: 9203}\n  EnvironmentCards:',1)
path.write_text(source+new,encoding='utf-8')
print('Approved artwork linked; existing prefab objects preserved.')
