"""Author the two stage-3 HUD elements without regenerating existing UI assets."""
from pathlib import Path
import json
import re

root = Path(__file__).resolve().parents[2]
path = root / 'Assets/Resources/UI/Prefabs/Hud.prefab'
source = path.read_text(encoding='utf-8')
if re.search(r'^  - Control: 16$', source, re.M):
    raise SystemExit('Sprint HUD already authored; no changes.')
objects = {int(m[2]): (int(m[1]), m[3]) for m in re.finditer(
    r'^--- !u!(\d+) &(\d+)\n([\s\S]*?)(?=^--- !u!|\Z)', source, re.M)}
next_id = max(objects) + 100
added, children, entries = [], [], []

def clone(control, new_control, name):
    global next_id
    entry = re.search(r'^  - Control: '+str(control)+r'\n[\s\S]*?(?=^  - Control:|^  \w+:)', source, re.M)[0]
    rect = int(re.search(r'Root: \{fileID: (\d+)\}', entry)[1])
    ids = set()
    def visit(rect_id):
        go = int(re.search(r'm_GameObject: \{fileID: (\d+)\}', objects[rect_id][1])[1])
        ids.add(go)
        components = [int(i) for i in re.findall(r'component: \{fileID: (\d+)\}', objects[go][1])]
        ids.update(components)
        body = objects[rect_id][1].split('  m_Children:', 1)[1].split('  m_Father:', 1)[0]
        for child in re.findall(r'fileID: (\d+)', body):
            visit(int(child))
    visit(rect)
    mapping = {old: next_id+i for i, old in enumerate(sorted(ids))}
    next_id += len(ids)
    def remap(body):
        # External asset references use a GUID and must keep their local fileID.
        return re.sub(r'fileID: (\d+)(?=\})', lambda m: 'fileID: '+str(mapping.get(int(m[1]), int(m[1]))), body)
    for old in sorted(ids):
        kind, body = objects[old]
        body = remap(body)
        body = re.sub(r'  Control: '+str(control)+r'\b', '  Control: '+str(new_control), body)
        if old == rect:
            body = re.sub(r'  m_AnchoredPosition: .*', '  m_AnchoredPosition: {x: '+('210, y: -420}' if new_control==16 else '18, y: -105}'), body)
        if old == int(re.search(r'm_GameObject: \{fileID: (\d+)\}', objects[rect][1])[1]):
            body = re.sub(r'  m_Name: .*', '  m_Name: '+name, body)
        # Label is replaced by the presenter; keep the authored prefab legible too.
        if '  m_text:' in body:
            title = int(re.search(r'Title: \{fileID: (\d+)\}', entry)[1])
            label = ('CORRER' if new_control==16 else 'STAMINA 100 / 100') if old==title else ''
            body = re.sub(r'  m_text: .*', '  m_text: '+label, body)
        if new_control==16 and kind==224:
            body = body.replace('m_SizeDelta: {x: 50, y: 50}', 'm_SizeDelta: {x: 60, y: 60}')
        added.append(f'--- !u!{kind} &{mapping[old]}\n'+body)
    children.append(mapping[rect])
    entries.append(remap(entry).replace('Control: '+str(control), 'Control: '+str(new_control), 1))

clone(7, 16, 'Sprint')
clone(0, 17, 'Stamina')
panel = objects[1002][1]
source = source.replace(panel, panel.replace('  m_Father:', ''.join('  - {fileID: '+str(i)+'}\n' for i in children)+'  m_Father:', 1), 1)
source = source.replace('  Panel:', ''.join(entries)+'  Panel:', 1)
path.write_text(source+''.join(added), encoding='utf-8')

path = root / 'Assets/Resources/UI/HudLayoutDefaults.json'
data = json.loads(path.read_text(encoding='utf-8'))
for control, anchor, offset, size in [(16, (0,1), (210,-180), (60,60)), (17, (0,0), (18,105), (230,42))]:
    data['Controls'].append({'Control': control, 'Placement': {
        'Anchor': dict(zip(('x','y'), anchor)), 'Offset': dict(zip(('x','y'), offset)), 'Size': dict(zip(('x','y'), size))}})
path.write_text(json.dumps(data, indent=4)+'\n', encoding='utf-8')
print('Authored Sprint and Stamina; preserved all existing HUD object IDs and placements.')
