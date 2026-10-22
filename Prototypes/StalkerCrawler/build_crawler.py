"""Editable four-limb crawler for the approved Stalker silhouette. Blender 4.5."""
import bpy, math, sys
from pathlib import Path
from mathutils import Vector, Matrix
OUT=Path(__file__).resolve().parent
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene; scene.render.fps=24; scene.frame_start=1; scene.frame_end=120
def material(name,bright,dark):
    mat=bpy.data.materials.new(name); mat.use_nodes=True
    nodes=mat.node_tree.nodes; links=mat.node_tree.links
    noise=nodes.new('ShaderNodeTexNoise'); noise.inputs['Scale'].default_value=11; noise.inputs['Detail'].default_value=3
    ramp=nodes.new('ShaderNodeValToRGB'); ramp.color_ramp.elements[0].color=(*dark,1); ramp.color_ramp.elements[1].color=(*bright,1)
    bsdf=nodes.get('Principled BSDF'); bsdf.inputs['Roughness'].default_value=.84
    links.new(noise.outputs['Fac'],ramp.inputs[0]); links.new(ramp.outputs[0],bsdf.inputs['Base Color'])
    return mat
skin=material('Pele escura ressecada',(.10,.075,.05),(.028,.023,.019))
bone=material('Ossos sob pele',(.16,.12,.08),(.07,.055,.04))
skull=material('Cabeca palida',(.40,.36,.32),(.18,.16,.14))
black=material('Olhos e fendas',(.025,.02,.015),(.005,.004,.003))
def sphere(name,loc,scale,mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24,ring_count=12,location=loc)
    obj=bpy.context.object; obj.name=name; obj.scale=scale; obj.data.materials.append(mat)
    for face in obj.data.polygons: face.use_smooth=True
    return obj
def segment(name,a,b,r1,r2,mat):
    a,b=Vector(a),Vector(b)
    bpy.ops.mesh.primitive_cone_add(vertices=12,radius1=r1,radius2=r2,depth=(b-a).length,location=(a+b)/2)
    obj=bpy.context.object; obj.name=name; obj.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler(); obj.data.materials.append(mat)
    bevel=obj.modifiers.new('Arestas suaves','BEVEL'); bevel.width=.014; bevel.segments=2
    obj.modifiers.new('Normais','WEIGHTED_NORMAL'); return obj
torso=[]
torso.append(sphere('Costas estreitas',(-.02,0,.59),(.58,.245,.18),skin))
torso.append(sphere('Torax',(.29,0,.61),(.28,.32,.18),skin))
torso.append(sphere('Cintura funda',(-.39,0,.48),(.28,.175,.145),skin))
torso.append(sphere('Pelvis',(-.64,0,.39),(.24,.27,.16),skin))
torso.append(sphere('Pescoco',(.53,0,.65),(.14,.13,.14),skin))
head=sphere('Cabeca calva',(.71,0,.76),(.225,.185,.22),skull)
head_parts=[head,sphere('Rosto estreito',(.85,0,.72),(.13,.12,.10),skin)]
for side in [-1,1]:
    torso.append(sphere('Escapula '+str(side),(.18,side*.20,.745),(.18,.065,.028),bone))
    torso.append(sphere('Quadril saliente '+str(side),(-.65,side*.19,.51),(.11,.065,.028),bone))
    head_parts.append(sphere('Olho '+str(side),(.855,side*.082,.79),(.023,.018,.018),black))
    head_parts.append(sphere('Orelha '+str(side),(.70,side*.19,.75),(.06,.025,.07),skin))
for i in range(7):
    x=.37-i*.125
    z=.59+.18*math.sqrt(max(0,1-((x+.02)/.58)**2))
    if abs(x-.29)<.28: z=max(z,.61+.18*math.sqrt(max(0,1-((x-.29)/.28)**2)))
    torso.append(sphere('Vertebra '+str(i),(x,0,z+.006),(.033,.035,.024),bone))

def control(name,loc):
    obj=bpy.data.objects.new(name,None); scene.collection.objects.link(obj)
    obj.location=loc; obj.empty_display_size=.12; return obj
def parent_keep(obj,parent):
    bpy.context.view_layer.update(); world=obj.matrix_world.copy()
    obj.parent=parent; obj.matrix_world=world
head_control=control('CTRL - Cabeca mordida',(.53,0,.65))
for obj in head_parts: parent_keep(obj,head_control)
jaw_control=control('CTRL - Mandibula',(.82,0,.66)); parent_keep(jaw_control,head_control)
jaw_parts=[sphere('Mandibula inferior',(.925,0,.64),(.14,.108,.035),skin),
           sphere('Interior da boca',(.94,0,.677),(.11,.085,.018),black)]
