"""Build each chosen original creature independently; FK limbs and editable ability controls."""
from pathlib import Path
import sys,math
sys.path.insert(0,str(Path(__file__).resolve().parent))
from common import *
kind=sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else 'Charger'
OUT=Path(__file__).resolve().parent/kind
root=ctrl('CTRL - Corpo',(0,0,0)); limbs=[]; special=[]
bone=material('Osso envelhecido',(.21,.17,.105),(.56,.49,.35),.78,.14)
rust_skin=material('Pele ferrugem',(.065,.031,.015),(.25,.14,.067),.86,.22)
pale_skin=material('Pele cinza terrosa',(.10,.087,.068),(.34,.29,.225),.86,.16)
fungus=material('Bolsas de esporos',(.10,.11,.05),(.35,.32,.15),.75,.13)
def attached(o,parent=root): parent_keep(o,parent); return o
def mass(name,parts,mat=skin):
    return attached(unify([sphere(name+str(i),p,s,mat) for i,(p,s) in enumerate(parts)],name))
def head(name,pos,size,mat):
    c=ctrl('CTRL - '+name,pos); attached(c)
    p=Vector(pos); sx,sy,sz=size
    parts=[sphere(name+' cranio',p,size,mat),sphere(name+' focinho',p+Vector((sx*.70,0,-sz*.35)),(sx*.65,sy*.66,sz*.50),mat)]
    attached(unify(parts,name+' continuo'),c)
    for side in [-1,1]:
        attached(sphere(name+' olho',p+Vector((sx*.75,side*sy*.65,sz*.22)),(.022,.025,.018),eye_mat),c)
    return c
def cup(name,pos,rx,ry,depth,mat=lip_mat,parent=root):
    c=ctrl('CTRL - '+name,pos); attached(c,parent); verts=[]; faces=[]; rings=8; count=32
    for j in range(rings+1):
        r=.05+.95*j/rings
        for i in range(count):
            t=i*math.tau/count; verts.append((rx*r*math.cos(t),ry*r*math.sin(t),depth*r*r))
    for j in range(rings):
        for i in range(count):
            a=j*count+i; b=j*count+(i+1)%count; faces.append((a,a+count,b+count,b))
    mesh=bpy.data.meshes.new(name); mesh.from_pydata(verts,[],faces); mesh.update()
    o=bpy.data.objects.new(name,mesh); scene.collection.objects.link(o); o.data.materials.append(mat); o.data.materials.append(lip_mat); o.data.materials.append(dark); o.parent=c
    for i,p in enumerate(mesh.polygons): p.use_smooth=True; p.material_index=2 if i//count<2 else 1 if i//count<5 else 0
    solid=o.modifiers.new('Parede espessa','SOLIDIFY'); solid.thickness=.028
    cap=sphere(name+' cavidade',pos,(rx*.36,ry*.36,.012),dark); attached(cap,c)
    return c
def leg(name,hip,knee,foot,a=.14,b=.09,mat=skin,digits=3):
    limb=Limb(name,hip,knee,foot,root,a,b,mat,digits); limbs.append(limb); return limb
if kind=='Charger':
    mass('Torso musculoso',[((-.08,0,.68),(.63,.34,.30)),((.23,0,.75),(.34,.48,.32)),((-.58,0,.48),(.29,.24,.23))],rust_skin)
    skull=head('Cabeca baixa',(.51,0,.66),(.23,.22,.23),rust_skin)
    for s in [-1,1]:
        attached(sphere('Ombro osseo',(.14,s*.32,.96),(.34,.22,.15),bone))
        leg('Pata dianteira '+str(s),(.23,s*.34,.72),(.29,s*.64,.43),(.65,s*.69,.07),.22,.13,rust_skin,4)
    for s in [-1,1]: leg('Pata traseira '+str(s),(-.56,s*.21,.48),(-.46,s*.37,.27),(-.80,s*.42,.06),.12,.07,rust_skin)
    for i in range(5): attached(sphere('Crista dorsal',(-.45+i*.14,0,.88),(.055,.06,.05),rust_skin))
