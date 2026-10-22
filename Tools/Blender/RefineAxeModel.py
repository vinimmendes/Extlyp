"""Refine the approved axe on a new Blender file; render the weapon independently.
Coordinates preserve the existing X-axis shaft, right grip at 50/128 and left at 22/128.
"""
import bpy, math, bmesh
from pathlib import Path
from mathutils import Vector

ROOT=Path('C:/Users/Vinni/Documents/GitHub/Extlyp')
OUT=ROOT/'Prototypes/PlayerWalk3D/AxeRefined'
MODEL=OUT/'protagonista-machado-refinado.blend'
PREFIX='GEO-refined-axe-'
WORLD_SIZE=5.1

def material(name,base,roughness,metallic,grain=False):
    m=bpy.data.materials.new(name);m.diffuse_color=(*base,1);m.use_nodes=True
    n=m.node_tree.nodes;links=m.node_tree.links;bs=next(node for node in n if node.type=='BSDF_PRINCIPLED')
    bs.inputs['Roughness'].default_value=roughness;bs.inputs['Metallic'].default_value=metallic
    coord=n.new('ShaderNodeTexCoord');mapping=n.new('ShaderNodeVectorMath');mapping.operation='MULTIPLY'
    mapping.inputs[1].default_value=(3,95,95) if grain else (26,26,26)
    links.new(coord.outputs['Generated'],mapping.inputs[0])
    noise=n.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=1;noise.inputs['Detail'].default_value=2
    links.new(mapping.outputs['Vector'],noise.inputs['Vector'])
    ramp=n.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].position=.18;ramp.color_ramp.elements[1].position=.82
    ramp.color_ramp.elements[0].color=(*(v*(.65 if grain else .88) for v in base),1)
    ramp.color_ramp.elements[1].color=(*(min(1,v*(1.30 if grain else 1.12)) for v in base),1)
    links.new(noise.outputs['Fac'],ramp.inputs[0]);links.new(ramp.outputs['Color'],bs.inputs['Base Color'])
    bump=n.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.16 if grain else .04
    bump.inputs['Distance'].default_value=.0012 if grain else .0003
    links.new(noise.outputs['Fac'],bump.inputs['Height']);links.new(bump.outputs['Normal'],bs.inputs['Normal'])
    return m

def mesh(name,verts,faces,mats,indices=None):
    data=bpy.data.meshes.new(PREFIX+name);data.from_pydata(verts,[],faces);data.update()
    bm=bmesh.new();bm.from_mesh(data)
    bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.000001)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(data);bm.free()
    obj=bpy.data.objects.new(PREFIX+name,data);bpy.context.scene.collection.objects.link(obj)
    for mat in mats:data.materials.append(mat)
    if indices:
        for poly,index in zip(data.polygons,indices):poly.material_index=index
    bevel=obj.modifiers.new('Small forged edge bevel','BEVEL');bevel.width=.002;bevel.segments=2
    obj.modifiers.new('Weighted face normals','WEIGHTED_NORMAL')
    obj.parent=bpy.data.objects['CTRL-axe-right-grip']
    return obj

def shaft(wood,leather):
    # Elliptical rings: the contact region is straight and keeps its original radius.
    rings=[(-.350,.029,.023,0),(-.332,.037,.029,0),(-.286,.038,.029,0),
        (-.10,.038,.029,0),(.02,.037,.028,0),(.20,.034,.026,.008),(.41,.031,.025,.010),(.532,.028,.023,.010)]
    v=[];sides=20
    for x,ry,rz,offset in rings:
        for i in range(sides):
            t=2*math.pi*i/sides;v.append((x,offset+ry*math.cos(t),rz*math.sin(t)))
    faces=[tuple(reversed(range(sides)))];ids=[0]
    for j in range(len(rings)-1):
        for i in range(sides):
            a=j*sides+i;b=j*sides+(i+1)%sides
            faces.append((a,b,b+sides,a+sides));ids.append(0)
    faces.append(tuple((len(rings)-1)*sides+i for i in range(sides)));ids.append(0)
    handle=mesh('oval-wooden-haft',v,faces,[wood],ids)
    for p in handle.data.polygons:p.use_smooth=len(p.vertices)==4
    # Broad spiralling leather strip. The wood remains visible at the leading hand.
    v=[];faces=[];steps=120;turns=4.3
    for i in range(steps+1):
        t=i/steps;angle=2*math.pi*turns*t
        x=-.318+.225*t
        for edge in [-.028,.028]:v.append((x+edge,.040*math.cos(angle),.031*math.sin(angle)))
    for i in range(steps):faces.append((2*i,2*i+1,2*i+3,2*i+2))
    wrap=mesh('leather-grip-wrap',v,faces,[leather]);wrap.modifiers['Small forged edge bevel'].width=.0008
    solid=wrap.modifiers.new('Leather thickness','SOLIDIFY');solid.thickness=.002

