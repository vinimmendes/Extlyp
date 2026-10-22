"""New biped Stalker, static approval stage. Run with Blender --background --python."""
import bpy, math, random
from pathlib import Path
from mathutils import Vector

OUT = Path(__file__).resolve().parent
random.seed(1310)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.render.fps = 24

def material(name, dark, light):
    m = bpy.data.materials.new(name); m.use_nodes = True
    m.diffuse_color=(*dark,1)
    n = m.node_tree.nodes; l = m.node_tree.links
    n.clear()
    noise = n.new('ShaderNodeTexNoise'); noise.inputs['Scale'].default_value = 17
    noise.inputs['Detail'].default_value = 4
    ramp = n.new('ShaderNodeValToRGB')
    ramp.color_ramp.elements[0].color = (*dark, 1)
    ramp.color_ramp.elements[1].color = (*light, 1)
    bs = next((node for node in n if node.type == 'BSDF_PRINCIPLED'), None)
    if bs is None: bs = n.new('ShaderNodeBsdfPrincipled')
    output = next((node for node in n if node.type == 'OUTPUT_MATERIAL'), None)
    if output is None: output = n.new('ShaderNodeOutputMaterial')
    l.new(bs.outputs['BSDF'], output.inputs['Surface'])
    bs.inputs['Roughness'].default_value = .95
    l.new(noise.outputs['Fac'], ramp.inputs[0]); l.new(ramp.outputs[0], bs.inputs['Base Color'])
    # Keep the ramp's paint values; the default ramp mode may use HSV in user builds.
    ramp.color_ramp.color_mode='RGB'
    bs.inputs['Base Color'].default_value=(*dark,1)
    bump = n.new('ShaderNodeBump'); bump.inputs['Strength'].default_value = .16
    bump.inputs['Distance'].default_value = .022
    l.new(noise.outputs['Fac'], bump.inputs['Height']); l.new(bump.outputs[0], bs.inputs['Normal'])
    return m

cloth = material('Tecido carvao deteriorado', (.035,.031,.026), (.17,.15,.12))
brown = material('Tiras marrons desbotadas', (.065,.047,.032), (.26,.20,.13))
skin = material('Maos palidas ressecadas', (.20,.17,.13), (.54,.49,.39))
bone = material('Garras de osso', (.19,.15,.10), (.68,.60,.45))
dark = material('Sombra interior capuz', (.004,.004,.004), (.009,.008,.007))

def sphere(name, p, s, mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, location=p)
    o=bpy.context.object; o.name=name; o.scale=s; o.data.materials.append(mat)
    for f in o.data.polygons: f.use_smooth=True
    return o

def tube(name, points, radii, mat, sides=12):
    vertices=[]; faces=[]
    points=[Vector(p) for p in points]
    for i,p in enumerate(points):
        tangent=(points[min(i+1,len(points)-1)]-points[max(0,i-1)]).normalized()
        u=tangent.cross(Vector((0,0,1)))
        if u.length < .01: u=tangent.cross(Vector((0,1,0)))
        u.normalize(); v=tangent.cross(u).normalized()
        for j in range(sides):
            a=j*math.tau/sides; vertices.append(p+radii[i]*(math.cos(a)*u+math.sin(a)*v))
    for i in range(len(points)-1):
        for j in range(sides):
            a=i*sides+j; b=i*sides+(j+1)%sides
            faces.append((a,b,b+sides,a+sides))
    faces.extend([tuple(reversed(range(sides))),tuple((len(points)-1)*sides+j for j in range(sides))])
    return mesh(name,vertices,faces,mat,False)

def mesh(name, vertices, faces, mat, fabric=True):
    data=bpy.data.meshes.new(name); data.from_pydata(vertices,[],faces); data.update()
    o=bpy.data.objects.new(name,data); scene.collection.objects.link(o); data.materials.append(mat)
    for p in data.polygons: p.use_smooth=True
    if fabric:
        solid=o.modifiers.new('Espessura tecido','SOLIDIFY'); solid.thickness=.012
        sub=o.modifiers.new('Dobras suaves','SUBSURF'); sub.levels=1; sub.render_levels=2
    return o

# Upright biped, forward +X, arched spine. Separate meshes remain editable.
sphere('Quadril',(-.18,0,1.10),(.22,.24,.23),cloth)
sphere('Tronco arqueado',(-.14,0,1.67),(.33,.29,.49),cloth)
sphere('Ombros curvados',(.02,0,1.99),(.29,.40,.22),cloth)
for side in (-1,1):
    y=side*.18
    tube('Perna '+str(side),[(-.18,y,1.15),(.12,y,.66),(-.08,y,.20)],[.16,.115,.075],cloth)
    sphere('Pe apoiado '+str(side),(.03,y,.10),(.25,.105,.10),cloth)
    for toe in range(3):
        ty=y+(toe-1)*.06
        tube('Garra pe',[(.18,ty,.09),(.30,ty,.07),(.34,ty,.035)],[.026,.018,.001],bone)
    shoulder=(.04,side*.34,1.99); elbow=(-.09,side*.54,1.43); wrist=(.24,side*.62,.94)
    tube('Manga '+str(side),[shoulder,elbow,wrist],[.145,.105,.066],cloth)
    sphere('Cotovelo '+str(side),elbow,(.115,.11,.12),cloth)
    sphere('Palma '+str(side),(.29,side*.63,.82),(.11,.085,.17),skin)
    for digit in range(4):
        y=side*.63+(digit-1.5)*.049; x=.32+.018*math.sin(digit)
        tube('Dedo '+str(side)+' '+str(digit),[(x,y,.78),(x+.07,y,.65),(x+.12,y,.59)],[.027,.023,.015],skin)
        tube('Garra mao '+str(side)+' '+str(digit),[(x+.12,y,.59),(x+.19,y,.52),(x+.18,y,.44)],[.019,.011,.001],bone)
    tube('Polegar '+str(side),[(.30,side*.54,.88),(.43,side*.51,.78),(.47,side*.53,.68)],[.035,.026,.003],skin)

