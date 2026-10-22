import bpy, math, json, sys
from pathlib import Path
from mathutils import Vector, Matrix, Quaternion
P=Path('/workspace/warrior-3d-v2')
source=(P/'base_geometry.py').read_text().split('# Reusable walk action.')[0]
source=source.replace("P=Path('/workspace/warrior-3d')", "P=Path('/workspace/warrior-3d-v2')")
source=source.replace("(.20,.004,.003),0,.84,.23", "(.19,.0015,.001),0,.88,.12")
source=source.replace("default_value=.32", "default_value=.035")
source=source.replace("1,.34,.18", "1,.46,.055")
source=source.replace("default_value=.01\nml.new", "default_value=.001\nml.new")
source=source.replace("(.095,y,.092),(.185,.082,.09)", "(.095,y,.108),(.185,.082,.070)")
source=source.replace("(.092,.37,(1,.12,.008),2),(.063,.29,(1,.40,.015),3),(.036,.21,(1,.85,.25),4)","(.125,.49,(1,.065,.001),1),(.084,.36,(1,.30,.004),1.7),(.043,.25,(1,.72,.10),2.2)")
source=source.replace("[.30,.60,1.0][idx]", "[1.0,1.0,1.0][idx]")
exec(compile(source,'base_geometry.py','exec'),globals())
def remove_prefix(prefix):
    for o in list(meshes):
        if o.name.startswith('GEO-'+prefix):
            meshes.remove(o); bpy.data.objects.remove(o,do_unlink=True)

# Closed helmet with tapered rings and a raised central crest.
remove_prefix('helmet-shell')
v=[]; f=[]; n=32
for z,rx,ry in [(1.63,.19,.16),(1.72,.227,.187),(1.88,.215,.181),(1.99,.14,.12),(2.045,.058,.047)]:
    for i in range(n):
        a=2*math.pi*i/n
        v.append((.012+rx*math.cos(a),ry*math.sin(a),z+.012*abs(math.cos(a))))
for j in range(4):
    for i in range(n): f.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
v.append((.016,0,2.072)); tip=len(v)-1
for i in range(n): f.append((4*n+i,4*n+(i+1)%n,tip))
f.append(tuple(reversed(range(n))))
surface('helmet-forged-shell-v2',v,f,steel,'head')
curve('helmet-steel-crest',[(.235,0,1.78),(.226,0,1.88),(.14,0,2.026),(.014,0,2.085),(-.14,0,1.986),(-.20,0,1.82)],.018,steel,'head')
for sg in [-1,1]:
    sphere('helmet-hinge-'+str(sg),(.02,sg*.192,1.77),(.026,.014,.026),edge,'head',16)

# Shoulder shells and overlapping lower lames replace stacked spheres.
remove_prefix('pauldron')
for side,sg in [('L',1),('R',-1)]:
    bone='upper_arm.'+side
    v=[]; f=[]; n=24
    for j in range(6):
        t=.10+j*.27
        for i in range(n):
            a=2*math.pi*i/n
            x=.17*math.sin(t)*math.cos(a)
            y=sg*.345+.185*math.sin(t)*math.sin(a)
            z=1.49+.118*math.cos(t)-.012*abs(math.sin(a))
            v.append((x,y,z))
    for j in range(5):
        for i in range(n): f.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
    f.append(tuple(reversed(range(n))))
    surface('pauldron-shell-'+side,v,f,steel,bone,True,.012)
    border=v[-n:]+[v[-n]]
    curve('pauldron-rim-'+side,border,.008,edge,bone)
    for k in range(2):
        v=[]; f=[]
        for row in range(3):
            for i in range(13):
                a=math.pi*i/12
                v.append((.145*math.cos(a),sg*(.35+.125*math.sin(a)+k*.013),1.46-k*.04-row*.015))
        for j in range(2):
            for i in range(12):
                q=j*13+i; f.append((q,q+1,q+14,q+13))
        surface('pauldron-lame-v2-%s-%s'%(side,k),v,f,steel,bone,True,.01)
    for i in range(4):
        a=i*math.pi/2+.25
        sphere('pauldron-rivet-%s-%s'%(side,i),(.15*math.cos(a),sg*.345+.16*math.sin(a),1.52),(.009,.009,.009),edge,bone,12)

