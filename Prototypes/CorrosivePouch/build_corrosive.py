"""Original Papo Inchado. Blender 4.5: editable rig, procedural materials and baked gait."""
import bpy, math, random, sys
from pathlib import Path
from mathutils import Vector, Matrix
OUT=Path(__file__).resolve().parent
print('BUILD CORROSIVE START',flush=True)
random.seed(51)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene; scene.render.fps=24; scene.frame_start=1; scene.frame_end=145

def material(name,dark,light,roughness=.7,pores=.07):
    m=bpy.data.materials.new(name); m.use_nodes=True
    n=m.node_tree.nodes; l=m.node_tree.links; p=n.get('Principled BSDF')
    p.inputs['Roughness'].default_value=roughness
    noise=n.new('ShaderNodeTexNoise'); noise.inputs['Scale'].default_value=7; noise.inputs['Detail'].default_value=4
    ramp=n.new('ShaderNodeValToRGB'); ramp.color_ramp.elements[0].position=.25; ramp.color_ramp.elements[0].color=(*dark,1)
    ramp.color_ramp.elements[1].position=.75; ramp.color_ramp.elements[1].color=(*light,1)
    l.new(noise.outputs['Fac'],ramp.inputs[0]); l.new(ramp.outputs[0],p.inputs['Base Color'])
    micro=n.new('ShaderNodeTexNoise'); micro.inputs['Scale'].default_value=85; micro.inputs['Detail'].default_value=2
    bump=n.new('ShaderNodeBump'); bump.inputs['Strength'].default_value=pores; bump.inputs['Distance'].default_value=.018
    l.new(micro.outputs['Fac'],bump.inputs['Height']); l.new(bump.outputs[0],p.inputs['Normal'])
    return m
skin=material('Pele castanha ressecada',(.085,.057,.037),(.30,.225,.16),.81,.20)
head_skin=material('Cabeca cicatrizada',(.16,.105,.066),(.39,.305,.225),.77,.16)
pouch_mat=material('Papo amarelado doente',(.18,.17,.062),(.47,.43,.18),.48,.10)
vein_mat=material('Veias sob a pele',(.11,.080,.043),(.23,.155,.09),.73,.05)
lip_mat=material('Labios secos',(.18,.095,.064),(.36,.22,.155),.68,.1)
dark=material('Cavidade da boca',(.006,.003,.002),(.037,.011,.008),.38,.02)
tongue_mat=material('Lingua e gengiva',(.13,.052,.031),(.34,.16,.09),.43,.07)
teeth_mat=material('Dentes amarelados',(.26,.22,.12),(.58,.52,.34),.70,.04)
eye_mat=material('Olhos fundos',(.008,.006,.004),(.027,.023,.014),.25,0)

def sphere(name,loc,scale,mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32,ring_count=20,location=loc)
    o=bpy.context.object; o.name=name; o.scale=scale; o.data.materials.append(mat)
    for p in o.data.polygons: p.use_smooth=True
    return o
def segment(name,a,b,r1,r2,mat):
    a,b=Vector(a),Vector(b)
    bpy.ops.mesh.primitive_cone_add(vertices=24,radius1=r1,radius2=r2,depth=(b-a).length,location=(a+b)/2)
    o=bpy.context.object; o.name=name; o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler(); o.data.materials.append(mat)
    bevel=o.modifiers.new('Bordas arredondadas','BEVEL'); bevel.width=.022; bevel.segments=3
    for p in o.data.polygons: p.use_smooth=True
    return o
def line(name,pts,radius,mat):
    c=bpy.data.curves.new(name,'CURVE'); c.dimensions='3D'; c.bevel_depth=radius; c.bevel_resolution=3
    s=c.splines.new('POLY'); s.points.add(len(pts)-1)
    for p,co in zip(s.points,pts): p.co=(*co,1)
    o=bpy.data.objects.new(name,c); scene.collection.objects.link(o); o.data.materials.append(mat); return o
def ctrl(name,loc):
    o=bpy.data.objects.new(name,None); scene.collection.objects.link(o); o.location=loc; o.empty_display_size=.10; return o
