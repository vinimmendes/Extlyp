"""Static medieval protagonist, authored for Blender 5.2 via official MCP.

Run stages individually with runpy. Does not delete existing scenes or objects.
Units are meters; local front is -Y; presentation root faces east (+X).
"""
import bpy
import math
import random
from mathutils import Vector
from pathlib import Path

ROOT = Path('C:/Users/Vinni/Documents/GitHub/Extlyp')
OUT = ROOT / 'Art/PlayerProposals/Medieval3D/v1'
SCENE = 'CyberExtraction-MedievalStatic-v1'

def activate():
    scene = bpy.data.scenes[SCENE]
    bpy.context.window.scene = scene
    return scene

def mat(name, color, metal=0, rough=.5, noise=0):
    m = bpy.data.materials.new('MAT-Medieval-' + name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    n = m.node_tree.nodes
    n.clear()
    bs = n.new('ShaderNodeBsdfPrincipled')
    output = n.new('ShaderNodeOutputMaterial')
    m.node_tree.links.new(bs.outputs['BSDF'], output.inputs['Surface'])
    bs.inputs['Base Color'].default_value = (*color, 1)
    bs.inputs['Metallic'].default_value = metal
    bs.inputs['Roughness'].default_value = rough
    if noise:
        tex = n.new('ShaderNodeTexNoise')
        tex.inputs['Scale'].default_value = 120 if metal else 75
        tex.inputs['Detail'].default_value = 3
        ramp = n.new('ShaderNodeValToRGB')
        ramp.color_ramp.elements[0].position = .25
        ramp.color_ramp.elements[0].color = (*(v*.45 for v in color),1)
        ramp.color_ramp.elements[1].position = .75
        ramp.color_ramp.elements[1].color = (*(min(v*1.35,1) for v in color),1)
        m.node_tree.links.new(tex.outputs['Fac'],ramp.inputs['Fac'])
        m.node_tree.links.new(ramp.outputs['Color'],bs.inputs['Base Color'])
        bump=n.new('ShaderNodeBump')
        bump.inputs['Strength'].default_value=noise
        bump.inputs['Distance'].default_value=.00065
        m.node_tree.links.new(tex.outputs['Fac'],bump.inputs['Height'])
        m.node_tree.links.new(bump.outputs['Normal'],bs.inputs['Normal'])
    return m

def material(name):
    return bpy.data.materials['MAT-Medieval-'+name]

def finish(obj,name,material_name,group='Armor',smooth=True):
    obj.name='GEO-Medieval-'+name
    col=bpy.data.collections['COL-Medieval-'+group]
    for c in list(obj.users_collection):
        c.objects.unlink(obj)
    col.objects.link(obj)
    obj.parent=bpy.data.objects.get('GEO-Medieval-Root')
    obj.data.materials.append(material(material_name))
    if obj.type=='MESH':
        for p in obj.data.polygons:
            p.use_smooth=smooth
    return obj

def mesh(name,verts,faces,ma,group='Armor',bevel=0,smooth=True):
    me=bpy.data.meshes.new('Mesh-Medieval-'+name)
    me.from_pydata(verts,[],faces)
    me.update()
    ob=bpy.data.objects.new('GEO-Medieval-'+name,me)
    bpy.context.scene.collection.objects.link(ob)
    finish(ob,name,ma,group,smooth)
    if bevel:
        mod=ob.modifiers.new('Forged edges','BEVEL')
        mod.width=bevel
        mod.segments=2
    return ob

def ellipsoid(name,loc,scale,ma,group='Armor'):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24,ring_count=16,location=loc)
    ob=bpy.context.object
    ob.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return finish(ob,name,ma,group)

def box(name,loc,scale,ma,bevel=.008,group='Armor'):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    ob=bpy.context.object
    ob.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    finish(ob,name,ma,group,False)
    if bevel:
        mod=ob.modifiers.new('Rounded forged edges','BEVEL')
        mod.width=bevel
        mod.segments=3
        ob.modifiers.new('Weighted normals','WEIGHTED_NORMAL')
    return ob