# Faceted front armor with a broad central ridge and restrained edge trim.
v=[(.155,-.215,1.20),(.203,0,1.17),(.155,.215,1.20),
   (.211,-.245,1.43),(.256,0,1.44),(.211,.245,1.43),
   (.16,-.185,1.51),(.192,0,1.53),(.16,.185,1.51)]
surface('breastplate-front',v,[(0,1,4,3),(1,2,5,4),(3,4,7,6),(4,5,8,7)],steel,'spine',True,.012)
for sg in [-1,1]:
    curve('breastplate-edge-'+str(sg),[(.155,sg*.215,1.20),(.211,sg*.245,1.43),(.16,sg*.185,1.51)],.006,edge,'spine')

# Genuine overlapping toe plates instead of bead-like bumps.
remove_prefix('sabatons-')
for side,sg in [('L',1),('R',-1)]:
    y=sg*.165
    for k in range(4):
        v=[]; f=[]
        for row in range(3):
            x=.015+k*.058+row*.027
            width=.083-max(0,x-.13)*.22
            for col in range(13):
                a=math.pi*col/12
                v.append((x,y+width*math.cos(a),.085+(.075-k*.009)*math.sin(a)))
        for row in range(2):
            for col in range(12):
                q=row*13+col; f.append((q,q+1,q+14,q+13))
        surface('sabatons-plate-%s-%s'%(side,k),v,f,steel,'foot.'+side,True,.005)
    # A palm-back plate and curled leather fingers keep the hilt anchored.
    surface('gauntlet-back-'+side,[(.398,sg*.505,1.345),(.404,sg*.57,1.345),(.489,sg*.575,1.347),(.501,sg*.50,1.347)],[(0,1,2,3)],steel,'hand.'+side,True,.012)
    for k in range(3):
        y0=sg*.535+(k-1)*.027
        curve('curled-finger-%s-%s'%(side,k),[(.457,y0,1.335),(.493,y0,1.315),(.488,y0,1.278),(.46,y0,1.27)],.012,leather,'hand.'+side)
    # Visible stitched bracer edge; leather seams are geometry at sprite scale.
    curve('bracer-seam-'+side,[(.17,sg*.50,1.26),(.25,sg*.52,1.29),(.36,sg*.55,1.325)],.003,edge,'forearm.'+side)

# Make the core of the fire visible from above, with a forward leaning outline.
for idx in range(3):
    o=bpy.data.objects['GEO-torch-flame-'+str(idx)]
    for block in o.data.shape_keys.key_blocks:
        for pt in block.data:
            t=max(0,pt.co.z-top.z)
            pt.co.x+=t*.58+idx*.020
            pt.co.y+=idx*.014
    b=o.data.materials[0].node_tree.nodes.get('Principled BSDF')
    b.inputs['Roughness'].default_value=1
    b.inputs['Specular IOR Level'].default_value=0

# Flexible cloth zones with separate tip controls, independent of armor.
remove_prefix('tabard-border-')
bpy.context.view_layer.objects.active=A
for o in bpy.context.selected_objects: o.select_set(False)
A.select_set(True); bpy.ops.object.mode_set(mode='EDIT')
for name,h,t,parent in [
    ('cape.tip',(-.23,0,1.10),(-.38,0,.90),'cape'),
    ('tabard',( .20,0,1.08),(.25,0,.86),'pelvis')]:
    b=AD.edit_bones.new(name); b.head=h; b.tail=t; b.parent=AD.edit_bones[parent]
