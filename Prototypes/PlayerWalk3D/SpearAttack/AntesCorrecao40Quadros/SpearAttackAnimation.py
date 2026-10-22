"""Two-handed spear thrust and refined weapon, based on the preserved rustic hero."""
import bpy, math, json, bmesh
from pathlib import Path
from mathutils import Vector, Matrix
from bpy_extras.object_utils import world_to_camera_view

ROOT=Path('C:/Users/Vinni/Documents/GitHub/Extlyp')
BASE=ROOT/'Prototypes/PlayerWalk3D'
OUT=BASE/'SpearAttack'
MODEL=OUT/'protagonista-estocada-lanca.blend'
PREFIX='GEO-refined-spear-'
RATIO=3.4/2.6500000953674316
WORLD_SIZE=5.1
LEGS=('Coxa','Canela','Bota','Sola ','Cano bota','Cadarco','Remendo calca')

def mesh(name,vertices,faces,materials,indices=None):
    data=bpy.data.meshes.new(PREFIX+name);data.from_pydata(vertices,[],faces);data.update()
    bm=bmesh.new();bm.from_mesh(data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.000001)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(data);bm.free()
    obj=bpy.data.objects.new(PREFIX+name,data);bpy.context.scene.collection.objects.link(obj)
    for material in materials:data.materials.append(material)
    if indices:
        for poly,index in zip(data.polygons,indices):poly.material_index=index
    bevel=obj.modifiers.new('Soft forged edges','BEVEL');bevel.width=.001;bevel.segments=2
    obj.modifiers.new('Weighted normals','WEIGHTED_NORMAL');obj.parent=bpy.data.objects['CTRL-spear-rear-grip']
    return obj

def tube(name,rings,material):
    vertices=[];faces=[];sides=20
    for x,ry,rz in rings:
        for j in range(sides):
            a=j*2*math.pi/sides;vertices.append((x,ry*math.cos(a),rz*math.sin(a)))
    faces.append(tuple(reversed(range(sides))))
    for i in range(len(rings)-1):
        for j in range(sides):
            a=i*sides+j;b=i*sides+(j+1)%sides;faces.append((a,b,b+sides,a+sides))
    faces.append(tuple((len(rings)-1)*sides+j for j in range(sides)))
    obj=mesh(name,vertices,faces,[material])
    for face in obj.data.polygons:face.use_smooth=len(face.vertices)==4
    return obj

def weapon(material):
    wood=material('MAT-spear-aged-wood',(.22,.09,.028),.68,0,True)
    leather=material('MAT-spear-dark-binding',(.025,.011,.006),.76,0)
    iron=material('MAT-spear-dark-steel',(.075,.085,.09),.46,.88)
    edge=material('MAT-spear-sharpened-edge',(.52,.56,.54),.29,.76)
    # Right grip is 0.60 game units from the butt, left grip 1.00 unit from it.
    # At impact right grip is at x=0.55, placing the tip at x=2.20 game units.
    butt=-.60/RATIO;tip=1.65/RATIO;neck=tip-.36
    tube('wooden-shaft',[(butt,.023,.021),(butt+.025,.027,.024),(-.10,.027,.024),
        (.35,.026,.023),(neck-.06,.023,.022),(neck+.015,.023,.022)],wood)
    tube('iron-butt-cap',[(butt-.004,.024,.022),(butt+.008,.029,.026),(butt+.043,.029,.026)],iron)
    tube('head-socket',[(neck-.052,.030,.028),(neck+.025,.030,.028),(neck+.047,.025,.023)],iron)
    # Continuous leather binding around the socket, away from both gripping hands.
    vertices=[];faces=[];steps=96
    for j in range(steps+1):
        t=j/steps;a=2*math.pi*3.3*t;x=neck-.16+.12*t
        for delta in [-.021,.021]:vertices.append((x+delta,.028*math.cos(a),.025*math.sin(a)))
    for j in range(steps):faces.append((2*j,2*j+1,2*j+3,2*j+2))
    binding=mesh('socket-binding',vertices,faces,[leather]);binding.modifiers.new('Leather thickness','SOLIDIFY').thickness=.0015
    # Broad leaf outline and central ridge; the two edge strips are honed steel.
    stations=[(neck,.019),(neck+.045,.042),(neck+.115,.077),(neck+.185,.069),(neck+.26,.038),(tip,.0008)]
    vertices=[];faces=[];indices=[]
    for sign in [1,-1]:
        for x,width in stations:
            ridge=min(.033,.004+width*.39)
            for y,z in [(-width,.002),(-width*.77,ridge*.52),(0,ridge),(width*.77,ridge*.52),(width,.002)]:
                vertices.append((x,y,z*sign))
    n=len(stations);offset=n*5
    for i in range(n-1):
        for j in range(4):
            a=i*5+j;faces.extend([(a,a+1,a+6,a+5),(offset+a,offset+a+5,offset+a+6,offset+a+1)])
            indices.extend([1 if j in (0,3) else 0]*2)
        for j in [0,4]:
            a=i*5+j;faces.append((a,a+5,offset+a+5,offset+a));indices.append(1)
    faces.extend([tuple(list(range(5))+list(reversed(range(offset,offset+5)))),
        tuple(list(range((n-1)*5,n*5))+list(reversed(range(offset+(n-1)*5,offset+n*5))))]);indices.extend([0,1])
    mesh('leaf-blade-with-ridge',vertices,faces,[iron,edge],indices)

