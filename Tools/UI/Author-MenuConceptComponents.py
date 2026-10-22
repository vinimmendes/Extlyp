"""Incrementally author layered menus and live item/recipe widgets."""
from pathlib import Path
import re,uuid
ROOT=Path(__file__).resolve().parents[2]
BASE=ROOT/'Assets/Resources/UI/Prefabs'
def parse(s):return {int(m.group(2)):m.group(0) for m in re.finditer(r'^--- !u!(\d+) &(\d+)\n.*?(?=^--- !u!|\Z)',s,re.M|re.S)}
templates=parse((BASE/'Expedition.prefab').read_text(encoding='utf-8'))
slottemplates=parse((BASE/'ItemSlot.prefab').read_text(encoding='utf-8'))
slotcomponent=next(v for v in slottemplates.values() if re.search(r'^  icon:',v,re.M))
iconid=int(re.search(r'  icon: \{fileID: (\d+)\}',slotcomponent).group(1))
def guid(name):
    file=ROOT/'Assets/Resources/UI/Art'/f'{name}.png'
    meta=Path(str(file)+'.meta')
    if not meta.exists():
        g=uuid.uuid5(uuid.NAMESPACE_URL,'cyber-extraction-ui/'+name).hex
        sample=(ROOT/'Assets/Resources/UI/Art/EntryCabinBackground.png.meta').read_text()
        meta.write_text(re.sub(r'^guid: \w+','guid: '+g,sample,flags=re.M),encoding='utf-8')
    return re.search(r'^guid: (\w+)',meta.read_text(),re.M).group(1)
forest=guid('ExpeditionForestBackdrop');cabin=guid('EntryCabinBackground')
frameguid=re.search(r'm_Script: \{fileID: 11500000, guid: (\w+)',templates[1004]).group(1)
def clone(block,id,go):
    block=re.sub(r'^(--- !u!\d+ &)\d+',lambda m:m.group(1)+str(id),block,flags=re.M)
    return re.sub(r'm_GameObject: \{fileID: \d+\}','m_GameObject: {fileID: '+str(go)+'}',block)
class Edit:
    def __init__(self,name):
        self.path=BASE/(name+'.prefab');self.source=self.path.read_text(encoding='utf-8');self.blocks=parse(self.source);self.next=10000
    def node(self,name,parent=1002,kind='frame',text='',texture=None):
        n=self.next;self.next+=10
        go=clone(templates[1001],n,n)
        go=re.sub(r'  m_Component:\n.*?(?=  m_Layer:)', '  m_Component:\n'+''.join('  - component: {fileID: '+str(i)+'}\n' for i in [n+1,n+2,n+3]),go,flags=re.S)
        go=re.sub(r'^  m_Name:.*$','  m_Name: '+name,go,flags=re.M)
        rect=clone(templates[1006],n+1,n)
        rect=re.sub(r'm_Father: \{fileID: \d+\}','m_Father: {fileID: '+str(parent)+'}',rect)
        renderer=clone(templates[1007],n+2,n)
        component=clone(templates[1008] if kind=='text' else templates[1020] if kind=='raw' else slottemplates[iconid] if kind=='image' else templates[1004],n+3,n)
        if kind=='text':
            import json
            component=re.sub(r'^  m_text:.*$',lambda m:'  m_text: '+json.dumps(text,ensure_ascii=False),component,flags=re.M)
        elif kind=='raw':component=re.sub(r'm_Texture: \{[^\n]+\}','m_Texture: {fileID: 2800000, guid: '+texture+', type: 3}',component)
        elif kind=='frame':component+='  MenuStyle: 1\n'
        elif kind=='image':component=re.sub(r'm_Sprite: \{[^\n]+\}','m_Sprite: {fileID: 0}',component)
        component=re.sub(r'm_RaycastTarget: \d+','m_RaycastTarget: 0',component)
        self.blocks.update({n:go,n+1:rect,n+2:renderer,n+3:component})
        self.child(parent,n+1)
        return n+1,n+3
    def child(self,parent,child):
        block=self.blocks[parent]
        if '  m_Children: []' in block:block=block.replace('  m_Children: []','  m_Children:\n  - {fileID: '+str(child)+'}')
        else:block=block.replace('  m_Father:', '  - {fileID: '+str(child)+'}\n  m_Father:',1)
        self.blocks[parent]=block
    def viewfields(self,fields):
        id=next(i for i,v in self.blocks.items() if '  Elements:' in v)
        self.blocks[id]+=fields
    def save(self):self.path.write_text('%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'+''.join(self.blocks.values()),encoding='utf-8')
