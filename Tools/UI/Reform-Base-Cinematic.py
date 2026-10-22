"""Incrementally extend BaseHub; retain all existing object IDs and callbacks."""
from pathlib import Path
import re,json
ROOT=Path(__file__).resolve().parents[2]
path=ROOT/'Assets/Resources/UI/Prefabs/BaseHub.prefab'
source=path.read_text(encoding='utf-8-sig')
blocks={int(m[2]):(int(m[1]),m[3]) for m in re.finditer(r'^--- !u!(\d+) &(\d+)\n([\s\S]*?)(?=^--- !u!|\Z)',source,re.M)}
view=next(i for i,(_,b) in blocks.items() if '  PreparationFade:' in b)
if '  BackpackCount:' in blocks[view][1]:
    print('BaseHub already extended; preserving authored edits.')
    raise SystemExit(0)
counter=max(blocks)+100
def fieldid(name):return int(re.search(r'^  '+name+r': \{fileID: (\d+)\}',blocks[view][1],re.M)[1])
def go_of(component):return int(re.search(r'm_GameObject: \{fileID: (\d+)\}',blocks[component][1])[1])
def rect_of(component):
    go=go_of(component)
    return next(i for i,(kind,b) in blocks.items() if kind==224 and 'm_GameObject: {fileID: '+str(go)+'}' in b)
def children(rect):return [int(i) for i in re.findall(r'fileID: (\d+)',blocks[rect][1].split('  m_Children:')[1].split('  m_Father:')[0])]
def setchildren(rect,ids):
    k,b=blocks[rect]; b=re.sub(r'  m_Children:.*?  m_Father:',('  m_Children:\n'+''.join('  - {fileID: '+str(i)+'}\n' for i in ids) if ids else '  m_Children: []\n')+'  m_Father:',b,flags=re.S); blocks[rect]=(k,b)
def reparent(component,parent,first=False):
    rect=rect_of(component); k,b=blocks[rect]
    old=int(re.search(r'm_Father: \{fileID: (\d+)\}',b)[1])
    setchildren(old,[i for i in children(old) if i!=rect])
    blocks[rect]=(k,re.sub(r'm_Father: \{fileID: \d+\}','m_Father: {fileID: '+str(parent)+'}',b))
    ids=children(parent); setchildren(parent,([rect]+ids) if first else ids+[rect])
def clone(component,name,parent):
    global counter
    go=go_of(component); ids=[go]+[int(i) for i in re.findall(r'component: \{fileID: (\d+)\}',blocks[go][1])]
    mapping={old:counter+n for n,old in enumerate(ids)}; counter+=len(ids)
    for old in ids:
        k,b=blocks[old]; b=re.sub(r'fileID: (\d+)(?=\})',lambda m:'fileID: '+str(mapping.get(int(m[1]),int(m[1]))),b)
        if old==go:b=re.sub(r'  m_Name: .*','  m_Name: '+json.dumps(name),b)
        if k==224:
            b=re.sub(r'  m_Father: \{fileID: \d+\}','  m_Father: {fileID: '+str(parent)+'}',b)
            b=re.sub(r'  m_Children:.*?  m_Father:','  m_Children: []\n  m_Father:',b,flags=re.S)
        blocks[mapping[old]]=(k,b)
    setchildren(parent,children(parent)+[mapping[rect_of(component)]])
    return mapping[component]
def field(name,value):
    k,b=blocks[view]
    b+='  '+name+':'+ ('\n'+''.join('  - {fileID: '+str(i)+'}\n' for i in value) if isinstance(value,list) else ' {fileID: '+str(value)+'}\n')
    blocks[view]=(k,b)
def active(component,state):
    go=go_of(component); k,b=blocks[go]; blocks[go]=(k,re.sub(r'm_IsActive: \d','m_IsActive: '+str(int(state)),b))
def box(component,x,y,w,h):
    rect=rect_of(component); k,b=blocks[rect]
    for name,value in [('m_AnchorMin','{x: 0, y: 1}'),('m_AnchorMax','{x: 0, y: 1}'),('m_Pivot','{x: 0, y: 1}'),('m_AnchoredPosition',f'{{x: {x}, y: {-y}}}'),('m_SizeDelta',f'{{x: {w}, y: {h}}}')]:
        b=re.sub(r'  '+name+r': .*','  '+name+': '+value,b)
    blocks[rect]=(k,b)
def label(name,parent,caption,size,bounds):
    comp=clone(fieldid('Heading'),name,parent); k,b=blocks[comp]
    b=re.sub(r'  m_text: .*','  m_text: '+json.dumps(caption,ensure_ascii=False),b)
    b=re.sub(r'  m_fontSize(Base)?: .*',lambda m:'  m_fontSize'+('Base' if m[1] else '')+': '+str(size),b)
    b=b.replace('m_HorizontalAlignment: 2','m_HorizontalAlignment: 1').replace('m_VerticalAlignment: 512','m_VerticalAlignment: 256')
    blocks[comp]=(k,b); active(comp,True); box(comp,*bounds); return comp