elif kind=='Listener':
    mass('Tronco estreito',[((-.11,0,1.03),(.23,.27,.38)),((-.13,0,.61),(.22,.25,.20)),((.05,0,1.30),(.16,.16,.24))],pale_skin)
    skull=head('Cabeca inclinada',(.27,0,1.49),(.20,.19,.23),pale_skin)
    large_ear=cup('Orelha grande',(.17,-.43,1.64),.23,.35,.18,pale_skin,skull)
    small_ear=cup('Orelha pequena',(.22,.235,1.60),.055,.065,.045,pale_skin,skull)
    for s in [-1,1]: leg('Perna '+str(s),(-.10,s*.20,.64),(.13,s*.31,.33),(.02,s*.47,.06),.10,.065,pale_skin)
    for s in [-1,1]: leg('Braco '+str(s),(.03,s*.28,1.28),(.13,s*.48,.88),(.32,s*.48,.60),.085,.055,pale_skin,4)
elif kind=='Shell':
    mass('Corpo enrolado',[((-.08,0,.96),(.56,.58,.58)),((-.29,0,.57),(.37,.43,.30))],rust_skin)
    skull=head('Cabeca recolhida',(.36,0,1.30),(.20,.20,.23),pale_skin)
    vertices=[]; faces=[]
    for i in range(41):
        a=math.radians(-110+220*i/40)
        for r in [.43,.70]: vertices.append((.15+r*math.cos(a),r*.94*math.sin(a),.97+.68*math.cos(a)))
    for i in range(40): faces.append((i*2,i*2+1,i*2+3,i*2+2))
    mesh=bpy.data.meshes.new('Crescente osseo'); mesh.from_pydata(vertices,[],faces); mesh.update()
    o=bpy.data.objects.new('Crescente protetor frontal',mesh); scene.collection.objects.link(o); o.data.materials.append(bone); attached(o)
    m=o.modifiers.new('Queratina espessa','SOLIDIFY'); m.thickness=.11
    b=o.modifiers.new('Bordas arredondadas','BEVEL'); b.width=.035; b.segments=3
    for s in [-1,1]: leg('Perna '+str(s),(-.18,s*.30,.65),(.12,s*.48,.33),(.12,s*.61,.06),.18,.105,rust_skin)
    for i in range(4): attached(line('Dobras nas costas',[(-.53+i*.065,-.36,.93),(-.61+i*.065,0,1.18),(-.53+i*.065,.36,.93)],.018,skin))
elif kind=='Herald':
    mass('Tronco ressonante',[((-.10,0,1.04),(.28,.35,.40)),((-.17,0,.58),(.24,.27,.20))],rust_skin)
    skull=head('Cabeca pequena',(.36,0,1.37),(.17,.17,.21),pale_skin)
    voice=cup('Boca de grito',(.63,0,1.40),.10,.08,.08,lip_mat,skull)
    ribs=ctrl('CTRL - Costelas',(0,0,0)); attached(ribs)
    for x in [.24,-.04,-.32]:
        attached(line('Arco de costela',[(x,-.34,1.00),(x-.03,-.47,1.45),(x-.09,-.28,1.98),(x-.12,0,2.07),(x-.09,.28,1.98),(x-.03,.47,1.45),(x,.34,1.00)],.067,bone,True),ribs)
    for s in [-1,1]: leg('Perna '+str(s),(-.18,s*.22,.62),(.10,s*.33,.34),(.03,s*.44,.06),.13,.075,rust_skin)
    for s in [-1,1]: leg('Braco '+str(s),(.02,s*.34,1.28),(.16,s*.54,.88),(.27,s*.53,.57),.11,.065,rust_skin,4)
