"""Original pale spider, eight articulated legs and editable alternating gait / web cast."""
import bpy, math, sys
from pathlib import Path
from mathutils import Vector, Matrix
OUT=Path(__file__).resolve().parent
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene; scene.render.fps=24
scene.frame_start=1; scene.frame_end=128

def material(name,light,dark,roughness=.8):
    mat=bpy.data.materials.new(name); mat.use_nodes=True
    nodes=mat.node_tree.nodes; links=mat.node_tree.links
    bsdf=nodes.get('Principled BSDF'); bsdf.inputs['Roughness'].default_value=roughness
    noise=nodes.new('ShaderNodeTexNoise'); noise.inputs['Scale'].default_value=9; noise.inputs['Detail'].default_value=2
    ramp=nodes.new('ShaderNodeValToRGB'); ramp.color_ramp.elements[0].position=.38; ramp.color_ramp.elements[0].color=(*dark,1)
    ramp.color_ramp.elements[1].position=.58; ramp.color_ramp.elements[1].color=(*light,1)
    links.new(noise.outputs['Fac'],ramp.inputs[0]); links.new(ramp.outputs[0],bsdf.inputs['Base Color'])
    return mat
ivory=material('Quitina palida - manchas',(.73,.69,.58),(.32,.28,.22))
legs_mat=material('Patas marfim',(.64,.61,.53),(.47,.44,.37))
head_mat=material('Cabeca cinza quente',(.14,.125,.10),(.07,.06,.045))
dark=material('Juntas e presas',(.18,.15,.11),(.09,.075,.05))
eyes_mat=material('Olhos pretos',(.022,.018,.015),(.008,.007,.006),.22)
def sphere(name,loc,scale,mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24,ring_count=12,location=loc)
    obj=bpy.context.object; obj.name=name; obj.scale=scale; obj.data.materials.append(mat)
    for face in obj.data.polygons: face.use_smooth=True
    return obj
def segment(name,a,b,r1,r2,mat):
    a,b=Vector(a),Vector(b)
    bpy.ops.mesh.primitive_cone_add(vertices=12,radius1=r1,radius2=r2,depth=(b-a).length,location=(a+b)/2)
    obj=bpy.context.object; obj.name=name; obj.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler(); obj.data.materials.append(mat)
    bevel=obj.modifiers.new('Arestas suaves','BEVEL'); bevel.width=.013; bevel.segments=2
    obj.modifiers.new('Normais','WEIGHTED_NORMAL')
    return obj
abdomen=sphere('Abdomen palido',(-.48,0,.57),(.69,.48,.39),ivory)
sphere('Cefalotorax',(.28,0,.52),(.43,.34,.24),head_mat)
for i,(x,y,z,r) in enumerate([(.57,-.16,.69,.044),(.63,-.075,.69,.052),(.63,.075,.69,.052),(.57,.16,.69,.044),
                             (.48,-.22,.68,.031),(.48,.22,.68,.031),(.36,-.27,.66,.026),(.36,.27,.66,.026)]):
    sphere('Olho '+str(i+1),(x,y,z),(r,r,r*.8),eyes_mat)
fangs=[]
for side in [-1,1]:
    fang=segment('Mandibula '+str(side),(.58,side*.13,.47),(.83,side*.17,.40),.022,.063,dark); fangs.append(fang)

arm=bpy.data.armatures.new('Esqueleto oito patas')
rig=bpy.data.objects.new('RIG - Aranha Palida',arm); scene.collection.objects.link(rig)
bpy.context.view_layer.objects.active=rig; rig.select_set(True); bpy.ops.object.mode_set(mode='EDIT')
leg_specs=[]
for side in [-1,1]:
    for index,(hip_x,foot_x,foot_y) in enumerate([(.38,1.20,.92),(.18,.65,1.35),(-.05,-.35,1.42),(-.28,-1.12,1.05)]):
        label=('D' if side<0 else 'E')+str(index+1)
        hip=Vector((hip_x,side*.25,.52))
        knee=Vector((hip_x*.35+foot_x*.65,side*(foot_y*.62),.64))
        foot=Vector((foot_x,side*foot_y,.07))
        upper=arm.edit_bones.new('Femur.'+label); upper.head=hip; upper.tail=knee
        lower=arm.edit_bones.new('Tibia.'+label); lower.head=knee; lower.tail=foot; lower.parent=upper; lower.use_connect=True
        leg_specs.append((side,index,label,hip,knee,foot))
bpy.ops.object.mode_set(mode='OBJECT'); rig.show_in_front=True
def bind(obj,bone):
    bpy.context.view_layer.objects.active=obj; bpy.ops.object.select_all(action='DESELECT'); obj.select_set(True)
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    group=obj.vertex_groups.new(name=bone); group.add(list(range(len(obj.data.vertices))),1,'REPLACE')
    mod=obj.modifiers.new('Articulacao','ARMATURE'); mod.object=rig
