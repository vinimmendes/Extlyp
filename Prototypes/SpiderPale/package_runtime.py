"""Install approved Blender frames as Unity atlases, facing +X like the monster root."""
from pathlib import Path
from PIL import Image
BASE=Path(__file__).resolve().parent
DEST=BASE.parents[1]/'Assets'/'Resources'/'Monsters'/'Weaver'
DEST.mkdir(parents=True,exist_ok=True)
for action,count,start in [('Walk',32,1),('Web',28,101)]:
    atlas=Image.new('RGBA',(2048,1024))
    for i in range(count):
        frame=Image.open(BASE/action/f'{start+i:03}.png').convert('RGBA')
        # Approved preview faces up; clockwise rotation gives game forward +X.
        frame=frame.transpose(Image.Transpose.ROTATE_270).resize((256,256),Image.Resampling.LANCZOS)
        atlas.paste(frame,((i%8)*256,(i//8)*256))
    atlas.save(DEST/(action+'.png'))
print('Weaver walking and web-cast atlases installed.')
