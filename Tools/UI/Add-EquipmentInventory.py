"""Add eight authored slots to the current inventory prefab, preserving existing IDs."""
from pathlib import Path
import re

root=Path(__file__).resolve().parents[2]
path=root/'Assets/Resources/UI/Prefabs/Inventory.prefab'
source=path.read_text(encoding='utf-8')
if '  EquipmentSlots:' in source:
    raise SystemExit('Equipment slots already authored; no changes.')
parts=list(re.finditer(r'^--- !u!(\d+) &(\d+)\n([\s\S]*?)(?=^--- !u!|\Z)',source,re.M))
objects={int(m[2]):(int(m[1]),m[3]) for m in parts}
next_id=max(objects)+100
added=[]
children=[]

def clone(component,name):
    global next_id
    go=int(re.search(r'm_GameObject: \{fileID: (\d+)\}',objects[component][1])[1])
    ids=set()
    def visit(game_object):
        ids.add(game_object)
        components=[int(x) for x in re.findall(r'component: \{fileID: (\d+)\}',objects[game_object][1])]
        ids.update(components)
        for comp in components:
            kind,body=objects[comp]
            if kind==224:
                chunk=body.split('  m_Children:',1)[1].split('  m_Father:',1)[0]
                for child in re.findall(r'fileID: (\d+)',chunk):
                    child_go=int(re.search(r'm_GameObject: \{fileID: (\d+)\}',objects[int(child)][1])[1])
                    visit(child_go)
    visit(go)
    mapping={old:next_id+i for i,old in enumerate(sorted(ids))}
    next_id+=len(ids)
    rect=next(i for i in ids if objects[i][0]==224 and re.search(r'm_GameObject: \{fileID: '+str(go)+r'\}',objects[i][1]))
    for old in sorted(ids):
        kind,body=objects[old]
        body=re.sub(r'fileID: (\d+)',lambda m:'fileID: '+str(mapping.get(int(m[1]),int(m[1]))),body)
        if old==go: body=re.sub(r'  m_Name: .*', '  m_Name: '+name,body)
        if old==rect: body=re.sub(r'  m_Father: \{fileID: \d+\}', '  m_Father: {fileID: 1002}',body)
        added.append(f'--- !u!{kind} &{mapping[old]}\n'+body)
    children.append(mapping[rect])
    return mapping[component]

slots=[clone(1042,'Equipment'+str(i)) for i in range(8)]
heading=clone(1008,'EquipmentHeading')
panel=objects[1002][1]
updated=panel.replace('  m_Father:', ''.join('  - {fileID: '+str(i)+'}\n' for i in children)+'  m_Father:',1)
source=source.replace(panel,updated,1)
fields='  EquipmentSlots:\n'+''.join('  - {fileID: '+str(i)+'}\n' for i in slots)
fields+='  EquipmentHeading: {fileID: '+str(heading)+'}\n'
source=source.replace('  DragVisual:',fields+'  DragVisual:',1)
path.write_text(source+''.join(added),encoding='utf-8')
print('Added 8 equipment slots and character heading to Inventory.prefab.')
