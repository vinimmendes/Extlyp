"""Use the antialiased native graphic for existing HUD bars, preserving IDs."""
from pathlib import Path
import re
ROOT=Path(__file__).resolve().parents[2]
path=ROOT/'Assets/Resources/UI/Prefabs/Hud.prefab'
source=path.read_text(encoding='utf-8')
guid=re.search(r'guid: (\w+)',(ROOT/'Assets/Scripts/UI/HudFrameGraphic.cs.meta').read_text()).group(1)
ids=set(re.findall(r'^    Bar(?:Track)?: \{fileID: (\d+)\}',source,re.M))
for id in ids:
    if id=='0':continue
    match=re.search(r'^--- !u!114 &'+id+r'\n.*?(?=^--- !u!|\Z)',source,re.M|re.S)
    block=match.group(0)
    if '  BarStyle: 1' in block:continue
    updated=re.sub(r'm_Script: \{fileID: 11500000, guid: \w+, type: 3\}', 'm_Script: {fileID: 11500000, guid: '+guid+', type: 3}',block)
    source=source.replace(block,updated+'  BarStyle: 1\n')
path.write_text(source,encoding='utf-8')
print('Existing bar IDs and references preserved; native feathered edges configured.')
