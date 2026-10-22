"""Non-destructive hand-crossbow animation for the existing rustic sprite character.
Run build() through Blender MCP, then render_clip(name,start,end) in small batches.
"""
import bpy, math
from pathlib import Path
from mathutils import Vector

ROOT=Path('C:/Users/Vinni/Documents/GitHub/Extlyp')
BASE=ROOT/'Prototypes/PlayerWalk3D'
OUT=BASE/'HandCrossbow'
MODEL=OUT/'personagem-besta-de-mao.blend'
LEGS=('Coxa','Canela','Bota','Sola ','Cano bota','Cadarco','Remendo calca')

def key(o):
    for p in ('location','rotation_euler','scale'): o.keyframe_insert(data_path=p)

def sample(keys,f):
    for (a,x),(b,y) in zip(keys,keys[1:]):
        if f<=b:
            t=max(0,min(1,(f-a)/(b-a)));t=t*t*(3-2*t)
            return Vector(x).lerp(Vector(y),t)
    return Vector(keys[-1][1])

def build():
    if MODEL.exists(): raise FileExistsError(MODEL)
    if Path(bpy.data.filepath).resolve()!=(BASE/'VisualV1/personagem-rustico.blend').resolve():
        raise RuntimeError('Open the existing rustic character first.')
    OUT.mkdir(parents=True,exist_ok=True)
    ns={'__file__':str(BASE/'export_game_character.py')}
    exec((BASE/'export_game_character.py').read_text(encoding='utf-8').split('variants=')[0],ns)
    box,sphere,sleeve=ns['box'],ns['sphere'],ns['sleeve']
    s=bpy.context.scene;s.frame_set(9)
    for o in s.objects:
        if o.name.startswith(ns['gun_names']+ns['arm_names']):
            o.hide_render=True;o.hide_set(True)
    steel=bpy.data.materials['Aco escurecido'];leather=bpy.data.materials['Couro marrom gasto']
    cloth=bpy.data.materials['Jaqueta oliva - tecido gasto'];skin=bpy.data.materials['Pele']
    wood=bpy.data.materials['Mochila lona caqui'].copy();wood.name='MAT-crossbow-wood'
    ramp=next(n for n in wood.node_tree.nodes if n.type=='VALTORGB')
    ramp.color_ramp.elements[0].color=(.045,.018,.006,1)
    ramp.color_ramp.elements[1].color=(.24,.105,.035,1)
    cord=bpy.data.materials['Linha de costura'].copy();cord.name='MAT-crossbow-cord'
    ctrl=bpy.data.objects.new('CTRL-crossbow',None);s.collection.objects.link(ctrl)
    ctrl.location=(.64,-.19,1.43)
    def parent(o):o.parent=ctrl;return o
    parent(box('GEO-crossbow-stock',(.13,0,0),(.46,.075,.072),wood))
    parent(box('GEO-crossbow-grip',(-.04,0,-.09),(.075,.075,.19),leather))
    for side in (-1,1):
        parent(box('GEO-crossbow-rail'+str(side),(.14,side*.025,.045),(.43,.012,.02),steel))
        pts=[(.32,0,.015),(.30,side*.11,.015),(.26,side*.24,.015),(.20,side*.36,.015)]
        for i,(a,b) in enumerate(zip(pts,pts[1:])):
            o=parent(box('GEO-crossbow-limb'+str(side)+'-'+str(i),(Vector(a)+Vector(b))/2,((Vector(b)-Vector(a)).length+.02,.042,.028),steel))
            o.rotation_euler.z=math.atan2(b[1]-a[1],b[0]-a[0])
        parent(sphere('GEO-crossbow-tip'+str(side),pts[-1],(.025,.025,.022),leather))
    parent(box('GEO-crossbow-mount',(.32,0,.02),(.065,.12,.065),steel))
    parent(box('GEO-crossbow-catch',(-.065,0,.054),(.045,.042,.03),steel))
    # One curve with three points: equal string halves in the rail's top plane.
    curve=bpy.data.curves.new('GEO-crossbow-string','CURVE');curve.dimensions='3D';curve.bevel_depth=.006;curve.bevel_resolution=2
    poly=curve.splines.new('POLY');poly.points.add(2)
    string=bpy.data.objects.new(curve.name,curve);s.collection.objects.link(string);parent(string);curve.materials.append(cord)
    bolt=parent(box('GEO-crossbow-bolt',(.105,0,.067),(.32,.012,.012),wood))
    arms=[]
    for side in (-1,1):
        shoulder=Vector((.055,side*.30,1.56))
        sphere('GEO-crossbow-shoulder'+str(side),shoulder,(.14,.14,.15),cloth)
        upper=sleeve('GEO-crossbow-upper'+str(side),shoulder,(.28,side*.36,1.3),.115,cloth)
        lower=sleeve('GEO-crossbow-forearm'+str(side),(.28,side*.36,1.3),(.60,-.19,1.38),.09,cloth)
        joint=sphere('GEO-crossbow-elbow'+str(side),shoulder,(.11,.11,.10),cloth)
        hand=sphere('GEO-crossbow-hand'+str(side),shoulder,(.085,.065,.065),skin)
        cuff=sphere('GEO-crossbow-cuff'+str(side),shoulder,(.09,.09,.035),leather)
        arms.append((side,shoulder,upper,lower,joint,hand,cuff))
    s.render.fps=24;s.frame_start=1;s.frame_end=104
    for f in range(1,105):
        s.frame_set(f);reload=41<=f<=89;fire=f>=97;t=f-40
        ctrl.location=(.64,-.19,1.43);ctrl.rotation_euler=(0,0,0)
        catch=-.065
        if reload:
            ctrl.location=sample([(1,(.64,-.19,1.43)),(9,(.53,-.12,1.37)),(38,(.53,-.12,1.37)),(49,(.64,-.19,1.43))],t)
            catch=.20 if t<7 else .20+(-.265)*min(1,max(0,(t-7)/13))
        if fire:
            kick=[0,.012,.018,.013,.008,.004,.001,0][f-97]
            ctrl.location.x-=kick;ctrl.rotation_euler.y=-kick*1.2
            catch=-.065 if f==97 else .20
        key(ctrl)
        for point,co in zip(poly.points,[(.20,-.36,.061),(catch,0,.061),(.20,.36,.061)]):
            point.co=(*co,1);point.keyframe_insert(data_path='co')
        bolt.hide_render=(reload and t<34) or (fire and f>97)
        bolt.keyframe_insert(data_path='hide_render')
        bpy.context.view_layer.update()
        right=ctrl.matrix_world@Vector((-.04,-.01,-.075))
        left=Vector((.08,.38,1.12))
        if reload:
            pull=ctrl.matrix_world@Vector((catch,.02,.08))
            ready=ctrl.matrix_world@Vector((.12,.025,.10))
            belt=Vector((.03,.40,1.12))
            left=sample([(1,left),(7,ctrl.matrix_world@Vector((.20,.02,.08))),(20,pull),(24,belt),(29,belt),(35,ready),(39,ready),(49,left)],t)
        for side,shoulder,upper,lower,joint,hand,cuff in arms:
            wrist=right if side==-1 else left
            elbow=shoulder.lerp(wrist,.52)+Vector((-.02,side*.16,-.13))
            for o,a,b,r in [(upper,shoulder,elbow,.115),(lower,elbow,wrist,.09)]:
                o.location=(a+b)/2;o.scale=(r,r,(b-a).length/2+.03)
                o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();key(o)
            joint.location=elbow;key(joint)
            hand.location=wrist;hand.rotation_euler=lower.rotation_euler;key(hand)
            cuff.location=elbow.lerp(wrist,.93);cuff.rotation_euler=lower.rotation_euler;key(cuff)
    for datablock in list(bpy.data.objects)+[curve]:
        action=datablock.animation_data.action if datablock.animation_data else None
        if not action:continue
        curves=list(action.fcurves) if hasattr(action,'fcurves') else [fc for l in action.layers for st in l.strips if hasattr(st,'channelbags') for cb in st.channelbags for fc in cb.fcurves]
        for fc in curves:
            for p in fc.keyframe_points:p.interpolation='CONSTANT' if fc.data_path=='hide_render' else 'LINEAR'
    s.timeline_markers.clear()
    for name,f in [('Walk',1),('ReloadLower',41),('DrawString',47),('StringLatched',60),('ReachBolt',64),('SeatBolt',75),('AimReady',89),('Shot',97),('Release',98),('Recovery',104)]:s.timeline_markers.new(name,frame=f)
    s.camera=bpy.data.objects['Camera jogo - topo'];s.render.resolution_x=s.render.resolution_y=256
    s.render.resolution_percentage=100;s.render.film_transparent=True;s.render.image_settings.file_format='PNG';s.render.image_settings.color_mode='RGBA'
    s.cycles.samples=16;s.cycles.use_denoising=True;s.frame_set(9)
    bpy.ops.wm.save_as_mainfile(filepath=str(MODEL))
    return {'model':str(MODEL),'walk':32,'reload':49,'shot':8,'version':bpy.app.version_string}

