"""Rig and animate the approved V3, preserving its static source. Blender 5.2."""
import bpy, math, json, sys, re
from pathlib import Path
from mathutils import Vector, Matrix
BASE=Path(__file__).resolve().parent
OUT=BASE/'Animation';OUT.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(BASE/'espreitador-encapuzado-v3.blend'))
scene=bpy.context.scene;scene.render.fps=24
geometry=[o for o in scene.objects if o.type in ('MESH','CURVE')]
for o in geometry:
    bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
    if o.type=='CURVE':bpy.ops.object.convert(target='MESH')
    # World-space meshes eliminate object-origin differences during skinning.
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)

data=bpy.data.armatures.new('StalkerV3_Skeleton');rig=bpy.data.objects.new('RIG_Espreitador_V3',data);scene.collection.objects.link(rig)
rig.show_in_front=True
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.object.mode_set(mode='EDIT')
def bone(name,head,tail,parent=None,deform=True):
    b=data.edit_bones.new(name);b.head=head;b.tail=tail;b.use_deform=deform
    if parent:b.parent=data.edit_bones[parent]
    return b
bone('Root',(0,0,0),(0,0,.28),deform=False)
bone('Pelvis',(-.27,0,1.10),(-.28,0,1.40),'Root')
bone('Chest',(-.28,0,1.40),(.02,0,1.98),'Pelvis')
bone('Head',(.03,0,1.96),(.35,0,2.12),'Chest')
feet={};wrists={};digit_roots={}
for side,label in [(-1,'R'),(1,'L')]:
    y=side*.22;advance=.08 if side==-1 else -.08
    hip=(-.25,y,1.15);knee=(.11+advance,y,.67);ankle=(-.10+advance,y,.20)
    feet[label]=Vector(ankle)
    bone('Thigh.'+label,hip,knee,'Pelvis');bone('Shin.'+label,knee,ankle,'Thigh.'+label)
    bone('Foot.'+label,ankle,(.20+advance,y,.07),'Shin.'+label)
    bone('CTRL_Foot.'+label,ankle,(ankle[0]+.22,y,.20),'Root',False)
    bone('POLE_Knee.'+label,(.85,y,.69),(.85,y,.90),'Root',False)
    shoulder=(0,side*.33,1.98+(.045 if side==-1 else -.04));elbow=(-.12,side*.56,1.43);wrist=(.24,side*.68,.94)
    wrists[label]=Vector(wrist)
    bone('UpperArm.'+label,shoulder,elbow,'Chest');bone('Forearm.'+label,elbow,wrist,'UpperArm.'+label)
    bone('Hand.'+label,wrist,(.33,side*.73,.78),'Forearm.'+label)
    bone('CTRL_Wrist.'+label,wrist,(.24,side*.68,1.17),'Root',False)
    bone('POLE_Elbow.'+label,(-.65,side*.82,1.43),(-.65,side*.82,1.66),'Root',False)
    for digit in range(4):
        yy=side*.72+(digit-1.5)*.049;length=[.24,.29,.275,.225][digit];splay=(digit-1.5)*.021
        start=(.33,yy,.80);end=(.44,yy+splay*1.5,.80-length)
        bone('Finger'+str(digit)+'.'+label,start,end,'Hand.'+label);digit_roots[label,digit]=Vector(start)
    bone('Thumb.'+label,(.28,side*.62,.90),(.48,side*.59,.68),'Hand.'+label)

cloth_bones={}
for o in geometry:
    if o.name.startswith(('Painel casaco','Faixa pendente','Sobreposicao ombro')):
        center=sum((v.co for v in o.data.vertices),Vector())/len(o.data.vertices)
        top=max(v.co.z for v in o.data.vertices);bottom=min(v.co.z for v in o.data.vertices)
        pivot=(center.x,center.y,top-.10)
        name='Cloth_'+str(len(cloth_bones)).zfill(2)
        bone(name,pivot,(center.x,center.y,max(bottom,pivot[2]-.45)),'Chest')
        cloth_bones[o.name]=(name,pivot[2],bottom)
bpy.ops.object.mode_set(mode='OBJECT')

