"""Normalize generated surfaces, repair repeat edges and package small ground decals."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter
import numpy as np
import random, uuid

root = Path(__file__).resolve().parents[1]
art = root / 'Art/SawmillTerrain'
out = root / 'Assets/Resources/Environment/Sawmill'
out.mkdir(parents=True, exist_ok=True)
template = (root / 'Assets/Resources/Monsters/Stalker/ClawAttack.png.meta').read_text()

def save(image, name, repeat=False):
    path = out / (name + '.png')
    image.save(path)
    meta = path.with_suffix('.png.meta')
    if not meta.exists():
        text = template.replace(template.splitlines()[1], 'guid: ' + uuid.uuid4().hex)
        if repeat:
            text = text.replace('enableMipMap: 0', 'enableMipMap: 1')
            for axis in 'UVW': text = text.replace('wrap' + axis + ': 1', 'wrap' + axis + ': 0')
        meta.write_text(text)

surfaces = {}
for name in ['LeafSoil', 'RoadSoil', 'WetSoil']:
    image = Image.open(art / (name + '-source.png')).convert('RGB').resize((1024, 1024), Image.Resampling.LANCZOS)
    values = np.asarray(image).astype(float)
    # Crossfade symmetric border strips; opposite outer rows/columns become identical.
    for axis in [1, 0]:
        source = values.copy()
        for i in range(64):
            weight = .5 * (1 - i / 64) ** 2
            if axis == 1:
                values[:, i] = source[:, i] * (1-weight) + source[:, -1-i] * weight
                values[:, -1-i] = source[:, -1-i] * (1-weight) + source[:, i] * weight
            else:
                values[i] = source[i] * (1-weight) + source[-1-i] * weight
                values[-1-i] = source[-1-i] * (1-weight) + source[i] * weight
    image = Image.fromarray(values.clip(0,255).astype('uint8')).convert('RGBA')
    surfaces[name] = image
    save(image, name, True)
    mosaic = Image.new('RGB', (768,768))
    tile = image.resize((256,256),Image.Resampling.LANCZOS)
    for y in range(3):
        for x in range(3): mosaic.paste(tile,(x*256,y*256))
    mosaic.save(art / (name + '-repeat-3x3.jpg'))

rng = random.Random(43127)
for name in ['Leaves', 'Moss', 'Pebbles', 'Sawdust', 'WheelRuts', 'DampPatch']:
    image = Image.new('RGBA', (256,256))
    draw = ImageDraw.Draw(image)
    if name in ['Moss','DampPatch']:
        mask = Image.new('L',(256,256)); brush = ImageDraw.Draw(mask)
        for i in range(18):
            x,y=rng.randrange(55,170),rng.randrange(55,170)
            brush.ellipse((x-35,y-25,x+35,y+25),fill=rng.randrange(65,150))
        mask=mask.filter(ImageFilter.GaussianBlur(8))
        image=surfaces['WetSoil'].resize((256,256)); image.putalpha(mask)
    elif name == 'WheelRuts':
        for x in [91,165]:
            for y in range(12,244,7):
                draw.line((x+rng.randrange(-2,3),y,x+rng.randrange(-2,3),y+5),fill=(42,35,28,55),width=4)
        image=image.filter(ImageFilter.GaussianBlur(1))
    else:
        colors={'Leaves':[(101,83,45,100),(74,67,36,95)], 'Pebbles':[(99,95,82,110),(65,66,53,105)], 'Sawdust':[(138,110,61,75),(112,88,43,70)]}[name]
        for i in range(200 if name=='Sawdust' else 45):
            x,y=rng.gauss(128,38),rng.gauss(128,32)
            size=rng.uniform(.5,1.6) if name=='Sawdust' else rng.uniform(1.2,3.6)
            draw.ellipse((x-size*1.8,y-size,x+size*1.8,y+size),fill=rng.choice(colors))
    save(image,name)

log = Image.open(root / 'Art/ForestProps/LogPile-v2.png').convert('RGBA')
box = log.getchannel('A').getbbox()
save(log.crop(box).resize((1024,264),Image.Resampling.LANCZOS),'LogPile')
