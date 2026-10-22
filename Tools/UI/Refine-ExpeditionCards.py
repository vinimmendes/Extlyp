"""Surgical addition of the two original environment cards to the existing prefab."""
from pathlib import Path
import re
ROOT=Path(__file__).resolve().parents[2]
path=ROOT/'Assets/Resources/UI/Prefabs/Expedition.prefab'
source=path.read_text(encoding='utf-8')
if '  EnvironmentCards:' in source:raise SystemExit('Environment cards already authored; refusing to overwrite.')
blocks={int(m.group(2)):m.group(0) for m in re.finditer(r'^--- !u!(\d+) &(\d+)\n.*?(?=^--- !u!|\Z)',source,re.M|re.S)}
def clone(identifier,new,go):
    block=blocks[identifier]
    block=re.sub(r'^(--- !u!\d+ &)\d+',lambda m:m.group(1)+str(new),block,flags=re.M)
    return re.sub(r'm_GameObject: \{fileID: \d+\}','m_GameObject: {fileID: '+str(go)+'}',block)
def go_block(new,name,components):
    text=blocks[1001]
    text=re.sub(r'&1001\n','&'+str(new)+'\n',text,count=1)
    text=re.sub(r'  m_Component:\n.*?(?=  m_Layer:)', '  m_Component:\n'+''.join('  - component: {fileID: '+str(c)+'}\n' for c in components),text,flags=re.S)
    return re.sub(r'^  m_Name:.*$','  m_Name: '+name,text,flags=re.M)
def rect(new,go,parent,box,children=()):
    text=clone(1002,new,go)
    kids='  m_Children:\n'+''.join('  - {fileID: '+str(c)+'}\n' for c in children) if children else '  m_Children: []\n'
    text=re.sub(r'  m_Children:.*?(?=  m_Father:)',kids,text,flags=re.S)
    text=re.sub(r'm_Father: \{fileID: \d+\}','m_Father: {fileID: '+str(parent)+'}',text)
    x,y,w,h=box
    text=re.sub(r'm_AnchoredPosition:.*','m_AnchoredPosition: {x: '+str(x)+', y: '+str(-y)+'}',text)
    return re.sub(r'm_SizeDelta:.*','m_SizeDelta: {x: '+str(w)+', y: '+str(h)+'}',text)
append=''
for i in range(2):
    n=9000+i*100
    append+=go_block(n,'EnvironmentCard'+str(i),[n+1,n+2,n+3])+rect(n+1,n,1002,(24+i*456,130,432,250),[n+11,n+21])+clone(1003,n+2,n)+clone(1004,n+3,n)
    for offset,name,text,box,size,bold in [(10,'Title','CABANA NORMAL' if i==0 else 'ACAMPAMENTO DE TESTE',(18,22,396,42),24,True),(20,'Description','Sua mochila, seu baú e a expedição Floresta Velha. Progresso real.' if i==0 else 'Recursos temporários, mapas de teste e laboratório. Sem alterar seu progresso real.',(18,78,396,82),18,False)]:
        k=n+offset
        caption=clone(1008,k+3,k)
        import json
        caption=re.sub(r'^  m_text:.*$',lambda m:'  m_text: '+json.dumps(text,ensure_ascii=False),caption,flags=re.M)
        caption=re.sub(r'^  m_fontSize(?:Base)?:.*$',lambda m:m.group(0).split(':')[0]+': '+str(size),caption,flags=re.M)
        caption=re.sub(r'^  m_fontWeight:.*$','  m_fontWeight: '+('600' if bold else '400'),caption,flags=re.M)
        caption=re.sub(r'^  m_HorizontalAlignment:.*$','  m_HorizontalAlignment: '+('2' if bold else '1'),caption,flags=re.M)
        caption=re.sub(r'^  m_VerticalAlignment:.*$','  m_VerticalAlignment: 256',caption,flags=re.M)
        append+=go_block(k,name,[k+1,k+2,k+3])+rect(k+1,k,n+1,box)+clone(1007,k+2,k)+caption
root=blocks[1002].replace('  m_Children:\n', '  m_Children:\n  - {fileID: 9001}\n  - {fileID: 9101}\n',1)
source=source.replace(blocks[1002],root)
source=source.replace('  Elements: []','  EnvironmentCards:\n  - {fileID: 9001}\n  - {fileID: 9101}\n  Elements: []',1)
path.write_text(source+append,encoding='utf-8')
print('Added two editable environment cards; existing objects and references preserved.')
