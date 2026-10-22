"""Editable pistol magazine reload using the approved rustic character, Blender 4.5."""
import bpy, math, sys
from pathlib import Path
from mathutils import Vector, Euler
BASE=Path(__file__).resolve().parent
# Reuse construction helpers only. Never export or replace the production assets.
exec((BASE/'export_game_character.py').read_text(encoding='utf-8').split('variants=')[0])
OUT=BASE/'PistolReload'; (OUT/'frames').mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(BASE/'VisualV1/personagem-rustico.blend'))
scene=bpy.context.scene; scene.frame_set(9)
for o in scene.objects:
    if o.animation_data: o.animation_data_clear()
scene.timeline_markers.clear(); scene.frame_start=1; scene.frame_end=37; scene.render.fps=24
steel=bpy.data.materials['Aco escurecido']; leather=bpy.data.materials['Couro marrom gasto']
jacket=bpy.data.materials['Jaqueta oliva - tecido gasto']; skin=bpy.data.materials['Pele']
def empty(name,loc):
    o=bpy.data.objects.new(name,None); scene.collection.objects.link(o); o.location=loc
    o.empty_display_type='ARROWS'; o.empty_display_size=.10; return o
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
def transform_offset(o,offset): return o.location+o.rotation_euler.to_matrix()@Vector(offset)
def segment_pose(o,a,b,r):
    o.location=(a+b)/2; o.scale=(r,r,(b-a).length/2+.03)
    o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler(); key(o)

pistol=empty('CTRL - Pistola recarga',(.70,0,1.43))
bpy.context.view_layer.update()
for o in list(scene.objects):
    if o.name.startswith(gun_names): parent_keep(o,pistol)
    if o.name.startswith(arm_names): o.hide_render=True; o.hide_set(True)
old_mag=box('Carregador retirado',(.72,0,1.27),(.066,.050,.14),steel)
new_mag=box('Carregador novo',(.04,.47,1.15),(.066,.050,.14),steel)
base_plate=box('Base carregador novo',(.04,.47,1.08),(.079,.060,.015),steel)
parent_keep(base_plate,new_mag)
arms=[]
for side in [-1,1]:
    shoulder=Vector((.055,side*.30,1.56))
    upper=sleeve('Manga recarga '+str(side),shoulder,(.35,side*.37,1.32),.115,jacket)
    lower=sleeve('Antebraco recarga '+str(side),(.35,side*.37,1.32),(.68,side*.055,1.38),.09,jacket)
    joint=sphere('Cotovelo recarga '+str(side),(.35,side*.37,1.32),(.11,.11,.10),jacket)
    hand=sphere('Mao recarga '+str(side),(.68,side*.055,1.38),(.10,.075,.075) if side==-1 else (.085,.060,.060),skin)
    cuff=sphere('Punho recarga '+str(side),(.66,side*.055,1.38),(.095,.095,.035),leather)
    sphere('Ombro recarga '+str(side),shoulder,(.15,.15,.15),jacket)
    arms.append((side,shoulder,upper,lower,joint,hand,cuff))

gun_locs=[(1,(.70,0,1.43)),(3,(.70,0,1.43)),(10,(.63,-.18,1.54)),(29,(.63,-.18,1.54)),(37,(.70,0,1.43))]
gun_angles=[(1,(0,0,0)),(3,(0,0,0)),(10,(65,0,22)),(29,(65,0,22)),(37,(0,0,0))]
grip=Vector((.02,0,-.16)); extracted=Vector((.02,0,-.40)); belt=Vector((.03,.47,1.12))
for frame in range(1,38):
    scene.frame_set(frame)
    pistol.location=interpolate(gun_locs,frame)
    pistol.rotation_euler=Euler(tuple(math.radians(v) for v in interpolate(gun_angles,frame)),'XYZ'); key(pistol)
    seated=transform_offset(pistol,grip); out=transform_offset(pistol,extracted)
    if frame<=10: old_mag.location=seated
    elif frame<=14: old_mag.location=interpolate([(10,seated),(14,out)],frame)
    else: old_mag.location=interpolate([(14,out),(18,belt)],frame)
    old_mag.rotation_euler=pistol.rotation_euler; key(old_mag)
    old_mag.hide_render=frame>=19; old_mag.keyframe_insert(data_path='hide_render')
    if frame<=20: new_mag.location=belt
    elif frame<=24: new_mag.location=interpolate([(20,belt),(24,out)],frame)
    elif frame<=28: new_mag.location=interpolate([(24,out),(28,seated)],frame)
    else: new_mag.location=seated
    new_mag.rotation_euler=pistol.rotation_euler; key(new_mag)
    for o in [new_mag,base_plate]:
        o.hide_render=frame<20; o.keyframe_insert(data_path='hide_render')
    right=transform_offset(pistol,(-.02,-.055,-.05))
    support=transform_offset(pistol,(-.02,.055,-.05))
    grasp=transform_offset(pistol,(.02,.035,-.235))
    if frame<=10:
        left=interpolate([(1,(.68,.055,1.38)),(3,(.68,.055,1.38)),(10,grasp)],frame)
    elif frame<=18: left=old_mag.location+pistol.rotation_euler.to_matrix()@Vector((0,.035,-.075))
    elif frame<=28: left=new_mag.location+pistol.rotation_euler.to_matrix()@Vector((0,.035,-.075))
    else:
        left=interpolate([(28,grasp),(32,support),(37,support)],frame)
    for side,shoulder,upper,lower,joint,hand,cuff in arms:
        wrist=right if side==-1 else left
        elbow=shoulder.lerp(wrist,.52)+Vector((-.015,side*.18,-.13))
        segment_pose(upper,shoulder,elbow,.115); segment_pose(lower,elbow,wrist,.09)
        joint.location=elbow; key(joint)
        hand.location=wrist; hand.rotation_euler=lower.rotation_euler; key(hand)
        cuff.location=elbow.lerp(wrist,.93); cuff.rotation_euler=lower.rotation_euler; key(cuff)

for o in scene.objects:
    if o.animation_data and o.animation_data.action:
        for curve in o.animation_data.action.fcurves:
            for point in curve.keyframe_points: point.interpolation='CONSTANT' if curve.data_path=='hide_render' else 'LINEAR'
for name,frame in [('Mira',1),('Inclinar pistola',10),('Retirar carregador',14),('Guardar carregador',18),('Buscar novo',20),('Alinhar carregador',24),('Encaixar',28),('Apoiar mao',32),('Mira pronta',37)]:
    scene.timeline_markers.new(name,frame=frame)
scene.camera=bpy.data.objects['Camera jogo - topo']; scene.render.resolution_x=512; scene.render.resolution_y=512
scene.render.resolution_percentage=100; scene.render.film_transparent=True; scene.cycles.samples=24; scene.cycles.use_denoising=True
scene.frame_set(1); scene.render.filepath=str(OUT/'frames'/'001.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'protagonista-recarga-pistola.blend'))
frames=range(1,38)
if '--poses-only' in sys.argv: frames=[1,10,14,20,24,28,37]
if '--no-render' not in sys.argv:
    for frame in frames:
        scene.frame_set(frame); scene.render.filepath=str(OUT/'frames'/f'{frame:03}.png'); bpy.ops.render.render(write_still=True)
print('PISTOL RELOAD SAVED AND RENDERED',flush=True)
