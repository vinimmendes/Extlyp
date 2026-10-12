"""Package the eight generated sprite-sheet cells as an animated GIF preview."""
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parents[1]
folder = root / 'Assets/Art/Player/WalkTest'
sheet = Image.open(folder / 'player-shotgun-walk-sheet-v1.png').convert('RGBA')
width, height = sheet.size
cell_w, cell_h = (width + 3) // 4, (height + 1) // 2
frames = []
for index in range(8):
    col, row = index % 4, index // 4
    bounds = (round(col*width/4), round(row*height/2),
              round((col+1)*width/4), round((row+1)*height/2))
    cell = sheet.crop(bounds)
    frame = Image.new('RGBA', (cell_w, cell_h), (0, 0, 0, 0))
    frame.paste(cell, (0, 0))
    frame.save(folder / f'walk-{index+1:02d}.png')
    # GIF is a preview container: PNG originals retain full RGBA and colors.
    gif_frame = frame.convert('RGB').quantize(colors=255)
    alpha = frame.getchannel('A')
    gif_frame.paste(255, mask=alpha.point(lambda a: 255 if a < 128 else 0))
    palette = gif_frame.getpalette()
    palette += [0] * (768-len(palette))
    gif_frame.putpalette(palette)
    frames.append(gif_frame)
output = folder / 'player-shotgun-walk-preview-v1.gif'
frames[0].save(output, save_all=True, append_images=frames[1:], duration=125,
               loop=0, disposal=2, transparency=255, optimize=False)
with Image.open(output) as preview:
    assert preview.n_frames == 8
    assert preview.info['loop'] == 0
    print(f'{width}x{height}; cells {cell_w}x{cell_h}; {preview.n_frames} frames; 8 fps; loop forever')
