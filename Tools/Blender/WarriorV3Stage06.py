"""Stage 6: rest-anchored surface materials and localized edge wear.

Mesh positions/weights/topology and animation are left intact. Vertex attributes
encode authoring masks, not a claim of baked physical occlusion or final textures.
"""
import bpy,bmesh,math,json,heapq
from pathlib import Path

ROOT=Path('C:/Users/Vinni/Documents/GitHub/Extlyp/Art/PlayerProposals/WarriorArtV3')
SOURCE=ROOT/'working-v3/warrior_v3-step05-cloth-leather.blend'
DEST=ROOT/'working-v3/warrior_v3-step06-materials.blend'
TARGETS=['MAT-weathered-steel','MAT-blade-steel','MAT-antique-brass','MAT-worn-brown-leather','MAT-crimson-woven-cloth','MAT-dark-joints','MAT-torch-wood','MAT-chainmail']

def distances(adj,seeds):
    d=[float('inf')]*len(adj);q=[]
    for i in seeds:d[i]=0;heapq.heappush(q,(0,i))
    while q:
        dist,i=heapq.heappop(q)
        if dist!=d[i]:continue
        if dist>.025:continue
        for j,w in adj[i]:
            nd=dist+w
            if nd<d[j]:d[j]=nd;heapq.heappush(q,(nd,j))
    return d

def attributes(me):
    bm=bmesh.new();bm.from_mesh(me);bm.verts.ensure_lookup_table();bm.verts.index_update()
    adj=[[] for _ in bm.verts];edge=set();crease=set()
    for e in bm.edges:
        a,b=e.verts;length=e.calc_length()
        adj[a.index].append((b.index,length));adj[b.index].append((a.index,length))
        if e.is_boundary:edge.update([a.index,b.index])
        elif e.is_manifold:
            angle=e.calc_face_angle_signed(0)
            if angle>.48:edge.update([a.index,b.index])
            elif angle<-.48:crease.update([a.index,b.index])
    ed=distances(adj,edge);cd=distances(adj,crease);bm.free()
    rest=me.attributes.get('ArtRestPosition') or me.attributes.new('ArtRestPosition','FLOAT_VECTOR','POINT')
    mask=me.color_attributes.get('ArtSurfaceMask') or me.color_attributes.new(name='ArtSurfaceMask',type='FLOAT_COLOR',domain='POINT')
    for v in me.vertices:
        rest.data[v.index].vector=v.co
        wear=max(0,1-ed[v.index]/.012)
        dirt=max(0,1-cd[v.index]/.016)
        mask.data[v.index].color=(wear,dirt,0,1)
    return {'vertices':len(me.vertices),'edge_seeds':len(edge),'crease_seeds':len(crease)}

def shader(name,base,wear,rough,metal,bump=.06,sheen=0,scratch=False):
    m=bpy.data.materials[name];m.use_nodes=True;n=m.node_tree.nodes;l=m.node_tree.links
    n.clear()
    def node(kind,label,x,y):
        ob=n.new(kind);ob.label=label;ob.location=(x,y);return ob
    def link(a,out,b,inp):l.new(a.outputs[out],b.inputs[inp])
    bs=node('ShaderNodeBsdfPrincipled','Surface',800,100)
    bs.inputs['Metallic'].default_value=metal;bs.inputs['Roughness'].default_value=rough
    if bs.inputs.get('Sheen Weight'):bs.inputs['Sheen Weight'].default_value=sheen
    output=node('ShaderNodeOutputMaterial','Output',1100,100);link(bs,'BSDF',output,'Surface')
    pos=node('ShaderNodeAttribute','Coordinates anchored to original vertices',-1000,-300);pos.attribute_name='ArtRestPosition'
    mask=node('ShaderNodeAttribute','R: edges / G: concave creases',-1000,400);mask.attribute_name='ArtSurfaceMask'
    split=node('ShaderNodeSeparateColor','Wear and crease masks',-780,400);link(mask,'Color',split,'Color')
    noise=node('ShaderNodeTexNoise','Restrained surface variation',-780,0)
    noise.inputs['Scale'].default_value=28 if metal else 45;noise.inputs['Detail'].default_value=2
    link(pos,'Vector',noise,'Vector')
    ramp=node('ShaderNodeValToRGB','Narrow tonal range',-520,80)
    ramp.color_ramp.elements[0].position=.20;ramp.color_ramp.elements[1].position=.80
    ramp.color_ramp.elements[0].color=(*(v*.88 for v in base),1)
    ramp.color_ramp.elements[1].color=(*(v*1.10 for v in base),1)
    link(noise,'Fac',ramp,'Fac')
    edge=node('ShaderNodeMixRGB','Localized edge polish / wear',-220,200)
    edge.blend_type='MIX';link(split,'Red',edge,0);link(ramp,'Color',edge,1);edge.inputs[2].default_value=(*wear,1)
    dirt=node('ShaderNodeMixRGB','Restrained darkening at concave creases',10,170)
    dirt.blend_type='MIX';link(edge,'Color',dirt,1);dirt.inputs[2].default_value=(*(v*.65 for v in base),1)
    amount=node('ShaderNodeMath','Limit dirt influence',-220,420);amount.operation='MULTIPLY';amount.inputs[1].default_value=.45
    link(split,'Green',amount,0);link(amount,0,dirt,0);link(dirt,'Color',bs,'Base Color')
    rr=node('ShaderNodeMapRange','Subtle roughness range',0,-70)
    rr.inputs['From Min'].default_value=0;rr.inputs['From Max'].default_value=1
    rr.inputs['To Min'].default_value=max(.1,rough-.035);rr.inputs['To Max'].default_value=min(1,rough+.035)
    link(noise,'Fac',rr,'Value')
    polished=node('ShaderNodeMixRGB','Smoother worn edges',270,-60)
    link(split,'Red',polished,0);link(rr,'Result',polished,1)
    polished.inputs[2].default_value=(max(.20,rough-.12),)*3+(1,)
    link(polished,'Color',bs,'Roughness')
    micro=node('ShaderNodeTexNoise','Fine grain',-520,-330)
    micro.inputs['Scale'].default_value=190;micro.inputs['Detail'].default_value=2
    if scratch:
        stretch=node('ShaderNodeVectorMath','Directional polishing grain',-780,-330)
        stretch.operation='MULTIPLY';stretch.inputs[1].default_value=(.16,3.5,.5)
        link(pos,'Vector',stretch,0);link(stretch,'Vector',micro,'Vector')
    else:link(pos,'Vector',micro,'Vector')
    bumpnode=node('ShaderNodeBump','Microrelief, no silhouette displacement',330,-280)
    bumpnode.inputs['Strength'].default_value=bump
    bumpnode.inputs['Distance'].default_value=.00020 if metal else .0003
    link(micro,'Fac',bumpnode,'Height');link(bumpnode,'Normal',bs,'Normal')
    m.diffuse_color=(*base,1)
    m['art_stage_06']='Rest-position grain; vertex edge/crease authoring masks'

