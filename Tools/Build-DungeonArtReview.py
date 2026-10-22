"""Build an offline art catalogue from existing files. No Unity, game simulation or image editing."""
from pathlib import Path
import hashlib
import html
import json
import os
import re
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT/'Art/Catacumbas/Revisao'
PACKS = [('Pisos','Floors',None), ('Arquitetura','Architecture','ArchitectureArt.json'),
         ('Sepulturas','Props','PropsArt.json'), ('Setores','Sectors','SectorsArt.json'),
         ('Extracoes','Exits','ExtractionsArt.json'), ('Detalhes','Details','DetailsArt.json')]


def main():
    catalogue = json.loads((ROOT/'Docs/Mapas/CATACUMBAS_CATALOGO_SPRITES.json').read_text(encoding='utf8'))
    families = {a['id']: a for a in catalogue['assets']}
    cards, inventory, findings, guids, pack_counts = [], [], [], {}, {}
    OUT.mkdir(parents=True, exist_ok=True)
    escape = html.escape
    for label, folder, manifest_name in PACKS:
        records = json.loads((ROOT/f'Art/Catacumbas/{label}/prompts-v1.json').read_text(encoding='utf8'))
        runtime_folder = ROOT/f'Assets/Resources/Environment/Dungeon/{folder}'
        entries = json.loads((runtime_folder/manifest_name).read_text())['assets'] if manifest_name else []
        pack_counts[label] = len(records)
        for record in records:
            name = record['id']; family = families[name.split('_')[0]]
            source = ROOT/record['source']
            runtime = ROOT/'Assets/Resources'/(record['resource']+'.png') if 'resource' in record else runtime_folder/(name+'.png')
            facts = dict(id=name, pack=label, source=source.relative_to(ROOT).as_posix(), runtime=runtime.relative_to(ROOT).as_posix())
            if not source.exists() or not runtime.exists():
                findings.append(dict(id=name,kind='missing_file',note='Source or runtime PNG absent'))
                inventory.append(facts); continue
            facts['same_bytes'] = hashlib.sha256(source.read_bytes()).digest() == hashlib.sha256(runtime.read_bytes()).digest()
            if not facts['same_bytes']: findings.append(dict(id=name,kind='different_bytes',note='Selected source differs from runtime PNG'))
            with Image.open(runtime) as picture:
                width, height = picture.size
                alpha = picture.getchannel('A').getextrema() if picture.mode == 'RGBA' else (255,255)
                facts.update(mode=picture.mode,source_pixels=[width,height],alpha_min=alpha[0],alpha_max=alpha[1])
            opaque_floor = name.startswith(tuple(f'CAT-F{i:02}_' for i in (1,2,3,4,5,7)))
            if not opaque_floor and alpha[0] == 255:
                findings.append(dict(id=name,kind='opaque_sprite',note='Expected transparent sprite has no transparent pixels'))
            metadata = Path(str(runtime)+'.meta')
            if metadata.exists():
                text = metadata.read_text(encoding='utf-8-sig')
                guid = re.search(r'^guid: (\w+)',text,re.M)
                if guid:
                    previous = guids.get(guid[1])
                    if previous: findings.append(dict(id=name,kind='duplicate_guid',note=previous))
                    guids[guid[1]] = name
                maximum = re.search(r'maxTextureSize: (\d+)',text)
                facts['import_max_size'] = int(maximum[1]) if maximum else None
            else: findings.append(dict(id=name,kind='missing_meta',note='Texture metadata absent'))
            matching = [e for e in entries if e['id']==name or e['id'].startswith(name+'_f')]
            if not manifest_name:
                matching = [dict(id=name,x=0,y=0,width=1,height=1)]
            if not matching: findings.append(dict(id=name,kind='missing_region',note='No matching sprite region'))
            facts['regions'] = matching
            for entry in matching:
                if min(entry['x'],entry['y']) < 0 or min(entry['width'],entry['height']) <= 0 or entry['x']+entry['width'] > 1.000001 or entry['y']+entry['height'] > 1.000001:
                    findings.append(dict(id=entry['id'],kind='region_bounds',note='Region outside texture'))
            inventory.append(facts)
            url = escape(Path(os.path.relpath(runtime,OUT)).as_posix(),quote=True)
            previews = []
            for entry in matching:
                x, y = entry['x']*width, (1-entry['y']-entry['height'])*height
                w, h = entry['width']*width, entry['height']*height
                previews.append(f'<figure><svg class="art" viewBox="{x} {y} {w} {h}" role="img" aria-label="{escape(entry["id"])}"><image href="{url}" width="{width}" height="{height}"/></svg><figcaption>{escape(entry["id"])}</figcaption></figure>')
            opacity = {2:.38,3:.45,4:.3,5:.55,6:.42,7:.9,8:.28,9:.25,12:.38}.get(int(name[5:7]),.65) if name.startswith('CAT-D') else 1
            title = family['nome']; footprint = f'{family["largura_u"]} × {family["altura_u"]} u' if family['largura_u'] else 'Ícone / UI'
            search = escape((name+' '+label+' '+title).lower(),quote=True)
            cards.append(f'''<article data-pack="{label}" data-search="{search}" style="--detail-opacity:{opacity}"><h2>{name}</h2><p>{escape(title)}</p><div class="sample"><img class="art" loading="lazy" src="{url}" alt="{escape(title)}"></div><p>{footprint} · {width} × {height} px de fonte · {len(matching)} região(ões)</p><details><summary>Recortes usados na apresentação</summary><div class="frames">{''.join(previews)}</div></details><a href="{url}" target="_blank" rel="noopener">PNG original integrado</a></article>''')
    report = dict(kind='Static file inventory; no runtime validation',packs=pack_counts,selected_pngs=len(inventory),
                  sprite_regions=sum(len(row.get('regions',[])) for row in inventory),findings=findings,assets=inventory)
    (ROOT/'Docs/Mapas/CATACUMBAS_INVENTARIO_ARTE.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
    options = ''.join(f'<option>{name}</option>' for name,_,_ in PACKS)
    page = '''<!doctype html><html lang="pt-BR"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Catacumbas — revisão da arte</title>
<style>:root{color-scheme:dark;--sample:#68645b}body{margin:24px;background:#171a1c;color:#d6cdbb;font:15px system-ui}h1{font-size:27px}p{line-height:1.5}header{max-width:1000px}nav{position:sticky;top:0;background:#171a1c;padding:12px 0;display:flex;flex-wrap:wrap;gap:12px;z-index:1}input,select,button{font:inherit;padding:8px;background:#292d2e;color:inherit;border:1px solid #68645b;border-radius:4px}label{display:flex;gap:6px;align-items:center}main{display:grid;grid-template-columns:repeat(auto-fill,minmax(270px,1fr));gap:18px}article{background:#292d2e;padding:14px;border-radius:6px}article[hidden]{display:none}h2{font-size:16px;margin:0}.sample,.frames{background:var(--sample);border-radius:4px}.sample{height:150px;padding:10px}.sample img{width:100%;height:100%;object-fit:contain}.frames{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:8px;padding:8px}.frames figure{margin:0}.frames svg{width:100%;height:130px}figcaption{font-size:10px;overflow-wrap:anywhere;color:#fff;background:#292d2e;padding:3px}body.opacity .art{opacity:var(--detail-opacity)}a{color:#d0b37b}summary{cursor:pointer;padding:10px 0}#count{padding:8px}small{color:#b0a88a}</style>
<header><h1>Catacumbas — revisão do conjunto</h1><p>175 PNGs selecionados · 189 regiões de sprites · 69 famílias. Fontes existentes e recortes do manifesto, sem executar o jogo. Esta página não representa uma captura da Unity: não reproduz fog, iluminação, importação, colisão, composição das salas ou desempenho.</p><p>Use os filtros para comparar materiais e variantes. As dimensões são envelopes do catálogo; nichos, cordas e peças recolhidas têm encaixes menores documentados. A opacidade opcional reproduz apenas os valores da camada de detalhes.</p><p><a href="../../../Docs/Mapas/CATACUMBAS_ETAPA_3_8.md">Relatório e roteiro de avaliação prática</a> · <a href="../../../Docs/Mapas/CATACUMBAS_INVENTARIO_ARTE.json">Inventário estático</a></p></header>
<nav><input id="query" aria-label="Buscar arte" placeholder="ID ou nome do asset"><select id="pack" aria-label="Lote"><option value="">Todos os lotes</option>''' + options + '''</select><select id="backdrop" aria-label="Fundo de comparação"><option value="#68645b">Pedra média</option><option value="#171a1c">Fundo escuro</option><option value="#c2b7a2">Fundo claro</option></select><label><input id="opacity" type="checkbox">Opacidade dos detalhes</label><span id="count"></span></nav><main>''' + '\n'.join(cards) + '''</main><script>
const cards=[...document.querySelectorAll('article')],query=document.querySelector('#query'),pack=document.querySelector('#pack');
function filter(){let count=0;for(const card of cards){card.hidden=!!((pack.value&&card.dataset.pack!==pack.value)||!card.dataset.search.includes(query.value.toLowerCase()));if(!card.hidden)count++;}document.querySelector('#count').textContent=count+' imagens';}
query.addEventListener('input',filter);pack.addEventListener('change',filter);document.querySelector('#backdrop').addEventListener('change',e=>document.documentElement.style.setProperty('--sample',e.target.value));document.querySelector('#opacity').addEventListener('change',e=>document.body.classList.toggle('opacity',e.target.checked));filter();
</script></html>'''
    (OUT/'index.html').write_text(page,encoding='utf8')
    print(json.dumps({k:report[k] for k in ('packs','selected_pngs','sprite_regions','findings')},ensure_ascii=True))


if __name__=='__main__':
    main()
