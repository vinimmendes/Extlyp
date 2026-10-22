"""Stage 3: refine existing armor meshes, retaining objects and rigid bone links.

Execute through official Blender MCP in the prepared v3 source file only.
No frame changes, animation playback, simulation, render or package scripts.
"""
import bpy
import bmesh
import math
import json
from pathlib import Path

ROOT=Path('C:/Users/Vinni/Documents/GitHub/Extlyp/Art/PlayerProposals/WarriorArtV3')
SOURCE=ROOT/'working-v3/warrior_v3.blend'
DEST=ROOT/'working-v3/warrior_v3-step03-armor.blend'

def replace_surface(name,verts,faces,smooth=False,thickness=.006):
    ob=bpy.data.objects[name]
    bone_names=[g.name for g in ob.vertex_groups]
    if len(bone_names)!=1:
        raise RuntimeError('Expected a rigid single-bone plate: '+name)
    mats=list(ob.data.materials)
    old_counts=[len(ob.data.vertices),len(ob.data.polygons)]
    me=bpy.data.meshes.new(name+'-step03-mesh')
    me.from_pydata(verts,[],faces)
    bm=bmesh.new();bm.from_mesh(me)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    bm.to_mesh(me);bm.free();me.update()
    for mat in mats:me.materials.append(mat)
    ob.data=me
    for p in me.polygons:p.use_smooth=smooth
    group=ob.vertex_groups.get(bone_names[0]) or ob.vertex_groups.new(name=bone_names[0])
    group.add(list(range(len(me.vertices))),1.0,'REPLACE')
    sol=next((m for m in ob.modifiers if m.type=='SOLIDIFY'),None)
    if sol is None:sol=ob.modifiers.new('Plate thickness','SOLIDIFY')
    sol.thickness=thickness;sol.offset=-1
    bevel=next((m for m in ob.modifiers if m.type=='BEVEL'),None)
    if bevel is None:bevel=ob.modifiers.new('Forged bevel','BEVEL')
    bevel.width=.0025;bevel.segments=2;bevel.limit_method='ANGLE'
    bevel.angle_limit=math.radians(24)
    with bpy.context.temp_override(object=ob,active_object=ob):
        bpy.ops.object.modifier_move_to_index(modifier=sol.name,index=0)
        bpy.ops.object.modifier_move_to_index(modifier=bevel.name,index=1)
    ob['art_stage']='03: shaped plate silhouette'
    return {'object':name,'before_vertices_faces':old_counts,'after_vertices_faces':[len(verts),len(faces)],'rigid_bone':bone_names[0],'thickness_m':thickness}

def shoulder(side,sign):
    # Preserve the original skirt footprint and border. A broad upper crown
    # transitions through a defined shoulder break instead of a spherical cap.
    n=24;vs=[];fs=[]
    for radius,height in [(.08,.104),(.43,.100),(.77,.077),(1.0,0.0)]:
        for i in range(n):
            a=math.tau*i/n
            x=.1687612*radius*math.cos(a)
            y=sign*.345+.1836519*radius*math.sin(a)
            # Modest fore/aft ridge, retained within the original envelope.
            z=1.5042193-.012*abs(math.sin(a))+height
            vs.append((x,y,z))
    fs.append(tuple(range(n)))
    for j in range(3):
        for i in range(n):
            fs.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
    report=[replace_surface('GEO-pauldron-shell-'+side,vs,fs,True,.009)]
    # Curved lames with a flared lower lip and clear overlap. Same bones.
    for k in range(2):
        vs=[];fs=[];cols=17
        for j,(zoff,flare) in enumerate([(0,0),(-.022,.004),(-.044,.010)]):
            for i in range(cols):
                a=math.pi*i/(cols-1)
                x=.145*math.cos(a)
                y=sign*(.35+(.125+k*.013+flare)*math.sin(a))
                z=1.466-k*.038+zoff+.008*abs(math.cos(a))
                vs.append((x,y,z))
        for row in range(2):
            for i in range(cols-1):
                q=row*cols+i;fs.append((q,q+1,q+cols+1,q+cols))
        report.append(replace_surface('GEO-pauldron-lame-v2-'+side+'-'+str(k),vs,fs,True,.007))
    # Rivets sit on the new shell, following its radial height profile.
    for i in range(4):
        ob=bpy.data.objects['GEO-pauldron-rivet-'+side+'-'+str(i)]
        a=i*math.pi/2+.25
        r=math.sqrt((.15*math.cos(a)/.1687612)**2+(.16*math.sin(a)/.1836519)**2)
        h=.077*(1-r)/.23 if r>.77 else .077
        ob.location.z=1.5042193-.012*abs(math.sin(a))+h+.008
    return report