elif kind=='Devourer':
    grips=[]
    mass('Animal colhedor',[((-.19,0,.72),(.61,.31,.28)),((-.62,0,.65),(.27,.32,.28))],pale_skin)
    neck=ctrl('CTRL - Pescoco',(0,0,0)); attached(neck)
    attached(line('Pescoco curvo',[(.25,0,.73),(.41,0,.98),(.70,0,1.04),(.96,0,.86),(1.00,0,.57)],.105,pale_skin,True),neck)
    skull=head('Cabeca compacta',(1.00,0,.52),(.17,.14,.16),pale_skin); parent_keep(skull,neck)
    for s in [-1,1]:
        grip=attached(sphere('Labio coletor',(1.10,s*.057,.38),(.083,.040,.075),bone),skull); grips.append((s,grip))
        leg('Pata coletora '+str(s),(.24,s*.25,.74),(.23,s*.39,.34),(.40,s*.46,.055),.09,.065,pale_skin,0)
        for j in [-1,0,1]:
            o=sphere('Apoio achatado',(.49,s*.46+j*.07,.057),(.12,.054,.025),pale_skin); limbs[-1].bind(o,'Inferior',root)
    for s in [-1,1]: leg('Pata traseira '+str(s),(-.58,s*.22,.62),(-.52,s*.34,.31),(-.70,s*.40,.055),.13,.075,pale_skin)
    for i in range(7): attached(sphere('Textura dorsal',(-.46+i*.12,0,.98),(.075,.08,.025),rust_skin))
elif kind=='Spore':
    mass('Animal esporeiro',[((-.12,0,.61),(.59,.32,.26)),((-.50,0,.58),(.24,.29,.22))],rust_skin)
    skull=head('Cabeca esporeiro',(.56,0,.48),(.20,.15,.16),pale_skin)
    sacks=[]
    for i,pos in enumerate([(.08,-.25,1.01),(.08,.25,1.01),(-.37,0,1.17)]):
        c=ctrl('CTRL - Bolsa '+str(i),pos); attached(c); sacks.append(c)
        attached(sphere('Bolsa dorsal',pos,(.31,.29,.32),fungus),c)
        cup('Poro superior',(pos[0],pos[1],pos[2]+.324),.09,.09,.045,lip_mat,c)
        guide=ctrl('Origem esporos '+str(i),(pos[0],pos[1],pos[2]+.34)); attached(guide,c)
    for x in [.33,-.10,-.49]:
        for s in [-1,1]: leg('Pata '+str(x)+' '+str(s),(x,s*.24,.61),(x+.01,s*.47,.32),(x+.14,s*.60,.06),.09,.060,rust_skin)
else: raise ValueError('Unknown creature '+kind)

walking(limbs,.20 if kind not in ['Spore','Shell'] else .13)
for f in range(1,34):
    if kind=='Shell': root.rotation_euler.x=math.radians(2)*math.sin((f-1)/32*math.tau)
    else: root.rotation_euler.x=0
    key(root,f)

def stand(frame):
    root.location=(0,0,0); root.rotation_euler=(0,0,0); root.scale=(1,1,1); key(root,frame)
    for limb in limbs: limb.pose(frame)
    skull.rotation_euler=(0,0,0); key(skull,frame)
def melee():
    for f in range(301,321):
        stand(f); t=(f-301)/19
        thrust=.14*smooth(t/.35) if t<.35 else .14*(1-smooth((t-.35)/.65))
        skull.rotation_euler.y=thrust*2; key(skull,f)
        root.location.x=thrust*.3; key(root,f)
        if kind in ['Listener','Herald']:
            for limb in limbs:
                if limb.name=='Braco -1': limb.pose(f,limb.foot+Vector((thrust*1.8,0,thrust*.2)))
        if kind=='Devourer':
            for side,grip in grips: grip.location.y=side*(.057+.04*math.sin(math.pi*t)); key(grip,f)
    return ('Attack',range(301,321))