for y in [-.073,-.035,0,.035,.073]:
    jaw_parts.append(segment('Dente inferior',(.985,y,.663),(.995,y,.704),.012,.003,skull))
    tooth=segment('Dente superior',(.984,y,.728),(.994,y,.694),.012,.003,skull)
    parent_keep(tooth,head_control)
for obj in jaw_parts: parent_keep(obj,jaw_control)
for frame in list(range(1,34))+list(range(101,121)):
    scene.frame_set(frame)
    retract=thrust=opening=0
    if 101<=frame<=108:
        t=(frame-101)/7; retract=.10*t; opening=math.radians(48)*t
    elif 108<frame<=112:
        t=(frame-108)/4; retract=.10*(1-t); thrust=.43*t
        opening=math.radians(48)*(1-t)
    elif 112<frame<=120:
        t=(frame-112)/8; thrust=.43*(1-t)
    head_control.location=(.53-retract+thrust,0,.65-.045*(thrust/.43))
    head_control.rotation_euler=(0,math.radians(8)*(thrust/.43)-math.radians(25)*(retract/.10),0)
    jaw_control.rotation_euler=(0,opening,0)
    neck=bpy.data.objects['Pescoco']
    neck.location=(.53+(thrust-retract)*.5,0,.65-.02*(thrust/.43))
    neck.scale=(.14+thrust*.5,.13,.14)
    neck.keyframe_insert(data_path='location',frame=frame)
    neck.keyframe_insert(data_path='scale',frame=frame)
    head_control.keyframe_insert(data_path='location',frame=frame)
    head_control.keyframe_insert(data_path='rotation_euler',frame=frame)
    jaw_control.keyframe_insert(data_path='rotation_euler',frame=frame)

arm=bpy.data.armatures.new('Esqueleto Rastejante'); rig=bpy.data.objects.new('RIG - Rastejante',arm)
scene.collection.objects.link(rig); bpy.ops.object.select_all(action='DESELECT'); rig.select_set(True)
bpy.context.view_layer.objects.active=rig; bpy.ops.object.mode_set(mode='EDIT')
specs=[]
for side in [-1,1]:
    for kind in ['Braco','Perna']:
        label=kind+('.D' if side<0 else '.E')
        if kind=='Braco': hip=Vector((.35,side*.29,.62)); knee=Vector((.03,side*.75,.45)); foot=Vector((-.37,side*.78,.07))
        else: hip=Vector((-.62,side*.20,.40)); knee=Vector((-.78,side*.44,.24)); foot=Vector((-1.23,side*.34,.055))
        upper=arm.edit_bones.new('Superior.'+label); upper.head=hip; upper.tail=knee
        lower=arm.edit_bones.new('Inferior.'+label); lower.head=knee; lower.tail=foot; lower.parent=upper; lower.use_connect=True
        specs.append((kind,side,label,hip,knee,foot))
bpy.ops.object.mode_set(mode='OBJECT'); rig.show_in_front=True
def bind(obj,bone_name):
    bpy.ops.object.select_all(action='DESELECT'); obj.select_set(True); bpy.context.view_layer.objects.active=obj
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    group=obj.vertex_groups.new(name=bone_name); group.add(list(range(len(obj.data.vertices))),1,'REPLACE')
    mod=obj.modifiers.new('Segue osso','ARMATURE'); mod.object=rig
