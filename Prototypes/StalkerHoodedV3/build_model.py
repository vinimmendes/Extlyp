"""Original hooded biped V3. Blender 5.2; reproducible static art delivery."""
import bpy, math, random
from mathutils import Vector
from pathlib import Path
OUT=Path(__file__).resolve().parent
random.seed(13103)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene
scene.render.engine='CYCLES'; scene.cycles.samples=64; scene.cycles.use_denoising=False
scene.render.threads_mode='FIXED'; scene.render.threads=4
scene.render.resolution_x=1200; scene.render.resolution_y=1200; scene.render.resolution_percentage=100
scene.render.film_transparent=True; scene.render.image_settings.file_format='PNG'; scene.render.image_settings.color_mode='RGBA'
scene.render.use_compositing=False; scene.view_settings.view_transform='AgX'

def mat(name,color,variation=.25,scale=12,bump=.025):
    m=bpy.data.materials.new(name); m.use_nodes=True; m.diffuse_color=(*color,1)
    ns=m.node_tree.nodes; ns.clear(); links=m.node_tree.links
    out=ns.new('ShaderNodeOutputMaterial'); shader=ns.new('ShaderNodeBsdfDiffuse')
    noise=ns.new('ShaderNodeTexNoise'); ramp=ns.new('ShaderNodeValToRGB'); fine=ns.new('ShaderNodeTexNoise'); relief=ns.new('ShaderNodeBump')
    links.new(noise.outputs['Fac'],ramp.inputs['Fac']); links.new(ramp.outputs['Color'],shader.inputs['Color'])
    links.new(fine.outputs['Fac'],relief.inputs['Height']); links.new(relief.outputs['Normal'],shader.inputs['Normal']); links.new(shader.outputs[0],out.inputs['Surface'])
    noise.inputs['Scale'].default_value=scale; noise.inputs['Detail'].default_value=5
    fine.inputs['Scale'].default_value=145; fine.inputs['Detail'].default_value=3
    ramp.color_ramp.elements[0].position=.20; ramp.color_ramp.elements[1].position=.80
    ramp.color_ramp.elements[0].color=(*(c*(1-variation) for c in color),1)
    ramp.color_ramp.elements[1].color=(*(c*(1+variation) for c in color),1)
    relief.inputs['Strength'].default_value=.24; relief.inputs['Distance'].default_value=bump
    shader.inputs['Roughness'].default_value=.85
    return m
cloth=mat('Carvao tecido grosseiro',(.070,.063,.055),.5,8,.014)
weather=mat('Tecido marrom gasto',(.135,.100,.064),.45,10,.012)
edge=mat('Bordas e costuras gastas',(.21,.163,.104),.3,18,.008)
skin=mat('Pele palida e tendões',(.40,.32,.235),.26,20,.007)
claw=mat('Queratina envelhecida',(.26,.205,.13),.25,12,.004)
black=mat('Interior capuz',(.002,.002,.002),0,10,0)
thread=mat('Fibras tecido',(.18,.145,.10),.3,15,.002)

def mesh(name,vs,fs,material,sub=1,thickness=0):
    d=bpy.data.meshes.new(name); d.from_pydata(vs,[],fs); d.update()
    o=bpy.data.objects.new(name,d); scene.collection.objects.link(o); d.materials.append(material)
    for f in d.polygons:f.use_smooth=True
    if sub:
        m=o.modifiers.new('Forma suave editavel','SUBSURF'); m.levels=sub; m.render_levels=sub
    if thickness:
        m=o.modifiers.new('Espessura real','SOLIDIFY'); m.thickness=thickness; m.offset=-.5
    return o

