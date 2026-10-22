import bpy,json
from pathlib import Path
from mathutils.bvhtree import BVHTree
P=Path('/workspace/warrior-3d-v2'); S=bpy.context.scene
def tree(obj,deps,limit=None):
    eo=obj.evaluated_get(deps); me=eo.to_mesh()
    verts=[eo.matrix_world@v.co for v in me.vertices]
    faces=[list(p.vertices) for p in me.polygons if limit is None or sum(verts[i].z for i in p.vertices)/len(p.vertices)<limit]
    result=BVHTree.FromPolygons(verts,faces,all_triangles=False,epsilon=.0002)
    eo.to_mesh_clear(); return result
hits=[]
for f in range(1,37):
    S.frame_set(f); bpy.context.view_layer.update(); deps=bpy.context.evaluated_depsgraph_get()
    limbs=[(n,tree(bpy.data.objects[n],deps)) for n in ['GEO-thigh-plate-L','GEO-thigh-plate-R','GEO-mail-thigh-L','GEO-mail-thigh-R','GEO-greave-L','GEO-greave-R']]
    for name,limit in [('GEO-short-red-cape',1.12),('GEO-red-tabard',1.04)]:
        cloth=tree(bpy.data.objects[name],deps,limit)
        for n,t in limbs:
            count=len(cloth.overlap(t))
            if count: hits.append({'frame':f,'cloth':name,'limb':n,'triangle_pairs':count})
report={'frames':list(range(1,37)),'method':'evaluated world-space BVH triangle overlap; cloth roots excluded','passed':len(hits)==0,'hits':hits}
(P/'validation'/'cloth_collision.json').write_text(json.dumps(report,indent=2)); print(json.dumps(report))
