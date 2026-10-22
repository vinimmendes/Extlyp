"""One shared worn travel chest; normal opening and hidden/revealed crawling mimic."""
from pathlib import Path
import sys,random,math
sys.path.insert(0,str(Path(__file__).resolve().parent))
from common import *
OUT=Path(__file__).resolve().parent/('Mimic' if '--mimic' in sys.argv else 'Chest')
wood=material('Madeira gasta',(.055,.037,.022),(.30,.22,.13),.88,.28)
noise=next(n for n in wood.node_tree.nodes if n.type=='TEX_NOISE')
tex=wood.node_tree.nodes.new('ShaderNodeTexCoord'); stretch=wood.node_tree.nodes.new('ShaderNodeVectorMath'); stretch.operation='MULTIPLY'; stretch.inputs[1].default_value=(20,1.2,5)
wood.node_tree.links.new(tex.outputs['Generated'],stretch.inputs[0]); wood.node_tree.links.new(stretch.outputs[0],noise.inputs['Vector'])
leather=material('Couro ressecado',(.025,.012,.007),(.12,.065,.030),.92,.35)
rust=material('Ferro oxidado',(.018,.016,.013),(.11,.073,.038),.72,.14)
rust.node_tree.nodes.get('Principled BSDF').inputs['Metallic'].default_value=.45
inner=material('Madeira interna empoeirada',(.035,.020,.010),(.13,.085,.04),.92,.2)
root=ctrl('CTRL - Corpo bau',(0,0,0)); lid=ctrl('CTRL - Tampa',(-.46,0,.59)); parent_keep(lid,root)
base_parts=[box('Fundo',(0,0,.08),(.98,1.54,.11),inner)]
for x in [-.475,.475]:
    for i in range(3): base_parts.append(box('Tabua parede frontal traseira',(x,0,.205+i*.145),(.085,1.54,.14),wood,.012))
for y in [-.745,.745]:
    for i in range(3): base_parts.append(box('Tabua parede lateral',(0,y,.205+i*.145),(.89,.085,.14),wood,.012))
def arch(x): return .59+.18*math.sqrt(max(0,1-(x/.46)**2))
def curved(name,x0,x1,y0,y1,offset,mat):
    vertices=[]; steps=8
    for i in range(steps+1):
        x=x0+(x1-x0)*i/steps; z=arch(x)+offset
        vertices.extend([(x,y0,z),(x,y1,z),(x,y0,z-.035),(x,y1,z-.035)])
    faces=[]
    for i in range(steps):
        a=i*4; b=a+4
        faces.extend([(a,a+1,b+1,b),(a+2,b+2,b+3,a+3),(a,b,b+2,a+2),(a+1,a+3,b+3,b+1)])
    faces.extend([(0,2,3,1),(steps*4,steps*4+1,steps*4+3,steps*4+2)])
    mesh=bpy.data.meshes.new(name); mesh.from_pydata(vertices,[],faces); mesh.update()
    o=bpy.data.objects.new(name,mesh); scene.collection.objects.link(o); o.data.materials.append(mat)
    bevel=o.modifiers.new('Bordas suaves','BEVEL'); bevel.width=.006; bevel.segments=2; o.modifiers.new('Normais','WEIGHTED_NORMAL'); return o
lid_parts=[]
for y in [-.755,.755]:
    vertices=[(-.46,y,.555)]+[(-.46+i*.92/16,y,arch(-.46+i*.92/16)) for i in range(17)]+[(.46,y,.555)]
    mesh=bpy.data.meshes.new('Perfil lateral arqueado'); mesh.from_pydata(vertices,[],[tuple(range(len(vertices)))]); mesh.update()
    o=bpy.data.objects.new('Lateral da tampa fechada',mesh); scene.collection.objects.link(o); o.data.materials.append(wood)
    mod=o.modifiers.new('Espessura madeira','SOLIDIFY'); mod.thickness=.045
    lid_parts.append(o)
for i in range(6):
    x0=-.46+i*.92/6+.003; x1=-.46+(i+1)*.92/6-.003
    lid_parts.append(curved('Tabua tampa arqueada',x0,x1,-.765,.765,0,wood))
for y in [-.43,.43]:
    lid_parts.append(curved('Cinta couro tampa',-.46,.46,y-.060,y+.060,.038,leather))
    base_parts.append(box('Cinta couro frente',(.526,y,.34),(.028,.125,.48),leather,.008))
    base_parts.append(box('Cinta couro traseira',(-.526,y,.34),(.028,.125,.48),leather,.008))
    for a in [-1,1]:
        base_parts.append(box('Fivela vertical',(.548,y+a*.063,.48),(.020,.015,.125),rust,.006))
        base_parts.append(box('Fivela horizontal',(.548,y,.48+a*.058),(.020,.135,.015),rust,.006))
    base_parts.append(box('Pino fivela',(.558,y,.48),(.015,.011,.11),rust,.004))
    for x in [-.37,-.20,0,.20,.37]: lid_parts.append(sphere('Rebite cinta',(x,y,arch(x)+.049),(.015,.018,.009),rust))
