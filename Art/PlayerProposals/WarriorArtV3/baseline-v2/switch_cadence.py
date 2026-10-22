"""Run in Blender Text Editor. Set CADENCE to 'heavy' or 'alert'."""
import bpy
CADENCE='heavy'
is_heavy=CADENCE=='heavy'
arm=bpy.data.objects['ARM-warrior-3D']
arm.animation_data.action=bpy.data.actions['Walk_Heavy_36f_IK' if is_heavy else 'Walk_Alert_30f_IK']
for i in range(3):
    keys=bpy.data.objects['GEO-torch-flame-'+str(i)].data.shape_keys
    keys.animation_data.action=bpy.data.actions['Fire_%d_%s'%(i,'Heavy_36f' if is_heavy else 'Alert_30f')]
bpy.context.scene.frame_end=36 if is_heavy else 30
bpy.context.scene['walk_speed_m_s']=.46/(.60*(bpy.context.scene.frame_end/30))
bpy.context.scene.frame_set(1)
print('Cadence: '+CADENCE)
