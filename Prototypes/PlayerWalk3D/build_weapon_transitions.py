"""Four direct transitions among approved weapons. Editable Blender 4.5 clips."""
import bpy, math, sys
from pathlib import Path
from mathutils import Vector,Euler
BASE=Path(__file__).resolve().parent
exec((BASE/'export_game_character.py').read_text(encoding='utf-8').split('variants=')[0])
OUT=BASE/'WeaponTransitions'; OUT.mkdir(exist_ok=True)
PAIRS=[('Shotgun','Pistol'),('Rifle','Pistol'),('Shotgun','Rifle'),('Rifle','Shotgun')]
def empty(name,loc):
    o=bpy.data.objects.new(name,None); bpy.context.scene.collection.objects.link(o); o.location=loc; o.empty_display_type='ARROWS'; o.empty_display_size=.12; return o
def parent_keep(o,parent):
    bpy.context.view_layer.update(); world=o.matrix_world.copy(); o.parent=parent; o.matrix_world=world
def interpolate(keys,frame):
    for i in range(len(keys)-1):
        fa,a=keys[i]; fb,b=keys[i+1]
        if frame<=fb:
            t=max(0,min(1,(frame-fa)/(fb-fa))); t=t*t*(3-2*t); return Vector(a).lerp(Vector(b),t)
    return Vector(keys[-1][1])
def key(o):
    for path in ['location','rotation_euler','scale']: o.keyframe_insert(data_path=path)
def point(o,offset): return o.location+o.rotation_euler.to_matrix()@Vector(offset)
def segment_pose(o,a,b,r):
    o.location=(a+b)/2; o.scale=(r,r,(b-a).length/2+.03); o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler(); key(o)