def loft(name,rings,material,sides=24,wrinkle=.0,sub=2):
    vs=[]; fs=[]
    for i,(x,y,z,rx,ry) in enumerate(rings):
        for j in range(sides):
            a=math.tau*j/sides; ripple=1+wrinkle*math.sin(a*7+i*.8)
            vs.append((x+rx*math.cos(a)*ripple,y+ry*math.sin(a)*ripple,z))
    for i in range(len(rings)-1):
        for j in range(sides):
            a=i*sides+j;b=i*sides+(j+1)%sides;fs.append((a,b,b+sides,a+sides))
    fs.extend([tuple(reversed(range(sides))),tuple((len(rings)-1)*sides+j for j in range(sides))])
    return mesh(name,vs,fs,material,sub)

def tube(name,points,radii,material,sides=12,sub=1):
    # Interpolate the centerline rather than shrink joint/end geometry with subdivision.
    raw=[Vector(p) for p in points]; ps=[]; smoothr=[]
    for k in range(len(raw)-1):
        a=raw[max(0,k-1)];b=raw[k];c=raw[k+1];d=raw[min(len(raw)-1,k+2)]
        for q in range(6):
            t=q/6
            ps.append(.5*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t))
            smoothr.append(radii[k]*(1-t)+radii[k+1]*t)
    ps.append(raw[-1]);smoothr.append(radii[-1]);radii=smoothr
    sides=max(20,sides);vs=[];fs=[]
    for i,p in enumerate(ps):
        tangent=(ps[min(i+1,len(ps)-1)]-ps[max(0,i-1)]).normalized()
        u=tangent.cross(Vector((0,1,0)))
        if u.length<.01:u=tangent.cross(Vector((0,0,1)))
        u.normalize();v=tangent.cross(u).normalized()
        for j in range(sides):
            a=math.tau*j/sides;vs.append(p+radii[i]*(u*math.cos(a)+v*math.sin(a)))
    for i in range(len(ps)-1):
        for j in range(sides):
            a=i*sides+j;b=i*sides+(j+1)%sides;fs.append((a,b,b+sides,a+sides))
    fs.extend([tuple(reversed(range(sides))),tuple((len(ps)-1)*sides+j for j in range(sides))])
    return mesh(name,vs,fs,material,0)

def cord(name,ps,radius,material):
    d=bpy.data.curves.new(name,'CURVE');d.dimensions='3D';d.bevel_depth=radius;d.bevel_resolution=2
    sp=d.splines.new('POLY');sp.points.add(len(ps)-1)
    for p,co in zip(sp.points,ps):p.co=(*co,1)
    o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);d.materials.append(material);return o

def organic_joint(name,p,radius,material):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=10,location=p)
    o=bpy.context.object;o.name=name;o.scale=(radius,radius*.9,radius*1.1);o.data.materials.append(material)
    for f in o.data.polygons:f.use_smooth=True
    return o

