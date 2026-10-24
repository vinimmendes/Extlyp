"""Assemble comparison evidence and production sheets from saved native outputs.
No gameplay session, test or player build. Does not approve any comparison.
"""
from pathlib import Path
from collections import Counter
import hashlib,json,math,textwrap
from PIL import Image,ImageDraw,ImageFont

ROOT=Path(__file__).resolve().parents[1]
ART=ROOT/'Art/Catacumbas/ReworkCompleto'
DOC=ROOT/'Docs/Mapas/ReworkCompleto'
OUT=ART/'Comparacoes';OUT.mkdir(exist_ok=True)
data=json.loads((ART/'MontagensUnity/Autoria.json').read_text(encoding='utf-8'))
ideas=json.loads((ART/'ideias.json').read_text(encoding='utf-8'))
bases=json.loads((ART/'bases.json').read_text(encoding='utf-8'))
catalog=json.loads((ROOT/'Assets/Resources/Environment/Dungeon/ReworkComplete/CatacombArt.catacombkit').read_text(encoding='utf-8'))['entries']
font=lambda size:ImageFont.truetype('C:/Windows/Fonts/arial.ttf',size)
sha=hashlib.sha256((ART/'MontagensUnity/Autoria.json').read_bytes()).hexdigest()
review_path=OUT/'revisao.json'
old=json.loads(review_path.read_text(encoding='utf-8')) if review_path.exists() else {}
records=[];md=['# Fichas de produção e comparação — Catacumbas','',
'Referências aprovadas: oito pranchas revisadas e as 24 ideias. Montagens capturadas por câmera ortográfica de produção na cena de autoria, fora do Play. Não comprovam combate, rede, navegação em execução ou desempenho.','',
'Cada comparação conserva a orientação superior e a sala inteira. O cais tem enquadramento ampliado para mostrar a barca real. A grade mede 1,06 unidade; personagem Ø 0,80. Não interpretar as proporções aproximadas dos conceitos como autorização para mudar pegadas físicas.','',
'Etapas só encerram após a revisão de fidelidade da montagem correspondente. Qualquer afastamento reabre o ciclo de causa → replanejamento → produção → comparação.','',
'Kit: '+str(len(catalog))+' entradas persistentes, incluindo 188 paredes e '+str(sum(e['family']=='Floors' for e in catalog))+' tiles de piso/transição. Fontes, prompts e recortes estão em Art/Catacumbas/ReworkCompleto.','']
gallery=Image.new('RGB',(1800,6*490),'#1b201d');gd=ImageDraw.Draw(gallery)
for sector,base in enumerate(bases):
    slug=base['slug'];ids=base['rooms']
    reference=Image.open(ART/f'Conceito-{slug}-v1.png').convert('RGB')
    for index,r in enumerate(ids):
        if slug=='Vestibulos':
            col,row=index%4,index//4;rect=(col/4,.12 if row==0 else .58,(col+1)/4,.505 if row==0 else .97)
        elif slug=='Galerias':
            col,row=index%2,index//2;rect=(col/2,.094 if row==0 else .545,(col+1)/2,.485 if row==0 else .95)
        elif slug=='Cisternas': rect=((.04,.09,.46,.515),(.51,.135,.995,.47),(.025,.56,.48,.98))[index]
        elif len(ids)==1: rect=(0,.12,1,.98)
        else: rect=(index/2,.16,(index+1)/2,.95)
        x0,y0,x1,y1=rect;ref=reference.crop((round(x0*reference.width),round(y0*reference.height),round(x1*reference.width),round(y1*reference.height)))
        actual=Image.open(ART/f'MontagensUnity/R{r:02d}.png').convert('RGB')
        board=Image.new('RGB',(1840,1110),'#1b201d');d=ImageDraw.Draw(board)
        d.text((20,15),f'R{r:02d} — CONCEITO APROVADO',font=font(25),fill='#e4d9be')
        d.text((940,15),'MONTAGEM NATIVA — FORA DO PLAY',font=font(25),fill='#e4d9be')
        for x,im in ((20,ref),(940,actual.copy())):
            factor=min(880/im.width,930/im.height);im=im.resize((round(im.width*factor),round(im.height*factor)),Image.Resampling.LANCZOS)
            board.paste(im,(x+(880-im.width)//2,65+(930-im.height)//2))
        note='Escala física preservada. Leito 2,0 × 0,8 u; bases existentes 2,12 × 2,12 u.' if r==17 else 'Peças funcionais e posições reais incluídas. A comparação visual não valida funcionamento em partida.'
        d.text((20,1030),note,font=font(20),fill='#c8c3b4')
        d.text((20,1065),'Desvios exigem replanejamento e ajuste antes da publicação.',font=font(20),fill='#c8c3b4')
        board.save(OUT/f'R{r:02d}.png')
        thumb=actual.copy();thumb.thumbnail((440,440));gx=(r%4)*450;gy=(r//4)*490
        gallery.paste(thumb,(gx+(450-thumb.width)//2,gy+40+(440-thumb.height)//2));gd.text((gx+10,gy+7),f'R{r:02d} — {data["rooms"][r]["name"]}',font=font(16),fill='#e4d9be')
        old_record=next((x for x in old.get('rooms',[]) if x['room']==r),{})
        record=dict(room=r,concept=f'Art/Catacumbas/ReworkCompleto/Conceito-{slug}-v1.png',comparison=f'Art/Catacumbas/ReworkCompleto/Comparacoes/R{r:02d}.png',
                    resolved=False,pendingDeviations='Revisar a montagem e registrar eventuais desvios.',replan=old_record.get('replan',''))
        if old.get('sourceSha256')==sha: record.update(old_record)
        records.append(record)
        idea=next(x for x in ideas[sector]['rooms'] if x[0]==f'R{r:02d}')
        visuals=[v for v in data['visuals'] if v['room']==r]
        counts=Counter(v['asset'] for v in visuals)
        md.extend([f'## R{r:02d} — {idea[1]}','',idea[2],'',
                   f'[Comparação lado a lado](../../../{record["comparison"]}) · [Montagem nativa](../../../Art/Catacumbas/ReworkCompleto/MontagensUnity/R{r:02d}.png)','',
                   f'Sala preservada: {data["rooms"][r]["width"]} × {data["rooms"][r]["height"]} células. {len(visuals)} peças decorativas editáveis, além do terreno e dos pontos funcionais.','',
                   '| Asset | Quantidade | Dimensões utilizadas (u) |','| --- | ---: | --- |'])
        for asset,count in sorted(counts.items()):
            sizes=sorted({f'{v["size"]["x"]:.2f} × {v["size"]["y"]:.2f}' for v in visuals if v['asset']==asset})
            md.append(f'| {asset} | {count} | {"; ".join(sizes)} |')
        md.extend(['','As pegadas vêm do terreno físico. Decoração não recebe colisão por aparência. Conjuntos podem ser desmontados no editor; nichos e apoios de vigília têm peças individuais.',''])
gallery.save(ART/'Galeria-montagens-Unity.png')
review_path.write_text(json.dumps(dict(sourceSha256=sha,kind='native-art-comparison',rooms=sorted(records,key=lambda r:r['room'])),ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
(DOC/'FICHAS_DE_PRODUCAO.md').write_text('\n'.join(md),encoding='utf-8')
print(json.dumps(dict(rooms=24,sourceSha256=sha,comparisons=str(OUT)),ensure_ascii=False))
