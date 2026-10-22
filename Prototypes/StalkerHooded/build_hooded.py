"""Single hooded Stalker study. Actual open cloth mesh, mantle and posed original rig."""
import bpy,math,random
from pathlib import Path
from mathutils import Vector,Matrix
BASE=Path(__file__).resolve().parent; random.seed(83)
bpy.ops.wm.open_mainfile(filepath=str(BASE.parent/'StalkerCrawler/espreitador-rastejante.blend'))
scene=bpy.context.scene; scene.frame_set(120); bpy.context.view_layer.update()
for o in scene.objects:
    if o.animation_data: o.animation_data_clear()
scene.timeline_markers.clear(); scene.frame_start=1; scene.frame_end=1; scene.frame_set(1)
def parent_keep(o,parent):
    bpy.context.view_layer.update(); m=o.matrix_world.copy(); o.parent=parent; o.matrix_world=m
def hide(o): o.hide_render=True; o.hide_set(True)
head=bpy.data.objects['CTRL - Cabeca mordida']; rig=bpy.data.objects['RIG - Rastejante']
for o in list(scene.objects):
    if o.parent==head or o.parent==bpy.data.objects.get('CTRL - Mandibula'): hide(o)
    if o.name.startswith(('Vertebra','Escapula','Guia contato','Quadril saliente','Pescoco')): hide(o)
    if o.type=='MESH' and any(m and m.name=='Ossos sob pele' for m in o.data.materials):
        for slot in o.material_slots: slot.material=bpy.data.materials['Pele escura ressecada']
def pose_arm(label,end):
    upper=rig.pose.bones['Superior.'+label]; lower=rig.pose.bones['Inferior.'+label]
    hip=upper.bone.head_local.copy(); knee=upper.bone.tail_local.copy(); foot=lower.bone.tail_local.copy()
    end=Vector(end); a=(knee-hip).length; b=(foot-knee).length; d=(end-hip).normalized()
    dist=max(abs(a-b)+.001,min((end-hip).length,a+b-.001)); along=(a*a-b*b+dist*dist)/(2*dist)
    bend=knee-hip-d*(knee-hip).dot(d); mid=hip+d*along+bend.normalized()*math.sqrt(max(0,a*a-along*along)); end=hip+d*dist
    for pb,oa,ob,na,nb in [(upper,hip,knee,hip,mid),(lower,knee,foot,mid,end)]:
        pb.rotation_mode='QUATERNION'; rot=(ob-oa).rotation_difference(nb-na).to_matrix().to_4x4()
        pb.matrix=Matrix.Translation(na)@rot@Matrix.Translation(-oa)@pb.bone.matrix_local; bpy.context.view_layer.update()
for side,label in [(-1,'Braco.D'),(1,'Braco.E')]: pose_arm(label,(.57,side*.57,.08))
# Bake the neutral pose, then make it the editable skeleton's new rest pose.
bound=[]
for o in list(scene.objects):
    if o.type!='MESH': continue
    for mod in list(o.modifiers):
        if mod.type=='ARMATURE' and mod.object==rig:
            bpy.ops.object.select_all(action='DESELECT'); o.hide_set(False); o.select_set(True); bpy.context.view_layer.objects.active=o
            bpy.ops.object.modifier_apply(modifier=mod.name); bound.append(o)
            if o.hide_render: o.hide_set(True)
bpy.ops.object.select_all(action='DESELECT'); rig.select_set(True); bpy.context.view_layer.objects.active=rig
bpy.ops.object.mode_set(mode='POSE'); bpy.ops.pose.armature_apply(); bpy.ops.object.mode_set(mode='OBJECT')
for o in bound:
    if o.name.startswith(('Braco.D','Braco.E')) and any(term in o.name for term in ['palma','dedo','ponta']): hide(o)
    mod=o.modifiers.new('Rig encapuzado','ARMATURE'); mod.object=rig

def fabric(name,dark,light):
    m=bpy.data.materials.new(name); m.use_nodes=True; n=m.node_tree.nodes; links=m.node_tree.links; bsdf=n.get('Principled BSDF')
    bsdf.inputs['Roughness'].default_value=.96
    noise=n.new('ShaderNodeTexNoise'); noise.inputs['Scale'].default_value=15; noise.inputs['Detail'].default_value=4
    ramp=n.new('ShaderNodeValToRGB'); ramp.color_ramp.elements[0].position=.28; ramp.color_ramp.elements[0].color=(*dark,1); ramp.color_ramp.elements[1].position=.75; ramp.color_ramp.elements[1].color=(*light,1)
    links.new(noise.outputs['Fac'],ramp.inputs[0]); links.new(ramp.outputs[0],bsdf.inputs['Base Color'])
    weave=n.new('ShaderNodeTexNoise'); weave.inputs['Scale'].default_value=210; weave.inputs['Detail'].default_value=2
    bump=n.new('ShaderNodeBump'); bump.inputs['Strength'].default_value=.22; bump.inputs['Distance'].default_value=.012
    links.new(weave.outputs['Fac'],bump.inputs['Height']); links.new(bump.outputs[0],bsdf.inputs['Normal']); return m
