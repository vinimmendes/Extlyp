from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
BASE=Path(__file__).resolve().parent/'PistolReload'
frames=[Image.open(BASE/'frames'/f'{i:03}.png').convert('RGBA') for i in range(1,38)]
font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',23)
views=[]
for frame in frames:
    image=Image.new('RGB',(640,680),(27,29,26)); f=frame.resize((640,640),Image.Resampling.LANCZOS)
    image.paste(f,(0,40),f); ImageDraw.Draw(image).text((18,8),'Recarga da pistola · Blender · 1,5 s',font=font,fill=(224,224,213)); views.append(image)
views[0].save(BASE/'recarga-pistola.gif',save_all=True,append_images=views[1:],duration=[500]+[42]*35+[700],loop=0,disposal=2)
sheet=Image.new('RGBA',(2048,1280))
for i,f in enumerate(frames): sheet.paste(f.resize((256,256),Image.Resampling.LANCZOS),((i%8)*256,(i//8)*256))
sheet.save(BASE/'sprite-sheet-recarga-pistola.png')
contact=Image.new('RGB',(1920,360),(27,29,26)); draw=ImageDraw.Draw(contact)
for j,(frame,label) in enumerate([(1,'Mira'),(14,'Retirar'),(20,'Buscar'),(24,'Alinhar'),(28,'Encaixar'),(37,'Pronto')]):
    f=frames[frame-1].resize((320,320),Image.Resampling.LANCZOS); contact.paste(f,(j*320,40),f)
    draw.text((j*320+12,8),label,font=font,fill=(224,224,213))
contact.save(BASE/'etapas-recarga-pistola.png')
print('Pistol reload preview and atlas packaged.')
