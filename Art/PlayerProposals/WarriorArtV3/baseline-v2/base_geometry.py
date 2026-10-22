import bpy, math, json, sys, random
from pathlib import Path
from mathutils import Vector, Matrix
P=Path('/workspace/warrior-3d')
random.seed(12)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
S=bpy.context.scene
S.render.engine='CYCLES'; S.cycles.samples=32; S.cycles.use_denoising=False
S.cycles.max_bounces=6
S.render.resolution_x=640; S.render.resolution_y=640; S.render.resolution_percentage=100
S.render.image_settings.file_format='PNG'; S.render.image_settings.color_mode='RGBA'
S.render.film_transparent=True; S.render.fps=30; S.frame_start=1; S.frame_end=30
S.view_settings.view_transform='AgX'
S.view_settings.look='AgX - Medium High Contrast'
S.world.use_nodes=True
S.world.node_tree.nodes.clear()
bg=S.world.node_tree.nodes.new('ShaderNodeBackground'); bg.inputs[0].default_value=(.22,.25,.30,1); bg.inputs[1].default_value=.35
wo=S.world.node_tree.nodes.new('ShaderNodeOutputWorld'); S.world.node_tree.links.new(bg.outputs[0],wo.inputs[0])

def material(name,color,metal=0,rough=.5,noise=0):
    m=bpy.data.materials.new('MAT-'+name); m.use_nodes=True
    n=m.node_tree.nodes; l=m.node_tree.links; b=n.get('Principled BSDF')
    b.inputs['Base Color'].default_value=(*color,1); b.inputs['Metallic'].default_value=metal; b.inputs['Roughness'].default_value=rough
    if noise:
        tex=n.new('ShaderNodeTexNoise'); tex.inputs['Scale'].default_value=85; tex.inputs['Detail'].default_value=3
        bump=n.new('ShaderNodeBump'); bump.inputs['Strength'].default_value=noise; bump.inputs['Distance'].default_value=.002
        l.new(tex.outputs['Fac'],bump.inputs['Height']); l.new(bump.outputs[0],b.inputs['Normal'])
        ramp=n.new('ShaderNodeValToRGB')
        ramp.color_ramp.elements[0].color=(*[v*.65 for v in color],1)
        ramp.color_ramp.elements[1].color=(*color,1)
        l.new(tex.outputs['Fac'],ramp.inputs[0]); l.new(ramp.outputs[0],b.inputs['Base Color'])
    return m
steel=material('weathered-steel',(.38,.42,.47),1,.34,.18)
edge=material('antique-brass',(.46,.28,.095),1,.32,.08)
dark=material('dark-joints',(.027,.025,.023),0,.8,.2)
leather=material('worn-brown-leather',(.115,.052,.022),0,.67,.25)
red=material('crimson-woven-cloth',(.20,.004,.003),0,.84,.23)
red.node_tree.nodes.get('Principled BSDF').inputs['Sheen Weight'].default_value=.32
blade=material('blade-steel',(.63,.69,.73),1,.22,.03)
wood=material('torch-wood',(.17,.068,.025),0,.8,.25)
mail=material('chainmail',(.12,.145,.16),1,.55)
mn=mail.node_tree.nodes; ml=mail.node_tree.links
vt=mn.new('ShaderNodeTexVoronoi'); vt.inputs['Scale'].default_value=150; vt.feature='DISTANCE_TO_EDGE'
mb=mn.new('ShaderNodeBump'); mb.inputs['Strength'].default_value=.5; mb.inputs['Distance'].default_value=.01
ml.new(vt.outputs['Distance'],mb.inputs['Height']); ml.new(mb.outputs[0],mn.get('Principled BSDF').inputs['Normal'])

