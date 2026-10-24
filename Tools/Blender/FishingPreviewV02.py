"""Reviewable fishing storyboard. No Unity assets or gameplay are changed."""
import bpy, math, random, json
from pathlib import Path

ROOT=Path('C:/Users/Vinni/Documents/GitHub/Extlyp')
OUT=ROOT/'Art/Fishing/Preview-v02'
NAME='Fishing-Preview-v02'
if bpy.data.scenes.get(NAME) or (OUT/'fishing-preview-v02.blend').exists():
    raise RuntimeError('Existing preview is preserved. Choose a new version before authoring again.')
OUT.mkdir(parents=True,exist_ok=True)
if bpy.context.screen and bpy.context.screen.is_animation_playing:
    bpy.ops.screen.animation_cancel(restore_frame=False)
s=bpy.data.scenes.new(NAME); bpy.context.window.scene=s
s.render.engine='BLENDER_EEVEE'; s.eevee.taa_render_samples=16
s.render.resolution_x=1280; s.render.resolution_y=720; s.render.resolution_percentage=100
s.render.fps=12; s.frame_start=1; s.frame_end=216
s.render.image_settings.file_format='PNG'; s.render.filepath=str(OUT/'frames/frame_')
s.view_settings.view_transform='Standard'
s.world=bpy.data.worlds.new('WORLD-FishingV02'); s.world.use_nodes=True
nodes=s.world.node_tree.nodes; nodes.clear()
bg=nodes.new('ShaderNodeBackground'); bg.inputs[0].default_value=(.01,.014,.01,1)
wo=nodes.new('ShaderNodeOutputWorld'); s.world.node_tree.links.new(bg.outputs[0],wo.inputs[0])

def material(name,c):
    m=bpy.data.materials.new('MAT-FV02-'+name); m.diffuse_color=(*c,1); m.use_nodes=True
    n=m.node_tree.nodes; n.clear(); em=n.new('ShaderNodeEmission'); em.name='Color'; em.inputs[0].default_value=(*c,1)
    out=n.new('ShaderNodeOutputMaterial'); m.node_tree.links.new(em.outputs[0],out.inputs[0]); return m
M={k:material(k,v) for k,v in {
    'Backdrop':(.009,.014,.011),'Panel':(.016,.022,.016),'Inset':(.010,.016,.012),
    'Button':(.034,.043,.026),'ButtonPressed':(.072,.088,.041),'Edge':(.25,.155,.079),
    'EdgeLight':(.43,.28,.14),'Text':(.79,.72,.56),'Muted':(.38,.41,.31),
    'Green':(.23,.36,.17),'DarkGreen':(.055,.095,.052),'Danger':(.55,.16,.105),
    'Water':(.018,.066,.066),'Wave':(.060,.15,.14),'Fish':(.035,.093,.086),
    'Fin':(.028,.078,.073),'FishTop':(.044,.106,.095),'Eye':(.08,.16,.14),
    'Wood':(.11,.070,.036),'Grain':(.064,.043,.025),'Red':(.65,.10,.065),
    'Cream':(.80,.72,.52),'Worm':(.40,.205,.135),'Hook':(.28,.33,.29)
}.items()}
def noise_surface(mat,dark,light,scale):
    n=mat.node_tree.nodes; a=n.new('ShaderNodeTexNoise'); a.inputs['Scale'].default_value=scale; a.inputs['Detail'].default_value=2
    r=n.new('ShaderNodeValToRGB'); r.color_ramp.elements[0].color=(*dark,1); r.color_ramp.elements[1].color=(*light,1)
    mat.node_tree.links.new(a.outputs['Fac'],r.inputs[0]); mat.node_tree.links.new(r.outputs[0],n['Color'].inputs[0])
noise_surface(M['Water'],(.013,.043,.044),(.025,.077,.073),7)
noise_surface(M['Panel'],(.015,.020,.014),(.020,.025,.017),110)
noise_surface(M['Wood'],(.073,.045,.023),(.14,.091,.047),8)
def mesh(name,vs,fs,mat):
    d=bpy.data.meshes.new('MESH-FV02-'+name); d.from_pydata(vs,[],fs); d.update()
    o=bpy.data.objects.new('GEO-FV02-'+name,d); s.collection.objects.link(o); d.materials.append(M[mat]); return o
