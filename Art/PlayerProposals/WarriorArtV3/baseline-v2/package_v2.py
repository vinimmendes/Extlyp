from pathlib import Path
from PIL import Image,ImageDraw,ImageFilter,ImageChops
import json,zipfile,shutil
P=Path(__file__).resolve().parent; V=P/'validation'
def load(p): return Image.open(p).convert('RGBA')
def gif(paths,dest,duration,size=512):
    ims=[]
    for path in paths:
        im=load(path); bg=Image.new('RGBA',im.size,(24,27,33,255)); bg.alpha_composite(im)
        ims.append(bg.convert('RGB').resize((size,size),Image.Resampling.LANCZOS))
    ims[0].save(dest,save_all=True,append_images=ims[1:],duration=duration,loop=0,disposal=2)
top=sorted((P/'frames').glob('walk_*.png')); perspective=sorted((P/'perspective_frames').glob('view_*.png')); vertical=sorted((P/'vertical_frames').glob('view_*.png'))
assert len(top)==36 and len(perspective)==18 and len(vertical)==36
gif(top,P/'warrior_v2_topdown.gif',[30,30,40]*12)
gif(perspective,P/'warrior_v2_perspective.gif',[60,70,70]*6)
gif(vertical,P/'warrior_v2_vertical.gif',[30,30,40]*12,384)
gif(top,P/'cadence_alert_preview.gif',[30]*28+[20]*8)
sheet=Image.new('RGBA',(640*6,640*6))
for i,path in enumerate(top): sheet.paste(load(path),((i%6)*640,(i//6)*640))
sheet.save(P/'warrior_v2_spritesheet.png')
for name,paths in [('topdown',top),('perspective',perspective),('vertical',vertical)]: shutil.copy2(paths[0],V/(name+'.png'))
meta={'image':'warrior_v2_spritesheet.png','frame_width':640,'frame_height':640,'columns':6,'rows':6,'frames':36,'fps':30,'seconds':1.2,'loop':True,'pivot':[320,320],'direction':'right','movement':'in place','equipment':{'sword':'anatomical right','torch':'anatomical left'},'source':'3D model and IK rig','root_speed_equivalent_m_s':.46/(.60*1.2)}
meta['pivot']=json.loads((V/'camera_registration.json').read_text())['CAM-game-topdown']['root_pivot']
meta['pivot_space']='pixels from top-left; projection of stationary 3D root'
(P/'sprite_metadata.json').write_text(json.dumps(meta,indent=2))
framing={}
for name,paths in [('topdown',top),('perspective',perspective),('vertical',vertical)]:
    boxes=[load(p).getchannel('A').point(lambda v:255 if v>4 else 0).getbbox() for p in paths]
    size=load(paths[0]).size[0]
    passed=all(b and b[0]>0 and b[1]>0 and b[2]<size and b[3]<size for b in boxes)
    framing[name]={'passed':passed,'bounds':boxes,'size':size}
    assert passed,(name,boxes)
(V/'framing.json').write_text(json.dumps(framing,indent=2))

# Show actual pixel sizes rather than hiding readability problems by upscaling.
small=Image.new('RGB',(1160,550),(24,27,33)); d=ImageDraw.Draw(small)
y=12
for size in [64,128,256]:
    d.text((10,y+5),str(size)+' px',fill='white')
    for j,k in enumerate([0,9,18,27]):
        im=load(top[k]).resize((size,size),Image.Resampling.LANCZOS)
        small.paste(im,(100+j*264,y),im)
    y+=size+26
small.save(V/'readability_64_128_256.png')

def panel(path,w=420):
    im=load(path); bg=Image.new('RGBA',im.size,(24,27,33,255)); bg.alpha_composite(im)
    im=bg.convert('RGB'); im.thumbnail((w,w),Image.Resampling.LANCZOS)
    canvas=Image.new('RGB',(w,w),(24,27,33)); canvas.paste(im,((w-im.width)//2,(w-im.height)//2)); return canvas
compare=Image.new('RGB',(1260,450),(24,27,33)); cd=ImageDraw.Draw(compare)
for j,(title,path) in enumerate([('Referencia',P/'reference.png'),('Modelo v1',Path('/workspace/warrior-3d/validation/topdown.png')),('Modelo v2',top[0])]):
    compare.paste(panel(path),(j*420,30)); cd.text((j*420+12,10),title,fill='white')
compare.save(V/'reference_v1_v2.png')

# Diagnostic overlay with normalized alpha bounds; not a claim of exact pose fit.
def normalize(im):
    bbox=im.getchannel('A').point(lambda v:255 if v>10 else 0).getbbox(); im=im.crop(bbox)
    im.thumbnail((540,540),Image.Resampling.LANCZOS)
    out=Image.new('RGBA',(600,600)); out.alpha_composite(im,((600-im.width)//2,(600-im.height)//2)); return out
ref=normalize(load(P/'reference.png')); target=normalize(load(top[0]))
overlay=Image.new('RGB',(600,630),(24,27,33)); od=ImageDraw.Draw(overlay)
for im,color in [(ref,(235,95,95)),(target,(65,220,225))]:
    alpha=im.getchannel('A').point(lambda v:255 if v>80 else 0)
    outline=ImageChops.subtract(alpha.filter(ImageFilter.MaxFilter(5)),alpha.filter(ImageFilter.MinFilter(3)))
    overlay.paste(color,(0,30),outline)
od.text((8,8),'Vermelho: referencia / Ciano: v2 (limites normalizados)',fill='white')
overlay.save(V/'reference_overlay.png')
(V/'registration_report.json').write_text(json.dumps({'method':'alpha-bounds normalized contour overlay, separate canonical camera','source':'reference.png','target':'topdown frame 1','exact_pose_alignment':False,'remaining_difference':'single painted reference differs in pose, perspective, proportions and surface detail; v2 is a stylized reconstruction'},indent=2))

regions={'helmet':[540,440,260,235],'body':[290,410,380,430],'torch_hand':[775,290,165,145],'sword_hand':[510,875,140,140],'sword_blade':[640,940,625,220],'torch_flame':[1020,40,260,255]}
seeds={'schema':'source_part_seed_manifest.v1','image':'reference.png','method':'manual bbox seeds multiplied by reference alpha; ambiguous overlaps retained','parts':[]}
(V/'reference_parts').mkdir(exist_ok=True)
for name,(x,y,w,h) in regions.items():
    mask=Image.new('L',load(P/'reference.png').size); mask.paste(load(P/'reference.png').getchannel('A').crop((x,y,x+w,y+h)),(x,y))
    mask.save(V/'reference_parts'/(name+'.png')); seeds['parts'].append({'name':name,'bbox':[x,y,w,h],'mode':'non_background'})
(V/'source_part_seed_manifest.json').write_text(json.dumps(seeds,indent=2))
print('Previews, sheet, registration comparison, readability and framing checks completed.')
