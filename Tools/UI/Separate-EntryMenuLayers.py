"""Surgical layer separation of the entry view, preserving all existing objects."""
from pathlib import Path
import re,uuid
ROOT=Path(__file__).resolve().parents[2]
path=ROOT/'Assets/Resources/UI/Prefabs/Expedition.prefab'
source=path.read_text(encoding='utf-8')
if '  EnvironmentPortraits:' in source:raise SystemExit('Entry layers already configured; preserving current prefab.')
blocks={int(m.group(2)):m.group(0) for m in re.finditer(r'^--- !u!(\d+) &(\d+)\n.*?(?=^--- !u!|\Z)',source,re.M|re.S)}
def asset(name):
    file=ROOT/'Assets/Resources/UI/Art'/f'{name}.png'
    guid=uuid.uuid5(uuid.NAMESPACE_URL,'cyber-extraction-ui/'+name).hex
    meta=Path(str(file)+'.meta')
    if not meta.exists():
        template=(ROOT/'Assets/Resources/UI/Art/EnvironmentSelectionApproved.png.meta').read_text()
        meta.write_text(re.sub(r'^guid: \w+','guid: '+guid,template,flags=re.M),encoding='utf-8')
    return re.search(r'^guid: (\w+)',meta.read_text(),re.M).group(1)
assets={n:asset(n) for n in ['EntryCabinBackground','EntryNormalCabin','EntryTestWorkshop']}
append=''
for i,name in enumerate(['EntryNormalCabin','EntryTestWorkshop']):
    start=9300+i*100
    mapping={1017:start,1018:start+1,1019:start+2,1020:start+3}
    for old,new in mapping.items():
        block=blocks[old]
        block=re.sub(r'^(--- !u!\d+ &)\d+',lambda m:m.group(1)+str(new),block,flags=re.M)
        block=re.sub(r'\{fileID: (1017|1018|1019|1020)\}',lambda m:'{fileID: '+str(mapping[int(m.group(1))])+'}',block)
        if old==1017:block=block.replace('m_Name: ForestPortrait','m_Name: '+name)
        if old==1018:block=block.replace('m_Father: {fileID: 1002}','m_Father: {fileID: '+str(9001+i*100)+'}')
        if old==1020:block=block.replace('m_Texture: {fileID: 0}','m_Texture: {fileID: 2800000, guid: '+assets[name]+', type: 3}')
        append+=block
    parent=blocks[9001+i*100]
    source=source.replace(parent,parent.replace('  m_Children:\n','  m_Children:\n  - {fileID: '+str(start+1)+'}\n',1))
mapping={1005:9500,1006:9501,1007:9502,1008:9503}
for old,new in mapping.items():
    block=blocks[old]
    block=re.sub(r'^(--- !u!\d+ &)\d+',lambda m:m.group(1)+str(new),block,flags=re.M)
    block=re.sub(r'\{fileID: (1005|1006|1007|1008)\}',lambda m:'{fileID: '+str(mapping[int(m.group(1))])+'}',block)
    if old==1005:block=block.replace('m_Name: Heading','m_Name: EntrySubtitle')
    if old==1008:block=re.sub(r'^  m_text:.*$','  m_text: "Escolha onde começar"',block,flags=re.M)
    append+=block
root=blocks[1002]
source=source.replace(root,root.replace('  m_Children:\n','  m_Children:\n  - {fileID: 9501}\n',1))
background=blocks[9203]
source=source.replace(background,re.sub(r'm_Texture: \{[^\n]+\}','m_Texture: {fileID: 2800000, guid: '+assets['EntryCabinBackground']+', type: 3}',background))
fields='''  EnvironmentPortraits:
  - {fileID: 9303}
  - {fileID: 9403}
  EnvironmentSubtitle: {fileID: 9503}
  EnvironmentTitles:
  - {fileID: 9013}
  - {fileID: 9113}
  EnvironmentDescriptions:
  - {fileID: 9023}
  - {fileID: 9123}
'''
source=source.replace('  EnvironmentArtwork:',fields+'  EnvironmentArtwork:',1)
for id in [9003,9103]:
    block=blocks[id]
    source=source.replace(block,block+'  EntryStyle: 1\n  Primary: '+str(int(id==9003))+'\n')
path.write_text(source+append,encoding='utf-8')
print('Separated background, two card photos and editable subtitle in existing prefab.')
