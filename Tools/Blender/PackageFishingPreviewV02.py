"""Package actual Blender frames into the review deliverables."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import json, shutil

out=Path(__file__).resolve().parents[2]/'Art/Fishing/Preview-v02'
paths=[out/'frames'/f'frame_{f:04d}.png' for f in range(1,217)]
missing=[str(p) for p in paths if not p.exists()]
if missing: raise RuntimeError(f'Missing {len(missing)} original Blender frames')
if not (out/'concept-simple.png').exists(): raise RuntimeError('Simple theme still is missing')
views=[]
for p in paths:
    with Image.open(p) as im:
        if im.size!=(1280,720): raise ValueError(f'Unexpected frame size: {p}')
        views.append(im.convert('RGB').resize((960,540),Image.Resampling.LANCZOS))
palette=views[139].quantize(colors=128,method=Image.Quantize.MEDIANCUT)
indexed=[im.quantize(palette=palette,dither=Image.Dither.NONE) for im in views]
indexed[0].save(out/'fishing-preview-v02.gif',save_all=True,append_images=indexed[1:],duration=[80,80,90]*72,loop=0,optimize=False)
shutil.copyfile(out/'frames/frame_0140.png',out/'concept-somber.png')
font=ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf',24)
sheet=Image.new('RGB',(1920,1180),(13,19,15)); draw=ImageDraw.Draw(sheet)
for i,(f,label) in enumerate([(8,'01 · Escolher a isca'),(63,'02 · O peixe recusa'),(110,'03 · Mordida e consumo da minhoca'),(149,'04 · Disputa de tensão')]):
    x=(i%2)*960; y=(i//2)*590
    draw.text((x+22,y+12),label,font=font,fill=(225,215,188))
    sheet.paste(views[f-1],(x,y+45))
sheet.save(out/'storyboard-v02.png')
compare=Image.new('RGB',(1280,1480),(13,19,15))
draw=ImageDraw.Draw(compare)
for i,(name,label) in enumerate([('concept-somber.png','Sombria'),('concept-simple.png','Simples · mesmas posições e controles')]):
    y=i*740; draw.text((24,y+7),label,font=font,fill=(225,215,188))
    with Image.open(out/name) as im: compare.paste(im.convert('RGB'),(0,y+30))
compare.save(out/'themes-v02.png')
with Image.open(out/'fishing-preview-v02.gif') as gif:
    duration=0
    for f in range(gif.n_frames): gif.seek(f); duration+=gif.info.get('duration',0)
    stats={'frames':gif.n_frames,'duration_ms':duration,'size':gif.size,'bytes':(out/'fishing-preview-v02.gif').stat().st_size}
(out/'preview-manifest.json').write_text(json.dumps(stats,indent=2),encoding='utf-8')
print(json.dumps(stats))