# Spine bends forward from narrow pelvis, rounded back returns into dropped neck.
loft('Corpo sob roupa',[(-.27,0,.94,.13,.15),(-.29,0,1.15,.18,.20),(-.32,0,1.38,.14,.18),(-.34,0,1.58,.21,.24),(-.27,0,1.82,.29,.30),(-.12,0,2.00,.27,.35),(.07,0,2.05,.20,.28),(.18,0,1.98,.13,.19)],cloth,32,.025)
for side in [-1,1]:
    sy=side*.22; advance=.08 if side==-1 else -.08
    loft('Calca anatomica '+str(side),[(-.25,sy,1.15,.135,.14),(-.17,sy,1.00,.145,.125),(.10+advance,sy,.72,.105,.11),(.11+advance,sy,.63,.10,.10),(-.05+advance,sy,.39,.085,.085),(-.11+advance,sy,.16,.052,.065)],cloth,24,.075)
    # Long, narrow bare feet with heel, bridge and five toes.
    loft('Pe anatomico '+str(side),[(-.08+advance,sy,.05,.11,.085),(.00+advance,sy,.08,.19,.10),(-.04+advance,sy,.15,.12,.075),(-.10+advance,sy,.23,.055,.064)],skin,24,0,2)
    for k in range(5):
        yy=sy+(k-2)*.037;ln=.09-.009*abs(k-1)
        tube('Dedo pe',[(.08+advance,yy,.09),(.16+advance,yy,.065),(.16+advance+ln,yy,.05)],[.025,.023,.014],skin)
        tube('Unha pe',[(.16+advance+ln-.018,yy,.065),(.19+advance+ln,yy,.048),(.20+advance+ln,yy,.023)],[.015,.009,.001],claw)
    shoulder=(.00,side*.33,1.98+(0.045 if side==-1 else -.04))
    elbow=(-.12,side*.56,1.43);wrist=(.24,side*.68,.94)
    ps=[shoulder,(-.06,side*.40,1.82),(-.10,side*.48,1.63),elbow,(.00,side*.62,1.22),(.15,side*.66,1.03),wrist]
    tube('Manga dobrada '+str(side),ps,[.15,.135,.12,.103,.098,.076,.062],cloth,20,2)
    # Local compression folds around elbow and cuff, each a small curved ridge.
    for q in range(7):
        z=1.38+q*.044;x=-.10-q*.012
        cord('Dobra manga',[(x-.045,side*.53,z),(x-.06,side*.61,z-.025),(x+.045,side*.62,z-.04)],.0035,cloth)
    cord('Borda punho',[(.18,side*.61,.98),(.24,side*.68,1.0),(.28,side*.73,.98)],.017,edge)
    # Palm tapers from wrist to spread knuckles. Fingers have continuous curved paths.
    loft('Palma definida '+str(side),[(.24,side*.68,1.015,.043,.052),(.26,side*.69,.96,.055,.064),(.29,side*.70,.88,.065,.078),(.32,side*.72,.80,.058,.090),(.33,side*.73,.755,.039,.069)],skin,24,0,2)
    for digit in range(4):
        yy=side*.72+(digit-1.5)*.049;length=[.24,.29,.275,.225][digit];splay=(digit-1.5)*.021
        ps=[(.33,yy,.80),(.36,yy+splay*.4,.73),(.41,yy+splay,.80-length*.57),(.44,yy+splay*1.5,.80-length)]
        tube('Dedo articulado '+str(side)+' '+str(digit),ps,[.024,.025,.020,.014],skin,12,2)
        organic_joint('Articulacao mao '+str(side)+' '+str(digit),ps[1],.028,skin)
        base=Vector(ps[-1]);tip=base+Vector((.025,splay*.3,-.17))
        tube('Garra curva '+str(side)+' '+str(digit),[base,base+Vector((.052,0,-.046)),base+Vector((.060,splay*.15,-.105)),tip],[.017,.014,.008,.0008],claw,12,2)
        cord('Tendao mao',[(.25,yy,.97),(.30,yy,.88),(.337,yy,.80)],.007,skin)
    tube('Polegar curvo '+str(side),[(.28,side*.62,.90),(.37,side*.58,.85),(.45,side*.57,.76),(.48,side*.59,.68)],[.032,.029,.024,.013],skin,12,2)
    tube('Garra polegar',[(.48,side*.59,.68),(.51,side*.60,.62),(.48,side*.60,.56)],[.016,.009,.001],claw,12,2)
    # Fuse flesh volumes so palm and fingers are one continuous organic mesh.
    pieces=[o for o in scene.objects if o.type=='MESH' and (o.name=='Palma definida '+str(side) or o.name.startswith('Dedo articulado '+str(side)+' ') or o.name.startswith('Articulacao mao '+str(side)+' ') or o.name=='Polegar curvo '+str(side))]
    bpy.ops.object.select_all(action='DESELECT')
    for o in pieces:
        bpy.context.view_layer.objects.active=o;o.select_set(True)
        for modifier in list(o.modifiers):bpy.ops.object.modifier_apply(modifier=modifier.name)
    bpy.context.view_layer.objects.active=pieces[0];bpy.ops.object.join()
    hand=bpy.context.object;hand.name='Mao organica fundida '+str(side)
    remesh=hand.modifiers.new('Uniao anatomica','REMESH');remesh.mode='VOXEL';remesh.voxel_size=.006;remesh.use_smooth_shade=True
    bpy.ops.object.modifier_apply(modifier=remesh.name)
    smooth=hand.modifiers.new('Suavizar transicoes','SMOOTH');smooth.factor=.6;smooth.iterations=3
    for k in range(3):
        yy=side*.72+(k-1)*.047
        cord('Veia dorsal',[(.303,yy,.945),(.344,yy+.012,.872),(.376,yy,.79)],.004,skin)

