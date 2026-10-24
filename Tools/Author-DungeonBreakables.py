"""Author stage-two candidate data from the current, fixed dungeon geometry.

No Unity execution, gameplay simulation, image modification or save access.
Re-running replaces only the generated candidate catalogue, not art sources.
"""
import json
import math
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
layout = (ROOT / 'Assets/Scripts/Match/DungeonLayout.cs').read_text(encoding='utf-8-sig')
content = (ROOT / 'Assets/Scripts/Match/DungeonContent.cs').read_text(encoding='utf-8-sig')
rooms = []
for name, sector, x, y, w, h in re.findall(r'new Room\("([^"]+)",(\d+),(-?\d+),(-?\d+),(\d+),(\d+)\)', layout):
    sector, x, y, w, h = map(int, (sector, x, y, w, h))
    rooms.append(dict(name=name, sector=sector, x=x+64-w//2, y=y+64-h//2, w=w, h=h, cx=x+64, cy=y+64))
floor, blocked, water = set(), set(), set()
def paint(x,y,w,h):
    floor.update((i,j) for j in range(y,y+h) for i in range(x,x+w))
for r in rooms:
    paint(r['x'],r['y'],r['w'],r['h'])
links_text=layout.split('public static readonly int[,] Links=')[1].split('};')[0]
links=[tuple(map(int,m)) for m in re.findall(r'\{(\d+),(\d+)\}',links_text)]
for ia,ib in links:
    a,b=rooms[ia],rooms[ib]
    start,end=(a['cx'],a['cy']),(b['cx'],b['cy'])
    bend=(start[0],end[1]) if ia<8 else (end[0],start[1])
    for p,q in ((start,bend),(bend,end)):
        paint(min(p[0],q[0])-2,min(p[1],q[1])-2,abs(p[0]-q[0])+4,abs(p[1]-q[1])+4)
for r in rooms[8:]:
    for ox,oy in ((-6,-4),(4,2)):
        blocked.update((r['cx']+ox+x,r['cy']+oy+y) for y in range(2) for x in range(2))
r=rooms[12]
water.update((r['cx']+x,r['cy']+y) for y in range(-4,4) for x in range(-2,2))
floor-=blocked|water
def world(x,y): return ((x-64)*1.06,(y-64)*1.06)
def centre(i): return world(rooms[i]['cx'],rooms[i]['cy'])
reserves=[]
reserves.extend((*centre(i),3) for i in range(8))
for r in rooms[8:]:
    sx,sy=world(r['x']+1.5,r['y']+1.5)
    reserves.append((sx,sy,1.1)) # Existing pottery decal in DungeonProps.BuildShards.
exits=[(centre(i)[0],centre(i)[1]+4) for i in (14,18,13)]
reserves.extend((x,y,5) for x,y in exits)
for i,(x,y) in enumerate(exits): reserves.append((x-2.5,y-2,2.8))
reserves.extend([(centre(19)[0],centre(19)[1]+3,3),(centre(10)[0],centre(10)[1]+3,3)])
reserves.append((centre(12)[0]+5*1.06,centre(12)[1]-2,2.8))
for ri,x,y in re.findall(r'new LootSlot\((\d+),(-?\d+),(-?\d+)',content):
    i=int(ri); p=centre(i)
    reserves.append((p[0]+int(x)+(5*1.06 if i==12 else 0),p[1]+int(y),2.8))
for ri,x,y in re.findall(r'new Encounter\((\d+),(-?\d+),(-?\d+)',content):
    p=centre(int(ri)); reserves.append((p[0]+int(x),p[1]+int(y),2))
def inside(r,x,y): return r['x']<=x<r['x']+r['w'] and r['y']<=y<r['y']+r['h']
def door_near(x,y):
    for r in rooms:
        for bx in range(r['x'],r['x']+r['w']):
            for by,dy in ((r['y'],-1),(r['y']+r['h']-1,1)):
                if (bx,by+dy) in floor and abs(x-bx)<=2 and abs(y-by)<=2: return True
        for by in range(r['y'],r['y']+r['h']):
            for bx,dx in ((r['x'],-1),(r['x']+r['w']-1,1)):
                if (bx+dx,by) in floor and abs(x-bx)<=2 and abs(y-by)<=2: return True
    return False
candidates=[]
def safe(x,y):
    if (x,y) not in floor or door_near(x,y): return False
    wx,wy=world(x+.5,y+.5)
    if any(math.hypot(wx-px,wy-py)<rad+.55 for px,py,rad in reserves): return False
    boat=exits[2]
    if boat[0]-3.55<wx<boat[0]+9.55 and boat[1]-2.55<wy<boat[1]+2.55: return False
    return all(math.hypot(wx-p['x'],wy-p['y'])>=2.6 for p in candidates)
def add(x,y,room,group):
    n=len(candidates); wx,wy=world(x+.5,y+.5)
    family=('Urn' if rooms[room]['sector'] in (0,1,2,3,6) else 'Barrel' if rooms[room]['sector']==4 else 'Crate') if room>=0 else ('Crate' if n%2 else 'Urn')
    candidates.append(dict(id=n,room=room,group=group,cellX=x,cellY=y,x=round(wx,3),y=round(wy,3),family=family,variant=1+n%3,angle=90*(n%4)))
for i,r in enumerate(rooms):
    target=1 if i<8 else 3
    choices=[(x,y) for y in range(r['y']+1,r['y']+r['h']-1) for x in range(r['x']+1,r['x']+r['w']-1)
             if min(x-r['x'],r['x']+r['w']-1-x,y-r['y'],r['y']+r['h']-1-y)<=3]
    selected=0
    for k in range(target):
        choices.sort(key=lambda p: (-min((math.hypot(p[0]-(c['cellX']),p[1]-(c['cellY'])) for c in candidates if c['room']==i),default=0),p[1],p[0]))
        p=next((p for p in choices if safe(*p)),None)
        if p is None: raise ValueError(f'No safe authored candidate in room {i}: {r["name"]}')
        add(*p,i,'Entry' if i<8 else 'Room'); selected+=1
for n in range(128*128):
    if len(candidates)==64: break
    idx=(n*7919+431)%(128*128); x,y=idx%128,idx//128
    if any(inside(r,x,y) for r in rooms): continue
    if not any((x+dx,y+dy) not in floor and (x+dx,y+dy) not in water|blocked for dx,dy in ((1,0),(-1,0),(0,1),(0,-1))): continue
    if safe(x,y): add(x,y,-1,'Corridor')
if len(candidates)!=64: raise ValueError(f'Only {len(candidates)} safe positions available')
# Authored exception to the automatic spread: R17 stores its remaining urns as
# one small lot under the southeast ossuary niches. Apply AFTER automatic
# selection so no other room/corridor changes. Keep IDs, rewards and 2-of-3 spawn.
ossuary=[p for p in candidates if p['room']==17 and p['group']=='Room']
for point,(x,y) in zip(ossuary,((27,37),(28,37),(28,38))):
    wx,wy=world(x+.5,y+.5)
    point.update(cellX=x,cellY=y,x=round(wx,3),y=round(wy,3))
out=ROOT/'Assets/Resources/Environment/Dungeon/Breakables'
out.mkdir(parents=True,exist_ok=True)
props=json.loads((ROOT/'Assets/Resources/Environment/Dungeon/Props/PropsArt.json').read_text(encoding='utf-8-sig'))['assets']
art=[]
for variant in range(1,4):
    for state,source in (('Intact','CAT-P05'),('Broken','CAT-P10')):
        entry=next(e for e in props if e['id']==f'{source}_v{variant:02}')
        art.append(dict(family='Urn',variant=variant,state=state,resource='Environment/Dungeon/Props/'+entry['id'],rect={k:entry[k] for k in ('x','y','width','height')}))
    for family in ('Crate','Barrel'):
        for state,x in (('Intact',0),('Broken',.5)):
            art.append(dict(family=family,variant=variant,state=state,resource=f'Environment/Dungeon/Breakables/{family}_v{variant:02}',rect=dict(x=x,y=0,width=.5,height=1)))
data=dict(version=1,cellSize=1.06,worldFootprint=.9,art=art,candidates=candidates)
(out/'BreakablesCatalog.json').write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
review=ROOT/'Art/Catacumbas/Quebraveis'
review.mkdir(parents=True,exist_ok=True)
svg=['<svg xmlns="http://www.w3.org/2000/svg" width="1100" height="1170" viewBox="0 0 136 145">',
     '<rect width="136" height="145" fill="#10140f"/>',
     '<g transform="translate(4 132) scale(1 -1)">']
for x,y in sorted(floor):
    svg.append(f'<rect x="{x}" y="{y}" width="1" height="1" fill="#333b2c"/>')
for i,r in enumerate(rooms):
    svg.append(f'<rect x="{r["x"]}" y="{r["y"]}" width="{r["w"]}" height="{r["h"]}" fill="none" stroke="#6d735b" stroke-width=".14"/>')
for p in candidates:
    color={'Entry':'#dcbb72','Room':'#a8c68b','Corridor':'#7fb0bc'}[p['group']]
    x,y=p['cellX']+.5,p['cellY']+.5
    svg.append(f'<circle cx="{x}" cy="{y}" r=".62" fill="{color}"/><text transform="translate({x+.8} {y}) scale(1 -1)" font-size="1.7" fill="#eee5cc">{p["id"]}</text>')
svg.extend(['</g>','<text x="4" y="139" fill="#eee5cc" font-family="sans-serif" font-size="2.4">64 candidatos: 8 entradas / 48 salas / 8 corredores</text>',
            '<text x="4" y="143" fill="#b6b59c" font-family="sans-serif" font-size="1.8">Autoria geométrica. A composição visual final precisa de avaliação na Unity.</text>','</svg>'])
(review/'Posicoes.svg').write_text('\n'.join(svg),encoding='utf-8')
print(f'Authored {len(candidates)} candidates: 8 entry, 48 room, 8 corridor; 18 art states.')
