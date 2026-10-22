"""Render key poses before full animation export."""
import bpy
from pathlib import Path
BASE=Path(__file__).resolve().parent/'Animation'
bpy.ops.wm.open_mainfile(filepath=str(BASE/'espreitador-v3-animado.blend'))
scene=bpy.context.scene;rig=bpy.data.objects['RIG_Espreitador_V3']
scene.render.resolution_x=512;scene.render.resolution_y=512;scene.cycles.samples=16
dest=BASE/'Review';dest.mkdir(exist_ok=True)
for action,frames in [('Walk',[1,9,17]),('ClawAttack',[1,7,12,20])]:
    rig.animation_data.action=bpy.data.actions[action]
    for f in frames:
        scene.frame_set(f);bpy.context.view_layer.update()
        print(action,f,'knees',tuple(rig.pose.bones['Shin.R'].head),tuple(rig.pose.bones['Shin.L'].head),'wrist',tuple(rig.pose.bones['Hand.R'].head))
        for camera,label in [('Tres quartos','Perspective'),('Superior frente +X','Top')]:
            scene.camera=bpy.data.objects[camera];scene.render.filepath=str(dest/f'{action}-{f:02}-{label}.png');bpy.ops.render.render(write_still=True)