clips=[('Walk',range(1,33))]
if kind=='Charger':
    for f in range(101,233):
        stand(f)
        if f<=172: load=smooth((f-101)/71); root.rotation_euler.y=math.radians(9)*load; root.location.x=-.055*load
        elif f<=184:
            root.rotation_euler.y=math.radians(12); root.location.x=.07
            for i,limb in enumerate(limbs):
                p=((f-173)/8+(i%2)*.5+((i//2)%2)*.5)%1
                x=.20-.40*p/.6 if p<.6 else -.20+.40*(p-.6)/.4
                lift=0 if p<.6 else .07*math.sin(math.pi*(p-.6)/.4)
                limb.pose(f,limb.foot+Vector((x,0,lift)))
        else: root.rotation_euler.y=math.radians(12)*(1-smooth((f-185)/47))
        key(root,f)
        if f<=172 or f>=185:
            bpy.context.view_layer.update(); inverse=root.matrix_world.inverted()
            for limb in limbs: limb.pose(f,inverse@limb.foot)
    clips.append(('Charge',range(101,233,2)))
elif kind=='Listener':
    for f in range(101,141):
        stand(f); t=(f-101)/39
        skull.rotation_euler.z=math.radians(32)*math.sin(t*math.tau); key(skull,f)
        large_ear.rotation_euler.x=math.radians(10)*math.sin(t*math.tau*2); key(large_ear,f)
    clips.append(('Listen',range(101,141)))
elif kind=='Shell':
    for f in range(101,121):
        stand(f); t=(f-101)/19
        root.rotation_euler.y=math.radians(11)*math.sin(math.pi*t); root.location.x=.13*math.sin(math.pi*t); key(root,f)
        bpy.context.view_layer.update(); inverse=root.matrix_world.inverted()
        for limb in limbs: limb.pose(f,inverse@limb.foot)
    clips.append(('Ram',range(101,121)))
elif kind=='Herald':
    for f in range(101,173):
        stand(f)
        if f<=148: load=smooth((f-101)/47)
        elif f<=160: load=1-.25*math.sin((f-148)/12*math.tau*2)
        else: load=1-smooth((f-160)/12)
        ribs.scale=(1+load*.08,1+load*.08,1); key(ribs,f)
        voice.scale=(1+load*.4,1+load*.3,1); key(voice,f)
        skull.rotation_euler.y=-math.radians(12)*load; key(skull,f)
    clips.append(('Shout',range(101,173,2)))
elif kind=='Devourer':
    for f in range(101,181):
        stand(f); t=(f-101)/79; load=smooth(t/.18) if t<.18 else 1 if t<.75 else 1-smooth((t-.75)/.25)
        neck.rotation_euler.y=math.radians(10)*load+math.radians(2)*math.sin(t*math.tau*5)*load; key(neck,f)
        for side,grip in grips: grip.location.y=side*(.057+.03*abs(math.sin(t*math.tau*5))*load); key(grip,f)
        if f>162: root.scale=(1+.08*smooth((f-162)/18),1+.10*smooth((f-162)/18),1); key(root,f)
    clips.append(('Feed',range(101,181,2)))
    for f in range(201,225):
        stand(f); grow=smooth((f-201)/23); root.scale=(1+.12*grow,1+.16*grow,1+.05*grow); key(root,f)
    clips.append(('Growth',range(201,225)))
elif kind=='Spore':
    for f in range(101,145):
        stand(f)
        if f<=129: load=smooth((f-101)/28)
        elif f<=134: load=1-1.5*smooth((f-129)/5)
        else: load=-.5*(1-smooth((f-134)/10))
        for i,c in enumerate(sacks): c.scale=(1+.15*load,1+.15*load,1+.22*load); key(c,f)
    clips.append(('Spores',range(101,145)))
clips.append(melee())
stand(501)
if kind=='Listener': large_ear.rotation_euler=(0,0,0); key(large_ear,501)
if kind=='Herald':
    for o in [ribs,voice]: o.scale=(1,1,1); key(o,501)
if kind=='Devourer':
    neck.rotation_euler=(0,0,0); key(neck,501)
    for side,grip in grips: grip.location.y=side*.057; key(grip,501)
if kind=='Spore':
    for c in sacks: c.scale=(1,1,1); key(c,501)
finish(OUT,clips,3.6 if kind=='Herald' else 3.4,1.05 if kind=='Herald' else .85)
