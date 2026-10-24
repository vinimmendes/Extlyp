"""Standalone fishing motion study; preserves all existing Blender scenes."""
import bpy, math, random
from pathlib import Path

ROOT = Path('C:/Users/Vinni/Documents/GitHub/Extlyp')
OUT = ROOT / 'Art/Fishing/Preview-v01'
OUT.mkdir(parents=True, exist_ok=True)
if bpy.data.scenes.get('Fishing-Preview-v01c'):
    raise RuntimeError('Preview scene already exists; preserve it and choose a new version.')
scene = bpy.data.scenes.new('Fishing-Preview-v01c')
bpy.context.window.scene = scene
scene.render.engine = 'BLENDER_EEVEE'
scene.render.resolution_x = 960
scene.render.resolution_y = 540
scene.render.resolution_percentage = 100
scene.render.fps = 12
scene.frame_start, scene.frame_end = 1, 96
scene.render.image_settings.file_format = 'PNG'
scene.render.filepath = str(OUT / 'frames/frame_')
scene.view_settings.view_transform = 'Standard'
world = bpy.data.worlds.new('WORLD-FishingPreview')
world.use_nodes = True
world.node_tree.nodes.clear()
bg=world.node_tree.nodes.new('ShaderNodeBackground')
bg.inputs[0].default_value = (.015,.02,.016,1)
wo=world.node_tree.nodes.new('ShaderNodeOutputWorld')
world.node_tree.links.new(bg.outputs[0],wo.inputs[0])
scene.world = world

def mat(name, color):
    m=bpy.data.materials.new('MAT-Fishing-'+name); m.diffuse_color=(*color,1); m.use_nodes=True
    n=m.node_tree.nodes; n.clear()
    e=n.new('ShaderNodeEmission'); e.name='Emission'; e.inputs[0].default_value=(*color,1)
    o=n.new('ShaderNodeOutputMaterial'); m.node_tree.links.new(e.outputs[0],o.inputs[0])
    return m
panel=mat('Panel',(.021,.028,.021)); copper=mat('Copper',(.32,.22,.12))
ivory=mat('Ivory',(.78,.73,.59)); muted=mat('Muted',(.40,.44,.34))
green=mat('Moss',(.24,.40,.19)); danger=mat('Warning',(.65,.21,.14))
water=mat('Water',(.025,.09,.09)); ripple=mat('Ripple',(.09,.20,.19))
fishmat=mat('SubmergedFish',(.055,.135,.125)); finmat=mat('FishFin',(.045,.115,.11))
red=mat('BobberRed',(.72,.16,.10)); cream=mat('BobberIvory',(.80,.75,.59))
wood=mat('Wood',(.17,.105,.057)); grain=mat('Grain',(.095,.063,.036))
water.node_tree.nodes.get('Emission').inputs[0].default_value=(.025,.09,.09,1)
n=water.node_tree.nodes; noise=n.new('ShaderNodeTexNoise'); noise.inputs['Scale'].default_value=6
noise.inputs['Detail'].default_value=2
ramp=n.new('ShaderNodeValToRGB'); ramp.color_ramp.elements[0].color=(.018,.054,.054,1)
ramp.color_ramp.elements[1].color=(.035,.105,.10,1)
water.node_tree.links.new(noise.outputs['Fac'],ramp.inputs[0])
water.node_tree.links.new(ramp.outputs[0],n.get('Emission').inputs[0])

def mesh(name, verts, faces, material):
    d=bpy.data.meshes.new(name); d.from_pydata(verts,[],faces); d.update()
    o=bpy.data.objects.new('GEO-'+name,d); scene.collection.objects.link(o); d.materials.append(material); return o
def rect(name,x,y,w,h,z,material):
    return mesh(name,[(x,y,z),(x+w,y,z),(x+w,y+h,z),(x,y+h,z)],[(0,1,2,3)],material)
def path(name,coords,material,width=.012,cyclic=False):
    d=bpy.data.curves.new(name,'CURVE'); d.dimensions='3D'; d.bevel_depth=width; d.bevel_resolution=1
    s=d.splines.new('POLY'); s.points.add(len(coords)-1)
    for p,co in zip(s.points,coords): p.co=(*co,1)
    s.use_cyclic_u=cyclic
    o=bpy.data.objects.new('GEO-'+name,d); scene.collection.objects.link(o); d.materials.append(material); return o