# Anatomical convention: forward +X, left +Y, right -Y, Z upward.
rests={
 'root':((0,0,0),(0,0,.25),None),
 'pelvis':((0,0,1.02),(0,0,1.19),'root'),
 'spine':((0,0,1.19),(0,0,1.55),'pelvis'),
 'head':((0,0,1.55),(0,0,1.91),'spine'),
 'cape':((-.16,0,1.49),(-.23,0,1.10),'spine'),
}
for side,sign in [('L',1),('R',-1)]:
    y=sign*.165
    rests['thigh.'+side]=((0,y,1.02),(.055,y,.57),'pelvis')
    rests['shin.'+side]=((.055,y,.57),(0,y,.13),'thigh.'+side)
    rests['foot.'+side]=((0,y,.13),(.24,y,.105),'shin.'+side)
    rests['upper_arm.'+side]=((0,sign*.33,1.49),(.12,sign*.46,1.20),'spine')
    rests['forearm.'+side]=((.12,sign*.46,1.20),(.40,sign*.52,1.29),'upper_arm.'+side)
    rests['hand.'+side]=((.40,sign*.52,1.29),(.51,sign*.54,1.30),'forearm.'+side)
AD=bpy.data.armatures.new('ARM-warrior-3D')
A=bpy.data.objects.new('ARM-warrior-3D',AD); S.collection.objects.link(A)
bpy.context.view_layer.objects.active=A; A.select_set(True); bpy.ops.object.mode_set(mode='EDIT')
for name,(h,t,parent) in rests.items():
    b=AD.edit_bones.new(name); b.head=h; b.tail=t
    if parent: b.parent=AD.edit_bones[parent]
bpy.ops.object.mode_set(mode='OBJECT'); A.show_in_front=True
meshes=[]
def finish(o,name,mat,bone,smooth=True):
    o.name='GEO-'+name; o.data.materials.append(mat)
    if smooth:
        for f in o.data.polygons: f.use_smooth=True
    if bone:
        vg=o.vertex_groups.new(name=bone); vg.add(list(range(len(o.data.vertices))),1,'REPLACE')
        m=o.modifiers.new('Armature','ARMATURE'); m.object=A
    meshes.append(o); return o
def sphere(name,loc,scale,mat,bone,seg=24):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg,ring_count=12,location=loc)
    o=bpy.context.object; o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return finish(o,name,mat,bone)
def tube(name,a,b,r1,r2,mat,bone,n=20):
    a,b=Vector(a),Vector(b)
    bpy.ops.mesh.primitive_cone_add(vertices=n,radius1=r1,radius2=r2,depth=(b-a).length,location=(a+b)/2)
    o=bpy.context.object; o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()
    return finish(o,name,mat,bone)
def cube(name,loc,scale,mat,bone,bev=.015):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc); o=bpy.context.object; o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bev:
        m=o.modifiers.new('Soft forged edges','BEVEL'); m.width=bev; m.segments=3
    return finish(o,name,mat,bone,False)
def surface(name,verts,faces,mat,bone,smooth=True,thick=0):
    me=bpy.data.meshes.new(name); me.from_pydata(verts,[],faces); me.update()
    o=bpy.data.objects.new('GEO-'+name,me); S.collection.objects.link(o)
    if thick:
        m=o.modifiers.new('Plate thickness','SOLIDIFY'); m.thickness=thick
        m=o.modifiers.new('Forged bevel','BEVEL'); m.width=.005; m.segments=2
    return finish(o,name,mat,bone,smooth)
def curve(name,points,r,mat,bone):
    cu=bpy.data.curves.new(name,'CURVE'); cu.dimensions='3D'; cu.bevel_depth=r; cu.bevel_resolution=2
    sp=cu.splines.new('POLY'); sp.points.add(len(points)-1)
    for p,co in zip(sp.points,points): p.co=(*co,1)
    o=bpy.data.objects.new(name,cu); S.collection.objects.link(o)
    bpy.context.view_layer.objects.active=o; o.select_set(True)
    for other in bpy.context.selected_objects:
        if other!=o: other.select_set(False)
    bpy.ops.object.convert(target='MESH')
    return finish(bpy.context.object,name,mat,bone)