def head(steel,edge):
    # A single curved cutting wing with a concave beard and a compact flat poll.
    outer=[(.46,-.065),(.52,-.05),(.52,.04),(.55,.10),(.58,.22),(.57,.30),
        (.51,.36),(.42,.39),(.31,.39),(.20,.36),(.14,.30),(.21,.26),
        (.265,.19),(.295,.12),(.31,.04),(.34,-.045)]
    inner=list(outer)
    inner[4:11]=[(.53,.20),(.525,.27),(.48,.31),(.41,.337),(.32,.337),(.235,.31),(.19,.277)]
    # Smooth the cutting arc and concave beard while retaining the socket corners.
    def smooth(points):
        result=[]
        for i in range(len(points)):
            a,b,c,d=[Vector(points[j%len(points)]) for j in [i-1,i,i+1,i+2]]
            steps=4 if 3<=i<=13 else 1
            for k in range(steps):
                t=k/steps
                result.append(tuple(.5*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t)))
        return result
    outer=smooth(outer);inner=smooth(inner)
    n=len(outer);verts=[]
    for sign in [1,-1]:
        verts.extend((x,y,sign*.038) for x,y in inner)
        verts.extend((x,y,sign*(.008 if (Vector(inner[i])-Vector(outer[i])).length>.000001 else .038)) for i,(x,y) in enumerate(outer))
    faces=[tuple(range(n)),tuple(reversed(range(2*n,3*n)))];ids=[0,0]
    for i in range(n):
        j=(i+1)%n
        if inner[i]!=outer[i] or inner[j]!=outer[j]:
            faces.extend([(i,n+i,n+j,j),(2*n+i,2*n+j,3*n+j,3*n+i)])
            ids.extend([1,1])
        faces.append((n+i,3*n+i,3*n+j,n+j));ids.append(1 if (Vector(inner[i])-Vector(outer[i])).length>.000001 else 0)
    blade=mesh('forged-crescent-head',verts,faces,[steel,edge],ids)
    blade.modifiers['Small forged edge bevel'].width=.0015
    # Closed iron collar around the shaft, with an actual open bore for the wood.
    v=[];faces=[];sides=16
    for x,ry,rz in [(.345,.057,.047),(.465,.057,.047),(.345,.033,.027),(.465,.033,.027)]:
        for i in range(sides):
            a=2*math.pi*i/sides;v.append((x,.010+ry*math.cos(a),rz*math.sin(a)))
    for i in range(sides):
        j=(i+1)%sides
        faces.extend([(i,j,sides+j,sides+i),(2*sides+i,3*sides+i,3*sides+j,2*sides+j),
            (i,2*sides+i,2*sides+j,j),(sides+i,sides+j,3*sides+j,3*sides+i)])
    mesh('iron-eye-collar',v,faces,[steel])

