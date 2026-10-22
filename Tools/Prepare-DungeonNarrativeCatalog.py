"""Prepare the complete narrative art catalogue and a local review gallery.

Inspects PNGs without repainting/resampling. Does not place objects or alter pilots.
"""
from pathlib import Path
import html
import json
import re
import uuid
from PIL import Image

ROOT=Path(__file__).resolve().parents[1]
ART=ROOT/'Art/Catacumbas/Narrativa'
RUNTIME=ROOT/'Assets/Resources/Environment/Dungeon/Narrative'
DOCS=ROOT/'Docs/Mapas/DecoracaoNarrativa'
SPECS=[
 ('N01',1,'Mortalha rasgada',[2,1],'sepultura','CoverOrFloor',.9,'Integrado no piloto R08.'),
 ('N02',1,'Caixão inacabado',[3,1],'coveiros','Stone',1,'Volume apenas sobre área inacessível; piloto R19.'),
 ('N03',1,'Ferramentas de coveiro',[2,1],'coveiros','Floor',.95,'Peças rasas, sem interação; piloto R19.'),
 ('N04',1,'Padiola funerária',[3,1],'coveiros','Stone',.95,'Volume em recesso; não criar cobertura ou pôr no eixo de circulação.'),
 ('N05',1,'Incensário caído',[1,1],'ritual','Floor',.85,'Sem luz, fumaça ou fogo; fora de interações.'),
 ('N06',1,'Tigela e utensílios de cela',[1,1],'cela','Floor',.8,'Objeto pequeno em bordas; não é recipiente de loot.'),
 ('N07',1,'Palha deteriorada',[2,1],'cela','Floor',.4,'Decalque raso e discreto; não é cama ou obstáculo.'),
 ('N08',1,'Madeira encalhada',[3,1],'enchente','Floor',.9,'Fragmentos rasos na margem seca; piloto R13. Não formar ponte.'),
 ('N09',2,'Sedimentos de margem',[3,1],'enchente','Floor',.5,'Decalque plano; versão 1 rejeitada, somente v02 selecionada.'),
 ('N10',1,'Mochila rasgada decorativa',[1,1],'expedicao','Floor',.75,'Vazia e inutilizável. Não usar ItemIconLibrary ou aparência de pickup.'),
 ('N11',1,'Acampamento apagado',[2,2],'expedicao','Floor',.65,'Cinzas e objetos rasos; sem fogo, interação, novo loot ou zona segura.'),
]
assets=[]
for id,version,name,envelope,family,support,opacity,notes in SPECS:
    filename=f'CAT-{id}_v{version:02}.png'
    path=RUNTIME/filename
    with Image.open(path) as image:
        if image.mode!='RGBA': raise ValueError(f'{filename}: falta RGBA')
        alpha=image.getchannel('A'); box=alpha.getbbox(); extrema=alpha.getextrema()
        if not box or extrema[0]!=0 or extrema[1]!=255:
            raise ValueError(f'{filename}: alpha vazio/opaco ou objeto sem corpo opaco')
        w,h=image.size; left,top,right,bottom=box
        if w<1024 or h<512: raise ValueError(f'{filename}: fonte pequena')
        assets.append(dict(id=id,name=name,resource='Environment/Dungeon/Narrative/'+filename[:-4],
                           x=left/w,y=(h-bottom)/h,width=(right-left)/w,height=(bottom-top)/h,
                           sourcePixels=[w,h],alphaBoundsPixels=list(box),
                           envelopeCells=envelope,family=family,support=support,
                           suggestedOpacity=opacity,notes=notes))
    meta=path.with_suffix('.png.meta')
    if not meta.exists():
        source=(RUNTIME/'CAT-N01_v01.png.meta').read_text(encoding='utf-8-sig')
        source=re.sub(r'guid: \w+','guid: '+uuid.uuid4().hex,source,count=1)
        meta.write_text(source,encoding='utf-8')
catalog=dict(version=1,stage=3,assets=assets,
             placementStatus='Art catalogue only; no additional map placements in stage 3.',
             geometry='Envelopes in cells, not colliders. Fit sprite uniformly inside envelope.',
             referenceCellSize=1.06)
