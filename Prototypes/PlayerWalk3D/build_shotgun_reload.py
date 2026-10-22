"""Editable break-action, two-shell reload of the approved shotgun. Blender 4.5."""
import bpy, math, sys
from pathlib import Path
from mathutils import Vector, Matrix, Euler
BASE=Path(__file__).resolve().parent
exec((BASE/'export_game_character.py').read_text(encoding='utf-8').split('variants=')[0])
single='--single-shell' in sys.argv
OUT=BASE/('ShotgunReloadSingle' if single else 'ShotgunReload'); (OUT/'frames').mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(BASE/'ShotgunSwitch/troca-pistola-shotgun.blend'))
scene=bpy.context.scene; scene.frame_set(32); bpy.context.view_layer.update()
for o in scene.objects:
    if o.animation_data: o.animation_data_clear()
scene.timeline_markers.clear(); scene.frame_start=1; scene.frame_end=64; scene.render.fps=24
gun=bpy.data.objects['CTRL - sacar shotgun']; gun.name='CTRL - Shotgun recarga'
steel=bpy.data.materials['Aco escurecido']; skin=bpy.data.materials['Pele']
for o in scene.objects:
    if o.name.startswith(gun_names): o.hide_render=True
def empty(name,loc):
    o=bpy.data.objects.new(name,None); scene.collection.objects.link(o); o.location=loc; o.empty_display_size=.12; o.empty_display_type='ARROWS'; return o
def parent_keep(o,parent):
    bpy.context.view_layer.update(); world=o.matrix_world.copy(); o.parent=parent; o.matrix_world=world
def key(o):
    for path in ['location','rotation_euler','scale']: o.keyframe_insert(data_path=path)
def interpolate(keys,frame):
    for i in range(len(keys)-1):
        fa,a=keys[i]; fb,b=keys[i+1]
        if frame<=fb:
            t=max(0,min(1,(frame-fa)/(fb-fa))); t=t*t*(3-2*t)
            return Vector(a).lerp(Vector(b),t)
    return Vector(keys[-1][1])
def gun_point(offset): return gun.location+gun.rotation_euler.to_matrix()@Vector(offset)
def segment_pose(o,a,b,r):
    o.location=(a+b)/2; o.scale=(r,r,(b-a).length/2+.03); o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler(); key(o)

hinge=empty('CTRL - Bascula dos canos',(.74,0,1.43)); parent_keep(hinge,gun)
barrel_parts=[o for o in list(scene.objects) if o.name.startswith(('Shotgun cano duplo','Shotgun guarda mao'))]
for o in barrel_parts: parent_keep(o,hinge)
# Separate the receiver from the barrel breech for a physically coherent hinge.
receiver=bpy.data.objects['Shotgun receptor']; receiver.scale.x=.55; receiver.location.x-=.035
dark=steel.copy(); dark.name='Interior das camaras'
p=dark.node_tree.nodes.get('Principled BSDF')
for link in list(dark.node_tree.links):
    if link.to_node==p and link.to_socket==p.inputs['Base Color']: dark.node_tree.links.remove(link)
p.inputs['Base Color'].default_value=(.004,.004,.003,1)
for y in [-.027,.027]:
    chamber=sphere('Camara aberta',(.759,y,1.445),(.007,.020,.020),dark); parent_keep(chamber,hinge)
red=steel.copy(); red.name='Cartucho vermelho opaco'
p=red.node_tree.nodes.get('Principled BSDF'); p.inputs['Metallic'].default_value=0; p.inputs['Roughness'].default_value=.75
for link in list(red.node_tree.links):
    if link.to_node==p and link.to_socket==p.inputs['Base Color']: red.node_tree.links.remove(link)
p.inputs['Base Color'].default_value=(.23,.037,.023,1)
brass=steel.copy(); brass.name='Base latao cartucho'
p=brass.node_tree.nodes.get('Principled BSDF'); p.inputs['Metallic'].default_value=.65
for link in list(brass.node_tree.links):
    if link.to_node==p and link.to_socket==p.inputs['Base Color']: brass.node_tree.links.remove(link)
p.inputs['Base Color'].default_value=(.39,.27,.11,1)
def shell(name):
    c=empty(name,(0,0,0))
    case=barrel(name+' corpo',(-.045,0,0),(.045,0,0),.023,red)
    cap=barrel(name+' base',(-.054,0,0),(-.038,0,0),.025,brass)
    for o in [case,cap]: parent_keep(o,c)
    return c
old=[shell('Estojo usado '+str(i+1)) for i in range(2)]
fresh=[shell('Cartucho novo '+str(i+1)) for i in range(2)]
arms=[]
for side in [-1,1]:
    upper=bpy.data.objects['Manga troca '+str(side)]; lower=bpy.data.objects['Antebraco troca '+str(side)]
    joint=bpy.data.objects['Cotovelo troca '+str(side)]; hand=bpy.data.objects['Mao troca '+str(side)]; cuff=bpy.data.objects['Punho troca '+str(side)]
    if side==1: hand.scale=(.080,.060,.060)
    arms.append((side,Vector((.055,side*.30,1.56)),upper,lower,joint,hand,cuff))
