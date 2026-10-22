import bpy
from pathlib import Path
P=Path('/workspace/warrior-3d-v2'); A=bpy.data.objects['ARM-warrior-3D']
col=A.data.collections.get('Controles') or A.data.collections.new('Controles')
deform=A.data.collections.get('Deformacao') or A.data.collections.new('Deformacao')
for b in A.data.bones:
    (col if b.name.startswith('CTRL') or b.name in ['root','pelvis'] else deform).assign(b)
    b.color.palette='THEME04' if b.name.startswith('CTRL') else 'THEME03'
def widget(name,verts,edges):
    me=bpy.data.meshes.new(name); me.from_pydata(verts,edges,[]); me.update()
    o=bpy.data.objects.new(name,me); bpy.context.scene.collection.objects.link(o)
    o.hide_render=True; o.hide_set(True); return o
w=widget('WGT-foot-control',[(-.12,-.08,0),(.12,-.08,0),(.12,.30,0),(-.12,.30,0)],[(0,1),(1,2),(2,3),(3,0)])
k=widget('WGT-knee-control',[(0,-.07,0),(.07,0,0),(0,.07,0),(-.07,0,0)],[(0,1),(1,2),(2,3),(3,0)])
for side in ['L','R']:
    p=A.pose.bones['CTRL-foot.'+side]; p.custom_shape=w; p.use_custom_shape_bone_size=False
    p=A.pose.bones['CTRL-knee.'+side]; p.custom_shape=k; p.use_custom_shape_bone_size=False
for i in range(3):
    keys=bpy.data.objects['GEO-torch-flame-'+str(i)].data.shape_keys
    act=keys.animation_data.action; act.name='Fire_%d_Heavy_36f'%i; act.use_fake_user=True
    alt=act.copy(); alt.name='Fire_%d_Alert_30f'%i; alt.use_fake_user=True
    for fc in alt.fcurves:
        for kp in fc.keyframe_points:
            for v in [kp.co,kp.handle_left,kp.handle_right]: v.x=1+(v.x-1)*30/36
bpy.context.scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(P/'warrior_v2.blend'))
print('Editable foot and knee widgets, bone collections, and paired cadence actions saved.')