def build(revision=False):
    if MODEL.exists() and not revision:raise FileExistsError(MODEL)
    if Path(bpy.data.filepath).resolve()!=(BASE/'VisualV1/personagem-rustico.blend').resolve():
        raise RuntimeError('Expected original rustic character')
    OUT.mkdir(exist_ok=True)
    helpers={'__file__':str(BASE/'export_game_character.py')}
    exec((BASE/'export_game_character.py').read_text(encoding='utf-8').split('variants=')[0],helpers)
    timing={};exec((ROOT/'Tools/Blender/SwordAttackAnimation.py').read_text(encoding='utf-8'),timing)
    materials={};exec((ROOT/'Tools/Blender/RefineAxeModel.py').read_text(encoding='utf-8'),materials)
    sample=timing['sample'];sphere=helpers['sphere'];s=bpy.context.scene;s.frame_set(9)
    original=list(s.objects)
    for o in original:
        if o.name.startswith(helpers['gun_names']+helpers['arm_names']+('Cano pistola','Cabo pistola')+LEGS):o.hide_render=True
    torso=bpy.data.objects.new('CTRL-spear-torso',None);s.collection.objects.link(torso);torso.location=(0,0,1.25)
    bpy.context.view_layer.update()
    for o in original:
        if o.type in {'MESH','CURVE'} and not o.hide_render and o.parent is None:
            world=o.matrix_world.copy();o.parent=torso;o.matrix_world=world
    cloth=bpy.data.materials['Jaqueta oliva - tecido gasto'];skin=bpy.data.materials['Pele']
    leather=bpy.data.materials['Couro marrom gasto'];arms=[]
    for side in [-1,1]:
        parts=[sphere('GEO-spear-'+name,(0,0,0),scale,mat) for name,scale,mat in [
            ('sleeve',(.112,.112,1),cloth),('forearm',(.088,.088,1),cloth),('shoulder',(.13,.13,.14),cloth),
            ('elbow',(.10,.10,.10),cloth),('cuff',(.09,.09,.045),leather),('hand',(.075,.070,.070),skin)]]
        arms.append((side,*parts))
    rear=bpy.data.objects.new('CTRL-spear-rear-grip',None);front=bpy.data.objects.new('CTRL-spear-front-grip',None)
    s.collection.objects.link(rear);s.collection.objects.link(front)
    weapon(materials['material'])
    s.render.fps=24;s.frame_start=1;s.frame_end=20;s.camera=bpy.data.objects['Camera jogo - topo']
    s.render.resolution_x=s.render.resolution_y=256;s.render.resolution_percentage=100
    s.render.film_transparent=True;s.render.image_settings.file_format='PNG';s.render.image_settings.color_mode='RGBA'
    s.cycles.samples=32;s.cycles.use_denoising=True;rows=[]
    for f in range(1,21):
        s.frame_set(f)
        twist=sample([(1,(0,0,0)),(8,(0,0,-.20)),(9,(0,0,.10)),(12,(0,0,.12)),(20,(0,0,0))],f).z
        torso.rotation_euler.z=twist;torso.keyframe_insert(data_path='rotation_euler',frame=f)
        p=sample([(1,(.34,-.23,1.44)),(8,(.08,-.42,1.47)),(9,(.55/RATIO,0,1.44)),
            (12,(.55/RATIO,0,1.44)),(16,(.30,-.18,1.44)),(20,(.34,-.23,1.44))],f)
        tilt=math.radians(sample([(1,(8,0,0)),(8,(5,0,0)),(9,(0,0,0)),(12,(0,0,0)),(20,(8,0,0))],f).x)
        axis=Vector((math.cos(tilt),0,math.sin(tilt)));q=p+axis*(.4/RATIO)
        rear.location=p;rear.rotation_euler=axis.to_track_quat('X','Z').to_euler();front.location=q
        for obj in (rear,front):
            for prop in ('location','rotation_euler'):obj.keyframe_insert(data_path=prop,frame=f)
        for side,upper,lower,shoulder,elbow,cuff,hand in arms:
            a=Matrix.Rotation(twist,4,'Z')@Vector((.04,side*.30,1.55));c=p if side==-1 else q
            b=(a+c)/2+Vector((-.06,side*.17,-.10))
            for obj,start,end,radius in [(upper,a,b,.112),(lower,b,c,.088)]:
                obj.location=(start+end)/2;obj.scale=(radius,radius,(end-start).length/2+.035)
                obj.rotation_euler=(end-start).to_track_quat('Z','Y').to_euler()
            shoulder.location=a;elbow.location=b;cuff.location=c;cuff.rotation_euler=(c-b).to_track_quat('Z','Y').to_euler()
            hand.location=c;hand.rotation_euler=axis.to_track_quat('X','Z').to_euler()
            for obj in (upper,lower,shoulder,elbow,cuff,hand):
                for prop in ('location','rotation_euler','scale'):obj.keyframe_insert(data_path=prop,frame=f)
        bpy.context.view_layer.update()
        rp=world_to_camera_view(s,s.camera,p);fp=world_to_camera_view(s,s.camera,q)
        tip=world_to_camera_view(s,s.camera,p+axis*(1.65/RATIO))
        rows.append({'x':(rp.x-.5)*3.4,'y':(rp.y-.5)*3.4,'angle':0,'scaleX':math.cos(tilt),'scaleY':1,
            'leftX':(fp.x-.5)*3.4,'leftY':(fp.y-.5)*3.4,'tipX':(tip.x-.5)*3.4,'tipY':(tip.y-.5)*3.4})
    (OUT/'SpearGrip.json').write_text(json.dumps({'frames':rows},indent=2)+'\n',encoding='utf-8')
    for name,f in [('Guarda',1),('Recuo',8),('Impacto',9),('Fim extensao',12),('Retorno',20)]:s.timeline_markers.new(name,frame=f)
    reference=bpy.data.images.load(str(ROOT/'Assets/Resources/Items/IconsV2/Spear.png'),check_existing=True);reference.pack()
    s.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(MODEL))
    return {'model':str(MODEL),'frames':20,'impactTip':rows[8],'version':bpy.app.version_string}

