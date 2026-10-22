"""Author the rustic protagonist's separate sword upper-body action through Blender MCP.
Original scene, camera, materials and walking rig are retained in a new file.
"""
import bpy, math, json
from pathlib import Path
from mathutils import Vector, Matrix
from bpy_extras.object_utils import world_to_camera_view

ROOT=Path('C:/Users/Vinni/Documents/GitHub/Extlyp')
BASE=ROOT/'Prototypes/PlayerWalk3D'
OUT=BASE/'SwordAttack'
MODEL=OUT/'protagonista-ataque-espada.blend'
LEGS=('Coxa','Canela','Bota','Sola ','Cano bota','Cadarco','Remendo calca')

def sample(keys,f):
    for (a,x),(b,y) in zip(keys,keys[1:]):
        if f<=b:
            t=max(0,min(1,(f-a)/(b-a)));t=t*t*(3-2*t)
            return Vector(x).lerp(Vector(y),t)
    return Vector(keys[-1][1])

def build():
    if MODEL.exists():raise FileExistsError(MODEL)
    if Path(bpy.data.filepath).resolve()!=(BASE/'VisualV1/personagem-rustico.blend').resolve():
        raise RuntimeError('Expected the original rustic character')
    OUT.mkdir(parents=True,exist_ok=True)
    ns={'__file__':str(BASE/'export_game_character.py')}
    exec((BASE/'export_game_character.py').read_text(encoding='utf-8').split('variants=')[0],ns)
    sphere=ns['sphere'];s=bpy.context.scene;s.frame_set(9)
    original=list(s.objects)
    hidden=ns['gun_names']+ns['arm_names']+('Cano pistola','Cabo pistola')
    for o in original:
        if o.name.startswith(hidden+LEGS):o.hide_render=True
    # Rotate the existing torso and its clothing together around the waist.
    pivot=bpy.data.objects.new('CTRL-sword-torso',None);s.collection.objects.link(pivot)
    pivot.location=(0,0,1.25)
    bpy.context.view_layer.update()
    for o in original:
        if o.type in {'MESH','CURVE'} and not o.hide_render and o.parent is None:
            world=o.matrix_world.copy();o.parent=pivot;o.matrix_world=world
    cloth=bpy.data.materials['Jaqueta oliva - tecido gasto']
    skin=bpy.data.materials['Pele'];leather=bpy.data.materials['Couro marrom gasto']
    arms=[]
    for side in [-1,1]:
        upper=sphere('GEO-sword-sleeve', (0,0,0),(.112,.112,1),cloth)
        lower=sphere('GEO-sword-forearm',(0,0,0),(.088,.088,1),cloth)
        shoulder=sphere('GEO-sword-shoulder',(0,0,0),(.13,.13,.14),cloth)
        elbow=sphere('GEO-sword-elbow',(0,0,0),(.10,.10,.10),cloth)
        cuff=sphere('GEO-sword-cuff',(0,0,0),(.09,.09,.045),leather)
        hand=sphere('GEO-sword-hand',(0,0,0),(.085,.064,.073),skin)
        arms.append((side,upper,lower,shoulder,elbow,cuff,hand))
    grip=bpy.data.objects.new('CTRL-sword-grip',None);s.collection.objects.link(grip)
    tip=bpy.data.objects.new('CTRL-sword-direction',None);s.collection.objects.link(tip)
    s.render.fps=24;s.frame_start=1;s.frame_end=20
    s.camera=bpy.data.objects['Camera jogo - topo']
    s.render.resolution_x=s.render.resolution_y=256;s.render.resolution_percentage=100
    s.render.film_transparent=True;s.render.image_settings.file_format='PNG';s.render.image_settings.color_mode='RGBA'
    s.cycles.samples=16;s.cycles.use_denoising=True
    rows=[]
    for f in range(1,21):
        s.frame_set(f)
        twist=sample([(1,(0,0,0)),(8,(0,0,-.22)),(9,(0,0,.10)),(12,(0,0,.23)),(20,(0,0,0))],f).z
        pivot.rotation_euler.z=twist;pivot.keyframe_insert(data_path='rotation_euler',frame=f)
        rot=Matrix.Rotation(twist,4,'Z')
        for side,upper,lower,shoulder,elbow,cuff,hand in arms:
            a=Vector((.04,side*.30,1.55));a=rot@a
            if side==-1:
                b=sample([(1,(.24,-.42,1.39)),(8,(.04,-.49,1.45)),(9,(.45,-.24,1.43)),(12,(.36,.12,1.40)),(20,(.24,-.42,1.39))],f)
                c=sample([(1,(.49,-.30,1.42)),(8,(.23,-.59,1.49)),(9,(.70,-.07,1.44)),(12,(.52,.36,1.42)),(20,(.49,-.30,1.42))],f)
            else:
                b=sample([(1,(.00,.41,1.32)),(8,(-.04,.44,1.36)),(12,(-.10,.44,1.31)),(20,(.00,.41,1.32))],f)
                c=sample([(1,(.21,.33,1.24)),(8,(.08,.40,1.30)),(12,(.05,.49,1.27)),(20,(.21,.33,1.24))],f)
            for obj,start,end,radius in [(upper,a,b,.112),(lower,b,c,.088)]:
                obj.location=(start+end)/2;obj.scale=(radius,radius,(end-start).length/2+.035)
                obj.rotation_euler=(end-start).to_track_quat('Z','Y').to_euler()
            shoulder.location=a;elbow.location=b;cuff.location=c
            hand.location=c+Vector((.035,0,0))
            for obj in (upper,lower,shoulder,elbow,cuff,hand):
                for prop in ('location','rotation_euler','scale'):obj.keyframe_insert(data_path=prop,frame=f)
            if side==-1:grip.location=hand.location.copy()
        angle=sample([(1,(0,0,-22)),(8,(0,0,-78)),(9,(0,0,0)),(12,(0,0,62)),(20,(0,0,-22))],f).z
        tip.location=grip.location+Vector((math.cos(math.radians(angle)),math.sin(math.radians(angle)),0))
        grip.rotation_euler.z=math.radians(angle)
        for obj in (grip,tip):
            obj.keyframe_insert(data_path='location',frame=f);obj.keyframe_insert(data_path='rotation_euler',frame=f)
        bpy.context.view_layer.update()
        p=world_to_camera_view(s,s.camera,grip.location);q=world_to_camera_view(s,s.camera,tip.location)
        rows.append({'x':(p.x-.5)*3.4,'y':(p.y-.5)*3.4,'angle':math.degrees(math.atan2(q.y-p.y,q.x-p.x))})
    (OUT/'SwordGrip.json').write_text(json.dumps({'frames':rows},indent=2)+'\n',encoding='utf-8')
    for name,f in [('Guarda',1),('Preparacao',8),('Impacto',9),('Fim do corte',12),('Retorno',20)]:s.timeline_markers.new(name,frame=f)
    s.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(MODEL))
    return {'model':str(MODEL),'frames':20,'version':bpy.app.version_string}

def render_frames(first,last):
    if Path(bpy.data.filepath).resolve()!=MODEL.resolve():raise RuntimeError('Wrong source scene')
    folder=OUT/'Upper';folder.mkdir(exist_ok=True)
    s=bpy.context.scene
    for f in range(first,last+1):
        s.frame_set(f);s.render.filepath=str(folder/f'{f:03}.png');bpy.ops.render.render(write_still=True)
    return {'first':first,'last':last}