# Hood: faceted pointed crown, drooping triangular mouth, genuinely open rim.
vs=[];fs=[];N=64;R=18
for i in range(R):
    t=i/(R-1);cx=-.13+.68*t;cz=2.06-.05*t
    for j in range(N):
        a=j*math.tau/N;sn=math.sin(a);co=math.cos(a)
        width=(.025+.245*math.sin(t*math.pi/2))*(1-.12*max(0,sn))
        height=.025+.28*math.sin(t*math.pi/2)
        peak=.20*max(0,sn)**5*math.sin(t*math.pi*.9)
        sag=.12*max(0,-sn)**3*t**3
        fold=.025*math.sin(a*9+t*8)*math.sin(t*math.pi/2)
        xx=cx-.13*max(0,sn)*t**4+.07*max(0,-sn)*t**4
        vs.append((xx,width*co+fold*co,cz+height*sn+peak-sag))
for i in range(R-1):
    for j in range(N):
        a=i*N+j;b=i*N+(j+1)%N;fs.append((a,b,b+N,a+N))
fs.append(tuple(reversed(range(N))))
mesh('Capuz ponta dobrada',vs,fs,cloth,1,.025)
rim=vs[-N:];cord('Borda capuz desgastada',rim+[rim[0]],.011,weather)
# Recessed black curved occlusion closes only the rear of the cavity.
loft('Rosto oculto',[(.30,0,1.78,.11,.12),(.36,0,1.95,.13,.20),(.36,0,2.17,.12,.19),(.25,0,2.27,.08,.10)],black,32,0,2)

def panel(name,start,end,z0,length,material,seed,r0=.32,shoulder=False):
    rng=random.Random(seed);cols=14;rows=17;vs=[];fs=[]
    hem=[rng.uniform(-.08,.12) for _ in range(cols+1)]
    for i in range(rows+1):
        t=i/rows
        for j in range(cols+1):
            u=j/cols;a=start+(end-start)*u
            radius=r0+.055*math.sin(t*math.pi)+.035*t
            if shoulder:radius=.16+.245*math.sin(min(1,t*2)*math.pi/2)-.045*t
            folds=.018*math.sin(u*math.pi*8+t*.9)+.011*math.sin(u*29-t*2)
            radius+=folds
            x=(-.10-.12*t if shoulder else -.17-.18*t)+radius*math.cos(a);y=radius*math.sin(a)
            z=z0-length*t+(hem[j]*t**7)+(.03*math.sin(a*3) if shoulder else 0)
            vs.append((x,y,z))
    for i in range(rows):
        for j in range(cols):
            # Actual irregular holes near worn lower hem, not only shader dots.
            if i>rows-4 and (j+seed)%9==0 and i%3==0:continue
            q=i*(cols+1)+j;fs.append((q,q+1,q+cols+2,q+cols+1))
    o=mesh(name,vs,fs,material,1,.009)
    for j in [0,cols]:
        cord('Costura '+name,[vs[i*(cols+1)+j] for i in range(rows+1)],.0035,edge)
    for j in range(0,cols+1,2):
        p=Vector(vs[-cols-1+j]);cord('Fiapo bainha',[p,p+Vector((rng.uniform(-.025,.025),rng.uniform(-.02,.02),-.04-rng.random()*.045))],.0015,thread)
    return o

# Fitted open coat, different lengths and overlaps; silhouette stays bipedal.
for j,(a,b,L) in enumerate([(.48,1.35,1.20),(1.24,2.20,1.35),(2.04,3.05,1.45),(2.94,3.98,1.38),(3.82,4.87,1.27),(4.73,5.78,1.17)]):
    panel('Painel casaco '+str(j),a,b,1.96,L,cloth,100+j)
