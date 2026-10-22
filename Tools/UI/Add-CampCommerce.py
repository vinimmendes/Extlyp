"""Append authored commerce controls to Inventory.prefab; preserve existing hierarchy."""
from pathlib import Path
import re
import json

root = Path(__file__).resolve().parents[2]
path = root / 'Assets/Resources/UI/Prefabs/Inventory.prefab'
source = path.read_text(encoding='utf-8')
if '  CommerceToggle:' in source:
    raise SystemExit('Commerce already authored; no changes.')
objects = {int(m[2]): (int(m[1]), m[3]) for m in re.finditer(r'^--- !u!(\d+) &(\d+)\n([\s\S]*?)(?=^--- !u!|\Z)', source, re.M)}
next_id = max(objects) + 100
added, children = [], []


def clone(component, name, label):
    global next_id
    go = int(re.search(r'm_GameObject: \{fileID: (\d+)\}', objects[component][1])[1])
    ids = set()

    def visit(game_object):
        ids.add(game_object)
        components = [int(i) for i in re.findall(r'component: \{fileID: (\d+)\}', objects[game_object][1])]
        ids.update(components)
        for comp in components:
            if objects[comp][0] != 224:
                continue
            body = objects[comp][1].split('  m_Children:', 1)[1].split('  m_Father:', 1)[0]
            for child in re.findall(r'fileID: (\d+)', body):
                visit(int(re.search(r'm_GameObject: \{fileID: (\d+)\}', objects[int(child)][1])[1]))
    visit(go)
    mapping = {old: next_id + i for i, old in enumerate(sorted(ids))}
    next_id += len(ids)
    rect = next(i for i in ids if objects[i][0] == 224 and re.search(r'm_GameObject: \{fileID: ' + str(go) + r'\}', objects[i][1]))
    for old in sorted(ids):
        kind, body = objects[old]
        body = re.sub(r'fileID: (\d+)(?=\})', lambda m: 'fileID: ' + str(mapping.get(int(m[1]), int(m[1]))), body)
        if old == go:
            body = re.sub(r'  m_Name: .*', '  m_Name: ' + name, body)
            body = body.replace('  m_IsActive: 1', '  m_IsActive: 0')
        if '  m_text:' in body:
            body = re.sub(r'  m_text: .*', lambda _: '  m_text: ' + json.dumps(label, ensure_ascii=False), body)
        if old == rect:
            body = re.sub(r'  m_Father: \{fileID: \d+\}', '  m_Father: {fileID: 1002}', body)
        added.append(f'--- !u!{kind} &{mapping[old]}\n' + body)
    children.append(mapping[rect])
    return mapping[component]


controls = {name: clone(1021, name, label) for name, label in (
    ('CommerceToggle', 'Comerciante'), ('CommerceMode', 'Comprar'), ('TradeConfirm', 'Confirmar'),
    ('TradeUnload', 'Descarregar'), ('TradeLess', '-'), ('TradeMore', '+'), ('TradeAll', 'Tudo'),
    ('RespecButton', 'Redistribuir'))}
offers = [clone(1021, 'ShopOffer' + str(i), 'Oferta') for i in range(6)]
heading = clone(1008, 'CommerceHeading', 'COMERCIANTE')
panel = objects[1002][1]
source = source.replace(panel, panel.replace('  m_Father:', ''.join('  - {fileID: ' + str(i) + '}\n' for i in children) + '  m_Father:', 1), 1)
fields = ''.join('  ' + name + ': {fileID: ' + str(i) + '}\n' for name, i in controls.items())
fields += '  ShopOffers:\n' + ''.join('  - {fileID: ' + str(i) + '}\n' for i in offers)
fields += '  CommerceHeading: {fileID: ' + str(heading) + '}\n'
source = source.replace('  DragVisual:', fields + '  DragVisual:', 1)
path.write_text(source + ''.join(added), encoding='utf-8')
print('Authored commerce controls, six offers and redistribution button in existing inventory.')
