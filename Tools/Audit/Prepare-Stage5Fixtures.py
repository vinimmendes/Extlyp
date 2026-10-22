"""Extract explicit numeric authoring fixtures from the frozen Unity YAML; no engine code runs."""
from pathlib import Path
import hashlib
import json
import re
import sys

base, output = map(Path, sys.argv[1:3])
records = []
for folder, kind in [('Items/Equipment/Definitions', 'ItemDefinition'), ('Items/Treasure/Definitions', 'ItemDefinition'), ('Items/Loot/Tables', 'LootTableDefinition')]:
    for path in sorted((base / 'Assets/Resources' / folder).glob('*.asset')):
        text = path.read_text(encoding='utf-8-sig')
        fields = {'name': path.stem}
        section, current = None, None
        for line in text.splitlines():
            top = re.fullmatch(r'  (\w+):(?: (.*))?', line)
            if top:
                key, value = top.groups()
                section, current = None, None
                if key in ('Categories', 'Entries', 'Rarities'):
                    section = key
                    fields[key] = []
                elif key == 'SecondaryPool':
                    raw = bytes.fromhex(value) if value and value != '[]' else b''
                    if len(raw) % 4:
                        raise ValueError(path)
                    fields[key] = [int.from_bytes(raw[i:i+4], 'little') for i in range(0, len(raw), 4)]
                elif key in ('Id', 'Category', 'Slot', 'PrimaryAttribute', 'SellPrice', 'Map', 'Source', 'PresencePercent', 'MinimumStacks', 'MaximumStacks'):
                    fields[key] = int(value)
                elif key == 'GuaranteeEquipment':
                    fields[key] = bool(int(value))
                elif key == 'DisplayName':
                    fields[key] = json.loads(value) if value.startswith('"') else value
                continue
            if section:
                match = re.fullmatch(r'  - (\w+): (-?\d+)', line)
                if match:
                    current = {match[1]: int(match[2])}
                    fields[section].append(current)
                else:
                    match = re.fullmatch(r'    (\w+): (-?\d+)', line)
                    if not match or current is None:
                        raise ValueError(f'Unsupported fixture YAML: {path}: {line}')
                    current[match[1]] = int(match[2])
        records.append({'resource': folder + '/' + path.stem, 'type': kind, 'values': fields, 'source': path.relative_to(base).as_posix(), 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()})
path = base / 'Assets/Resources/Gameplay.asset'
values = {'name': 'Gameplay'}
for key, value in re.findall(r'^  (\w+): ([0-9.]+)\s*$', path.read_text(encoding='utf-8-sig'), re.M):
    if not key.startswith('m_'):
        values[key] = float(value) if '.' in value else int(value)
records.append({'resource': 'Gameplay', 'type': 'GameplayDefinition', 'values': values, 'source': path.relative_to(base).as_posix(), 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()})
output.write_text(json.dumps(records, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
print(f'{len(records)} authoring fixtures extracted; icons/native serialization intentionally excluded.')