for side,sg in [('L',1),('R',-1)]:
    for name,h,t in [('CTRL-foot.'+side,(0,sg*.165,.13),(.24,sg*.165,.105)),('CTRL-knee.'+side,(.8,sg*.165,.58),(.8,sg*.165,.75))]:
        b=AD.edit_bones.new(name); b.head=h; b.tail=t; b.parent=AD.edit_bones['root']; b.use_deform=False
    AD.edit_bones['shin.'+side].use_connect=True
    AD.edit_bones['foot.'+side].use_connect=True
    AD.edit_bones['forearm.'+side].use_connect=True
    AD.edit_bones['hand.'+side].use_connect=True
bpy.ops.object.mode_set(mode='OBJECT')
for side in ['L','R']:
    ik=A.pose.bones['shin.'+side].constraints.new('IK'); ik.name='Leg IK / fixed lengths'
    ik.target=A; ik.subtarget='CTRL-foot.'+side; ik.pole_target=A; ik.pole_subtarget='CTRL-knee.'+side
    ik.chain_count=2; ik.use_stretch=False; ik.iterations=100
    for n in ['thigh.','shin.']: A.pose.bones[n+side].ik_stretch=0
    cr=A.pose.bones['foot.'+side].constraints.new('COPY_ROTATION'); cr.name='Foot roll orientation'
    cr.target=A; cr.subtarget='CTRL-foot.'+side; cr.owner_space='POSE'; cr.target_space='POSE'
for name,prefix,bone in [('GEO-short-red-cape','cape','cape.tip'),('GEO-red-tabard','pelvis','tabard')]:
    o=bpy.data.objects[name]
    o.vertex_groups.clear(); g1=o.vertex_groups.new(name=prefix); g2=o.vertex_groups.new(name=bone)
    for v in o.data.vertices:
        if 'cape' in name:
            t=max(0,min(1,(1.42-v.co.z)/.38))
            v.co.x-=.025*math.sin(v.co.y*35)
        else:
            t=max(0,min(1,(1.12-v.co.z)/.24))
            v.co.x+=.14*t
            v.co.y*=.40
        t=t*t*(3-2*t)
        g1.add([v.index],1-t,'REPLACE'); g2.add([v.index],t,'REPLACE')

N=36; fps=30; support=.60; stride=.46; duration=N/fps; speed=stride/(support*duration)
S.frame_start=1; S.frame_end=N; S.render.fps=fps
def smooth(t): return t*t*(3-2*t)
def path_at(u):
    u%=1
    if u<support:
        x=stride/2-stride*u/support; lift=0
        roll=.12*smooth(max(0,(u-.49)/(.60-.49)))
    else:
        t=(u-support)/(1-support)
        m=-stride*(1-support)/support
        x=-stride/2+m*t+(stride-m)*(10*t**3-15*t**4+6*t**5)
        lift=.135*math.sin(math.pi*t)**4
        roll=.12*(1-smooth(t))-.12*math.sin(math.pi*t)**4
    q=Quaternion((0,1,0),roll)
    pivot=Vector((.27,0,-.113))
    ankle=Vector((x,0,.113+lift))+pivot-q@pivot
    return ankle,roll,u<support
def set_control(name,head,roll=0):
    b=AD.bones[name]
    A.pose.bones[name].matrix=Matrix.Translation(Vector(head))@Quaternion((0,1,0),roll).to_matrix().to_4x4()@b.matrix_local.to_quaternion().to_matrix().to_4x4()
def delta(name,axis,angle):
    b=A.pose.bones[name]; q=AD.bones[name].matrix_local.to_quaternion()
    b.rotation_quaternion=q.inverted()@Quaternion(axis,angle)@q
for b in A.pose.bones: b.rotation_mode='QUATERNION'

