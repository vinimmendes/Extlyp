"""Package original extraction PNGs and describe sprite regions; never edit image pixels."""
from pathlib import Path
import hashlib
import html
import json
import shutil
import uuid
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
ART = ROOT / 'Art/Catacumbas/Extracoes'
RUNTIME = ROOT / 'Assets/Resources/Environment/Dungeon/Exits'
ITEMS = ROOT / 'Assets/Resources/Items/Dungeon'
COUNTS = [1, 1, 1, 1, 1, 1, 3, 1, 1, 2, 1, 2, 1, 1, 1, 1, 3]


def meta(path, body):
    target = Path(str(path) + '.meta')
    if not target.exists():
        target.write_text('fileFormatVersion: 2\nguid: ' + uuid.uuid4().hex + '\n' + body, encoding='utf8')


def main():
    records, entries, cards = [], [], []
    for family, count in enumerate(COUNTS, 1):
        for variant in range(1, count + 1):
            name = f'CAT-E{family:02}_v{variant:02}'
            record = json.loads((ART / 'Prompts' / (name + '.json')).read_text(encoding='utf-8-sig'))
            source = ROOT / record['selected_source'] if 'selected_source' in record else ART / 'Fontes' / (name + '.png')
            resource = f'Items/Dungeon/CAT-E{family:02}' if family in (15, 16) else 'Environment/Dungeon/Exits/' + name
            regions = []
            with Image.open(source) as image:
                if image.mode != 'RGBA':
                    raise ValueError(name + ': RGBA required')
                width, height = image.size
                alpha = image.getchannel('A').point(lambda a: 255 if a > 16 else 0)
                cols, rows = record['columns'], record['rows']
                for frame in range(record['frames']):
                    col, row = frame % cols, frame // cols
                    x0, y0 = round(col*width/cols), round(row*height/rows)
                    x1, y1 = round((col+1)*width/cols), round((row+1)*height/rows)
                    # Read-only analysis within each frame; both gate stops / lock footing anchor its body.
                    box = alpha.crop((x0, y0, x1, y1)).getbbox()
                    if box is None:
                        raise ValueError(f'{name}: empty frame {frame+1}')
                    left, top, right, bottom = box
                    left += x0; right += x0; top += y0; bottom += y0
                    pivot_x, pivot_y = .5, .5
                    if family == 11:
                        # The rotating latch extends past the footing. Register the fixed stone support,
                        # rather than rescaling each frame's changing silhouette.
                        band_top = top + round((bottom-top)*.1)
                        band_bottom = top + round((bottom-top)*.3)
                        band = alpha.crop((x0, band_top, x1, band_bottom)).getbbox()
                        if band is None:
                            raise ValueError(name + ': lock support anchor missing')
                        support_width = band[2]-band[0]
                        anchor_x = x0+(band[0]+band[2])/2
                        anchor_y = top+support_width*.48
                        left = round(anchor_x-216); right = left+432
                        if left < x0 or right > x1:
                            raise ValueError(name + ': registered lock frame crosses cell boundary')
                        top, bottom = y0, y1
                        pivot_x = (anchor_x-left)/(right-left)
                        pivot_y = (bottom-anchor_y)/(bottom-top)
                    regions.append([left, top, right, bottom])
                    entry_id = name + (f'_f{frame+1:02}' if record['frames'] > 1 else '')
                    entries.append(dict(id=entry_id, resource=resource, x=left/width, y=(height-bottom)/height,
                                        width=(right-left)/width, height=(bottom-top)/height, pivotX=pivot_x, pivotY=pivot_y))
            record.update(source=source.relative_to(ROOT).as_posix(), source_pixels=[width, height], sprite_regions_pixels=regions, resource=resource)
            if record.get('revisions'):
                record['revision_prompts'] = [json.loads((ROOT/path).read_text(encoding='utf-8-sig'))['prompt'] for path in record['revisions']]
            records.append(record)

    for folder in (RUNTIME, ITEMS):
        folder.mkdir(parents=True, exist_ok=True)
        meta(folder, 'folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n')
    for record in records:
        source = ROOT / record['source']
        destination = ROOT / 'Assets/Resources' / (record['resource'] + '.png')
        if destination.exists():
            if hashlib.sha256(source.read_bytes()).digest() != hashlib.sha256(destination.read_bytes()).digest():
                raise FileExistsError('Preserving different existing asset: ' + str(destination))
        else:
            shutil.copyfile(source, destination)
        maximum = 128
        if record['family'] not in (15, 16):
            width, height = record['source_pixels']
            required = max(max(record['w']*128*width/(r-l), record['h']*128*height/(b-t))
                           for l, t, r, b in record['sprite_regions_pixels'])
            while maximum < required:
                maximum *= 2
        meta(destination, f'''TextureImporter:
  serializedVersion: 13
  mipmaps:
    enableMipMap: 0
    sRGBTexture: 1
  isReadable: 0
  maxTextureSize: {maximum}
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  textureType: 0
  textureShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: {maximum}
    textureFormat: -1
    textureCompression: 0
    overridden: 0
''')
        cards.append(f'<article><h2>{record["id"]}</h2><p>{record["frames"]} quadro(s)</p><img src="Fontes/{source.name}" alt="{record["id"]}"><details><summary>Prompt</summary><p>{html.escape(record["prompt"])}</p></details></article>')
    manifest = RUNTIME / 'ExtractionsArt.json'
    manifest.write_text(json.dumps(dict(assets=entries), indent=2)+'\n', encoding='utf8')
    meta(manifest, 'TextScriptImporter:\n  externalObjects: {}\n')
    (ART/'prompts-v1.json').write_text(json.dumps(records, ensure_ascii=False, indent=2)+'\n', encoding='utf8')
    (ART/'index.html').write_text('''<!doctype html><html lang="pt-BR"><meta charset="utf-8"><title>Catacumbas — extrações</title>
<style>body{margin:32px;background:#171a1c;color:#d6cdbb;font:16px system-ui}main{display:grid;grid-template-columns:repeat(auto-fit,minmax(280px,1fr));gap:24px}article{padding:16px;background:#292d2e;border-radius:8px}h2{font-size:16px}img{width:100%;height:260px;object-fit:contain;background:#50534e}p{line-height:1.5}</style>
<h1>Catacumbas — extrações e mecanismos</h1><p>23 PNGs originais, 37 regiões de sprites incluindo dois conjuntos de oito quadros. Arte isolada; não é captura da Unity. Ícones importados com limite de 128px.</p><main>'''+'\n'.join(cards)+'</main></html>', encoding='utf8')
    for script in ('Assets/Scripts/Match/DungeonExtractionArt.cs', 'Assets/Scripts/Items/DungeonToolArt.cs'):
        meta(ROOT/script, '')
    print(f'Packaged {len(records)} intact RGBA PNGs; {len(entries)} sprite regions.')


if __name__ == '__main__':
    main()
