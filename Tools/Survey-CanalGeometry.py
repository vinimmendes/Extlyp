"""Offline source survey and breakable authoring; no Unity or gameplay tests.

Produces a schematic and path-length estimates from authored cell rectangles.
--relocate-breakables updates only candidates affected by the new shoreline.
"""
from pathlib import Path
from collections import deque
import json
import math
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
source = (ROOT/'Assets/Scripts/Match/DungeonLayout.cs').read_text(encoding='utf-8-sig')
canal = (ROOT/'Assets/Scripts/Match/DungeonLayout.Canal.cs').read_text(encoding='utf-8-sig')
rooms = [(name, *map(int, values)) for name, *values in re.findall(r'new Room\("([^"]+)",(\d+),(-?\d+),(-?\d+),(\d+),(\d+)\)', source)]
links = [tuple(map(int, p)) for p in re.findall(r'\{(\d+),(\d+)\}',source.split('Links={')[1].split('};')[0])]
cell = float(re.search(r'CellSize=([\d.]+)f',source)[1])
dry, water = set(), set()
rects = {name:tuple(map(int, args.split(','))) for name,args in re.findall(r'RectInt (\w+)=CanalCells\(([^)]+)\)',canal)}
def cells(rect):
    x,y,w,h = rect
    return {(xx,yy) for yy in range(y,y+h) for xx in range(x,x+w)}
def room_rect(room):
    _,_,x,y,w,h = room
    return x-w//2,y-h//2,w,h
for room in rooms: dry |= cells(room_rect(room))
for a,b in links:
    ax,ay = rooms[a][2:4]; bx,by = rooms[b][2:4]
    mx,my = (ax,by) if a<8 else (bx,ay)
    for x1,y1,x2,y2 in [(ax,ay,mx,my),(mx,my,bx,by)]:
        dry |= cells((min(x1,x2)-2,min(y1,y2)-2,abs(x1-x2)+4,abs(y1-y2)+4))
for _,_,x,y,_,_ in rooms[8:]:
    dry -= cells((x-6,y-4,2,2)) | cells((x+4,y+2,2,2))
dry -= cells(rects['CanalArea'])
dry |= cells(rects['CanalDetour']) | cells(room_rect(rooms[13]))
for args in re.findall(r'Paint\(CanalCells\(([^)]+)\)',canal): dry |= cells(tuple(map(int,args.split(','))))
water = cells(rects['CanalEastRiver']) | cells(rects['CanalSouthRiver'])
dry -= water
dry |= cells(rects['CanalBoarding'])
for args in re.findall(r'AddObstacle\(CanalCells\(([^)]+)\)',canal): dry -= cells(tuple(map(int,args.split(','))))
bridge = cells(rects['CanalBridge'])
start = tuple(map(int,re.search(r'CanalControl=CanalPoint\(([^)]+)\)',canal)[1].split(',')))
goal = tuple(map(int,re.search(r'ExitPositions=.*CanalPoint\(([^)]+)\)',source)[1].split(',')))
def path(mask,a,b):
    q=deque([a]); previous={a:None}
    if a not in mask or b not in mask: return []
    while q:
        p=q.popleft()
        if p==b:
            result=[]
            while p is not None: result.append(p); p=previous[p]
            return result[::-1]
        for n in [(p[0]+1,p[1]),(p[0]-1,p[1]),(p[0],p[1]+1),(p[0],p[1]-1)]:
            if n in mask and n not in previous: previous[n]=p; q.append(n)
    return []
def main():
    closed = path(dry,start,goal); opened = path(dry|bridge,start,goal)
    report = {'kind':'offline authored-cell survey, not Unity validation', 'cellSize':cell,
        'closedPathUnits':round(max(0,len(closed)-1)*cell,2), 'openPathUnits':round(max(0,len(opened)-1)*cell,2),
        'reductionPercent':round(100*(1-(len(opened)-1)/(len(closed)-1)),1) if len(closed)>1 and opened else None,
        'entriesReachDockClosed':[bool(path(dry,tuple(r[2:4]),goal)) for r in rooms[:8]],
        'bridgeCells':len(bridge), 'permanentWaterCells':len(water-bridge-cells(rects['CanalBoarding']))}
    
    catalog_path = ROOT/'Assets/Resources/Environment/Dungeon/Breakables/BreakablesCatalog.json'
    catalog=json.loads(catalog_path.read_text(encoding='utf-8-sig'))
    points=catalog['candidates']; moved=[]; occupied=set()
    protected=[start,goal]+[tuple(r[2:4]) for r in rooms[:8]]
    def safe(p):
        return p in dry-water and all(math.dist((p[0]+.5,p[1]+.5),q)>3 for q in protected) and all(math.dist(p,q)>1.5 for q in occupied)
    for point in points:
        old=(point['cellX']-64,point['cellY']-64); target=old
        # Retain unchanged candidates first; the moved entries/room get their previous offset.
        if point['room']>=0 and old not in cells(room_rect(rooms[point['room']])):
            if point['room']==6: target=(old[0]-24,old[1]-4)
            elif point['room']==7: target=(old[0],old[1]+24)
            elif point['room']==13: target=(old[0]+16,old[1]-24)
        if '--relocate-breakables' not in sys.argv:
            occupied.add(old); continue
        if target!=old or old not in dry-water:
            available=cells(room_rect(rooms[point['room']])) if point['room']>=0 else dry
            available=[p for p in available if safe(p)]
            if not available: raise ValueError('No dry candidate position for '+str(point['id']))
            target=min(available,key=lambda p:(math.dist(p,target),p))
            point['cellX'],point['cellY']=target[0]+64,target[1]+64
            point['x'],point['y']=round((target[0]+.5)*cell,5),round((target[1]+.5)*cell,5)
            moved.append(point['id'])
        occupied.add(target)
    if '--relocate-breakables' in sys.argv:
        catalog_path.write_text(json.dumps(catalog,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    report['relocatedCandidateIds']=moved
    report['invalidDryCandidates']=[p['id'] for p in points if (p['cellX']-64,p['cellY']-64) not in dry-water]
    out=ROOT/'Docs/Mapas/Canal'; out.mkdir(parents=True,exist_ok=True)
    (out/'levantamento.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    svg=['<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 768 820">','<rect width="768" height="820" fill="#151813"/>',
         '<text x="20" y="26" fill="#e6d9ba" font-family="sans-serif" font-size="18">Canal dos Defuntos — planta esquemática</text>',
         '<g transform="translate(0 40) scale(6)">']
    for points_to_draw,color in [(dry,'#55564a'),(water,'#193e51'),(bridge,'#a67b43')]:
        for x,y in sorted(points_to_draw): svg.append(f'<rect x="{x+64}" y="{63-y}" width="1" height="1" fill="{color}"/>')
    for route,color in [(closed,'#e1b990'),(opened,'#a9cd9d')]:
        if route: svg.append('<polyline fill="none" stroke="'+color+'" stroke-width=".45" points="'+' '.join(f'{x+64.5},{63.5-y}' for x,y in route)+'"/>')
    svg.append('</g><text x="20" y="800" fill="#e6d9ba" font-family="sans-serif" font-size="13">Bege: desvio fechado · verde: passagem aberta · sem simulação de colisão Unity</text></svg>')
    (out/'planta.svg').write_text('\n'.join(svg),encoding='utf-8')
    print(json.dumps(report,ensure_ascii=False))

if __name__ == '__main__':
    main()
