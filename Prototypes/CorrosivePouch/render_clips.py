"""Render animation frames from the saved editable model without rebuilding it."""
import bpy
from pathlib import Path
BASE=Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(BASE/'corrosivo-papo-inchado.blend'))
scene=bpy.context.scene; scene.camera=bpy.data.objects['Camera jogo - topo']
for action,frames in [('Walk',range(1,33)),('Spit',range(101,146))]:
    (BASE/action).mkdir(exist_ok=True)
    for f in frames:
        scene.frame_set(f); scene.render.filepath=str(BASE/action/f'{f:03}.png')
        bpy.ops.render.render(write_still=True)
print('CORROSIVE CLIPS RENDERED',flush=True)