def chainmail():
    # Preserve the existing ring-like Voronoi construction; reduce excess relief.
    m=bpy.data.materials['MAT-chainmail']
    n=m.node_tree.nodes
    bs=next(x for x in n if x.type=='BSDF_PRINCIPLED')
    bs.inputs['Base Color'].default_value=(.085,.10,.105,1)
    bs.inputs['Metallic'].default_value=.88;bs.inputs['Roughness'].default_value=.58
    for x in n:
        if x.type=='BUMP':
            x.inputs['Strength'].default_value=.22;x.inputs['Distance'].default_value=.001
    rest=n.new('ShaderNodeAttribute');rest.attribute_name='ArtRestPosition';rest.label='Stable ring coordinates'
    for x in list(n):
        if x.type=='TEX_VORONOI':m.node_tree.links.new(rest.outputs['Vector'],x.inputs['Vector'])
    m.diffuse_color=(.085,.10,.105,1)

def main():
    if Path(bpy.data.filepath).resolve()!=SOURCE.resolve():raise RuntimeError('Expected stage 5 file.')
    if DEST.exists():raise FileExistsError(DEST)
    meshes={}
    for o in bpy.context.scene.objects:
        if o.type=='MESH' and any(m and m.name in TARGETS for m in o.data.materials):
            if o.data.name not in meshes:meshes[o.data.name]=attributes(o.data)
    shader('MAT-weathered-steel',(.22,.25,.265),(.43,.46,.455),.48,.93,.055,scratch=True)
    shader('MAT-blade-steel',(.34,.38,.405),(.54,.57,.575),.30,.96,.025,scratch=True)
    shader('MAT-antique-brass',(.265,.16,.065),(.43,.29,.12),.48,.86,.045)
    shader('MAT-worn-brown-leather',(.080,.033,.016),(.145,.073,.035),.76,0,.10)
    shader('MAT-crimson-woven-cloth',(.135,.009,.014),(.17,.018,.022),.93,0,.065,.16)
    shader('MAT-dark-joints',(.023,.025,.022),(.035,.038,.032),.88,0,.035)
    shader('MAT-torch-wood',(.105,.043,.018),(.15,.073,.028),.84,0,.10,scratch=True)
    chainmail()
    bpy.context.scene['art_stage_06_done']=True
    report={'stage':6,'source':str(SOURCE),'saved_as':str(DEST),'blender':bpy.app.version_string,'materials_modified':TARGETS,'mesh_attributes':meshes,'wear_method':'Static vertex masks from boundary/convex dihedral edges; distance falloff over mesh edges. Concave-crease mask approximates local dirt, not physical occlusion.','texture_space':'ArtRestPosition captures original local positions; point attributes deform with meshes. No world-space or camera-space texture coordinates.','preserved':'Mesh positions/topology, weights, rig, Actions, keyframes, flame shaders, lights, cameras and color management.','review':'Frame 1 viewport inspection only; no playback, renders, simulations, Unity tests or builds. Temporal stability and final lighting remain unverified.','skill_limit':'Dedicated blender-materials skill not installed locally; applied installed orchestration/pro-workflow guidance without installing package skills.'}
    bpy.ops.wm.save_as_mainfile(filepath=str(DEST))
    (ROOT/'stage06-change-log.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    return report

if __name__=='__main__':print(json.dumps(main(),ensure_ascii=False))
