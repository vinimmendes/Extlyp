import bpy
from pathlib import Path
BASE=Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(BASE/'RifleReload/protagonista-recarga-rifle.blend'))
scene=bpy.context.scene
for frame in [29,38,44,47,51]:
    scene.frame_set(frame); bpy.context.view_layer.update()
    print('POSE',frame,[(i,bpy.data.objects['Municao '+str(i)].hide_render,tuple(bpy.data.objects['Municao '+str(i)].matrix_world.translation)) for i in range(1,6)],flush=True)
scene.frame_set(38)
for i in range(2,6):
    for prefix in ['Municao ','Ponta municao ']: bpy.data.objects[prefix+str(i)].hide_render=True
bpy.context.view_layer.update()
print('OVERRIDE',[(i,bpy.data.objects['Municao '+str(i)].hide_render) for i in range(1,6)],flush=True)
scene.render.resolution_x=256; scene.render.resolution_y=256; scene.cycles.samples=8
scene.render.filepath=str(BASE/'PendingRuntimeExport/diagnostic-visibility.png'); bpy.ops.render.render(write_still=True)
print('AFTER RENDER',[(i,bpy.data.objects['Municao '+str(i)].hide_render) for i in range(1,6)],flush=True)
