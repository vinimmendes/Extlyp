"""Author geometry guides for painted concepts; never changes Unity assets."""
from pathlib import Path
import json
import runpy
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT/'Art/Catacumbas/ReworkCompleto'
OUT.mkdir(parents=True, exist_ok=True)
survey = runpy.run_path(str(ROOT/'Tools/Survey-CanalGeometry.py'))
rooms, dry, water = (survey[k] for k in ('rooms', 'dry', 'water'))
cells = survey['cells']
canal = cells(survey['rects']['CanalArea'])
low = set()
for i, (_, _, x, y, _, _) in enumerate(rooms):
    if i < 8: continue
    if i % 3: low |= cells((x-6,y-4,2,2))-canal
    low |= cells((x+4,y+2,2,2))-canal
for rect in ((38,-18,2,2),(40,-35,2,2),(24,-36,2,2)):
    low |= cells(rect)
manifest=json.loads((ROOT/'Assets/Resources/Environment/Dungeon/ReworkPilot/Pilot.json').read_text(encoding='utf-8-sig'))
cache={}
def sprite(name):
    a=next(a for a in manifest['assets'] if a['id']==name)
    path=ROOT/'Assets/Resources'/Path(a['resource']+'.png')
    if path not in cache: cache[path]=Image.open(path).convert('RGBA')
    im=cache[path]; x,y,w,h=(a[k] for k in ('x','y','width','height'))
    return im.crop((x,im.height-y-h,x+w,im.height-y))
def font(n): return ImageFont.truetype('C:/Windows/Fonts/arial.ttf',n)
def stone(p): return p not in dry and p not in water and p not in low
def mask(x,y):
    ns=[(0,1),(1,0),(0,-1),(-1,0),(1,1),(1,-1),(-1,-1),(-1,1)]
    m=sum(1<<i for i,(dx,dy) in enumerate(ns) if stone((x+dx,y+dy)))
    for bit,a,b in ((4,0,1),(5,1,2),(6,2,3),(7,3,0)):
        if not(m & 1<<a and m & 1<<b): m &= ~(1<<bit)
    return m
def room_image(i, size=34):
    name,sector,cx,cy,w,h=rooms[i]
    x0,y0=cx-w//2-2,cy-h//2-2
    rw,rh=w+4,h+4
    if i==13: rw+=4
    im=Image.new('RGBA',(rw*size,rh*size),'#202220')
    floor=[sprite(f'CRP_Floor_R17_{j:02d}_00').resize((size,size)) for j in range(4)]
    state='Roots' if sector==2 and i!=17 else 'Damp' if sector==4 else 'Dry'
    for yy in range(rh):
        for xx in range(rw):
            x,y=x0+xx,y0+yy; pos=(xx*size,(rh-1-yy)*size)
            if (x,y) in dry: tile=floor[(x+y)%4]
            elif (x,y) in water: tile=Image.new('RGBA',(size,size),'#304e50')
            elif (x,y) in low: tile=sprite('CRP_LowCoverStone').resize((size,size))
            else: tile=sprite(f'CRP_Wall_{state}_{mask(x,y):02X}').resize((size,size))
            im.alpha_composite(tile,pos)
    d=ImageDraw.Draw(im)
    def point(x,y): return ((x-x0)*size,(y0+rh-y)*size)
    # Existing interaction footprints, reserved in the concept (not new furniture).
    if i in (10,19):
        px,py=point(cx,cy+3/1.06); r=size*.7
        d.rectangle((px-r,py-r,px+r,py+r),fill='#9e9068',outline='#e0d5b3',width=2)
    if i in (14,18):
        px,py=point(cx,cy+4/1.06); r=size*1.45
        if i==18: d.ellipse((px-r,py-r,px+r,py+r),fill='#131916',outline='#a79979',width=6)
        else:
            d.rectangle((px-r,py-r,px+r,py+r),fill='#292c28',outline='#a79979',width=4)
            for k in range(6): d.line((px-r+5,py-r+8+k*size*.4,px+r-5,py-r+8+k*size*.4),fill='#888473',width=3)
    return im.convert('RGB')

names=['Vestibulos','Galerias','Ossarios','Claustro','Cisternas','Prisao','Capela','Contrapesos']
sectors=[]
for s,slug in enumerate(names):
    ids=[i for i,r in enumerate(rooms) if r[1]==s]
    cols=4 if len(ids)==8 else 2 if len(ids)>1 else 1
    rows=(len(ids)+cols-1)//cols
    pw=470 if len(ids)==8 else 650
    ph=660 if len(ids)!=8 else 550
    board=Image.new('RGB',(pw*cols,ph*rows+90),'#191c1a')
    d=ImageDraw.Draw(board)
    d.text((20,15),slug.upper()+' — BASE DE GEOMETRIA',font=font(24),fill='#ddd2b5')
    d.text((20,49),'Vista superior · célula 1,06 u · personagem Ø 0,80 u · volumes e acessos existentes',font=font(16),fill='#a9b0a0')
    for n,i in enumerate(ids):
        x,y=(n%cols)*pw,(n//cols)*ph+90
        img=room_image(i); img.thumbnail((pw-20,ph-68))
        board.paste(img,(x+(pw-img.width)//2,y+45+(ph-68-img.height)//2))
        d.text((x+14,y+10),f'R{i:02d} — {rooms[i][0]}',font=font(20),fill='#e4d9be')
    dest=OUT/f'Base-{slug}.png'; board.save(dest)
    sectors.append({'sector':s,'slug':slug,'rooms':ids,'base':str(dest)})
(OUT/'bases.json').write_text(json.dumps(sectors,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(sectors,ensure_ascii=False))