for label in ['R','L']:
    c=rig.pose.bones['Shin.'+label].constraints.new('IK');c.target=rig;c.subtarget='CTRL_Foot.'+label;c.chain_count=2
    c.pole_target=rig;c.pole_subtarget='POLE_Knee.'+label;c.pole_angle=-math.pi/2
    c.use_stretch=False
    c=rig.pose.bones['Forearm.'+label].constraints.new('IK');c.target=rig;c.subtarget='CTRL_Wrist.'+label;c.chain_count=2
    c.pole_target=rig;c.pole_subtarget='POLE_Elbow.'+label;c.pole_angle=-math.pi/2;c.use_stretch=False
for p in rig.pose.bones:p.rotation_mode='XYZ'

# Calibrate each pole against the approved rest bend; a fixed -90 degree pole
# rotates these +X-facing knees sideways because their bone rolls differ.
for label in ['R','L']:
    side=-1 if label=='R' else 1;advance=.08 if label=='R' else -.08
    for bone_name,desired in [('Shin.'+label,Vector((.11+advance,side*.22,.67))),('Forearm.'+label,Vector((-.12,side*.56,1.43)))]:
        c=rig.pose.bones[bone_name].constraints[0]
        def pole_error(angle):
            c.pole_angle=angle;bpy.context.view_layer.update()
            return (rig.pose.bones[bone_name].head-desired).length_squared
        best=min((i*math.tau/72-math.pi for i in range(72)),key=pole_error)
        best=min((best+i*.002 for i in range(-24,25)),key=pole_error)
        c.pole_angle=best;rig[bone_name+' pole angle']=best
bpy.context.view_layer.update()

def clamp(v):return max(0,min(1,v))
def dist_segment(p,a,b):
    d=b-a;t=clamp((p-a).dot(d)/d.length_squared);return (p-a-t*d).length
def side_for(o):
    if o.name.endswith(' -1'):return 'R'
    if o.name.endswith(' 1'):return 'L'
    mean=sum(v.co.y for v in o.data.vertices)/max(1,len(o.data.vertices));return 'R' if mean<0 else 'L'
def attach(o,weightfn):
    o.vertex_groups.clear();groups={}
    for v in o.data.vertices:
        weights=weightfn(v.co)
        for name,value in weights.items():
            if value<.0001:continue
            if name not in groups:groups[name]=o.vertex_groups.new(name=name)
            groups[name].add([v.index],value,'REPLACE')
    mod=o.modifiers.new('Skin editable V3','ARMATURE');mod.object=rig;mod.use_deform_preserve_volume=True
    # Deform before surface subdivision and fabric thickness.
    bpy.context.view_layer.objects.active=o
    while o.modifiers.find(mod.name)>0:bpy.ops.object.modifier_move_up(modifier=mod.name)
    o.parent=rig

