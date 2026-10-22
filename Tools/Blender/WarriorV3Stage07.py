"""Stage 7: asymmetrical flame layers, inherited flicker and clear hot core."""
import bpy,math,json
from pathlib import Path
from mathutils import Vector

ROOT=Path('C:/Users/Vinni/Documents/GitHub/Extlyp/Art/PlayerProposals/WarriorArtV3')
SOURCE=ROOT/'working-v3/warrior_v3-step06-materials.blend'
DEST=ROOT/'working-v3/warrior_v3-step07-flame.blend'

def reshape(ob,index):
    keys=ob.data.shape_keys
    basis=[p.co.copy() for p in keys.key_blocks['Basis'].data]
    zmin=min(p.z for p in basis);height=max(p.z for p in basis)-zmin
    attr=ob.data.attributes.get('FireHeight') or ob.data.attributes.new('FireHeight','FLOAT','POINT')
    # Keep original 8 x 20 ring topology and one tip, together with all weights.
    for j in range(8):
        ids=list(range(j*20,(j+1)*20))
        t=sum(basis[i].z for i in ids)/20
        t=(t-zmin)/height
        for block in keys.key_blocks:
            center=sum((block.data[i].co for i in ids),Vector())/20
            for i in ids:
                a=(i%20)*math.tau/20
                delta=block.data[i].co-center
                taper=math.sin(math.pi*min(1,t))
                # Broad lobes, not high-frequency noise; attachment ring fixed.
                factor=1+taper*(.16*math.sin(3*a+2.7*t+index)+.08*math.sin(5*a-1.8*t))
                delta.x*=factor;delta.y*=factor
                drift=Vector((.014*math.sin(5*t+index)*t,.016*math.sin(4*t+index*1.9)*t,0))
                block.data[i].co=center+delta+drift
        for i in ids:attr.data[i].value=max(0,min(1,(basis[i].z-zmin)/height))
    for block in keys.key_blocks:
        block.data[160].co+=Vector((.017*math.cos(index),.012*math.sin(index+1),0))
    if index>0:
        # Bring the hot interior toward the existing game camera, rather than
        # allowing opaque outer surfaces to hide it entirely.
        for block in keys.key_blocks:
            for i,p in enumerate(block.data):
                t=max(0,min(1,(basis[i].z-zmin)/height))
                p.co.y-=[0,.060,.130][index]*math.sin(t*math.pi/2)
                p.co.z+=[0,.020,.060][index]*t
    attr.data[160].value=1
    ob.data.update()
    return {'object':ob.name,'shape_keys':[k.name for k in keys.key_blocks],'vertices':len(ob.data.vertices),'attachment_ring':'unchanged'}

def material(index):
    m=bpy.data.materials['MAT-fire-'+str(index)]
    n=m.node_tree.nodes;l=m.node_tree.links;n.clear()
    a=n.new('ShaderNodeAttribute');a.attribute_name='FireHeight';a.label='Stable height from original flame mesh';a.location=(-500,0)
    r=n.new('ShaderNodeValToRGB');r.location=(-260,0)
    palettes=[[(1,.25,.008,1),(1,.47,.025,1),(1,.13,.003,1),(1,.035,.001,1)],[(1,.48,.04,1),(1,.67,.13,1),(1,.35,.02,1),(1,.10,.003,1)],[(1,.90,.57,1),(1,.80,.36,1),(1,.62,.14,1),(1,.34,.03,1)]]
    ramp=r.color_ramp;ramp.elements.remove(ramp.elements[1])
    ramp.elements[0].position=0;ramp.elements[0].color=palettes[index][0]
    for p,c in zip([.30,.72,1],palettes[index][1:]):ramp.elements.new(p).color=c
    ramp.interpolation='EASE'
    em=n.new('ShaderNodeEmission');em.location=(0,0);em.inputs['Strength'].default_value=[1.35,1.55,1.7][index]
    out=n.new('ShaderNodeOutputMaterial');out.location=(230,0)
    l.new(a.outputs['Fac'],r.inputs['Fac']);l.new(r.outputs['Color'],em.inputs['Color']);l.new(em.outputs[0],out.inputs['Surface'])
    m.diffuse_color=palettes[index][1];m.node_tree.update_tag();m.update_tag()

def tongue(source,label,scale,offset,phase):
    name='GEO-torch-tongue-'+label+'-step07'
    if bpy.data.objects.get(name):raise RuntimeError(name+' already exists')
    ob=source.copy();ob.data=source.data.copy();ob.name=name
    if ob.data.shape_keys==source.data.shape_keys:raise RuntimeError('Shape keys must be independent')
    source.users_collection[0].objects.link(ob)
    keys=ob.data.shape_keys
    keys.animation_data_clear()
    original=[p.co.copy() for p in keys.key_blocks['Basis'].data]
    zmin=min(p.z for p in original);height=max(p.z for p in original)-zmin
    anchor=sum(original[:20],Vector())/20
    for block in keys.key_blocks:
        for i,p in enumerate(block.data):
            t=max(0,min(1,(original[i].z-zmin)/height))
            q=p.co-anchor
            q.x*=scale[0];q.y*=scale[1];q.z*=scale[2]
            q+=Vector(offset)*math.sin(t*math.pi/2)
            q+=Vector((.008*math.sin(t*7+phase)*t,.008*math.cos(t*5+phase)*t,0))
            p.co=anchor+q
    # No new time curves: follow source Flicker, including cadence switching.
    f=keys.key_blocks['Flicker'].driver_add('value')
    driver=f.driver;driver.type='SCRIPTED'
    variable=driver.variables.new();variable.name='source_flicker';variable.type='SINGLE_PROP'
    variable.targets[0].id_type='KEY';variable.targets[0].id=source.data.shape_keys
    variable.targets[0].data_path='key_blocks["Flicker"].value'
    driver.expression='source_flicker'
    ob.data.update()
    return {'object':name,'source_flicker':source.name,'relative_scale':scale,'driver':'source_flicker; no new keyframes or cadence'}

def main():
    if Path(bpy.data.filepath).resolve()!=SOURCE.resolve():raise RuntimeError('Expected stage 6 file.')
    if DEST.exists():raise FileExistsError(DEST)
    changed=[]
    for i in range(3):
        changed.append(reshape(bpy.data.objects['GEO-torch-flame-'+str(i)],i))
        material(i)
    outer=bpy.data.objects['GEO-torch-flame-0']
    extra=[tongue(outer,'A',(.68,.65,.76),(-.042,.045,.005),.4),tongue(outer,'B',(.55,.58,.60),(.045,-.047,.004),2.1)]
    bpy.context.view_layer.update()
    bpy.context.scene['art_stage_07_done']=True
    report={'stage':7,'source':str(SOURCE),'saved_as':str(DEST),'blender':bpy.app.version_string,'main_flames_modified':changed,'secondary_tongues':extra,'materials_modified':['MAT-fire-0','MAT-fire-1','MAT-fire-2'],'animation':'Original Flicker shape keys and Heavy/Alert Actions remain. Extra tongues follow the outer Flicker via drivers and static asymmetric geometry; no independently timed motion was added.','preserved':'Hand.L anchors, torch shaft/bowl, rig, walk Actions/keyframes, cameras, lights, stages 3-6 materials and geometry.','review':'Static frame 1 viewport only. Full-cycle silhouette, temporal behavior, layer overlap and framing are pending stage 9. No playback, render, simulation, Unity test or build.'}
    bpy.ops.wm.save_as_mainfile(filepath=str(DEST))
    (ROOT/'stage07-change-log.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    return report

if __name__=='__main__':print(json.dumps(main(),ensure_ascii=False))
