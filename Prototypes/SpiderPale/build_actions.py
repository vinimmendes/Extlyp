"""Additional actions on the approved appended scene; never rebuild the source spider."""
import bpy, math
from pathlib import Path
from mathutils import Vector, Matrix

BASE=Path(__file__).resolve().parent/'Actions'
BASE.mkdir(exist_ok=True)
scene=bpy.context.scene
rig=scene.objects.get('RIG - Aranha Palida')
if rig is None:
    raise RuntimeError('Append the approved spider scene before running this authoring script.')
if scene.objects.get('CTRL-WeaverActions'):
    raise RuntimeError('Action root already exists; inspect the current scene before reauthoring.')
scene.frame_set(128)
root=bpy.data.objects.new('CTRL-WeaverActions',None)
scene.collection.objects.link(root)
for obj in list(scene.objects):
    if obj!=root and obj.type not in {'CAMERA','LIGHT'} and obj.parent is None:
        obj.parent=root
spider_meshes=[obj for obj in scene.objects if obj.type=='MESH']
abdomen=scene.objects['Abdomen palido']
root.rotation_mode='XYZ'

def pose_leg(label, endpoint, frame):
    upper=rig.data.bones['Femur.'+label]
    lower=rig.data.bones['Tibia.'+label]
    hip,knee,foot=upper.head_local.copy(),upper.tail_local.copy(),lower.tail_local.copy()
    a,b=(knee-hip).length,(foot-knee).length
    axis=(endpoint-hip).normalized()
    distance=max(.01,min((endpoint-hip).length,a+b-.0001))
    along=(a*a-b*b+distance*distance)/(2*distance)
    bend=knee-hip-axis*(knee-hip).dot(axis)
    if bend.length<.001: bend=Vector((0,0,1))-axis*axis.z
    new_knee=hip+axis*along+bend.normalized()*math.sqrt(max(0,a*a-along*along))
    endpoint=hip+axis*distance
    for name,old_a,old_b,new_a,new_b in [
        ('Femur.'+label,hip,knee,hip,new_knee),('Tibia.'+label,knee,foot,new_knee,endpoint)]:
        bone=rig.pose.bones[name]; bone.rotation_mode='QUATERNION'
        rotation=(old_b-old_a).rotation_difference(new_b-new_a).to_matrix().to_4x4()
        bone.matrix=Matrix.Translation(new_a)@rotation@Matrix.Translation(-old_a)@bone.bone.matrix_local
        bpy.context.view_layer.update()
        bone.keyframe_insert(data_path='rotation_quaternion',frame=frame)
        bone.keyframe_insert(data_path='location',frame=frame)

for action,start in [('Dash',201),('Descend',301)]:
    for index in range(32):
        frame=start+index; scene.frame_set(frame)
        t=index/31
        root.location=(0,0,0); root.rotation_euler=(0,0,0); root.scale=(1,1,1)
        abdomen.location=(-.48,0,.57); abdomen.rotation_euler=(0,0,0)
        if action=='Dash':
            preparation=min(1,index/11) if index<12 else 0
            extension=1 if 12<=index<18 else 0
            relax=max(0,1-(index-18)/13) if index>=18 else 0
            root.rotation_euler.y=math.radians(9*preparation-7*extension-4*relax)
            root.scale.z=1-.17*preparation+.06*extension
        else:
            opening=max(0,min(1,(t-.15)/.65)); opening=opening*opening*(3-2*opening)
            root.location.z=1.4*(1-min(1,t/.8))
            root.rotation_euler.x=math.radians(6)*math.sin(t*math.tau*1.5)*(1-t)
            root.rotation_euler.y=math.radians(-16)*(1-opening)
            if t>.8: root.scale.z=1-.18*math.sin((t-.8)/.2*math.pi)
        for side in ['D','E']:
            sign=-1 if side=='D' else 1
            for number in range(1,5):
                label=side+str(number)
                foot=rig.data.bones['Tibia.'+label].tail_local.copy()
                if action=='Dash':
                    front=number<=2
                    foot+=Vector(((-.28 if front else .14)*preparation+
                        (.35 if front else -.28)*extension+(.12 if front else -.12)*relax,
                        -sign*(.13*preparation+.10*extension),
                        (.26 if front else .06)*preparation+.12*extension+.04*relax))
                else:
                    hip=rig.data.bones['Femur.'+label].head_local.copy()
                    curled=hip+Vector(((foot.x-hip.x)*.45,sign*.33,-.03))
                    foot=curled.lerp(foot,opening)
                pose_leg(label,foot,frame)
        for path in ['location','rotation_euler','scale']: root.keyframe_insert(data_path=path,frame=frame)
        abdomen.keyframe_insert(data_path='location',frame=frame)
        abdomen.keyframe_insert(data_path='rotation_euler',frame=frame)

