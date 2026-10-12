"""Package native Blender renders; does not generate or retouch character art."""
from pathlib import Path
from PIL import Image, ImageDraw
import sys

root=Path(sys.argv[1]).resolve() if len(sys.argv)>1 else Path(__file__).resolve().parent
for view in ['topo','lateral']:
    frames=[]
    for path in sorted((root/view).glob('*.png')):
        src=Image.open(path).convert('RGBA')
        bg=Image.new('RGBA',src.size,(29,36,32,255))
        bg.alpha_composite(src)
        frames.append(bg.convert('RGB'))
    if len(frames)!=32:raise RuntimeError(f'{view}: expected 32 frames, got {len(frames)}')
    # GIF centisecond quantization: 40,40,40,40,50,40 gives ~24 fps.
    durations=[round((i+1)*1000/24/10)*10-round(i*1000/24/10)*10 for i in range(32)]
    frames[0].save(root/f'caminhada-{view}.gif',save_all=True,append_images=frames[1:],duration=durations,loop=0)
sheet=Image.new('RGB',(1280,680),(29,36,32))
draw=ImageDraw.Draw(sheet)
for col,frame in enumerate([1,9,17,25]):
    for row,view in enumerate(['topo','lateral']):
        src=Image.open(root/view/f'{frame:03}.png').convert('RGBA');src.thumbnail((320,320))
        sheet.paste(src,(col*320,row*340),src)
    draw.text((col*320+14,660),f'Quadro {frame:02}',fill='white')
sheet.save(root/'poses.png')
print('GIFs e prancha prontos')