def ring(name,center,rx,ry,z,mat,bone,r=.009):
    points=[(center[0]+rx*math.cos(a),center[1]+ry*math.sin(a),z) for a in [2*math.pi*k/48 for k in range(49)]]
    return curve(name,points,r,mat,bone)

# Tapered torso armor with actual curved cross-sections and raised brass seams.
levels=[(1.07,.15,.22),(1.20,.17,.255),(1.40,.225,.31),(1.52,.16,.245)]
vv=[]; ff=[]; N=40
for z,rx,ry in levels:
    for i in range(N):
        a=2*math.pi*i/N; vv.append((rx*math.cos(a),ry*math.sin(a),z))
for j in range(len(levels)-1):
    for i in range(N): ff.append((j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i))
ff.extend([tuple(reversed(range(N))),tuple((len(levels)-1)*N+i for i in range(N))])
surface('cuirass',vv,ff,steel,'spine')
ring('waist-cuirass-trim',(0,0),.153,.225,1.078,edge,'spine')
curve('chest-central-ridge',[(.157,0,1.075),(.183,0,1.2),(.242,0,1.40),(.173,0,1.52)],.010,edge,'spine')
sphere('neck-mail',(0,0,1.58),(.17,.18,.13),mail,'head')
sphere('hips-leather',(0,0,1.015),(.17,.225,.17),leather,'pelvis')
ring('belt',(0,0),.183,.26,1.065,leather,'pelvis',.036)
cube('belt-buckle',(.19,0,1.065),(.045,.095,.075),edge,'pelvis',.009)
cube('buckle-inset',(.216,0,1.065),(.008,.058,.040),dark,'pelvis',.004)
for i in range(16):
    a=2*math.pi*i/16
    sphere('belt-rivet-%02d'%i,(.192*math.cos(a),.266*math.sin(a),1.066),(.011,.011,.011),edge,'pelvis',12)
sphere('belt-pouch',(-.04,-.285,.99),(.085,.073,.12),leather,'pelvis')
cube('pouch-flap',(.015,-.313,1.04),(.10,.05,.055),leather,'pelvis')
sphere('pouch-button',(.03,-.345,1.025),(.010,.010,.010),edge,'pelvis',12)

# Helmet: closed metal bowl, forward visor, crown seam, breathing ports.
sphere('helmet-shell',(.012,0,1.79),(.225,.195,.27),steel,'head',32)
ring('helmet-lower-rim',(.025,0),.215,.19,1.64,edge,'head',.010)
curve('helmet-crown-ridge',[(.21,0,1.71),(.231,0,1.82),(.15,0,2.00),(.015,0,2.065),(-.14,0,1.96),(-.205,0,1.80)],.012,edge,'head')
visor_v=[]
for z in [1.645,1.74,1.85,1.905]:
    for i in range(17):
        a=-1.1+2.2*i/16
        visor_v.append((.022+.233*math.cos(a),.194*math.sin(a),z))
visor_f=[(j*17+i,j*17+i+1,(j+1)*17+i+1,(j+1)*17+i) for j in range(3) for i in range(16)]
surface('visor-faceplate',visor_v,visor_f,steel,'head',True,.009)
curve('visor-eye-slit',[(.024+.242*math.cos(a),.198*math.sin(a),1.846) for a in [-.88+1.76*i/24 for i in range(25)]],.014,dark,'head')
curve('visor-brow',[(.023+.247*math.cos(a),.20*math.sin(a),1.881) for a in [-.94+1.88*i/24 for i in range(25)]],.009,edge,'head')
curve('visor-nasal',[(.267,0,1.90),(.270,0,1.80),(.260,0,1.68)],.014,edge,'head')
for sg in [-1,1]:
    for i in range(4):
        a=sg*(.22+i*.14)
        sphere('visor-air-%s-%s'%(sg,i),(.028+.240*math.cos(a),.198*math.sin(a),1.716),(.006,.006,.009),dark,'head',12)

# Scarf and hanging red tabard, modeled with folds and solid thickness.
ring('scarf-wrap',(0,0),.202,.218,1.545,red,'spine',.055)
for layer in range(3):
    ring('scarf-fold-%d'%layer,(0,0),.200+layer*.008,.222+layer*.003,1.52+layer*.024,red,'spine',.015)