for source_kind,target_kind in PAIRS:
    name=source_kind+'To'+target_kind; folder=OUT/name; (folder/'frames').mkdir(parents=True,exist_ok=True)
    bpy.ops.wm.open_mainfile(filepath=str(BASE/'ShotgunSwitch/troca-pistola-shotgun.blend'))
    scene=bpy.context.scene; scene.frame_set(32); bpy.context.view_layer.update()
    for o in scene.objects:
        if o.animation_data: o.animation_data_clear()
    # Append only the rifle, preserving the established character and shotgun.
    with bpy.data.libraries.load(str(BASE/'RifleSwitch/troca-pistola-rifle.blend'),link=False) as (src,dst):
        dst.objects=[n for n in src.objects if n.startswith(('CTRL - sacar rifle','Rifle '))]
    for o in dst.objects:
        if o: scene.collection.objects.link(o)
    scene.frame_set(32); bpy.context.view_layer.update()
    for o in dst.objects:
        if o and o.animation_data: o.animation_data_clear()
    scene.timeline_markers.clear(); scene.frame_start=1; scene.frame_end=36; scene.render.fps=24
    guns={'Pistol':bpy.data.objects['CTRL - guardar pistola'],'Shotgun':bpy.data.objects['CTRL - sacar shotgun'],'Rifle':bpy.data.objects['CTRL - sacar rifle']}
    for kind,o in guns.items():
        o.name='CTRL - '+kind
        for part in o.children: part.hide_render=kind not in [source_kind,target_kind]
    source=guns[source_kind]; target=guns[target_kind]
    source_locs=[(1,(.70,0,1.43)),(3,(.70,0,1.43)),(12,(-.16,.35,1.43)),(36,(-.16,.35,1.43))]
    source_angles=[(1,(0,0,0)),(3,(0,0,0)),(12,(0,20,100)),(36,(0,20,100))]
    if target_kind=='Pistol':
        target_locs=[(1,(.13,-.38,1.20)),(12,(.13,-.38,1.20)),(18,(.39,-.30,1.42)),(28,(.70,0,1.43)),(36,(.70,0,1.43))]
        target_angles=[(1,(0,65,-65)),(12,(0,65,-65)),(18,(0,20,-28)),(28,(0,0,0)),(36,(0,0,0))]
    else:
        target_locs=[(1,(-.16,-.35,1.43)),(12,(-.16,-.35,1.43)),(18,(.04,-.42,1.52)),(26,(.55,-.08,1.48)),(32,(.70,0,1.43)),(36,(.70,0,1.43))]
        target_angles=[(1,(0,20,-100)),(12,(0,20,-100)),(18,(0,0,-65)),(26,(0,0,-15)),(32,(0,0,0)),(36,(0,0,0))]
    arms=[]
    for side in [-1,1]:
        arms.append((side,Vector((.055,side*.30,1.56)),bpy.data.objects['Manga troca '+str(side)],bpy.data.objects['Antebraco troca '+str(side)],bpy.data.objects['Cotovelo troca '+str(side)],bpy.data.objects['Mao troca '+str(side)],bpy.data.objects['Punho troca '+str(side)]))
    for f in range(1,37):
        scene.frame_set(f)
        for o,locs,angles in [(source,source_locs,source_angles),(target,target_locs,target_angles)]:
            o.location=interpolate(locs,f); o.rotation_euler=Euler(tuple(math.radians(v) for v in interpolate(angles,f)),'XYZ'); key(o)
        for part in source.children: part.hide_render=f>=14; part.keyframe_insert(data_path='hide_render')
        # Target remains in its stowed pose, then is drawn as a continuous object.
        for part in target.children: part.hide_render=False; part.keyframe_insert(data_path='hide_render')
        src_right=point(source,(-.02,-.055,-.05)); dst_right=point(target,(-.02,-.055,-.05))
        src_left=point(source,(.12,.055,-.04)); dst_left=point(target,(-.02,.055,-.05) if target_kind=='Pistol' else (.12,.055,-.04))
        for side,shoulder,upper,lower,joint,hand,cuff in arms:
            if side==-1:
                if f<=12: wrist=src_right
                elif f<=18: wrist=interpolate([(12,src_right),(18,dst_right)],f)
                else: wrist=dst_right
            else:
                if f<=12: wrist=src_left
                elif target_kind=='Pistol':
                    rest=Vector((.25,.40,1.34))
                    wrist=interpolate([(12,src_left),(18,rest),(27,dst_left),(36,dst_left)],f)
                else: wrist=interpolate([(12,src_left),(23,dst_left),(36,dst_left)],f)
            elbow=shoulder.lerp(wrist,.52)+Vector((-.015,side*.18,-.13))
            segment_pose(upper,shoulder,elbow,.115); segment_pose(lower,elbow,wrist,.09)
            joint.location=elbow; key(joint); hand.location=wrist; hand.rotation_euler=lower.rotation_euler; key(hand)
            cuff.location=elbow.lerp(wrist,.93); cuff.rotation_euler=lower.rotation_euler; key(cuff)
    for o in scene.objects:
        if o.animation_data and o.animation_data.action:
            for curve in o.animation_data.action.fcurves:
                for pt in curve.keyframe_points: pt.interpolation='CONSTANT' if curve.data_path=='hide_render' else 'LINEAR'
    for label,f in [('Mira de origem',1),('Guardar arma longa',12),('Sacar destino',18),('Apoiar duas maos',27),('Mira pronta',36)]: scene.timeline_markers.new(label,frame=f)
    scene.camera=bpy.data.objects['Camera jogo - topo']; scene.render.resolution_x=512; scene.render.resolution_y=512; scene.render.resolution_percentage=100
    scene.cycles.samples=16; scene.cycles.use_denoising=True; scene.render.film_transparent=True; scene.frame_set(1)
    scene.render.filepath=str(folder/'frames'/'001.png'); bpy.ops.wm.save_as_mainfile(filepath=str(folder/(name+'.blend')))
    frames=range(1,37) if '--poses-only' not in sys.argv else [1,12,18,27,36]
    for f in frames:
        scene.frame_set(f); scene.render.filepath=str(folder/'frames'/f'{f:03}.png'); bpy.ops.render.render(write_still=True)
    print('WEAPON TRANSITION READY '+name,flush=True)