def build(resume=False):
    if MODEL.exists() and not resume:raise FileExistsError(MODEL)
    expected=ROOT/'Prototypes/PlayerWalk3D/AxeAttack/protagonista-ataque-machado.blend'
    if resume:
        if Path(bpy.data.filepath).resolve()!=MODEL.resolve() or any(o.name.startswith(PREFIX) for o in bpy.data.objects):
            raise RuntimeError('Resume only an empty, newly created refined copy')
    elif Path(bpy.data.filepath).resolve()!=expected.resolve():raise RuntimeError('Expected approved axe animation')
    OUT.mkdir(exist_ok=True)
    # New file includes the current unsaved scene state; source is never overwritten.
    if not resume:bpy.ops.wm.save_as_mainfile(filepath=str(MODEL))
    for o in bpy.context.scene.objects:
        if o.name in ('GEO-axe-shaft-guide','GEO-axe-head-guide'):o.hide_render=True;o.hide_set(True)
    wood=material('MAT-axe-worn-warm-wood',(.24,.105,.035),.66,0,True)
    leather=material('MAT-axe-dark-leather',(.025,.011,.006),.76,0)
    steel=material('MAT-axe-forged-dark-iron',(.070,.080,.085),.48,.90)
    edge=material('MAT-axe-honed-edge',(.55,.58,.54),.28,.75)
    shaft(wood,leather);head(steel,edge)
    bpy.context.scene.frame_set(1)
    bpy.context.scene.cycles.samples=32
    # Pack the approved design reference into the source file.
    reference=bpy.data.images.load(str(ROOT/'Assets/Resources/Items/IconsV2/BattleAxe.png'),check_existing=True)
    reference.pack()
    bpy.ops.wm.save_as_mainfile(filepath=str(MODEL))
    return {'model':str(MODEL),'weapon_objects':[o.name for o in bpy.context.scene.objects if o.name.startswith(PREFIX)]}

def visibility():
    state=[]
    for obj in bpy.context.scene.objects:
        if obj.type in {'MESH','CURVE'}:
            state.append((obj,obj.visible_camera));obj.visible_camera=obj.name.startswith(PREFIX)
    return state

def render_frames(first,last):
    s=bpy.context.scene
    if Path(bpy.data.filepath).resolve()!=MODEL.resolve():raise RuntimeError('Wrong scene')
    folder=OUT/'Weapon';folder.mkdir(exist_ok=True)
    state=visibility();cam=s.camera;ortho=cam.data.ortho_scale;frame=s.frame_current
    try:
        cam.data.ortho_scale=WORLD_SIZE/(3.4/2.6500000953674316)
        s.render.resolution_x=s.render.resolution_y=256;s.render.resolution_percentage=100
        s.render.film_transparent=True;s.render.image_settings.color_mode='RGBA'
        for f in range(first,last+1):
            s.frame_set(f);s.render.filepath=str(folder/f'{f:03}.png');bpy.ops.render.render(write_still=True)
    finally:
        cam.data.ortho_scale=ortho;s.frame_set(frame)
        for obj,visible in state:obj.visible_camera=visible
    return {'first':first,'last':last,'worldSize':WORLD_SIZE}

def render_isolated(hero=False):
    s=bpy.context.scene;state=visibility();oldcam=s.camera;frame=s.frame_current
    # Detached linked copies prevent render-time animation evaluation moving the item.
    shadows=[];copies=[]
    for obj,_ in state:
        shadows.append((obj,obj.hide_render));obj.hide_render=True
        if obj.name.startswith(PREFIX):
            copy=obj.copy();copy.name='GEO-isolated-'+obj.name;copy.animation_data_clear()
            copy.parent=None;copy.matrix_world=obj.matrix_basis.copy()
            copy.hide_render=False;copy.visible_camera=True;s.collection.objects.link(copy);copies.append(copy)
    data=bpy.data.cameras.new('CAM-axe-isolated');camera=bpy.data.objects.new(data.name,data);s.collection.objects.link(camera)
    try:
        target=Vector((.115,.14,0));camera.location=(.80,-1.10,1.65) if hero else (.115,-.40,2.5)
        camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
        data.type='ORTHO';data.ortho_scale=1.15 if hero else 1.12;s.camera=camera
        s.render.resolution_x=s.render.resolution_y=1024 if hero else 512
        s.render.resolution_percentage=100;s.render.film_transparent=True;s.render.image_settings.color_mode='RGBA'
        s.render.filepath=str(OUT/('machado-detalhe.png' if hero else 'machado-chao.png'))
        bpy.context.view_layer.update();bpy.ops.render.render(write_still=True)
        return {'path':s.render.filepath}
    finally:
        s.camera=oldcam;s.frame_set(frame)
        for obj,visible in state:obj.visible_camera=visible
        for obj,hidden in shadows:obj.hide_render=hidden
        for obj in copies:bpy.data.objects.remove(obj,do_unlink=True)
        bpy.data.objects.remove(camera,do_unlink=True);bpy.data.cameras.remove(data)
