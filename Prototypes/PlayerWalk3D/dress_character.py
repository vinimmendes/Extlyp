"""Dress the approved rig without changing cameras, controls, or animation curves."""
import bpy, math, random, sys
from pathlib import Path
from mathutils import Vector
BASE=Path(__file__).resolve().parent
OUT=BASE/'VisualV1';OUT.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(BASE/'boneco-caminhada.blend'))
scene=bpy.context.scene
random.seed(24)

def cloth(name,dark,light,scale=65,bump=.08,metal=0):
    m=bpy.data.materials.new(name);m.diffuse_color=(*light,1);m.use_nodes=True
    n=m.node_tree.nodes;l=m.node_tree.links;p=n.get('Principled BSDF')
    p.inputs['Roughness'].default_value=.82 if not metal else .36
    p.inputs['Metallic'].default_value=metal
    noise=n.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=scale;noise.inputs['Detail'].default_value=3
    ramp=n.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].position=.22;ramp.color_ramp.elements[0].color=(*dark,1)
    ramp.color_ramp.elements[1].position=.78;ramp.color_ramp.elements[1].color=(*light,1)
    l.new(noise.outputs['Fac'],ramp.inputs[0]);l.new(ramp.outputs[0],p.inputs['Base Color'])
    b=n.new('ShaderNodeBump');b.inputs['Strength'].default_value=bump;b.inputs['Distance'].default_value=.025
    l.new(noise.outputs['Fac'],b.inputs['Height']);l.new(b.outputs[0],p.inputs['Normal'])
    return m
jacket=cloth('Jaqueta oliva - tecido gasto',(.065,.077,.041),(.21,.23,.125))
pants=cloth('Calca marrom - sarja',(.105,.062,.029),(.29,.18,.087))
canvas=cloth('Mochila lona caqui',(.12,.085,.043),(.34,.265,.14))
leather=cloth('Couro marrom gasto',(.035,.020,.012),(.14,.081,.040),38,.11)
patch=cloth('Remendos de lona',(.22,.17,.095),(.39,.32,.19))
thread=cloth('Linha de costura',(.24,.18,.10),(.49,.39,.22),40,.02)
steel=cloth('Aco escurecido',(.022,.027,.03),(.105,.12,.13),90,.015,.75)
silver=cloth('Fivelas envelhecidas',(.14,.15,.14),(.38,.4,.36),30,.01,.8)
hair=cloth('Cabelo castanho escuro',(.019,.010,.006),(.085,.041,.020),100,.16)
hairlight=cloth('Mechas castanhas',(.044,.023,.012),(.13,.070,.032),55,.07)
skin=cloth('Pele',(.32,.19,.11),(.57,.37,.23),85,.045)
sole=cloth('Sola escura',(.015,.013,.010),(.055,.043,.03),60,.10)

def assign(o,m):o.data.materials.clear();o.data.materials.append(m)
def finish(o,name,m):
    o.name=name;assign(o,m)
    if o.type=='MESH':
        for p in o.data.polygons:p.use_smooth=True
    return o
def sphere(name,loc,scale,m):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24,ring_count=16,location=loc)
    o=finish(bpy.context.object,name,m);o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return o
def box(name,loc,scale,m,bevel=.02):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    o=finish(bpy.context.object,name,m);o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    mod=o.modifiers.new('Cantos macios','BEVEL');mod.width=bevel;mod.segments=3
    o.modifiers.new('Normais','WEIGHTED_NORMAL')
    return o
def line(name,points,radius,m):
    c=bpy.data.curves.new(name,'CURVE');c.dimensions='3D';c.bevel_depth=radius;c.bevel_resolution=2
    s=c.splines.new('POLY');s.points.add(len(points)-1)
    for p,co in zip(s.points,points):p.co=(*co,1)
    o=bpy.data.objects.new(name,c);scene.collection.objects.link(o);o.data.materials.append(m);return o
def bind(o,bone):
    g=o.vertex_groups.new(name=bone);g.add(list(range(len(o.data.vertices))),1,'REPLACE')
    mod=o.modifiers.new('Segue perna aprovada','ARMATURE');mod.object=bpy.data.objects['RIG - pernas IK']

