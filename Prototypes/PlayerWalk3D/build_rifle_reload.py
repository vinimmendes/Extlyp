"""Editable five-round stripper-clip reload of the current bolt-action rifle."""
import bpy, math, sys
from pathlib import Path
from mathutils import Vector, Euler
BASE=Path(__file__).resolve().parent
exec((BASE/'export_game_character.py').read_text(encoding='utf-8').split('variants=')[0])
OUT=BASE/'RifleReload'; (OUT/'frames').mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(BASE/'RifleSwitch/troca-pistola-rifle.blend'))
scene=bpy.context.scene; scene.frame_set(32); bpy.context.view_layer.update()
for o in scene.objects:
    if o.animation_data: o.animation_data_clear()
scene.timeline_markers.clear(); scene.frame_start=1; scene.frame_end=78; scene.render.fps=24
gun=bpy.data.objects['CTRL - sacar rifle']; gun.name='CTRL - Rifle recarga'
steel=bpy.data.materials['Aco escurecido']
for o in scene.objects:
    if o.name.startswith(gun_names): o.hide_render=True
def empty(name,loc):
    o=bpy.data.objects.new(name,None); scene.collection.objects.link(o); o.location=loc; o.empty_display_size=.10; o.empty_display_type='ARROWS'; return o
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
def point(offset): return gun.location+gun.rotation_euler.to_matrix()@Vector(offset)
def segment_pose(o,a,b,r):
    o.location=(a+b)/2; o.scale=(r,r,(b-a).length/2+.03); o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler(); key(o)
bolt=empty('CTRL - Ferrolho',(.69,-.025,1.455)); parent_keep(bolt,gun)
for name in ['Rifle alavanca ferrolho','Rifle manopla ferrolho']: parent_keep(bpy.data.objects[name],bolt)
bolt_body=barrel('Corpo ferrolho',(.65,0,1.464),(.745,0,1.464),.021,steel); parent_keep(bolt_body,bolt)
bolt_base=bolt.location.copy()
brass=steel.copy(); brass.name='Latao municao rifle'
p=brass.node_tree.nodes.get('Principled BSDF')
for link in list(brass.node_tree.links):
    if link.to_node==p and link.to_socket==p.inputs['Base Color']: brass.node_tree.links.remove(link)
p.inputs['Base Color'].default_value=(.42,.28,.105,1); p.inputs['Metallic'].default_value=.6
tip_mat=brass.copy(); tip_mat.name='Ponta cobre rifle'; tip_mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(.36,.15,.07,1)
clip=empty('CTRL - Pente de cinco municoes',(0,0,0))
carrier=box('Pente metalico',(-.060,0,.069),(.016,.023,.153),steel); parent_keep(carrier,clip)
rounds=[]
for i in range(5):
    z=.016+i*.026
    cartridge=barrel('Municao '+str(i+1),(-.050,0,z),(.043,0,z),.011,brass)
    bullet=barrel('Ponta municao '+str(i+1),(.043,0,z),(.069,0,z),.008,tip_mat)
    parent_keep(cartridge,clip); parent_keep(bullet,clip); rounds.append((cartridge,bullet,z))
arms=[]
for side in [-1,1]:
    hand=bpy.data.objects['Mao troca '+str(side)]
    if side==1: hand.scale=(.085,.060,.060)
    arms.append((side,Vector((.055,side*.30,1.56)),bpy.data.objects['Manga troca '+str(side)],bpy.data.objects['Antebraco troca '+str(side)],bpy.data.objects['Cotovelo troca '+str(side)],hand,bpy.data.objects['Punho troca '+str(side)]))