def tube(name,points,radius,ma,group='Armor'):
    cu=bpy.data.curves.new('Curve-Medieval-'+name,'CURVE')
    cu.dimensions='3D'
    cu.resolution_u=10
    cu.bevel_depth=radius
    cu.bevel_resolution=2
    sp=cu.splines.new('POLY')
    sp.points.add(len(points)-1)
    for p,co in zip(sp.points,points):
        p.co=(*co,1)
    ob=bpy.data.objects.new('GEO-Medieval-'+name,cu)
    bpy.context.scene.collection.objects.link(ob)
    return finish(ob,name,ma,group)

def segment(name,a,b,r1,r2,ma,group='Armor',sides=20):
    a,b=Vector(a),Vector(b)
    bpy.ops.mesh.primitive_cone_add(vertices=sides,radius1=r1,radius2=r2,depth=(b-a).length,location=(a+b)/2)
    ob=bpy.context.object
    ob.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()
    return finish(ob,name,ma,group)

def shell(name,rows,ma='Steel',front=True):
    # Rows: z, half-width, half-depth, y-offset. Closed sculpted cross sections.
    count=48
    vs=[]
    for z,rx,ry,cy in rows:
        for i in range(count):
            t=2*math.pi*i/count
            y=cy+ry*math.sin(t)
            if front and math.sin(t)<0:
                y-=.025*(1-abs(math.cos(t)))
            vs.append((rx*math.cos(t),y,z))
    fs=[]
    for j in range(len(rows)-1):
        for i in range(count):
            ni=(i+1)%count
            fs.append((j*count+i,j*count+ni,(j+1)*count+ni,(j+1)*count+i))
    fs.extend([tuple(reversed(range(count))),tuple((len(rows)-1)*count+i for i in range(count))])
    return mesh(name,vs,fs,ma,bevel=.002)