def text(name,body,x,y,size,material=ivory):
    d=bpy.data.curves.new(name,'FONT'); d.body=body; d.size=size; d.space_character=1.1
    o=bpy.data.objects.new('TXT-'+name,d); scene.collection.objects.link(o); o.location=(x,y,.65); d.materials.append(material); return o
def border(name,x,y,w,h,z,material,width=.012):
    return path(name,[(x,y,z),(x+w,y,z),(x+w,y+h,z),(x,y+h,z)],material,width,True)
rect('Background',-9,-5,18,10,-.1,panel)
border('Frame',-7.8,-3.95,15.6,7.9,.1,copper,.018)
text('Title','P E S C A',-7.3,3.35,.38)
text('Subtitle','ESTUDO DE MOVIMENTO  /  BOIA E PEIXE',-3.8,3.39,.18,muted)
path('Header',[(-7.3,3.08,.2),(7.3,3.08,.2)],copper)
rect('Water',-7.3,-2.9,9.25,5.65,0,water)
border('WaterFrame',-7.3,-2.9,9.25,5.65,.1,copper)
for i in range(12):
    x=-7.3+i*.775
    rect('Dock%02d'%i,x,-2.9,.75,.7,.25,wood)
    for j in range(3):
        path('Grain%02d-%d'%(i,j),[(x+.12,-2.78+j*.19,.27),(x+.58,-2.75+j*.19,.27)],grain,.009)
path('Divider',[(2.3,-3.4,.3),(2.3,2.75,.3)],copper)
text('Tension','TENSAO DA LINHA',2.75,1.25,.21)
rect('TensionBack',2.75,.64,4.3,.31,.3,panel)
border('TensionBorder',2.75,.64,4.3,.31,.32,copper)
tension=rect('TensionFill',0,0,1,.27,.34,green); tension.location=(2.77,.66,0)
rect('DangerZone',6.3,.66,.73,.27,.36,danger)
marker=rect('TensionMarker',0,0,.035,.43,.4,ivory)
text('Progress','APROXIMACAO',2.75,-.05,.21)
border('ProgressBorder',2.75,-.53,4.3,.22,.3,copper)
progress=rect('ProgressFill',0,0,1,.18,.34,green); progress.location=(2.77,-.51,0)
rect('Button',2.75,-1.85,4.3,.85,.3,wood)
border('ButtonBorder',2.75,-1.85,4.3,.85,.4,copper,.02)
text('Button','RECOLHER',3.68,-1.55,.32)
text('Help','Segure para recolher\nSolte para aliviar',3.10,-2.35,.22)
text('Footer','PREVIA BLENDER  /  NAO INTEGRADA AO JOGO',-7.3,-3.55,.17,muted)
labels=[]
for name,body in [('Wait','AGUARDE A MORDIDA'),('Nibble','BELISCANDO...'),('Bite','FISGUE!'),('Pull','CEDA LINHA!'),('Reel','RECOLHA')]:
    labels.append(text(name,body,2.75,2.22,.30,danger if name=='Pull' else ivory))

rig=bpy.data.objects.new('ARM-FishMotion',None); scene.collection.objects.link(rig)
verts=[]; faces=[]
for x,w in [(-.60,.025),(-.40,.11),(-.12,.21),(.23,.20),(.48,.12),(.59,.025)]:
    for j in range(12):
        a=2*math.pi*j/12; verts.append((x,w*math.cos(a),w*.32*math.sin(a)))
for i in range(5):
    for j in range(12): faces.append((i*12+j,i*12+(j+1)%12,(i+1)*12+(j+1)%12,(i+1)*12+j))
faces.extend([tuple(range(11,-1,-1)),tuple(range(60,72))])
body=mesh('FishBody',verts,faces,fishmat); body.parent=rig
body.shape_key_add(name='Basis'); bend=body.shape_key_add(name='SwimBend')
for v in bend.data: v.co.y+=.16*max(0,-v.co.x/.6)**2
tail=mesh('FishTail',[(-.48,0,0),(-.94,.23,0),(-.86,0,0),(-.94,-.23,0)],[(0,1,2,3)],finmat); tail.parent=rig
fin=mesh('FishFin',[(0,.11,.01),(-.20,.34,.01),(.28,.17,.01)],[(0,1,2)],finmat); fin.parent=rig
eye=path('FishEye',[(.41,.08,.08),(.43,.08,.08)],muted,.018); eye.parent=rig
floatrig=bpy.data.objects.new('ARM-BobberMotion',None); scene.collection.objects.link(floatrig)
for name,yy,mm in [('Lower',-.05,cream),('Upper',.05,red)]:
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,radius=1)
    o=bpy.context.object; o.name='GEO-Bobber'+name; o.parent=floatrig; o.location=(0,yy,0); o.scale=(.09,.065,.055); o.data.materials.append(mm)