def seq(name,ids):return '  '+name+':\n'+''.join('  - {fileID: '+str(i)+'}\n' for i in ids)
for name in ['Expedition','Inventory','Activities']:
    e=Edit(name)
    if '  Backdrop:' in e.source:print(name+': already layered; preserving');continue
    bg,bgi=e.node('MenuBackdrop',kind='raw',texture=forest if name=='Expedition' else cabin)
    surface,si=e.node('MenuSurface')
    root=e.blocks[1002]
    for id in [bg,surface]:root=root.replace('  - {fileID: '+str(id)+'}\n','')
    e.blocks[1002]=root.replace('  m_Children:\n','  m_Children:\n  - {fileID: '+str(bg)+'}\n  - {fileID: '+str(surface)+'}\n',1)
    # Root is a container. The surface is a separate editable graphic above the background.
    e.blocks[1004]=e.blocks[1004].replace('  m_Enabled: 1','  m_Enabled: 0').replace('  m_RaycastTarget: 1','  m_RaycastTarget: 0')
    fields='  Backdrop: {fileID: '+str(bgi)+'}\n  Surface: {fileID: '+str(si)+'}\n'
    if name=='Activities':
        strip,_=e.node('ResourceStrip')
        icons=[];values=[];cards=[];recipeicons=[];titles=[];details=[]
        for i in range(3):
            _,icon=e.node('ResourceIcon'+str(i),strip,kind='image');icons.append(icon)
            _,value=e.node('ResourceValue'+str(i),strip,kind='text');values.append(value)
            card,_=e.node('RecipeCard'+str(i));cards.append(card)
            _,icon=e.node('RecipeIcon'+str(i),card,kind='image');recipeicons.append(icon)
            _,title=e.node('RecipeTitle'+str(i),card,kind='text');titles.append(title)
            _,detail=e.node('RecipeCost'+str(i),card,kind='text');details.append(detail)
            buttonids=re.search(r'  Buttons:\n((?:  - \{fileID: \d+\}\n)+)',e.source).group(1)
            button=int(re.findall(r'fileID: (\d+)',buttonids)[i]);go=int(re.search(r'm_GameObject: \{fileID: (\d+)\}',e.blocks[button]).group(1))
            rect=next(k for k,v in e.blocks.items() if v.startswith('--- !u!224') and 'm_GameObject: {fileID: '+str(go)+'}' in v)
            e.blocks[1002]=e.blocks[1002].replace('  - {fileID: '+str(rect)+'}\n','')
            e.blocks[rect]=e.blocks[rect].replace('m_Father: {fileID: 1002}','m_Father: {fileID: '+str(card)+'}')
            e.child(card,rect)
        fields+='  ResourceStrip: {fileID: '+str(strip)+'}\n'+seq('ResourceIcons',icons)+seq('ResourceValues',values)+seq('RecipeCards',cards)+seq('RecipeIcons',recipeicons)+seq('RecipeTitles',titles)+seq('RecipeDetails',details)
    e.viewfields(fields);e.save();print(name+': background/surface and widgets added')
# Quantity labels are real text children; icon/description slots remain intact.
for name in ['Inventory','Activities','ItemSlot']:
    e=Edit(name)
    if re.search(r'^  quantity:',e.source,re.M):print(name+': badges already wired; preserving');continue
    for id,block in list(e.blocks.items()):
        if not re.search(r'^  icon:',block,re.M):continue
        go=int(re.search(r'm_GameObject: \{fileID: (\d+)\}',block).group(1))
        rect=next(k for k,v in e.blocks.items() if v.startswith('--- !u!224') and 'm_GameObject: {fileID: '+str(go)+'}' in v)
        # Choose an unused range even when this view already has the new widgets.
        e.next=max(e.next,max(e.blocks)+10)
        _,quantity=e.node('Quantity',rect,kind='text')
        e.blocks[id]+='  quantity: {fileID: '+str(quantity)+'}\n'
        for k,v in list(e.blocks.items()):
            if 'guid: '+frameguid+',' in v and 'm_GameObject: {fileID: '+str(go)+'}' in v:e.blocks[k]+='  SlotStyle: 1\n'
    e.save();print(name+': independent quantity labels added')
script=ROOT/'Assets/Scripts/UI/UguiMenuStyle.cs'
meta=Path(str(script)+'.meta')
if not meta.exists():meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid5(uuid.NAMESPACE_URL,'cyber-extraction-ui/UguiMenuStyle').hex+'\nMonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n',encoding='utf-8')
