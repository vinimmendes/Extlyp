"""Stage 5: cloth folds and leather construction; retain cloth topology/weights."""
import bpy, bmesh, math, json
from pathlib import Path
from mathutils import Vector

ROOT=Path('C:/Users/Vinni/Documents/GitHub/Extlyp/Art/PlayerProposals/WarriorArtV3')
SOURCE=ROOT/'working-v3/warrior_v3-step04-equipment.blend'
DEST=ROOT/'working-v3/warrior_v3-step05-cloth-leather.blend'
CHANGED=[];ADDED=[]

def tube(name,points,radius,source,weights=None,material=None):
    """Build a small construction seam with inherited deformation per sample."""
    if bpy.data.objects.get(name):raise RuntimeError('Already exists: '+name)
    n=6;vs=[];fs=[]
    for i,p in enumerate(points):
        p=Vector(p)
        t=Vector(points[min(i+1,len(points)-1)])-Vector(points[max(0,i-1)])
        t.normalize();axis=Vector((0,0,1))
        if abs(t.dot(axis))>.92:axis=Vector((0,1,0))
        u=t.cross(axis).normalized();v=t.cross(u).normalized()
        for j in range(n):
            a=math.tau*j/n;vs.append(p+radius*(u*math.cos(a)+v*math.sin(a)))
    for i in range(len(points)-1):
        for j in range(n):fs.append((i*n+j,i*n+(j+1)%n,(i+1)*n+(j+1)%n,(i+1)*n+j))
    fs.extend([tuple(reversed(range(n))),tuple((len(points)-1)*n+j for j in range(n))])
    me=bpy.data.meshes.new(name+'-mesh');me.from_pydata(vs,[],fs);me.update()
    ob=bpy.data.objects.new(name,me);source.users_collection[0].objects.link(ob)
    ob.matrix_world=source.matrix_world.copy()
    me.materials.append(material or source.data.materials[0])
    for g in source.vertex_groups:ob.vertex_groups.new(name=g.name)
    for i in range(len(points)):
        sample=weights[i] if weights else [(0,1.0)]
        for gi,w in sample:
            if w>0:ob.vertex_groups[gi].add(list(range(i*n,(i+1)*n)),w,'REPLACE')
    arm=ob.modifiers.new('Armature','ARMATURE');arm.object=bpy.data.objects['ARM-warrior-3D']
    for p in me.polygons:p.use_smooth=True
    ADDED.append(name)
    return ob

def hem(source,indices,name,radius,offset):
    points=[source.data.vertices[i].co+Vector(offset) for i in indices]
    weights=[[(g.group,g.weight) for g in source.data.vertices[i].groups] for i in indices]
    return tube(name,points,radius,source,weights)

def cloth():
    cape=bpy.data.objects['GEO-short-red-cape']
    for v in cape.data.vertices:
        row,col=divmod(v.index,11)
        t=row/8;u=col/10
        # Keep the attachment row unchanged. Move free cloth away from body.
        fold=.014*math.sin(math.pi*t/2)*(math.sin(u*math.tau*2+.32*t)+.35*math.sin(u*math.tau))
        v.co.x-=.006*t+fold
        if row==8:
            v.co.z+=.006*(1+math.sin(col*2.1))
            v.co.y+=.003*math.sin(col*2.8)
    cape.data.update();CHANGED.append(cape.name)
    # Same existing skinning; added hems copy source edge weights exactly.
    hem(cape,list(range(88,99)),'GEO-cape-hem-step05',.0025,(-.002,0,0))
    hem(cape,[j*11 for j in range(9)],'GEO-cape-side-L-step05',.002,(-.002,0,0))
    hem(cape,[j*11+10 for j in range(9)],'GEO-cape-side-R-step05',.002,(-.002,0,0))
    tab=bpy.data.objects['GEO-red-tabard']
    for v in tab.data.vertices:
        row,col=divmod(v.index,9);t=row/8;u=col/8
        v.co.x+=.004*t+.006*math.sin(math.pi*t)*math.cos(u*math.tau*2)
        if row==8:v.co.z+=.004*(1+math.cos(col*1.7))
    tab.data.update();CHANGED.append(tab.name)
    hem(tab,list(range(72,81)),'GEO-tabard-hem-step05',.002,(.002,0,0))
    for side,col in [('L',0),('R',8)]:
        hem(tab,[j*9+col for j in range(9)],'GEO-tabard-side-'+side+'-step05',.0015,(.002,0,0))
    # Break uniform tubular scarf folds without expanding the neck silhouette.
    for name in ['GEO-scarf-wrap','GEO-scarf-fold-0','GEO-scarf-fold-1','GEO-scarf-fold-2']:
        ob=bpy.data.objects[name]
        for v in ob.data.vertices:
            angle=math.atan2(v.co.y,v.co.x)
            v.co.z+=.0035*math.sin(3*angle+.4)+.0015*math.sin(5*angle)
        ob.data.update();CHANGED.append(name)