# Match pole angles to the forward-bending knee plane without guessed signs.
S.frame_set(1)
A.pose.bones['pelvis'].location=(0,-.105,0)
for side,sg in [('L',1),('R',-1)]:
    pt,roll,_=path_at(0 if side=='L' else .5); pt.y=sg*.165
    set_control('CTRL-foot.'+side,pt,roll)
    ik=A.pose.bones['shin.'+side].constraints[0]
    best=(1e9,0)
    for i in range(73):
        angle=-math.pi+i*math.pi/36; ik.pole_angle=angle; bpy.context.view_layer.update()
        k=A.pose.bones['shin.'+side].head
        score=abs(k.y-sg*.165)*20-k.x
        if score<best[0]: best=(score,angle)
    ik.pole_angle=best[1]

controlled=['pelvis','spine','head','cape','cape.tip','tabard','upper_arm.L','upper_arm.R','forearm.L','forearm.R','hand.L','hand.R','CTRL-foot.L','CTRL-foot.R']
for frame in range(1,N+2):
    S.frame_set(frame); u=(frame-1)/N; phase=2*math.pi*u
    pelvis=A.pose.bones['pelvis']; pelvis.location=(0,-.105+.012*math.cos(2*phase),-.012*math.sin(phase))
    delta('pelvis',(0,0,1),.025*math.sin(phase))
    delta('spine',(0,0,1),-.043*math.sin(phase))
    delta('head',(0,0,1),.012*math.sin(phase))
    delta('cape',(0,1,0),.035*math.sin(phase-.5))
    delta('cape.tip',(0,1,0),.07*math.sin(phase-.95))
    delta('tabard',(0,1,0),-.045*math.sin(phase-.65))
    for side,sg in [('L',1),('R',-1)]:
        pt,roll,_=path_at(u+(0 if side=='L' else .5)); pt.y=sg*.165
        set_control('CTRL-foot.'+side,pt,roll)
        swing=sg*math.cos(phase)
        delta('upper_arm.'+side,(0,1,0),.065*swing)
        delta('forearm.'+side,(0,1,0),-.025*swing)
        delta('hand.'+side,(0,1,0),-.020*swing)
    for name in controlled:
        b=A.pose.bones[name]
        for prop in ['location','rotation_quaternion']: b.keyframe_insert(prop,frame=frame,group=name)
    for i,key in enumerate(flames):
        key.value=.5+.40*math.sin(3*phase+i*.7)+.08*math.sin(5*phase+i)
        key.keyframe_insert('value',frame=frame)
act=A.animation_data.action; act.name='Walk_Heavy_36f_IK'; act.use_fake_user=True
for action in bpy.data.actions:
    for fc in action.fcurves:
        points=list(fc.keyframe_points)
        for i,kp in enumerate(points):
            prev=points[-2] if i==0 else points[i-1]
            nxt=points[1] if i==len(points)-1 else points[i+1]
            slope=(nxt.co.y-prev.co.y)/2
            kp.interpolation='BEZIER'; kp.handle_left_type='FREE'; kp.handle_right_type='FREE'
            kp.handle_left=(kp.co.x-1/3,kp.co.y-slope/3)
            kp.handle_right=(kp.co.x+1/3,kp.co.y+slope/3)
        fc.modifiers.new('CYCLES')
alert=act.copy(); alert.name='Walk_Alert_30f_IK'; alert.use_fake_user=True
for fc in alert.fcurves:
    for kp in fc.keyframe_points:
        kp.co.x=1+(kp.co.x-1)*30/36
        kp.handle_left.x=1+(kp.handle_left.x-1)*30/36; kp.handle_right.x=1+(kp.handle_right.x-1)*30/36

def camera(name,loc,target,scale):
    d=bpy.data.cameras.new(name); o=bpy.data.objects.new(name,d); S.collection.objects.link(o)
    o.location=loc; o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler(); d.type='ORTHO'; d.ortho_scale=scale; return o
