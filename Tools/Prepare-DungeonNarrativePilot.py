"""Author pilot manifest, texture metadata and static placement report; no Unity.

Reads images to inspect alpha bounds; never repaints or resamples the generated art.
"""
from pathlib import Path
import json, math, runpy, uuid
from PIL import Image

ROOT=Path(__file__).resolve().parents[1]
folder=ROOT/'Assets/Resources/Environment/Dungeon/Narrative'
geo=runpy.run_path(str(ROOT/'Tools/Survey-CanalGeometry.py'))
plan=json.loads((ROOT/'Docs/Mapas/DecoracaoNarrativa/distribuicao.json').read_text(encoding='utf-8-sig'))
assets=[]
for id,version in [('N01',1),('N02',1),('N03',1),('N08',1),('N09',2)]:
    name=f'CAT-{id}_v{version:02}'
    path=folder/(name+'.png')
    with Image.open(path) as im:
        if im.mode!='RGBA': raise ValueError(f'{name}: alpha channel missing')
        alpha=im.getchannel('A'); box=alpha.getbbox()
        if not box or alpha.getextrema()[0]!=0: raise ValueError(f'{name}: transparency missing')
        w,h=im.size; l,t,r,b=box
        assets.append(dict(id=id,resource='Environment/Dungeon/Narrative/'+name,
                           x=l/w,y=(h-b)/h,width=(r-l)/w,height=(b-t)/h,
                           sourcePixels=[w,h],alphaBoundsPixels=list(box)))
    meta=path.with_suffix('.png.meta')
    if not meta.exists():
        template=(ROOT/'Assets/Resources/Environment/Dungeon/Props/CAT-P02_v01.png.meta').read_text(encoding='utf-8-sig')
        import re
        template=re.sub(r'guid: \w+', 'guid: '+uuid.uuid4().hex,template,count=1)
        template=template.replace('maxTextureSize: 512','maxTextureSize: 2048')
        meta.write_text(template,encoding='utf-8')

# Import previous family crop rectangles; no duplicate texture files.
for group,ids in [('Props',['CAT-P02_v01','CAT-P10_v01']),('Details',['CAT-D10_v01','CAT-D08_v01'])]:
    source=json.loads((ROOT/f'Assets/Resources/Environment/Dungeon/{group}/{"PropsArt" if group=="Props" else "DetailsArt"}.json').read_text(encoding='utf-8-sig'))
    for id in ids:
        entry=next(a for a in source['assets'] if a['id']==id)
        assets.append(dict(entry,resource=f'Environment/Dungeon/{group}/{id}'))

def piece(id,room,art,x,y,w,h,support='Floor',angle=0,opacity=1,order=-7):
    return dict(id=id,room=room,art=art,x=x,y=y,width=w,height=h,
                support=support,angle=angle,opacity=opacity,order=order)
pieces=[
 piece('R08-shroud',8,'N01',-29,39,1.7,.8,'Cover',order=-2),
 piece('R08-displaced-lid',8,'CAT-P02_v01',-35,46,1.6,1.6,'Stone',order=-3),
 piece('R08-shards',8,'CAT-P10_v01',-31.4,45.5,.9,.8,opacity=.85),
 piece('R19-unfinished-coffin',19,'N02',-36.5,32.6,3,1,'Stone',order=-3),
 piece('R19-tools',19,'N03',-34,30.8,2,.85,opacity=.95),
 piece('R19-chain',19,'CAT-D10_v01',-31,31,.7,.7,opacity=.8),
 piece('R13-bank-silt',13,'N09',52.8,-43.8,2.7,1,angle=90,opacity=.5),
 piece('R13-bank-timber',13,'N08',52.8,-43.8,2.2,.6,angle=90,opacity=.9,order=-6),
 piece('R13-west-silt',13,'N09',36,-54.4,3,1,opacity=.42),
 piece('R13-damp',13,'CAT-D08_v01',39,-54.5,1.4,.8,opacity=.45),
 piece('R13-shards',13,'CAT-P10_v01',38,-51.5,.8,.8,opacity=.8),
]
# Allow one explicit layering pair (timber over sediment); all others stand alone.
for p in pieces: p['layerGroup']='R13-bank' if p['id'].startswith('R13-bank-') else p['id']
manifest=dict(version=1,assets=assets,pieces=pieces)
(folder/'NarrativePilot.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')

def intersect(a,b): return a[0]<b[2] and a[2]>b[0] and a[1]<b[3] and a[3]>b[1]
def door_approach(x,y):
    for room in geo['rooms']:
        rx,ry,rw,rh=geo['room_rect'](room)
        if x<rx-2 or x>rx+rw+1 or y<ry-2 or y>ry+rh+1: continue
        for dx,dy in [(0,1),(1,0),(0,-1),(-1,0)]:
            for i in [-1,0,1]:
                bx=x+i if dx==0 else rx+rw-1 if dx>0 else rx
                by=y+i if dy==0 else ry+rh-1 if dy>0 else ry
                if not (rx<=bx<rx+rw and ry<=by<ry+rh): continue
                if abs(x-bx)>2 or abs(y-by)>2: continue
                if (bx,by) in geo['dry'] and (bx+dx,by+dy) in geo['dry']: return True
    return False
report=[]
for p in pieces:
    vertical=abs(p['angle'])%180==90
    w,h=(p['height'],p['width']) if vertical else (p['width'],p['height'])
    rect=(p['x']-w/2,p['y']-h/2,p['x']+w/2,p['y']+h/2)
    cells={(x,y) for x in range(math.floor(rect[0]+.0001),math.ceil(rect[2]-.0001)) for y in range(math.floor(rect[1]+.0001),math.ceil(rect[3]-.0001))}
    errors=[]
    if cells & geo['water']: errors.append('water')
    if p['support']=='Floor' and not cells<=geo['dry']: errors.append('not entirely dry floor')
    if p['support']=='Floor' and any(door_approach(x,y) for x,y in cells): errors.append('door approach')
    if p['support']=='Stone' and cells & geo['dry']: errors.append('stone intersects floor')
    if p['support']=='Cover':
        cover=set()
        canal_cells=geo['cells'](geo['rects']['CanalArea'])
        for ri,room in enumerate(geo['rooms']):
            if ri<8 or ri%3==0: continue
            cx,cy=room[2:4]
            footprint=geo['cells']((cx-6,cy-4,2,2))
            if not footprint & canal_cells: cover |= footprint
        if not cells<=cover: errors.append('not entirely on existing low cover')
    for point in plan['protectedPoints']:
        x,y=[v/geo['cell'] for v in point['worldPosition']]
        dx=max(rect[0]-x,0,x-rect[2]); dy=max(rect[1]-y,0,y-rect[3])
        if math.hypot(dx,dy)*geo['cell']<point['radiusUnits']: errors.append(point['id'])
    for b in plan['breakableReservations']['candidates']:
        x,y=b['x']/geo['cell'],b['y']/geo['cell']; r=.55/geo['cell']
        if intersect(rect,(x-r,y-r,x+r,y+r)): errors.append('breakable '+str(b['id']))
    report.append(dict(id=p['id'],boundsRelativeCells=rect,sourceGeometryConflicts=errors,
                       pending='Native sprite bounds and existing art checked by runtime placement guards; user visual review pending.'))
(ROOT/'Docs/Mapas/DecoracaoNarrativa/pilotos-levantamento.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps(report,ensure_ascii=False))
if any(r['sourceGeometryConflicts'] for r in report): raise ValueError('Pilot anchors need adjustment; see report')
