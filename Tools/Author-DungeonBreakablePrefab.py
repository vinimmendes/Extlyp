"""Incremental prefab authoring. Does not run Unity or MvpSetup.Build."""
from pathlib import Path
import re
import uuid
import zlib

root=Path(__file__).resolve().parents[1]
script=root/'Assets/Scripts/Combat/DungeonBreakable.cs'
for path in (script,root/'Assets/Scripts/Match/DungeonBreakables.cs'):
    meta=Path(str(path)+'.meta')
    if not meta.exists(): meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n',encoding='utf-8')
script_guid=re.search(r'guid: (\w+)',Path(str(script)+'.meta').read_text(encoding='utf-8-sig'))[1]
target=root/'Assets/Resources/Environment/Dungeon/Breakables/DungeonBreakable.prefab'
meta=Path(str(target)+'.meta')
if not meta.exists(): meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nPrefabImporter:\n  externalObjects: {}\n',encoding='utf-8')
prefab_guid=re.search(r'guid: (\w+)',meta.read_text(encoding='utf-8-sig'))[1]
source=(root/'Assets/Prefabs/Loot.prefab').read_text(encoding='utf-8-sig')
parts=re.split(r'(?=--- !u!)',source)
header=parts[0]
game=next(p for p in parts if p.startswith('--- !u!1 '))
transform=next(p for p in parts if p.startswith('--- !u!4 '))
network=next(p for p in parts if 'guid: d5a57f767e5e46a458fc5d3c628d0cbb' in p)
game=re.sub(r'  m_Component:.*?  m_Layer:', '  m_Component:\n  - component: {fileID: 3576310968212470307}\n  - component: {fileID: 8538988178453756931}\n  - component: {fileID: 4000100}\n  m_Layer:',game,flags=re.S)
game=game.replace('m_Name: Loot','m_Name: DungeonBreakable')
transform=transform.replace('m_LocalScale: {x: 0.45, y: 0.45, z: 1}','m_LocalScale: {x: 1, y: 1, z: 1}')
network=re.sub(r'GlobalObjectIdHash: \d+',f'GlobalObjectIdHash: {zlib.crc32(prefab_guid.encode())}',network,count=1)
behaviour=f'''--- !u!114 &4000100
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 2656584173970377489}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {script_guid}, type: 3}}
  m_Name:
  m_EditorClassIdentifier:
'''
target.write_text(header+game+transform+network+behaviour,encoding='utf-8')
registry=root/'Assets/DefaultNetworkPrefabs.asset'
text=registry.read_text(encoding='utf-8-sig')
if prefab_guid not in text:
    text+=f'''  - Override: 0
    Prefab: {{fileID: 2656584173970377489, guid: {prefab_guid}, type: 3}}
    SourcePrefabToOverride: {{fileID: 0}}
    SourceHashToOverride: 0
    OverridingTargetPrefab: {{fileID: 0}}
'''
    registry.write_text(text,encoding='utf-8')
print('DungeonBreakable prefab and registration authored; no scene rebuild.')
