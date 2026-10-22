"""Build atlases, animated previews and a pose sheet from Blender PNGs."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json,zipfile
BASE=Path(__file__).resolve().parent/'Animation'
font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',20)
BG=(24,26,27)
def label(canvas,text,xy=(12,8)):
    ImageDraw.Draw(canvas).text(xy,text,font=font,fill=(224,220,207))
def presentation(frame,text):
    out=Image.new('RGB',(384,424),BG);out.paste(frame,(0,40),frame);label(out,text);return out
for action,count,filename,title in [('Walk',32,'walk','Caminhada'),('ClawAttack',20,'claw-attack','Golpe de garras')]:
    sets={view:[Image.open(BASE/view/action/f'{i:03}.png').convert('RGBA') for i in range(1,count+1)] for view in ['Top','Perspective']}
    durations=[50 if i%6==5 else 40 for i in range(count)] if action=='Walk' else [30]*8+[20]*3+[40]*9
    combined=[]
    for i in range(count):
        canvas=Image.new('RGB',(768,424),BG)
        canvas.paste(sets['Top'][i],(0,40),sets['Top'][i]);canvas.paste(sets['Perspective'][i],(384,40),sets['Perspective'][i])
        label(canvas,title+' · vista superior');label(canvas,'Vista em três quartos',(396,8));combined.append(canvas)
    combined[0].save(BASE/(filename+'.gif'),save_all=True,append_images=combined[1:],duration=durations,loop=0,disposal=2)
    for view,frames in sets.items():
        previews=[presentation(im,title+' · '+view) for im in frames]
        previews[0].save(BASE/(filename+'-'+view.lower()+'.gif'),save_all=True,append_images=previews[1:],duration=durations,loop=0,disposal=2)
    atlas=Image.new('RGBA',(2048,((count+7)//8)*256))
    for i,im in enumerate(sets['Top']):atlas.paste(im.resize((256,256),Image.Resampling.LANCZOS),((i%8)*256,(i//8)*256))
    atlas.save(BASE/(filename+'-sprite-sheet.png'))
    for i,frame in enumerate(sets['Top'],1):
        alpha=frame.getchannel('A');box=alpha.getbbox()
        if box is None or min(box[0],box[1])==0 or box[2]>=384 or box[3]>=384:raise RuntimeError(f'Empty or clipped frame {action}/{i}')
sheet=Image.new('RGB',(1536,900),BG)
for row,(action,indices,names) in enumerate([('Walk',[1,9,17,25],['Apoio direito','Transição','Apoio esquerdo','Transição']),('ClawAttack',[1,7,12,20],['Repouso','Preparação','Impacto · quadro 12','Retorno'])]):
    for col,(f,name) in enumerate(zip(indices,names)):
        im=Image.open(BASE/'Perspective'/action/f'{f:03}.png').convert('RGBA')
        x=col*384;y=row*450;sheet.paste(im,(x,y+40),im);label(sheet,name,(x+12,y+8))
sheet.save(BASE/'poses-animacao.png')
Image.open(BASE/'Top/ClawAttack/020.png').save(BASE/'pose-parada.png')
summary={'png_frames':104,'walk_atlas':[2048,1024],'attack_atlas':[2048,768],'clipping':'no alpha touches frame edges in all top-down frames','gif_attack_preparation_ms':300,'integration':'not performed'}
(BASE/'export-summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
with zipfile.ZipFile(BASE.parent/'espreitador-v3-animacoes.zip','w',zipfile.ZIP_DEFLATED) as archive:
    for path in sorted(BASE.rglob('*')):
        if path.is_file() and 'Review' not in path.parts and path.suffix!='.blend1':archive.write(path,path.relative_to(BASE.parent))
    for name in ['animate_model.py','render_animation.py','package_animation.py','build_model.py','espreitador-encapuzado-v3.blend']:
        archive.write(BASE.parent/name,name)
print('GIFs, 32/20-frame atlases and pose sheet saved; top frames have alpha margins.')
