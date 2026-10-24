"""Asset production: slice painted sources, build shared atlases and authored recipes.
No Unity session, gameplay checks, tests or build is started.
"""
from pathlib import Path
import json
import math
import uuid
import re
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(__file__).resolve().parents[1]
ART=ROOT/'Art/Catacumbas/ReworkCompleto'
OUT=ROOT/'Assets/Resources/Environment/Dungeon/ReworkComplete'
PILOT=ROOT/'Art/Catacumbas/ReworkPiloto'
OUT.mkdir(parents=True,exist_ok=True)
entries=[]; images={}; tile_images=[]
SIZE,PAD,PITCH=128,2,132

def texture_meta(path):
    dest=Path(str(path)+'.meta')
    if dest.exists(): return
    dest.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'''\nTextureImporter:
  serializedVersion: 13
  mipmaps:
    enableMipMap: 0
    sRGBTexture: 1
  isReadable: 0
  maxTextureSize: 4096
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  textureType: 0
  textureShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 4096
    textureFormat: -1
    textureCompression: 0
    overridden: 0
''',encoding='utf-8')

def entry(id,path,rect,size,family,state='',tile=False,structural=False,kind=1,order=-3):
    x,y,w,h=rect
    e=dict(id=id,texture=path.relative_to(ROOT).as_posix(),x=x,y=y,width=w,height=h,
           worldWidth=size[0],worldHeight=size[1],pivotX=.5,pivotY=.5,family=family,sector='',state=state,
           tile=tile,structural=structural,physicalKind=kind,order=order)
    entries.append(e)
    return e

def periodic(im):
    a=np.asarray(im.convert('RGB').resize((512,512),Image.Resampling.LANCZOS)).astype(np.float32)
    for axis in (0,1):
        original=a.copy()
        for i in range(32):
            weight=.5*(1-i/32)**2
            if axis==0:
                a[i]=original[i]*(1-weight)+original[-1-i]*weight
                a[-1-i]=original[-1-i]*(1-weight)+original[i]*weight
            else:
                a[:,i]=original[:,i]*(1-weight)+original[:,-1-i]*weight
                a[:,-1-i]=original[:,-1-i]*(1-weight)+original[:,i]*weight
    return Image.fromarray(a.clip(0,255).astype('uint8')).convert('RGBA')

def normalized(mask):
    for bit,a,b in ((4,0,1),(5,1,2),(6,2,3),(7,3,0)):
        if not(mask & 1<<a and mask & 1<<b): mask &= ~(1<<bit)
    return mask

pilot=json.loads((ROOT/'Assets/Resources/Environment/Dungeon/ReworkPilot/Pilot.json').read_text(encoding='utf-8-sig'))
for a in pilot['assets']:
    if a['id'].startswith('CRP_Wall_') or a['id']=='CRP_LowCoverStone':
        path=ROOT/'Assets/Resources'/Path(a['resource']+'.png')
        id=a['id'].replace('CRP_Wall_','Wall_').replace('CRP_LowCoverStone','LowCoverStoneLegacy')
        e=entry(id,path,(a['x'],a['y'],a['width'],a['height']),(1.06,1.06),'Walls' if id.startswith('Wall_') else 'Bases',tile=id.startswith('Wall_'),structural=id.startswith('Wall_'),kind=0,order=-5 if id.startswith('Wall_') else -4)
        im=Image.open(path).convert('RGBA'); images[id]=im.crop((a['x'],im.height-a['y']-a['height'],a['x']+a['width'],im.height-a['y']))

