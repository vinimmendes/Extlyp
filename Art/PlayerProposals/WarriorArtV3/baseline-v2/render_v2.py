import bpy,sys
from pathlib import Path
P=Path('/workspace/warrior-3d-v2'); S=bpy.context.scene
mode=sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else 'topdown'
def shot(cam,path,frame=1,size=640,samples=64):
    S.camera=bpy.data.objects[cam]; S.frame_set(frame)
    S.render.resolution_x=size; S.render.resolution_y=size
    S.cycles.samples=samples; S.render.filepath=str(path)
    bpy.ops.render.render(write_still=True)
if mode=='topdown':
    (P/'frames').mkdir(exist_ok=True)
    for f in range(1,37): shot('CAM-game-topdown',P/'frames'/('walk_%04d.png'%f),f,640,64)
elif mode=='perspective':
    (P/'perspective_frames').mkdir(exist_ok=True)
    for f in range(1,37,2): shot('CAM-perspective-proof',P/'perspective_frames'/('view_%04d.png'%f),f,512,48)
elif mode=='vertical':
    (P/'vertical_frames').mkdir(exist_ok=True)
    for f in range(1,37): shot('CAM-vertical-topdown',P/'vertical_frames'/('view_%04d.png'%f),f,384,24)
elif mode=='neutral':
    m=bpy.data.materials.new('MAT-neutral-review'); m.use_nodes=True
    b=m.node_tree.nodes.get('Principled BSDF'); b.inputs['Base Color'].default_value=(.25,.25,.25,1); b.inputs['Roughness'].default_value=.7
    S.view_layers[0].material_override=m
    for o in S.objects:
        if 'flame' in o.name: o.hide_render=True
    shot('CAM-game-topdown',P/'validation'/'neutral_topdown.png',1,384,24)
    shot('CAM-perspective-proof',P/'validation'/'neutral_perspective.png',1,384,24)
print('Completed render mode: '+mode)