for o in list(scene.objects):
    if o.type!='MESH':continue
    name=o.name
    if name.startswith(('Tronco','Braco','Antebraco')):assign(o,jacket)
    elif name.startswith(('Cabeca','Mao')):assign(o,skin)
    elif name.startswith('Mochila'):assign(o,canvas)
    elif name.startswith(('Coxa','Canela')):assign(o,pants)
    elif name.startswith('Bota'):assign(o,leather)
    elif name.startswith(('Cano','Cabo')):o.hide_render=True;o.hide_set(True)

# Clothing volumes soften the original cylinder joints; all are static above waist.
for s in [-1,1]:
    sphere('Ombro tecido',(.055,s*.30,1.56),(.16,.16,.16),jacket)
    sphere('Cotovelo tecido',(.35,s*.37,1.32),(.12,.125,.12),jacket)
    # Small elbow repair with individually visible stitches on its upper surface.
    o=box('Remendo cotovelo',(.34,s*.39,1.423),(.14,.12,.012),patch,.018)
    for i in range(6):
        x=.282+i*.023
        line('Ponto remendo',[(x,s*.39-.052,1.434),(x+.008,s*.39-.038,1.434)],.0025,thread)
        line('Ponto remendo',[(x,s*.39+.039,1.434),(x+.008,s*.39+.053,1.434)],.0025,thread)
    # Jacket shoulder seam, cuffs and sleeve folds.
    line('Costura ombro',[(.0,s*.34,1.69),(.10,s*.40,1.65),(.18,s*.42,1.58)],.006,thread)
    a=Vector((.36,s*.38,1.31));b=Vector((.68,s*.055,1.38));axis=(b-a).normalized()
    u=axis.cross(Vector((0,0,1))).normalized();v=axis.cross(u)
    for t in [.16,.28,.80,.86]:
        center=a.lerp(b,t)
        pts=[center+.092*(u*math.cos(j*math.tau/24)+v*math.sin(j*math.tau/24)) for j in range(25)]
        line('Dobra manga',[tuple(p) for p in pts],.007,leather if t>.75 else jacket)
    sphere('Orelha',(.16,s*.215,1.77),(.055,.035,.07),skin)

# Hair cap is an actual open hemisphere, not a second floating head.
verts=[];faces=[];rings=12;segments=48
for i in range(rings+1):
    theta=.025+(math.pi*.54-.025)*i/rings
    for j in range(segments):
        phi=math.tau*j/segments
        r=1+.012*math.sin(phi*9+theta*13)
        verts.append((.14+.239*r*math.sin(theta)*math.cos(phi),.224*r*math.sin(theta)*math.sin(phi),1.79+.247*r*math.cos(theta)))
for i in range(rings):
    for j in range(segments):
        a=i*segments+j;b=i*segments+(j+1)%segments;faces.append((a,b,b+segments,a+segments))
mesh=bpy.data.meshes.new('Cabelo malha');mesh.from_pydata(verts,[],faces);mesh.update()
o=bpy.data.objects.new('Cabelo curto castanho',mesh);scene.collection.objects.link(o);finish(o,o.name,hair)
for j in range(100):
    phi=random.uniform(0,math.tau);theta=random.uniform(.12,1.5)
    pts=[]
    for k in range(7):
        t=theta+k*.026;p=phi+k*.027
        pts.append((.14+.244*math.sin(t)*math.cos(p),.229*math.sin(t)*math.sin(p),1.79+.253*math.cos(t)))
    line('Mecha',pts,random.uniform(.0015,.0035),hairlight if j%4==0 else hair)

# Backpack flap, leather straps, buckles, seams, rolled bedroll and pockets.
box('Corpo mochila lona',(-.38,0,1.40),(.27,.55,.56),canvas,.075)
box('Aba mochila',(-.38,0,1.69),(.27,.56,.07),canvas,.035)
for y in [-.17,.17]:
    box('Correia mochila',(-.392,y,1.736),(.285,.042,.019),leather,.007)
    # Buckle in XY plane, visible from game camera.
    x=-.35;z=1.75
    line('Fivela mochila',[(x-.026,y-.032,z),(x+.026,y-.032,z),(x+.026,y+.032,z),(x-.026,y+.032,z),(x-.026,y-.032,z)],.006,silver)
    line('Pino fivela',[(x,y-.03,z+.003),(x,y+.018,z+.003)],.004,silver)