def rect(name,x,y,w,h,z,mat):
    return mesh(name,[(x,y,z),(x+w,y,z),(x+w,y+h,z),(x,y+h,z)],[(0,1,2,3)],mat)
def path(name,coords,mat='Edge',width=.01,closed=False):
    d=bpy.data.curves.new('CURVE-FV02-'+name,'CURVE'); d.dimensions='3D'; d.bevel_depth=width; d.bevel_resolution=2
    p=d.splines.new('POLY'); p.points.add(len(coords)-1)
    for v,co in zip(p.points,coords): v.co=(*co,1)
    p.use_cyclic_u=closed
    o=bpy.data.objects.new('GEO-FV02-'+name,d); s.collection.objects.link(o); d.materials.append(M[mat]); return o
def outline(name,x,y,w,h,z,mat='Edge',width=.012):
    cut=.075
    return path(name,[(x+cut,y,z),(x+w-cut,y,z),(x+w,y+cut,z),(x+w,y+h-cut,z),(x+w-cut,y+h,z),(x+cut,y+h,z),(x,y+h-cut,z),(x,y+cut,z)],mat,width,True)
font=bpy.data.fonts.load('C:/Windows/Fonts/segoeui.ttf')
bold=bpy.data.fonts.load('C:/Windows/Fonts/segoeuib.ttf')
def text(name,body,x,y,size=.22,mat='Text',heavy=False,align='LEFT'):
    d=bpy.data.curves.new('FONT-FV02-'+name,'FONT'); d.body=body; d.font=bold if heavy else font; d.size=size; d.align_x=align
    o=bpy.data.objects.new('TXT-FV02-'+name,d); s.collection.objects.link(o); o.location=(x,y,.75); d.materials.append(M[mat]); return o
def visible(o,yes,f):
    o.hide_render=not yes; o.keyframe_insert('hide_render',frame=f)
    o.scale=(1,1,1) if yes else (0,0,0); o.keyframe_insert('scale',frame=f)
def key(o,prop,f): o.keyframe_insert(prop,frame=f)
def smooth(v): v=max(0,min(1,v)); return v*v*(3-2*v)
def mix(a,b,v): return a+(b-a)*v

rect('Backdrop',-9,-5,18,10,-.5,'Backdrop')
rect('Panel',-7.9,-4.04,15.8,8.08,-.25,'Panel')
outline('Frame',-7.9,-4.04,15.8,8.08,.01,'Edge',.022)
outline('InnerFrame',-7.82,-3.96,15.64,7.92,.02,'Edge',.006)
for x,dx in [(-7.68,1),(7.68,-1)]:
    for y,dy in [(-3.82,1),(3.82,-1)]:
        path('Corner',[(x,y+dy*.22,.1),(x,y,.1),(x+dx*.28,y,.1)],'EdgeLight',.014)
text('Title','PESCA',-7.25,3.32,.38,heavy=True)
text('Subtitle','À BEIRA DO PÍER',-5.73,3.37,.16,'Muted')
text('BuildLabel','CONCEITO V02  ·  SOMBRIA',7.22,3.37,.15,'Muted',align='RIGHT')
path('Header',[(-7.25,3.06,.1),(7.25,3.06,.1)],'Edge',.009)
rect('Water',-7.25,-2.82,9.15,5.5,0,'Water')
outline('WaterRim',-7.30,-2.87,9.25,5.6,.04,'Edge',.018)
random.seed(24)
for i in range(35):
    x=random.uniform(-7,1.6); y=random.uniform(-2,2.4)
    path('WaterDash%d'%i,[(x,y,.02),(x+random.uniform(.04,.13),y,.02)],'Wave',.003)
for i in range(13):
    x=-7.25+i*.704
    rect('Plank%d'%i,x,-2.82,.675,.58,.20,'Wood')
    path('PlankEdge%d'%i,[(x+.03,-2.28,.21),(x+.64,-2.28,.21)],'Grain',.012)
    for j in range(3):
        yy=-2.72+j*.14
        path('Grain%d-%d'%(i,j),[(x+.08,yy,.22),(x+.48,yy+.018,.22)],'Grain',.006)
    for yy in [-2.74,-2.33]:
        path('Nail',[(x+.11,yy,.23),(x+.125,yy,.23)],'Hook',.012)
