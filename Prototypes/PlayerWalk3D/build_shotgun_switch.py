"""Build an editable pistol-to-shotgun transition from the approved Blender character."""
import bpy, math, sys
from pathlib import Path
from mathutils import Vector, Matrix, Euler

BASE=Path(__file__).resolve().parent
OUT=BASE/'ShotgunSwitch'
(OUT/'frames').mkdir(parents=True,exist_ok=True)
# Reuse the production model construction helpers, without exporting other poses.
exec((BASE/'export_game_character.py').read_text(encoding='utf-8').split('variants=')[0])
OUT=BASE/'ShotgunSwitch'
bpy.ops.wm.open_mainfile(filepath=str(BASE/'VisualV1/personagem-rustico.blend'))
scene=bpy.context.scene
scene.frame_set(9)
for obj in scene.objects:
    if obj.animation_data: obj.animation_data_clear()
scene.frame_start=1; scene.frame_end=32; scene.render.fps=24
steel=bpy.data.materials['Aco escurecido']
leather=bpy.data.materials['Couro marrom gasto']
jacket=bpy.data.materials['Jaqueta oliva - tecido gasto']
skin=bpy.data.materials['Pele']
wood=bpy.data.materials['Mochila lona caqui'].copy(); wood.name='Madeira shotgun - troca'
ramp=next(n for n in wood.node_tree.nodes if n.type=='VALTORGB')
ramp.color_ramp.elements[0].color=(.055,.026,.010,1)
ramp.color_ramp.elements[1].color=(.22,.105,.035,1)

def empty(name,loc):
    obj=bpy.data.objects.new(name,None); scene.collection.objects.link(obj); obj.location=loc
    obj.empty_display_type='ARROWS'; obj.empty_display_size=.15
    return obj
def parent_keep(obj,parent):
    world=obj.matrix_world.copy(); obj.parent=parent; obj.matrix_world=world
def key(obj):
    obj.keyframe_insert(data_path='location'); obj.keyframe_insert(data_path='rotation_euler'); obj.keyframe_insert(data_path='scale')

pistol=empty('CTRL - guardar pistola',(.70,0,1.43))
bpy.context.view_layer.update()
for obj in list(scene.objects):
    if obj.name.startswith(gun_names): parent_keep(obj,pistol)
    if obj.name.startswith(arm_names): obj.hide_render=True; obj.hide_set(True)

shotgun=empty('CTRL - sacar shotgun',(.70,0,1.43))
bpy.context.view_layer.update()
parts=[box('Shotgun coronha',(.48,0,1.40),(.43,.072,.07),wood),
       box('Shotgun receptor',(.71,0,1.43),(.22,.076,.065),steel),
       box('Shotgun guarda mao',(.81,0,1.40),(.20,.082,.057),wood),
       box('Shotgun trava',(.68,0,1.472),(.045,.018,.012),steel)]
for y in [-.027,.027]: parts.append(barrel('Shotgun cano duplo',(.76,y,1.445),(1.10,y,1.445),.025,steel))
bpy.context.view_layer.update()
for obj in parts: parent_keep(obj,shotgun)

arms=[]
for side in [-1,1]:
    shoulder=Vector((.055,side*.30,1.56))
    upper=sleeve('Manga troca '+str(side),shoulder,(.35,side*.37,1.32),.115,jacket)
    lower=sleeve('Antebraco troca '+str(side),(.35,side*.37,1.32),(.68,side*.055,1.38),.09,jacket)
    joint=sphere('Cotovelo troca '+str(side),(.35,side*.37,1.32),(.11,.11,.10),jacket)
    hand=sphere('Mao troca '+str(side),(.68,side*.055,1.38),(.10,.075,.075),skin)
    cuff=sphere('Punho troca '+str(side),(.66,side*.055,1.38),(.095,.095,.035),leather)
    sphere('Ombro troca '+str(side),shoulder,(.15,.15,.15),jacket)
    arms.append((side,shoulder,upper,lower,joint,hand,cuff))