# Open hood loft: rear shell closes, front rim remains genuinely open.
verts=[]; faces=[]; rings=18; sides=48
for i in range(rings):
    t=i/(rings-1); center=Vector((-.18+.85*t,0,2.14-.12*t))
    ry=.018+.25*math.sin(t*math.pi/2); rz=.025+.33*math.sin(t*math.pi/2)
    for j in range(sides):
        a=j*math.tau/sides; fold=1+.045*math.cos(a*7+t*5)
        peak=.16*max(0,math.sin(a))**7*math.sin(t*math.pi*.8)
        verts.append(center+Vector((0,ry*math.cos(a)*fold,rz*math.sin(a)*fold+peak)))
for i in range(rings-1):
    for j in range(sides):
        a=i*sides+j; b=i*sides+(j+1)%sides; faces.append((a,b,b+sides,a+sides))
faces.append(tuple(reversed(range(sides))))
mesh('Capuz aberto angular',verts,faces,cloth)
sphere('Rosto oculto dentro capuz',(.36,0,2.10),(.15,.17,.23),dark)

# Coat split into overlapping independently deformable panels, ragged hem.
for j in range(12):
    a=j*math.tau/12
    if abs(math.atan2(math.sin(a),math.cos(a))) < .50: continue
    vs=[]; fs=[]
    for r in range(6):
        t=r/5; z=2.02-1.24*t; cx=-.08-.13*t; radius=.30+.055*math.sin(t*math.pi)
        for k in range(5):
            angle=a+(k/4-.5)*.64
            hem=(.12*math.sin(j*3+k*2)+.08*(k%2)) if r==5 else 0
            vs.append((cx+radius*math.cos(angle),radius*math.sin(angle),z+hem))
    for r in range(5):
        for k in range(4):
            q=r*5+k; fs.append((q,q+1,q+6,q+5))
    mesh('Aba casaco '+str(j),vs,fs,brown if j%3==0 else cloth)

# Layered frayed shoulder strips, follow the curved back rather than a flat cape.
for j in range(9):
    y=(j-4)*.083; vs=[]
    for i in range(7):
        t=i/6; x=.17-.65*t; z=2.13-.64*t*t
        width=.042*(1-.65*t)
        vs.extend([(x,y-width,z),(x-.025,y+width,z+.012*math.sin(i+j))])
    mesh('Tira ombro '+str(j),vs,[(i*2,i*2+1,i*2+3,i*2+2) for i in range(6)],brown if j%2==0 else cloth)

def camera(name,p,target,scale):
    data=bpy.data.cameras.new(name); data.type='ORTHO'; data.ortho_scale=scale
    o=bpy.data.objects.new(name,data); scene.collection.objects.link(o); o.location=p
    o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler(); return o
top=camera('Camera superior +X',(0,0,7),(0,0,0),2.45)
top.rotation_euler=(0,0,0)
perspective=camera('Camera tres quartos',(4,-6,3.4),(0,0,1.25),3.25)
for name,p,power,size in [('Luz principal',(2,-3,5),650,4),('Luz preenchimento',(-2,2,3),420,3),('Luz contorno',(-3,-1,4),550,2)]:
    d=bpy.data.lights.new(name,'AREA'); d.energy=power; d.shape='DISK'; d.size=size
    o=bpy.data.objects.new(name,d); scene.collection.objects.link(o); o.location=p
    o.rotation_euler=(Vector((0,0,1.2))-o.location).to_track_quat('-Z','Y').to_euler()
scene.world=bpy.data.worlds.new('Ambiente controlado')
scene.world.use_nodes=True
background=next(n for n in scene.world.node_tree.nodes if n.type=='BACKGROUND')
background.inputs['Color'].default_value=(.10,.12,.14,1)
background.inputs['Strength'].default_value=.18
scene.render.use_compositing=False
scene.render.engine='CYCLES'; scene.cycles.samples=32
scene.cycles.use_denoising=False
scene.render.threads_mode='FIXED'; scene.render.threads=4
scene.render.resolution_x=1024; scene.render.resolution_y=1024
scene.render.resolution_percentage=100; scene.render.film_transparent=True
scene.render.image_settings.file_format='PNG'; scene.render.image_settings.color_mode='RGBA'
scene.view_settings.view_transform='AgX'
scene.camera=perspective
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'espreitador-encapuzado-v2.blend'))
for cam,name in [(perspective,'pose-tres-quartos.png'),(top,'pose-superior.png')]:
    scene.camera=cam; scene.render.filepath=str(OUT/name); bpy.ops.render.render(write_still=True)
print('Static stage delivered; awaiting visual approval before rig.')
