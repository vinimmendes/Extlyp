"""Package painted sources into exact modules and the R17 composition.

Asset authoring only: no Unity, build, gameplay probe or automated tests.
ImageGen paints all material/object sources; this script only slices, samples,
blends surface transitions, extrudes atlas gutters and composes documentation.
"""
from pathlib import Path
import json
import math
import re
import uuid
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'Art/Catacumbas/ReworkPiloto'
OUT = ROOT / 'Assets/Resources/Environment/Dungeon/ReworkPilot'
DOC = ROOT / 'Docs/Mapas/ReworkPiloto'
RESOURCE = 'Environment/Dungeon/ReworkPilot/'
SIZE, PAD, PITCH = 128, 2, 132
assets, images = [], {}


def meta(path, texture=False):
    dest = Path(str(path) + '.meta')
    if dest.exists():
        return
    header = 'fileFormatVersion: 2\nguid: ' + uuid.uuid4().hex + '\n'
    if texture:
        header += '''TextureImporter:
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
'''
    else:
        header += 'TextScriptImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n'
    dest.write_text(header, encoding='utf-8')


def save(image, name):
    path = OUT / (name + '.png')
    image.save(path)
    meta(path, True)


def register(name, texture, rect, image):
    assets.append(dict(id=name, resource=RESOURCE + texture, x=rect[0], y=rect[1], width=rect[2], height=rect[3]))
    images[name] = image


def smooth(t):
    t = np.clip(t, 0, 1)
    return t * t * (3 - 2 * t)


def periodic(image, size=512):
    """Repair opposite sample edges, preserving the painted interior."""
    a = np.asarray(image.convert('RGB').resize((size, size), Image.Resampling.LANCZOS)).astype(np.float32)
    band = 32
    for axis in (0, 1):
        source = a.copy()
        for i in range(band):
            weight = .5 * (1 - i / band) ** 2
            if axis == 0:
                a[i] = source[i] * (1-weight) + source[-1-i] * weight
                a[-1-i] = source[-1-i] * (1-weight) + source[i] * weight
            else:
                a[:, i] = source[:, i] * (1-weight) + source[:, -1-i] * weight
                a[:, -1-i] = source[:, -1-i] * (1-weight) + source[:, i] * weight
    return a