for s in [-1,1]:
    box('Bolso lateral mochila',(-.39,s*.31,1.40),(.20,.12,.29),canvas,.04)
    # Shoulder harness connects the pack over shoulder to chest.
    line('Alca couro', [(-.36,s*.23,1.71),(-.17,s*.245,1.73),(.01,s*.245,1.69),(.15,s*.245,1.58),(.23,s*.21,1.39)],.024,leather)
    line('Costura alca',[(-.35,s*.255,1.718),(-.17,s*.269,1.735),(.01,s*.269,1.698),(.15,s*.269,1.589)],.003,thread)
    box('Fivela alca',(-.02,s*.245,1.711),(.065,.062,.018),silver,.009)
    box('Miolo fivela',(-.02,s*.245,1.724),(.041,.039,.013),leather,.004)
for j in range(20):
    y=-.235+j*.025
    line('Costura aba',[(-.513,y,1.72),(-.505,y+.008,1.724)],.0025,thread)
sphere('Rolo de tecido',(-.47,0,1.11),(.13,.34,.115),canvas)
for y in [-.21,.21]:
    line('Amarra rolo',[(-.47+.135*math.cos(j*math.tau/24),y,1.11+.12*math.sin(j*math.tau/24)) for j in range(25)],.015,leather)

# Brown boots, separate soles and laces follow the original target controls.
for label,y in [('Direita',-.18),('Esquerda',.18)]:
    target=bpy.data.objects['Controle pe '+label]
    o=sphere('Sola '+label,(0,0,0),(.194,.109,.025),sole);o.parent=target;o.location=(.09,0,-.049)
    o=sphere('Cano bota '+label,(0,0,0),(.09,.09,.13),leather);o.parent=target;o.location=(-.015,0,.09)
    for i in range(5):
        x=.015+i*.026
        o=line('Cadarco '+label,[(x,-.048,.066),(x+.02,.048,.066)],.004,thread);o.parent=target
    o=box('Remendo calca '+label,(.285,y,.60),(.10,.14,.15),patch,.02);bind(o,'Coxa.'+label)

# Narrow top view pistol: slide, barrel, sights and grip below hands.
box('Ferrolho pistola',(.86,0,1.445),(.33,.075,.073),steel,.012)
box('Empunhadura pistola',(.72,0,1.32),(.08,.068,.20),leather,.012)
box('Mira traseira',(.737,0,1.49),(.026,.079,.025),steel,.004)
box('Massa de mira',(.996,0,1.49),(.019,.020,.025),steel,.003)
box('Janela superior',(.85,0,1.484),(.05,.046,.005),sole,.004)
for i in range(5):
    line('Ranhura ferrolho',[(.727+i*.014,-.034,1.482),(.727+i*.014,.034,1.482)],.002,silver)

scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
scene.cycles.device='CPU'
scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.35,.39,.44,1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value=.35
def light(name,pos,power,size):
    d=bpy.data.lights.new(name,'AREA');o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=pos
    o.rotation_euler=(Vector((0,0,1))-o.location).to_track_quat('-Z','Y').to_euler();d.energy=power;d.shape='DISK';d.size=size
light('Luz suave', (1,-3,5),300,4)
light('Preenchimento',(-2,2,3),160,3)
scene.view_settings.view_transform='AgX'
scene.render.resolution_x=640;scene.render.resolution_y=640
scene.camera=bpy.data.objects['Camera jogo - topo'];scene.frame_set(9)
scene.render.film_transparent=True
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'personagem-rustico.blend'))
frames=range(1,33) if '--full' in sys.argv else [1,9]
for name,cam in [('topo','Camera jogo - topo'),('lateral','Camera inspecao')]:
    (OUT/name).mkdir(exist_ok=True);scene.camera=bpy.data.objects[cam]
    for f in frames:
        scene.frame_set(f);scene.render.filepath=str(OUT/name/f'{f:03}.png');bpy.ops.render.render(write_still=True)
print('VISUAL_READY',OUT)