for o in geometry:
    name=o.name;label=side_for(o)
    if name in cloth_bones:
        b,top,bottom=cloth_bones[name]
        attach(o,lambda p,b=b,top=top,bottom=bottom:{b:clamp((top-p.z)/max(.1,top-bottom)),'Chest':1-clamp((top-p.z)/max(.1,top-bottom))})
    elif name.startswith('Costura Painel casaco') or name.startswith('Costura Sobreposicao ombro') or name.startswith('Costura Faixa pendente'):
        key=re.sub(r'\.\d{3}$','',name[len('Costura '):]);b,top,bottom=cloth_bones[key]
        attach(o,lambda p,b=b,top=top,bottom=bottom:{b:clamp((top-p.z)/max(.1,top-bottom)),'Chest':1-clamp((top-p.z)/max(.1,top-bottom))})
    elif name.startswith('Fiapo bainha'):
        closest=min(cloth_bones,key=lambda k:(Vector((sum(v.co.x for v in o.data.vertices)/len(o.data.vertices),sum(v.co.y for v in o.data.vertices)/len(o.data.vertices),0))-Vector((rig.data.bones[cloth_bones[k][0]].head_local.x,rig.data.bones[cloth_bones[k][0]].head_local.y,0))).length)
        attach(o,lambda p,b=cloth_bones[closest][0]:{b:1})
    elif name.startswith(('Capuz','Borda capuz','Rosto oculto')):attach(o,lambda p:{'Head':1})
    elif name.startswith('Calca anatomica'):
        attach(o,lambda p,l=label:{'Thigh.'+l:clamp((p.z-.59)/.18),'Shin.'+l:1-clamp((p.z-.59)/.18)})
    elif name.startswith(('Pe anatomico','Dedo pe','Unha pe')):attach(o,lambda p,l=label:{'Foot.'+l:1})
    elif name.startswith(('Manga dobrada','Dobra manga')):
        attach(o,lambda p,l=label:{'UpperArm.'+l:clamp((p.z-1.35)/.18),'Forearm.'+l:1-clamp((p.z-1.35)/.18)})
    elif name.startswith('Borda punho'):attach(o,lambda p,l=label:{'Forearm.'+l:1})
    elif name.startswith('Mao organica'):
        def handweights(p,l=label):
            candidates=['Finger'+str(k)+'.'+l for k in range(4)]+['Thumb.'+l]
            b=min(candidates,key=lambda k:dist_segment(p,rig.data.bones[k].head_local,rig.data.bones[k].tail_local))
            w=clamp((.83-p.z)/.10)
            return {b:w,'Hand.'+l:1-w}
        attach(o,handweights)
    elif name.startswith('Garra curva'):
        parts=name.split();l='R' if parts[2]=='-1' else 'L';attach(o,lambda p,b='Finger'+parts[3]+'.'+l:{b:1})
    elif name.startswith('Garra polegar'):attach(o,lambda p,l=label:{'Thumb.'+l:1})
    elif name.startswith(('Tendao mao','Veia dorsal')):attach(o,lambda p,l=label:{'Hand.'+l:1})
    else:
        attach(o,lambda p:{'Pelvis':1-clamp((p.z-1.16)/.48),'Chest':clamp((p.z-1.16)/.48)})

def reset():
    for p in rig.pose.bones:p.location=(0,0,0);p.rotation_euler=(0,0,0);p.scale=(1,1,1)
def target(name,point):
    p=rig.pose.bones[name];p.location=p.bone.matrix_local.to_3x3().inverted()@(Vector(point)-p.bone.head_local)
def keys(frame):
    for p in rig.pose.bones:
        p.keyframe_insert(data_path='location',frame=frame);p.keyframe_insert(data_path='rotation_euler',frame=frame)
def smooth(t):return t*t*(3-2*t)
def pose_walk(frame):
    reset();phase=(frame-1)/32;wave=math.sin(phase*math.tau)
    rig.pose.bones['Pelvis'].location.y=.012*math.cos(phase*math.tau*2)
    rig.pose.bones['Pelvis'].rotation_euler.y=.025*wave
    rig.pose.bones['Chest'].rotation_euler.y=-.045*wave
    rig.pose.bones['Chest'].rotation_euler.z=.022*math.cos(phase*math.tau*2)
    rig.pose.bones['Head'].rotation_euler.z=-.018*math.cos(phase*math.tau*2-.45)
    for label,offset in [('R',0),('L',.5)]:
        t=(phase+offset)%1
        if t<.60:x=.20-.40*t/.60;lift=0
        else:u=(t-.60)/.40;x=-.20+.40*smooth(u);lift=.105*math.sin(u*math.pi)**1.4
        target('CTRL_Foot.'+label,feet[label]+Vector((x,0,lift)))
        # Keep the sole orientation in world space despite shin IK.
        bpy.context.view_layer.update()
        p=rig.pose.bones['Foot.'+label];rest=p.bone.matrix_local.copy();rest.translation=rig.pose.bones['CTRL_Foot.'+label].head
        p.matrix=rest
        target('CTRL_Wrist.'+label,wrists[label]+Vector((-.075*math.sin((phase+offset)*math.tau),0,.018*math.cos((phase+offset)*math.tau))))
        rig.pose.bones['Hand.'+label].rotation_euler.y=.025*math.sin((phase+offset)*math.tau-.4)
    for i,(name,_,_) in enumerate(cloth_bones.values()):
        p=rig.pose.bones[name];p.rotation_euler.x=.035*math.sin(phase*math.tau-.6+i*.3);p.rotation_euler.z=.018*math.sin(phase*math.tau*2-.8+i*.2)
    keys(frame)

