"""Offline art composition using the shipped assets; this is not a Unity capture."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter
import numpy as np

root=Path(__file__).resolve().parents[1]
assets=root/'Assets/Resources/Environment/Sawmill'
width,height,ppu=960,540,24

def repeat(name):
    tile=Image.open(assets/(name+'.png')).convert('RGB').resize((8*ppu,8*ppu),Image.Resampling.LANCZOS)
    canvas=Image.new('RGB',(width,height))
    for y in range(0,height,tile.height):
        for x in range(0,width,tile.width): canvas.paste(tile,(x,y))
    return canvas

soil,road,wet=map(repeat,['LeafSoil','RoadSoil','WetSoil'])
mask=Image.new('L',(width,height));draw=ImageDraw.Draw(mask)
# Representative junction, deliberately not advertised as the procedural map layout.
path=[(x,470-int(220*(x/560)**1.2)) for x in range(0,561,4)]
branch=[(x,250-int(150*((x-560)/400)**.8)) for x in range(560,961,4)]
upper=[(560-int(150*t/100),250-int(250*t/100)) for t in range(101)]
draw.line(path+branch,fill=255,width=7*ppu,joint='curve')
draw.line(upper,fill=255,width=7*ppu,joint='curve')
for x,y in [path[-1],upper[-1]]:draw.ellipse((x-84,y-84,x+84,y+84),fill=255)
mask=mask.filter(ImageFilter.GaussianBlur(7))
base=Image.composite(road,soil,mask)
values=np.asarray(base).astype(float)/255
value=values@np.array([.299,.587,.114])
values=(.7*values+.3*value[:,:,None])*.88
base=Image.fromarray((values*255).clip(0,255).astype('uint8')).convert('RGBA')
logs=Image.open(assets/'LogPile.png').resize((168,43),Image.Resampling.LANCZOS)
dust=Image.open(assets/'Sawdust.png').resize((216,216),Image.Resampling.LANCZOS)
for y in [105,187,269]:
    base.alpha_composite(dust,(75,y-86));base.alpha_composite(logs,(99,y))
for name,x,y,size in [('Leaves',745,350,96),('Moss',640,360,96),('Pebbles',390,345,48),('DampPatch',850,420,96)]:
    stamp=Image.open(assets/(name+'.png')).resize((size,size),Image.Resampling.LANCZOS)
    base.alpha_composite(stamp,(x,y))
base.convert('RGB').save(root/'Art/SawmillTerrain/material-composition.jpg',quality=95)
