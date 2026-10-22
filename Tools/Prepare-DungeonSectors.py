"""Package original PNGs; measure alpha bounds without modifying image pixels."""
from pathlib import Path
import hashlib
import html
import json
import re
import shutil
import uuid
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
ART = ROOT / 'Art/Catacumbas/Setores'
RUNTIME = ROOT / 'Assets/Resources/Environment/Dungeon/Sectors'
COUNTS = [8, 1, 1, 1, 1, 1, 1, 1]


def write_meta(path, text):
    if not path.exists():
        path.write_text('fileFormatVersion: 2\nguid: ' + uuid.uuid4().hex + '\n' + text, encoding='utf-8')


def main():
    records, bounds, cards = [], [], []
    RUNTIME.mkdir(parents=True, exist_ok=True)
    write_meta(Path(str(RUNTIME) + '.meta'), 'folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n')
    # Check the complete batch before changing catalogue or creating runtime assets.
    for family, count in enumerate(COUNTS, 1):
        for variant in range(1, count + 1):
            name = f'CAT-S{family:02}_v{variant:02}'
            record = json.loads((ART / 'Prompts' / (name + '.json')).read_text(encoding='utf-8-sig'))
            source = ART / 'Fontes' / (name + '.png')
            with Image.open(source) as image:
                if image.mode != 'RGBA':
                    raise ValueError(f'{name}: expected genuine RGBA')
                # Read-only mask analysis: ignore nearly invisible generation fringe.
                box = image.getchannel('A').point(lambda value: 255 if value > 16 else 0).getbbox()
                if box is None:
                    raise ValueError(f'{name}: empty alpha')
                left, top, right, bottom = box
                width, height = image.size
            bounds.append(dict(id=name, x=left/width, y=(height-bottom)/height,
                               width=(right-left)/width, height=(bottom-top)/height))
            record['source'] = source.relative_to(ROOT).as_posix()
            record['source_pixels'] = [width, height]
            record['alpha_body_bounds'] = list(box)
            record['sprite_region_pixels'] = [left, top, right, bottom]
            records.append(record)

    for record in records:
        source = ROOT / record['source']
        destination = RUNTIME / source.name
        if destination.exists():
            if hashlib.sha256(destination.read_bytes()).digest() != hashlib.sha256(source.read_bytes()).digest():
                raise FileExistsError(f'Preserving existing asset: {destination}')
        else:
            shutil.copyfile(source, destination)
        maximum = 128
        left, top, right, bottom = record['sprite_region_pixels']
        source_width, source_height = record['source_pixels']
        required = max(record['w']*128*source_width/(right-left),
                       record['h']*128*source_height/(bottom-top))
        while maximum < required:
            maximum *= 2
        texture_meta = Path(str(destination) + '.meta')
        write_meta(texture_meta, f'''TextureImporter:
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
        # Preserve GUIDs and unrelated settings when the body crop changes its pixel budget.
        metadata = texture_meta.read_text(encoding='utf-8-sig')
        texture_meta.write_text(re.sub(r'maxTextureSize: \d+', f'maxTextureSize: {maximum}', metadata), encoding='utf-8')
        cards.append(f'<article><h2>{record["id"]}</h2><img src="Fontes/{source.name}" alt="{record["id"]}"><details><summary>Prompt</summary><p>{html.escape(record["prompt"])}</p></details></article>')

    manifest = RUNTIME / 'SectorsArt.json'
    manifest.write_text(json.dumps(dict(assets=bounds), indent=2) + '\n', encoding='utf-8')
    write_meta(Path(str(manifest) + '.meta'), 'TextScriptImporter:\n  externalObjects: {}\n')
    (ART / 'prompts-v1.json').write_text(json.dumps(records, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    (ART / 'index.html').write_text('''<!doctype html><html lang="pt-BR"><meta charset="utf-8"><title>Catacumbas — marcos dos setores</title>
<style>body{margin:32px;background:#171a1c;color:#d6cdbb;font:16px system-ui}main{display:grid;grid-template-columns:repeat(auto-fit,minmax(260px,1fr));gap:24px}article{padding:16px;background:#292d2e;border-radius:8px}h2{font-size:16px}img{width:100%;height:220px;object-fit:contain;background:#50534e}p{line-height:1.5}</style>
<h1>Catacumbas — marcos dos setores</h1><p>15 variantes. Fontes originais com alpha; não é captura da Unity. O jogo recorta a margem pelo manifesto e aplica a pegada do catálogo.</p><main>''' + '\n'.join(cards) + '</main></html>', encoding='utf-8')
    write_meta(ROOT / 'Assets/Scripts/Match/DungeonSectorArt.cs.meta', '')
    print(f'Packaged {len(records)} original RGBA assets; no image pixels edited.')


if __name__ == '__main__':
    main()
