"""Import three starter icons using the existing V2 profile, without editing pixels."""
from pathlib import Path
import csv, json, re, shutil, uuid
from PIL import Image

ROOT=Path(__file__).resolve().parents[2]
selection=json.loads((ROOT/'Art/ItemIconsV2/starter-selected.json').read_text(encoding='utf-8-sig'))
resources=ROOT/'Assets/Resources/Items/IconsV2'
originals=ROOT/'Art/ItemIconsV2/Originals'
template=(resources/'Pistol.png.meta').read_text(encoding='utf-8-sig')
source=(ROOT/'Assets/Scripts/Items/ItemCatalog.cs').read_text(encoding='utf-8-sig')
ids=[s.strip() for s in re.search(r'enum ItemId : byte\s*\{([^}]+)',source)[1].split(',')]
catalog_path=ROOT/'Assets/Resources/ItemVisualCatalog.asset'
catalog=catalog_path.read_text(encoding='utf-8-sig')
report=[]; manifest={'Items':{}}
for entry in selection['assets']:
    name=entry['id']; src=Path(entry['source']); index=ids.index(name)
    for dest in [originals/(name+'.png'),resources/(name+'.png')]:
        if not dest.exists(): shutil.copyfile(src,dest)
        elif dest.read_bytes()!=src.read_bytes(): raise ValueError('Preserve edited art: '+str(dest))
    with Image.open(src) as image:
        if image.mode!='RGBA' or min(image.size)<1024 or image.getchannel('A').getextrema()[0]!=0:
            raise ValueError('Invalid source transparency or resolution: '+name)
        record=dict(item=name,id=index,sourcePixels=list(image.size),alphaBounds=image.getchannel('A').getbbox(),
                    importMaxSize=512,previewPixels=[32,48,64,96,128])
        report.append(record)
    meta=resources/(name+'.png.meta')
    if not meta.exists():
        text=re.sub(r'^guid: \w+','guid: '+uuid.uuid4().hex,template,flags=re.M)
        text=re.sub(r'spriteID: \w+','spriteID: '+uuid.uuid4().hex,text)
        meta.write_text(text,encoding='utf-8')
    guid=re.search(r'^guid: (\w+)',meta.read_text(encoding='utf-8-sig'),re.M)[1]
    ref='{fileID: 21300000, guid: '+guid+', type: 3}'
    expected=f'  - Id: {index}\n    InterfaceIcon: {ref}\n    WorldIcon: {ref}\n'
    existing=re.search(r'^  - Id: '+str(index)+r'\n    InterfaceIcon: [^\n]+\n    WorldIcon: [^\n]+\n?',catalog,re.M)
    if existing:
        if existing[0].rstrip()!=expected.rstrip(): raise ValueError('Preserve edited visual entry: '+name)
    else: catalog+=expected
    prompt_path=ROOT/f'Art/ItemIconsV2/{name}-prompt.txt'
    if not prompt_path.exists(): prompt_path.write_text(entry['prompt']+'\n',encoding='utf-8')
    manifest['Items'][name]={'Source':str(src),'PromptFile':str(prompt_path.relative_to(ROOT)).replace('\\','/'),
                             'World':'Explicit shared sprite; stable pickup scale'}
catalog_path.write_text(catalog,encoding='utf-8')
(ROOT/'Art/ItemIconsV2/lote-iniciais.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
docs=ROOT/'Docs/UI/ItemIconsV2'
csv_path=docs/'catalogo.csv'
with csv_path.open(encoding='utf-8-sig',newline='') as stream:
    reader=csv.DictReader(stream); fields=reader.fieldnames; rows=list(reader)
names={'ImprovisedHandCrossbow':'Besta de mão improvisada','RustySword':'Espada enferrujada','CrossbowBolt':'Virotes'}
for record in report:
    if any(row['Item']==record['item'] for row in rows): continue
    rows.append(dict(zip(fields,[record['id'],record['item'],names[record['item']],'I',
        'Novo item','ItemVisualCatalog.World explícito','Items/IconsV2/'+record['item'],'Integrado; avaliação prática pendente'])))
with csv_path.open('w',encoding='utf-8-sig',newline='') as stream:
    writer=csv.DictWriter(stream,fields); writer.writeheader(); writer.writerows(rows)
(docs/'iniciais-levantamento.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
page=['<!doctype html><html lang="pt-BR"><meta charset="utf-8"><title>Ícones iniciais</title>',
 '<style>body{background:#191e19;color:#eadfc5;font:16px system-ui;margin:24px}section{border:1px solid #756348;padding:16px;margin:16px 0}.sizes{display:flex;align-items:end;gap:24px;flex-wrap:wrap}figure{margin:0;text-align:center}img{object-fit:contain;background:#252d23}figcaption{margin-top:8px;font-size:13px}</style>',
 '<h1>Itens iniciais — V2</h1><p>Fontes originais em 32, 48, 64, 96 e 128 px. Prévia de arte; não simula Canvas, importação ou aparelho.</p>']
for entry in selection['assets']:
    page.append('<section><h2>'+entry['id']+'</h2><div class="sizes">')
    for size in [32,48,64,96,128]:
        page.append(f'<figure><img width="{size}" height="{size}" src="../../../Art/ItemIconsV2/Originals/{entry["id"]}.png"><figcaption>{size} px</figcaption></figure>')
    page.append('</div></section>')
page.append('</html>')
(docs/'iniciais.html').write_text('\n'.join(page),encoding='utf-8')
print(json.dumps({'newItems':len(report),'ids':[r['id'] for r in report],'iconCount':len(re.findall(r'^  - Id:',catalog,re.M))}))