text('WaterFooter','UMA ISCA. UMA CHANCE. ESPERE O PEIXE DECIDIR.',-7.2,-3.23,.16,'Muted')

X=2.47; W=4.76
rect('BaitCard',X,1.76,W,.91,.12,'Inset'); outline('BaitRim',X,1.76,W,.91,.13,'Edge',.009)
worm_icon=path('WormIcon',[(X+.22+i*.014,2.14+.11*math.sin(i/4),.2) for i in range(32)],'Worm',.027)
text('BaitName','MINHOCA',X+.83,2.28,.21,heavy=True)
text('BaitBonus','Peixe raro: 10% → 25%',X+.83,1.98,.18,'Muted')
counts=[text('BaitCount3','3 un.',X+W-.22,2.28,.19,align='RIGHT'),text('BaitCount2','2 un.',X+W-.22,2.28,.19,align='RIGHT')]
text('TensionName','TENSÃO',X,.61,.20,heavy=True)
text('TensionLegend','LIMITE',X+W,.65,.12,'Muted',align='RIGHT')
rect('TensionWell',X,.10,W,.28,.2,'Inset'); outline('TensionBorder',X-.035,.065,W+.07,.35,.22,'Edge',.008)
rect('DangerArea',X+W*.8,.11,W*.2-.02,.26,.25,'Danger')
tension=rect('Tension',0,0,1,.24,.28,'Green'); tension.location=(X+.02,.12,0)
pointer=mesh('TensionPointer',[(-.055,.09,.42),(.055,.09,.42),(0,0,.42)],[(0,1,2)],'Cream')
for i in range(11):
    xx=X+W*i/10
    path('Tick%d'%i,[(xx,-.04,.25),(xx,-.10 if i%5==0 else -.075,.25)],'Muted',.006)
text('ProgressName','CAPTURA',X,-.56,.20,heavy=True)
rect('ProgressWell',X,-.93,W,.16,.20,'Inset'); outline('ProgressRim',X-.02,-.95,W+.04,.20,.23,'Edge',.006)
progress=rect('Progress',0,0,1,.12,.27,'Green'); progress.location=(X+.02,-.91,0)

rect('ButtonShadow',X,-2.04,W,.76,.19,'Inset')
button=rect('ButtonFace',X,-1.98,W,.76,.24,'Button')
outline('ButtonBevel',X,-1.98,W,.76,.26,'EdgeLight',.016)
path('ButtonTop',[(X+.12,-1.28,.3),(X+W-.12,-1.28,.3)],'EdgeLight',.008)
path('ButtonBottom',[(X+.12,-1.91,.3),(X+W-.12,-1.91,.3)],'Inset',.012)
for j in range(4):
    path('Spool%d'%j,[(X+.33,-1.47-j*.065,.4),(X+.63,-1.47-j*.065,.4)],'Text',.014)
path('SpoolSides',[(X+.34,-1.43,.4),(X+.29,-1.49,.4),(X+.29,-1.72,.4),(X+.35,-1.77,.4),(X+.62,-1.77,.4),(X+.67,-1.70,.4),(X+.74,-1.78,.4),(X+.83,-1.69,.4)],'Text',.013)
button_texts=[text('Button'+str(i),label,X+W*.57,-1.75,.26,heavy=True,align='CENTER') for i,label in enumerate(['LANÇAR ANZOL','AGUARDE','FISGAR','RECOLHER','LANÇAR NOVAMENTE'])]
help_texts=[text('Help'+str(i),label,X,-2.43,.175,'Muted') for i,label in enumerate(['Minhoca selecionada · uso na mordida','Observe a boia. Espere a mordida.','Pressione agora para fisgar.','Segure para recolher · solte para aliviar','Peixe guardado. Novo lançamento é manual.'])]
outline('Cancel',X+W-1.63,-3.30,1.63,.46,.25,'Edge',.008)
cancel=text('Cancel','Cancelar',X+W-.82,-3.15,.18,align='CENTER')
footer=text('Footer','PRÉVIA DE ARTE E MOVIMENTO · SEM INTEGRAÇÃO UNITY',-7.25,-3.69,.14,'Muted')