materials=Image.open(PILOT/'Fontes/Materiais.png')
floor_sheet=Image.open(ART/'Fontes/Pisos-v1.png')
water_sheet=Image.open(PILOT/'Fontes/EstadosAgua.png')
stone=periodic(materials.crop((materials.width//2,0,materials.width,materials.height//2)))
sources={'Stone':stone}
for i,name in enumerate(('StoneGenerated','Ornament','Ruin','Earth','Mixed','Roots')):
    w,h=floor_sheet.width//3,floor_sheet.height//2
    sources[name]=periodic(floor_sheet.crop(((i%3)*w+4,(i//3)*h+4,(i%3+1)*w-4,(i//3+1)*h-4)))
sources['Flooded']=periodic(water_sheet.crop((water_sheet.width//2,water_sheet.height//2,water_sheet.width,water_sheet.height)))
sources['Wet']=periodic(Image.open(ROOT/'Assets/Resources/Environment/Dungeon/Floors/CAT-F07_v01.png'))
sources['Stone']=periodic(Image.open(ART/'Fontes/Lajes-fidelidade-v2.png'))
damp=np.asarray(sources['Stone']).astype(np.float32); damp[:,:,:3]*=np.array([.65,.74,.66]); sources['Damp']=Image.fromarray(damp.astype('uint8'))
for family in ('Ornament','Ruin','Earth','Mixed','Roots','Damp','Flooded'):
    path=OUT/('FloorMaterial_'+family+'.png');sources[family].save(path);texture_meta(path)
for family in ('Stone','Ornament','Ruin','Earth','Mixed','Roots','Wet','Damp','Flooded'):
    count=8 if family=='Stone' else 4
    source=sources[family].resize((count*128,count*128),Image.Resampling.LANCZOS)
    for y in range(count):
        for x in range(count):
            id=f'Floor_{family}_{y*count+x:02d}'
            tile_images.append((id,source.crop((x*128,(count-1-y)*128,(x+1)*128,(count-y)*128)),family))
limestone=sources['Stone'].resize((768,768),Image.Resampling.LANCZOS)
for y in range(6):
    for x in range(6): tile_images.append((f'Floor_Limestone_{y*6+x:02d}',limestone.crop((x*128,(5-y)*128,(x+1)*128,(6-y)*128)),'Limestone'))

yy,xx=np.mgrid[0:128,0:128]; u=(xx+.5)/128; v=(yy+.5)/128
for family in ('Ornament','Ruin','Earth','Mixed','Roots','Damp','Flooded'):
    material=periodic(sources[family]).resize((128,128),Image.Resampling.LANCZOS)
    for mask in range(256):
        if normalized(mask)!=mask: continue
        alpha=np.ones((128,128),dtype=np.float32)
        for bit,distance in ((0,v),(1,1-u),(2,1-v),(3,u)):
            if not(mask & 1<<bit): alpha=np.minimum(alpha,np.clip((distance-.025)/.20,0,1))
        for bit,a,b,cx,cy in ((4,0,1,1,0),(5,1,2,1,1),(6,2,3,0,1),(7,3,0,0,0)):
            if mask & 1<<a and mask & 1<<b and not(mask & 1<<bit):
                distance=np.sqrt((u-cx)**2+(v-cy)**2)
                alpha=np.minimum(alpha,np.clip((distance-.07)/.20,0,1))
        alpha=alpha*alpha*(3-2*alpha)
        image=material.copy(); image.putalpha(Image.fromarray((alpha*255).astype('uint8')))
        tile_images.append((f'Transition_{family}_{mask:02X}',image,family))

atlas_sizes=[]
for page,start in enumerate(range(0,len(tile_images),16*31)):
    tiles=tile_images[start:start+16*31]
    atlas=Image.new('RGBA',(16*PITCH,math.ceil(len(tiles)/16)*PITCH))
    atlas_path=OUT/('SharedFloorAtlas'+('' if page==0 else '_'+str(page+1).zfill(2))+'.png')
    for i,(id,tile,family) in enumerate(tiles):
        x=(i%16)*PITCH+PAD; y=(i//16)*PITCH+PAD
        padded=Image.new('RGBA',(132,132)); padded.paste(tile,(2,2))
        padded.paste(tile.crop((0,0,1,128)).resize((2,128)),(0,2)); padded.paste(tile.crop((127,0,128,128)).resize((2,128)),(130,2))
        padded.paste(padded.crop((0,2,132,3)).resize((132,2)),(0,0)); padded.paste(padded.crop((0,129,132,130)).resize((132,2)),(0,130))
        atlas.paste(padded,(x-2,y-2)); images[id]=tile
        entry(id,atlas_path,(x,atlas.height-y-128,128,128),(1.06,1.06),'Floors',family,tile=True,order=-6)
    atlas.save(atlas_path);texture_meta(atlas_path);atlas_sizes.append(atlas.size)

packs=[
    (PILOT/'Fontes/Funerarios.png',('CoffinClosed','CoffinOpen','CoffinBroken','UrnIntact','UrnBroken','Skeleton'),((1.8,.8),(1.8,.8),(1.9,1.1),(.6,.6),(.65,.6),(1.7,1))),
    (PILOT/'Fontes/Ossuario-v3.png',('UrnRecess','DamagedRecess','GatheredBones','StoneDust','FineRoots','BoneCasket'),((2.8,1.06),(2.8,1.06),(1.1,.7),(1.2,.65),(2.8,1.4),(1.8,1.0))),
    (ART/'Fontes/Arquitetura-v1.png',('MemorialPlaque','ScrapedPlaque','ProcessionRelief','BrokenRelief','RitualCompartment','EmptyRecess'),((2.8,1.06),)*6),
    (ART/'Fontes/VigiliaPrisao-v1.png',('DeadVotiveShelf','MourningCloth','IronGrille','RingChain','GatheredChains','StrawBowl'),((2.4,.85),(2.2,1.06),(2.7,.85),(.8,1.2),(.7,.5),(1.6,1.0))),
    (ART/'Fontes/OficiosRito-v1.png',('CrackedAltar','FoldedRitualCloth','ViolatedCase','PulleyAssembly','UnfinishedLid','MooringRope'),((1.8,1.8),(.75,.7),(.9,.7),(2.8,.85),(1.8,.65),(.7,.7))),
    (ART/'Fontes/AbrigoRaizes-v1.png',('AbandonedBed','AbandonedBag','ColdAsh','CookingPot','RootIngress','MineralStain'),((2.0,.8),(.55,.4),(.65,.55),(.4,.4),(4.2,4.8),(2.8,.45))),
    (ART/'Fontes/PassagensAgua-v1.png',('FuneralBell','WaterGauge','WallDrain','CarryingSkids','StoneChips','CrumblingMortar'),((.9,.8),(.35,1.1),(1.5,.8),(1.3,1.8),(1.2,.7),(1.0,1.0))),
    (ART/'Fontes/Ossuario-fidelidade-v2.png',('UrnRecessDeep','DamagedRecessDeep','RootBranches','FineDust','FuneraryBase','BoneCasketDeep'),((2.8,1.2),(2.8,1.2),(4.0,4.5),(1.6,.9),(2.12,2.12),(1.8,.9))),
    (ART/'Fontes/DetalhesIndividuais-v1.png',('Chisel','BrokenChainLink','DeadCandles','FoldedLinen','IncenseBowl','MourningRag'),((.55,.16),(.3,.22),(.5,.5),(.65,.45),(.32,.32),(.65,.65))),
    (ART/'Fontes/NichoDesmontavel-v1.png',('EmptyDeepNiche','RoundUrnWarm','RoundUrnPale','RoundUrnBrown','NicheEndcap','AshSpill'),((2.8,1.2),(.45,.45),(.45,.45),(.45,.45),(.22,1.2),(1.2,.45))),
    (ART/'Fontes/Vestigios-v1.png',('FallenMasonry','OldCanteen','Driftwood','DeadWax','FineSediment','RustAtFixing'),((1.6,1.3),(.35,.25),(1.0,.35),(.5,.35),(1.4,.5),(.45,.9))),
    (ART/'Fontes/TraducaoFuncional-v1.png',('RoundWellRim','RoundLiftDeck','AltarRunner','MainRootStem','WetFoundation','ErodedBase'),((3.2,3.2),(2.5,2.5),(.55,1.3),(3.2,6),(2.8,.8),(2.12,2.12))),
    (ART/'Fontes/ModulosCerimoniais-v1.png',('PalmInlay','RosetteInlay','WornSealInlay','InlayStraight','InlayCorner','EmptyVotiveShelf'),((2.12,2.12),(2.12,2.12),(2.12,2.12),(2.12,.18),(.65,.65),(2.4,.8))),
    (ART/'Fontes/Vigilia-fidelidade-v2.png',('EmptyFlatShelf','ProfileCandlesA','ProfileCandlesB','SingleDeadCandle','HangingMourningCloth','DamagedFlatShelf'),((2.4,.8),(.65,.55),(.55,.55),(.35,.4),(.7,1.25),(1.8,.8)))
]
for path,names,sizes in packs:
    sheet=Image.open(path).convert('RGBA')
    for i,(id,size) in enumerate(zip(names,sizes)):
        w,h=sheet.width//3,sheet.height//2
        sprite=sheet.crop(((i%3)*w,(i//3)*h,(i%3+1)*w,(i//3+1)*h))
        if id=='EmptyFlatShelf': sprite=sheet.crop((0,190,790,460))
        if id=='ProfileCandlesA': sprite=sheet.crop((790,150,1150,490))
        if id=='ProfileCandlesB': sprite=sheet.crop((1180,150,1536,490))
        if id=='SingleDeadCandle': sprite=sheet.crop((100,610,450,900))
        if id=='HangingMourningCloth': sprite=sheet.crop((550,490,910,1024))
        if id=='DamagedFlatShelf': sprite=sheet.crop((930,650,1536,920))
        if id=='EmptyDeepNiche': sprite=sheet.crop((70,135,660,465))
        if id=='RoundUrnWarm': sprite=sheet.crop((680,140,1010,470))
        if id=='RoundUrnPale': sprite=sheet.crop((1080,140,1410,470))
        if id=='RoundWellRim': sprite=sheet.crop((25,0,500,440))
        if id=='RoundLiftDeck': sprite=sheet.crop((570,0,1020,455))
        if id=='AltarRunner': sprite=sheet.crop((1120,0,1470,540))
        if id=='MainRootStem': sprite=sheet.crop((0,440,480,1024))
        if id=='WetFoundation': sprite=sheet.crop((480,600,1080,850))
        if id=='ErodedBase': sprite=sheet.crop((1090,530,1536,970))
        if id=='DamagedRecessDeep': sprite=sprite.crop((0,0,sprite.width,440))
        if id=='FuneraryBase': sprite=sheet.crop((550,490,980,925))
        if id=='GatheredBones': sprite=sprite.crop((40,0,sprite.width,sprite.height))
        if id=='RootBranches': sprite=sprite.crop((50,0,sprite.width,500))
        if id=='BoneCasketDeep': sprite=sprite.crop((0,80,sprite.width,sprite.height))
        if id=='PulleyAssembly': sprite=sprite.crop((0,140,sprite.width,sprite.height))
        # Low-alpha contact shading can bleed from the next sheet row. It must
        # not become part of the footprint and squash the visible object.
        threshold=48 if id in ('RootBranches','FineRoots','FineDust','MineralStain') else 128
        box=sprite.getchannel('A').point(lambda a:255 if a>threshold else 0).getbbox()
        if not box: box=sprite.getchannel('A').point(lambda a:255 if a>8 else 0).getbbox()
        if not box: raise ValueError('Empty painted sprite: '+id)
        sprite=sprite.crop(box)
        if max(sprite.size)>512: sprite.thumbnail((512,512),Image.Resampling.LANCZOS)
        padded=Image.new('RGBA',(sprite.width+32,sprite.height+32)); padded.paste(sprite,(16,16))
        output=OUT/(id+'.png'); padded.save(output); texture_meta(output); images[id]=sprite
        entry(id,output,(16,16,sprite.width,sprite.height),size,'Decor',order=-3)
        if id=='FuneraryBase':
            entry('LowCoverStone',output,(16,16,sprite.width,sprite.height),(2.12,2.12),'Bases',order=-4)
            images['LowCoverStone']=sprite
            entry('MechanismPedestal',output,(16,16,sprite.width,sprite.height),(1.5,1.5),'Bases',order=-4)
            images['MechanismPedestal']=sprite

for source,id,size in (
    ('PlacaGravada-v2','InscribedMemorialPlaque',(2.8,1.2)),
    ('RastroPassagem-v1','DustyBootTrail',(5.3,1.1)),
    ('MemorialRitual-v1','RitualMemorialBay',(3,1.2)),
    ('LinhoPendente-v1','DrapedLinen',(1.5,1.4)),
    ('CorrenteSuspensa-v1','LongAnchorChain',(.65,1.8)),
    ('GradeRecolhida-v1','RecollectedGrille',(2.5,1.45)),
    ('CordaLigacao-v1','PulleyRopeArc',(1.5,.35))):
    image=Image.open(ART/'Fontes'/(source+'.png')).convert('RGBA')
    pixels=np.array(image);rgb=pixels[:,:,:3]
    matte=((rgb[:,:,0]>180)&(rgb[:,:,1]<100)&(rgb[:,:,2]<100))|((rgb[:,:,0]>180)&(rgb[:,:,1]>170)&(rgb[:,:,2]<60))
    pixels[:,:,3][matte]=0;image=Image.fromarray(pixels)
    box=image.getchannel('A').point(lambda a:255 if a>96 else 0).getbbox();image=image.crop(box);image.thumbnail((768,768),Image.Resampling.LANCZOS)
    path=OUT/(id+'.png');image.save(path);texture_meta(path);images[id]=image
    entry(id,path,(0,0,image.width,image.height),size,'Decor',order=-3)
rust=Image.open(OUT/'RustAtFixing.png').convert('RGBA');rust=rust.crop((0,round(rust.height*.4),rust.width,rust.height))
path=OUT/'RustTrail.png';rust.save(path);texture_meta(path);images['RustTrail']=rust
entry('RustTrail',path,(0,0,rust.width,rust.height),(.7,1.8),'Decor',order=-6)

grate=Image.open(ART/'Fontes/BaseCisterna-v1.png').convert('RGBA')
box=grate.getchannel('A').point(lambda a:255 if a>128 else 0).getbbox();grate=grate.crop(box);grate.thumbnail((512,512),Image.Resampling.LANCZOS)
path=OUT/'StoneLatticeBase.png';grate.save(path);texture_meta(path);images['StoneLatticeBase']=grate
entry('StoneLatticeBase',path,(0,0,grate.width,grate.height),(2.12,2.12),'Bases',kind=3,order=-4)

cabinet=Image.open(ART/'Fontes/CompartimentoVazio-v2.png').convert('RGBA')
box=cabinet.getchannel('A').point(lambda a:255 if a>128 else 0).getbbox()
cabinet=cabinet.crop(box);cabinet.thumbnail((512,512),Image.Resampling.LANCZOS)
path=OUT/'EmptyRitualCabinet.png';cabinet.save(path);texture_meta(path);images['EmptyRitualCabinet']=cabinet
entry('EmptyRitualCabinet',path,(0,0,cabinet.width,cabinet.height),(2.8,1.2),'Decor',order=-3)

trace=Image.open(ART/'Fontes/VestigioCerimonial-v1.png').convert('RGBA')
pixels=np.array(trace);rgb=pixels[:,:,:3]
matte=((rgb[:,:,0]>180)&(rgb[:,:,1]<100)&(rgb[:,:,2]<100))|((rgb[:,:,0]>180)&(rgb[:,:,1]>170)&(rgb[:,:,2]<60))
pixels[:,:,3][matte]=0;trace=Image.fromarray(pixels)
box=trace.getchannel('A').point(lambda a:255 if a>32 else 0).getbbox();trace=trace.crop(box);trace.thumbnail((768,1536),Image.Resampling.LANCZOS)
path=OUT/'MourningFloorTrace.png';trace.save(path);texture_meta(path);images['MourningFloorTrace']=trace
entry('MourningFloorTrace',path,(0,0,trace.width,trace.height),(7,12),'Decor',order=-5)

# Remove only saturated matte contamination from the transparent decal export.
# Stone RGB is retained; no painted detail is synthesized by the packing step.
ruts=Image.open(ART/'Fontes/SulcosTransporte-v1.png').convert('RGBA')
pixels=np.array(ruts);rgb=pixels[:,:,:3]
contamination=((rgb[:,:,0]>180)&(rgb[:,:,1]<100)&(rgb[:,:,2]<100))|((rgb[:,:,0]>180)&(rgb[:,:,1]>170)&(rgb[:,:,2]<60))
pixels[:,:,3][contamination]=0;ruts=Image.fromarray(pixels)
box=ruts.getchannel('A').point(lambda a:255 if a>100 else 0).getbbox();ruts=ruts.crop(box);ruts.thumbnail((1024,256),Image.Resampling.LANCZOS)
path=OUT/'TransportRuts.png';ruts.save(path);texture_meta(path);images['TransportRuts']=ruts
entry('TransportRuts',path,(0,0,ruts.width,ruts.height),(4.24,.85),'FloorMotifs',order=-6)

# Cut a low quay cap from the shared painted limestone. The middle stays open
# over the existing two-cell boarding footprint; this adds no geometry.
cap=images['FuneraryBase'];w,h=cap.size
cap=cap.crop((round(w*.1),round(h*.86),round(w*.9),h)).resize((256,32),Image.Resampling.LANCZOS).rotate(90,expand=True)
quay=Image.new('RGBA',(128,768))
for y in (0,512): quay.alpha_composite(cap,(48,y))
path=OUT/'StoneQuayPier.png';quay.save(path);texture_meta(path);images['StoneQuayPier']=quay
entry('StoneQuayPier',path,(0,0,128,768),(1.06,6.36),'Decor',order=-6)

# Existing interactive art stays tied to the existing network/gameplay identities.
for side,source,box,size in (
    ('Left','Wall_Dry_0D',(85,0,128,128),(.356,1.06)),
    ('Right','Wall_Dry_07',(0,0,43,128),(.356,1.06)),
    ('Middle','Wall_Dry_0B',(0,85,128,128),(1.06,.356))):
    body=images[source].crop(box)
    for turn,direction in enumerate(('N','E','S','W')):
        id=f'DoorFinish_{side}_{direction}'; tile=body.rotate(-90*turn,expand=True)
        output=OUT/(id+'.png'); tile.save(output); texture_meta(output); images[id]=tile
        world=size if turn%2==0 else size[::-1]
        entry(id,output,(0,0,tile.width,tile.height),world,'Portals',direction,order=-3)

breakables=json.loads((ROOT/'Assets/Resources/Environment/Dungeon/Breakables/BreakablesCatalog.json').read_text(encoding='utf-8-sig'))
def persistent_crop(id,path,rect,size,family,state=''):
    im=Image.open(path).convert('RGBA');x,y,w,h=rect
    crop=im.crop((x,im.height-y-h,x+w,im.height-y))
    crop.thumbnail((512,512),Image.Resampling.LANCZOS)
    target=OUT/(id+'.png');crop.save(target);texture_meta(target)
    return entry(id,target,(0,0,crop.width,crop.height),size,family,state)
for a in breakables['art']:
    if a['family']=='Urn' and a['state']=='Intact':
        source=next(e for e in entries if e['id']==('RoundUrnWarm','RoundUrnPale','RoundUrnBrown')[a['variant']-1])
        entry(f"Breakable_Urn_Intact_{a['variant']}",ROOT/source['texture'],tuple(source[k] for k in ('x','y','width','height')),(.9,.9),'BreakableStates','Intact')
        continue
    path=ROOT/'Assets/Resources'/Path(a['resource']+'.png'); im=Image.open(path); r=a['rect']
    rect=(round(r['x']*im.width),round(r['y']*im.height),round(r['width']*im.width),round(r['height']*im.height))
    persistent_crop(f"Breakable_{a['family']}_{a['state']}_{a['variant']}",path,rect,(.9,.9),'BreakableStates',a['state'])
exits=json.loads((ROOT/'Assets/Resources/Environment/Dungeon/Exits/ExtractionsArt.json').read_text(encoding='utf-8-sig'))
for a in exits['assets']:
    path=ROOT/'Assets/Resources'/Path(a['resource']+'.png'); im=Image.open(path)
    x,y=math.floor(a['x']*im.width),math.floor(a['y']*im.height)
    rect=(x,y,min(math.ceil(a['width']*im.width),im.width-x),min(math.ceil(a['height']*im.height),im.height-y))
    persistent_crop(a['id'],path,rect,(1.06,1.06),'ExtractionStates')

labels={'Dry':'seca','Damp':'úmida','Cracked':'rachada','Roots':'raízes','Stone':'lajes amplas','Limestone':'lajes do conceito','Ornament':'ornamental','Ruin':'destruído','Earth':'terra','Mixed':'pedra e terra','Wet':'água'}
for e in entries:
    id=e['id']
    if id.startswith('Wall_'):
        e['label']='Parede '+labels[id.split('_')[1]]+' · '+id.split('_')[2];e['state']=id.split('_')[1];e['sector']='Arquitetura compartilhada'
    elif e['family']=='Floors':
        e['label']=('Transição ' if id.startswith('Transition') else 'Piso ')+('lajes alagadas' if e['state']=='Flooded' else labels.get(e['state'],e['state']))+' · '+id.split('_')[-1];e['sector']='Terreno / Pisos'
    else:
        e['label']=re.sub(r'(?<=[a-z])(?=[A-Z])',' ',id)
        e['sector']='Extrações / Estados' if e['family']=='ExtractionStates' else 'Interativos / Quebráveis' if e['family']=='BreakableStates' else 'Decoração / Peças individuais'
decor_tags={
    'Ossários / Recipientes':('Urn','Niche','Recess','Coffin','Casket','Bones','AshSpill'),
    'Claustro / Vigília':('Votive','Shelf','Candles','Candle','Mourning','DeadWax'),
    'Prisão / Fixações':('Chain','Grille','Straw','Fixing','RustTrail'),
    'Capela / Rito':('Ritual','Altar','Linen','Incense','Violated'),
    'Contrapesos / Ofício':('Pulley','Lid','Chisel','Mortar'),
    'Cisternas / Água':('Mineral','Water','Flood','WetFoundation','Mooring','Driftwood','Sediment','TransportRuts','Quay','Gauge','Drain'),
    'Abrigo / Acampamento':('Abandoned','CookingPot','ColdAsh','Canteen'),
    'Galerias / Memória':('Plaque','Relief','Inlay','Bell'),
    'Deterioração / Materiais':('Root','Dust','Masonry','StoneChips','Eroded')}
for e in entries:
    if e['id'] in ('FuneraryBase','LowCoverStone','LowCoverStoneLegacy','ErodedBase','StoneLatticeBase'): e['physicalKind']=3
    if e['family']!='Decor': continue
    tags=[tag for tag,terms in decor_tags.items() if any(term in e['id'] for term in terms)]
    e['sector']='; '.join(tags) or 'Arquitetura compartilhada'
    e['family']=tags[0].split(' / ')[-1] if tags else 'Bases e acabamentos'
    e['state']='Danificado' if any(t in e['id'] for t in ('Damaged','Broken','Cracked','Violated','Tattered')) else 'Vazio' if 'Empty' in e['id'] else 'Abandonado' if any(t in e['id'] for t in ('Dead','Abandoned','Cold')) else 'Preservado / gasto'
kit=OUT/'CatacombArt.catacombkit'
kit.write_text(json.dumps({'entries':entries},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
(ART/'catalogo-producao-v1.json').write_text(json.dumps({'entries':entries,'kind':'persistent sprite/tile import definitions, not Unity execution'},ensure_ascii=False,indent=2),encoding='utf-8')

# Offline contact sheet of the actual cropped production sprites.
props=[e for e in entries if e['id'] in images and not e['tile'] and e['id']!='LowCoverStone']
board=Image.new('RGB',(1200,math.ceil(len(props)/6)*190+65),'#20231f'); draw=ImageDraw.Draw(board)
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',16)
draw.text((18,16),'CATACUMBAS — ASSETS DE PRODUÇÃO V1',font=font,fill='#e4d9be')
for i,e in enumerate(props):
    sprite=images[e['id']].copy(); sprite.thumbnail((185,140),Image.Resampling.LANCZOS)
    x=(i%6)*200;y=(i//6)*190+60
    board.paste(sprite,(x+(200-sprite.width)//2,y+(140-sprite.height)//2),sprite)
    draw.text((x+8,y+145),e['id'],font=font,fill='#d4cbb8')
board.save(ART/'Assets-producao-v1.png')
print(json.dumps({'catalogEntries':len(entries),'floorTiles':len(tile_images),'wallTiles':188,'paintedProps':len(props),'atlases':atlas_sizes},ensure_ascii=False))
