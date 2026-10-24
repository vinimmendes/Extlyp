"""Package the horizontal fish-in-zone segment from original Blender renders."""
from pathlib import Path
from PIL import Image
import json, shutil
out=Path(__file__).resolve().parents[2]/'Art/Fishing/Preview-v03'
frames=[]
for f in range(117,190):
    with Image.open(out/'frames'/f'frame_{f:04d}.png') as im:
        frames.append(im.convert('RGB').resize((960,540),Image.Resampling.LANCZOS))
palette=frames[23].quantize(colors=128,method=Image.Quantize.MEDIANCUT)
indexed=[im.quantize(palette=palette,dither=Image.Dither.NONE) for im in frames]
indexed[0].save(out/'fish-in-zone-v03.gif',save_all=True,append_images=indexed[1:],duration=([80,80,90]*25)[:73],loop=0,optimize=False)
shutil.copyfile(out/'frames/frame_0140.png',out/'concept-somber.png')
with Image.open(out/'fish-in-zone-v03.gif') as im:
    duration=0
    for f in range(im.n_frames): im.seek(f); duration+=im.info['duration']
    stats={'frames':im.n_frames,'duration_ms':duration,'size':im.size}
(out/'preview-manifest.json').write_text(json.dumps(stats,indent=2),encoding='utf-8')
print(json.dumps(stats))