catalog_path=RUNTIME/'NarrativeCatalog.json'
catalog_path.write_text(json.dumps(catalog,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
meta=catalog_path.with_suffix('.json.meta')
if not meta.exists():
    meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+
                    '\nTextScriptImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n',encoding='utf-8')

# Check static resource references and independent import GUIDs; no Unity session.
guids=[]
for asset in assets:
    path=ROOT/'Assets/Resources'/(asset['resource']+'.png')
    meta=path.with_suffix('.png.meta').read_text(encoding='utf-8-sig')
    guid=re.search(r'guid: (\w+)',meta)[1]
    if guid in guids: raise ValueError('GUID duplicado no lote: '+guid)
    guids.append(guid)
    for setting in ['enableMipMap: 0','filterMode: 1','wrapU: 1','alphaIsTransparency: 1','textureCompression: 0']:
        if setting not in meta: raise ValueError(asset['id']+': importação divergente '+setting)
report=dict(kind='static art inspection, not Unity validation',families=len(assets),
            newInStage3=['N04','N05','N06','N07','N10','N11'],
            alphaAndResourcePaths='present',distinctImportGuids=len(guids),assets=assets)
(DOCS/'arte-etapa3-levantamento.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')

page=['<!doctype html><html lang="pt-BR"><meta charset="utf-8">',
 '<meta name="viewport" content="width=device-width,initial-scale=1">',
 '<title>Catacumbas — catálogo narrativo</title>',
 '<style>body{margin:24px;background:#171b19;color:#e6ddc6;font:16px system-ui}h1{font-size:26px}p{max-width:900px;line-height:1.5}button,select{font:inherit;padding:8px;margin:4px;border:1px solid #8c795a;background:#282d28;color:inherit}main{display:grid;grid-template-columns:repeat(auto-fit,minmax(260px,1fr));gap:16px}article{border:1px solid #665b46;padding:12px}h2{font-size:18px}figure{margin:0;height:210px;display:flex;align-items:center;justify-content:center;background:#303b36}img{max-width:95%;max-height:190px;object-fit:contain}small{color:#bcb8a7}body.light figure{background:#bbb5a4}body.check figure{background-color:#40443e;background-image:linear-gradient(45deg,#50554c 25%,transparent 25%),linear-gradient(-45deg,#50554c 25%,transparent 25%),linear-gradient(45deg,transparent 75%,#50554c 75%),linear-gradient(-45deg,transparent 75%,#50554c 75%);background-size:24px 24px;background-position:0 0,0 12px,12px -12px,-12px 0}</style>',
 '<h1>Catacumbas — 11 famílias narrativas</h1>',
 '<p>Galeria de fontes, sem iluminação ou fog da Unity. Cinco famílias dos pilotos e seis complementares. Não é uma captura do mapa. O tamanho selecionado representa o canvas da imagem; o enquadramento no jogo usa o limite do alpha.</p>',
 '<label>Fundo <select id="bg"><option value="">Escuro</option><option value="light">Claro</option><option value="check">Quadriculado</option></select></label>',
 '<label>Prévia <select id="size"><option value="">Ampla</option><option value="64">64 px</option><option value="96">96 px</option><option value="128">128 px</option></select></label><main>']
for asset in assets:
    filename=asset['resource'].split('/')[-1]+'.png'
    page.append(f'<article><h2>{asset["id"]} — {html.escape(asset["name"])}</h2><figure><img alt="{html.escape(asset["name"])}" src="{filename}"></figure><p>{html.escape(asset["notes"])}</p><small>{asset["sourcePixels"][0]}×{asset["sourcePixels"][1]} px · envelope {asset["envelopeCells"][0]}×{asset["envelopeCells"][1]} células</small></article>')
page+=['</main><script>document.getElementById("bg").onchange=e=>document.body.className=e.target.value;document.getElementById("size").onchange=e=>document.querySelectorAll("img").forEach(i=>{i.style.maxWidth=e.target.value?e.target.value+"px":"95%";i.style.maxHeight=e.target.value?e.target.value+"px":"190px"});</script></html>']
(ART/'galeria.html').write_text('\n'.join(page),encoding='utf-8')
print(json.dumps({'families':len(assets),'newInStage3':6,'distinctGuids':len(guids),
                  'runtimePlacementChanges':0},ensure_ascii=False))
