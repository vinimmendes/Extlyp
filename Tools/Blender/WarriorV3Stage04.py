"""Stage 4: helmet and sword geometry, retaining equipment anchors and rig."""
import bpy, bmesh, math, json
from pathlib import Path
from mathutils import Vector

ROOT=Path('C:/Users/Vinni/Documents/GitHub/Extlyp/Art/PlayerProposals/WarriorArtV3')
SOURCE=ROOT/'working-v3/warrior_v3-step03-armor.blend'
DEST=ROOT/'working-v3/warrior_v3-step04-equipment.blend'
CHANGES=[]

def swap(name,vs,fs,smooth=False,thickness=None,bevel=.0015):
    o=bpy.data.objects[name]
    bone=[g.name for g in o.vertex_groups]
    if len(bone)!=1:raise RuntimeError('Expected one rigid bone: '+name)
    mats=list(o.data.materials)
    me=bpy.data.meshes.new(name+'-step04')
    me.from_pydata([tuple(v) for v in vs],[],fs)
    bm=bmesh.new();bm.from_mesh(me)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
    me.update()
    for m in mats:me.materials.append(m)
    o.data=me
    g=o.vertex_groups.get(bone[0]) or o.vertex_groups.new(name=bone[0])
    g.add(list(range(len(vs))),1,'REPLACE')
    for f in me.polygons:f.use_smooth=smooth
    sol=next((m for m in o.modifiers if m.type=='SOLIDIFY'),None)
    if thickness is not None:
        sol=sol or o.modifiers.new('Plate thickness','SOLIDIFY')
        sol.thickness=thickness;sol.offset=-1
        with bpy.context.temp_override(object=o,active_object=o):
            bpy.ops.object.modifier_move_to_index(modifier=sol.name,index=0)
    if bevel:
        b=next((m for m in o.modifiers if m.type=='BEVEL'),None) or o.modifiers.new('Forged edge','BEVEL')
        b.width=bevel;b.segments=2;b.limit_method='ANGLE';b.angle_limit=math.radians(30)
        with bpy.context.temp_override(object=o,active_object=o):
            bpy.ops.object.modifier_move_to_index(modifier=b.name,index=1 if sol else 0)
    CHANGES.append({'object':name,'vertices':len(vs),'faces':len(fs),'bone':bone[0]})
    return o

def strip(name,points,width,depth):
    vs=[];fs=[]
    for i,(x,y,z) in enumerate(points):
        tangent=Vector(points[min(i+1,len(points)-1)])-Vector(points[max(i-1,0)])
        outward=Vector((tangent.z,0,-tangent.x)).normalized()
        c=Vector((x,y,z))+outward*.002
        vs.extend([c+Vector((0,-width,0)),c+outward*depth,c+Vector((0,width,0))])
    for j in range(len(points)-1):
        for k in range(2):fs.append((j*3+k,j*3+k+1,(j+1)*3+k+1,(j+1)*3+k))
    return swap(name,vs,fs,False,.003,.001)

def helmet():
    vs=[];fs=[];n=24
    # Extra profile break above the brow yields a forged crown, within v2 size.
    rows=[(1.63,.19,.16),(1.72,.227,.187),(1.855,.218,.183),(1.925,.19,.159),(1.99,.14,.12),(2.045,.058,.047)]
    for z,rx,ry in rows:
        for i in range(n):
            a=math.tau*i/n
            vs.append((.012+rx*math.cos(a),ry*math.sin(a),z+.012*abs(math.cos(a))))
    for j in range(len(rows)-1):
        for i in range(n):fs.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
    vs.append((.016,0,2.072));tip=len(vs)-1
    for i in range(n):fs.append(((len(rows)-1)*n+i,(len(rows)-1)*n+(i+1)%n,tip))
    fs.append(tuple(reversed(range(n))))
    swap('GEO-helmet-forged-shell-v2',vs,fs,True,None,.002)
    points=[(.239,0,1.732),(.230,0,1.867),(.202,0,1.937),(.152,0,2.002),(.070,0,2.057),(.016,0,2.072),(-.046,0,2.057),(-.128,0,2.002),(-.178,0,1.937),(-.206,0,1.867),(-.215,0,1.732)]
    strip('GEO-helmet-steel-crest',points,.019,.009)
    brass=[]
    for i,point in enumerate(points):
        tangent=Vector(points[min(i+1,len(points)-1)])-Vector(points[max(i-1,0)])
        normal=Vector((tangent.z,0,-tangent.x)).normalized()
        brass.append(Vector(point)+normal*.010)
    strip('GEO-helmet-crown-ridge',brass,.004,.002)
    # Separate upper/lower visor fields around the existing dark eye slot.
    vs=[];fs=[];n=17
    for rows in [[(1.645,.223),(1.72,.242),(1.818,.246),(1.834,.247)],[(1.858,.247),(1.882,.242),(1.905,.225)]]:
        start=len(vs)
        for z,rx in rows:
            for i in range(n):
                a=-1.10+2.20*i/(n-1)
                ridge=.009*(1-abs(math.sin(a)))
                vs.append((.022+rx*math.cos(a)+ridge,.194*math.sin(a),z))
        for j in range(len(rows)-1):
            for i in range(n-1):
                q=start+j*n+i;fs.append((q,q+1,q+n+1,q+n))
    swap('GEO-visor-faceplate',vs,fs,False,.007,.002)
    # Keep dark recess and openings in place; sharpen their cross sections.
    o=bpy.data.objects['GEO-visor-eye-slit']
    for v in o.data.vertices:v.co.z=1.846+(v.co.z-1.846)*.60
    strip('GEO-visor-nasal',[(.267,0,1.68),(.278,0,1.80),(.270,0,1.90)],.010,.005)

