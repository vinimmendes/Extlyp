"""Package Blender renders; timings follow sampled frame numbers, not an invented FPS."""
from pathlib import Path
import sys,json,shutil
from PIL import Image,ImageDraw,ImageFont
BASE=Path(__file__).resolve().parent
folder=BASE/sys.argv[1]
if (folder/'.rendering').exists(): raise RuntimeError('Wait for this model render to finish before packaging.')
data=json.loads((folder/'clips.json').read_text(encoding='utf-8'))
if folder.name=='Mimic':
    # Use one shared real Blender render for both closed disguises; separate renders
    # may differ in sampling noise even when geometry/materials are identical.
    chest=BASE/'Chest'
    if (chest/'.rendering').exists(): raise RuntimeError('Wait for the shared chest render.')
    for dest in [folder/'Disguise/601.png',folder/'Reveal/101.png']:
        shutil.copy2(chest/'idle.png',dest)
    data['disguiseSource']='../Chest/idle.png'
    (folder/'clips.json').write_text(json.dumps(data,indent=2),encoding='utf-8')
font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',22)
for clip in data['clips']:
    indices=clip['frames']; frames=[]
    for index in indices:
        f=Image.open(folder/clip['name']/f'{index:03}.png').convert('RGBA')
        view=Image.new('RGB',(640,680),(25,27,24)); f=f.resize((640,640),Image.Resampling.LANCZOS); view.paste(f,(0,40),f)
        ImageDraw.Draw(view).text((18,8),folder.name+' · '+clip['name']+' · Blender',font=font,fill=(228,226,213)); frames.append(view)
    duration=[max(1,round((indices[i+1]-f)/data['fps']*1000)) if i+1<len(indices) else (42 if clip['name']=='Walk' else 500) for i,f in enumerate(indices)]
    if len(duration)>1 and clip['name']!='Walk': duration[0]+=350
    frames[0].save(folder/(clip['name']+'.gif'),save_all=True,append_images=frames[1:],duration=duration,loop=0,disposal=2)
    for page in range((len(indices)+63)//64):
        group=indices[page*64:(page+1)*64]; atlas=Image.new('RGBA',(2048,((len(group)+7)//8)*256))
        for i,f in enumerate(group):
            im=Image.open(folder/clip['name']/f'{f:03}.png').convert('RGBA').resize((256,256),Image.Resampling.LANCZOS)
            atlas.paste(im,((i%8)*256,(i//8)*256))
        atlas.save(folder/(clip['name']+f'-sheet-{page+1}.png'))
contact=Image.new('RGB',(384*3,424),(25,27,24)); draw=ImageDraw.Draw(contact)
main=data['clips'][0]; samples=[('idle.png','Repouso'),('perspective.png','Perspectiva'),(main['name']+'/'+f"{main['frames'][len(main['frames'])//2]:03}.png",main['name'])]
for i,(file,label) in enumerate(samples):
    f=Image.open(folder/file).convert('RGBA'); contact.paste(f,(i*384,40),f); draw.text((i*384+12,8),label,font=font,fill=(228,226,213))
contact.save(folder/'contact.png')
print('Preview ready: '+folder.name)
