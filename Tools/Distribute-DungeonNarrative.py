"""Author fixed cosmetic compositions from the current geometry and reservations.

No Unity or gameplay tests. Alternatives are baked into JSON, never randomly
chosen per client. Native sprite bounds remain subject to runtime guards.
"""
from pathlib import Path
import copy
import html
import json
import math
import runpy
import uuid

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Docs/Mapas/DecoracaoNarrativa'
RES=ROOT/'Assets/Resources/Environment/Dungeon'
geo=runpy.run_path(str(ROOT/'Tools/Survey-CanalGeometry.py'))
rooms=geo['rooms']; dry=geo['dry']; water=geo['water']; cell=geo['cell']
plan=json.loads((OUT/'distribuicao.json').read_text(encoding='utf-8-sig'))
pilot=json.loads((RES/'Narrative/NarrativePilot.json').read_text(encoding='utf-8-sig'))
catalog=json.loads((RES/'Narrative/NarrativeCatalog.json').read_text(encoding='utf-8-sig'))
assets={a['id']:a for a in catalog['assets']}
for a in pilot['assets']: assets.setdefault(a['id'],a)
for directory,manifest,ids in [
 ('Details','DetailsArt',['CAT-D02_v01','CAT-D06_v01','CAT-D07_v01','CAT-D12_v01']),
 ('Props','PropsArt',['CAT-P03_v01','CAT-P04_v01','CAT-P06_v01','CAT-P07_v01'])]:
    source=json.loads((RES/directory/(manifest+'.json')).read_text(encoding='utf-8-sig'))
    for identifier in ids:
        a=next(a for a in source['assets'] if a['id']==identifier)
        assets[identifier]=dict(a,resource=f'Environment/Dungeon/{directory}/{identifier}')

def overlaps(a,b): return a[0]<b[2] and a[2]>b[0] and a[1]<b[3] and a[3]>b[1]
def as_bounds(r): x,y,w,h=r; return x,y,x+w,y+h
canal=as_bounds(geo['rects']['CanalArea']); detour=as_bounds(geo['rects']['CanalDetour'])
room_bounds=[as_bounds(geo['room_rect'](r)) for r in rooms]
room_cells=set().union(*(geo['cells'](geo['room_rect'](r)) for r in rooms))
covers=set()
for i,r in enumerate(rooms):
    if i<8 or i%3==0: continue
    cells=geo['cells']((r[2]-6,r[3]-4,2,2))
    if not cells & geo['cells'](geo['rects']['CanalArea']): covers |= cells

def bounds(p):
    w,h=(p['height'],p['width']) if p['angle']%180==90 else (p['width'],p['height'])
    return p['x']-w/2,p['y']-h/2,p['x']+w/2,p['y']+h/2
def cells_of(b):
    return {(x,y) for x in range(math.floor(b[0]+.0001),math.ceil(b[2]-.0001))
                  for y in range(math.floor(b[1]+.0001),math.ceil(b[3]-.0001))}
def near(b,point,radius):
    x,y=[v/cell for v in point]
    return math.hypot(max(b[0]-x,0,x-b[2]),max(b[1]-y,0,y-b[3]))*cell<radius
def contains(outer,inner): return outer[0]<=inner[0] and outer[1]<=inner[1] and outer[2]>=inner[2] and outer[3]>=inner[3]
def doorway(x,y):
    for rx,ry,xmax,ymax in room_bounds:
        if x<rx-2 or x>xmax+1 or y<ry-2 or y>ymax+1: continue
        for dx,dy in [(0,1),(1,0),(0,-1),(-1,0)]:
            for i in [-1,0,1]:
                bx=x+i if dx==0 else xmax-1 if dx>0 else rx
                by=y+i if dy==0 else ymax-1 if dy>0 else ry
                if not (rx<=bx<xmax and ry<=by<ymax) or abs(x-bx)>2 or abs(y-by)>2: continue
                if (bx,by) in dry and (bx+dx,by+dy) in dry: return True
    return False
door_cells={p for p in dry if doorway(*p)}

def safe(p):
    b=bounds(p); cells=cells_of(b)
    if any(not(-64<=x<64 and -64<=y<64) for x,y in cells) or cells & water: return False
    support=p['support']
    if support=='Floor' and (not cells<=dry or cells & door_cells): return False
    if support=='Stone' and cells & (dry|covers): return False
    if support=='Cover' and not cells<=covers: return False
    if p['room']>=0 and support!='Stone' and not contains(room_bounds[p['room']],b): return False
    if p['room']==-1 and cells & room_cells: return False
    if any(near(b,q['worldPosition'],q['radiusUnits']) for q in plan['protectedPoints']): return False
    for q in plan['breakableReservations']['candidates']:
        x,y=q['x']/cell,q['y']/cell; r=.55/cell
        if overlaps(b,(x-r,y-r,x+r,y+r)): return False
    if overlaps(b,canal) or overlaps(b,detour):
        if p['room']==13 and support=='Floor' and contains(room_bounds[13],b): pass
        elif p.get('zone')=='CanalDetour' and support=='Floor' and contains(detour,b): pass
        else: return False
    bx,by=56,-50
    if overlaps(b,(bx-1.5,by-16-2.5,bx+1.5,by+2.5)): return False
    return True