v=[]; f=[]
for j in range(9):
    z=1.38-j*.068
    for i in range(9):
        y=-.12+i*.030
        x=.195+.018*math.cos(i*math.pi/2)+.028*math.sin(j*.7)
        if z<1.07: x=.18+.038*math.sin(j*.7)
        v.append((x,y,z))
for j in range(8):
    for i in range(8):
        a=j*9+i; f.append((a,a+1,a+10,a+9))
surface('red-tabard',v,f,red,'pelvis',True,.009)
for side in [-1,1]:
    curve('tabard-border-%s'%side,[(.211+.032*math.sin(j*.7),side*.12,1.38-j*.068) for j in range(9)],.004,edge,'pelvis')
v=[]; f=[]
for j in range(9):
    for i in range(11):
        y=-.245+i*.049
        v.append((-.185-.025*j+.02*math.sin(i*1.4),y,1.51-j*.073+.018*math.cos(i*1.6)))
for j in range(8):
    for i in range(10):
        a=j*11+i; f.append((a,a+1,a+12,a+11))
surface('short-red-cape',v,f,red,'cape',True,.01)

# Articulated limb armor, leather straps and rounded sabatons.
for side,sg in [('L',1),('R',-1)]:
    y=sg*.165
    thigh='thigh.'+side; shin='shin.'+side; foot='foot.'+side
    tube('mail-thigh-'+side,(0,y,.99),(.05,y,.56),.115,.092,mail,thigh)
    sphere('thigh-plate-'+side,(.07,y,.79),(.10,.10,.21),steel,thigh)
    ring('thigh-strap-'+side,(.018,y),.117,.115,.865,leather,thigh,.019)
    sphere('knee-cap-'+side,(.100,y,.57),(.085,.09,.079),steel,shin)
    tube('greave-'+side,(.049,y,.53),(.005,y,.18),.085,.065,steel,shin)
    curve('greave-ridge-'+side,[(.13,y,.51),(.10,y,.36),(.07,y,.18)],.008,edge,shin)
    ring('ankle-leather-'+side,(0,y),.074,.076,.165,leather,foot,.02)
    sphere('boot-'+side,(.095,y,.092),(.185,.082,.09),leather,foot)
    cube('boot-sole-'+side,(.097,y,.035),(.33,.158,.036),dark,foot,.015)
    for k in range(4):
        sphere('sabatons-%s-%s'%(side,k),(.05+k*.052,y,.121-k*.006),(.059,.083,.043),steel,foot)
    upper='upper_arm.'+side; fore='forearm.'+side; hand='hand.'+side
    shoulder=(0,sg*.33,1.49); elbow=(.12,sg*.46,1.20); wrist=(.40,sg*.52,1.29)
    tube('mail-upper-arm-'+side,shoulder,elbow,.113,.085,mail,upper)
    for k in range(1):
        sphere('pauldron-%s-%s'%(side,k),(.01+k*.034,sg*(.335+k*.035),1.53-k*.058),(.155-k*.014,.16-k*.012,.125-k*.015),steel,upper)
    for k in range(2):
        pv=[]; pf=[]
        for row in range(4):
            ang=.4+row*.2
            for col in range(17):
                a=col*math.pi/16
                pv.append((.135*math.cos(a),sg*(.35+.14*math.sin(ang)*math.sin(a)+k*.018),1.51-.12*math.cos(ang)-k*.035))
        for row in range(3):
            for col in range(16):
                a=row*17+col; pf.append((a,a+1,a+18,a+17))
        surface('pauldron-lame-%s-%s'%(side,k),pv,pf,steel,upper,True,.008)
    curve('pauldron-border-'+side,[(.14*math.cos(a),sg*.35+.145*math.sin(a),1.50) for a in [2*math.pi*k/32 for k in range(33)]],.010,edge,upper)
    sphere('elbow-'+side,elbow,(.094,.082,.079),steel,fore)
    tube('bracer-'+side,(.15,sg*.468,1.21),wrist,.081,.067,leather,fore)
    tube('bracer-steel-'+side,(.20,sg*.475,1.255),(.355,sg*.507,1.30),.072,.066,steel,fore)
    for k in range(2):
        t=.30+k*.13
        pt=Vector(elbow).lerp(Vector(wrist),t)
        tube('bracer-band-%s-%s'%(side,k),pt-Vector((.008,0,0)),pt+Vector((.008,0,0)),.086,.086,edge,fore)
    sphere('glove-'+side,(.451,sg*.535,1.296),(.082,.065,.059),leather,hand)
    for k in range(4):
        sphere('glove-knuckle-%s-%s'%(side,k),(.466,sg*.535+(k-1.5)*.023,1.341),(.025,.014,.012),steel,hand,12)
    sphere('glove-thumb-'+side,(.428,sg*.578,1.26),(.04,.023,.033),leather,hand)

