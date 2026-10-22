"""Stage 8: lighting and frame-one approval renders. No cycle rendering."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector,Matrix

ROOT=Path('C:/Users/Vinni/Documents/GitHub/Extlyp/Art/PlayerProposals/WarriorArtV3')
SOURCE=ROOT/'working-v3/warrior_v3-step07-flame.blend'
DEST=ROOT/'working-v3/warrior_v3-step08-lighting.blend'
IMAGES=ROOT/'stage08-approval'

def setup():
    if Path(bpy.data.filepath).resolve()!=SOURCE.resolve():raise RuntimeError('Expected stage 7 file.')
    if DEST.exists():raise FileExistsError(DEST)
    IMAGES.mkdir(exist_ok=True)
    s=bpy.context.scene
    for name,loc,power,size,color in [
        ('LGT-key',(-3,-4,6),470,4,(1,.92,.81)),
        ('LGT-fill',(3,-1,4),130,4,(.78,.86,1)),
        ('LGT-rim',(-2,3,4),170,3,(.82,.88,1))]:
        o=bpy.data.objects[name];o.location=loc
        o.rotation_euler=(Vector((0,0,1.1))-o.location).to_track_quat('-Z','Y').to_euler()
        o.data.energy=power;o.data.size=size;o.data.color=color
    # Existing world retained, with only background strength adjusted.
    if s.world and s.world.use_nodes:
        for n in s.world.node_tree.nodes:
            if n.type=='BACKGROUND':n.inputs['Strength'].default_value=.24
    a=bpy.data.objects['ARM-warrior-3D'];bone=a.pose.bones['hand.L']
    restpos=Vector((.46,.535,1.30))+Vector((.35,.12,.46)).normalized()*.43+Vector((0,-.025,.085))
    currentpos=a.matrix_world@bone.matrix@a.data.bones['hand.L'].matrix_local.inverted()@restpos
    data=bpy.data.lights.new('LGT-torch-warm-step08','POINT')
    data.energy=42;data.color=(1,.36,.075);data.shadow_soft_size=.11
    o=bpy.data.objects.new(data.name,data);s.collection.objects.link(o)
    o.parent=a;o.parent_type='BONE';o.parent_bone='hand.L'
    bpy.context.view_layer.update();o.matrix_world=Matrix.Translation(currentpos)
    s.view_settings.view_transform='Standard';s.view_settings.look='None';s.view_settings.exposure=-.35
    s.render.engine='CYCLES';s.cycles.samples=128;s.cycles.use_denoising=True
    s.render.resolution_x=640;s.render.resolution_y=640;s.render.resolution_percentage=100
    s.render.film_transparent=True;s.render.image_settings.file_format='PNG';s.render.image_settings.color_mode='RGBA'
    s.render.filepath=str(IMAGES/'warrior-v3-frame01.png')
    s['art_stage_08_done']=True
    bpy.ops.wm.save_as_mainfile(filepath=str(DEST))
    return {'file':str(DEST),'frame':s.frame_current,'camera':s.camera.name,'torch_light_watts':42,'resolution':[640,640],'samples':128}

def render_v3():
    s=bpy.context.scene
    if Path(bpy.data.filepath).resolve()!=DEST.resolve():raise RuntimeError('Expected stage 8 file.')
    if s.frame_current!=1:raise RuntimeError('Expected frame 1; no animation frame changes requested.')
    bpy.ops.render.render(write_still=True)
    return {'image':s.render.filepath,'bytes':Path(s.render.filepath).stat().st_size}

def render_v2():
    # Append source scene without opening or running any scripts, and without
    # modifying the archived v2. Its own original lighting/color are preserved.
    if Path(bpy.data.filepath).resolve()!=DEST.resolve():raise RuntimeError('Expected stage 8 file.')
    active=bpy.context.scene
    path=str(ROOT/'baseline-v2/warrior_v2.blend')
    with bpy.data.libraries.load(path,link=False) as (src,dst):dst.scenes=list(src.scenes)
    baseline=dst.scenes[0]
    bpy.context.window.scene=baseline
    baseline.render.resolution_x=640;baseline.render.resolution_y=640;baseline.render.resolution_percentage=100
    baseline.cycles.samples=128;baseline.cycles.use_denoising=True
    baseline.render.image_settings.file_format='PNG';baseline.render.image_settings.color_mode='RGBA'
    baseline.render.filepath=str(IMAGES/'warrior-v2-frame01-same-blender.png')
    try:
        if baseline.frame_current!=1:raise RuntimeError('Baseline must already be at frame 1.')
        bpy.ops.render.render(write_still=True)
        info={'image':baseline.render.filepath,'bytes':Path(baseline.render.filepath).stat().st_size,'camera':baseline.camera.name,'frame':baseline.frame_current,'color_transform':baseline.view_settings.view_transform,'exposure':baseline.view_settings.exposure}
    finally:
        bpy.context.window.scene=active
        # Appended comparison scene is temporary and never saved into stage 8.
        bpy.data.scenes.remove(baseline)
    return info
