from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
BASE=Path(__file__).resolve().parent
font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',23)
def preview(frame,label):
    result=Image.new('RGB',(640,680),(25,27,24))
    frame=frame.resize((640,640),Image.Resampling.LANCZOS); result.paste(frame,(0,40),frame)
    ImageDraw.Draw(result).text((18,8),label,font=font,fill=(228,226,213)); return result
for action,count,start,label in [('Walk',32,1,'Corrosivo · caminhada'),('Spit',45,101,'Corrosivo · inflar e cuspir')]:
    frames=[Image.open(BASE/action/f'{i:03}.png').convert('RGBA') for i in range(start,start+count)]
    views=[preview(f,label) for f in frames]; durations=[42]*count
    if action=='Spit': durations[0]=400; durations[-1]=650
    views[0].save(BASE/(action.lower()+'.gif'),save_all=True,append_images=views[1:],duration=durations,loop=0,disposal=2)
    atlas=Image.new('RGBA',(2048,((count+7)//8)*256))
    for i,f in enumerate(frames): atlas.paste(f.resize((256,256),Image.Resampling.LANCZOS),((i%8)*256,(i//8)*256))
    atlas.save(BASE/(action.lower()+'-sprite-sheet.png'))
contact=Image.new('RGB',(2048,552),(25,27,24)); draw=ImageDraw.Draw(contact)
for i,(frame,label) in enumerate([(101,'Repouso'),(129,'Papo cheio'),(134,'Cuspe'),(145,'Recuperado')]):
    f=Image.open(BASE/'Spit'/f'{frame:03}.png').convert('RGBA'); contact.paste(f,(i*512,40),f)
    draw.text((i*512+12,8),label,font=font,fill=(228,226,213))
contact.save(BASE/'poses.png')
print('Corrosive previews packaged.')
