"""Authored room recipes and static comparisons from real packed sprites, not Unity.
Uses only technical crop/scale/rotation/composition of painted source assets.
"""
from pathlib import Path
import json
import math
import runpy
from PIL import Image,ImageDraw,ImageFont,ImageChops

ROOT=Path(__file__).resolve().parents[1]
ART=ROOT/'Art/Catacumbas/ReworkCompleto'
OUT=ROOT/'Assets/Resources/Environment/Dungeon/ReworkComplete'
survey=runpy.run_path(str(ROOT/'Tools/Survey-CanalGeometry.py'))
rooms,dry,water=(survey[k] for k in ('rooms','dry','water'))
catalog=json.loads((OUT/'CatacombArt.catacombkit').read_text(encoding='utf-8'))['entries']
entries={e['id']:e for e in catalog}; texture_cache={}; sprites={}
def sprite(id):
    if id not in sprites:
        e=entries[id]; path=ROOT/e['texture']
        if path not in texture_cache: texture_cache[path]=Image.open(path).convert('RGBA')
        im=texture_cache[path]; x,y,w,h=(e[k] for k in ('x','y','width','height'))
        sprites[id]=im.crop((x,im.height-y-h,x+w,im.height-y))
    return sprites[id].copy()
low=set(); cells=survey['cells']; canal=cells(survey['rects']['CanalArea'])
for i,(_,_,cx,cy,_,_) in enumerate(rooms):
    if i<8: continue
    if i%3: low |= cells((cx-6,cy-4,2,2))-canal
    low |= cells((cx+4,cy+2,2,2))-canal
for rect in ((38,-18,2,2),(40,-35,2,2),(24,-36,2,2)): low |= cells(rect)
def solid(x,y): return (x,y) not in dry and (x,y) not in water and (x,y) not in low
def mask(x,y):
    ns=((0,1),(1,0),(0,-1),(-1,0),(1,1),(1,-1),(-1,-1),(-1,1))
    m=sum(1<<i for i,(dx,dy) in enumerate(ns) if solid(x+dx,y+dy))
    for bit,a,b in ((4,0,1),(5,1,2),(6,2,3),(7,3,0)):
        if not(m & 1<<a and m & 1<<b): m &= ~(1<<bit)
    return m
pieces=[]; surfaces=[]
def piece(room,id,asset,cx,cy,width=None,height=None,angle=0,order=-3,group=''):
    e=entries[asset]
    pieces.append(dict(id=f'R{room:02d}_{id}',asset=asset,room=room,group=group or f'R{room:02d}',
        position=dict(x=round(cx*1.06,5),y=round(cy*1.06,5)),size=dict(x=width or e['worldWidth'],y=height or e['worldHeight']),angle=angle,order=order,flipX=False,flipY=False,color=dict(r=1,g=1,b=1,a=1)))

# R17 is the first executable composition reference. All coordinates below are
# map-centred cells, not arbitrary image positions.
r=17;cx,cy=rooms[r][2:4]
piece(r,'WestTomb','CoffinClosed',cx-5,cy-3,1.8,.8,group='R17_SW')
piece(r,'EastTomb','BoneCasketDeep',cx+5,cy+3,1.8,.9,group='R17_NE')
piece(r,'NorthDamage','DamagedRecessDeep',-51.0,-11.43,3.0,1.3)
piece(r,'NorthWest','UrnRecessDeep',-47.55,-11.43,2.8,1.2)
piece(r,'NorthEastA','UrnRecessDeep',-40.55,-11.43,2.8,1.2)
piece(r,'NorthEastB','UrnRecessDeep',-37.6,-11.43,2.8,1.2)
for i,y in enumerate((-14.3,-17.3,-20.3,-23.3,-26.3)):
    piece(r,'EastNiche'+str(i),'UrnRecessDeep',-34.43,y,2.8,1.2,-90)
piece(r,'RootIngress','RootBranches',-51.0,-13.7,4.0,4.5,order=-4,group='NorthDamage')
piece(r,'ExposedRemains','GatheredBones',-51.2,-13.5,.95,.6,order=-3,group='NorthDamage')
piece(r,'FallenMortar','StoneChips',-50.3,-13.9,1.2,.65,order=-6,group='NorthDamage')
piece(r,'DustAtFracture','FineDust',-52.1,-12.8,1.6,.9,-30,order=-6,group='NorthDamage')
piece(r,'FineDebrisTrail','StoneChips',-51.5,-14.4,1.1,.6,25,order=-6,group='NorthDamage')
piece(r,'AbandonedBed','AbandonedBed',-49.8,-24.7,2.0,.8,-18,group='AbandonedCamp')
piece(r,'AbandonedBag','AbandonedBag',-48.7,-24.2,.55,.4,group='AbandonedCamp')
piece(r,'ColdAsh','ColdAsh',-47.9,-25.0,.65,.55,order=-6,group='AbandonedCamp')
piece(r,'CookingPot','CookingPot',-47.2,-24.8,.4,.4,group='AbandonedCamp')

