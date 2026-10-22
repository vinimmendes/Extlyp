"""Package actual Blender renders as a preview, contact sheet and 256px sprite atlas."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
BASE=Path(__file__).resolve().parent/'ShotgunSwitch'
frames=[Image.open(BASE/'frames'/f'{i:03}.png').convert('RGBA') for i in range(1,33)]
font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',22)
def preview(frame):
    image=Image.new('RGB',(640,680),(27,29,26))
    image.paste(frame.resize((640,640),Image.Resampling.LANCZOS),(0,40),frame.resize((640,640),Image.Resampling.LANCZOS))
    ImageDraw.Draw(image).text((18,10),'Pistola → shotgun · Blender · 24 fps',font=font,fill=(224,224,213))
    return image
previews=[preview(f) for f in frames]
previews[0].save(BASE/'troca-shotgun.gif',save_all=True,append_images=previews[1:],duration=[500]+[42]*30+[700],loop=0,disposal=2)
sheet=Image.new('RGBA',(2048,1024))
for i,frame in enumerate(frames): sheet.paste(frame.resize((256,256),Image.Resampling.LANCZOS),((i%8)*256,(i//8)*256))
sheet.save(BASE/'sprite-sheet-troca-shotgun.png')
contact=Image.new('RGB',(1600,385),(27,29,26)); draw=ImageDraw.Draw(contact)
for j,(frame,label) in enumerate([(1,'1 · Pistola'),(10,'10 · Guardar'),(15,'15 · Sacar'),(22,'22 · Apoiar'),(28,'28 · Shotgun')]):
    image=frames[frame-1]; contact.paste(image,(j*320,40),image); draw.text((j*320+12,10),label,font=font,fill=(224,224,213))
contact.save(BASE/'etapas-troca-shotgun.png')
print('Preview, contact sheet and 32-frame sprite atlas saved.')