cloth=fabric('Lona oliva cinzenta gasta',(.035,.039,.027),(.065,.072,.048))
lining=fabric('Forro profundo',(.003,.004,.002),(.011,.014,.007))
seam_mat=fabric('Costura discreta',(.035,.034,.020),(.12,.115,.071))
skin=bpy.data.materials['Pele escura ressecada']
def mesh(name,verts,faces,mat,thickness=.01):
    data=bpy.data.meshes.new(name); data.from_pydata(verts,[],faces); data.update()
    o=bpy.data.objects.new(name,data); scene.collection.objects.link(o); o.data.materials.append(mat)
    for p in data.polygons: p.use_smooth=True
    solid=o.modifiers.new('Espessura de tecido','SOLIDIFY'); solid.thickness=thickness; solid.offset=-1
    sub=o.modifiers.new('Dobras suaves','SUBSURF'); sub.levels=1; sub.render_levels=1; return o
def curve(name,points,radius,mat):
    c=bpy.data.curves.new(name,'CURVE'); c.dimensions='3D'; c.bevel_depth=radius; c.bevel_resolution=2
    s=c.splines.new('POLY'); s.points.add(len(points)-1)
    for p,co in zip(s.points,points): p.co=(*co,1)
    o=bpy.data.objects.new(name,c); scene.collection.objects.link(o); o.data.materials.append(mat); return o

# Loft from back of skull to a real, tilted face opening. There is no cap across the aperture.
count=96; rings=25; back=Vector((.30,0,.75)); opening=Vector((.92,0,.68)); vertical=Vector((-.18,0,.984))
def hood_point(t,theta):
    r=math.sin(t*math.pi/2)**.70*(1-.22*t**4); centre=back.lerp(opening,t)
    top=max(0,math.sin(theta)); fold=1+.024*math.cos(theta*7+t*5)*math.sin(t*math.pi)
    p=centre+Vector((0,.244*r*math.cos(theta)*fold,0))+vertical*(.31*r*math.sin(theta)*fold)
    p.z+=.19*math.sin(math.pi*t)*top**4
    if t>.91:
        wear=.004*math.sin(theta*21)+.005*math.sin(theta*37)
        p+=vertical*wear*(t-.91)/.09
    return p
verts=[]; faces=[]
for j in range(rings):
    t=.018+.982*j/(rings-1)
    for i in range(count): verts.append(hood_point(t,i*math.tau/count))
for j in range(rings-1):
    for i in range(count):
        a=j*count+i; b=j*count+(i+1)%count; faces.append((a,b,b+count,a+count))
faces.append(tuple(reversed(range(count))))
hood=mesh('Capuz com abertura real',verts,faces,cloth,.014); hood.data.materials.append(lining)
hood.modifiers['Espessura de tecido'].material_offset=1; hood.modifiers['Espessura de tecido'].material_offset_rim=1
parent_keep(hood,head)
parent_keep(curve('Costura superior do capuz',[hood_point(.05+i*.90/50,math.pi/2)+Vector((0,0,.006)) for i in range(51)],.0023,seam_mat),head)
# Recessed dark face inside the fabric, away from the opening rim.
centre=opening-Vector((.925,0,.38))*.12
vs=[centre]+[centre+Vector((0,.18*math.cos(i*math.tau/64),0))+vertical*(.22*math.sin(i*math.tau/64)) for i in range(64)]
dark_face=mesh('Rosto oculto no capuz',vs,[(0,i+1,(i+1)%64+1) for i in range(64)],lining,.004); parent_keep(dark_face,head)

# Shoulder mantle follows the body surface instead of intersecting its shoulder volumes.
envelopes=[(-.02,0,.59,.58,.245,.18),(.29,0,.61,.28,.32,.18),(.53,0,.65,.14,.13,.14),(.35,-.29,.62,.115,.115,.105),(.35,.29,.62,.115,.115,.105)]
def body_top(x,y):
    z=.50
    for cx,cy,cz,rx,ry,rz in envelopes:
        value=1-((x-cx)/rx)**2-((y-cy)/ry)**2
        if value>=0: z=max(z,cz+rz*math.sqrt(value))
    return z