def sword():
    hand=Vector((.46,-.535,1.297))
    d=Vector((.90,-.40,.08)).normalized()
    cross=Vector((-d.y,d.x,0)).normalized();normal=d.cross(cross).normalized()
    guard=hand+d*.085
    # Small blade-only roll exposes its broad planes; grip and blade axis stay.
    roll=math.radians(9)
    w=cross*math.cos(roll)+normal*math.sin(roll)
    h=normal*math.cos(roll)-cross*math.sin(roll)
    profile=[(-1,0),(-.70,.60),(-.18,1),(0,.55),(.18,1),(.70,.60),(1,0),(.70,-.60),(.18,-1),(0,-.55),(-.18,-1),(-.70,-.60)]
    vs=[];fs=[];n=len(profile)
    for distance,width in [(.015,.050),(.10,.049),(.52,.038),(.68,.029),(.75,.018)]:
        c=guard+d*distance
        vs.extend([c+w*(width*x)+h*(.009*y) for x,y in profile])
    for j in range(4):
        for i in range(n):fs.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
    vs.append(guard+d*.83);tip=len(vs)-1
    for i in range(n):fs.append((4*n+i,4*n+(i+1)%n,tip))
    fs.append(tuple(reversed(range(n))))
    swap('GEO-sword-blade',vs,fs,False,None,0)
    # Chamfered quillons, same guard center and approximate outer envelope.
    vs=[];fs=[]
    for t,off,th in [(-.135,-.025,.012),(-.110,-.010,.014),(-.045,0,.018),(.045,0,.018),(.110,-.010,.014),(.135,-.025,.012)]:
        c=guard+cross*t+d*off
        vs.extend([c+d*a+normal*b for a,b in [(-th,-.017),(th,-.017),(th,.017),(-th,.017)]])
    for j in range(5):
        for i in range(4):fs.append((j*4+i,j*4+(i+1)%4,(j+1)*4+(i+1)%4,(j+1)*4+i))
    fs.extend([(3,2,1,0),tuple(20+i for i in range(4))])
    swap('GEO-sword-crossguard',vs,fs,False,None,.003)
    # Wheel pommel replaces ball, centered at the unchanged hilt end.
    o=bpy.data.objects['GEO-sword-pommel'];vs=[];fs=[];n=16
    for z,r in [(-.013,.026),(-.010,.037),(.010,.037),(.013,.026)]:
        for i in range(n):
            a=math.tau*i/n
            v=cross*(r*math.cos(a))+d*(r*math.sin(a))+normal*z
            vs.append(v)
    for j in range(3):
        for i in range(n):fs.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
    fs.extend([tuple(reversed(range(n))),tuple(3*n+i for i in range(n))])
    swap(o.name,vs,fs,False,None,.001)
    # Leather winding in local grip coordinates, rigidly linked to same hand.
    src=bpy.data.objects['GEO-sword-hilt']
    vs=[];fs=[];rings=100;sides=5
    for i in range(rings):
        t=i/(rings-1);a=t*math.tau*8
        for j in range(sides):
            b=j*math.tau/sides
            r=.0275+.0015*math.cos(b)
            vs.append((r*math.cos(a),r*math.sin(a),-.087+t*.17+.0015*math.sin(b)))
    for i in range(rings-1):
        for j in range(sides):fs.append((i*sides+j,i*sides+(j+1)%sides,(i+1)*sides+(j+1)%sides,(i+1)*sides+j))
    me=bpy.data.meshes.new('sword-leather-winding-step04');me.from_pydata(vs,[],fs);me.update()
    ob=bpy.data.objects.new('GEO-sword-grip-winding-step04',me)
    src.users_collection[0].objects.link(ob);ob.matrix_world=src.matrix_world.copy()
    me.materials.append(src.data.materials[0])
    vg=ob.vertex_groups.new(name='hand.R');vg.add(list(range(len(vs))),1,'REPLACE')
    arm=ob.modifiers.new('Armature','ARMATURE');arm.object=bpy.data.objects['ARM-warrior-3D']
    for p in me.polygons:p.use_smooth=True

def main():
    if Path(bpy.data.filepath).resolve()!=SOURCE.resolve():raise RuntimeError('Expected stage 3 file.')
    if DEST.exists():raise FileExistsError(DEST)
    helmet();sword()
    bpy.context.scene['art_stage_04_done']=True
    report={'stage':4,'source':str(SOURCE),'saved_as':str(DEST),'blender':bpy.app.version_string,'modified_surfaces':CHANGES,'additional_changes':['Eye slit cross section sharpened','One leather grip winding added'],'blade_roll_degrees':9,'preserved':'Hand positions, sword axis/length, torch, rig, keyframes, Actions, materials, cameras, lights, cloth and stage 3 armor','review':'Static viewport review at frame 1 only. Full-cycle clearance and final render appearance remain unverified.'}
    bpy.ops.wm.save_as_mainfile(filepath=str(DEST))
    (ROOT/'stage04-change-log.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    return report

if __name__=='__main__':print(json.dumps(main(),ensure_ascii=False))
