"""Two-handed diagonal axe action, authored on a separate copy of the rustic hero."""
import bpy, math, json
from pathlib import Path
from mathutils import Vector, Matrix
from bpy_extras.object_utils import world_to_camera_view

ROOT=Path('C:/Users/Vinni/Documents/GitHub/Extlyp')
BASE=ROOT/'Prototypes/PlayerWalk3D'
OUT=BASE/'AxeAttack'
MODEL=OUT/'protagonista-ataque-machado.blend'
LEGS=('Coxa','Canela','Bota','Sola ','Cano bota','Cadarco','Remendo calca')

def build(revision=False):
    if MODEL.exists() and not revision:raise FileExistsError(MODEL)
    if Path(bpy.data.filepath).resolve()!=(BASE/'VisualV1/personagem-rustico.blend').resolve():
        raise RuntimeError('Expected original rustic character')
    OUT.mkdir(exist_ok=True)
    helpers={'__file__':str(BASE/'export_game_character.py')}
    exec((BASE/'export_game_character.py').read_text(encoding='utf-8').split('variants=')[0],helpers)
    timing={};exec((ROOT/'Tools/Blender/SwordAttackAnimation.py').read_text(encoding='utf-8'),timing)
    sample=timing['sample'];sphere=helpers['sphere'];box=helpers['box']
    s=bpy.context.scene;s.frame_set(9)
    original=list(s.objects)
    for o in original:
        if o.name.startswith(helpers['gun_names']+helpers['arm_names']+('Cano pistola','Cabo pistola')+LEGS):o.hide_render=True
    pivot=bpy.data.objects.new('CTRL-axe-torso',None);s.collection.objects.link(pivot)
    pivot.location=(0,0,1.25);bpy.context.view_layer.update()
    for o in original:
        if o.type in {'MESH','CURVE'} and not o.hide_render and o.parent is None:
            world=o.matrix_world.copy();o.parent=pivot;o.matrix_world=world
    cloth=bpy.data.materials['Jaqueta oliva - tecido gasto'];skin=bpy.data.materials['Pele']
    leather=bpy.data.materials['Couro marrom gasto']
    arms=[]
    for side in [-1,1]:
        parts=[sphere('GEO-axe-'+name,(0,0,0),scale,material) for name,scale,material in [
            ('sleeve',(.112,.112,1),cloth),('forearm',(.088,.088,1),cloth),
            ('shoulder',(.13,.13,.14),cloth),('elbow',(.10,.10,.10),cloth),
            ('cuff',(.09,.09,.045),leather),('hand',(.075,.070,.070),skin)]]
        arms.append((side,*parts))
    grip=bpy.data.objects.new('CTRL-axe-right-grip',None);s.collection.objects.link(grip)
    support=bpy.data.objects.new('CTRL-axe-left-grip',None);s.collection.objects.link(support)
    # A viewport-only proxy matches the existing generated sprite's shaft and head.
    # The shipped weapon remains a separate SpriteRenderer.
    unit=1.55/(3.4/s.camera.data.ortho_scale)/128
    guide=box('GEO-axe-shaft-guide',(10*unit,0,0),(92*unit,8*unit,.035),leather)
    head=box('GEO-axe-head-guide',(44*unit,0,0),(16*unit,53*unit,.04),bpy.data.materials['Aco escurecido'])
    for o in (guide,head):o.parent=grip;o.hide_render=True
    s.render.fps=24;s.frame_start=1;s.frame_end=26
    s.camera=bpy.data.objects['Camera jogo - topo']
    s.render.resolution_x=s.render.resolution_y=256;s.render.resolution_percentage=100
    s.render.film_transparent=True;s.render.image_settings.file_format='PNG';s.render.image_settings.color_mode='RGBA'
    s.cycles.samples=16;s.cycles.use_denoising=True
    rows=[]
    for f in range(1,27):
        s.frame_set(f)
        twist=sample([(1,(0,0,0)),(8,(0,0,-.50)),(10,(0,0,-.18)),(11,(0,0,.12)),(14,(0,0,.35)),(26,(0,0,0))],f).z
        pivot.rotation_euler.z=twist;pivot.keyframe_insert(data_path='rotation_euler',frame=f)
        primary=sample([(1,(.64,-.28,1.47)),(8,(.12,-.50,1.77)),
            (9,(.25,-.55,1.65)),(10,(.53,-.38,1.46)),
            (11,(.75,.12,1.26)),(14,(.58,.45,1.05)),(19,(.62,.32,1.22)),(26,(.64,-.28,1.47))],f)
        angles=sample([(1,(-20,8,0)),(8,(-145,22,0)),(9,(-105,10,0)),(10,(-50,-5,0)),(11,(15,-15,0)),
            (14,(65,-35,0)),(19,(36,-12,0)),(26,(-20,8,0))],f)
        yaw,tilt=map(math.radians,(angles.x,angles.y))
        axis=Vector((math.cos(yaw)*math.cos(tilt),math.sin(yaw)*math.cos(tilt),math.sin(tilt)))
        secondary=primary-axis*(28*unit)
        grip.location=primary;grip.rotation_euler=axis.to_track_quat('X','Z').to_euler()
        support.location=secondary
        for obj in (grip,support):
            for prop in ('location','rotation_euler'):obj.keyframe_insert(data_path=prop,frame=f)
        for side,upper,lower,shoulder,elbow,cuff,hand in arms:
            a=Matrix.Rotation(twist,4,'Z')@Vector((.04,side*.30,1.55))
            c=primary if side==-1 else secondary
            b=(a+c)/2+Vector((-.09,side*.18,-.10))
            for obj,start,end,radius in [(upper,a,b,.112),(lower,b,c,.088)]:
                obj.location=(start+end)/2;obj.scale=(radius,radius,(end-start).length/2+.035)
                obj.rotation_euler=(end-start).to_track_quat('Z','Y').to_euler()
            shoulder.location=a;elbow.location=b;cuff.location=c
            cuff.rotation_euler=(c-b).to_track_quat('Z','Y').to_euler()
            hand.location=c;hand.rotation_euler=axis.to_track_quat('X','Z').to_euler()
            for obj in (upper,lower,shoulder,elbow,cuff,hand):
                for prop in ('location','rotation_euler','scale'):obj.keyframe_insert(data_path=prop,frame=f)
        bpy.context.view_layer.update()
        p=world_to_camera_view(s,s.camera,primary);q=world_to_camera_view(s,s.camera,primary+axis)
        left=world_to_camera_view(s,s.camera,secondary)
        rows.append({'x':(p.x-.5)*3.4,'y':(p.y-.5)*3.4,
            'angle':math.degrees(math.atan2(q.y-p.y,q.x-p.x)),
            'scaleX':math.hypot(q.x-p.x,q.y-p.y)*s.camera.data.ortho_scale,'scaleY':1,
            'leftX':(left.x-.5)*3.4,'leftY':(left.y-.5)*3.4})
    (OUT/'AxeGrip.json').write_text(json.dumps({'frames':rows},indent=2)+'\n',encoding='utf-8')
    for name,f in [('Guarda',1),('Preparacao alta',10),('Impacto',11),('Fim diagonal',14),('Retorno',26)]:s.timeline_markers.new(name,frame=f)
    s.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(MODEL))
    return {'model':str(MODEL),'frames':26,'version':bpy.app.version_string}

def render_frames(first,last):
    if Path(bpy.data.filepath).resolve()!=MODEL.resolve():raise RuntimeError('Wrong scene')
    folder=OUT/'Upper';folder.mkdir(exist_ok=True)
    for f in range(first,last+1):
        bpy.context.scene.frame_set(f);bpy.context.scene.render.filepath=str(folder/f'{f:03}.png')
        bpy.ops.render.render(write_still=True)
    return {'first':first,'last':last}
