"""Read-only asset inspection; does not run Unity or gameplay tests."""
from pathlib import Path
import csv
import json
import re
import sys
from PIL import Image

root = Path(__file__).resolve().parents[2]
batch = sys.argv[1].lower()
catalog = (root / 'Assets/Resources/ItemVisualCatalog.asset').read_text(encoding='utf-8-sig')
entries = re.findall(r'  - Id: (\d+)\n    InterfaceIcon: (\{[^\n]+\})\n    WorldIcon: (\{[^\n]+\})', catalog)
item_source=(root/'Assets/Scripts/Items/ItemCatalog.cs').read_text(encoding='utf-8-sig')
item_count=len(re.search(r'enum ItemId : byte\s*\{([^}]+)',item_source)[1].split(','))
assert [int(row[0]) for row in entries] == list(range(item_count))
assert all(row[2] == '{fileID: 0}' for row in entries[:59])
assert all(row[2] == row[1] for row in entries[59:])
rows = list(csv.DictReader((root / 'Docs/UI/ItemIconsV2/catalogo.csv').open(encoding='utf-8-sig')))
assert len(rows) == item_count
if batch == 'all':
    # Complete inspection uses the checked-in originals, including the three pilots.
    # It remains usable on another workstation without the generation cache.
    manifest = {'Items': {
        row['Item']: {
            'Source': str(root / f"Art/ItemIconsV2/Originals/{row['Item']}.png"),
            'PromptFile': f"Art/ItemIconsV2/{row['Item']}-prompt.txt",
        } for row in rows
    }}
    assert all(row[1] != '{fileID: 0}' for row in entries)
    references = [re.search(r'guid: (\w+)', row[1])[1] for row in entries]
    assert len(set(references)) == item_count
else:
    manifest = json.loads((root / f'Art/ItemIconsV2/lote-{batch}.json').read_text(encoding='utf-8-sig'))
for item, source in manifest['Items'].items():
    runtime = root / f'Assets/Resources/Items/IconsV2/{item}.png'
    original = root / f'Art/ItemIconsV2/Originals/{item}.png'
    assert runtime.read_bytes() == original.read_bytes() == Path(source['Source']).read_bytes()
    assert (root / source['PromptFile']).stat().st_size > 100
    with Image.open(runtime) as img:
        assert img.mode == 'RGBA' and min(img.size) >= 1024
        alpha = img.getchannel('A')
        assert alpha.getextrema() == (0, 255)
        bbox = alpha.point(lambda a: 255 if a > 128 else 0).getbbox()
        assert bbox and bbox[0] > 0 and bbox[1] > 0 and bbox[2] < img.width and bbox[3] < img.height
        if batch != 'all':
            print(item, img.size, 'opaque bounds', bbox)
    meta = runtime.with_suffix('.png.meta').read_text(encoding='utf-8-sig')
    guid = re.search(r'^guid: (\w+)', meta, re.M)[1]
    row = next(row for row in rows if row['Item'] == item)
    assert guid in entries[int(row['Id'])][1]
    assert row['Estado'].startswith('Integrado')
    for setting in ('filterMode: 1', 'wrapU: 1', 'wrapV: 1', 'enableMipMap: 0', 'spriteMode: 1', 'alphaIsTransparency: 1'):
        assert setting in meta, (item, setting)
    assert meta.count('maxTextureSize: 512') == 4
    assert meta.count('textureCompression: 0') == 3
    assert meta.count('overridden: 1') == 3
integrated = sum(row[1] != '{fileID: 0}' for row in entries)
assert integrated == sum(row['Estado'].startswith('Integrado') for row in rows)
world_count=sum(row[2]!='{fileID: 0}' for row in entries)
print(f'{item_count} IDs; {integrated} UI overrides; {world_count} world overrides; batch {batch.upper()} inspected.')