# Every floor recipe contains low-profile objects; tall silhouettes are separate
# Stone groups. Only sediment and its timber explicitly share a layerGroup.
recipes={
 'memorial':[('CAT-D07_v01',0,0,.8,.8,.85),('CAT-P10_v01',1.1,0,.65,.65,.8),('CAT-D12_v01',.3,-1.1,1,.6,.5)],
 'sepultura':[('N01',0,0,2,1,.85),('CAT-P10_v01',1.5,-.4,.7,.7,.8)],
 'ossario':[('CAT-D02_v01',0,0,1.8,.8,.6),('CAT-D06_v01',1.3,.6,.65,.65,.65),('CAT-D12_v01',-.5,1,.7,.6,.45)],
 'ritual':[('N05',0,0,.85,.85,.9),('CAT-D07_v01',1.1,0,.7,.7,.8),('CAT-D12_v01',.4,-1.05,1,.65,.5)],
 'cela':[('N07',0,0,1.8,.8,.4),('N06',1.35,.4,.8,.8,.85),('CAT-D10_v01',-.6,-1.05,.7,.7,.75)],
 'coveiros':[('N03',0,0,1.8,.85,.95),('CAT-D10_v01',1.3,.7,.7,.7,.8),('CAT-D12_v01',-.5,-1,.8,.6,.5)],
 'enchente':[('N09',0,0,2.4,.9,.45),('N08',0,0,1.8,.6,.85),('CAT-D08_v01',1.65,.6,.7,.7,.4)],
 'expedicao':[('N11',0,0,1.8,1.8,.65),('N10',1.5,0,.8,.8,.75)],
 'colapso':[('CAT-D12_v01',0,0,1.4,.8,.5),('CAT-P10_v01',1.2,.3,.7,.7,.75)]}
groups=[]; selected=[]; omitted=[]
def group_conflict(pieces,other):
    for p in pieces:
        for q in other:
            # Slight separation between compositions, without inflating overlay support.
            b=bounds(p); b=(b[0]-.15,b[1]-.15,b[2]+.15,b[3]+.15)
            if overlaps(b,bounds(q)): return True
    return False
def add_group(identifier,room,family,candidates):
    alternatives=[]; preferred=None
    for pieces in candidates:
        if not all(safe(p) for p in pieces) or group_conflict(pieces,selected): continue
        if any(overlaps(bounds(p),bounds(q)) and p['layerGroup']!=q['layerGroup']
               for i,p in enumerate(pieces) for q in pieces[i+1:]): continue
        if any(math.dist((pieces[0]['x'],pieces[0]['y']),
                        (a['pieces'][0]['x'],a['pieces'][0]['y']))<1.5 for a in alternatives): continue
        alternatives.append({'pieces':pieces})
        if preferred is None: preferred=pieces
        if len(alternatives)==6: break
    if alternatives:
        groups.append(dict(id=identifier,room=room,family=family,alternatives=alternatives))
        selected.extend(preferred)
    else: omitted.append(dict(id=identifier,room=room,family=family,reason='Sem envelope livre na geometria e reservas; não forçar densidade.'))

def build(identifier,room,recipe,x,y,angle=0,zone=''):
    result=[]; rad=math.radians(angle)
    for i,(art,dx,dy,w,h,opacity) in enumerate(recipe):
        result.append(dict(id=f'{identifier}-{i}',room=room,art=art,x=round(x+dx*math.cos(rad)-dy*math.sin(rad),4),
            y=round(y+dx*math.sin(rad)+dy*math.cos(rad),4),width=w,height=h,angle=angle,
            support='Floor',opacity=opacity,order=-6 if art=='N08' else -7,
            layerGroup=identifier+'-silt' if art in ['N08','N09'] else f'{identifier}-{i}',zone=zone))
    return result

# Preserve all fixed pilot anchors before searching additional room envelopes.
for room in [8,19,13]:
    pieces=[copy.deepcopy(p) for p in pilot['pieces'] if p['room']==room]
    if not all(safe(p) for p in pieces): raise ValueError('Piloto incompatível: '+str(room))
    groups.append(dict(id=f'R{room:02}-pilot',room=room,family=plan['rooms'][room]['primary'],alternatives=[dict(pieces=pieces)]))
    selected.extend(pieces)

