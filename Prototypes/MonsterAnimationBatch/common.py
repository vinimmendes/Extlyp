"""Original Papo Inchado. Blender 4.5: editable rig, procedural materials and baked gait."""
import bpy, math, random, sys
from pathlib import Path
from mathutils import Vector, Matrix
OUT=Path(__file__).resolve().parent
print('BUILD EDITABLE MODEL',flush=True)
random.seed(51)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene; scene.render.fps=24; scene.frame_start=1; scene.frame_end=145

def material(name,dark,light,roughness=.7,pores=.07):
    m=bpy.data.materials.new(name); m.use_nodes=True
    n=m.node_tree.nodes; l=m.node_tree.links; p=n.get('Principled BSDF')
    p.inputs['Roughness'].default_value=roughness
    noise=n.new('ShaderNodeTexNoise'); noise.inputs['Scale'].default_value=7; noise.inputs['Detail'].default_value=4
    ramp=n.new('ShaderNodeValToRGB'); ramp.color_ramp.elements[0].position=.25; ramp.color_ramp.elements[0].color=(*dark,1)
    ramp.color_ramp.elements[1].position=.75; ramp.color_ramp.elements[1].color=(*light,1)
    l.new(noise.outputs['Fac'],ramp.inputs[0]); l.new(ramp.outputs[0],p.inputs['Base Color'])
    micro=n.new('ShaderNodeTexNoise'); micro.inputs['Scale'].default_value=85; micro.inputs['Detail'].default_value=2
    bump=n.new('ShaderNodeBump'); bump.inputs['Strength'].default_value=pores; bump.inputs['Distance'].default_value=.018
    l.new(micro.outputs['Fac'],bump.inputs['Height']); l.new(bump.outputs[0],p.inputs['Normal'])
    return m
skin=material('Pele castanha ressecada',(.085,.057,.037),(.30,.225,.16),.81,.20)
head_skin=material('Cabeca cicatrizada',(.16,.105,.066),(.39,.305,.225),.77,.16)
pouch_mat=material('Papo amarelado doente',(.18,.17,.062),(.47,.43,.18),.48,.10)
vein_mat=material('Veias sob a pele',(.11,.080,.043),(.23,.155,.09),.73,.05)
lip_mat=material('Labios secos',(.18,.095,.064),(.36,.22,.155),.68,.1)
dark=material('Cavidade da boca',(.006,.003,.002),(.037,.011,.008),.38,.02)
tongue_mat=material('Lingua e gengiva',(.13,.052,.031),(.34,.16,.09),.43,.07)
teeth_mat=material('Dentes amarelados',(.26,.22,.12),(.58,.52,.34),.70,.04)
eye_mat=material('Olhos fundos',(.008,.006,.004),(.027,.023,.014),.25,0)

def sphere(name,loc,scale,mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32,ring_count=20,location=loc)
    o=bpy.context.object; o.name=name; o.scale=scale; o.data.materials.append(mat)
    for p in o.data.polygons: p.use_smooth=True
    return o

def box(name,loc,size,mat,bevel=.025):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    o=bpy.context.object; o.name=name; o.scale=size; o.data.materials.append(mat)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    b=o.modifiers.new('Bordas gastas','BEVEL'); b.width=bevel; b.segments=3
    o.modifiers.new('Normais ponderadas','WEIGHTED_NORMAL'); return o

def key(o,frame):
    for path in ['location','rotation_euler','scale']: o.keyframe_insert(data_path=path,frame=frame)

def smooth(t): return max(0,min(1,t))**2*(3-2*max(0,min(1,t)))