# A distinct, original ceiling web: ragged radial strands and a dense woven center.
silk=bpy.data.materials.new('MAT-WeaverCeilingSilk'); silk.use_nodes=True
shader=next((node for node in silk.node_tree.nodes if node.type=='BSDF_PRINCIPLED'),None)
if shader is None:
    shader=silk.node_tree.nodes.new('ShaderNodeBsdfPrincipled')
    output=silk.node_tree.nodes.new('ShaderNodeOutputMaterial')
    silk.node_tree.links.new(shader.outputs['BSDF'],output.inputs['Surface'])
shader.inputs['Base Color'].default_value=(.48,.47,.39,1)
shader.inputs['Roughness'].default_value=.92
web_root=bpy.data.objects.new('CTRL-WeaverCeilingWeb',None); scene.collection.objects.link(web_root)
def strand(name,points,width=.011):
    curve=bpy.data.curves.new(name,'CURVE'); curve.dimensions='3D'; curve.bevel_depth=width; curve.bevel_resolution=2
    spline=curve.splines.new('POLY'); spline.points.add(len(points)-1)
    for point,co in zip(spline.points,points): point.co=(*co,1)
    obj=bpy.data.objects.new(name,curve); scene.collection.objects.link(obj); obj.parent=web_root
    curve.materials.append(silk); return obj
for spoke in range(16):
    angle=spoke*math.tau/16
    length=1.2+.15*math.sin(spoke*8.7)
    strand('GEO-WebSpoke-%02d'%spoke,[(math.cos(angle)*r,math.sin(angle)*r,.15+.08*(1-r)) for r in [0,.25,.6,length]],.012)
for ring in range(1,10):
    points=[]
    for i in range(65):
        angle=i/64*math.tau; radius=ring*.125*(1+.07*math.sin(angle*5+ring))
        points.append((math.cos(angle)*radius,math.sin(angle)*radius,.16+.04*math.sin(angle*3)))
    strand('GEO-WebRing-%02d'%ring,points,.009 if ring>3 else .016)
for i in range(14):
    angle=i*2.39996
    strand('GEO-WebCore-%02d'%i,[(math.cos(angle)*.35,math.sin(angle)*.35,.18),
        (math.cos(angle+1.5)*.12,math.sin(angle+1.5)*.12,.24),
        (-math.cos(angle)*.35,-math.sin(angle)*.35,.18)],.014)
web_objects=[o for o in scene.objects if o.parent==web_root]
for obj in spider_meshes:
    for frame,hidden in [(1,False),(399,False),(400,True),(401,True)]:
        obj.hide_render=hidden; obj.keyframe_insert(data_path='hide_render',frame=frame)
for obj in web_objects:
    for frame,hidden in [(1,True),(399,True),(400,False),(401,False)]:
        obj.hide_render=hidden; obj.keyframe_insert(data_path='hide_render',frame=frame)
for frame,name in [(201,'Dash - anticipation'),(213,'Dash - thrust'),(219,'Dash - recovery'),(301,'Ceiling descent'),(326,'Landing'),(401,'Ceiling web')]:
    scene.timeline_markers.new(name,frame=frame)
scene.frame_end=401
scene.render.resolution_x=scene.render.resolution_y=384
scene.render.resolution_percentage=100
scene.cycles.samples=16
scene.render.film_transparent=True
scene.render.image_settings.file_format='PNG'; scene.render.image_settings.color_mode='RGBA'
scene.frame_set(201)
bpy.ops.wm.save_as_mainfile(filepath=str(BASE/'aranha-palida-acoes.blend'))
print('Authored Dash[201..232], Descend[301..332], Nest[401] in',BASE)
