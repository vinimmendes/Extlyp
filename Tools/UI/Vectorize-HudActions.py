"""Replace the four action icons and menu gear with their vector subclass.

Preserves all prefab object IDs, components, references, sprites and placements.
Does not launch Unity or regenerate the interface.
"""
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PATH = ROOT / 'Assets/Resources/UI/Prefabs/Hud.prefab'
GUID = 'b3246229238840818f5afcb1ad9ef887'
raw = PATH.read_bytes()
source = raw.decode('utf-8').replace('\r\n','\n')
changes = []
for control, symbol in [(5,7),(7,0),(8,1),(9,2),(10,3)]:
    binding = re.search(rf'^  - Control: {control}\n(?P<fields>(?:    .*\n)+)',source,re.M)
    if not binding: raise ValueError(f'Missing HUD control {control}')
    icon_id = re.search(r'^    Icon: \{fileID: (\d+)\}',binding['fields'],re.M).group(1)
    block = re.search(rf'^--- !u!114 &{icon_id}\n.*?(?=^---|\Z)',source,re.M|re.S)
    if not block: raise ValueError(f'Missing bound icon {icon_id}')
    text = block.group()
    script = re.search(r'^  m_Script: \{fileID: 11500000, guid: ([0-9a-f]+), type: 3\}',text,re.M)
    if script.group(1) not in ('fe87c0e1cc204ed48ad3b37840f39efc',GUID):
        raise ValueError(f'Unexpected icon component on control {control}; preserve it for review')
    text = text[:script.start(1)] + GUID + text[script.end(1):]
    text = re.sub(r'^  Symbol: .*\n','',text,flags=re.M).rstrip() + f'\n  Symbol: {symbol}\n'
    changes.append((block.start(),block.end(),text))
for start,end,text in sorted(changes,reverse=True): source=source[:start]+text+source[end:]
if b'\r\n' in raw: source=source.replace('\n','\r\n')
updated = source.encode('utf-8')
if updated != raw: PATH.write_bytes(updated)
print('HUD action icons and menu gear bound to vector geometry; object IDs preserved.')