def parent_keep(o,parent):
    bpy.context.view_layer.update(); world=o.matrix_world.copy(); o.parent=parent; o.matrix_world=world
def unify(parts,name):
    print('MERGING '+name,flush=True)
    bpy.ops.object.select_all(action='DESELECT')
    for o in parts: o.select_set(True)
    bpy.context.view_layer.objects.active=parts[0]; bpy.ops.object.join()
    o=bpy.context.object; o.name=name
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    remesh=o.modifiers.new('Unir volumes','REMESH'); remesh.mode='VOXEL'; remesh.voxel_size=.035
    bpy.ops.object.modifier_apply(modifier=remesh.name)
    smooth=o.modifiers.new('Suavizar unioes','SMOOTH'); smooth.factor=.65; smooth.iterations=6
    bpy.ops.object.modifier_apply(modifier=smooth.name)
    sub=o.modifiers.new('Superficie organica','SUBSURF'); sub.levels=1
    for p in o.data.polygons: p.use_smooth=True
    return o

body=ctrl('CTRL - Tronco',(0,0,0))
torso=[]
torso.append(sphere('Torax arqueado',(-.13,0,1.13),(.35,.40,.48),skin))
torso.append(sphere('Abdomen magro',(-.10,0,.82),(.28,.29,.33),skin))
torso.append(sphere('Quadril',(-.18,0,.60),(.30,.31,.20),skin))
torso.append(sphere('Pescoco',(.15,0,1.46),(.22,.23,.29),skin))
for s in [-1,1]:
    torso.append(sphere('Ombro '+str(s),(-.12,s*.40,1.34),(.22,.22,.22),skin))
    torso.append(sphere('Escapula '+str(s),(-.37,s*.20,1.42),(.14,.19,.14),head_skin))
for i in range(6):
    torso.append(sphere('Coluna '+str(i),(-.445,0,.88+i*.11),(.035,.055,.045),head_skin))
parent_keep(unify(torso,'Corpo continuo'),body)

head=ctrl('CTRL - Cabeca',(.12,0,1.54)); parent_keep(head,body)
head_parts=[sphere('Crânio calvo',(.19,0,1.64),(.265,.255,.285),head_skin),
            sphere('Testa',(.37,0,1.72),(.11,.21,.15),head_skin),
            sphere('Nariz',(.485,0,1.625),(.085,.055,.055),head_skin)]
for s in [-1,1]:
    head_parts.append(sphere('Cavidade olho '+str(s),(.418,s*.127,1.72),(.059,.065,.044),dark))
    head_parts.append(sphere('Olho '+str(s),(.446,s*.125,1.73),(.023,.028,.020),eye_mat))
    brow=sphere('Sobrancelha ossuda '+str(s),(.409,s*.125,1.773),(.065,.074,.036),head_skin); brow.rotation_euler.x=s*.25; head_parts.append(brow)
    head_parts.append(sphere('Orelha '+str(s),(.13,s*.263,1.64),(.078,.032,.105),skin))
    for j in range(3):
        head_parts.append(line('Dobra testa',[(.37-j*.028,s*.06,1.846+j*.016),(.33-j*.032,s*.13,1.851+j*.015),(.25-j*.025,s*.18,1.837+j*.014)],.007,skin))

# Mouth faces forward and slightly upward so its dark opening reads from above.
mouth_origin=Vector((.52,0,1.46)); v=Vector((-.45,0,.893)); normal=Vector((.893,0,.45))
mouth=sphere('Boca profunda',mouth_origin,(.038,.145,.235),dark)
mouth.rotation_euler.y=-math.atan2(normal.z,normal.x); head_parts.append(mouth)
upper_pts=[]; lower_pts=[]
for i in range(33):
    t=math.pi*i/32
    upper_pts.append(mouth_origin+Vector((0,.153*math.cos(t),0))+v*(.238*math.sin(t)))
    lower_pts.append(mouth_origin+Vector((0,.153*math.cos(t),0))-v*(.238*math.sin(t)))
