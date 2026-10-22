"""Render existing animated source without rebuilding or changing static V3."""
import bpy,sys
from pathlib import Path
BASE=Path(__file__).resolve().parent/'Animation'
bpy.ops.wm.open_mainfile(filepath=str(BASE/'espreitador-v3-animado.blend'))
scene=bpy.context.scene;rig=bpy.data.objects['RIG_Espreitador_V3']
scene.render.resolution_x=384;scene.render.resolution_y=384;scene.cycles.samples=16
scene.cycles.use_denoising=False
for action,count in [('Walk',32),('ClawAttack',20)]:
    rig.animation_data.action=bpy.data.actions[action]
    for view,camera in [('Top','Superior frente +X'),('Perspective','Tres quartos')]:
        dest=BASE/view/action;dest.mkdir(parents=True,exist_ok=True);scene.camera=bpy.data.objects[camera];scene.camera.data.ortho_scale=3.20
        for f in range(1,count+1):
            scene.frame_set(f);scene.render.filepath=str(dest/f'{f:03}.png');bpy.ops.render.render(write_still=True)
print('Delivered 104 PNGs across two actions and two views.')
