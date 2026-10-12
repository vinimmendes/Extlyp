"""Run with Blender --background --python build_walk.py. No external packages."""
import bpy, math
from pathlib import Path
from mathutils import Vector

OUT = Path(__file__).resolve().parent
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.frame_start, scene.frame_end = 1, 32
scene.render.fps = 24

def material(name, color):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1)
    return m
olive = material('Tronco verde', (.26,.34,.23))
skin = material('Cabeca e maos', (.68,.49,.32))
pack = material('Mochila', (.28,.21,.12))
gun = material('Pistola', (.10,.12,.14))
right = material('Perna direita - laranja', (.9,.32,.08))
left = material('Perna esquerda - azul', (.08,.48,.85))

def finish(obj, name, mat):
    obj.name = name
    obj.data.materials.append(mat)
    for p in obj.data.polygons: p.use_smooth = True
    return obj

def ellipsoid(name, pos, scale, mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=12, location=pos)
    o = finish(bpy.context.object, name, mat)
    o.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return o

def segment(name, a, b, radius, mat):
    a,b = Vector(a),Vector(b)
    bpy.ops.mesh.primitive_cylinder_add(vertices=20, radius=radius, depth=(b-a).length, location=(a+b)/2)
    o = finish(bpy.context.object, name, mat)
    o.rotation_mode='QUATERNION'
    o.rotation_quaternion=Vector((0,0,1)).rotation_difference(b-a)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    bevel=o.modifiers.new('Bordas arredondadas','BEVEL');bevel.width=.035;bevel.segments=3
    return o

ellipsoid('Tronco fixo', (0,0,1.33), (.39,.36,.43), olive)
ellipsoid('Cabeca fixa', (.14,0,1.79), (.235,.22,.24), skin)
ellipsoid('Mochila fixa', (-.32,0,1.37), (.20,.32,.34), pack)
for side in [-1,1]:
    shoulder=(.04,side*.31,1.57); elbow=(.36,side*.38,1.31); hand=(.68,side*.055,1.38)
    segment('Braco fixo',shoulder,elbow,.115,olive)
    segment('Antebraco fixo',elbow,hand,.09,olive)
    ellipsoid('Mao fixa',hand,(.10,.075,.075),skin)
segment('Cano pistola',(.70,0,1.42),(1.0,0,1.42),.048,gun)
segment('Cabo pistola',(.72,0,1.42),(.67,0,1.25),.05,gun)

# Two real two-bone IK chains. The feet are animation controls, not sprite offsets.
arm=bpy.data.armatures.new('Esqueleto pernas')
rig=bpy.data.objects.new('RIG - pernas IK',arm);scene.collection.objects.link(rig)
bpy.context.view_layer.objects.active=rig;rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
for label,y in [('Direita',-.18),('Esquerda',.18)]:
    upper=arm.edit_bones.new('Coxa.'+label);upper.head=(0,y,1.02);upper.tail=(.24,y,.54)
    lower=arm.edit_bones.new('Canela.'+label);lower.head=upper.tail;lower.tail=(0,y,.06)
    lower.parent=upper;lower.use_connect=True
bpy.ops.object.mode_set(mode='OBJECT')
rig.show_in_front=True

for label,y,mat,sign in [('Direita',-.18,right,1),('Esquerda',.18,left,-1)]:
    target=bpy.data.objects.new('Controle pe '+label,None);scene.collection.objects.link(target)
    target.empty_display_type='SPHERE';target.empty_display_size=.08
    pole=bpy.data.objects.new('Direcao joelho '+label,None);scene.collection.objects.link(pole)
    pole.location=(2,y,.55);pole.empty_display_type='PLAIN_AXES';pole.empty_display_size=.15
    ik=rig.pose.bones['Canela.'+label].constraints.new('IK');ik.target=target;ik.pole_target=pole;ik.chain_count=2
    ik.use_stretch=False
    for name,a,b in [('Coxa',(0,y,1.02),(.24,y,.54)),('Canela',(.24,y,.54),(0,y,.06))]:
        o=segment(name+' '+label,a,b,.105,mat)
        group=o.vertex_groups.new(name=name+'.'+label);group.add(list(range(len(o.data.vertices))),1,'REPLACE')
        mod=o.modifiers.new('Articulacao real','ARMATURE');mod.object=rig
    boot=ellipsoid('Bota '+label,(0,0,0),(.19,.105,.07),mat)
    boot.parent=target;boot.location=(.09,0,0)
    for f in range(1,34):
        p=(f-1)/32
        q=(-1+4*p if p<=.5 else 3-4*p)*sign
        advancing=(p<.5) if sign==1 else (p>=.5)
        lift=.10*math.sin(math.pi*((p% .5)*2)) if advancing else 0
        target.location=(.48*q,y,.08+lift)
        target.keyframe_insert(data_path='location',frame=f)
    for fc in target.animation_data.action.fcurves:
        for k in fc.keyframe_points:k.interpolation='LINEAR'
        fc.modifiers.new('CYCLES')

def camera(name,pos,look,scale):
    data=bpy.data.cameras.new(name);o=bpy.data.objects.new(name,data);scene.collection.objects.link(o)
    o.location=pos;o.rotation_euler=(Vector(look)-o.location).to_track_quat('-Z','Y').to_euler()
    data.type='ORTHO';data.ortho_scale=scale
    return o
top=camera('Camera jogo - topo',(0,0,8),(0,0,0),2.65)
# Explicit top orientation: forward/right on screen, right leg at screen bottom.
top.rotation_euler=(0,0,0)
side=camera('Camera inspecao',(3,-5,2.8),(0,0,1),2.8)
scene.camera=top
scene.render.engine='BLENDER_WORKBENCH'
scene.display.shading.light='STUDIO'
scene.display.shading.color_type='MATERIAL'
scene.display.shading.show_shadows=True
scene.display.shading.show_cavity=True
scene.display.shading.cavity_type='BOTH'
scene.display.shading.background_type='WORLD'
scene.world.color=(.055,.065,.06)
scene.render.resolution_x=640;scene.render.resolution_y=640;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA'
scene.render.film_transparent=True
scene.view_settings.view_transform='Standard'
scene.frame_set(9)
for obj in bpy.context.selected_objects:obj.select_set(False)
rig.select_set(True);bpy.context.view_layer.objects.active=rig
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':area.spaces.active.region_3d.view_perspective='CAMERA'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'boneco-caminhada.blend'))
for name,cam in [('topo',top),('lateral',side)]:
    (OUT/name).mkdir(exist_ok=True)
    scene.camera=cam
    for frame in range(1,33):
        scene.frame_set(frame);scene.render.filepath=str(OUT/name/f'{frame:03}.png')
        bpy.ops.render.render(write_still=True)
print('DELIVERY_READY',OUT)
