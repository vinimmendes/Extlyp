"""Export the approved rig for the 2D game, including equipment variants.

Run Blender --background --factory-startup --python this_file.py.
Pistol uses the existing approved renders; the other poses render from its model.
"""
import bpy, sys
from pathlib import Path
from mathutils import Vector

BASE=Path(__file__).resolve().parent
OUT=BASE/'GameExport';OUT.mkdir(exist_ok=True)

def finish(o,name,m):
    o.name=name;o.data.materials.append(m)
    return o
def box(name,loc,scale,m):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    o=finish(bpy.context.object,name,m);o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    mod=o.modifiers.new('Cantos','BEVEL');mod.width=.012;mod.segments=3
    o.modifiers.new('Normais','WEIGHTED_NORMAL')
    return o
def barrel(name,start,end,radius,m):
    a,b=Vector(start),Vector(end)
    bpy.ops.mesh.primitive_cylinder_add(vertices=16,radius=radius,depth=(b-a).length,location=(a+b)/2)
    o=finish(bpy.context.object,name,m);o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()
    return o

def sphere(name,loc,scale,m):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24,ring_count=12,location=loc)
    o=finish(bpy.context.object,name,m);o.scale=scale
    for face in o.data.polygons:face.use_smooth=True
    return o

def sleeve(name,a,b,radius,m):
    a,b=Vector(a),Vector(b)
    o=sphere(name,(a+b)/2,(radius,radius,(b-a).length/2+.03),m)
    o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()
    return o

gun_names=('Ferrolho pistola','Empunhadura pistola','Mira traseira','Massa de mira','Janela superior','Ranhura ferrolho')
arm_names=('Braco fixo','Antebraco fixo','Mao fixa','Ombro tecido','Cotovelo tecido','Remendo cotovelo','Ponto remendo','Costura ombro','Dobra manga')
variants=['Unarmed'] if '--unarmed-only' in sys.argv else ['Unarmed','Mosin','Shotgun']
for variant in variants:
    bpy.ops.wm.open_mainfile(filepath=str(BASE/'VisualV1/personagem-rustico.blend'))
    scene=bpy.context.scene
    for o in scene.objects:
        if o.name.startswith(gun_names):o.hide_render=True
    steel=bpy.data.materials['Aco escurecido'];leather=bpy.data.materials['Couro marrom gasto']
    wood=bpy.data.materials['Mochila lona caqui'].copy();wood.name='Madeira arma'
    ramp=next(n for n in wood.node_tree.nodes if n.type=='VALTORGB')
    ramp.color_ramp.elements[0].color=(.055,.026,.010,1)
    ramp.color_ramp.elements[1].color=(.22,.105,.035,1)
    if variant=='Unarmed':
        # Resting arms beside the body; leg rig/keys remain untouched.
        for o in scene.objects:
            if o.name.startswith(arm_names):o.hide_render=True
        jacket=bpy.data.materials['Jaqueta oliva - tecido gasto'];skin=bpy.data.materials['Pele']
        for side in [-1,1]:
            shoulder=(.04,side*.31,1.57);elbow=(.02,side*.39,1.29);wrist=(.07,side*.38,1.12)
            sphere('Ombro repouso',shoulder,(.14,.14,.15),jacket)
            sleeve('Manga repouso',shoulder,elbow,.115,jacket)
            sphere('Cotovelo repouso',elbow,(.11,.11,.10),jacket)
            sleeve('Antebraco repouso',elbow,wrist,.095,jacket)
            sphere('Punho repouso',wrist,(.092,.092,.032),leather)
            sphere('Mao repouso',(.07,side*.38,1.075),(.072,.073,.072),skin)
    else:
        # Both support hands meet the receiver/fore-end; stock extends toward shoulder.
        box('Coronha',(.48,0,1.40),(.43,.072,.07),wood)
        box('Receptor',(.71,0,1.43),(.22,.076,.065),steel)
        box('Guarda mao',(.81,0,1.40),(.20,.082,.057),wood)
        if variant=='Mosin':
            barrel('Cano rifle',(.76,0,1.44),(1.15,0,1.44),.023,steel)
            barrel('Alavanca ferrolho',(.69,-.025,1.455),(.69,-.092,1.445),.010,steel)
            box('Manopla',(.69,-.095,1.445),(.031,.027,.028),steel)
            box('Mira rifle',(1.10,0,1.474),(.022,.022,.020),steel)
        else:
            for y in [-.027,.027]:barrel('Cano escopeta',(.76,y,1.445),(1.10,y,1.445),.025,steel)
            box('Trava escopeta',(.68,0,1.472),(.045,.018,.012),steel)
    scene.camera=bpy.data.objects['Camera jogo - topo']
    scene.render.resolution_x=256;scene.render.resolution_y=256
    scene.cycles.samples=24
    (OUT/variant).mkdir(exist_ok=True)
    for frame in range(1,33):
        scene.frame_set(frame);scene.render.filepath=str(OUT/variant/f'{frame:03}.png')
        bpy.ops.render.render(write_still=True)
    print('EXPORTED',variant,flush=True)