def pose_attack(frame):
    reset()
    if frame<=7:
        t=smooth((frame-1)/6);w=wrists['R'].lerp(Vector((-.02,-.66,1.34)),t);twist=-.09*t;finger=.12*t
    elif frame<=12:
        t=smooth((frame-7)/5);w=Vector((-.02,-.66,1.34)).lerp(Vector((.78,.02,1.42)),t);twist=-.09+.23*t;finger=.12+.08*t
    else:
        t=smooth((frame-12)/8);w=Vector((.78,.02,1.42)).lerp(wrists['R'],t);twist=.14*(1-t);finger=.20*(1-t)
    target('CTRL_Wrist.R',w)
    target('CTRL_Wrist.L',wrists['L']+Vector((-.08*math.sin((frame-1)/19*math.pi),.035*math.sin((frame-1)/19*math.pi),.10*math.sin((frame-1)/19*math.pi))))
    rig.pose.bones['Chest'].rotation_euler.y=twist
    rig.pose.bones['Head'].rotation_euler.z=-.055*math.sin((frame-1)/19*math.pi)
    rig.pose.bones['Hand.R'].rotation_euler.x=-.22*math.sin((frame-1)/19*math.pi)
    for digit in range(4):rig.pose.bones['Finger'+str(digit)+'.R'].rotation_euler.x=finger
    for label in ['R','L']:
        target('CTRL_Foot.'+label,feet[label]);bpy.context.view_layer.update()
        p=rig.pose.bones['Foot.'+label];rest=p.bone.matrix_local.copy();rest.translation=rig.pose.bones['CTRL_Foot.'+label].head;p.matrix=rest
    # Secondary motion returns exactly to neutral at both ends.
    envelope=math.sin((frame-1)/19*math.pi)
    for i,(name,_,_) in enumerate(cloth_bones.values()):
        p=rig.pose.bones[name];p.rotation_euler.x=.045*envelope*math.sin((frame-1)*.35+i*.2);p.rotation_euler.z=-twist*.24*envelope
    keys(frame)

def new_action(name,frames,fn):
    rig.animation_data_create();rig.animation_data.action=None
    action=bpy.data.actions.new(name);rig.animation_data.action=action
    for f in frames:scene.frame_set(f);fn(f)
    action.use_fake_user=True
    # Dense keyed poses use linear interpolation to avoid IK overshoot.
    for layer in action.layers:
        for strip in layer.strips:
            for bag in strip.channelbags:
                for curve in bag.fcurves:
                    for point in curve.keyframe_points:point.interpolation='LINEAR'
    return action
walk=new_action('Walk',range(1,34),pose_walk)
attack=new_action('ClawAttack',range(1,21),pose_attack)
rig.animation_data.action=walk
scene.frame_start=1;scene.frame_end=32;scene.frame_set(1)
scene.timeline_markers.clear()
for name,f in [('Walk start',1),('Opposite support',17),('Loop closure (not exported)',33)]:scene.timeline_markers.new(name,frame=f)
scene.camera=bpy.data.objects['Superior frente +X'];scene.camera.data.ortho_scale=3.20
scene.render.resolution_x=384;scene.render.resolution_y=384;scene.cycles.samples=16
scene['Attack impact frame']=12;scene['Unity preparation seconds']=.3
scene['Animation notes']='Walk 1-32, closure 33. ClawAttack 1-20, impact 12. Root stationary. Actions selectable in Action Editor.'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'espreitador-v3-animado.blend'))
metadata={'fps':24,'walk_frames':32,'closure_frame':33,'attack_frames':20,'impact_frame':12,'preparation_seconds_game':.3,'root_motion':False,'blender':bpy.app.version_string}
(OUT/'animation.json').write_text(json.dumps(metadata,indent=2),encoding='utf-8')
if '--rig-only' not in sys.argv:
    for action,count,folder in [(walk,32,'Walk'),(attack,20,'ClawAttack')]:
        rig.animation_data.action=action
        for view,camera in [('Top','Superior frente +X'),('Perspective','Tres quartos')]:
            dest=OUT/view/folder;dest.mkdir(parents=True,exist_ok=True);scene.camera=bpy.data.objects[camera]
            scene.camera.data.ortho_scale=3.20
            for f in range(1,count+1):
                scene.frame_set(f);scene.render.filepath=str(dest/f'{f:03}.png');bpy.ops.render.render(write_still=True)
    print('Animation renders finished.')