stem=path('BobberStem',[(0,.07,0),(0,.20,0)],cream,.014); stem.parent=floatrig
line=path('Line',[(-6.6,-2.3,.33),(-3,.2,.33),(-2,.5,.33)],ivory,.007)
rings=[]
for j in range(4):
    rings.append(path('Ripple%d'%j,[(math.cos(k*math.tau/48),math.sin(k*math.tau/48)*.65,.03) for k in range(48)],ripple,.009,True))

for f in range(1,97):
    t=(f-1)/12
    struggle=49<=f<=84; bite=37<=f<=48; nibble=25<=f<=36
    bx=-2.8 + (.36*math.sin(t*8) if struggle else .035*math.sin(t*2))
    by=.35 + (.17*math.sin(t*6) if struggle else .035*math.sin(t*2.5))
    floatrig.location=(bx,by,.2)
    floatrig.rotation_euler=(0,0,.13*math.sin(t*3) if not nibble else .5*math.sin(t*18))
    s=1 if not bite else max(.12,1-(f-37)/9)
    floatrig.scale=(s,s,s)
    for prop in ['location','rotation_euler','scale']: floatrig.keyframe_insert(prop,frame=f)
    rig.location=(bx+.9+.24*math.sin(t*2),by-.45+.1*math.cos(t*3),.07)
    rig.rotation_euler=(0,0,.15*math.sin(t*3)+(.22*math.sin(t*9) if struggle else 0))
    for prop in ['location','rotation_euler']: rig.keyframe_insert(prop,frame=f)
    bend.value=.5+.5*math.sin(t*(18 if struggle else 7)); bend.keyframe_insert('value',frame=f)
    tail.rotation_euler.z=.32*math.sin(t*(18 if struggle else 7)); tail.keyframe_insert('rotation_euler',frame=f)
    for j,r in enumerate(rings):
        phase=(t*.65+j*.25)%1
        r.location=(bx,by,0); r.scale=(.2+phase*.6,.2+phase*.6,1)
        r.keyframe_insert('location',frame=f); r.keyframe_insert('scale',frame=f)
    pts=line.data.splines[0].points
    for p,co in zip(pts,[(-6.6,-2.3,.33),((-6.6+bx)/2,(by-2.3)/2+(.02 if struggle else -.18),.33),(bx,by,.33)]):
        p.co=(*co,1); p.keyframe_insert('co',frame=f)
    value=.28 if not struggle else .55+.32*math.sin((f-49)*.3)
    active=3 if struggle and value>.70 else 4 if struggle else 2 if bite else 1 if nibble else 0
    for i,label in enumerate(labels):
        label.hide_render=i!=active; label.keyframe_insert('hide_render',frame=f)
        label.scale=(1,1,1) if i==active else (0,0,0); label.keyframe_insert('scale',frame=f)
    value=.28 if not struggle else .55+.32*math.sin((f-49)*.3)
    tension.scale.x=value*4.24; tension.keyframe_insert('scale',frame=f)
    marker.location=(2.77+value*4.24,.60,0); marker.keyframe_insert('location',frame=f)
    progress.scale.x=(.15 if f<49 else .15+.70*min(1,(f-49)/35))*4.24; progress.keyframe_insert('scale',frame=f)

camdata=bpy.data.cameras.new('CAM-FishingPreview'); camera=bpy.data.objects.new('CAM-FishingPreview',camdata)
scene.collection.objects.link(camera); camera.location=(0,0,20); camdata.type='ORTHO'; camdata.ortho_scale=16.5; scene.camera=camera
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        area.spaces.active.region_3d.view_perspective='CAMERA'
        area.spaces.active.shading.type='MATERIAL'
scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'fishing-preview-v01.blend'))
result={'ok':True,'scene':scene.name,'blend':str(OUT/'fishing-preview-v01.blend'),'frames':96,'fps':12,'objects':len(scene.objects)}