def joint(name,kind,sign):
    # Local coordinates: existing object origin and bind transformation remain.
    # Shield-like outline and raised central field leave the rear joint open.
    if kind=='knee':
        outline=[(-.045,.067),(.045,.067),(.078,.038),(.09,.0),(.067,-.052),(.025,-.072),(-.025,-.072),(-.067,-.052),(-.09,0),(-.078,.038)]
        depth=.073;back=.003
    else:
        outline=[(-.044,.061),(.035,.061),(.072,.031),(.078,-.003),(.048,-.052),(-.022,-.064),(-.066,-.035),(-.078,.003)]
        depth=.080;back=-.005
    n=len(outline);vs=[];fs=[]
    for scale,x in [(1,back),(.63,depth*.86),(.19,depth)]:
        for y,z in outline:
            vs.append((x,y*scale,z*scale))
    for j in range(2):
        for i in range(n):fs.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
    fs.append(tuple(2*n+i for i in range(n)))
    return replace_surface(name,vs,fs,False,.006)

def thigh(side):
    # Cuisse follows the thigh, tapering toward the knee; back remains mail.
    vs=[];fs=[];n=13
    for z,rx,ry in [(-.17,.082,.085),(-.08,.096,.096),(.09,.093,.098),(.17,.072,.089)]:
        for i in range(n):
            a=-math.pi/2+math.pi*i/(n-1)
            vs.append((rx*math.cos(a),ry*math.sin(a),z))
    for j in range(3):
        for i in range(n-1):
            q=j*n+i;fs.append((q,q+1,q+n+1,q+n))
    return replace_surface('GEO-thigh-plate-'+side,vs,fs,True,.007)

def main():
    if Path(bpy.data.filepath).resolve()!=SOURCE.resolve():
        raise RuntimeError('Open the prepared unmodified warrior_v3.blend first.')
    if DEST.exists():raise FileExistsError(DEST)
    if bpy.context.scene.get('art_stage_03_done'):raise RuntimeError('Stage already applied in memory.')
    affected=[]
    for side,sign in [('L',1),('R',-1)]:
        affected.extend(shoulder(side,sign))
        affected.append(joint('GEO-knee-cap-'+side,'knee',sign))
        affected.append(joint('GEO-elbow-'+side,'elbow',sign))
        affected.append(thigh(side))
    s=bpy.context.scene
    s['art_stage_03_done']=True
    s['art_stage_03_note']='Armor mesh refinement only. Cycle not played or collision checked.'
    report={'stage':3,'source':str(SOURCE),'saved_as':str(DEST),'blender':bpy.app.version_string,'changed_surfaces':affected,'rivets_repositioned':8,'scope':'Shoulder shells/lames, knee/elbow guards, thigh plates. No rig, keyframe, material, camera, equipment or light edits.','verification':'Static authoring review only; no animation playback, simulation, render, Unity test or build.','limitations':'Articulation clearance over the full cycle awaits stage 9; no claim of collision-free animation.'}
    # Render output must stay in this version, not the old cloud filesystem.
    # Deliberately preserve it here: output redirection belongs to render stage.
    bpy.ops.wm.save_as_mainfile(filepath=str(DEST))
    (ROOT/'stage03-change-log.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    return report

if __name__=='__main__':
    print(json.dumps(main(),ensure_ascii=False))