states=[('ESCOLHA A ISCA','Minhoca favorece visitantes raros.'),('LANÇANDO','Anzol na água.'),('AGUARDE','Um peixe pode se aproximar.'),('APROXIMANDO','Ele está investigando a isca.'),('NÃO MORDEU','A isca continua no anzol.'),('FISGUE!','A boia afundou. Puxe agora!'),('RECOLHA','Aproveite a pausa do peixe.'),('CEDA LINHA!','Arrancada! Alivie a tensão.'),('CAPTURADO','Peixe raro · guardado na mochila.')]
headings=[]; descriptions=[]
for i,(title,desc) in enumerate(states):
    headings.append(text('State%d'%i,title,X,1.25,.30,'Danger' if i==7 else 'Green' if i==8 else 'Text',True))
    descriptions.append(text('Description%d'%i,desc,-7.02,2.30,.18,'Text'))

fish=bpy.data.objects.new('ARM-FV02-Fish',None); s.collection.objects.link(fish)
vs=[]; fs=[]
for x,w in [(-.49,.018),(-.33,.09),(-.09,.18),(.19,.175),(.39,.11),(.51,.025)]:
    for j in range(16):
        a=j*math.tau/16; vs.append((x,w*math.cos(a),.06*w*math.sin(a)))
for i in range(5):
    for j in range(16): fs.append((i*16+j,i*16+(j+1)%16,(i+1)*16+(j+1)%16,(i+1)*16+j))
fs.extend([tuple(range(15,-1,-1)),tuple(range(80,96))])
body=mesh('FishBody',vs,fs,'Fish'); body.parent=fish
body.shape_key_add(name='Basis'); left=body.shape_key_add(name='TailLeft'); right=body.shape_key_add(name='TailRight')
for a,b in zip(left.data,right.data):
    bend=.12*max(0,-a.co.x/.49)**2; a.co.y+=bend; b.co.y-=bend
tailpivot=bpy.data.objects.new('ARM-FV02-Tail',None); s.collection.objects.link(tailpivot); tailpivot.parent=fish; tailpivot.location=(-.43,0,0)
tail=mesh('FishTail',[(.04,0,0),(-.31,.18,0),(-.24,0,0),(-.31,-.18,0)],[(0,1,2,3)],'Fin'); tail.parent=tailpivot
fin=mesh('FishFin',[(-.05,.1,.02),(-.16,.27,.02),(.20,.14,.02)],[(0,1,2)],'Fin'); fin.parent=fish
spine=path('FishSpine',[(-.34,0,.04),(-.08,.025,.04),(.22,.02,.04),(.41,0,.04)],'FishTop',.025); spine.parent=fish
eye=path('FishEye',[(.355,.075,.045),(.365,.075,.045)],'Eye',.013); eye.parent=fish
boia=bpy.data.objects.new('ARM-FV02-Bobber',None); s.collection.objects.link(boia)
for name,y,mat in [('White',-.042,'Cream'),('Red',.043,'Red')]:
    bpy.ops.mesh.primitive_uv_sphere_add(segments=20,ring_count=10,radius=1)
    o=bpy.context.object; o.name='GEO-FV02-Bobber'+name; o.parent=boia; o.location=(0,y,0); o.scale=(.075,.063,.045); o.data.materials.append(M[mat])
stem=path('BobberStem',[(0,.08,0),(0,.19,0)],'Cream',.009); stem.parent=boia
line=path('Line',[(-6.25,-2.4,.38),(-4,-.6,.38),(-2.4,.4,.38)],'Cream',.006)
hook=path('Hook',[(-.06,.10,.09),(-.06,-.06,.09),(-.02,-.10,.09),(.04,-.09,.09),(.07,-.04,.09),(.04,-.02,.09)],'Hook',.009)
worm=path('WormOnHook',[(-.10,-.02,.10),(-.06,.025,.10),(-.01,-.025,.10),(.035,.015,.10),(.07,-.04,.10)],'Worm',.018)
rings=[]
for j in range(3):
    rings.append(path('Ring%d'%j,[(math.cos(k*math.tau/48),math.sin(k*math.tau/48),.18) for k in range(48)],'Wave',.007,True))