for x in [-.37,.37]:
    for y in [-.665,.665]:
        lid_parts.append(curved('Canto couro tampa',x-.07,x+.07,y-.08,y+.08,.018,leather))
        lid_parts.append(sphere('Rebite canto',(x,y,arch(x)+.03),(.014,.016,.009),rust))
        base_parts.append(box('Canto couro caixa',(x*1.36,y,.15),(.04,.145,.16),leather,.018))
base_parts.append(box('Fechadura central',(.531,0,.35),(.019,.145,.16),rust,.010))
base_parts.append(sphere('Orificio chave',(.543,0,.35),(.006,.018,.025),dark))
for y in [-.4,.4]: base_parts.append(box('Dobradiça real',(-.51,y,.56),(.070,.18,.045),rust,.010))
for y in [-.80,.80]:
    base_parts.append(line('Alca lateral',[(-.12,y,.37),(-.12,y*1.04,.29),(.12,y*1.04,.29),(.12,y,.37)],.014,rust))
for i in range(26):
    x=random.uniform(-.41,.41); y=random.uniform(-.65,.62); length=random.uniform(.04,.20)
    lid_parts.append(line('Risco tampa',[(x,y,arch(x)+.006),(x+.002,y+length,arch(x)+.006)],.0025,inner))
for o in base_parts: parent_keep(o,root)
for o in lid_parts: parent_keep(o,lid)
mimic='--mimic' in sys.argv
if not mimic:
    for frame in list(range(101,121))+list(range(201,221))+[501]:
        t=smooth((frame-101)/15) if frame<200 else 1-smooth((frame-201)/15) if frame<300 else 0
        lid.rotation_euler.y=-math.radians(110)*t; key(lid,frame)
    finish(OUT,[('Open',range(101,121)),('Close',range(201,221))],3.4,.45)
else:
    limbs=[]
    for side in [-1,1]: limbs.append(Limb('Braco '+str(side),(.06,side*.67,.35),(.15,side*.95,.24),(.68,side*.96,.06),root,.14,.095,skin,4))
    mouth_mat=dark.copy(); mouth_mat.name='Interior fosco do imitador'; mouth_mat.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.95
    organs=[sphere('Interior boca',(0,0,.41),(.37,.64,.028),mouth_mat),sphere('Lingua',(.19,0,.445),(.20,.32,.035),tongue_mat)]
    upper_teeth=[]
    for y in [-.58,-.38,-.18,.02,.22,.42,.60]:
        organs.append(segment('Dente inferior',(.42,y,.50),(.405,y,.61),.034,.007,teeth_mat))
        upper_teeth.append(segment('Dente tampa',(.42,y,arch(.42)-.05),(.41,y,arch(.42)-.14),.031,.006,teeth_mat))
    for o in organs: parent_keep(o,root)
    for o in upper_teeth: parent_keep(o,lid)
    walking(limbs,.19)
    for f in range(1,34): lid.rotation_euler.y=-math.radians(57); key(lid,f)
    for f in range(101,119):
        t=smooth((f-101)/17); lid.rotation_euler.y=-math.radians(57)*t; key(lid,f)
        for limb in limbs:
            limb.pose(f,Vector((.18,limb.foot.y*.62,.32)).lerp(limb.foot,t))
    for f in range(201,221):
        t=(f-201)/19
        angle=57+25*smooth(t/.40) if t<.4 else 82-76*smooth((t-.4)/.22) if t<.62 else 6+51*smooth((t-.62)/.38)
        lid.rotation_euler.y=-math.radians(angle); key(lid,f)
        for limb in limbs: limb.pose(f)
    for f,visible in [(1,True),(101,False),(103,True),(201,True),(501,True),(601,False)]:
        for o in organs+upper_teeth: o.hide_render=not visible; o.keyframe_insert(data_path='hide_render',frame=f)
        for limb in limbs: limb.visibility(f,visible)
    for f,angle in [(501,57),(601,0)]:
        lid.rotation_euler.y=-math.radians(angle); key(lid,f)
        for limb in limbs: limb.pose(f)
    finish(OUT,[('Walk',range(1,33)),('Reveal',range(101,119)),('Bite',range(201,221)),('Disguise',[601])],3.4,.45)