wall_notes=[]
def wall(room,id,asset,side,along,width=2.8,depth=1.2,order=-3):
    _,_,cx,cy,w,h=rooms[room]
    requested_depth=depth
    for shift in range(3):
        # Follow a contiguous floor extension (e.g. R18's northwest corridor),
        # never skip stone or cross a doorway to decorate its opposite wall.
        if shift:
            axis=range(math.floor(along-width/2/1.06+.0001),math.ceil(along+width/2/1.06-.0001))
            band=[]
            for a in axis:
                for step in range(shift):
                    band.append((a,cy+h//2+step) if side=='N' else (a,cy-h//2-1-step) if side=='S' else (cx+w//2+step,a) if side=='E' else (cx-w//2-1-step,a))
            if not all(p in dry and p not in low for p in band): continue
        for depth in dict.fromkeys((requested_depth,min(requested_depth,.9))):
            if side=='N': x,y,angle=along,cy+h/2+shift+depth/2/1.06,0
            elif side=='S': x,y,angle=along,cy-h/2-shift-depth/2/1.06,180
            elif side=='E': x,y,angle=cx+w/2+shift+depth/2/1.06,along,-90
            else: x,y,angle=cx-w/2-shift-depth/2/1.06,along,90
            bw,bh=(width,depth) if side in ('N','S') else (depth,width)
            bounds=(math.floor(x-bw/2/1.06+.0001),math.floor(y-bh/2/1.06+.0001),math.ceil(x+bw/2/1.06-.0001),math.ceil(y+bh/2/1.06-.0001))
            if all(solid(xx,yy) for yy in range(bounds[1],bounds[3]) for xx in range(bounds[0],bounds[2])):
                piece(room,id,asset,x,y,width,depth,angle,order,group=f'R{room:02d}_{id}')
                pieces[-1]['onWall']=True
                if depth!=requested_depth or shift:
                    wall_notes.append(dict(room=room,anchor=id,side=side,along=along,resolution=f'Encaixe na borda física real: prolongamento de piso {shift} célula(s), profundidade {depth} u. Sem mudança de geometria.'))
                return x,y
    wall_notes.append(dict(room=room,anchor=id,side=side,along=along,resolution='Vão real preservado; o ritmo arquitetônico termina antes deste acesso.'))
    return None
def run(room,side,asset,offsets,prefix,width=2.8,depth=1.2):
    _,_,cx,cy,_,_=rooms[room]
    for i,offset in enumerate(offsets): wall(room,f'{prefix}_{i}',asset,side,(cx if side in ('N','S') else cy)+offset,width,depth)
def base(room,which,asset,width=None,height=None,angle=0):
    _,_,cx,cy,_,_=rooms[room]
    if which=='SW' and room%3==0: return
    x,y=(cx-5,cy-3) if which=='SW' else (cx+5,cy+3)
    if (math.floor(x),math.floor(y)) not in low: return
    piece(room,which+'_'+asset,asset,x,y,width,height,angle,group=f'R{room:02d}_{which}')
def motif(room,x0,y0,w,h,opacity=.72):
    # Nine-slice a painted inlay into a rectangular border; continuous stone
    # remains visible inside it, matching the rite/cortege orientation.
    for y in range(h):
        for x in range(w):
            if x not in (0,w-1) and y not in (0,h-1): continue
            wx,wy=x0+x,y0+y
            if (wx,wy) not in dry: continue
            sx=0 if x==0 else 3 if x==w-1 else 1+x%2
            sy=0 if y==0 else 3 if y==h-1 else 1+y%2
            surfaces.append(dict(x=wx+64,y=wy+64,asset=f'Floor_Ornament_{sy*4+sx:02d}',order=-6,color=dict(r=1,g=1,b=1,a=opacity)))

# R18 — the same funerary order, pushed aside along the western wall.
r=18;cx,cy=rooms[r][2:4]
run(r,'N','UrnRecessDeep',(-4,2.5,6),'NorthRecess')
run(r,'W','UrnRecessDeep',(-6,-2,3,7),'WestRecess')
run(r,'E','UrnRecessDeep',(-7,7),'EastRemnant')
piece(r,'WesternRootTrunk','MainRootStem',-51.7,5.0,4.4,10.0,order=-3,group='R18_WesternDamage')
piece(r,'LowerRootTips','FineRoots',-52.1,1.8,3.0,1.5,-75,order=-4,group='R18_WesternDamage')
piece(r,'PillarSeekingRoots','FineRoots',-49.9,1.5,4.0,2.0,-10,order=-3,group='R18_WesternDamage')
piece(r,'DislodgedBones','GatheredBones',-52.0,1.0,1.0,.65,order=-3,group='R18_WesternDamage')
piece(r,'FallenStone','FineDust',-51.7,.3,1.6,.9,-75,order=-6,group='R18_WesternDamage')

# R08 — organized urn storage, with one localized failure.
r=8;cx,cy=rooms[r][2:4]
run(r,'N','UrnRecessDeep',(-3.5,1.1,5.7),'NorthStorage',4.2,1.2)
run(r,'S','UrnRecessDeep',(-8,-5,4,7),'SouthStorage')
run(r,'W','UrnRecessDeep',(-5,5),'EndStorage',2.8,.9)
run(r,'E','UrnRecessDeep',(-5,5),'EastStorage',2.8,.9)
wall(r,'CollapsedNorthBay','DamagedRecessDeep','N',cx-8,3.0,1.3)
piece(r,'UrnBelowFailure','UrnBroken',cx-7.7,cy+6.8,.65,.6,order=-5,group='R08_Collapse')
piece(r,'FailureDebris','FineDust',cx-7,cy+6.2,2.8,1.8,35,order=-6,group='R08_Collapse')
piece(r,'FallenUrnLid','UrnBroken',cx-7,cy+5.4,.95,.8,40,order=-5,group='R08_Collapse')
piece(r,'CrushedUrnAtWall','UrnBroken',cx-8.7,cy+6.4,.8,.7,-35,order=-5,group='R08_Collapse')
piece(r,'AshFromFailedBay','AshSpill',cx-7.9,cy+5.9,1.5,1.1,order=-6,group='R08_Collapse')
piece(r,'CollapseTravelTrace','FineDust',cx-3,cy+2,8.0,1.4,-35,order=-6,group='R08_Collapse')
pieces[-1]['color']=dict(r=.8,g=.78,b=.72,a=.35)
for which,dx,dy in (('SW',-5,-3),('NE',5,3)):
    for i,(ox,oy) in enumerate(((-.4,.2),(.3,.25),(0,-.4))):
        piece(r,which+'Container'+str(i),'UrnIntact',cx+dx+ox,cy+dy+oy,.52,.52,group='R08_'+which)

# R09 — a systematic memorial, deliberately scraped rather than overgrown.
r=9;cx,cy=rooms[r][2:4]
for i,dx in enumerate((-6,-3,0,3,6)):
    wall(r,'NorthNames'+str(i),'ScrapedPlaque' if i in (2,3) else 'MemorialPlaque','N',cx+dx)
run(r,'E','ScrapedPlaque',(-5,5),'ErasedEast')
base(r,'NE','MemorialPlaque',1.8,1.1)
piece(r,'DustBelowScraping','FineDust',cx+7,cy-3.7,1.7,.8,-65,order=-6,group='R09_Erasure')
piece(r,'RemovedMemorial','ScrapedPlaque',cx+7.1,cy-2.2,1.7,1.1,-18,order=-3,group='R09_Erasure')
piece(r,'ScrapingAtRemovedPanel','FineDust',cx+7.2,cy-2.0,2.5,3.4,-20,order=-6,group='R09_Erasure')
piece(r,'ChiselAtRemovedPanel','Chisel',cx+6.4,cy-3.6,.55,.16,-35,order=-2,group='R09_Erasure')

# R14 — the existing stair is the destination of the processional floor.
r=14;cx,cy=rooms[r][2:4]
motif(r,cx-1,cy-7,2,9)
run(r,'N','MemorialPlaque',(-5,5),'StairMemorial')
run(r,'W','DeadVotiveShelf',(-5,5),'StairVigils',2.0,.8)
base(r,'SW','FoldedRitualCloth',.8,.65)

# R15 — continuous narrative reliefs, interrupted at the real north doorway.
r=15;cx,cy=rooms[r][2:4]
run(r,'N','ProcessionRelief',(-5.2,5.2),'NorthRelief',6.0,1.8)
run(r,'S','ProcessionRelief',(-6,6),'SouthRelief',4.6,1.5)
wall(r,'SouthFracture','BrokenRelief','S',cx,5.0,1.5)
piece(r,'ReliefFragments','StoneChips',cx,cy-6.8,1.8,.8,order=-6,group='R15_Fracture')
for i,(asset,dx,dy) in enumerate((('RoundUrnWarm',-.45,.1),('RoundUrnPale',.3,.3),('RoundUrnBrown',.05,-.4))):
    piece(r,'ReliefBayVessel'+str(i),asset,cx+5+dx,cy+3+dy,.56,.56,order=-2,group='R15_NE')

# R16 and R20 — remains of vigils, quiet centers and distinct wear.
for r in (16,20):
    cx,cy=rooms[r][2:4]
    run(r,'N','DeadVotiveShelf',(-7,-4,4,7),'NorthVigil',2.8,.85)
    run(r,'S','DeadVotiveShelf',(-6,-3,3,6),'SouthVigil',2.8,.85)
    run(r,'W','DeadVotiveShelf',(-6,6),'WestVigil',2.4,.8)
    run(r,'E','DeadVotiveShelf',(-6,6),'EastVigil',2.4,.8)
    wall(r,'MourningCloth','MourningCloth','N',cx-7,2.2,1.06)
    base(r,'SW','FoldedRitualCloth',.65,.55)
    base(r,'NE','FoldedRitualCloth',.65,.55)

# R10 and R11 — fixtures, duration and the gathering of equipment.
r=10;cx,cy=rooms[r][2:4]
run(r,'N','EmptyDeepNiche',(-5,4),'CellRecesses',2.9,1.3)
run(r,'N','RecollectedGrille',(-5,4),'CellGrilles',2.7,1.2)
run(r,'N','RingChain',(-8,-1,7),'CellRings',.9,1.2)
run(r,'S','RingChain',(-6,6),'SouthRings',.9,1.2)
piece(r,'WornStraw','StrawBowl',cx-7,cy+6.2,1.6,1.0,order=-5,group='R10_Occupation')
piece(r,'TatteredPrisonCloth','MourningRag',cx-7.2,cy+6.3,1.6,.7,12,order=-4,group='R10_Occupation')
piece(r,'FallenFixingMasonry','FallenMasonry',cx+7,cy+6.3,1.0,.75,-20,order=-5,group='R10_Fixing')
piece(r,'PulledFixingDust','StoneChips',cx+7,cy+6.4,1.0,.6,order=-6,group='R10_Fixing')
r=11;cx,cy=rooms[r][2:4]
run(r,'N','RingChain',(-7,-4,4,7),'ChainHooks',1.2,1.2)
wall(r,'WesternChainRecess','EmptyDeepNiche','N',cx-5.5,4.8,1.3,order=-4)
wall(r,'EasternChainRecess','EmptyDeepNiche','N',cx+4,2.8,1.3,order=-4)
chain_store=wall(r,'StoredChainNiche','EmptyRecess','E',cy+5.5,3.0,1.2)
if chain_store:
    piece(r,'StoredChainBundle','GatheredChains',chain_store[0]-.2,chain_store[1],1.2,.8,90,order=-2,group='R11_Storage')
base(r,'NE','GatheredChains',1.1,.7)
piece(r,'FallenChainBundle','GatheredChains',cx+4,cy+1.6,1.1,.75,order=-5,group='R11_InterruptedStorage')

# R21 and R22 — ceremony and its preparation/storage.
r=21;cx,cy=rooms[r][2:4]
base(r,'NE','CrackedAltar',1.85,1.85)
run(r,'N','RitualMemorialBay',(-6,6),'ChapelMemorial',5.0,1.8)
motif(r,cx+3,cy-8,3,10)
piece(r,'AltarFragments','StoneChips',cx+5,cy+1.7,.95,.55,order=-6,group='R21_InterruptedRite')
r=22;cx,cy=rooms[r][2:4]
run(r,'N','RitualCompartment',(-6,-3,0,3,6),'NorthStorage')
run(r,'E','RitualCompartment',(-5,3,6),'EastStorage',2.8,.9)
base(r,'SW','FoldedRitualCloth',1.5,1.25)
base(r,'SW','ViolatedCase',1.4,.95)
base(r,'NE','FoldedRitualCloth',1.5,1.25)
piece(r,'AbandonedLinen','FoldedRitualCloth',cx-1.7,cy+6.7,.7,.55,order=-5,group='R22_SpoiledStorage')

# R19 — no extra workbench: the existing southwest support holds the task.
r=19;cx,cy=rooms[r][2:4]
run(r,'N','PulleyAssembly',(-7,-4,5,8),'WallPulleys',2.7,1.35)
for i,offset in enumerate((-7,-4,8)):
    wall(r,'PulleyMasonry'+str(i),'EmptyDeepNiche','N',cx+offset,2.9,1.35,order=-4)
piece(r,'PulleyRopeLink','PulleyRopeArc',cx-5.5,cy+8.1,1.55,.4,order=-2,group='R19_NorthPulleys')
base(r,'SW','UnfinishedLid',1.9,1.3,22)
base(r,'NE','GatheredChains',1.7,1.45)
piece(r,'WorkbenchDust','FineDust',cx-5.2,cy-3.5,3.2,2.3,order=-6,group='R19_Work')

# Cisterns: only the actual water masks carry water; dry rooms get wall traces.
for r in (12,23):
    cx,cy=rooms[r][2:4]
    run(r,'N','MineralStain',(-6,-3,3,6),'NorthFloodLine',2.8,.45)
    run(r,'W','MineralStain',(-6,-3,3,6),'WestFloodLine',2.8,.45)
    run(r,'E','MineralStain',(-6,6),'EastFloodLine',2.8,.45)
    if r==12: wall(r,'WaterGauge','WaterGauge','E',cy-2,1.1,.35)
    else: wall(r,'WallDrain','WallDrain','W',cy,1.5,.8)
    piece(r,'DepositedFragments','StoneChips',cx-6.8,cy-5.9,1.4,.65,order=-6,group=f'R{r:02d}_FloodDeposit')
r=13;cx,cy=rooms[r][2:4]
piece(r,'StoredCarryingSkids','CarryingSkids',cx-9.6,cy+4.0,1.3,2.8,order=-3,group='Canal')
piece(r,'NorthMooring','MooringRope',cx+8.4,cy+.8,.7,.7,group='Canal')
piece(r,'SouthMooring','MooringRope',cx+8.4,cy-3.1,.7,.7,group='Canal')
piece(r,'WetFragments','StoneChips',cx+7.8,cy-3.7,1.0,.5,order=-6,group='Canal')
run(r,'N','WetFoundation',(-7,-4,4,7),'QuayFloodLine',2.8,.8)

# Eight small thresholds; no extra occupation is invented for their empty centers.
for r in range(8):
    cx,cy=rooms[r][2:4]
    if r==0:
        wall(r,'BellMount','FuneralBell','N',cx,.9,.8)
        wall(r,'BellRecess','EmptyDeepNiche','N',cx,2.2,1.4,order=-4)
        piece(r,'ReceptionCandle','SingleDeadCandle',cx+.8,cy+4.5,.3,.4,order=-2,group='R00_ReceivingBell')
    elif r==1:
        wall(r,'UrnReception','UrnRecessDeep','N',cx,2.8,1.2)
        piece(r,'BrokenReceptionUrn','UrnBroken',cx+1.0,cy+3.8,.55,.5,order=-5,group='R01_Reception')
    elif r==2:
        wall(r,'ErasedEntranceName','ScrapedPlaque','N',cx,2.8,1.2)
        piece(r,'DisplacedNamePlaque','MemorialPlaque',cx+.7,cy+4.9,1.2,.9,-18,order=-2,group='R02_Name')
        piece(r,'NameDust','FineDust',cx+.8,cy+3.8,1.2,.5,order=-6,group='R02_Name')
    elif r==3:
        wall(r,'FoldedEntranceGrille','RecollectedGrille','N',cx,2.7,1.5)
        wall(r,'GrilleRecess','EmptyDeepNiche','N',cx,3.2,1.6,order=-4)
        wall(r,'EntranceRing','RingChain','N',cx-3,.8,1.1)
        wall(r,'RightEntranceRing','RingChain','N',cx+2.8,.7,1.0)
    elif r==4:
        piece(r,'RootedSill','RootBranches',cx-2.4,cy+2.8,4.4,5.5,order=-4,group='R04_RootIngress')
    elif r==5:
        wall(r,'VigilPreparation','DeadVotiveShelf','N',cx-3,2.0,.8)
        wall(r,'FoldedMourning','MourningCloth','N',cx+3,1.8,.8)
    elif r==6:
        wall(r,'ProcessionalMemorial','MemorialPlaque','E',cy,2.4,1.06)
        wall(r,'ProcessionBeforeSeal','ProcessionRelief','E',cy+2.6,2.7,1.06)
        wall(r,'ProcessionAfterSeal','BrokenRelief','E',cy-2.6,2.7,1.06)
        motif(r,cx-1,cy-4,2,7)
    else:
        wall(r,'CanalEntryWaterline','MineralStain','E',cy,2.8,.45)
        piece(r,'CarriedRope','MooringRope',cx+3.8,cy-3.5,.6,.6,group='Canal')
        piece(r,'CarriedTimbers','CarryingSkids',cx+3.0,cy-3.3,1.2,2.5,-65,group='Canal')

(ART/'ancoras-arquitetura.json').write_text(json.dumps(wall_notes,ensure_ascii=False,indent=2),encoding='utf-8')

# Local causes and their deposits remain grouped, without populating empty centers.
for r,x,y,w,h,angle in ((17,-50.7,-12.8,2.0,1.5,-10),(18,-52.1,7,1.4,2.0,-70),(8,rooms[8][2]-7.8,rooms[8][3]+7.2,1.3,1.0,10),(4,rooms[4][2]-4,rooms[4][3]+3,.8,.65,-10)):
    piece(r,'MasonryAtRupture','FallenMasonry',x,y,w,h,angle,order=-4,group={17:'NorthDamage',18:'R18_WesternDamage',8:'R08_Collapse',4:'R04_RootIngress'}[r])
piece(20,'ForgottenCanteen','OldCanteen',rooms[20][2]-7.6,rooms[20][3]+8.3,.35,.25,-25,group='R20_RecentPassage')
for r in (0,5,14,16,20):
    cx,cy=rooms[r][2:4]
    x,y=(cx+.8,cy+rooms[r][5]/2-.7) if r==0 else (cx-rooms[r][4]/2+.7,cy-5) if r==14 else (cx-3 if r==5 else cx-4,cy+rooms[r][5]/2-.7)
    piece(r,'WaxBelowSupport','DeadWax',x,y,.5,.35,order=-5,group=f'R{r:02d}_Vigil')
for r in (7,12,13,23):
    cx,cy=rooms[r][2:4]
    piece(r,'WaterCarriedWood','Driftwood',cx+3 if r==7 else cx-rooms[r][4]/2+1.3,cy-rooms[r][5]/2+1.0,1.0,.35,5,order=-5,group='Canal' if r==13 else f'R{r:02d}_FloodDeposit')
    if r!=7: piece(r,'FloodSediment','FineSediment',cx-rooms[r][4]/2+1.3,cy-rooms[r][5]/2+1.6,2.0,.6,70,order=-6,group=f'R{r:02d}_FloodDeposit')
for r in (12,23):
    cx,cy=rooms[r][2:4]
    for i,dx in enumerate((-5.5,4.8)):
        piece(r,'SaltAtWallFoot'+str(i),'MineralStain',cx+dx,cy+rooms[r][5]/2-.25,2.8,.5,order=-5,group=f'R{r:02d}_FloodDeposit')
for r in (16,20):
    cx,cy=rooms[r][2:4]
    for i,(dx,dy) in enumerate(((-6,rooms[r][5]/2-.1),(6,-rooms[r][5]/2+.1),(-rooms[r][4]/2+.1,-6))):
        piece(r,'WaxAtVigil'+str(i),'DeadWax',cx+dx,cy+dy,.5,.3,i*70,order=-5,group=f'R{r:02d}_Vigil')
for r,dx in ((10,7),(11,-4)):
    cx,cy=rooms[r][2:4]
    piece(r,'CorrosionAtFixing','RustAtFixing',cx+dx,cy+rooms[r][5]/2-.7,.45,.9,order=-5,group=f'R{r:02d}_Fixing')
piece(3,'RecollectedChain','GatheredChains',rooms[3][2]-3,rooms[3][3]+3.7,.5,.35,order=-5,group='R03_Fixing')
wall(0,'BellMemorial','MemorialPlaque','N',rooms[0][2],1.8,.85,order=-4)
piece(1,'AshBelowEmptyBay','AshSpill',rooms[1][2],rooms[1][3]+3.7,.8,.35,order=-6,group='R01_Reception')

# Expand reusable niches into native individually editable urns and architecture.
for v in pieces:
    if v.get('onWall') or v['asset'] in ('UrnRecessDeep','DamagedRecessDeep'):
        angle=math.radians(v['angle']);v['position']['x']+=math.sin(angle)*.22;v['position']['y']-=math.cos(angle)*.22
        if v['asset'] in ('RingChain','PulleyAssembly'):
            extra=.38 if v['asset']=='RingChain' else .22
            v['position']['x']+=math.sin(angle)*extra;v['position']['y']-=math.cos(angle)*extra
for v in list(pieces):
    if v['id']=='R18_WestRecess_2': v['asset']='DamagedRecessDeep'
    if v['asset']!='UrnRecessDeep': continue
    v['asset']='EmptyDeepNiche'; v['group']=v['id']+'_Producao'
    angle=math.radians(v['angle'])
    urn_span=v['size']['x']/2.8;urn_size=.55*min(urn_span,1.4)
    for index,(asset,offset) in enumerate(zip(('RoundUrnWarm','RoundUrnPale','RoundUrnBrown'),(-.83*urn_span,0,.83*urn_span))):
        if v['room']==1 and index==1: continue
        dx=offset*math.cos(angle)+.04*math.sin(angle)
        dy=offset*math.sin(angle)-.04*math.cos(angle)
        piece(v['room'],v['id']+'_Urn'+str(index),asset,(v['position']['x']+dx)/1.06,(v['position']['y']+dy)/1.06,urn_size,urn_size,v['angle'],v['order']+1,v['group'])
for v in pieces:
    if v['asset']=='FoldedRitualCloth': v['asset']='FoldedLinen'
    if v['room']==22 and v['id'].endswith(('SW_FoldedRitualCloth','NE_FoldedRitualCloth')):
        v['asset']='DrapedLinen';v['size']=dict(x=1.65,y=1.45)
    if v['room']==22 and v['asset']=='ViolatedCase':v['order']=-2
    if v['asset']=='RingChain' and (v['room']==11 or v['room']==10 and v['id'].endswith(('CellRings_0','CellRings_2'))):
        v['asset']='LongAnchorChain';v['size']=dict(x=.7,y=1.8)
    if v['asset']=='RustAtFixing':v['asset']='RustTrail'
    if v['asset']=='RitualCompartment' and v['id']!='R22_NorthStorage_1':
        v['asset']='EmptyRitualCabinet';v['flipX']=v['id'].endswith(('_0','_2','_4'))
    if v['room'] in (16,20) and v['asset']=='FoldedLinen': v['asset']='MourningRag';v['size']=dict(x=1.25,y=1.1)
    if v['asset']=='MourningCloth':
        v['asset']='HangingMourningCloth';v['size']=dict(x=.7,y=1.25);v['order']=-2
        v['position']['y']=(rooms[v['room']][3]+rooms[v['room']][5]/2)*1.06-.25
for v in list(pieces):
    if v['asset']!='DeadVotiveShelf': continue
    v['asset']='EmptyVotiveShelf';v['group']=v['id']+'_Vigilia'
    v['size']['y']=1.15
    angle=math.radians(v['angle'])
    # EmptyVotiveShelf already includes the stone lip. Do not squash an
    # entire second slab across it; this produced the overlapping dark band.
    for i,offset in enumerate((-.28,.27)):
        # Sparse empty supports and irregular groups express abandoned vigils.
        if i==1 and (v['room']==20 and not v['id'].endswith('_2') or v['room']==16 and v['id'].endswith('_1')): continue
        d=offset*v['size']['x'];dx=d*math.cos(angle)+.13*math.sin(angle);dy=d*math.sin(angle)-.13*math.cos(angle)
        single=v['id'].endswith('_3') and i==1
        asset='SingleDeadCandle' if single else 'ProfileCandlesB' if v['id'].endswith('_1') or i else 'ProfileCandlesA'
        piece(v['room'],v['id']+'_Candles'+str(i),asset,(v['position']['x']+dx)/1.06,(v['position']['y']+dy)/1.06,.35 if single else .7,.4 if single else .7,0,v['order']+2,v['group'])
    if v['id'].endswith(('_0','_2')):
        dx=math.sin(angle)*.76;dy=-math.cos(angle)*.76
        piece(v['room'],v['id']+'_WaxAtFoot','DeadWax',(v['position']['x']+dx)/1.06,(v['position']['y']+dy)/1.06,.65,.45,v['angle'],order=-5,group=v['group'])
    if v['id'].endswith('_0') and v['room'] in (16,20) and 'NorthVigil' not in v['id']:
        dx=math.sin(angle)*.45;dy=-math.cos(angle)*.45
        piece(v['room'],v['id']+'_DrapedRemnant','HangingMourningCloth',(v['position']['x']+dx)/1.06,(v['position']['y']+dy)/1.06,.65,.9,0,order=-2,group=v['group'])

# Deterioration follows the architecture that produced it, ending before
# openings. The deposits are separate editable decals, never new obstacles.
for v in list(pieces):
    if not v.get('onWall'): continue
    if v['asset'] not in ('EmptyDeepNiche','ScrapedPlaque','MemorialPlaque','ProcessionRelief','BrokenRelief','EmptyVotiveShelf','RitualCompartment','EmptyRitualCabinet','PulleyAssembly','RingChain','LongAnchorChain','RitualMemorialBay'): continue
    angle=math.radians(v['angle']);depth=v['size']['y'];distance=depth*.5+.4
    dx=math.sin(angle)*distance;dy=-math.cos(angle)*distance
    asset='RustTrail' if v['asset'] in ('RingChain','LongAnchorChain') else 'FineDust'
    width,height=(.7,1.8) if asset=='RustTrail' else (v['size']['x']*.95,1.4)
    piece(v['room'],v['id']+'_DepositAtFoot',asset,(v['position']['x']+dx)/1.06,(v['position']['y']+dy)/1.06,width,height,v['angle'],order=-6,group=v['group'])
for r in (14,21):
    cx,cy=rooms[r][2:4]
    for i,dy in enumerate(((-6.5,-4.3,-2.1,.1) if r==21 else (-6.5,-4.3,-2.1,.1,2.3))):
        piece(r,'CeremonialPanel'+str(i),('PalmInlay','RosetteInlay','WornSealInlay')[i%3],cx+(4.5 if r==21 else 0),cy+dy,2.12,2.12,order=-5,group=f'R{r:02d}_ProcessionFloor')
cx,cy=rooms[13][2:4]
route_start=(cx-8.3,cy-.6);route_end=(cx+8.3,cy-2.0)
dx,dy=route_end[0]-route_start[0],route_end[1]-route_start[1];length=math.hypot(dx,dy);angle=math.degrees(math.atan2(dy,dx))
for i in range(4):
    t=(i+.5)/4
    piece(13,'TransportWear'+str(i),'TransportRuts',route_start[0]+dx*t,route_start[1]+dy*t,length/4*1.06,.65,angle,order=-6,group='Canal')
for v in pieces:
    if v['asset'] in ('PalmInlay','RosetteInlay','WornSealInlay'):
        v['color']=dict(r=.75,g=.77,b=.73,a=.48 if v['room']==21 else .64)
    if v['asset']=='TransportRuts': v['color']=dict(r=.65,g=.67,b=.60,a=.55)
piece(19,'ChiselAtLid','Chisel',rooms[19][2]-5.8,rooms[19][3]-3.1,.55,.16,22,group='R19_SW')
piece(11,'BrokenFixing','BrokenChainLink',rooms[11][2]+4.7,rooms[11][3]+1.4,.3,.22,group='R11_InterruptedStorage')
piece(21,'IncenseAtAltar','IncenseBowl',rooms[21][2]+4.5,rooms[21][3]+3.4,.32,.32,order=-2,group='R21_NE')
piece(21,'TornAltarRunner','AltarRunner',rooms[21][2]+5.5,rooms[21][3]+2.6,.55,1.3,order=-2,group='R21_NE')
piece(22,'CaughtRunner','AltarRunner',rooms[22][2]-4.2,rooms[22][3]+7.6,.7,1.7,-12,order=-2,group='R22_SpoiledStorage')
piece(22,'DroppedVessel','IncenseBowl',rooms[22][2]-2.8,rooms[22][3]+6.6,.32,.32,order=-2,group='R22_SpoiledStorage')
for r in (10,11,12,18,19,23):
    base(r,'SW','StoneLatticeBase',2.12,2.12)
    base(r,'NE','StoneLatticeBase',2.12,2.12)
# The grated support belongs below its removable objects.
for v in pieces:
    if v['asset']=='StoneLatticeBase':v['order']=-3
    elif v['room'] in (10,11,12,18,19,23) and v['group'] in (f"R{v['room']:02d}_SW",f"R{v['room']:02d}_NE") and v['order']>=-3:v['order']=-2
for v in pieces:
    if v['asset']=='MineralStain' and v.get('onWall'):
        v['asset']='WetFoundation';v['size']['y']=.8
        angle=math.radians(v['angle']);v['position']['x']-=math.sin(angle)*.175;v['position']['y']+=math.cos(angle)*.175
piece(9,'AbandonedChisel','Chisel',rooms[9][2]+6.8,rooms[9][3]+6.9,.55,.16,-25,group='R09_Erasure')
for r,point in ((10,'KeyPedestal'),(19,'CrankPedestal')):
    cx,cy=rooms[r][2:4]
    piece(r,'FunctionalPedestal','MechanismPedestal',cx,cy+3/1.06,1.5,1.5,order=-4,group='Point/'+point)

# Mineral residues follow contiguous physical wall/water edges, interrupted
# by actual openings. Each segment is an editable decal with no collision.
for r in (7,12,13,23):
    _,_,cx,cy,w,h=rooms[r]
    xmin,xmax=cx-w//2,cx+w//2;ymin,ymax=cy-h//2,cy+h//2
    for side in ('N','E','S','W'):
        along=range(xmin,xmax) if side in ('N','S') else range(ymin,ymax)
        segments=[];current=[]
        for a in along:
            if side=='N': x,y,nx,ny=a,ymax-1,a,ymax
            elif side=='S': x,y,nx,ny=a,ymin,a,ymin-1
            elif side=='E': x,y,nx,ny=xmax-1,a,xmax,a
            else: x,y,nx,ny=xmin,a,xmin-1,a
            edge=(x,y) in dry and (solid(nx,ny) or (nx,ny) in water)
            if edge: current.append(a)
            elif current: segments.append(current);current=[]
        if current: segments.append(current)
        for j,segment in enumerate(segments):
            if len(segment)<2: continue
            count=math.ceil(len(segment)/4)
            for k in range(count):
                length=len(segment)/count;middle=segment[0]+(k+.5)*length
                if side=='N': x,y,angle=middle,ymax-.62,0
                elif side=='S': x,y,angle=middle,ymin+.62,180
                elif side=='E': x,y,angle=xmax-.62,middle,-90
                else: x,y,angle=xmin+.62,middle,90
                piece(r,f'{side}MineralEdge{j}_{k}','MineralStain',x,y,length*1.06,1.25,angle,order=-5,group='Canal' if r==13 else f'R{r:02d}_FloodDeposit')
for r in (16,20):
    cx,cy=rooms[r][2:4]
    piece(r,'SpentCandlesNE','DeadCandles',cx+5.25,cy+3,.5,.5,order=-2,group=f'R{r:02d}_NE')
cx,cy=rooms[20][2:4]
piece(20,'WornVigilTracery','MourningFloorTrace',cx+.5,cy,7.0,12.0,order=-5,group='R20_ProcessionalWear')
pieces[-1]['color']=dict(r=1,g=1,b=.95,a=.65)

# Residue is connected to the actual damaged wall, never a detached material island.
root_soil={(x,y) for x in range(-55,-49) for y in range(-16,-10) if (x+53)+(abs(y+12))<4}
edges={(x,y) for x in range(-53,-35) for y in range(-28,-12) if (x,y) in dry and any(solid(x+dx,y+dy) for dx,dy in ((0,1),(1,0),(0,-1),(-1,0)))}
for patch,opacity in ((edges,.18),(root_soil,.90)):
    for x,y in sorted(patch):
        if (x,y) not in dry: continue
        ns=((0,1),(1,0),(0,-1),(-1,0),(1,1),(1,-1),(-1,-1),(-1,1))
        m=sum(1<<i for i,(dx,dy) in enumerate(ns) if (x+dx,y+dy) in patch or solid(x+dx,y+dy))
        for bit,a,b in ((4,0,1),(5,1,2),(6,2,3),(7,3,0)):
            if not(m & 1<<a and m & 1<<b): m &= ~(1<<bit)
        surfaces.append(dict(x=x+64,y=y+64,asset=f'Transition_Earth_{m:02X}',order=-6,color=dict(r=1,g=1,b=1,a=opacity)))

def connected_patch(patch,family,opacity,order=-6):
    patch={p for p in patch if p in dry}
    for x,y in sorted(patch):
        ns=((0,1),(1,0),(0,-1),(-1,0),(1,1),(1,-1),(-1,-1),(-1,1))
        m=sum(1<<i for i,(dx,dy) in enumerate(ns) if (x+dx,y+dy) in patch or solid(x+dx,y+dy))
        for bit,a,b in ((4,0,1),(5,1,2),(6,2,3),(7,3,0)):
            if not(m & 1<<a and m & 1<<b): m &= ~(1<<bit)
        surfaces.append(dict(x=x+64,y=y+64,asset=f'Transition_{family}_{m:02X}',order=order,color=dict(r=1,g=1,b=1,a=opacity)))

for r in (12,23):
    _,_,cx,cy,w,h=rooms[r]
    patch={(x,y) for x in range(cx-w//2,cx+w//2) for y in range(cy-h//2,cy+h//2)
           if (x in (cx-w//2,cx+w//2-1) or y in (cy-h//2,cy+h//2-1))
           and any(solid(x+dx,y+dy) for dx,dy in ((0,1),(1,0),(0,-1),(-1,0)))}
    connected_patch(patch,'Damp',.85)
for r in (9,15,16,20,21,22):
    _,_,cx,cy,w,h=rooms[r]
    patch={(x,y) for x in range(cx-w//2,cx+w//2) for y in range(cy-h//2,cy+h//2)
           if any(solid(x+dx,y+dy) for dx,dy in ((0,1),(1,0),(0,-1),(-1,0)))}
    connected_patch(patch,'Earth',.20)
for r in (4,18):
    _,_,cx,cy,w,h=rooms[r]
    patch={(x,y) for x in range(cx-w//2,cx-w//2+2) for y in range(cy-h//2+2,cy+h//2)
           if x<cx-w//2+1 or y>cy+2}
    connected_patch(patch,'Earth',.65)
# The western failure in R18 sheds material from the opened niches down to
# the root foot. Three overlapping deposits follow that edge, with the
# strongest rubble at the broken bay; none invents a new physical obstacle.
for i,(x,y,w,h,angle) in enumerate(((-52.1,7.0,1.8,2.5,-65),(-52.0,3.2,1.4,2.2,-80),(-51.9,.2,1.35,1.8,-95))):
    piece(18,'RootWallScree'+str(i),'StoneChips',x,y,w,h,angle,order=-5,group='R18_WesternDamage')
    piece(18,'RootWallDust'+str(i),'FineDust',x+.35,y-.5,2.0,3.1,angle,order=-6,group='R18_WesternDamage')
piece(18,'OpenBayRemains','GatheredBones',-52.1,7.3,.8,.65,-35,order=-2,group='R18_WesternDamage')
# Broken stone at the western drain belongs to the former perimeter flow.
cx,cy=rooms[23][2:4]
piece(23,'DrainFall','FallenMasonry',cx-7.2,cy-.8,1.4,.9,-15,order=-4,group='R23_FloodDeposit')
piece(23,'DrainFallDust','FineDust',cx-6.9,cy-1.1,2.0,1.1,-20,order=-6,group='R23_FloodDeposit')
for r in (4,7,12,13,17,18,23):
    _,_,cx,cy,w,h=rooms[r]
    for x in range(cx-w//2-1,cx+w//2+1):
        for y in range(cy-h//2-1,cy+h//2+1):
            if not solid(x,y) or not any((x+dx,y+dy) in dry or (x+dx,y+dy) in water for dx,dy in ((0,1),(1,0),(0,-1),(-1,0))): continue
            if r in (4,17,18) and not(x<cx-w//2+2 and y>cy): continue
            state='Roots' if r in (4,17,18) else 'Damp'
            surfaces.append(dict(x=x+64,y=y+64,asset=f'Wall_{state}_{mask(x,y):02X}',order=-4,color=dict(r=1,g=1,b=1,a=1)))

for v in pieces:
    # Upright relief figures retain the concept's camera-facing projection;
    # their architectural anchor/deposits were already resolved above.
    if v['asset'] in ('ProcessionRelief','BrokenRelief') and v['angle']==180:v['angle']=0
    if v['asset']=='FallenMasonry':v['color']=dict(r=.8,g=.81,b=.77,a=1)
    if v['asset']=='MemorialPlaque':v['asset']='InscribedMemorialPlaque'
    if v['id'].startswith('R10_CellGrilles_'):v['order']=-2
    if v['id']=='R08_CollapseTravelTrace':
        v['asset']='DustyBootTrail';v['size']=dict(x=10.5,y=1.8);v['angle']=-38;v['color']=dict(r=.92,g=.90,b=.84,a=.72)
# Repeated passage rubs an arc through the quiet cloister, from its western
# entrance toward the north opening. It leaves the broad center empty.
cx,cy=rooms[16][2:4]
piece(16,'VigilPassageDust','DustyBootTrail',cx-3.0,cy+1.7,10.0,1.5,38,order=-6,group='R16_Circulation')
pieces[-1]['color']=dict(r=.8,g=.77,b=.70,a=.30)
surfaces=list({(s['order'],s['x'],s['y']):s for s in surfaces}.values())
# Existing entrance urns belong to the receiving/threshold compositions.
# IDs, families, variants, activation and rewards remain unchanged. R17's
# explicitly approved interactive group and all other candidates are preserved.
entrance_urn_offsets=((-1.5,4.5),(1.5,4.5),(-2.5,4.5),(-4.5,3.5),(-4.5,.5),(-4.5,3.5),(4.5,2.5),(-4.5,-3.5))
point_overrides=[]
for r,(dx,dy) in enumerate(entrance_urn_offsets):
    cx,cy=rooms[r][2:4];x,y=cx+dx,cy+dy
    if (math.floor(x),math.floor(y)) not in dry or (math.floor(x),math.floor(y)) in low: raise ValueError('Urna fora do piso: '+str(r))
    point_overrides.append(dict(id=f'Breakable/{r}',position=dict(x=round(x*1.06,5),y=round(y*1.06,5))))
recipes=dict(visuals=pieces,surfaces=surfaces,dressedRooms=list(range(24)),pointOverrides=point_overrides,functionalOverrides=[
    dict(id='CAT-E04_v01#0',asset='RoundWellRim',width=3.2,height=3.2,order=-5,preview=True),
    dict(id='CAT-E05_v01#0',asset='RoundLiftDeck',width=2.5,height=2.5,order=-4,preview=True),
    dict(id='DockPier#0',asset='StoneQuayPier',width=1.06,height=6.36,order=-6,preview=True,point='Exit/2',offsetX=-2.12),
    dict(id='DockEdge#0',asset='DockEdge',width=2.12,height=.53,order=-6,preview=False,hidden=True)])
(OUT/'AuthoringRecipes.json').write_text(json.dumps(recipes,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')

def montage(room_index,scale=96):
    name,sector,cx,cy,w,h=rooms[room_index]
    x0,y0=cx-w//2-2,cy-h//2-2;rw,rh=w+4,h+4
    image=Image.new('RGBA',(rw*scale,rh*scale),'#1b201d')
    for y in range(y0,y0+rh):
        for x in range(x0,x0+rw):
            if (x,y) in dry or (x,y) in low: id=f'Floor_Limestone_{((y+64)%6)*6+(x+64)%6:02d}'
            elif (x,y) in water: id=f'Floor_Wet_{((y+64)%4)*4+(x+64)%4:02d}'
            else: id=f'Wall_Dry_{mask(x,y):02X}'
            tile=sprite(id).resize((scale,scale),Image.Resampling.LANCZOS)
            if id.startswith('Floor_Limestone'): tile=ImageChops.multiply(tile,Image.new('RGBA',tile.size,(209,217,219,255)))
            image.alpha_composite(tile,((x-x0)*scale,(y0+rh-1-y)*scale))
    def draw(id,wx,wy,wu,hu,angle=0):
        tile=sprite(id).resize((max(1,round(wu/1.06*scale)),max(1,round(hu/1.06*scale))),Image.Resampling.LANCZOS)
        if angle: tile=tile.rotate(angle,Image.Resampling.BICUBIC,expand=True)
        px=(wx/1.06-x0)*scale;py=(y0+rh-wy/1.06)*scale
        image.alpha_composite(tile,(round(px-tile.width/2),round(py-tile.height/2)))
    for s in surfaces:
        x,y=s['x']-64,s['y']-64
        if not(x0<=x<x0+rw and y0<=y<y0+rh): continue
        tile=sprite(s['asset']).resize((scale,scale),Image.Resampling.LANCZOS)
        tile.putalpha(tile.getchannel('A').point(lambda a:round(a*s['color']['a'])))
        image.alpha_composite(tile,((x-x0)*scale,(y0+rh-1-y)*scale))
    for ox,oy in ((cx-6,cy-4),(cx+4,cy+2)):
        if (ox,oy) in low: draw('LowCoverStone',(ox+1)*1.06,(oy+1)*1.06,2.12,2.12)
    for v in sorted((p for p in pieces if p['room']==room_index),key=lambda p:p['order']):
        draw(v['asset'],v['position']['x'],v['position']['y'],v['size']['x'],v['size']['y'],v['angle'])
    if room_index==17:
        data=json.loads((ROOT/'Assets/Resources/Environment/Dungeon/Breakables/BreakablesCatalog.json').read_text(encoding='utf-8-sig'))
        for c in [c for c in data['candidates'] if c['room']==17][:2]:
            draw(f"Breakable_{c['family']}_Intact_{c['variant']}",c['x'],c['y'],.9,.9,c['angle'])
    return image.convert('RGB')

montage(17).save(ART/'Montagem-R17-v1.png')
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',25)
reference=Image.open(ART/'Conceito-Ossarios-v1.png').crop((0,150,825,953)).convert('RGB')
actual=montage(17); actual.thumbnail((1000,1000))
reference.thumbnail((1000,1000))
board=Image.new('RGB',(2020,1120),'#1c201c');d=ImageDraw.Draw(board)
d.text((20,15),'CONCEITO APROVADO — R17',font=font,fill='#e6d9bc')
d.text((1030,15),'MONTAGEM COM OS ASSETS — V1',font=font,fill='#e6d9bc')
board.paste(reference,((1000-reference.width)//2+10,65));board.paste(actual,(1020+(1000-actual.width)//2,65))
d.text((20,1070),'Comparação estática de composição. Não é captura Unity; duas urnas ilustram candidatos funcionais existentes.',font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',20),fill='#b8b7a9')
board.save(ART/'Comparacao-R17-v1.png')
print(json.dumps({'authoredPieces':len(pieces),'authoredRooms':24,'studyRoom':17,'comparison':str(ART/'Comparacao-R17-v1.png')},ensure_ascii=False))
gallery=Image.new('RGB',(1800,math.ceil(24/4)*470),'#1c201c')
gd=ImageDraw.Draw(gallery)
for r in range(24):
    m=montage(r,48);m.thumbnail((440,430))
    x=(r%4)*450;y=(r//4)*470
    gallery.paste(m,(x+(450-m.width)//2,y+35))
    gd.text((x+12,y+5),f'R{r:02d} — {rooms[r][0]}',font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',16),fill='#e4d9be')
gallery.save(ART/'Galeria-montagens-estaticas-v1.png')
