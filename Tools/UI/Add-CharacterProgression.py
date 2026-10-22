"""Author a character sheet in the existing inventory, preserving original objects."""
from pathlib import Path
import re

root=Path(__file__).resolve().parents[2]
path=root/'Assets/Resources/UI/Prefabs/Inventory.prefab'
source=path.read_text(encoding='utf-8')
if '  CharacterHeading:' in source:
    raise SystemExit('Character sheet already authored; no changes.')
objects={int(m[2]):(int(m[1]),m[3]) for m in re.finditer(r'^--- !u!(\d+) &(\d+)\n([\s\S]*?)(?=^--- !u!|\Z)',source,re.M)}
next_id=max(objects)+100
added,children=[],[]

def clone(component,name,label):
    global next_id
    go=int(re.search(r'm_GameObject: \{fileID: (\d+)\}',objects[component][1])[1])
    ids=set()
    def visit(game_object):
        ids.add(game_object)
        components=[int(i) for i in re.findall(r'component: \{fileID: (\d+)\}',objects[game_object][1])]
        ids.update(components)
        for comp in components:
            if objects[comp][0]!=224: continue
            body=objects[comp][1].split('  m_Children:',1)[1].split('  m_Father:',1)[0]
            for child in re.findall(r'fileID: (\d+)',body):
                visit(int(re.search(r'm_GameObject: \{fileID: (\d+)\}',objects[int(child)][1])[1]))
    visit(go)
    mapping={old:next_id+i for i,old in enumerate(sorted(ids))}
    next_id+=len(ids)
    rect=next(i for i in ids if objects[i][0]==224 and re.search(r'm_GameObject: \{fileID: '+str(go)+r'\}',objects[i][1]))
    for old in sorted(ids):
        kind,body=objects[old]
        body=re.sub(r'fileID: (\d+)(?=\})',lambda m:'fileID: '+str(mapping.get(int(m[1]),int(m[1]))),body)
        if old==go: body=re.sub(r'  m_Name: .*','  m_Name: '+name,body)
        if '  m_text:' in body: body=re.sub(r'  m_text: .*','  m_text: '+label,body)
        if old==rect: body=re.sub(r'  m_Father: \{fileID: \d+\}','  m_Father: {fileID: 1002}',body)
        # Hidden until the presenter chooses this sheet. The toggle itself remains visible.
        if old==go and name!='CharacterToggle': body=body.replace('  m_IsActive: 1','  m_IsActive: 0')
        added.append(f'--- !u!{kind} &{mapping[old]}\n'+body)
    children.append(mapping[rect])
    return mapping[component]

heading=clone(1008,'CharacterHeading','PERSONAGEM')
body=clone(1016,'CharacterBody','Atributos e experiencia')
toggle=clone(1021,'CharacterToggle','Personagem')
buttons=[clone(1021,'InvestAttribute'+str(i),'+ '+name) for i,name in enumerate(['Força','Destreza','Vigor','Percepção','Vitalidade'])]
panel=objects[1002][1]
source=source.replace(panel,panel.replace('  m_Father:',''.join('  - {fileID: '+str(i)+'}\n' for i in children)+'  m_Father:',1),1)
fields='  CharacterHeading: {fileID: '+str(heading)+'}\n  CharacterBody: {fileID: '+str(body)+'}\n  CharacterToggle: {fileID: '+str(toggle)+'}\n  AttributeButtons:\n'+''.join('  - {fileID: '+str(i)+'}\n' for i in buttons)
source=source.replace('  DragVisual:',fields+'  DragVisual:',1)
path.write_text(source+''.join(added),encoding='utf-8')
print('Authored character heading, body, toggle and five investment buttons.')