# Right-hand sword: true beveled diamond cross-section with a single pointed tip.
hand=Vector((.46,-.535,1.297)); direction=Vector((.90,-.40,.08)).normalized()
cross=Vector((-direction.y,direction.x,0)).normalized(); normal=direction.cross(cross).normalized()
guard=hand+direction*.085
tube('sword-hilt',hand-direction*.10,guard,.027,.026,leather,'hand.R')
sphere('sword-pommel',hand-direction*.105,(.041,.041,.041),edge,'hand.R')
curve('sword-crossguard',[guard-cross*.135-direction*.025,guard-cross*.08,guard,guard+cross*.08,guard+cross*.135-direction*.025],.023,edge,'hand.R')
vv=[]
for dist,w in [(.015,.050),(.65,.035)]:
    c=guard+direction*dist
    vv.extend([c+cross*w,c+normal*.010,c-cross*w,c-normal*.010])
vv.append(guard+direction*.83)
ff=[(i,(i+1)%4,(i+1)%4+4,i+4) for i in range(4)]+[(4+i,4+(i+1)%4,8) for i in range(4)]+[(3,2,1,0)]
surface('sword-blade',vv,ff,blade,'hand.R',False)

# Left-hand torch: wrapped wood, iron bowl and independent volumetric flame meshes.
th=Vector((.46,.535,1.30)); td=Vector((.35,.12,.46)).normalized(); top=th+td*.43
tube('torch-shaft',th-td*.14,top,.033,.044,wood,'hand.L')
for k in range(7):
    pt=top-td*(.012+k*.019)
    tube('torch-wrap-%d'%k,pt-td*.007,pt+td*.007,.055,.055,leather,'hand.L')
tube('torch-bowl',top-td*.025,top+td*.05,.054,.084,dark,'hand.L')
flames=[]
for idx,(radius,height,col,strength) in enumerate([(.092,.37,(1,.12,.008),2),(.063,.29,(1,.40,.015),3),(.036,.21,(1,.85,.25),4)]):
    fm=material('fire-'+str(idx),col,0,.4)
    bs=fm.node_tree.nodes.get('Principled BSDF'); bs.inputs['Emission Color'].default_value=(*col,1); bs.inputs['Emission Strength'].default_value=strength
    bs.inputs['Alpha'].default_value=[.30,.60,1.0][idx]
    vv=[]; ff=[]; rings=8; N=20
    for j in range(rings):
        t=j/(rings-1)
        rad=radius*(math.sin(math.pi*(t*.94+.04))**.7)*(1-t*.72)
        cx=.035*math.sin(t*5+idx)*t
        cy=.028*math.sin(t*7+idx)*t
        for i in range(N):
            a=i*2*math.pi/N
            vv.append(top+Vector((cx+rad*math.cos(a),cy+rad*math.sin(a),.035+height*t)))
    vv.append(top+Vector((.035*math.sin(5+idx),.028*math.sin(7+idx),.035+height*1.08)))
    for j in range(rings-1):
        for i in range(N): ff.append((j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i))
    for i in range(N): ff.append(((rings-1)*N+i,(rings-1)*N+(i+1)%N,rings*N))
    o=surface('torch-flame-%d'%idx,vv,ff,fm,'hand.L')
    basis=o.shape_key_add(name='Basis'); key=o.shape_key_add(name='Flicker')
    for v,b in zip(key.data,basis.data):
        t=max(0,(b.co.z-top.z)/height)
        v.co.x+=.027*math.sin(t*7+idx)*t; v.co.y+=.024*math.cos(t*6+idx)*t; v.co.z+=.035*t
    flames.append(key)