def stage_setup():
    if bpy.data.scenes.get(SCENE):
        raise RuntimeError('Scene already exists. Inspect it before rerunning creation.')
    OUT.mkdir(parents=True,exist_ok=True)
    scene=bpy.data.scenes.new(SCENE)
    bpy.context.window.scene=scene
    scene.unit_settings.system='METRIC'
    for name in ['Armor','Body','Sword','Details','Presentation','Reference']:
        col=bpy.data.collections.new('COL-Medieval-'+name)
        scene.collection.children.link(col)
    root=bpy.data.objects.new('GEO-Medieval-Root',None)
    scene.collection.objects.link(root)
    root['description']='Static interpretation of approved protagonist. No rig or animation.'
    root['height_m']=1.78
    root['reference']='player-medieval-one-handed-sword-east-v1.png'
    for args in [('Steel',(.22,.245,.25),.85,.4,.17),('Edge',(.39,.405,.39),.88,.3,.07),('Iron',(.065,.073,.07),.75,.5,.16),('Leather',(.105,.052,.025),0,.72,.25),('LeatherLight',(.20,.105,.045),0,.63,.2),('Copper',(.28,.16,.075),.8,.45,.14),('Cloth',(.047,.052,.035),0,.95,.3),('Skin',(.42,.235,.135),0,.65,.03),('Hair',(.055,.024,.010),0,.65,.18),('HairLight',(.10,.045,.018),0,.62,.15),('Eye',(.032,.022,.012),0,.32,0)]:
        mat(*args)
    world=bpy.data.worlds.new('World-Medieval-Studio')
    world.use_nodes=True
    world.node_tree.nodes.clear()
    bg=world.node_tree.nodes.new('ShaderNodeBackground')
    wo=world.node_tree.nodes.new('ShaderNodeOutputWorld')
    world.node_tree.links.new(bg.outputs['Background'],wo.inputs['Surface'])
    bg.inputs['Color'].default_value=(.12,.145,.17,1)
    bg.inputs['Strength'].default_value=.35
    scene.world=world
    scene.render.engine='BLENDER_EEVEE'
    scene.render.resolution_x=1440
    scene.render.resolution_y=1440
    scene.render.resolution_percentage=100
    scene.render.film_transparent=True
    scene.render.image_settings.file_format='PNG'
    scene.render.image_settings.color_mode='RGBA'
    scene.view_settings.view_transform='AgX'
    def camera(name,loc,target,scale):
        data=bpy.data.cameras.new(name)
        ob=bpy.data.objects.new(name,data)
        bpy.data.collections['COL-Medieval-Presentation'].objects.link(ob)
        ob.location=loc
        ob.rotation_euler=(Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()
        data.type='ORTHO'
        data.ortho_scale=scale
        return ob
    scene.camera=camera('CAM-Medieval-Hero',(3.2,-5.5,3.1),(.1,-.18,.9),2.6)
    camera('CAM-Medieval-TopDown',(0,-3.1,7),(.20,-.2,.8),2.5)
    for name,loc,power,size,color in [('Key',(-3,-4,5),500,4,(1,.86,.7)),('Fill',(4,-1,3),350,3,(.68,.80,1)),('Rim',(0,3,4),650,3,(1,.94,.8))]:
        data=bpy.data.lights.new('LGT-Medieval-'+name,'AREA')
        data.energy=power
        data.shape='DISK'
        data.size=size
        data.color=color
        ob=bpy.data.objects.new(data.name,data)
        bpy.data.collections['COL-Medieval-Presentation'].objects.link(ob)
        ob.location=loc
        ob.rotation_euler=(Vector((0,0,1))-ob.location).to_track_quat('-Z','Y').to_euler()

def stage_body():
    activate()
    shell('Gambeson',[(.83,.17,.105,0),(1.05,.19,.13,0),(1.3,.225,.13,0),(1.46,.205,.115,0)],'Cloth')
    shell('Breastplate',[(1.035,.165,.12,0),(1.12,.185,.14,0),(1.28,.235,.153,0),(1.40,.23,.12,0),(1.45,.16,.09,0)])
    for i in range(3):
        z=1.025-i*.047
        shell('Fauld-%02d'%i,[(z-.055,.177+i*.014,.13+i*.012,0),(z,.17+i*.013,.127+i*.011,0)],'Steel')
    # Neck and rolled gorget.
    segment('Neck', (0,0,1.43),(0,0,1.56),.065,.060,'Skin','Body')
    shell('Gorget',[(1.435,.14,.10,0),(1.49,.105,.083,0),(1.515,.079,.07,0)],'Iron',False)
    tube('Gorget-rim',[(.081*math.cos(i*math.tau/48),.072*math.sin(i*math.tau/48),1.515) for i in range(49)],.004,'Edge')
    # Articulated legs, staggered feet.
    for side,x,dy in [('L',-.145,-.13),('R',.155,.095)]:
        hip=(x,0,.92)
        knee=(x*1.22,dy,.53)
        ankle=(x*1.4,dy+.045,.14)
        segment('Thigh-mail-'+side,hip,knee,.10,.082,'Iron','Body')
        segment('Cuisse-'+side,Vector(hip).lerp(Vector(knee),.15),Vector(hip).lerp(Vector(knee),.85),.10,.087,'Steel')
        ellipsoid('Poleyn-'+side,(knee[0],knee[1]-.065,knee[2]),(.095,.065,.09),'Steel')
        ellipsoid('Knee-wing-'+side,(knee[0]+(-.08 if side=='L' else .08),knee[1],knee[2]),(.045,.04,.07),'Steel')
        segment('Shin-padding-'+side,knee,ankle,.075,.055,'Leather','Body')
        segment('Greave-'+side,Vector(ankle).lerp(Vector(knee),.09),Vector(ankle).lerp(Vector(knee),.85),.065,.082,'Steel')
        foot=(ankle[0],ankle[1]-.075,.085)
        ellipsoid('Boot-'+side,foot,(.078,.15,.073),'Leather')
        for j in range(5):
            ellipsoid('Sabaton-'+side+'-%02d'%j,(foot[0],foot[1]-.09+j*.039,.10+(.026 if j>2 else 0)),(.08,.037,.043),'Steel')
        for zz in [.28,.42]:
            t=(zz-ankle[2])/(knee[2]-ankle[2])
            c=Vector(ankle).lerp(Vector(knee),t)
            segment('Greave-strap-'+side+str(zz),c-Vector((0,0,.014)),c+Vector((0,0,.014)),.081,.081,'LeatherLight')
        # Three tassets per hip, overlapping above thigh.
        for j in range(3):
            ob=box('Tasset-'+side+'-%02d'%j,(x, -.12, .925-j*.047),(.17+j*.007,.045,.075),'Steel',.015)
            ob.rotation_euler.y=(-.13 if side=='L' else .13)
    arms={'R':[(.25,0,1.405),(.37,-.085,1.16),(.40,-.36,1.07)],'L':[(-.25,0,1.405),(-.34,.025,1.17),(-.34,-.055,.96)]}
    for side,(shoulder,elbow,wrist) in arms.items():
        segment('Upper-mail-'+side,shoulder,elbow,.086,.07,'Iron','Body')
        segment('Rerebrace-'+side,Vector(shoulder).lerp(Vector(elbow),.42),Vector(shoulder).lerp(Vector(elbow),.88),.086,.074,'Steel')
        ellipsoid('Couter-'+side,elbow,(.086,.078,.079),'Steel')
        segment('Forearm-mail-'+side,elbow,wrist,.067,.049,'Iron','Body')
        segment('Vambrace-'+side,Vector(elbow).lerp(Vector(wrist),.2),Vector(elbow).lerp(Vector(wrist),.94),.075,.055,'Steel')
        # Rounded shoulder cap and descending separate lames.
        ellipsoid('Pauldron-cap-'+side,(shoulder[0],.0,1.42),(.139,.142,.105),'Steel')
        sign=1 if side=='R' else -1
        for j in range(3):
            ob=ellipsoid('Pauldron-lame-'+side+str(j),(shoulder[0]+sign*(.038+j*.021),.002,1.38-j*.039),(.115-j*.012,.132-j*.008,.058),'Steel')
        hand=Vector(wrist)+Vector((0,-.034,-.027))
        ellipsoid('Gauntlet-palm-'+side,hand,(.047,.065,.037),'Leather')
        box('Gauntlet-back-'+side,hand+Vector((0,.014,.025)),(.086,.075,.022),'Steel',.012)
        for f in range(4):
            x=hand.x-.032+f*.021
            if side=='R':
                points=[(x,hand.y-.02,hand.z+.025),(x,hand.y-.053,hand.z+.005),(x,hand.y-.049,hand.z-.029),(x,hand.y-.018,hand.z-.033)]
            else:
                points=[(x,hand.y-.035,hand.z),(x,hand.y-.055,hand.z-.037),(x,hand.y-.041,hand.z-.065)]
            tube('Finger-'+side+str(f),points,.010,'Steel')
            ellipsoid('Knuckle-'+side+str(f),points[1],(.012,.012,.013),'Edge')
        tube('Thumb-'+side,[hand+Vector((.046,0,0)),hand+Vector((.055,-.035,-.015)),hand+Vector((.021,-.046,-.026))],.014,'Steel')

def stage_sword():
    activate()
    # Hilt passes through right fist; sword projects forward (-Y).
    base=Vector((.40,-.405,1.047))
    direction=Vector((.14,-.985,.10)).normalized()
    width=Vector((1,.142,0)).normalized()
    thick=direction.cross(width).normalized()
    def p(length,w=0,h=0):
        return base+direction*length+width*w+thick*h
    segment('Sword-grip',p(-.075),p(.065),.015,.014,'Leather','Sword')
    for i in range(9):
        c=p(-.065+i*.014)
        tube('Sword-grip-wrap-'+str(i),[c+width*(.015*math.cos(t))+thick*(.015*math.sin(t)) for t in [j*math.tau/20 for j in range(21)]],.0015,'LeatherLight','Sword')
    ellipsoid('Sword-pommel',p(-.105),(.027,.026,.026),'Steel','Sword')
    tube('Sword-crossguard',[p(.075,-.10,.012),p(.09,-.075),p(.09,0),p(.09,.075),p(.075,.10,.012)],.012,'Steel','Sword')
    vs=[]
    for l,w in [(.092,.026),(.15,.024),(.68,.018),(.80,.012)]:
        vs.extend([p(l,-w,0),p(l,0,.005),p(l,w,0),p(l,0,-.005)])
    vs.append(p(.87))
    fs=[]
    for k in range(3):
        for j in range(4):
            fs.append((k*4+j,k*4+(j+1)%4,(k+1)*4+(j+1)%4,(k+1)*4+j))
    for j in range(4):
        fs.append((12+j,12+(j+1)%4,16))
    fs.append((3,2,1,0))
    mesh('Sword-blade',[tuple(v) for v in vs],fs,'Edge','Sword',smooth=False)

def stage_head():
    activate()
    # Continuous sculpted head surface with recessed sockets, brow, nose and jaw.
    vs=[]
    rings=34
    n=64
    for j in range(rings+1):
        phi=.015+(math.pi-.03)*j/rings
        z=1.64+.125*math.cos(phi)
        for i in range(n):
            t=math.tau*i/n
            x=.086*math.sin(phi)*math.cos(t)
            y=.087*math.sin(phi)*math.sin(t)
            if y<0:
                front=(-math.sin(t))**8
                sockets=sum(math.exp(-((x-s*.034)/.019)**2-((z-1.66)/.015)**2) for s in [-1,1])
                nose=.030*math.exp(-(x/.014)**2-((z-1.635)/.031)**2)
                brow=.008*math.exp(-((z-1.68)/.012)**2)
                chin=.009*math.exp(-((z-1.555)/.018)**2)
                y+=front*(.013*sockets-nose-brow-chin)
                if z<1.62:
                    x*=.86+.14*(z-1.515)/.105
            vs.append((x,y-.012,z))
    fs=[]
    for j in range(rings):
        for i in range(n):
            fs.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
    fs.extend([tuple(reversed(range(n))),tuple(rings*n+i for i in range(n))])
    mesh('Head',vs,fs,'Skin','Body')
    for sign in [-1,1]:
        ellipsoid('Ear-'+str(sign),(sign*.083,-.002,1.634),(.016,.021,.029),'Skin','Body')
        ellipsoid('Eye-'+str(sign),(sign*.033,-.090,1.66),(.011,.005,.005),'Eye','Body')
        tube('Brow-'+str(sign),[(sign*.015,-.095,1.681),(sign*.033,-.099,1.687),(sign*.052,-.088,1.68)],.0035,'Hair','Body')
    tube('Mouth',[(-.024,-.093,1.587),(0,-.1,1.585),(.024,-.093,1.587)],.0015,'Leather','Body')
    # Hair cap with irregular hairline and layered swept locks.
    random.seed(31)
    hair_vs=[]
    for j in range(13):
        for i in range(64):
            t=math.tau*i/64
            limit=1.6+.34*math.sin(t)+.06*math.sin(t*7)
            phi=.025+(limit-.025)*j/12
            hair_vs.append((.092*math.sin(phi)*math.cos(t),-.006+.096*math.sin(phi)*math.sin(t),1.65+.129*math.cos(phi)))
    hair_fs=[]
    for j in range(12):
        for i in range(64):
            hair_fs.append((j*64+i,j*64+(i+1)%64,(j+1)*64+(i+1)%64,(j+1)*64+i))
    mesh('Hair-cap',hair_vs,hair_fs,'Hair','Body')
    for i in range(90):
        angle=i*2.399963
        start=.12+random.random()*.45
        end=1.37+.22*math.sin(angle)
        points=[]
        for j in range(12):
            t=j/11
            phi=start+(end-start)*t
            a=angle+.45*(1-t)
            r=1.02+.02*math.sin(math.pi*t)
            points.append((.093*r*math.sin(phi)*math.cos(a),-.006+.097*r*math.sin(phi)*math.sin(a),1.65+.130*r*math.cos(phi)))
        tube('Hair-lock-%03d'%i,points,.0027 if i%3 else .004,'HairLight' if i%5==0 else 'Hair','Body')

def stage_detail():
    activate()
    # Waist belt, aged buckles and torso straps.
    shell('Belt',[(1.022,.187,.153,0),(1.06,.183,.151,0)],'Leather',False)
    for name,points in [('Diagonal', [(-.18,-.122,1.42),(-.12,-.175,1.31),(0,-.18,1.18),(.13,-.15,1.065)]),('Shoulder-L',[(-.155,-.12,1.42),(-.155,0,1.475),(-.155,.12,1.4)]),('Shoulder-R',[(.155,-.12,1.42),(.155,0,1.475),(.155,.12,1.4)])]:
        # Flat strap ribbon with thickness.
        verts=[]
        for p in points:
            verts.extend([(p[0]-.014,p[1]-.003,p[2]),(p[0]+.014,p[1]-.003,p[2])])
        ob=mesh('Strap-'+name,verts,[(j*2,j*2+1,j*2+3,j*2+2) for j in range(len(points)-1)],'LeatherLight','Details')
        mod=ob.modifiers.new('Leather thickness','SOLIDIFY'); mod.thickness=.006
    for name,c in [('Waist',(.045,-.161,1.043)),('Chest',(-.08,-.19,1.26))]:
        x,y,z=c
        tube('Buckle-'+name,[(x-.024,y,z-.023),(x+.024,y,z-.023),(x+.024,y,z+.023),(x-.024,y,z+.023),(x-.024,y,z-.023)],.004,'Copper','Details')
        tube('Buckle-pin-'+name,[(x,y-.004,z-.022),(x,y-.004,z+.014)],.002,'Edge','Details')
    # Rolled chest rims and rivets on shoulder plates.
    for z,rx,ry in [(1.052,.167,.123),(1.4,.23,.12)]:
        tube('Chest-rim-'+str(z),[(rx*math.cos(i*math.tau/64),ry*math.sin(i*math.tau/64)-(.025*(1-abs(math.cos(i*math.tau/64))) if math.sin(i*math.tau/64)<0 else 0),z) for i in range(65)],.003,'Edge','Details')
    for sign in [-1,1]:
        for i in range(7):
            t=math.pi*2*i/7
            ellipsoid('Shoulder-rivet-'+str(sign)+'-'+str(i),(sign*.25+.11*math.cos(t),.105*math.sin(t),1.467),(.006,.006,.004),'Copper','Details')
        for i in range(3):
            ellipsoid('Tasset-rivet-'+str(sign)+str(i),(sign*.145+(-.056 if i==0 else .056),-.148,.92-i*.047),(.004,.003,.004),'Copper','Details')
    # Linked chainmail at elbows and collar, combined into one mesh.
    verts=[]; faces=[]
    def ring(c,r=.008):
        offset=len(verts)
        for i in range(10):
            t=i*math.tau/10
            for j in range(4):
                q=j*math.tau/4
                rr=r+.0013*math.cos(q)
                verts.append((c[0]+rr*math.cos(t),c[1]+.0013*math.sin(q),c[2]+rr*math.sin(t)))
        for i in range(10):
            for j in range(4):
                faces.append((offset+i*4+j,offset+((i+1)%10)*4+j,offset+((i+1)%10)*4+(j+1)%4,offset+i*4+(j+1)%4))
    for sign in [-1,1]:
        for row in range(6):
            for col in range(10):
                ring((sign*(.245+col*.010),-.090-row*.001,1.30-row*.013+(col%2)*.006))
        for row in range(4):
            for col in range(9):
                ring((sign*.145+(col-4)*.012,-.123, .90-row*.013))
    mesh('Chainmail-links',verts,faces,'Iron','Details')
    # Small practical leather pouch at rear hip.
    box('Belt-pouch',(-.19,.115,.955),(.115,.065,.14),'Leather',.022,'Details')
    box('Pouch-flap',(-.19,.153,.997),(.12,.017,.065),'LeatherLight',.012,'Details')
    ellipsoid('Pouch-stud',(-.19,.164,.976),(.006,.004,.006),'Copper','Details')

def stage_save():
    scene=activate()
    root=bpy.data.objects['GEO-Medieval-Root']
    root.rotation_euler.z=math.pi/2
    # East-facing presentation. Cameras target the complete character plus sword.
    hero=bpy.data.objects['CAM-Medieval-Hero']
    hero.location=(4.2,-5.7,3.5)
    hero.rotation_euler=(Vector((.20,0,.90))-hero.location).to_track_quat('-Z','Y').to_euler()
    top=bpy.data.objects['CAM-Medieval-TopDown']
    top.location=(.25,-2.9,7)
    top.rotation_euler=(Vector((.25,0,.8))-top.location).to_track_quat('-Z','Y').to_euler()
    scene.camera=hero
    # Keep the approved art as a packed, non-rendering reference.
    ref=bpy.data.images.load(str(ROOT/'Assets/Art/Player/player-medieval-one-handed-sword-east-v1.png'),check_existing=True)
    ref.pack()
    ob=bpy.data.objects.new('REF-Medieval-Approved',None)
    bpy.data.collections['COL-Medieval-Reference'].objects.link(ob)
    ob.empty_display_type='IMAGE'; ob.data=ref
    ob.location=(-2,0,1); ob.empty_display_size=1.8
    ob.hide_render=True; ob.hide_viewport=True
    # Select only the new character; preserve objects from other scenes.
    for ob in scene.objects:
        ob.select_set(False)
    meshes=[o for o in scene.objects if o.type=='MESH']
    for ob in meshes: ob.select_set(True)
    bpy.context.view_layer.objects.active=bpy.data.objects['GEO-Medieval-Breastplate']
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type=='VIEW_3D':
                space=area.spaces.active
                space.shading.type='MATERIAL'
                space.shading.use_scene_world=True
                space.shading.use_scene_lights=True
                space.region_3d.view_distance=3.6
                space.region_3d.view_location=(.25,0,.9)
                space.region_3d.view_rotation=hero.rotation_euler.to_quaternion()
    dest=OUT/'protagonist-medieval-static-v1.blend'
    if dest.exists():
        raise FileExistsError(dest)
    bpy.ops.wm.save_as_mainfile(filepath=str(dest))
    return {'blend':str(dest),'mesh_objects':len(meshes),'base_triangles':sum(sum(len(p.vertices)-2 for p in ob.data.polygons) for ob in meshes),'curves':sum(o.type=='CURVE' for o in scene.objects),'animations':len(bpy.data.actions)}

def stage_refine():
    activate()
    # Ensure the quilted underlayer lies beneath the breastplate, not through it.
    ob=bpy.data.objects['GEO-Medieval-Gambeson']
    for v in ob.data.vertices:
        v.co.x*=.90
        v.co.y*=.78
    for side in ['L','R']:
        for j in range(5):
            ob=bpy.data.objects['GEO-Medieval-Sabaton-'+side+'-%02d'%j]
            ob.location.z=.155+(.018 if j>2 else 0)
            ob.scale.y=1.18
        ob=bpy.data.objects['GEO-Medieval-Boot-'+side]
        ob.scale.z=.83
        # Lower shoulder caps read as armor rather than spherical pauldrons.
        ob=bpy.data.objects['GEO-Medieval-Pauldron-cap-'+side]
        ob.scale.z=.72
        for j in range(3):
            ob=bpy.data.objects['GEO-Medieval-Pauldron-lame-'+side+str(j)]
            ob.scale.z=.67
            ob.scale.x=1.10
    # Broad, low-contrast oxidation patches on forged steel.
    steel=material('Steel')
    nodes=steel.node_tree.nodes
    bs=next(n for n in nodes if n.type=='BSDF_PRINCIPLED')
    micro=next(n for n in nodes if n.type=='VALTORGB')
    mottling=nodes.new('ShaderNodeTexNoise')
    mottling.inputs['Scale'].default_value=7
    mottling.inputs['Detail'].default_value=4
    ramp=nodes.new('ShaderNodeValToRGB')
    ramp.color_ramp.elements[0].position=.30
    ramp.color_ramp.elements[0].color=(.105,.112,.108,1)
    ramp.color_ramp.elements[1].position=.65
    ramp.color_ramp.elements[1].color=(.205,.22,.215,1)
    steel.node_tree.links.new(mottling.outputs['Fac'],ramp.inputs['Fac'])
    steel.node_tree.links.new(ramp.outputs['Color'],bs.inputs['Base Color'])
    bs.inputs['Roughness'].default_value=.42
    # Plate edge rims and tiny fasteners emphasize separate forged lames.
    for sign,side in [(-1,'L'),(1,'R')]:
        for j in range(3):
            cx=sign*(.288+j*.021)
            cz=1.38-j*.039
            pts=[]
            for i in range(21):
                t=math.pi+math.pi*i/20
                pts.append((cx+(.115-j*.012)*1.10*math.cos(t),(.132-j*.008)*math.sin(t),cz-.008))
            tube('Pauldron-lower-edge-'+side+str(j),pts,.0028,'Edge','Details')
    return {'refined':'underlayer, sabatons, pauldrons, aged steel'}

def stage_render():
    scene=activate()
    scene.render.filepath=str(OUT/'protagonist-medieval-static-v1-hero.png')
    bpy.ops.render.render(write_still=True)
    return {'render':scene.render.filepath,'resolution':[scene.render.resolution_x,scene.render.resolution_y]}
