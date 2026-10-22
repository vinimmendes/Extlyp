"""Offline geometry survey, not a Unity/playability test. Reads the authored C# layout.
Run with the bundled Python runtime. Writes the area report and schematic only.
"""
from pathlib import Path
import json
import re
import sys
import runpy
from collections import deque
import numpy as np

ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'Assets/Scripts/Match/DungeonLayout.cs').read_text(encoding='utf-8')
rooms=[(name,int(sector),int(x),int(y),int(w),int(h)) for name,sector,x,y,w,h in
       re.findall(r'new Room\("([^"]+)",(\d+),(-?\d+),(-?\d+),(\d+),(\d+)\)',source)]
links=[tuple(map(int,m)) for m in re.findall(r'\{(\d+),(\d+)\}',source.split('Links={',1)[1].split('};',1)[0])]
cell=float(re.search(r'CellSize=([\d.]+)f',source)[1])
step=.25
# Actual Player.prefab: circle radius .5 and root scale .8 => .4 world units.
prefab=(ROOT/'Assets/Prefabs/Player.prefab').read_text(encoding='utf-8')
scale=float(re.search(r'm_LocalScale: \{x: ([\d.]+)',prefab)[1])
radius=float(re.search(r'm_Radius: ([\d.]+)',prefab)[1])*scale
floor=np.zeros((128,128),dtype=bool)
def paint(x,y,w,h): floor[max(0,y):min(128,y+h),max(0,x):min(128,x+w)]=True
for _,_,x,y,w,h in rooms: paint(x-w//2+64,y-h//2+64,w,h)
for a,b in links:
 ax,ay=rooms[a][2:4]; bx,by=rooms[b][2:4]
 mx,my=(ax,by) if a<8 else (bx,ay)
 for x1,y1,x2,y2 in [(ax,ay,mx,my),(mx,my,bx,by)]:
  paint(min(x1,x2)+62,min(y1,y2)+62,abs(x1-x2)+4,abs(y1-y2)+4)
for _,_,x,y,_,_ in rooms[8:]:
 for ox,oy in [(-6,-4),(4,2)]: floor[y+64+oy:y+66+oy,x+64+ox:x+66+ox]=False
canal_open="--canal-open" in sys.argv
# Use the current authored canal masks, not the former 4x8 basin.
# Importing the survey does not relocate candidates or write its reports.
canal_survey=runpy.run_path(str(ROOT/'Tools/Survey-CanalGeometry.py'))
floor[:]=False
for x,y in canal_survey['dry'] | (canal_survey['bridge'] if canal_open else set()):
 if -64<=x<64 and -64<=y<64: floor[y+64,x+64]=True

def component(mask,start):
 seen=np.zeros_like(mask); sy,sx=start
 if not mask[sy,sx]: return seen
 q=deque([(sy,sx)]); seen[sy,sx]=True
 while q:
  y,x=q.popleft()
  for yy,xx in [(y-1,x),(y+1,x),(y,x-1),(y,x+1)]:
   if 0<=yy<mask.shape[0] and 0<=xx<mask.shape[1] and mask[yy,xx] and not seen[yy,xx]:
    seen[yy,xx]=True; q.append((yy,xx))
 return seen

def test_area():
 xs=np.arange(-15+step/2,15,step); ys=np.arange(-23+step/2,23,step)
 xx,yy=np.meshgrid(xs,ys)
 free=(xx>-15+radius)&(xx<15-radius)&(yy>-23+radius)&(yy<23-radius)
 boxes=[(-4,0,.6,8),(4,0,.6,8),(-7,6,2,2),(7,-6,2,2),(-7,14,2,2),(7,-14,2,2)]
 for cy,side in [(18,-1),(-18,1)]:
  boxes += [(0,cy-side*2.5,8.6,.6),(-4,cy,.6,5),(4,cy,.6,5)]
  segment=(8.6-2)/2; offset=(2+segment)/2
  boxes += [(-offset,cy+side*2.5,segment,.6),(offset,cy+side*2.5,segment,.6)]
 for cx,cy,w,h in boxes:
  free &= np.maximum(np.abs(xx-cx)-w/2,0)**2+np.maximum(np.abs(yy-cy)-h/2,0)**2>radius**2
 connected=component(free,(int((18+23)/step),int(15/step)))
 return float(connected.sum()*step*step)

def dungeon_area():
 xs=np.arange(-64*cell+step/2,64*cell,step); ys=xs.copy()
 xx,yy=np.meshgrid(xs,ys)
 ix=np.floor(xx/cell+64).astype(int); iy=np.floor(yy/cell+64).astype(int)
 ix=np.clip(ix,0,127); iy=np.clip(iy,0,127)
 free=floor[iy,ix].copy()
 # Exact circle-vs-cell-box clearance against every neighbouring solid cell.
 for oy in [-1,0,1]:
  for ox in [-1,0,1]:
   nx=ix+ox; ny=iy+oy
   valid=(nx>=0)&(nx<128)&(ny>=0)&(ny<128)
   solid=~valid | ~floor[np.clip(ny,0,127),np.clip(nx,0,127)]
   cx=(nx-64+.5)*cell; cy=(ny-64+.5)*cell
   distance=np.maximum(np.abs(xx-cx)-cell/2,0)**2+np.maximum(np.abs(yy-cy)-cell/2,0)**2
   free &= ~(solid & (distance<=radius**2))
 sx,sy=rooms[0][2:4]
 connected=component(free,(int((sy*cell+64*cell)/step),int((sx*cell+64*cell)/step)))
 return float(connected.sum()*step*step),float(free.sum()*step*step)

baseline=test_area(); area,total=dungeon_area()
report={'method':'Static authored-geometry survey; no Unity, physics, network or playtest executed',
 'grid_step':step,'player_world_radius':radius,'dungeon_cell_size':cell,
 'test_connected_walkable_area':baseline,'dungeon_connected_walkable_area':area,
 'dungeon_all_walkable_area':total,'area_ratio':area/baseline,'rooms':len(rooms),'links':len(links),
 'canal_open':canal_open,'note':'Static floor and body clearance; cistern crossing open.' if canal_open else 'All solid footprints removed, including low cover and cistern water.'}
out=ROOT/'Docs/Mapas'; out.mkdir(parents=True,exist_ok=True)
suffix='_CANAL_ABERTO' if canal_open else ''
(out/('CATACUMBAS_AREA'+suffix+'.json')).write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
palette=['#817555','#998c72','#687859','#a39474','#547e86','#806e80','#a2766b','#96805c']
svg=['<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 920 820">',
 '<rect width="920" height="820" fill="#101216"/>',
 '<g font-family="sans-serif" fill="#eee8dc"><text x="25" y="32" font-size="23">Catacumbas dos Afogados — planta estrutural</text>',
 '<text x="25" y="57" font-size="14">Planta esquemática · 8 entradas · cenário em placeholders</text></g>',
 '<g transform="translate(35 80) scale(5)">']
for y in range(128):
 for x in range(128):
  if floor[y,x]: svg.append(f'<rect x="{x}" y="{127-y}" width="1" height="1" fill="#514d48"/>')
for i,(name,sector,x,y,w,h) in enumerate(rooms):
 svg.append(f'<rect x="{x-w//2+64}" y="{128-(y-h//2+64+h)}" width="{w}" height="{h}" fill="{palette[sector]}" opacity=".25"/>')
 svg.append(f'<text x="{x+64}" y="{128-(y+64)}" fill="white" font-family="sans-serif" font-size="2.7" text-anchor="middle">{i+1}</text>')
for i in range(8):
 x,y=rooms[i][2:4]; svg.append(f'<circle cx="{x+64}" cy="{64-y}" r="1.3" fill="#eee1a5"/>')
svg.append('</g><g font-family="sans-serif" fill="#eee8dc" font-size="11">')
for i,(name,sector,*_) in enumerate(rooms): svg.append(f'<text x="700" y="{98+i*25}">{i+1}. {name}</text>')
svg.append(f'<text x="35" y="765" font-size="15">Área caminhável: {area:.2f} u² / teste: {baseline:.2f} u² = {area/baseline:.3f}x</text>')
svg.append('</g></svg>')
(out/('CATACUMBAS_PLANTA'+suffix+'.svg')).write_text('\n'.join(svg),encoding='utf-8')
print(json.dumps(report,ensure_ascii=False))