# Shoulder cape follows body curvature, overlays descend into torn strips.
for j in range(5):
    a=.55+j*1.03
    panel('Sobreposicao ombro '+str(j),a,a+1.16,2.13,.51+random.random()*.27,weather if j==2 else cloth,210+j,.365,True)
for j in range(7):
    a=.85+j*.7
    panel('Faixa pendente '+str(j),a,a+.19,1.60,.73+random.random()*.32,weather,310+j,.373)
# Neckline covered by a folded, frayed collar with no visible open cylinder.
loft('Gola tecido enrugado',[(.05,0,1.98,.17,.23),(.00,0,2.02,.21,.26),(-.03,0,2.09,.18,.24),(.03,0,2.12,.13,.18)],cloth,40,.06,2)
# Continuous dorsal shoulder cloth covers the inner rim of the hanging panels.
vs=[];fs=[];N=24
for i in range(N+1):
    x=-.49+.64*i/N
    for j in range(N+1):
        y=-.33+.66*j/N
        z=2.10-.16*((x+.14)/.36)**2-.14*(y/.34)**2+.014*math.sin(j*.95+i*.22)
        vs.append((x,y,z))
for i in range(N):
    for j in range(N):
        q=i*(N+1)+j;fs.append((q,q+N+1,q+N+2,q+1))
mesh('Manto dorsal continuo',vs,fs,cloth,1,.014)
# Front lapels following chest, with thick folded edges rather than flat strips.
for side in [-1,1]:
    tube('Lapela dobrada '+str(side),[(.12,side*.17,1.94),(.16,side*.14,1.74),(.03,side*.17,1.52),(-.01,side*.19,1.31)],[.065,.060,.048,.018],weather,12,2)
    cord('Costura lapela',[(.18,side*.18,1.94),(.20,side*.16,1.73),(.07,side*.19,1.52)],.004,edge)

# Sparse hand stitching and repair patches: localized wear, not uniform noise.
for side in [-1,1]:
    for k in range(9):
        z=1.63-k*.038; y=side*(.285+.014*math.sin(k))
        cord('Ponto costura',[(.055,y,z),(.063,y+side*.022,z-.012)],.0025,thread)

def cam(name,loc,target,scale):
    d=bpy.data.cameras.new(name);d.type='ORTHO';d.ortho_scale=scale
    o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=loc
    o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler();return o
hero=cam('Tres quartos',(4.6,-7,3.1),(0,0,1.30),3.15)
top=cam('Superior frente +X',(0,0,7),(0,0,0),2.55);top.rotation_euler=(0,0,0)
sidecam=cam('Perfil postura',(0,-7,1.45),(0,0,1.3),3.15)
for name,loc,power,size,col in [('Principal',(3,-4,5),520,3,(1,.86,.69)),('Preenchimento',(2,3,3),260,3,(.68,.77,1)),('Contorno',(-3,1,4),650,2,(.87,.92,1))]:
    d=bpy.data.lights.new(name,'AREA');d.energy=power;d.size=size;d.color=col
    o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,1.3))-o.location).to_track_quat('-Z','Y').to_euler()
world=bpy.data.worlds.new('Ambiente');world.use_nodes=True;scene.world=world
bg=next(n for n in world.node_tree.nodes if n.type=='BACKGROUND');bg.inputs['Color'].default_value=(.12,.15,.19,1);bg.inputs['Strength'].default_value=.12
scene.camera=hero
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'espreitador-encapuzado-v3.blend'))
for camera,name in [(hero,'tres-quartos-v3.png'),(top,'superior-v3.png'),(sidecam,'perfil-v3.png')]:
    scene.camera=camera;scene.render.filepath=str(OUT/name);bpy.ops.render.render(write_still=True)
print('V3 static source and three alpha renders saved.')