head_parts.append(line('Labio superior',upper_pts,.029,lip_mat))
head_parts.append(sphere('Queixo pendente',(.62,0,1.23),(.11,.16,.09),skin))
for y in [-.105,-.055,0,.055,.105]:
    a=mouth_origin+Vector((0,y,0))+v*.19+normal*.025
    head_parts.append(segment('Dente superior',a,a-v*.075,.019,.006,teeth_mat))
skull_parts=[o for o in head_parts if o.type=='MESH' and o.data.materials[0]==head_skin]
skull_obj=unify(skull_parts,'Cabeca continua')
head_parts=[o for o in head_parts if o not in skull_parts]+[skull_obj]
for o in head_parts: parent_keep(o,head)
jaw=ctrl('CTRL - Mandibula',(.48,0,1.46)); parent_keep(jaw,head)
jaw_parts=[line('Labio inferior',lower_pts,.031,lip_mat),sphere('Lingua',(.60,0,1.33),(.075,.095,.038),tongue_mat)]
for y in [-.10,-.05,0,.05,.10]:
    a=mouth_origin+Vector((0,y,0))-v*.19+normal*.03
    jaw_parts.append(segment('Dente inferior',a,a+v*.052,.015,.005,teeth_mat))
for o in jaw_parts: parent_keep(o,jaw)
exit_point=ctrl('Saida visual do acido',mouth_origin+normal*.12); parent_keep(exit_point,head)

# Large twin throat pouches, each with independent swelling control and branching veins.
pouches=[]
for s in [-1,1]:
    location=Vector((.30,s*.315,1.20)); shape=Vector((.32,.28,.37))
    c=ctrl('CTRL - Papo '+str(s),location); parent_keep(c,body); pouches.append(c)
    sac=sphere('Papo '+str(s),location,shape,pouch_mat); parent_keep(sac,c)
    cheek=sphere('Bolsa bochecha '+str(s),(.33,s*.25,1.61),(.13,.13,.14),pouch_mat); parent_keep(cheek,head)
    for j in range(5):
        longitude=-.8+j*.38
        def surface(t,longitude):
            return location+Vector((shape.x*math.sin(t)*math.cos(longitude),shape.y*math.sin(t)*math.sin(longitude),shape.z*math.cos(t)))*1.008
        points=[surface(.18+i*.065,longitude+.035*math.sin(i*.9+j)) for i in range(29)]
        vein=line('Veia principal papo',points,.004,vein_mat); parent_keep(vein,c)
        for k in [7,14,21]:
            branch=[surface(.18+(k+i)*.065,longitude+.035*math.sin(k*.9+j)+s*i*.035) for i in range(7)]
            vein=line('Ramificacao papo',branch,.0025,vein_mat); parent_keep(vein,c)

arm=bpy.data.armatures.new('Esqueleto Corrosivo'); rig=bpy.data.objects.new('RIG - Corrosivo',arm)
scene.collection.objects.link(rig); parent_keep(rig,body)
bpy.ops.object.select_all(action='DESELECT'); rig.select_set(True); bpy.context.view_layer.objects.active=rig; bpy.ops.object.mode_set(mode='EDIT')
specs=[]
for s in [-1,1]:
    for kind in ['Perna','Braco']:
        label=kind+('.D' if s<0 else '.E')
        if kind=='Perna': hip=Vector((-.18,s*.24,.60)); knee=Vector((.13,s*.42,.34)); foot=Vector((.03,s*.56,.065))
        else: hip=Vector((-.12,s*.44,1.34)); knee=Vector((.02,s*.65,.84)); foot=Vector((.32,s*.65,.40))
        a=arm.edit_bones.new('Superior.'+label); a.head=hip; a.tail=knee
        b=arm.edit_bones.new('Inferior.'+label); b.head=knee; b.tail=foot; b.parent=a; b.use_connect=True
        specs.append((kind,s,label,hip,knee,foot))
bpy.ops.object.mode_set(mode='OBJECT'); rig.show_in_front=True
def bind(o,name):
    bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active=o
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    g=o.vertex_groups.new(name=name); g.add(list(range(len(o.data.vertices))),1,'REPLACE')
    mod=o.modifiers.new('Deformacao pelo rig','ARMATURE'); mod.object=rig
    parent_keep(o,body)