targets=[]
for side,index,label,hip,knee,foot in leg_specs:
    target=bpy.data.objects.new('CTRL - Pe '+label,None); scene.collection.objects.link(target)
    target.location=foot; target.empty_display_size=.10; target.empty_display_type='SPHERE'
    pole=bpy.data.objects.new('CTRL - Joelho '+label,None); scene.collection.objects.link(pole)
    pole.location=knee+Vector((0,0,1.5)); pole.empty_display_size=.07
    upper=segment('Pata '+label+' - femur',hip,knee,.10,.073,legs_mat); bind(upper,'Femur.'+label)
    lower=segment('Pata '+label+' - tibia',knee,foot,.07,.009,legs_mat); bind(lower,'Tibia.'+label)
    joint=sphere('Pata '+label+' - junta',knee,(.063,.063,.063),dark); bind(joint,'Tibia.'+label)
    socket=sphere('Pata '+label+' - raiz',hip,(.085,.085,.085),legs_mat); bind(socket,'Femur.'+label)
    targets.append((side,index,target,foot))
    def solve_leg(endpoint,frame):
        # Analytic two-segment IK baked to editable bone keys: no pole-roll flips or stretching.
        endpoint=Vector(endpoint); scene.frame_set(frame)
        a=(knee-hip).length; b=(foot-knee).length
        axis=(endpoint-hip).normalized(); distance=min((endpoint-hip).length,a+b-.0001)
        along=(a*a-b*b+distance*distance)/(2*distance)
        bend=knee-hip-axis*(knee-hip).dot(axis)
        if bend.length<.001: bend=Vector((0,0,1))-axis*axis.z
        new_knee=hip+axis*along+bend.normalized()*math.sqrt(max(0,a*a-along*along))
        endpoint=hip+axis*distance
        for bone_name,old_a,old_b,new_a,new_b in [('Femur.'+label,hip,knee,hip,new_knee),('Tibia.'+label,knee,foot,new_knee,endpoint)]:
            bone=rig.pose.bones[bone_name]; bone.rotation_mode='QUATERNION'
            rotation=(old_b-old_a).rotation_difference(new_b-new_a).to_matrix().to_4x4()
            bone.matrix=Matrix.Translation(new_a)@rotation@Matrix.Translation(-old_a)@bone.bone.matrix_local
            bpy.context.view_layer.update()
            bone.keyframe_insert(data_path='rotation_quaternion',frame=frame)
            bone.keyframe_insert(data_path='location',frame=frame)
    for frame in range(1,34):
        phase=((frame-1)/32+(index%2)*.5+(0 if side<0 else .5))%1
        if phase<.5: offset=.20-.80*phase; lift=0
        else: offset=-.20+.80*(phase-.5); lift=.16*math.sin((phase-.5)*math.tau)
        target.location=foot+Vector((offset,0,lift)); target.keyframe_insert(data_path='location',frame=frame)
        solve_leg(target.location,frame)
    for frame in range(101,129):
        t=min(1,(frame-101)/18); t=t*t*(3-2*t)
        if frame>=120: t=max(0,1-(frame-120)/8)
        offset=Vector((-.20,side*-.12,.32))*t if index==0 else Vector((0,0,0))
        target.location=foot+offset; target.keyframe_insert(data_path='location',frame=frame)
        solve_leg(target.location,frame)

for frame in range(101,129):
    t=min(1,(frame-101)/18); t=t*t*(3-2*t)
    if frame>=120: t=max(0,1-(frame-120)/8)
    abdomen.location=(-.48-.035*t,0,.57+.12*t); abdomen.rotation_euler.y=math.radians(16*t)
    abdomen.keyframe_insert(data_path='location',frame=frame); abdomen.keyframe_insert(data_path='rotation_euler',frame=frame)
for frame in [1,33]:
    abdomen.location=(-.48,0,.57); abdomen.rotation_euler.y=0
    abdomen.keyframe_insert(data_path='location',frame=frame); abdomen.keyframe_insert(data_path='rotation_euler',frame=frame)
for obj in [entry[2] for entry in targets]+[rig]:
    # Explicit linear interpolation preserves the monotonic foot trajectory in each half-cycle.
    for curve in obj.animation_data.action.fcurves:
        for point in curve.keyframe_points: point.interpolation='LINEAR'
for name,frame in [('Caminhada inicio',1),('Caminhada fim / ciclo',33),('Preparar teia',101),('Disparar teia',120),('Repouso',128)]:
    scene.timeline_markers.new(name,frame=frame)
def camera(name,position,scale):
    data=bpy.data.cameras.new(name); data.type='ORTHO'; data.ortho_scale=scale
    obj=bpy.data.objects.new(name,data); scene.collection.objects.link(obj); obj.location=position
    obj.rotation_euler=(0,0,-math.pi/2); return obj
top=camera('Camera topo - cabeca para cima',(0,0,6),3.65)
side=camera('Camera inspecao',(3,-4,3),4)
side.rotation_euler=(Vector((0,0,.4))-side.location).to_track_quat('-Z','Y').to_euler()
scene.camera=top
world=bpy.data.worlds.new('Ambiente neutro'); world.use_nodes=True; world.node_tree.nodes['Background'].inputs[0].default_value=(.18,.18,.18,1)
scene.world=world
for name,loc,power,size in [('Principal',(1,-2,5),450,4),('Preenchimento',(-2,2,3),240,3)]:
    light=bpy.data.lights.new(name,'AREA'); light.energy=power; light.shape='DISK'; light.size=size
    obj=bpy.data.objects.new(name,light); scene.collection.objects.link(obj); obj.location=loc
    obj.rotation_euler=(-obj.location).to_track_quat('-Z','Y').to_euler()
scene.render.engine='CYCLES'; scene.cycles.samples=16
scene.render.resolution_x=384; scene.render.resolution_y=384; scene.render.resolution_percentage=100
scene.render.film_transparent=True; scene.render.image_settings.file_format='PNG'; scene.render.image_settings.color_mode='RGBA'
scene.view_settings.view_transform='AgX'
scene.frame_set(1); scene.render.filepath=str(OUT/'idle.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'aranha-palida.blend'))
for name,frames in [('Walk',range(1,33)),('Web',range(101,129))]:
    folder=OUT/name; folder.mkdir(exist_ok=True)
    if '--preview-only' in sys.argv: frames=[1] if name=='Walk' else [119]
    for frame in frames:
        scene.frame_set(frame); scene.render.filepath=str(folder/f'{frame:03}.png'); bpy.ops.render.render(write_still=True)
print('PALE SPIDER MODEL AND ANIMATIONS READY',flush=True)
