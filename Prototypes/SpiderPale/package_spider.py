from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
BASE=Path(__file__).resolve().parent
font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',24)
def preview(frame,label):
    result=Image.new('RGB',(640,680),(25,27,24))
    image=frame.resize((640,640),Image.Resampling.LANCZOS)
    result.paste(image,(0,40),image)
    ImageDraw.Draw(result).text((18,8),label,font=font,fill=(230,228,214))
    return result
for action,count,start,label in [('Walk',32,1,'Aranha Pálida · caminhada'),('Web',28,101,'Aranha Pálida · preparar teia')]:
    frames=[Image.open(BASE/action/f'{frame:03}.png').convert('RGBA') for frame in range(start,start+count)]
    views=[preview(frame,label) for frame in frames]
    durations=[42]*count
    if action=='Web': durations[0]=450; durations[-1]=600
    views[0].save(BASE/(action.lower()+'.gif'),save_all=True,append_images=views[1:],duration=durations,loop=0,disposal=2)
    atlas=Image.new('RGBA',(2048,1024))
    for i,frame in enumerate(frames): atlas.paste(frame.resize((256,256),Image.Resampling.LANCZOS),((i%8)*256,(i//8)*256))
    atlas.save(BASE/(action.lower()+'-sprite-sheet.png'))
Image.open(BASE/'Web'/'128.png').save(BASE/'pose-parada.png')
contact=Image.new('RGB',(1536,424),(25,27,24)); draw=ImageDraw.Draw(contact)
for i,(action,frame,label) in enumerate([('Walk',1,'Caminhada 1'),('Walk',9,'Caminhada 9'),('Web',119,'Preparação'),('Web',128,'Repouso')]):
    image=Image.open(BASE/action/f'{frame:03}.png').convert('RGBA')
    contact.paste(image,(i*384,40),image); draw.text((i*384+12,8),label,font=font,fill=(230,228,214))
contact.save(BASE/'poses.png')
print('Spider previews and atlases packaged from Blender renders.')
