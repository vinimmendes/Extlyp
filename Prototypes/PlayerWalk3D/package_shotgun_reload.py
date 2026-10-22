from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
BASE=Path(__file__).resolve().parent/'ShotgunReload'
frames=[Image.open(BASE/'frames'/f'{i:03}.png').convert('RGBA') for i in range(1,65)]
font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',23)
views=[]
for f in frames:
    image=Image.new('RGB',(640,680),(27,29,26)); f=f.resize((640,640),Image.Resampling.LANCZOS); image.paste(f,(0,40),f)
    ImageDraw.Draw(image).text((18,8),'Recarga shotgun · dois cartuchos · Blender',font=font,fill=(224,224,213)); views.append(image)
views[0].save(BASE/'recarga-shotgun.gif',save_all=True,append_images=views[1:],duration=[500]+[42]*62+[700],loop=0,disposal=2)
sheet=Image.new('RGBA',(2048,2048))
for i,f in enumerate(frames): sheet.paste(f.resize((256,256),Image.Resampling.LANCZOS),((i%8)*256,(i//8)*256))
sheet.save(BASE/'sprite-sheet-recarga-shotgun.png')
contact=Image.new('RGB',(1920,360),(27,29,26)); draw=ImageDraw.Draw(contact)
for j,(f,label) in enumerate([(1,'Mira'),(14,'Abrir'),(18,'Retirar'),(34,'Primeiro'),(47,'Segundo'),(64,'Pronto')]):
    image=frames[f-1].resize((320,320),Image.Resampling.LANCZOS); contact.paste(image,(j*320,40),image)
    draw.text((j*320+12,8),label,font=font,fill=(224,224,213))
contact.save(BASE/'etapas-recarga-shotgun.png')
print('Shotgun reload preview and atlas packaged.')