locs=[(1,(.70,0,1.43)),(4,(.70,0,1.43)),(12,(.67,-.16,1.56)),(52,(.67,-.16,1.56)),(64,(.70,0,1.43))]
angles=[(1,(0,0,0)),(4,(0,0,0)),(12,(62,0,-12)),(52,(62,0,-12)),(64,(0,0,0))]
opening=[(1,(0,0,0)),(7,(0,0,0)),(14,(0,48,0)),(51,(0,48,0)),(57,(0,0,0)),(64,(0,0,0))]
belt=Vector((.03,.47,1.15))
for frame in range(1,65):
    scene.frame_set(frame)
    gun.location=interpolate(locs,frame); gun.rotation_euler=Euler(tuple(math.radians(v) for v in interpolate(angles,frame)),'XYZ'); key(gun)
    hinge.rotation_euler=Euler(tuple(math.radians(v) for v in interpolate(opening,frame)),'XYZ'); key(hinge)
    rotation=gun.rotation_euler.to_matrix()@hinge.rotation_euler.to_matrix()
    origin=gun_point(hinge.location)
    def chamber_point(offset): return origin+rotation@Vector(offset)
    def grasp(o): return o.location+rotation@Vector((-.085,.034,0))
    for i,o in enumerate(old):
        seated=chamber_point((.070,(-.027 if i==0 else .027),.015))
        extracted=chamber_point((-.110,(-.027 if i==0 else .027),.015))
        if frame<=14: o.location=seated
        elif frame<=18: o.location=interpolate([(14,seated),(18,extracted)],frame)
        else: o.location=interpolate([(18,extracted),(25,belt+Vector((0,i*.06,0)))],frame)
        o.rotation_euler=rotation.to_euler(); key(o)
        for part in o.children: part.hide_render=frame<14 or frame>=25 or (single and i==1); part.keyframe_insert(data_path='hide_render')
    for i,o in enumerate(fresh):
        start,align,insert=(28,34,38) if i==0 else (41,47,51)
        seated=chamber_point((.070,(-.027 if i==0 else .027),.015)); extracted=chamber_point((-.110,(-.027 if i==0 else .027),.015))
        if frame<=start: o.location=belt
        elif frame<=align: o.location=interpolate([(start,belt),(align,extracted)],frame)
        elif frame<=insert: o.location=interpolate([(align,extracted),(insert,seated)],frame)
        else: o.location=seated
        o.rotation_euler=rotation.to_euler(); key(o)
        for part in o.children: part.hide_render=frame<start or frame>=insert or (single and i==1); part.keyframe_insert(data_path='hide_render')
    right=gun_point((-.02,-.055,-.05)); support=chamber_point((.080,.055,-.025))
    extractor_hand=chamber_point((-.015,.045,.015))
    if single and frame>38: left=interpolate([(38,grasp(fresh[0])),(42,support),(64,support)],frame)
    elif frame<=14: left=interpolate([(1,(.82,.055,1.39)),(4,(.82,.055,1.39)),(14,extractor_hand)],frame)
    elif frame<=24: left=grasp(old[0])
    elif frame<=38: left=grasp(fresh[0])
    elif frame<=41: left=interpolate([(38,grasp(fresh[0])),(41,grasp(fresh[1]))],frame)
    elif frame<=51: left=grasp(fresh[1])
    else: left=interpolate([(51,grasp(fresh[1])),(55,support),(64,support)],frame)
    for side,shoulder,upper,lower,joint,hand,cuff in arms:
        wrist=right if side==-1 else left; elbow=shoulder.lerp(wrist,.52)+Vector((-.015,side*.18,-.13))
        segment_pose(upper,shoulder,elbow,.115); segment_pose(lower,elbow,wrist,.09)
        joint.location=elbow; key(joint); hand.location=wrist; hand.rotation_euler=lower.rotation_euler; key(hand)
        cuff.location=elbow.lerp(wrist,.93); cuff.rotation_euler=lower.rotation_euler; key(cuff)
for o in scene.objects:
    if o.animation_data and o.animation_data.action:
        for curve in o.animation_data.action.fcurves:
            for pt in curve.keyframe_points: pt.interpolation='CONSTANT' if curve.data_path=='hide_render' else 'LINEAR'
for name,f in [('Mira',1),('Abrir bascula',14),('Retirar estojos',18),('Buscar cartuchos',28),('Primeiro cartucho',38),('Segundo cartucho',51),('Fechar canos',57),('Mira pronta',64)]: scene.timeline_markers.new(name,frame=f)
scene.camera=bpy.data.objects['Camera jogo - topo']; scene.render.resolution_x=512; scene.render.resolution_y=512; scene.render.resolution_percentage=100
scene.render.film_transparent=True; scene.cycles.samples=24; scene.cycles.use_denoising=True
scene.frame_set(1); scene.render.filepath=str(OUT/'frames'/'001.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'protagonista-recarga-shotgun.blend'))
frames=range(1,65) if '--poses-only' not in sys.argv else [1,14,18,34,38,47,51,57,64]
for f in ([] if '--no-render' in sys.argv else frames):
    scene.frame_set(f); scene.render.filepath=str(OUT/'frames'/f'{f:03}.png'); bpy.ops.render.render(write_still=True)
print('SHOTGUN RELOAD SAVED AND RENDERED',flush=True)
