"""Package actual Blender-rendered frames, without modifying their artwork."""
from pathlib import Path
from PIL import Image
import json

root=Path(__file__).resolve().parents[2]/'Art/Fishing/Preview-v01'
files=sorted((root/'frames').glob('frame_*.png'))
if len(files)!=96:
    raise RuntimeError(f'Expected 96 rendered frames, found {len(files)}')
frames=[Image.open(f).convert('RGB') for f in files]
frames[0].save(root/'fishing-preview-v01.gif',save_all=True,append_images=frames[1:],duration=[83,83,84]*32,loop=0,optimize=False)
contact=Image.new('RGB',(960*2,540*2))
for i,index in enumerate([12,30,42,65]):
    contact.paste(frames[index-1],((i%2)*960,(i//2)*540))
contact.save(root/'fishing-preview-poses.png')
print(json.dumps({'gif':str(root/'fishing-preview-v01.gif'),'frames':len(frames),'seconds':8}))