card=rect('ResultCard',-4.8,-.65,4.4,1.9,.55,'Panel'); cardrim=outline('ResultRim',-4.8,-.65,4.4,1.9,.57,'EdgeLight',.012)
resultlabels=[text('ResultName','PEIXE RARO',-2.6,.63,.31,'Text',True,'CENTER'),text('ResultStored','Guardado na mochila',-2.6,.14,.23,'Green',align='CENTER'),text('ResultBait','1 minhoca utilizada',-2.6,-.27,.18,'Muted',align='CENTER')]
choices=[]
for i,(name,label) in enumerate([('NoBait','Sem isca'),('Worm','Minhoca')]):
    xx=-5.9+i*3.2
    choices.extend([rect('Choice'+name,xx,-1.30,2.8,.65,.52,'Button' if i==0 else 'DarkGreen'),outline('ChoiceRim'+name,xx,-1.30,2.8,.65,.54,'Edge' if i==0 else 'EdgeLight'),text('Choice'+name,label,xx+1.4,-1.09,.23,align='CENTER')])
for o in s.objects:
    if o.type=='FONT': o.data.size*=1.45

timeline=[]
for f in range(1,217):
    t=(f-1)/12
    bx=-2.4; by=.35+.025*math.sin(t*3); fx=-6.6; fy=1.15; angle=0; fishscale=0; showfloat=t>=1.15; caught=t>=16.35
    tensionvalue=0; prog=0; state=0; btn=0; helpidx=0
    if t<1.15: pass
    elif t<1.75:
        q=smooth((t-1.15)/.6); bx=mix(-6.25,-2.4,q); by=mix(-2.35,.35,q)+.85*math.sin(q*math.pi); state=1; btn=1; helpidx=1
    elif t<3.0: state=2; btn=1; helpidx=1
    elif t<4.35:
        q=smooth((t-3)/1.35); fx=mix(-6.55,-3.06,q); fy=mix(1.30,.35,q); angle=-.15; fishscale=.84*min(1,(t-3)/.22); state=3; btn=1; helpidx=1
    elif t<4.95:
        fx=-3.06+.025*math.sin(t*9); fy=.35+.045*math.sin(t*6); angle=.15*math.sin(t*8); fishscale=.84; state=3; btn=1; helpidx=1
    elif t<5.85:
        q=smooth((t-4.95)/.9); fx=mix(-3.06,-6.55,q); fy=mix(.35,-.85,q); angle=math.pi+q*.25; fishscale=.84*min(1,(5.85-t)/.2); state=4; btn=1; helpidx=1
    elif t<7.15: state=2; btn=1; helpidx=1
    elif t<8.35:
        q=smooth((t-7.15)/1.2); fx=mix(-6.55,-2.96,q); fy=mix(-.70,.35,q); angle=.13; fishscale=1.03*min(1,(t-7.15)/.2); state=3; btn=1; helpidx=1
    elif t<8.95:
        fx=-2.96+.025*math.sin(t*8); fy=.35+.035*math.sin(t*11); angle=.12*math.sin(t*6); fishscale=1.03; state=3; btn=1; helpidx=1
    elif t<9.65:
        fx=-2.96; fy=.35; fishscale=1.03; state=5; btn=2; helpidx=2
    elif t<15.8:
        u=t-9.65; cycle=u%1.8; pulling=cycle>.9
        tensionvalue=.25+.60*smooth(cycle/.9) if cycle<.9 else .85-.57*smooth((cycle-.9)/.9)
        state=7 if tensionvalue>=.8 else 6; btn=3; helpidx=3
        fx=-3.0+.85*math.sin(u*2.5); fy=.35+.38*math.sin(u*3.7); angle=.23*math.sin(u*4)
        fishscale=1.03; bx=fx+.51*fishscale*math.cos(angle); by=fy+.51*fishscale*math.sin(angle)
        prog=.20+.78*min(1,u/6.15)
    elif t<16.35:
        q=smooth((t-15.8)/.55); fx=mix(-3,-5.8,q); fy=mix(.35,-2.1,q); angle=-2.45; fishscale=1.03*(1-q*.75)
        bx=fx+.51*fishscale*math.cos(angle); by=fy+.51*fishscale*math.sin(angle); prog=1; state=6; btn=1; helpidx=3
    else:
        showfloat=False; state=8; btn=4; helpidx=4; prog=1
    boia.location=(bx,by,.35); boia.rotation_euler.z=(.14*math.sin(t*3) if state!=5 else .42*math.sin(t*18))
    if state==5: boia.location.z=.35-.6*smooth((t-8.95)/.22)
    key(boia,'location',f)
    bs=1
    boia.scale=(bs,bs,bs) if showfloat else (0,0,0)
    for p in ['location','rotation_euler','scale']: key(boia,p,f)
    fish.location=(fx,fy,.09); fish.rotation_euler.z=angle; fish.scale=(fishscale,fishscale,fishscale)
    for p in ['location','rotation_euler','scale']: key(fish,p,f)
    swim=math.sin(t*(19 if state in (6,7) else 8))
    left.value=max(0,swim); right.value=max(0,-swim); key(left,'value',f); key(right,'value',f)
    tailpivot.rotation_euler.z=.25*swim; key(tailpivot,'rotation_euler',f)
    tailpivot.location.y=.09*swim; key(tailpivot,'location',f)
    for o in [hook,worm]:
        o.location=(bx+.13,by-.18,0); key(o,'location',f); visible(o,showfloat and t<8.95,f)
    line.hide_render=not showfloat; key(line,'hide_render',f)
    for p,co in zip(line.data.splines[0].points,[(-6.25,-2.4,.38),((-6.25+bx)/2,(-2.4+by)/2-(.10 if state<6 else 0),.38),(bx,by,.38)]):
        p.co=(*co,1); p.keyframe_insert('co',frame=f)
    for j,r in enumerate(rings):
        ph=(t*.65+j/3)%1; r.location=(bx,by,0); r.scale=((.13+.48*ph),(.10+.34*ph),1) if showfloat else (0,0,0)
        key(r,'location',f); key(r,'scale',f)
        # Close the loop invisibly rather than popping a full-strength large ring.
        r.hide_render=not showfloat or ph>.88; key(r,'hide_render',f)
    for i,o in enumerate(headings): visible(o,i==state,f)
    for i,o in enumerate(descriptions): visible(o,i==state and not caught,f)
    for i,o in enumerate(button_texts): visible(o,i==btn,f)
    for i,o in enumerate(help_texts): visible(o,i==helpidx,f)
    visible(counts[0],t<8.95,f); visible(counts[1],t>=8.95,f)
    tension.scale.x=max(.001,tensionvalue*(W-.04)); key(tension,'scale',f)
    pointer.location=(X+max(.02,tensionvalue*W),.48,0); key(pointer,'location',f)
    progress.scale.x=max(.001,prog*(W-.04)); key(progress,'scale',f)
    for o in [card,cardrim,*resultlabels]: visible(o,caught,f)
    for o in choices: visible(o,t<1.15,f)
    visible(cancel,not caught,f)
    visible(bpy.data.objects['GEO-FV02-Cancel'],not caught,f)
    if f==1 or not timeline or timeline[-1]['state']!=states[state][0]: timeline.append({'frame':f,'seconds':round(t,3),'state':states[state][0]})

camdata=bpy.data.cameras.new('CAM-FV02-Ortho'); cam=bpy.data.objects.new('CAM-FV02-Ortho',camdata)
s.collection.objects.link(cam); cam.location=(0,0,20); camdata.type='ORTHO'; camdata.ortho_scale=16.5; s.camera=cam
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        area.spaces.active.region_3d.view_perspective='CAMERA'; area.spaces.active.shading.type='MATERIAL'
s.frame_set(140)
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'fishing-preview-v02.blend'))
(OUT/'timeline.json').write_text(json.dumps({'fps':12,'frames':216,'seconds':18,'states':timeline},ensure_ascii=False,indent=2),encoding='utf-8')
result={'ok':True,'scene':NAME,'file':str(OUT/'fishing-preview-v02.blend'),'objects':len(s.objects),'frames':216,'fps':12}
