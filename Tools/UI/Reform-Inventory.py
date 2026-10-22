"""Incremental authoring for inventory v5. Preserves all existing object IDs."""
from pathlib import Path
import re, uuid

ROOT=Path(__file__).resolve().parents[2]

def reform(name):
    path=ROOT/'Assets/Resources/UI/Prefabs'/(name+'.prefab')
    source=path.read_text(encoding='utf-8-sig')
    blocks={int(m[2]):(int(m[1]),m[3]) for m in re.finditer(r'^--- !u!(\d+) &(\d+)\n([\s\S]*?)(?=^--- !u!|\Z)',source,re.M)}
    view_id=next(i for i,(_,b) in blocks.items() if '  Elements:' in b or '  PreparationFade:' in b)
    if name=='Inventory' and '  BackpackSlots:' in blocks[view_id][1]:
        print(name+': already reformed; preserving edits');return
    if name=='Activities' and len(sequence('Slots',blocks[view_id][1]))==12:
        print(name+': already expanded');return
    if name=='BaseHub' and '  CharacterShortcut:' in blocks[view_id][1]: return
    counter=max(blocks)+100

    def rect_of(component):
        go=int(re.search(r'm_GameObject: \{fileID: (\d+)\}',blocks[component][1])[1])
        return next(i for i,(kind,b) in blocks.items() if kind==224 and 'm_GameObject: {fileID: '+str(go)+'}' in b)

    def attach(parent,child):
        kind,b=blocks[parent]
        if '  m_Children: []' in b:b=b.replace('  m_Children: []','  m_Children:\n  - {fileID: '+str(child)+'}')
        else:b=b.replace('  m_Father:', '  - {fileID: '+str(child)+'}\n  m_Father:',1)
        blocks[parent]=(kind,b)

    def clone(component,label,parent):
        nonlocal counter
        go=int(re.search(r'm_GameObject: \{fileID: (\d+)\}',blocks[component][1])[1]);ids=set()
        def visit(game_object):
            ids.add(game_object)
            for item in re.findall(r'component: \{fileID: (\d+)\}',blocks[game_object][1]):
                item=int(item);ids.add(item)
                if blocks[item][0]==224:
                    children=blocks[item][1].split('  m_Children:',1)[1].split('  m_Father:',1)[0]
                    for child in re.findall(r'fileID: (\d+)',children):visit(int(re.search(r'm_GameObject: \{fileID: (\d+)\}',blocks[int(child)][1])[1]))
        visit(go);mapping={old:counter+i for i,old in enumerate(sorted(ids))};counter+=len(ids)
        rect=rect_of(component)
        for old in sorted(ids):
            kind,b=blocks[old]
            b=re.sub(r'fileID: (\d+)(?=\})',lambda m:'fileID: '+str(mapping.get(int(m[1]),int(m[1]))),b)
            if old==go:b=re.sub(r'  m_Name: .*','  m_Name: '+label,b)
            if old==rect:b=re.sub(r'  m_Father: \{fileID: \d+\}','  m_Father: {fileID: '+str(parent)+'}',b)
            blocks[mapping[old]]=(kind,b)
        attach(parent,mapping[rect]);return mapping[component]

    def field(key,value):
        kind,b=blocks[view_id]
        b=re.sub(r'^  '+key+r':.*(?:\n  - \{fileID: \d+\})*\n','',b,flags=re.M)
        b+=('  '+key+':\n'+''.join('  - {fileID: '+str(i)+'}\n' for i in value)) if isinstance(value,list) else '  '+key+': {fileID: '+str(value)+'}\n'
        blocks[view_id]=(kind,b)

    view=blocks[view_id][1];slots=sequence('Slots',view);grids=sequence('Grids',view)
    if name=='BaseHub':
        organize=int(re.search(r'^  Organize: \{fileID: (\d+)\}',view,re.M)[1])
        parent=int(re.search(r'^  Equipment: \{fileID: (\d+)\}',view,re.M)[1])
        field('CharacterShortcut',clone(organize,'CharacterShortcut',parent))
        for i,(kind,body) in list(blocks.items()):
            if 'm_text: \"EQUIPAMENTO\"' in body: blocks[i]=(kind,body.replace('m_text: \"EQUIPAMENTO\"','m_text: \"PREPARATIVOS\"'))
    elif name=='Inventory':
        pack=slots[:9];chest=slots[9:18]
        pack += [clone(pack[0],'BackpackSlot'+str(i),grids[0]) for i in range(9,12)]
        chest += [clone(chest[0],'SafeChestSlot'+str(i),grids[1]) for i in range(9,16)]
        field('Slots',pack+chest);field('BackpackSlots',pack);field('StorageSlots',chest)
        offers=sequence('ShopOffers',view)
        panel=int(re.search(r'^  Panel: \{fileID: (\d+)\}',view,re.M)[1])
        offers += [clone(offers[0],'ShopOffer'+str(i),panel) for i in range(6,12)]
        field('ShopOffers',offers)
        icon=int(re.search(r'^  icon: \{fileID: (\d+)\}',blocks[slots[0]][1],re.M)[1])
        icons=[]
        for i,offer in enumerate(offers):
            component=clone(icon,'OfferIcon'+str(i),rect_of(offer));icons.append(component)
            rect=rect_of(component);kind,b=blocks[rect]
            for key,value in [('m_AnchorMin','{x: 0.12, y: 0.46}'),('m_AnchorMax','{x: 0.88, y: 0.96}'),('m_AnchoredPosition','{x: 0, y: 0}'),('m_SizeDelta','{x: 0, y: 0}')]:b=re.sub(r'  '+key+r': .*','  '+key+': '+value,b)
            blocks[rect]=(kind,b)
        field('ShopIcons',icons)
        close=sequence('Buttons',view)[0]
        for key in ['DiscardButton','CancelDiscard','SaleSource','UnequipButton']:field(key,clone(close,key,panel))
    else:
        slots += [clone(slots[0],'CookingSlot'+str(i),grids[0]) for i in range(9,12)]
        field('Slots',slots)
    path.write_text('%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'+''.join('--- !u!'+str(kind)+' &'+str(i)+'\n'+b for i,(kind,b) in blocks.items()),encoding='utf-8')
    print(name+': expanded authored controls and explicit slot arrays')

