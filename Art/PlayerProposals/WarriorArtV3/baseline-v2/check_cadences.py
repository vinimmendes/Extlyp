import bpy,json
from pathlib import Path
P=Path('/workspace/warrior-3d-v2'); S=bpy.context.scene; A=bpy.data.objects['ARM-warrior-3D']
src=(P/'switch_cadence.py').read_text(); results=[]
for mode,n in [('heavy',36),('alert',30)]:
    exec(src.replace("CADENCE='heavy'", "CADENCE='"+mode+"'"))
    S.frame_set(1); bpy.context.view_layer.update(); start={b.name:b.matrix.copy() for b in A.pose.bones}
    fire=[o.data.shape_keys.key_blocks['Flicker'].value for o in bpy.data.objects if o.name.startswith('GEO-torch-flame-')]
    S.frame_set(n+1); bpy.context.view_layer.update()
    err=max(abs(start[b.name][i][j]-b.matrix[i][j]) for b in A.pose.bones for i in range(4) for j in range(4))
    fire_end=[o.data.shape_keys.key_blocks['Flicker'].value for o in bpy.data.objects if o.name.startswith('GEO-torch-flame-')]
    ferr=max(abs(a-b) for a,b in zip(fire,fire_end))
    results.append({'cadence':mode,'frames':n,'loop_matrix_error':err,'fire_loop_error':ferr,'passed':err<.0001 and ferr<.0001})
(P/'validation'/'cadences.json').write_text(json.dumps(results,indent=2)); print(json.dumps(results))