def quadrant(image, x, y):
    w, h = image.size
    return image.crop((x*w//2, y*h//2, (x+1)*w//2, (y+1)*h//2))


def normalize(mask):
    value = mask & 15
    for i in range(4):
        if mask & (1 << i) and mask & (1 << ((i+1) % 4)) and mask & (1 << (i+4)):
            value |= 1 << (i+4)
    return value


def atlas(tiles, name, columns):
    rows = math.ceil(len(tiles) / columns)
    sheet = Image.new('RGBA', (columns*PITCH, rows*PITCH))
    for i, (identifier, tile) in enumerate(tiles):
        # Extrude the actual edge pixels, including corners, into the gutter.
        a = np.pad(np.asarray(tile.convert('RGBA')), ((PAD, PAD), (PAD, PAD), (0, 0)), mode='edge')
        left, top = (i % columns)*PITCH, (i // columns)*PITCH
        sheet.paste(Image.fromarray(a), (left, top))
        register(identifier, name, (left+PAD, sheet.height-top-PAD-SIZE, SIZE, SIZE), tile)
    save(sheet, name)
    return sheet


for folder in (ART, OUT, DOC):
    folder.mkdir(parents=True, exist_ok=True)

materials = Image.open(ART / 'Fontes/Materiais.png')
weather = Image.open(ART / 'Fontes/EstadosAgua.png')
stone, soil, roots = (periodic(quadrant(materials, x, y)) for x,y in ((1,0),(0,1),(1,1)))
water = periodic(quadrant(weather, 1, 1))

# The room is 18x16 cells. Every tile samples one shared continuous surface
# field; cutting it into sprites cannot change where one material meets another.
width, height = 18*SIZE, 16*SIZE
yy, xx = np.mgrid[0:height, 0:width]
cx, cy = (xx+.5)/SIZE, 16-(yy+.5)/SIZE
edge = smooth(np.minimum.reduce([cx,18-cx,cy,16-cy]) / 1.25)
earth_mask = np.maximum(smooth((3.7-np.hypot((cx-3.8)*.85,cy-3.3))/1.1),
                        smooth((3.0-np.hypot(cx-13.1,(cy-13.5)*1.3))/1.0)) * edge
root_mask = smooth((5.2-np.hypot((cx-1.5)*1.25,cy-13.8))/1.0) * edge
wet_mask = smooth((2.1-np.hypot((cx-15.2)*.8,(cy-3.0)*1.5))/0.8) * edge
surface = stone[yy % 512, xx % 512].copy()
for texture, mask in ((soil,earth_mask),(roots,root_mask)):
    surface = surface*(1-mask[:,:,None]) + texture[yy % 512,xx % 512]*mask[:,:,None]
floor_image = Image.fromarray(surface.clip(0,255).astype('uint8')).convert('RGBA')
floor_cells, floor_tiles = [], []
for y in range(16):
    for x in range(18):
        name = f'CRP_Floor_R17_{x:02}_{y:02}'
        tile = floor_image.crop((x*SIZE,(15-y)*SIZE,(x+1)*SIZE,(16-y)*SIZE))
        floor_tiles.append((name,tile))
        floor_cells.append(dict(art=name,x=11+x,y=36+y))
atlas(floor_tiles, 'CRP_FloorAtlas', 18)
# Shallow water stays a separate editable transparent layer, never a water mask.
wet_pixels=np.empty((height,width,4),dtype='uint8')
wet_pixels[:,:,:3]=water[yy%512,xx%512].clip(0,255).astype('uint8')
wet_pixels[:,:,3]=(wet_mask*.8*255).astype('uint8')
wet_image=Image.fromarray(wet_pixels)
water_tiles,water_cells=[],[]
for y in range(16):
    for x in range(18):
        tile=wet_image.crop((x*SIZE,(15-y)*SIZE,(x+1)*SIZE,(16-y)*SIZE))
        if tile.getchannel('A').getbbox() is None:
            continue
        name=f'CRP_ShallowWater_R17_{x:02}_{y:02}'
        water_tiles.append((name,tile)); water_cells.append(dict(art=name,x=11+x,y=36+y))
atlas(water_tiles,'CRP_WaterAtlas',8)

# Dark solid tops and a broad raised coping distinguish walls from paving.
# All relief remains INSIDE the same solid cell; no visual passage is closed.
wall_mass = Image.open(ART / 'Fontes/MacicoParede-v2.png')
coping = periodic(quadrant(materials,0,0).crop((70,70,454,454)), SIZE)
state_sources = [periodic(quadrant(wall_mass,x,y),SIZE) for x,y in ((0,0),(1,0),(0,1),(1,1))]
dry = state_sources[0]
py, px = np.mgrid[0:SIZE,0:SIZE]
border = smooth(np.minimum.reduce([px,127-px,py,127-py]) / 20)
masks = [m for m in range(256) if normalize(m)==m]
wall_entries, wall_tiles = [], []
for state, source in zip(('Dry','Damp','Cracked','Roots'),state_sources):
    paint = dry*(1-border[:,:,None]) + source*border[:,:,None]
    for mask in masks:
        distance = np.full((SIZE,SIZE),1000.,dtype=float)
        for bit, d in enumerate((py,127-px,127-py,px)):
            if not mask & (1 << bit):
                distance = np.minimum(distance,d)
        for i,(dx,dy) in enumerate(((127-px,py),(127-px,127-py),(px,127-py),(px,py))):
            if mask & (1<<i) and mask & (1<<((i+1)%4)) and not mask & (1<<(i+4)):
                distance = np.minimum(distance,np.hypot(dx,dy))
        # Contact seam, recessed face, bevel, broad light coping, inner recess,
        # then quiet charcoal mass. This is architectural relief, not a floor tint.
        rim = coping*.7 + np.array([79,78,73])
        pixels = paint*.86
        pixels = np.where((distance<36)[:,:,None],rim*.30,pixels)
        pixels = np.where((distance<30)[:,:,None],rim*.94,pixels)
        pixels = np.where((distance<17)[:,:,None],rim*1.12,pixels)
        pixels = np.where((distance<11)[:,:,None],rim*.53,pixels)
        pixels = np.where((distance<4)[:,:,None],rim*.18,pixels)
        tile = Image.fromarray(pixels.clip(0,255).astype('uint8')).convert('RGBA')
        name = f'CRP_Wall_{state}_{mask:02X}'
        wall_tiles.append((name,tile)); wall_entries.append(dict(state=state,mask=mask,art=name))
atlas(wall_tiles,'CRP_WallAtlas',8)
# Low cover keeps its original lighter, shallow stone support. It must not inherit
# the tall wall's dark mass just because both formerly reused mask 00.
cover_distance=np.minimum.reduce([px,127-px,py,127-py])
cover_shade=np.where(cover_distance<2,.54,np.where(cover_distance<5,1.18,np.where(cover_distance<18,1.06,np.where(cover_distance<22,.54,.72))))
cover=Image.fromarray((coping*cover_shade[:,:,None]).clip(0,255).astype('uint8')).convert('RGBA')
save(cover,'CRP_LowCoverStone')
register('CRP_LowCoverStone','CRP_LowCoverStone',(0,0,SIZE,SIZE),cover)

# Preserve generated alpha; separate cells mechanically, with fixed padding.
prop_names = [
    ('Funerarios',('CoffinClosed','CoffinOpen','CoffinBroken','UrnIntact','UrnBroken','Skeleton')),
    ('Acampamento',('FireAsh','Pot','ShovelBroken','RopeBroken','Equipment','Niche')),
]
for source_name, names in prop_names:
    sheet = Image.open(ART / f'Fontes/{source_name}.png').convert('RGBA')
    for i,name in enumerate(names):
        left,top = (i%3)*sheet.width//3,(i//3)*sheet.height//2
        sprite = sheet.crop((left,top,left+sheet.width//3,top+sheet.height//2))
        box = sprite.getchannel('A').point(lambda a: 255 if a>8 else 0).getbbox()
        if box is None:
            raise ValueError('Empty sprite cell: '+name)
        sprite = sprite.crop(box)
        # Body stays at the generated resolution; transparent margin is not footprint.
        padded = Image.new('RGBA',(sprite.width+32,sprite.height+32))
        padded.paste(sprite,(16,16))
        identifier = 'CRP_'+name
        save(padded,identifier)
        register(identifier,identifier,(16,16,sprite.width,sprite.height),sprite)


def piece(identifier, art, x,y,w,h, support='Floor',order=-4,angle=0,group='Camp'):
    return dict(id=identifier,art='CRP_'+art,x=x,y=y,width=w,height=h,support=support,order=order,angle=angle,group=group)


pieces = [
    piece('WestTomb','CoffinClosed',15,41,1.8,1.1,'Cover',-3,group='Burials'),
    piece('EastTomb','CoffinOpen',25,47,1.85,1.65,'Cover',-3,group='Burials'),
    piece('DestroyedBurial','CoffinBroken',25.7,35.5,2.6,.9,'Stone',-3,group='BrokenBurial'),
    piece('NorthNicheWest','Niche',14,52.5,3,.9,'Stone',-4,group='UrnNiches'),
    piece('NorthNicheEast','Niche',25.2,52.5,3,.9,'Stone',-4,group='UrnNiches'),
    piece('AshUrnWest','UrnIntact',13.35,52.5,.5,.5,'Stone',-3,group='UrnNiches'),
    piece('AshUrnEast','UrnIntact',24.55,52.5,.5,.5,'Stone',-3,group='UrnNiches'),
    piece('BrokenAshUrn','UrnBroken',25.8,52.5,.65,.6,'Stone',-3,group='UrnNiches'),
    piece('OldSkeleton','Skeleton',13.3,38.3,1.75,.85,order=-5,angle=90,group='Burials'),
    piece('ExtinguishedFire','FireAsh',24.0,49.95,1.45,1.45,order=-5),
    piece('CookingPot','Pot',25.35,49.95,.6,.6),
    piece('TornEquipment','Equipment',23.8,51.15,.85,.95,angle=90),
    piece('PartedRope','RopeBroken',25.2,48.65,.85,.5,order=-5),
    piece('BrokenShovel','ShovelBroken',27.1,37.3,1.35,.48,order=-5,group='BrokenBurial'),
    piece('ScatteredCeramic','UrnBroken',27.0,38.35,.65,.6,order=-5,group='BrokenBurial'),
]
manifest = dict(version=1,room=17,cellSize=1.06,assets=assets,floors=floor_cells,water=water_cells,walls=wall_entries,pieces=pieces)
(OUT/'Pilot.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
meta(OUT/'Pilot.json')

# Static drawing of the CURRENT authored room and entrances. No runtime capture.
source = (ROOT/'Assets/Scripts/Match/DungeonLayout.cs').read_text(encoding='utf-8-sig')
room_data = [(name,*map(int,v)) for name,*v in re.findall(r'new Room\("([^"]+)",(\d+),(-?\d+),(-?\d+),(\d+),(\d+)\)',source)]
links = [tuple(map(int,v)) for v in re.findall(r'\{(\d+),(\d+)\}',source.split('Links={')[1].split('};')[0])]
dry_cells, covers = set(),set()
def rect_cells(x,y,w,h):
    return {(a,b) for b in range(y,y+h) for a in range(x,x+w)}
for _,_,x,y,w,h in room_data:
    dry_cells |= rect_cells(x-w//2+64,y-h//2+64,w,h)
for a,b in links:
    ax,ay=room_data[a][2:4]; bx,by=room_data[b][2:4]
    mx,my=(ax,by) if a<8 else (bx,ay)
    for x1,y1,x2,y2 in ((ax,ay,mx,my),(mx,my,bx,by)):
        dry_cells |= rect_cells(min(x1,x2)+62,min(y1,y2)+62,abs(x1-x2)+4,abs(y1-y2)+4)
for _,_,x,y,_,_ in room_data[8:]:
    for ox,oy in ((-6,-4),(4,2)):
        obstacle = rect_cells(x+64+ox,y+64+oy,2,2)
        dry_cells -= obstacle
        covers |= obstacle
def stone_cell(x,y):
    return (x,y) not in dry_cells and (x,y) not in covers
def wall_mask(x,y):
    return normalize(sum(1<<i for i,(dx,dy) in enumerate(((0,1),(1,0),(0,-1),(-1,0),(1,1),(1,-1),(-1,-1),(-1,1))) if stone_cell(x+dx,y+dy)))
def wall_state(x,y):
    return 'Roots' if x<15 and y>44 else 'Damp' if x>22 and y<38 else 'Cracked' if (x+3*y)%9==0 else 'Dry'

preview = Image.new('RGBA',(20*SIZE,18*SIZE),(26,28,28,255))
for y in range(35,53):
    for x in range(10,30):
        if stone_cell(x,y):
            tile = images[f'CRP_Wall_{wall_state(x,y)}_{wall_mask(x,y):02X}']
        elif 11<=x<29 and 36<=y<52:
            tile = images[f'CRP_Floor_R17_{x-11:02}_{y-36:02}']
        else:
            tile = images['CRP_Floor_R17_09_08']
        preview.alpha_composite(tile,((x-10)*SIZE,(52-y)*SIZE))
for cell in water_cells:
    preview.alpha_composite(images[cell['art']],((cell['x']-10)*SIZE,(52-cell['y'])*SIZE))
# Existing two low-cover footprints remain explicit stone supports under coffins.
for x,y in ((14,40),(24,46)):
    slab = images['CRP_LowCoverStone'].resize((256,256),Image.Resampling.LANCZOS)
    preview.alpha_composite(slab,((x-10)*SIZE,(53-y-2)*SIZE))
for p in sorted(pieces,key=lambda p:p['order']):
    image=images[p['art']]
    scale=min(p['width']*SIZE/image.width,p['height']*SIZE/image.height)
    image=image.resize((max(1,round(image.width*scale)),max(1,round(image.height*scale))),Image.Resampling.LANCZOS)
    if p['angle']:
        image=image.rotate(p['angle'],expand=True,resample=Image.Resampling.BICUBIC)
    preview.alpha_composite(image,(round((p['x']-10)*SIZE-image.width/2),round((53-p['y'])*SIZE-image.height/2)))
preview.convert('RGB').save(ART/'OssarioInferior-composicao.png')

font_path=Path('C:/Windows/Fonts/segoeui.ttf')
def font(size):
    return ImageFont.truetype(str(font_path),size) if font_path.exists() else ImageFont.load_default()
board=Image.new('RGB',(1800,1420),'#171b1c'); draw=ImageDraw.Draw(board)
draw.text((40,24),'CATACUMBAS / OSSÁRIO INFERIOR',font=font(32),fill='#e3d5b7')
draw.text((40,72),'Prancha de acabamento • arte final do piloto • 23/10/2026',font=font(20),fill='#b2aaa0')
scene=preview.convert('RGB'); scene.thumbnail((1160,1150),Image.Resampling.LANCZOS)
board.paste(scene,(30,130))
draw.text((40,1320),'Montagem estática dos sprites e da geometria autorada. Não é captura da Unity.',font=font(20),fill='#c1b7a4')
draw.text((40,1355),'Acessos, coberturas e regras existentes preservados. Avaliação prática pendente.',font=font(20),fill='#c1b7a4')
labels=[('CoffinClosed','Caixão preservado'),('CoffinOpen','Caixão aberto'),('CoffinBroken','Caixão destruído'),('UrnIntact','Urna de cinzas'),('UrnBroken','Urna quebrada'),('Skeleton','Esqueleto'),('FireAsh','Fogueira apagada'),('Pot','Panela velha'),('ShovelBroken','Pá quebrada'),('RopeBroken','Corda partida'),('Equipment','Equipamento gasto'),('Niche','Nicho funerário')]
for i,(name,label) in enumerate(labels):
    x,y=1210+(i%2)*280,135+(i//2)*190
    draw.rounded_rectangle((x,y,x+260,y+175),radius=8,fill='#242a2b')
    image=images['CRP_'+name].copy(); image.thumbnail((225,128),Image.Resampling.LANCZOS)
    board.paste(image,(x+(260-image.width)//2,y+8+(128-image.height)//2),image)
    draw.text((x+12,y+144),label,font=font(17),fill='#d1c6ad')
board.save(ART/'Prancha-acabamento.png')

# Catalog of normalized masks, showing actual atlas crops rather than diagrams.
contact=Image.new('RGB',(8*150,24*165),'#202526'); brush=ImageDraw.Draw(contact)
for i,(name,image) in enumerate(wall_tiles):
    x,y=(i%8)*150,(i//8)*165
    contact.paste(image,(x+11,y+3),image)
    brush.text((x+5,y+135),name.replace('CRP_Wall_',''),font=font(13),fill='#d6cbb4')
contact.save(ART/'Paredes-47x4.png')

previous=ART/'Historico/v1/OssarioInferior-composicao.png'
if previous.exists():
    comparison=Image.new('RGB',(1420,760),'#171b1c')
    label=ImageDraw.Draw(comparison)
    for i,(path,title) in enumerate(((previous,'ANTES'),(ART/'OssarioInferior-composicao.png','REVISÃO: PAREDE COM VOLUME'))):
        sample=Image.open(path).convert('RGB')
        sample.thumbnail((670,630),Image.Resampling.LANCZOS)
        x=25+i*710
        label.text((x,20),title,font=font(23),fill='#dfd4bc')
        comparison.paste(sample,(x,75))
    label.text((25,700),'Montagem estática. Maciço escuro, borda elevada e sombra de contato; geometria preservada.',font=font(20),fill='#c1b7a4')
    comparison.save(ART/'Comparacao-paredes-v2.png')

catalog=dict(stage='Pilot only; expansion waits for user feedback',room=17,pieces=pieces,
             counts=dict(props=12,wallMasks=47,wallStates=4,floorCells=288,waterCells=len(water_cells)),
             note='Pilot floor is one continuous painted-material composition sliced into editable cells. Full reusable floor families remain stage 2.')
(DOC/'catalogo.json').write_text(json.dumps(catalog,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
# Reusable recipes: local cell offsets, fixed pivot, art, support and order.
# These are authoring recipes, not extra runtime objects or new interactions.
recipes=[]
for group in dict.fromkeys(p['group'] for p in pieces):
    members=[p for p in pieces if p['group']==group]
    pivot=members[0]
    recipes.append(dict(id=group,pivot=dict(x=pivot['x'],y=pivot['y']),
        pieces=[dict(p, x=round(p['x']-pivot['x'],4),y=round(p['y']-pivot['y'],4)) for p in members]))
(DOC/'composicoes.json').write_text(json.dumps(dict(units='cells',recipes=recipes),ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('Authored 12 separate props, 47 x 4 wall samples, 288 floor cells, 15 placed objects and two presentation boards.')