class Limb:
    """Two bones, analytical endpoint trajectory baked to FK; no live IK pole flips."""
    def __init__(self,name,hip,knee,foot,root,upper_radius=.12,lower_radius=.075,mat=None,digits=3):
        mat=mat or skin; self.name=name; self.hip=Vector(hip); self.knee=Vector(knee); self.foot=Vector(foot)
        a=bpy.data.armatures.new('Ossos '+name); rig=bpy.data.objects.new('RIG '+name,a); scene.collection.objects.link(rig)
        self.rig=rig; parent_keep(rig,root)
        bpy.ops.object.select_all(action='DESELECT'); rig.select_set(True); bpy.context.view_layer.objects.active=rig; bpy.ops.object.mode_set(mode='EDIT')
        upper=a.edit_bones.new('Superior'); upper.head=self.hip; upper.tail=self.knee
        lower=a.edit_bones.new('Inferior'); lower.head=self.knee; lower.tail=self.foot; lower.parent=upper; lower.use_connect=True
        bpy.ops.object.mode_set(mode='OBJECT'); rig.show_in_front=True
        self.objects=[]
        self.bind(segment(name+' musculo',hip,knee,upper_radius,lower_radius,mat),'Superior',root)
        muscle=sphere(name+' volume muscular',self.hip.lerp(self.knee,.32),(upper_radius*1.1,upper_radius*.95,(self.knee-self.hip).length*.30),mat)
        muscle.rotation_euler=(self.knee-self.hip).to_track_quat('Z','Y').to_euler()
        self.bind(muscle,'Superior',root)
        self.bind(segment(name+' membro',knee,foot,lower_radius,lower_radius*.60,mat),'Inferior',root)
        self.bind(sphere(name+' junta',knee,(lower_radius*1.1,)*3,mat),'Inferior',root)
        self.bind(sphere(name+' apoio',foot,(lower_radius*1.9,lower_radius*1.4,.045),mat),'Inferior',root)
        for i in range(digits):
            y=(i-(digits-1)/2)*.042
            p=self.foot+Vector((.05,y,0)); q=p+Vector((.11,y*.2,-.005))
            self.bind(segment(name+' dedo',p,q,.023,.009,mat),'Inferior',root)
            self.bind(segment(name+' unha',q,q+Vector((.045,0,-.01)),.011,.002,teeth_mat),'Inferior',root)
    def bind(self,o,bone,root):
        bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active=o
        bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
        group=o.vertex_groups.new(name=bone); group.add(list(range(len(o.data.vertices))),1,'REPLACE')
        mod=o.modifiers.new('Rig editavel','ARMATURE'); mod.object=self.rig; parent_keep(o,root); self.objects.append(o)
    def pose(self,frame,end=None):
        scene.frame_set(frame); end=Vector(end) if end is not None else self.foot.copy()
        hip,knee,foot=self.hip,self.knee,self.foot; a=(knee-hip).length; b=(foot-knee).length
        d=(end-hip).normalized(); dist=max(abs(a-b)+.0001,min((end-hip).length,a+b-.0001)); along=(a*a-b*b+dist*dist)/(2*dist)
        bend=knee-hip-d*(knee-hip).dot(d)
        if bend.length<.001: bend=Vector((0,0,1))-d*d.z
        new_knee=hip+d*along+bend.normalized()*math.sqrt(max(0,a*a-along*along)); end=hip+d*dist
        for name,old_a,old_b,new_a,new_b in [('Superior',hip,knee,hip,new_knee),('Inferior',knee,foot,new_knee,end)]:
            pb=self.rig.pose.bones[name]; pb.rotation_mode='QUATERNION'
            rotation=(old_b-old_a).rotation_difference(new_b-new_a).to_matrix().to_4x4()
            pb.matrix=Matrix.Translation(new_a)@rotation@Matrix.Translation(-old_a)@pb.bone.matrix_local
            bpy.context.view_layer.update(); pb.keyframe_insert(data_path='rotation_quaternion',frame=frame); pb.keyframe_insert(data_path='location',frame=frame)
    def visibility(self,frame,visible):
        for o in self.objects: o.hide_render=not visible; o.keyframe_insert(data_path='hide_render',frame=frame)