def interpolate(keys,frame):
    for i in range(len(keys)-1):
        fa,a=keys[i]; fb,b=keys[i+1]
        if frame<=fb:
            t=max(0,min(1,(frame-fa)/(fb-fa))); t=t*t*(3-2*t)
            return Vector(a).lerp(Vector(b),t)
    return Vector(keys[-1][1])
def pose(obj,locations,angles,frame):
    obj.location=interpolate(locations,frame)
    obj.rotation_euler=Euler(tuple(math.radians(v) for v in interpolate(angles,frame)),'XYZ')
    key(obj)
def wrist_on_weapon(obj,offset):
    rotation=obj.rotation_euler.to_matrix()
    return obj.location+rotation@Vector(offset)
def segment_pose(obj,a,b,radius):
    obj.location=(a+b)/2
    obj.scale=(radius,radius,(b-a).length/2+.03)
    obj.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()
    key(obj)

pistol_locs=[(1,(.70,0,1.43)),(3,(.70,0,1.43)),(10,(.13,-.38,1.20)),(32,(.13,-.38,1.20))]
pistol_angles=[(1,(0,0,0)),(3,(0,0,0)),(10,(0,65,-65)),(32,(0,65,-65))]
gun_locs=[(1,(-.12,.30,1.45)),(10,(-.12,.30,1.45)),(15,(-.03,.44,1.53)),(22,(.40,.16,1.49)),(28,(.70,0,1.43)),(32,(.70,0,1.43))]
gun_angles=[(1,(0,20,100)),(10,(0,20,100)),(15,(0,0,65)),(22,(0,0,20)),(28,(0,0,0)),(32,(0,0,0))]
for frame in range(1,33):
    scene.frame_set(frame)
    pose(pistol,pistol_locs,pistol_angles,frame)
    pose(shotgun,gun_locs,gun_angles,frame)
    for obj in pistol.children:
        obj.hide_render=frame>=11; obj.keyframe_insert(data_path='hide_render')
    right_grip=wrist_on_weapon(pistol,(-.02,-.055,-.05))
    shotgun_grip=wrist_on_weapon(shotgun,(-.02,-.055,-.05))
    shotgun_support=wrist_on_weapon(shotgun,(.12,.055,-.04))
    for side,shoulder,upper,lower,joint,hand,cuff in arms:
        if side==-1:
            if frame<=10: wrist=right_grip
            else:
                t=max(0,min(1,(frame-11)/10)); t=t*t*(3-2*t)
                wrist=Vector((.12,-.38,1.18)).lerp(shotgun_grip,t)
        else:
            t=max(0,min(1,(frame-3)/7)); t=t*t*(3-2*t)
            wrist=Vector((.68,.055,1.38)).lerp(shotgun_support,t)
        elbow=shoulder.lerp(wrist,.52)+Vector((-.015,side*.18,-.13))
        segment_pose(upper,shoulder,elbow,.115)
        segment_pose(lower,elbow,wrist,.09)
        joint.location=elbow; key(joint)
        hand.location=wrist; hand.rotation_euler=lower.rotation_euler; key(hand)
        cuff.location=elbow.lerp(wrist,.93); cuff.rotation_euler=lower.rotation_euler; key(cuff)

for name,frame in [('Pistola em mira',1),('Guardar pistola',10),('Puxar shotgun',15),('Apoiar duas maos',22),('Shotgun em mira',28)]:
    scene.timeline_markers.new(name,frame=frame)
scene.camera=bpy.data.objects['Camera jogo - topo']
scene.render.resolution_x=320; scene.render.resolution_y=320; scene.render.resolution_percentage=100
scene.render.film_transparent=True; scene.cycles.samples=16
scene.frame_set(1)
scene.render.filepath=str(OUT/'frames'/"001.png")
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'troca-pistola-shotgun.blend'))
if '--no-render' not in sys.argv:
    for frame in range(1,33):
        scene.frame_set(frame); scene.render.filepath=str(OUT/'frames'/f'{frame:03}.png')
        bpy.ops.render.render(write_still=True)
print('SHOTGUN SWITCH SAVED AND RENDERED',flush=True)