def render_clip(name,start,end):
    s=bpy.context.scene
    if Path(bpy.data.filepath).resolve()!=MODEL.resolve():raise RuntimeError('Wrong animation file')
    folder=OUT/name;folder.mkdir(exist_ok=True)
    upper=name not in ('Walk','EmptyWalk')
    for o in s.objects:
        if o.type in {'MESH','CURVE'} and o.name.startswith(LEGS):o.hide_render=upper
    muted=[]
    if name=='EmptyWalk':
        for block in (bpy.data.objects['GEO-crossbow-bolt'],bpy.data.curves['GEO-crossbow-string']):
            a=block.animation_data.action
            curves=list(a.fcurves) if hasattr(a,'fcurves') else [fc for l in a.layers for st in l.strips if hasattr(st,'channelbags') for cb in st.channelbags for fc in cb.fcurves]
            for fc in curves:muted.append((fc,fc.mute));fc.mute=True
        bpy.data.objects['GEO-crossbow-bolt'].hide_render=True
        bpy.data.curves['GEO-crossbow-string'].splines[0].points[1].co=(.20,0,.061,1)
    for f in range(start,end+1):
        s.frame_set(f);s.render.filepath=str(folder/f'{f:03}.png');bpy.ops.render.render(write_still=True)
    for fc,previous in muted:fc.mute=previous
    s.frame_set(9)
    return {'clip':name,'first':start,'last':end,'folder':str(folder)}