def walking(limbs,stride=.20):
    for frame in range(1,34):
        for i,limb in enumerate(limbs):
            p=((frame-1)/32+(i%2)*.5+((i//2)%2)*.5)%1
            if p<.6: x=stride-2*stride*p/.6; z=0
            else: t=(p-.6)/.4; x=-stride+2*stride*t; z=.075*math.sin(math.pi*t)
            limb.pose(frame,limb.foot+Vector((x,0,z)))

def finish(out,clips,scale=3.4,focus=.8):
    clips=[(name,list(dict.fromkeys(frames))) for name,frames in clips]
    out=Path(out); out.mkdir(parents=True,exist_ok=True)
    (out/'.rendering').write_text('Blender render in progress',encoding='utf-8')
    for o in scene.objects:
        if o.animation_data and o.animation_data.action:
            for curve in o.animation_data.action.fcurves:
                for point in curve.keyframe_points: point.interpolation='CONSTANT' if curve.data_path=='hide_render' else 'LINEAR'
    def camera(name,loc,target,ortho):
        d=bpy.data.cameras.new(name); d.type='ORTHO'; d.ortho_scale=ortho
        o=bpy.data.objects.new(name,d); scene.collection.objects.link(o); o.location=loc; o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler(); return o
    top=camera('Camera topo',(0,0,6),(0,0,0),scale); top.rotation_euler=(0,0,-math.pi/2)
    side=camera('Camera perspectiva',(4,-5,3.8),(0,0,focus),scale)
    world=bpy.data.worlds.new('Ambiente'); world.use_nodes=True; world.node_tree.nodes['Background'].inputs[0].default_value=(.14,.14,.13,1); world.node_tree.nodes['Background'].inputs[1].default_value=.45; scene.world=world
    for name,loc,power,size in [('Principal',(2,-3,5),250,4),('Preenchimento',(-3,2,3),110,3)]:
        d=bpy.data.lights.new(name,'AREA'); d.energy=power; d.shape='DISK'; d.size=size
        o=bpy.data.objects.new(name,d); scene.collection.objects.link(o); o.location=loc; o.rotation_euler=(Vector((0,0,focus))-o.location).to_track_quat('-Z','Y').to_euler()
    scene.render.engine='CYCLES'; scene.cycles.samples=24; scene.cycles.use_denoising=True
    scene.render.resolution_x=384; scene.render.resolution_y=384; scene.render.resolution_percentage=100; scene.render.film_transparent=True
    scene.render.image_settings.file_format='PNG'; scene.render.image_settings.color_mode='RGBA'; scene.view_settings.view_transform='AgX'; scene.view_settings.look='AgX - Medium High Contrast'
    scene.camera=top; scene.frame_start=1; scene.frame_end=601; scene.frame_set(501)
    scene.render.filepath=str(out/'idle.png'); bpy.ops.wm.save_as_mainfile(filepath=str(out/(out.name+'.blend')))
    bpy.ops.render.render(write_still=True)
    scene.camera=side; scene.render.filepath=str(out/'perspective.png'); bpy.ops.render.render(write_still=True); scene.camera=top
    for label,frames in clips:
        folder=out/label; folder.mkdir(exist_ok=True); scene.timeline_markers.new(label,frame=frames[0])
        if '--only-clip' in sys.argv and label!=sys.argv[sys.argv.index('--only-clip')+1]: continue
        for frame in frames:
            scene.frame_set(frame); scene.render.filepath=str(folder/f'{frame:03}.png'); bpy.ops.render.render(write_still=True)
    # Save camera/markers and editable rig in idle, not the last rendered pose.
    scene.frame_set(501); scene.render.filepath=str(out/'idle.png'); bpy.ops.wm.save_as_mainfile(filepath=str(out/(out.name+'.blend')))
    import json
    (out/'clips.json').write_text(json.dumps({'fps':24,'forward':'+X','preview':'head up','ortho':scale,'clips':[{'name':name,'frames':list(frames)} for name,frames in clips]},indent=2),encoding='utf-8')
    (out/'.rendering').unlink()
    print('MODEL AND CLIPS COMPLETE '+out.name,flush=True)
def segment(name,a,b,r1,r2,mat):
    a,b=Vector(a),Vector(b)
    bpy.ops.mesh.primitive_cone_add(vertices=24,radius1=r1,radius2=r2,depth=(b-a).length,location=(a+b)/2)
    o=bpy.context.object; o.name=name; o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler(); o.data.materials.append(mat)
    bevel=o.modifiers.new('Bordas arredondadas','BEVEL'); bevel.width=.022; bevel.segments=3
    for p in o.data.polygons: p.use_smooth=True
    return o
def line(name,pts,radius,mat,smooth_curve=False):
    c=bpy.data.curves.new(name,'CURVE'); c.dimensions='3D'; c.bevel_depth=radius; c.bevel_resolution=3
    s=c.splines.new('NURBS' if smooth_curve else 'POLY'); s.points.add(len(pts)-1)
    for p,co in zip(s.points,pts): p.co=(*co,1)
    if smooth_curve: s.order_u=min(3,len(pts)); s.use_endpoint_u=True
    o=bpy.data.objects.new(name,c); scene.collection.objects.link(o); o.data.materials.append(mat); return o
def ctrl(name,loc):
    o=bpy.data.objects.new(name,None); scene.collection.objects.link(o); o.location=loc; o.empty_display_size=.10; return o
def parent_keep(o,parent):
    bpy.context.view_layer.update(); world=o.matrix_world.copy(); o.parent=parent; o.matrix_world=world
def unify(parts,name):
    print('MERGING '+name,flush=True)
    bpy.ops.object.select_all(action='DESELECT')
    for o in parts: o.select_set(True)
    bpy.context.view_layer.objects.active=parts[0]; bpy.ops.object.join()
    o=bpy.context.object; o.name=name
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    remesh=o.modifiers.new('Unir volumes','REMESH'); remesh.mode='VOXEL'; remesh.voxel_size=.035
    bpy.ops.object.modifier_apply(modifier=remesh.name)
    smooth=o.modifiers.new('Suavizar unioes','SMOOTH'); smooth.factor=.65; smooth.iterations=6
    bpy.ops.object.modifier_apply(modifier=smooth.name)
    sub=o.modifiers.new('Superficie organica','SUBSURF'); sub.levels=1
    for p in o.data.polygons: p.use_smooth=True
    return o
