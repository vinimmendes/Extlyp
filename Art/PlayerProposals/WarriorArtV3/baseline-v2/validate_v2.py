import bpy, math, json
from pathlib import Path
from mathutils import Vector
P=Path('/workspace/warrior-3d-v2'); S=bpy.context.scene; A=bpy.data.objects['ARM-warrior-3D']
N=36; fps=30; duration=N/fps; speed=S['walk_speed_m_s']
rows=[]; errors=[]; snapshots={}
for f in range(1,N+2):
    S.frame_set(f); bpy.context.view_layer.update()
    deps=bpy.context.evaluated_depsgraph_get(); ea=A.evaluated_get(deps)
    matrices={b.name:b.matrix.copy() for b in ea.pose.bones}
    if f in [1,2,N,N+1]: snapshots[f]=matrices
    for side in ['L','R']:
        thigh=ea.pose.bones['thigh.'+side]; shin=ea.pose.bones['shin.'+side]; foot=ea.pose.bones['foot.'+side]; ctrl=ea.pose.bones['CTRL-foot.'+side]
        ikerr=(shin.tail-ctrl.head).length
        jointgap=max((thigh.tail-shin.head).length,(shin.tail-foot.head).length)
        grounds=[]
        for name in ['GEO-boot-sole-'+side,'GEO-boot-'+side]:
            o=bpy.data.objects[name].evaluated_get(deps); me=o.to_mesh()
            grounds.extend((o.matrix_world@v.co).z for v in me.vertices); o.to_mesh_clear()
        u=((f-1)/N+(0 if side=='L' else .5))%1
        # A toe pivot remains fixed during roll in the support interval.
        rest=Vector((.27,0,.017))
        rest.y=.165 if side=='L' else -.165
        deform=foot.matrix@A.data.bones['foot.'+side].matrix_local.inverted()
        contact=deform@rest
        rows.append({'frame':f,'side':side,'phase':u,'support':u<.60,'ik_error':ikerr,'joint_gap':jointgap,'sole_min_z':min(grounds),'world_contact_x':contact.x+speed*(f-1)/fps,'contact_z':contact.z})
        errors.append(ikerr)
seam=max(abs(snapshots[1][b][i][j]-snapshots[N+1][b][i][j]) for b in snapshots[1] for i in range(4) for j in range(4))
slips=[]
for side in ['L','R']:
    samples=[r for r in rows if r['side']==side]
    for a,b in zip(samples,samples[1:]):
        if a['support'] and b['support'] and b['phase']>a['phase']:
            slips.append(abs(b['world_contact_x']-a['world_contact_x']))
vjump=0; vbone=''
for name in snapshots[1]:
    a=(snapshots[2][name].translation-snapshots[1][name].translation)*fps
    b=(snapshots[N+1][name].translation-snapshots[N][name].translation)*fps
    if (a-b).length>vjump: vjump=(a-b).length; vbone=name
report={'bones':len(A.data.bones),'live_ik_constraints':sum(c.type=='IK' for b in A.pose.bones for c in b.constraints),'max_ik_target_error_m':max(errors),'max_joint_gap_m':max(r['joint_gap'] for r in rows),'min_sole_height_m':min(r['sole_min_z'] for r in rows),'max_support_contact_drift_per_frame_m':max(slips),'loop_pose_matrix_error':seam,'max_translation_velocity_seam_difference_m_s':vjump,'velocity_seam_bone':vbone,'frames':N,'fps':fps,'root_motion_test_speed_m_s':speed,'samples':rows}
def heads_at(frame):
    base=math.floor(frame); S.frame_set(base,subframe=frame-base); bpy.context.view_layer.update()
    return {b.name:b.head.copy() for b in A.evaluated_get(bpy.context.evaluated_depsgraph_get()).pose.bones}
eps=.05
left=heads_at(N+1-eps); right=heads_at(1+eps); center=heads_at(1)
fine=max((((right[n]-center[n])-(center[n]-left[n]))*(fps/eps)).length for n in center)
report['velocity_limit_probe_frame_step']=eps
report['max_velocity_seam_difference_fine_m_s']=fine
report['passed']=max(errors)<.003 and max(r['joint_gap'] for r in rows)<.0001 and min(r['sole_min_z'] for r in rows)>-.006 and max(slips)<.003 and seam<.0001 and fine<.08
(P/'validation'/'rig_and_contact.json').write_text(json.dumps(report,indent=2))
print(json.dumps({k:v for k,v in report.items() if k!='samples'},indent=2))
