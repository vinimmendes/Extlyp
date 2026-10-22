"""Retire firearm entries without renumbering IDs or touching source artwork."""
from pathlib import Path
import re
ROOT=Path(__file__).resolve().parents[1]
paths=[ROOT/'Assets/Resources/MerchantCatalog.asset',
       ROOT/'Assets/Resources/Environment/Dungeon/Breakables/BreakableLoot.asset',
       *sorted((ROOT/'Assets/Resources/Items/Loot/Tables').glob('*.asset'))]
for path in paths:
    text=path.read_text(encoding='utf-8-sig')
    def replace(match):
        entry=match[0];item=int(match[1])
        if item in (14,15,16,17):return ''
        if item in (6,7):return re.sub(r'Item: \d+', 'Item: '+str(59 if item==6 else 61),entry,count=1)
        return entry
    text=re.sub(r'^  - Item: (\d+)\n(?:(?:    [^\n]*\n)|(?:\n))*',replace,text,flags=re.M)
    if re.search(r'^  - Item: (6|7|14|15|16|17)$',text,re.M):raise ValueError('Retired asset entry: '+str(path))
    path.write_text(text,encoding='utf-8')
    print(path.relative_to(ROOT))
