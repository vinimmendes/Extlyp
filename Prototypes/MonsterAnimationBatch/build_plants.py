"""The chosen woody Root Knot family. Shared seed geometry for maturation and minion."""
from pathlib import Path
import sys,math
sys.path.insert(0,str(Path(__file__).resolve().parent))
from common import *
kind=sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else 'Mother'
OUT=Path(__file__).resolve().parent/kind
root=ctrl('CTRL - Planta',(0,0,0)); halves=[]; leaves=[]; limbs=[]
wood=material('Raiz lenhosa',(.035,.021,.010),(.21,.135,.062),.89,.23)
leaf_mat=material('Folhas oliva',(.025,.035,.012),(.18,.20,.075),.86,.10)
sap=material('Seiva ambar',(.11,.045,.005),(.50,.30,.052),.35,.06)
sap.node_tree.nodes.get('Principled BSDF').inputs['Metallic'].default_value=.12
def attach(o,parent=root): parent_keep(o,parent); return o
def leaf(name,a,b,width,parent=root):
    a,b=Vector(a),Vector(b); middle=a.lerp(b,.50); side=(b-a).cross(Vector((0,0,1))).normalized()*width
    verts=[a,middle+side,b,middle-side,middle+Vector((0,0,.045))]
    mesh=bpy.data.meshes.new(name); mesh.from_pydata(verts,[],[(0,1,4),(1,2,4),(2,3,4),(3,0,4)]); mesh.update()
    o=bpy.data.objects.new(name,mesh); scene.collection.objects.link(o); o.data.materials.append(leaf_mat)
    solid=o.modifiers.new('Espessura folha','SOLIDIFY'); solid.thickness=.012
    sub=o.modifiers.new('Folha macia','SUBSURF'); sub.levels=1; attach(o,parent); leaves.append(o)
    attach(line('Nervura central',[a,middle+Vector((0,0,.045)),b],.005,wood),parent)
    return o
def foliage(radius=.5,height=.14,parent=root,count=8):
    for i in range(count):
        t=i*math.tau/count; start=Vector((.12*math.cos(t),.12*math.sin(t),height))
        end=Vector((radius*math.cos(t),radius*math.sin(t),height+.10+(i%2)*.06))
        leaf('Folha '+str(i),start,end,.13 if radius<.7 else .24,parent)
def seed(parent=root,scale=1):
    lobes=[]
    for side in [-1,1]:
        c=ctrl('CTRL - Valva lenhosa '+str(side),(0,side*.10,.27)); attach(c,parent)
        attach(sphere('Valva de semente',(0,side*.09,.44),(.22,.125,.25),wood),c); lobes.append(c)
    attach(sphere('Cavidade da semente',(.05,0,.46),(.145,.055,.17),dark),parent)
    return lobes
if kind=='Mother':
    core=ctrl('CTRL - No central',(0,0,0)); attach(core)
    attach(unify([sphere('Massa de raiz',(0,0,.37),(.42,.43,.28),wood),sphere('No elevado',(-.17,0,.63),(.28,.31,.28),wood)],'No central continuo'),core)
    maw=ctrl('CTRL - Boca elevada',(0,0,0)); attach(maw,core)
    mouth=seed(maw); maw.location.z=.32
    for i in range(4):
        t=i*math.pi/2+math.pi/4; direction=Vector((math.cos(t),math.sin(t),0))
        pts=[direction*.20+Vector((0,0,.55)),direction*.46+Vector((0,0,.70)),direction*.78+Vector((0,0,.36)),direction*1.02+Vector((0,0,.07))]
        branch=attach(line('Raiz em arco '+str(i),pts,.115,wood,True),core)
        for p,radius in zip(branch.data.splines[0].points,[1.05,.9,.65,.23]): p.radius=radius
        attach(sphere('Ponta natural da raiz',pts[-1],(.035,.035,.030),wood),core)
        attach(sphere('No na raiz',pts[2],(.16,.14,.13),wood),core)
    foliage(1.04,.46,core,10)
    bud=ctrl('CTRL - Bulbo gerado',(.84,.65,.10)); attach(bud)
    # Child meshes are local to the sprout controller, not laid out elsewhere in the scene.
    b=sphere('FX_Bulbo gerado',(.84,.65,.28),(.16,.14,.20),wood); attach(b,bud)
    for f in range(101,137):
        t=smooth((f-101)/35); core.scale=(1+.045*math.sin(t*math.pi),)*3; key(core,f)
        bud.scale=(max(.001,t),)*3; key(bud,f)
        for side,c in zip([-1,1],mouth): c.rotation_euler.x=-side*math.radians(16+12*math.sin(math.pi*t)); key(c,f)
    for f,visible in [(1,False),(101,True),(137,False),(501,False)]: b.hide_render=not visible; b.keyframe_insert(data_path='hide_render',frame=f)
    for f in range(401,433): core.scale=(1+.012*math.sin((f-401)/32*math.tau),)*3; key(core,f)
    core.scale=(1,1,1); key(core,501)
    for side,c in zip([-1,1],mouth): c.rotation_euler=( -side*math.radians(16),0,0); key(c,501)
    clips=[('Spawn',range(101,137)),('Breathe',range(401,433))]; scale=3.1