for kind,side,label,hip,knee,foot in specs:
    upper=segment(label+' musculo',hip,knee,.10 if kind=='Braco' else .12,.067,skin); bind(upper,'Superior.'+label)
    lower=segment(label+' membro fino',knee,foot,.066,.035,skin); bind(lower,'Inferior.'+label)
    joint=sphere(label+' articulacao',knee,(.073,.067,.063),bone); bind(joint,'Inferior.'+label)
    socket=sphere(label+' ombro quadril',hip,(.115,.115,.105),skin); bind(socket,'Superior.'+label)
    palm=sphere(label+' palma' if kind=='Braco' else label+' pe',foot,(.095,.11 if kind=='Braco' else .065,.035),skin); bind(palm,'Inferior.'+label)
    for digit in range(5 if kind=='Braco' else 3):
        y=(digit-2)*.035 if kind=='Braco' else (digit-1)*.027
        a=foot+Vector((-.03,y,.005)); b=foot+Vector((-.13-abs(y)*.35,y*1.8,.003)); c=b+Vector((-.08,y*.35,-.01))
        finger=segment(label+' dedo '+str(digit),a,b,.025,.017,skin); bind(finger,'Inferior.'+label)
        tip=segment(label+' ponta '+str(digit),b,c,.017,.006,bone); bind(tip,'Inferior.'+label)
    guide=bpy.data.objects.new('Guia contato '+label,None); scene.collection.objects.link(guide); guide.empty_display_size=.08
    def solve(endpoint,frame):
        endpoint=Vector(endpoint); scene.frame_set(frame)
        a=(knee-hip).length; b=(foot-knee).length; direction=(endpoint-hip).normalized()
        distance=min((endpoint-hip).length,a+b-.0001); along=(a*a-b*b+distance*distance)/(2*distance)
        bend=knee-hip-direction*(knee-hip).dot(direction)
        if bend.length<.001: bend=Vector((0,0,1))-direction*direction.z
        new_knee=hip+direction*along+bend.normalized()*math.sqrt(max(0,a*a-along*along)); endpoint=hip+direction*distance
        for name,old_a,old_b,new_a,new_b in [('Superior.'+label,hip,knee,hip,new_knee),('Inferior.'+label,knee,foot,new_knee,endpoint)]:
            bone_obj=rig.pose.bones[name]; bone_obj.rotation_mode='QUATERNION'
            rotation=(old_b-old_a).rotation_difference(new_b-new_a).to_matrix().to_4x4()
            bone_obj.matrix=Matrix.Translation(new_a)@rotation@Matrix.Translation(-old_a)@bone_obj.bone.matrix_local
            bpy.context.view_layer.update(); bone_obj.keyframe_insert(data_path='rotation_quaternion',frame=frame); bone_obj.keyframe_insert(data_path='location',frame=frame)
        guide.location=endpoint; guide.keyframe_insert(data_path='location',frame=frame)
    for frame in range(1,34):
        phase=((frame-1)/32+(0 if side<0 else .5)+(.5 if kind=='Perna' else 0))%1
        if phase<.5: offset=.13-.52*phase; lift=0
        else: offset=-.13+.52*(phase-.5); lift=.09*math.sin((phase-.5)*math.tau)
        solve(foot+Vector((offset,0,lift)),frame)
    for frame in range(101,121):
        solve(foot,frame)
for obj in [rig,head_control,jaw_control,neck]+[o for o in scene.objects if o.name.startswith('Guia contato')]:
    for curve in obj.animation_data.action.fcurves:
        for point in curve.keyframe_points: point.interpolation='LINEAR'
for name,frame in [('Caminhada',1),('Fechar ciclo',33),('Abrir mandibula',101),('Mordida',109),('Retorno',113),('Parado',120)]: scene.timeline_markers.new(name,frame=frame)
def camera(name,loc,scale):
    data=bpy.data.cameras.new(name); data.type='ORTHO'; data.ortho_scale=scale
    obj=bpy.data.objects.new(name,data); scene.collection.objects.link(obj); obj.location=loc; return obj
top=camera('Camera topo',(0,0,6),3.3); top.rotation_euler=(0,0,-math.pi/2)
side=camera('Camera inspecao',(3,-4,2.4),3.3); side.rotation_euler=(Vector((0,0,.45))-side.location).to_track_quat('-Z','Y').to_euler()
scene.camera=top
world=bpy.data.worlds.new('Ambiente'); world.use_nodes=True; world.node_tree.nodes['Background'].inputs[0].default_value=(.11,.11,.11,1); scene.world=world
for name,loc,power,size in [('Principal',(1,-2,4),200,3),('Preenchimento',(-2,2,3),110,3)]:
    data=bpy.data.lights.new(name,'AREA'); data.energy=power; data.shape='DISK'; data.size=size
    obj=bpy.data.objects.new(name,data); scene.collection.objects.link(obj); obj.location=loc; obj.rotation_euler=(-obj.location).to_track_quat('-Z','Y').to_euler()
scene.render.engine='CYCLES'; scene.cycles.samples=16; scene.render.resolution_x=384; scene.render.resolution_y=384
scene.render.resolution_percentage=100; scene.render.film_transparent=True
scene.render.image_settings.file_format='PNG'; scene.render.image_settings.color_mode='RGBA'; scene.view_settings.view_transform='AgX'
scene.frame_set(1); scene.render.filepath=str(OUT/'idle.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'espreitador-rastejante.blend'))
for action,frames in [('Walk',range(1,33)),('Attack',range(101,121))]:
    if '--attack-only' in sys.argv and action=='Walk': continue
    folder=OUT/action; folder.mkdir(exist_ok=True)
    for frame in frames:
        scene.frame_set(frame); scene.render.filepath=str(folder/f'{frame:03}.png'); bpy.ops.render.render(write_still=True)
scene.camera=side; scene.frame_set(108)
scene.render.filepath=str(OUT/'mordida-perspectiva.png'); bpy.ops.render.render(write_still=True)
print('CRAWLER MODEL AND ANIMATIONS READY',flush=True)
