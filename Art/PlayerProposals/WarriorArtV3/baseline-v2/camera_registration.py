import bpy,json
from pathlib import Path
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view
P=Path('/workspace/warrior-3d-v2'); S=bpy.context.scene
S.frame_set(1); bpy.context.view_layer.update(); A=bpy.data.objects['ARM-warrior-3D']
report={}
for name,size in [('CAM-game-topdown',640),('CAM-vertical-topdown',384),('CAM-perspective-proof',512)]:
    cam=bpy.data.objects[name]
    def pixel(v):
        p=world_to_camera_view(S,cam,Vector(v)); return [round(p.x*size,3),round((1-p.y)*size,3)]
    report[name]={'size':[size,size],'root_pivot':pixel((0,0,0)),'landmarks':{n:pixel(A.matrix_world@A.pose.bones[n].head) for n in ['head','upper_arm.L','upper_arm.R','hand.L','hand.R','foot.L','foot.R']},'projection':'ORTHO'}
(P/'validation'/'camera_registration.json').write_text(json.dumps(report,indent=2)); print(json.dumps(report))