# Reusable walk action. 3D two-bone leg solution preserves exact limb lengths.
def desired(name,h,t):
    rest=AD.bones[name]
    delta=(rest.tail_local-rest.head_local).rotation_difference(Vector(t)-Vector(h))
    q=delta@rest.matrix_local.to_quaternion()
    A.pose.bones[name].matrix=Matrix.Translation(Vector(h))@q.to_matrix().to_4x4()
    bpy.context.view_layer.update()
def knee_for(hip,ankle,l1,l2):
    dvec=ankle-hip; d=dvec.length; e=dvec.normalized()
    d=min(d,l1+l2-.001)
    a=(l1*l1-l2*l2+d*d)/(2*d)
    height=math.sqrt(max(0,l1*l1-a*a))
    f=Vector((1,0,0)); bend=(f-e*f.dot(e)).normalized()
    return hip+e*a+bend*height
for frame in range(1,32,3):
    S.frame_set(frame); phase=2*math.pi*(frame-1)/30
    lift=.018*math.cos(2*phase); shift=Vector((0,.014*math.sin(phase),lift))
    desired('root',(0,0,0),(0,0,.25))
    desired('pelvis',Vector(rests['pelvis'][0])+shift,Vector(rests['pelvis'][1])+shift)
    desired('spine',Vector(rests['spine'][0])+shift,Vector(rests['spine'][1])+shift+Vector((.012*math.sin(phase),0,0)))
    desired('head',Vector(rests['head'][0])+shift,Vector(rests['head'][1])+shift)
    desired('cape',Vector(rests['cape'][0])+shift,Vector(rests['cape'][1])+shift+Vector((-.025*math.sin(phase+.7),.012*math.cos(phase),0)))
    for side,sg in [('L',1),('R',-1)]:
        p=phase+(0 if side=='L' else math.pi)
        hip=Vector((0,sg*.165,1.02))+shift
        ankle=Vector((.27*math.cos(p),sg*.165,.13+.10*max(0,-math.sin(p))))
        l1=(Vector(rests['thigh.'+side][1])-Vector(rests['thigh.'+side][0])).length
        l2=(Vector(rests['shin.'+side][1])-Vector(rests['shin.'+side][0])).length
        # Lower the pelvis enough for full strides; ankle positions are in-place.
        hip.z-=.075
        k=knee_for(hip,ankle,l1,l2)
        desired('thigh.'+side,hip,k); desired('shin.'+side,k,ankle)
        toe=ankle+Vector((.24,0,-.025+.025*max(0,-math.sin(p))))
        desired('foot.'+side,ankle,toe)
        shoulder=Vector(rests['upper_arm.'+side][0])+shift
        elbow=Vector(rests['upper_arm.'+side][1])+shift+Vector((-.035*math.cos(p),0,0))
        wrist=Vector(rests['forearm.'+side][1])+shift+Vector((-.04*math.cos(p),0,.025*math.sin(p)))
        desired('upper_arm.'+side,shoulder,elbow); desired('forearm.'+side,elbow,wrist)
        desired('hand.'+side,wrist,wrist+Vector((.11,sg*.02,.01)))
    for b in A.pose.bones:
        b.rotation_mode='QUATERNION'
        for prop in ['location','rotation_quaternion','scale']: b.keyframe_insert(prop,frame=frame,group=b.name)
    for idx,key in enumerate(flames):
        key.value=.5+.5*math.sin(phase*3+idx); key.keyframe_insert('value',frame=frame)