for r in plan['rooms']:
    ri=r['roomIndex']; rx,ry,xmax,ymax=room_bounds[ri]; family=r['primary']
    if ri not in [8,13,19]:
        entrance={
            'memorial':[('CAT-D07_v01',0,0,.6,.6,.8),('CAT-D12_v01',.8,.1,.6,.5,.45)],
            'sepultura':[('N01',0,0,1.1,.5,.8),('CAT-P10_v01',.95,.1,.5,.5,.7)],
            'ossario':[('CAT-D02_v01',0,0,1,.5,.5),('CAT-D06_v01',.9,.1,.45,.45,.6)],
            'ritual':[('CAT-D07_v01',0,0,.5,.5,.8),('N05',.75,.1,.45,.45,.85)],
            'cela':[('N07',0,0,.85,.4,.4),('N06',.85,.1,.5,.5,.8)],
            'enchente':[('N09',0,0,.95,.4,.4),('CAT-P10_v01',.85,.1,.45,.45,.65)]}
        recipe=recipes[family] if ri>=8 else entrance[family]
        positions=[]
        # A broad peripheral band, leaving the central combat space quiet.
        for y in range(ry,ymax):
            for x in range(rx,xmax):
                if min(x-rx,xmax-1-x,y-ry,ymax-1-y)>3: continue
                positions.append((x+.5,y+.5))
        # Side preference from the approved survey; deterministic order.
        positions.sort(key=lambda p:(abs(p[0]-(rx+1.5 if r['searchZone']['side']=='oeste' else xmax-1.5)),p[1]))
        candidates=(build(f'R{ri:02}-main',ri,recipe,x,y,a) for x,y in positions for a in [0,90,180,270])
        add_group(f'R{ri:02}-main',ri,family,candidates)
    # Add one contextual wall composition where there is already solid masonry.
    if ri>=8 and ri not in [8,13]:
        art,w,h=({'memorial':('CAT-P03_v01',.9,.9),'sepultura':('N02',2.8,.9),
                  'ossario':('CAT-P04_v01',1.8,.7),'ritual':('CAT-P06_v01',2,.8),
                  'cela':('CAT-P06_v01',2,.8),'coveiros':('N04',2.8,.9)}).get(family,('CAT-P03_v01',.8,.8))
        candidates=[]
        for side in ([1,2,3,0] if ri==19 else range(4)):
            for v in range((rx if side<2 else ry)+1,(xmax if side<2 else ymax)-1):
                x,y=(v+.5,ymax+.6 if side==0 else ry-.6) if side<2 else (rx-.6 if side==2 else xmax+.6,v+.5)
                p=dict(id=f'R{ri:02}-wall',room=ri,art=art,x=x,y=y,width=w,height=h,
                       angle=0 if side<2 else 90,support='Stone',opacity=.95,order=-3,layerGroup=f'R{ri:02}-wall',zone='')
                # Keep every supporting volume adjacent to open floor of its room.
                ix,iy=(int(v),ymax-1 if side==0 else ry) if side<2 else (rx if side==2 else xmax-1,int(v))
                if (ix,iy) in dry: candidates.append([p])
        add_group(f'R{ri:02}-wall',ri,family,candidates)
    secondary=r.get('secondaryOptional')
    if secondary:
        positions=[(x+.5,y+.5) for y in range(ry+2,ymax-2) for x in range(rx+2,xmax-2)
                   if min(x-rx,xmax-1-x,y-ry,ymax-1-y)<=4]
        add_group(f'R{ri:02}-secondary',ri,secondary,
                  (build(f'R{ri:02}-secondary',ri,recipes[secondary],x,y,a)
                   for x,y in positions for a in [0,90,180,270]))

# Corridor intersections are reserved even where separate links share the same
# physical corridor. Room interiors are always excluded from these groups.
segments=[]
for c in plan['corridors'][:len(geo['links'])]:
    pts=c['relativeCellPolyline']
    for a,b in zip(pts,pts[1:]):
        if a!=b: segments.append((tuple(a),tuple(b)))
junctions=set()
for a,b in segments:
    if a[1]!=b[1]: continue
    for c,d in segments:
        if c[0]!=d[0]: continue
        if min(a[0],b[0])<=c[0]<=max(a[0],b[0]) and min(c[1],d[1])<=a[1]<=max(c[1],d[1]):
            if (c[0],a[1]) not in room_cells: junctions.add((c[0],a[1]))