def sequence(key,text):
    m=re.search(r'^  '+key+r':\n((?:  - \{fileID: \d+\}\n)+)',text,re.M)
    return [int(i) for i in re.findall(r'fileID: (\d+)',m[1])] if m else []

reform('Inventory');reform('Activities');reform('BaseHub')

def author_shop_stock():
    path=ROOT/'Assets/Resources/UI/Prefabs/Inventory.prefab'
    source=path.read_text(encoding='utf-8-sig')
    if '  ShopStock:' in source:return
    blocks={int(m[2]):(int(m[1]),m[3]) for m in re.finditer(r'^--- !u!(\d+) &(\d+)\n([\s\S]*?)(?=^--- !u!|\Z)',source,re.M)}
    view_id=next(i for i,(_,b) in blocks.items() if '  BackpackSlots:' in b);view=blocks[view_id][1]
    quantity=int(re.search(r'^  quantity: \{fileID: (\d+)\}',source,re.M)[1])
    go=int(re.search(r'm_GameObject: \{fileID: (\d+)\}',blocks[quantity][1])[1])
    ids=[go]+[int(i) for i in re.findall(r'component: \{fileID: (\d+)\}',blocks[go][1])]
    counter=max(blocks)+100;stock=[]
    for offer in sequence('ShopOffers',view):
        owner=int(re.search(r'm_GameObject: \{fileID: (\d+)\}',blocks[offer][1])[1])
        parent=next(i for i,(kind,b) in blocks.items() if kind==224 and 'm_GameObject: {fileID: '+str(owner)+'}' in b)
        mapping={old:counter+i for i,old in enumerate(ids)};counter+=len(ids)
        for old in ids:
            kind,b=blocks[old];b=re.sub(r'fileID: (\d+)(?=\})',lambda m:'fileID: '+str(mapping.get(int(m[1]),int(m[1]))),b)
            if old==go:b=re.sub(r'  m_Name: .*','  m_Name: StockPrice',b)
            if kind==224:
                b=re.sub(r'  m_Father: .*','  m_Father: {fileID: '+str(parent)+'}',b)
                for key,value in [('m_AnchorMin','{x: 0.06, y: 0.80}'),('m_AnchorMax','{x: 0.94, y: 0.98}'),('m_AnchoredPosition','{x: 0, y: 0}'),('m_SizeDelta','{x: 0, y: 0}')]:b=re.sub(r'  '+key+r': .*','  '+key+': '+value,b)
                pk,pb=blocks[parent];blocks[parent]=(pk,pb.replace('  m_Father:','  - {fileID: '+str(mapping[old])+'}\n  m_Father:',1))
            blocks[mapping[old]]=(kind,b)
        stock.append(mapping[quantity])
    kind,b=blocks[view_id];blocks[view_id]=(kind,b+'  ShopStock:\n'+''.join('  - {fileID: '+str(i)+'}\n' for i in stock))
    path.write_text('%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'+''.join('--- !u!'+str(kind)+' &'+str(i)+'\n'+b for i,(kind,b) in blocks.items()),encoding='utf-8')

author_shop_stock()
path=ROOT/'Assets/Resources/UI/Prefabs/PlayerInterface.prefab'
source=path.read_text(encoding='utf-8-sig')
source=re.sub(r'^  groundItems: .*\n','',source,flags=re.M)
path.write_text(source,encoding='utf-8')
for name in ['UguiInventoryPresenter.Layout']:
    path=ROOT/'Assets/Scripts/UI'/(name+'.cs.meta')
    if not path.exists():path.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nMonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n',encoding='utf-8')