elif kind in ['AttackBulb','Minion']:
    halves=seed(); foliage(.54,.16,root,6)
    for side in [-1,1]:
        limb=Limb('Raiz perna '+str(side),(-.04,side*.12,.35),(.07,side*.24,.18),(.13,side*.34,.045),root,.075,.048,wood,0)
        limbs.append(limb)
    for i in range(3): attach(line('Raiz lateral',[(0,(i-1)*.10,.14),(-.20,(i-1)*.19,.07),(-.39,(i-1)*.24,.025)],.032,wood))
    if kind=='AttackBulb':
        for f in range(101,318):
            t=smooth((f-101)/216)
            for side,c in zip([-1,1],halves): c.rotation_euler.x=-side*math.radians(18)*t; key(c,f)
            for limb in limbs:
                end=limb.foot.lerp(Vector((.10,limb.foot.y*.6,.15)),1-t)
                limb.pose(f,end); limb.visibility(f,f>=140)
        for limb in limbs: limb.pose(501); limb.visibility(501,False)
        for c in halves: c.rotation_euler=(0,0,0); key(c,501)
        clips=[('Mature',list(range(101,318,6)))]
    else:
        walking(limbs,.11)
        for f in list(range(1,34))+[501]:
            for side,c in zip([-1,1],halves): c.rotation_euler.x=-side*math.radians(18); key(c,f)
            for limb in limbs: limb.visibility(f,True)
        for f in range(101,121):
            t=(f-101)/19; gape=18+15*smooth(t/.4) if t<.4 else 33*(1-smooth((t-.4)/.3)) if t<.7 else 18*smooth((t-.7)/.3)
            for side,c in zip([-1,1],halves): c.rotation_euler.x=-side*math.radians(gape); key(c,f)
            for limb in limbs: limb.pose(f)
        for limb in limbs: limb.pose(501)
        clips=[('Walk',range(1,33)),('Attack',range(101,121))]
    scale=1.7
elif kind=='HealingBulb':
    attach(sphere('No de cura',(0,0,.25),(.26,.25,.18),wood)); foliage(.54,.19,root,6)
    glow=ctrl('CTRL - Seiva',(0,0,.48)); attach(glow)
    attach(sphere('Seiva central',(0,0,.48),(.17,.17,.17),sap),glow)
    for i in range(3):
        t=i*math.tau/3; d=Vector((math.cos(t),math.sin(t),0))
        attach(line('Cunha de raiz',[d*.20+Vector((0,0,.23)),d*.29+Vector((0,0,.42)),d*.19+Vector((0,0,.62))],.055,wood))
    particles=[]
    for i in range(5): particles.append(sphere('FX_Seiva em viagem',(0,0,.5),(.025,.025,.025),sap))
    target=ctrl('GUIA - Destino da cura',(1.05,0,.42))
    for f in range(101,137):
        t=(f-101)/35; glow.scale=(1+.08*math.sin(t*math.tau*2),)*3; key(glow,f)
        for i,o in enumerate(particles):
            p=max(0,min(1,(t-i*.035)*1.3)); o.location=(1.05*p,.08*math.sin(p*math.pi+i),.5+.22*math.sin(p*math.pi))
            o.scale=(.018*math.sin(p*math.pi),)*3; key(o,f)
    for f in list(range(401,433))+[501]: glow.scale=(1+.02*math.sin((f-401)/32*math.tau),)*3 if f<500 else (1,1,1); key(glow,f)
    for f,visible in [(1,False),(101,True),(137,False),(501,False)]:
        for o in particles: o.hide_render=not visible; o.keyframe_insert(data_path='hide_render',frame=f)
    clips=[('Heal',range(101,137)),('Breathe',range(401,433))]; scale=2.8
else: raise ValueError(kind)
finish(OUT,clips,scale,.45)