topcam=camera('CAM-game-topdown',(.28,-2.6,6),(.28,0,1),2.85)
truecam=camera('CAM-vertical-topdown',(.28,0,6),(.28,0,0),2.85)
perscam=camera('CAM-perspective-proof',(3.8,-5.7,3.6),(.20,0,.96),3.05)
def area(name,loc,energy,size,color):
    d=bpy.data.lights.new(name,'AREA'); d.energy=energy; d.size=size; d.color=color
    o=bpy.data.objects.new(name,d); S.collection.objects.link(o); o.location=loc
    o.rotation_euler=(Vector((0,0,1))-o.location).to_track_quat('-Z','Y').to_euler()
area('LGT-key',(-2,-3,5),420,3,(1,.94,.84))
area('LGT-fill',(3,1,4),180,3,(.79,.86,1))
area('LGT-rim',(-2,3,3),260,2.5,(.83,.88,1))
# Emission-only flame surfaces preserve orange instead of reflecting white studio lights.
for i,col in enumerate([(1,.09,.001),(1,.40,.006),(1,.86,.28)]):
    mat=bpy.data.materials['MAT-fire-'+str(i)]; n=mat.node_tree.nodes; n.clear()
    em=n.new('ShaderNodeEmission'); em.inputs[0].default_value=(*col,1); em.inputs[1].default_value=[1.6,1.8,2.0][i]
    tex=n.new('ShaderNodeTexCoord'); sep=n.new('ShaderNodeSeparateXYZ'); ramp=n.new('ShaderNodeValToRGB')
    ramp.color_ramp.elements[0].color=(1,.56,.018,1)
    ramp.color_ramp.elements[1].color=(1,.035,.001,1)
    e=ramp.color_ramp.elements.new(.30); e.color=(1,.82,.10,1)
    e=ramp.color_ramp.elements.new(.60); e.color=(1,.24,.002,1)
    mat.node_tree.links.new(tex.outputs['Generated'],sep.inputs[0]); mat.node_tree.links.new(sep.outputs['Z'],ramp.inputs[0]); mat.node_tree.links.new(ramp.outputs[0],em.inputs[0])
    out=n.new('ShaderNodeOutputMaterial'); mat.node_tree.links.new(em.outputs[0],out.inputs[0])
S.view_settings.view_transform='Standard'; S.view_settings.look='Medium High Contrast' if False else 'None'
S.view_settings.exposure=-.65
S.cycles.samples=64; S.cycles.use_denoising=False
S.render.resolution_x=640; S.render.resolution_y=640
S.camera=topcam; S.frame_set(1); bpy.context.view_layer.update()
S.render.filepath=str(P/'frames'/'walk_')
S['walk_speed_m_s']=speed; S['support_fraction']=support
S['version']='v2: plate geometry, live IK, foot roll, connected skeleton, weighted cloth, two cadences'
S['equipment']='Sword hand.R; torch hand.L'
for o in bpy.context.selected_objects: o.select_set(False)
A.select_set(True); bpy.context.view_layer.objects.active=A
for a in bpy.context.screen.areas:
    if a.type=='VIEW_3D': a.spaces.active.shading.type='MATERIAL'; a.spaces.active.region_3d.view_perspective='CAMERA'
bpy.ops.wm.save_as_mainfile(filepath=str(P/'warrior_v2.blend'))
report={'version':2,'meshes':len(meshes),'base_vertices':sum(len(o.data.vertices) for o in meshes),'bones':len(AD.bones),'frames':N,'fps':fps,'speed_m_s':speed,'support_fraction':support,'cadences':['Walk_Heavy_36f_IK','Walk_Alert_30f_IK'],'pole_angles':{s:A.pose.bones['shin.'+s].constraints[0].pole_angle for s in ['L','R']}}
(P/'model_report.json').write_text(json.dumps(report,indent=2)); print(json.dumps(report))
if '--build-only' not in sys.argv:
    S.cycles.samples=48
    for camera_,file in [(topcam,'topdown'),(perscam,'perspective'),(truecam,'vertical')]:
        S.camera=camera_; S.render.filepath=str(P/'validation'/(file+'.png')); bpy.ops.render.render(write_still=True)