for kind,s,label,hip,knee,foot in specs:
    upper=segment(label+' superior',hip,knee,.16 if kind=='Perna' else .115,.09,skin)
    muscle=sphere(label+' musculo',hip.lerp(knee,.35),(.14 if kind=='Perna' else .11,.12,.20),skin)
    muscle.rotation_euler=(knee-hip).to_track_quat('Z','Y').to_euler()
    bind(unify([upper,muscle],label+' musculo continuo'),'Superior.'+label)
    bind(segment(label+' inferior',knee,foot,.10,.060,skin),'Inferior.'+label)
    bind(sphere(label+' joelho cotovelo',knee,(.105,.095,.10),head_skin),'Inferior.'+label)
    palm=sphere(label+' pe mao',foot+Vector((.075 if kind=='Perna' else .018,0,0)),(.17 if kind=='Perna' else .085,.105,.045 if kind=='Perna' else .07),skin)
    bind(palm,'Inferior.'+label)
    for j in range(4 if kind=='Perna' else 5):
        y=(j-1.5)*.047 if kind=='Perna' else (j-2)*.035
        a=foot+Vector((.14,y,.0)) if kind=='Perna' else foot+Vector((.03,y,-.045))
        b=a+Vector((.09,y*.25,-.01)) if kind=='Perna' else a+Vector((.055,y*.4,-.13))
        c=b+Vector((.025,0,-.015)) if kind=='Perna' else b+Vector((-.03,0,-.03))
        bind(segment(label+' dedo',a,b,.025,.013,skin),'Inferior.'+label)
        bind(segment(label+' unha',b,c,.014,.003,teeth_mat),'Inferior.'+label)
    guide=ctrl('Guia apoio '+label,foot); parent_keep(guide,body)
    def solve(endpoint,frame):
        scene.frame_set(frame); endpoint=Vector(endpoint)
        a=(knee-hip).length; b=(foot-knee).length; d=(endpoint-hip).normalized()
        distance=max(.01,min((endpoint-hip).length,a+b-.0001)); along=(a*a-b*b+distance*distance)/(2*distance)
        bend=knee-hip-d*(knee-hip).dot(d)
        if bend.length<.001: bend=Vector((0,0,1))-d*d.z
        new_knee=hip+d*along+bend.normalized()*math.sqrt(max(0,a*a-along*along)); end=hip+d*distance
        for name,old_a,old_b,new_a,new_b in [('Superior.'+label,hip,knee,hip,new_knee),('Inferior.'+label,knee,foot,new_knee,end)]:
            pb=rig.pose.bones[name]; pb.rotation_mode='QUATERNION'
            rot=(old_b-old_a).rotation_difference(new_b-new_a).to_matrix().to_4x4()
            pb.matrix=Matrix.Translation(new_a)@rot@Matrix.Translation(-old_a)@pb.bone.matrix_local
            bpy.context.view_layer.update(); pb.keyframe_insert(data_path='rotation_quaternion',frame=frame); pb.keyframe_insert(data_path='location',frame=frame)
        guide.location=end; guide.keyframe_insert(data_path='location',frame=frame)
    for frame in range(1,34):
        phase=((frame-1)/32+(0 if s<0 else .5))%1
        if phase<.6: x=.30-.60*(phase/.6); lift=0
        else: t=(phase-.6)/.4; x=-.30+.60*t; lift=.10*math.sin(math.pi*t)
        if kind=='Braco': x=-.07*math.cos(phase*math.tau); lift=.015*math.sin(phase*math.tau)
        solve(foot+Vector((x,0,lift)),frame)
    for frame in range(101,146):
        load=min(1,(frame-101)/28) if frame<=129 else max(0,1-(frame-129)/16)
        solve(foot+Vector((-.075*load,0,.02*load)) if kind=='Braco' else foot,frame)