def render_frames(kind,first,last):
    s=bpy.context.scene
    if Path(bpy.data.filepath).resolve()!=MODEL.resolve():raise RuntimeError('Wrong scene')
    if kind not in ('Upper','Weapon'):raise ValueError(kind)
    folder=OUT/kind;folder.mkdir(exist_ok=True);state=[];ortho=s.camera.data.ortho_scale;frame=s.frame_current
    try:
        for obj in s.objects:
            if obj.type in {'MESH','CURVE'}:
                state.append((obj,obj.visible_camera));isWeapon=obj.name.startswith(PREFIX)
                obj.visible_camera=isWeapon if kind=='Weapon' else not isWeapon
        s.camera.data.ortho_scale=WORLD_SIZE/RATIO if kind=='Weapon' else 3.4/RATIO
        s.render.resolution_x=s.render.resolution_y=256;s.render.resolution_percentage=100
        for f in range(first,last+1):
            s.frame_set(f);s.render.filepath=str(folder/f'{f:03}.png');bpy.ops.render.render(write_still=True)
    finally:
        s.camera.data.ortho_scale=ortho;s.frame_set(frame)
        for obj,visible in state:obj.visible_camera=visible
    return {'kind':kind,'first':first,'last':last}

def render_isolated(hero=False):
    s=bpy.context.scene;oldcam=s.camera;state=[];copies=[]
    for obj in list(s.objects):
        if obj.type in {'MESH','CURVE'}:
            state.append((obj,obj.hide_render));obj.hide_render=True
            if obj.name.startswith(PREFIX):
                copy=obj.copy();copy.animation_data_clear();copy.parent=None;copy.matrix_world=obj.matrix_basis.copy()
                copy.hide_render=False;copy.visible_camera=True;s.collection.objects.link(copy);copies.append(copy)
    data=bpy.data.cameras.new('CAM-spear-isolated');camera=bpy.data.objects.new(data.name,data);s.collection.objects.link(camera)
    try:
        target=Vector((.40,0,0));camera.location=(.90,-1.0,2.6) if hero else (.40,-.35,3)
        camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
        data.type='ORTHO';data.ortho_scale=2.15;s.camera=camera
        s.render.resolution_x=s.render.resolution_y=1024 if hero else 512;s.render.resolution_percentage=100
        s.render.filepath=str(OUT/('lanca-detalhe.png' if hero else 'lanca-chao.png'));bpy.context.view_layer.update()
        bpy.ops.render.render(write_still=True);return {'path':s.render.filepath}
    finally:
        s.camera=oldcam
        for obj,hidden in state:obj.hide_render=hidden
        for obj in copies:bpy.data.objects.remove(obj,do_unlink=True)
        bpy.data.objects.remove(camera,do_unlink=True);bpy.data.cameras.remove(data)