locs=[(1,(.70,0,1.43)),(4,(.70,0,1.43)),(12,(.79,-.17,1.56)),(66,(.79,-.17,1.56)),(78,(.70,0,1.43))]
angles=[(1,(0,0,0)),(4,(0,0,0)),(12,(-35,0,12)),(66,(-35,0,12)),(78,(0,0,0))]
bolt_lift=[(1,(0,0,0)),(8,(0,0,0)),(15,(-70,0,0)),(66,(-70,0,0)),(72,(0,0,0)),(78,(0,0,0))]
bolt_pull=[(1,(0,0,0)),(15,(0,0,0)),(22,(-.105,0,0)),(60,(-.105,0,0)),(66,(0,0,0)),(78,(0,0,0))]
belt=Vector((.03,.47,1.15))
for frame in range(1,79):
    scene.frame_set(frame); rotation=None
    gun.location=interpolate(locs,frame); gun.rotation_euler=Euler(tuple(math.radians(v) for v in interpolate(angles,frame)),'XYZ'); key(gun)
    bolt.location=bolt_base+interpolate(bolt_pull,frame); bolt.rotation_euler=Euler(tuple(math.radians(v) for v in interpolate(bolt_lift,frame)),'XYZ'); key(bolt)
    rotation=gun.rotation_euler.to_matrix()
    alignment=point((.0,0,.205)); seated=point((.0,0,.074)); removed=point((-.01,.08,.28))
    if frame<=29: clip.location=belt
    elif frame<=38: clip.location=interpolate([(29,belt),(38,alignment)],frame)
    elif frame<=44: clip.location=interpolate([(38,alignment),(44,seated)],frame)
    elif frame<=51: clip.location=seated
    elif frame<=56: clip.location=interpolate([(51,seated),(56,removed)],frame)
    else: clip.location=interpolate([(56,removed),(60,belt)],frame)
    clip.rotation_euler=gun.rotation_euler; key(clip)
    carrier.hide_render=frame<29 or frame>=61; carrier.keyframe_insert(data_path='hide_render')
    feed=max(0,min(1,(frame-44)/7))
    for case,tip,z in rounds:
        case.location.z=z-feed*.17; tip.location.z=z-feed*.17
        for o in [case,tip]:
            key(o); o.hide_render=frame<29 or frame>51 or (frame>=45 and .074+z-feed*.17<.040); o.keyframe_insert(data_path='hide_render')
    bolt_hand=point(bolt.location+bolt.rotation_euler.to_matrix()@Vector((0,-.090,0)))
    right_grip=point((-.02,-.055,-.05)); support=point((.12,.055,-.04))
    if frame<=8: right=interpolate([(1,(.68,-.055,1.38)),(4,(.68,-.055,1.38)),(8,bolt_hand)],frame)
    elif frame<=22: right=bolt_hand
    elif frame<=26: right=interpolate([(22,bolt_hand),(26,right_grip)],frame)
    elif frame<=58: right=right_grip
    elif frame<=60: right=interpolate([(58,right_grip),(60,bolt_hand)],frame)
    elif frame<=72: right=bolt_hand
    else: right=interpolate([(72,bolt_hand),(78,right_grip)],frame)
    clip_grip=clip.location+rotation@Vector((-.090,.035,.10))
    push_hand=clip.location+rotation@Vector((-.005,.027,.12-feed*.10))
    if frame<=23: left=support
    elif frame<=29: left=interpolate([(23,support),(29,clip_grip)],frame)
    elif frame<=44: left=clip_grip
    elif frame<=51: left=interpolate([(44,clip_grip),(47,push_hand),(51,push_hand)],frame)
    elif frame<=59: left=clip_grip
    else: left=interpolate([(59,clip_grip),(65,support),(78,support)],frame)
    for side,shoulder,upper,lower,joint,hand,cuff in arms:
        wrist=right if side==-1 else left; elbow=shoulder.lerp(wrist,.52)+Vector((-.015,side*.18,-.13))
        segment_pose(upper,shoulder,elbow,.115); segment_pose(lower,elbow,wrist,.09)
        joint.location=elbow; key(joint); hand.location=wrist; hand.rotation_euler=lower.rotation_euler; key(hand)
        cuff.location=elbow.lerp(wrist,.93); cuff.rotation_euler=lower.rotation_euler; key(cuff)
for o in scene.objects:
    if o.animation_data and o.animation_data.action:
        for curve in o.animation_data.action.fcurves:
            for pt in curve.keyframe_points: pt.interpolation='CONSTANT' if curve.data_path=='hide_render' else 'LINEAR'
for name,f in [('Mira',1),('Levantar ferrolho',15),('Puxar ferrolho',22),('Buscar pente',29),('Alinhar',38),('Apoiar pente',44),('Carregar cinco',51),('Retirar pente',56),('Avancar ferrolho',66),('Travar ferrolho',72),('Mira pronta',78)]: scene.timeline_markers.new(name,frame=f)
scene.camera=bpy.data.objects['Camera jogo - topo']; scene.render.resolution_x=512; scene.render.resolution_y=512; scene.render.resolution_percentage=100
scene.render.film_transparent=True; scene.cycles.samples=24; scene.cycles.use_denoising=True
scene.frame_set(1); scene.render.filepath=str(OUT/'frames'/'001.png'); bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'protagonista-recarga-rifle.blend'))
frames=range(1,79) if '--poses-only' not in sys.argv else [1,15,22,38,44,51,56,72,78]
for f in frames:
    scene.frame_set(f); scene.render.filepath=str(OUT/'frames'/f'{f:03}.png'); bpy.ops.render.render(write_still=True)
print('RIFLE RELOAD SAVED AND RENDERED',flush=True)