def flatten_ring(name,cx,cy,rx,ry,factor):
    ob=bpy.data.objects[name]
    for v in ob.data.vertices:
        a=math.atan2((v.co.y-cy)/ry,(v.co.x-cx)/rx)
        x=cx+rx*math.cos(a);y=cy+ry*math.sin(a)
        v.co.x=x+(v.co.x-x)*factor
        v.co.y=y+(v.co.y-y)*factor
    ob.data.update();CHANGED.append(name)
    return ob

def leather():
    belt=flatten_ring('GEO-belt',0,0,.183,.26,.38)
    # Raised edges give the belt a constructed strip profile.
    for z,label in [(1.039,'lower'),(1.091,'upper')]:
        points=[(.198*math.cos(i*math.tau/64),.275*math.sin(i*math.tau/64),z) for i in range(65)]
        tube('GEO-belt-edge-'+label+'-step05',points,.0018,belt)
    for side,sg in [('L',1),('R',-1)]:
        strap=flatten_ring('GEO-thigh-strap-'+side,.018,sg*.165,.117,.115,.40)
        for z,label in [(.854,'lower'),(.876,'upper')]:
            points=[(.018+.126*math.cos(i*math.tau/40),sg*.165+.124*math.sin(i*math.tau/40),z) for i in range(41)]
            tube('GEO-thigh-strap-edge-'+side+'-'+label+'-step05',points,.0014,strap)
        glove=bpy.data.objects['GEO-glove-'+side]
        # Only dorsal leather changes; the gripping underside remains untouched.
        for v in glove.data.vertices:
            if v.co.z>.015:
                v.co.z=.015+(v.co.z-.015)*.90
        glove.data.update();CHANGED.append(glove.name)
        for sign in [-1,1]:
            points=[(-.050,sign*.040,.026),(-.028,sign*.049,.032),(.0,sign*.051,.030),(.030,sign*.043,.023)]
            tube('GEO-glove-seam-'+side+'-'+str(sign)+'-step05',points,.0017,glove)
    # An open rectangular buckle retains its original placement and dark inset.
    buckle=bpy.data.objects['GEO-belt-buckle'];old=buckle.data
    verts=[]
    for x,wy,hz in [(-.014,.0475,.0375),(.0225,.0475,.0375),(.0225,.030,.021),(-.014,.030,.021)]:
        verts.extend([(x,-wy,-hz),(x,wy,-hz),(x,wy,hz),(x,-wy,hz)])
    faces=[]
    for loopa,loopb in [(0,1),(1,2),(2,3),(3,0)]:
        for i in range(4):faces.append((loopa*4+i,loopa*4+(i+1)%4,loopb*4+(i+1)%4,loopb*4+i))
    me=bpy.data.meshes.new('belt-buckle-frame-step05');me.from_pydata(verts,[],faces)
    bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free();me.update()
    for m in old.materials:me.materials.append(m)
    buckle.data=me
    group=buckle.vertex_groups.get('pelvis') or buckle.vertex_groups.new(name='pelvis')
    group.add(list(range(len(verts))),1,'REPLACE')
    for mod in buckle.modifiers:
        if mod.type=='BEVEL':mod.width=.003;mod.segments=2
    CHANGED.append(buckle.name)
    tube('GEO-belt-buckle-pin-step05',[(.025,-.028,0),(.027,0,0),(.025,.020,0)],.0028,buckle,material=me.materials[0])
    # Pocket flap seams follow its own pelvis binding and local transform.
    flap=bpy.data.objects['GEO-pouch-flap']
    tube('GEO-pouch-flap-seam-step05',[(-.039,-.027,.018),(-.039,-.027,-.017),(.038,-.027,-.017),(.038,-.027,.018)],.0016,flap)

def main():
    if Path(bpy.data.filepath).resolve()!=SOURCE.resolve():raise RuntimeError('Expected stage 4 file.')
    if DEST.exists():raise FileExistsError(DEST)
    cloth();leather()
    bpy.context.scene['art_stage_05_done']=True
    report={'stage':5,'source':str(SOURCE),'saved_as':str(DEST),'blender':bpy.app.version_string,'modified_objects':CHANGED,'added_construction_details':ADDED,'cloth_method':'Existing vertex topology, indices and weights retained; positions only. Hems inherit the sampled edge vertex weights.','preserved':'Rig, Actions, keyframes, hand contacts, equipment, prior armor/helmet/sword edits, materials, lighting and cameras.','review':'Static frame 1 viewport only; cloth clearance throughout the cycle is not validated. No playback, renders, simulations, Unity tests or builds.'}
    bpy.ops.wm.save_as_mainfile(filepath=str(DEST))
    (ROOT/'stage05-change-log.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    return report

if __name__=='__main__':print(json.dumps(main(),ensure_ascii=False))