icon_template=int(re.search(r'  EquipmentIcons:\n  - \{fileID: (\d+)\}',blocks[view][1])[1])
def image(name,parent,bounds,alpha):
    comp=clone(icon_template,name,parent); k,b=blocks[comp]
    b=re.sub(r'  m_Color: .*',f'  m_Color: {{r: 0, g: 0, b: 0, a: {alpha}}}',b)
    b=re.sub(r'  m_Sprite: .*','  m_Sprite: {fileID: 0}',b)
    b=b.replace('m_RaycastTarget: 1','m_RaycastTarget: 0')
    blocks[comp]=(k,b); active(comp,True); box(comp,*bounds); return comp
root=fieldid('Panel'); left=fieldid('Equipment'); right=fieldid('Destination'); scenic=fieldid('Scenery')
art=fieldid('Artwork'); reparent(art,root,first=True); box(art,0,0,1066,600)
shade=image('BackdropShade',root,(0,0,1066,600),.22); reparent(shade,root,first=True); reparent(art,root,first=True); field('BackdropShade',shade)
for name,bounds,alpha in [('HeaderShade',(0,0,1066,108),.92),('FooterShade',(0,550,1066,50),.85)]:
    comp=image(name,root,bounds,alpha)
    # Place shades behind existing controls, after the background pair.
    ids=children(root); ids.remove(rect_of(comp)); ids.insert(2,rect_of(comp)); setchildren(root,ids); field(name,comp)
active(fieldid('Resources'),False); active(fieldid('EquipmentText'),False); active(fieldid('DestinationText'),False)
for name,parent,caption,size,bounds in [
    ('Brand',root,'CYBER EXTRACTION',14,(16,8,170,20)),
    ('PreparationsTitle',left,'PREPARATIVOS',24,(20,18,200,32)),
    ('BackpackLabel',left,'MOCHILA',18,(62,76,160,24)),
    ('BackpackCount',left,'7 / 12 espaços',25,(62,104,166,34)),
    ('StatusText',left,'',18,(20,248,208,40)),
    ('DestinationLabel',scenic,'DESTINO SELECIONADO',16,(16,44,320,26)),
    ('ExpeditionTitle',right,'EXPEDIÇÃO',24,(20,18,260,32)),
    ('ConnectionText',right,'Conectado',20,(20,138,260,32)),
    ('RiskText',right,'Se morrer, perde os itens da mochila.',20,(20,188,260,92))]:
    field(name,label(name,parent,caption,size,bounds))
# The previous left heading remains serialized, but is no longer presented.
for i,(kind,b) in list(blocks.items()):
    if kind!=224 or 'm_Father: {fileID: '+str(left)+'}' not in b:continue
    go=go_of(i)
    if re.search(r'm_Name: "?Title"?\n',blocks[go][1]):active(i,False)
title_shade=image('TitleShade',scenic,(0,36,440,160),.76); reparent(title_shade,scenic,first=True); field('TitleShade',title_shade)
reparent(fieldid('MapTitle'),scenic)
field('BackpackIcon',image('BackpackIcon',left,(20,82,32,40),1))
track=image('CapacityTrack',left,(20,152,208,7),1); field('CapacityTrack',track)
field('CapacityFill',image('CapacityFill',rect_of(track),(0,0,120,7),1))
for fieldname,count,parent in [('ResourceLabels',3,root),('ResourceValues',3,root),('SupplyLabels',2,left),('SupplyValues',2,left)]:
    values=[]
    for i in range(count):
        caption=(['Moedas','Madeira','Pregos'][i] if fieldname=='ResourceLabels' else ['Bandagens','Munição reserva'][i] if fieldname=='SupplyLabels' else '0')
        values.append(label(fieldname+str(i),parent,caption,18,(200+i*150,16,130,24)))
    field(fieldname,values)
for fieldname,count,parent in [('ResourceIcons',3,root),('SupplyIcons',2,left)]:
    field(fieldname,[image(fieldname+str(i),parent,(20,186+i*40,24,24),1) for i in range(count)])
# Persist the native flat style too, so the prefab remains editable as components.
frame_guid=re.search(r'guid: (\w+)',(ROOT/'Assets/Scripts/UI/HudFrameGraphic.cs.meta').read_text(encoding='utf-8-sig'))[1]
k,b=blocks[title_shade]
b=re.sub(r'm_Script: .*','m_Script: {fileID: 11500000, guid: '+frame_guid+', type: 3}',b)
blocks[title_shade]=(k,b+'  BaseVeil: 1\n')
tabs=[int(i) for i in re.findall(r'fileID: (\d+)',blocks[view][1].split('  Tabs:')[1].split('  ActivityButtons:')[0])]
for i,(kind,b) in list(blocks.items()):
    if kind!=114 or 'guid: '+frame_guid not in b: continue
    b+='  BaseStyle: 1\n'
    if i in tabs:b+='  BaseTab: 1\n  BaseSelected: '+str(int(i==tabs[0]))+'\n'
    if go_of(i)==go_of(root):b=re.sub(r'  FillOpacity: .*','  FillOpacity: 0',b)
    blocks[i]=(kind,b)
path.write_text('%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'+''.join(f'--- !u!{kind} &{i}\n{body}' for i,(kind,body) in blocks.items()),encoding='utf-8')
print('Extended BaseHub in place: cinematic background and independent data controls.')