action=A.animation_data.action; action.name='Walk_3D_Right_30f'; action.use_fake_user=True
for act in bpy.data.actions:
    for fc in act.fcurves:
        for kp in fc.keyframe_points:
            kp.interpolation='BEZIER'; kp.handle_left_type='AUTO_CLAMPED'; kp.handle_right_type='AUTO_CLAMPED'
        fc.modifiers.new('CYCLES')

def camera(name,loc,target,scale):
    d=bpy.data.cameras.new(name); o=bpy.data.objects.new(name,d); S.collection.objects.link(o)
    o.location=loc; o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
    d.type='ORTHO'; d.ortho_scale=scale; return o
topcam=camera('CAM-game-topdown',(0.25,-1.4,7),(.25,0,1),2.7)
truecam=camera('CAM-vertical-topdown',(.25,0,7),(.25,0,0),2.7)
perscam=camera('CAM-perspective-proof',(3.8,-5.7,3.6),(.25,0,1),3.1)
def area(name,loc,target,energy,size,color):
    d=bpy.data.lights.new(name,'AREA'); d.energy=energy; d.shape='DISK'; d.size=size; d.color=color
    o=bpy.data.objects.new(name,d); S.collection.objects.link(o); o.location=loc
    o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
area('LGT-key',(-2,-3,5),(0,0,1),650,3,(1,.91,.78))
area('LGT-fill',(3,1,4),(0,0,1),230,2.5,(.73,.84,1))
area('LGT-rim',(-2,3,3),(0,0,1),480,2,(.74,.83,1))
ld=bpy.data.lights.new('LGT-torch','POINT'); ld.energy=12; ld.color=(1,.23,.025); ld.shadow_soft_size=.15
lo=bpy.data.objects.new('LGT-torch',ld); S.collection.objects.link(lo); lo.location=top+Vector((0,0,.12))
lo.parent=A; lo.parent_type='BONE'; lo.parent_bone='hand.L'
# Preserve world transform when assigning bone parent.
bpy.context.view_layer.update(); lo.matrix_parent_inverse=(A.matrix_world@A.pose.bones['hand.L'].matrix@Matrix.Translation((0,AD.bones['hand.L'].length,0))).inverted()

S.camera=topcam; S.frame_set(1); S.render.filepath=str(P/'frames'/'walk_')
S['asset_type']='Volumetric 3D knight; articulated solid meshes, 17-bone armature, no sprite plane'
S['equipment']='Sword: anatomical right; torch: anatomical left'
S['animation']='In-place 1 second walk, 30 fps, repeated action; limbs use 3D joint rotations'
for o in bpy.context.selected_objects: o.select_set(False)
A.select_set(True); bpy.context.view_layer.objects.active=A
for area_ in bpy.context.screen.areas:
    if area_.type=='VIEW_3D':
        area_.spaces.active.shading.type='MATERIAL'; area_.spaces.active.region_3d.view_perspective='CAMERA'
bpy.ops.wm.save_as_mainfile(filepath=str(P/'warrior_3d_walk.blend'))
stats={'mesh_objects':len(meshes),'vertices':sum(len(o.data.vertices) for o in meshes),'bones':len(AD.bones),'frames':30,'fps':30,'source':'approved sprite reference; rebuilt solid geometry','model':'stylized armored warrior','hands':{'sword':'R','torch':'L'},'cameras':[topcam.name,truecam.name,perscam.name]}
(P/'model_report.json').write_text(json.dumps(stats,indent=2)); print(json.dumps(stats))
if '--build-only' not in sys.argv:
    S.camera=perscam; S.render.filepath=str(P/'validation'/'perspective.png'); bpy.ops.render.render(write_still=True)
    S.camera=topcam; S.render.filepath=str(P/'validation'/'topdown.png'); bpy.ops.render.render(write_still=True)
    S.render.filepath=str(P/'frames'/'walk_'); bpy.ops.render.render(animation=True)