for c in plan['corridors'][:len(geo['links'])]:
    recipe=[('CAT-P10_v01',0,0,.6,.6,.7),('CAT-D12_v01',.8,0,.65,.5,.45)]
    if c['family']=='ossario': recipe[0]=('CAT-D02_v01',0,0,1.1,.5,.5)
    elif c['family']=='cela': recipe[0]=('N07',0,0,1.1,.5,.35)
    elif c['family']=='ritual': recipe[0]=('CAT-D07_v01',0,0,.55,.55,.75)
    if recipe[0][3]>1:
        a,_,dy,w,h,opacity=recipe[1]; recipe[1]=(a,1.1,dy,w,h,opacity)
    pts=c['relativeCellPolyline']; options=[]
    for a,b in zip(pts,pts[1:]):
        vertical=a[0]==b[0]
        for v in range(min(a[1],b[1]) if vertical else min(a[0],b[0]),max(a[1],b[1]) if vertical else max(a[0],b[0])):
            for side in [-1,1]:
                x,y=(a[0]+side*1.45,v+.5) if vertical else (v+.5,a[1]+side*1.45)
                if any(math.dist((x,y),j)<3 for j in junctions): continue
                edge=(math.floor(x)+side,math.floor(y)) if vertical else (math.floor(x),math.floor(y)+side)
                if edge in dry or edge in water: continue
                options.append((x,y,90 if vertical else 0))
    for n in range(c['targetGroups']):
        add_group(c['id']+f'-{n}',-1,c['family'],
                  (build(c['id']+f'-{n}',-1,recipe,x,y,a) for x,y,a in options))
for n,y in enumerate([-32.5,-26.5]):
    add_group(f'CA01-{n}',-1,'enchente',
        (build(f'CA01-{n}',-1,[('N09',0,0,1.4,.6,.4),('CAT-P10_v01',1,0,.5,.5,.65)],
               x,yy,90,'CanalDetour') for yy in [y,y+2,y-2] for x in [24.7,29.3]))

for group in groups:
    for alternative in group['alternatives']:
        if not all(safe(p) for p in alternative['pieces']): raise ValueError('Reserva inválida: '+group['id'])
        for p in alternative['pieces']:
            if p['art'] not in assets: raise ValueError('Arte não catalogada: '+p['art'])
if len({g['id'] for g in groups})!=len(groups): raise ValueError('Grupo repetido')
for asset in assets.values():
    if not (ROOT/'Assets/Resources'/(asset['resource']+'.png')).is_file():
        raise ValueError('Textura ausente: '+asset['resource'])
manifest=dict(version=1,assets=list(assets.values()),groups=groups)
(RES/'Narrative/NarrativeDistribution.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
meta=RES/'Narrative/NarrativeDistribution.json.meta'
if not meta.exists(): meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nTextScriptImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n',encoding='utf-8')
report=dict(kind='offline authored composition survey; not Unity validation',groupCount=len(groups),
            preferredPieces=len(selected),roomsWithGroups=sorted({g['room'] for g in groups if g['room']>=0}),
            corridorGroups=sum(g['room']==-1 for g in groups),omitted=omitted,
            status='Native renderer bounds may reject an alternative; all alternatives are fixed and statically screened.',
            groups=[dict(id=g['id'],room=g['room'],family=g['family'],alternatives=len(g['alternatives']),
                         preferred=g['alternatives'][0]['pieces']) for g in groups])
(OUT/'distribuicao-etapa4-levantamento.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
svg=['<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 840 880"><rect width="840" height="880" fill="#161a18"/>',
     '<text x="20" y="28" fill="#ece1c6" font-family="sans-serif" font-size="20">Decoração — composições autoradas (etapa 4)</text>',
     '<g transform="translate(36 50) scale(6)">']
for mask,color in [(dry,'#50574f'),(water,'#274857')]:
    for x,y in sorted(mask): svg.append(f'<rect x="{x+64}" y="{63-y}" width="1" height="1" fill="{color}"/>')
for p in selected:
    x0,y0,x1,y1=bounds(p)
    color='#d2a364' if p['support']=='Stone' else '#a6c585'
    svg.append(f'<rect x="{x0+64}" y="{64-y1}" width="{x1-x0}" height="{y1-y0}" fill="{color}" fill-opacity=".9"><title>{html.escape(p["id"])}</title></rect>')
for i,r in enumerate(rooms): svg.append(f'<text x="{r[2]+64}" y="{64-r[3]}" fill="#fff2d3" text-anchor="middle" font-family="sans-serif" font-size="1.6">R{i:02}</text>')
svg+=['</g><g fill="#e3d9c3" font-family="sans-serif" font-size="13"><text x="20" y="846">Verde: piso/cobertura · cobre: alvenaria. Envelopes preferenciais; não é captura do jogo.</text><text x="20" y="866">Reservas conferidas na fonte; encaixe final depende dos bounds da arte existente.</text></g></svg>']
(OUT/'planta-etapa4.svg').write_text('\n'.join(svg),encoding='utf-8')
print(json.dumps({k:report[k] for k in ['groupCount','preferredPieces','roomsWithGroups','corridorGroups','omitted']},ensure_ascii=False))