all_frames=list(range(1,34))+list(range(101,146))
for frame in all_frames:
    scene.frame_set(frame); inflation=1; retract=thrust=jaw_open=0
    if 101<=frame<=129:
        t=(frame-101)/28; inflation=1+.23*t; retract=.075*t; jaw_open=math.radians(13)*t
    elif 130<=frame<=134:
        t=(frame-129)/5; inflation=1.23-.38*t; retract=.075*(1-t); thrust=.16*t; jaw_open=math.radians(13)+math.radians(14)*t
    elif frame>=135:
        t=(frame-134)/11; inflation=.85+.15*t; thrust=.16*(1-t); jaw_open=math.radians(27)*(1-t)
    for c in pouches:
        c.scale=(inflation,inflation,1+(inflation-1)*.75); c.keyframe_insert(data_path='scale',frame=frame)
    head.location=(.12-retract+thrust,0,1.54)
    head.rotation_euler=(0,-math.radians(8)*(retract/.075)+math.radians(5)*(thrust/.16),0)
    head.keyframe_insert(data_path='location',frame=frame); head.keyframe_insert(data_path='rotation_euler',frame=frame)
    jaw.rotation_euler=(0,jaw_open,0); jaw.keyframe_insert(data_path='rotation_euler',frame=frame)
for o in scene.objects:
    if o.animation_data and o.animation_data.action:
        for curve in o.animation_data.action.fcurves:
            for point in curve.keyframe_points: point.interpolation='LINEAR'
for name,frame in [('Caminhada',1),('Fechar ciclo',33),('Inflar papo - 1.2s',101),('Papo cheio',129),('Cuspe',130),('Recuperacao',135),('Parado',145)]:
    scene.timeline_markers.new(name,frame=frame)

def camera(name,loc,target,scale):
    d=bpy.data.cameras.new(name); d.type='ORTHO'; d.ortho_scale=scale
    o=bpy.data.objects.new(name,d); scene.collection.objects.link(o); o.location=loc
    o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler(); return o
top=camera('Camera jogo - topo',(0,0,6),(0,0,0),2.4); top.rotation_euler=(0,0,-math.pi/2)
perspective=camera('Camera inspecao',(4,-5,3.8),(0,0,.95),2.8)
world=bpy.data.worlds.new('Ambiente suave'); world.use_nodes=True; world.node_tree.nodes['Background'].inputs[0].default_value=(.15,.15,.13,1); world.node_tree.nodes['Background'].inputs[1].default_value=.4; scene.world=world
for name,loc,power,size in [('Principal',(2,-3,5),230,4),('Preenchimento',(-3,2,3),90,3),('Recorte',(-2,-1,4),100,2)]:
    d=bpy.data.lights.new(name,'AREA'); d.energy=power; d.shape='DISK'; d.size=size
    o=bpy.data.objects.new(name,d); scene.collection.objects.link(o); o.location=loc; o.rotation_euler=(Vector((0,0,1))-o.location).to_track_quat('-Z','Y').to_euler()
scene.render.engine='CYCLES'; scene.cycles.samples=32; scene.cycles.use_denoising=True
scene.render.resolution_x=512; scene.render.resolution_y=512; scene.render.resolution_percentage=100
scene.render.film_transparent=True; scene.render.image_settings.file_format='PNG'; scene.render.image_settings.color_mode='RGBA'
scene.view_settings.view_transform='AgX'; scene.view_settings.look='AgX - Medium High Contrast'; scene.camera=top; scene.frame_set(145)
scene.render.filepath=str(OUT/'pose-parada.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'corrosivo-papo-inchado.blend'))
bpy.ops.render.render(write_still=True)
for action,frames in [('Walk',range(1,33)),('Spit',range(101,146))]:
    if '--poses-only' in sys.argv: frames=[1,17] if action=='Walk' else [101,129,134,145]
    folder=OUT/action; folder.mkdir(exist_ok=True)
    for frame in frames:
        scene.frame_set(frame); scene.render.filepath=str(folder/f'{frame:03}.png'); bpy.ops.render.render(write_still=True)
scene.camera=perspective; scene.frame_set(145)
scene.render.filepath=str(OUT/'modelo-perspectiva.png'); bpy.ops.render.render(write_still=True)
print('CORROSIVE MODEL AND ANIMATIONS READY',flush=True)