verts=[]; faces=[]; nr=14; nc=120
for j in range(nr):
    t=j/(nr-1)
    for i in range(nc):
        angle=i*math.tau/nc; front=max(math.cos(angle),0)
        radius=(.15*(1-t)+(.20 if math.cos(angle)>0 else 1.05)*t)
        x=.48+radius*math.cos(angle); y=(.13*(1-t)+.46*t)*math.sin(angle)
        z=max(body_top(x,y)+.027,.845-.12*t*t-.15*math.sin(angle)**2*t*t)+.012*math.sin(angle*9+t*3)*t-.10*front**5*t
        if j==nr-1:
            z-=.012*(.5+.5*math.sin(angle*23))
            x+=.012*math.sin(angle*29)*math.cos(angle); y+=.012*math.sin(angle*29)*math.sin(angle)
        verts.append((x,y,z))
for j in range(nr-1):
    for i in range(nc):
        a=j*nc+i; b=j*nc+(i+1)%nc; faces.append((a,a+nc,b+nc,b))
faces.append(tuple(range(nc)))
mantle=mesh('Manto curto de ombros',verts,faces,cloth,.012)

def sphere(name,pos,scale,mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24,ring_count=16,location=pos)
    o=bpy.context.object; o.name=name; o.scale=scale; o.data.materials.append(mat)
    for p in o.data.polygons: p.use_smooth=True
    return o
def segment(name,a,b,r1,r2):
    a,b=Vector(a),Vector(b); bpy.ops.mesh.primitive_cone_add(vertices=20,radius1=r1,radius2=r2,depth=(b-a).length,location=(a+b)/2)
    o=bpy.context.object; o.name=name; o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler(); o.data.materials.append(skin)
    for p in o.data.polygons: p.use_smooth=True
    bevel=o.modifiers.new('Articulacao macia','BEVEL'); bevel.width=.006; bevel.segments=2; return o
def bone_parent(o,bone):
    bpy.context.view_layer.update(); world=o.matrix_world.copy(); o.parent=rig; o.parent_type='BONE'; o.parent_bone=bone; o.matrix_world=world
for side,label in [(-1,'Braco.D'),(1,'Braco.E')]:
    wrist=Vector((.57,side*.57,.08)); objects=[sphere('Palma nova',wrist,(.095,.085,.035),skin)]
    for i in range(4):
        y=(i-1.5)*.040; length=.13-.02*abs(i-1.5)
        a=wrist+Vector((.035,y,.002)); b=a+Vector((length*.58,y*.12,-.009)); c=b+Vector((length*.48,y*.1,-.006))
        objects.extend([segment('Dedo primeira falange',a,b,.021,.015),sphere('No do dedo',b,(.018,.018,.016),skin),segment('Dedo distal',b,c,.015,.006)])
    a=wrist+Vector((-.01,-side*.055,.0)); b=a+Vector((.07,-side*.045,-.01)); objects.append(segment('Polegar curvado',a,b,.025,.009))
    for o in objects: bone_parent(o,'Inferior.'+label)

# Only two views, no animation render batch or changes to game resources.
scene.camera=bpy.data.objects['Camera topo']; scene.camera.data.ortho_scale=3.2
scene.render.resolution_x=768; scene.render.resolution_y=768; scene.render.resolution_percentage=100
scene.render.engine='CYCLES'; scene.cycles.samples=32; scene.cycles.use_denoising=True; scene.render.film_transparent=True
scene.render.image_settings.file_format='PNG'; scene.render.image_settings.color_mode='RGBA'
scene.view_settings.view_transform='AgX'; scene.view_settings.look='AgX - Medium High Contrast'
side=bpy.data.objects['Camera inspecao']; side.location=(3.7,-4.2,3.6); side.rotation_euler=(Vector((0,0,.48))-side.location).to_track_quat('-Z','Y').to_euler(); side.data.ortho_scale=3.1
scene.frame_set(1); scene.render.filepath=str(BASE/'pose-topo.png')
bpy.ops.wm.save_as_mainfile(filepath=str(BASE/'espreitador-encapuzado.blend'))
bpy.ops.render.render(write_still=True)
scene.camera=side; scene.render.filepath=str(BASE/'pose-perspectiva.png'); bpy.ops.render.render(write_still=True)
scene.camera=bpy.data.objects['Camera topo']; scene.render.filepath=str(BASE/'pose-topo.png')
bpy.ops.wm.save_as_mainfile(filepath=str(BASE/'espreitador-encapuzado.blend'))
print('HOODED STALKER STATIC STUDY READY',flush=True)
