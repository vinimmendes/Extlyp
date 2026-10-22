from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
BASE=Path(__file__).resolve().parent
font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',23)
for folder,count,name,label,stages in [
    ('RifleSwitch',32,'troca-rifle','Pistola → rifle · Blender',[(1,'Pistola'),(10,'Guardar'),(15,'Sacar'),(22,'Apoiar'),(28,'Rifle')]),
    ('RifleReload',78,'recarga-rifle','Recarga rifle · cinco tiros · Blender',[(1,'Mira'),(22,'Abrir'),(38,'Pente'),(51,'Carregar'),(56,'Retirar'),(78,'Pronto')])]:
    dest=BASE/folder; frames=[Image.open(dest/'frames'/f'{i:03}.png').convert('RGBA') for i in range(1,count+1)]
    views=[]
    for f in frames:
        image=Image.new('RGB',(640,680),(27,29,26)); f=f.resize((640,640),Image.Resampling.LANCZOS); image.paste(f,(0,40),f)
        ImageDraw.Draw(image).text((18,8),label,font=font,fill=(224,224,213)); views.append(image)
    views[0].save(dest/(name+'.gif'),save_all=True,append_images=views[1:],duration=[500]+[42]*(count-2)+[700],loop=0,disposal=2)
    # Keep each atlas within 2048x2048. Reload spans two sheets.
    for sheet_index in range((count+63)//64):
        group=frames[sheet_index*64:(sheet_index+1)*64]; sheet=Image.new('RGBA',(2048,((len(group)+7)//8)*256))
        for i,f in enumerate(group): sheet.paste(f.resize((256,256),Image.Resampling.LANCZOS),((i%8)*256,(i//8)*256))
        sheet.save(dest/f'sprite-sheet-{name}-{sheet_index+1}.png')
    contact=Image.new('RGB',(320*len(stages),360),(27,29,26)); draw=ImageDraw.Draw(contact)
    for j,(f,text) in enumerate(stages):
        image=frames[f-1].resize((320,320),Image.Resampling.LANCZOS); contact.paste(image,(j*320,40),image)
        draw.text((j*320+12,8),text,font=font,fill=(224,224,213))
    contact.save(dest/('etapas-'+name+'.png'))
print('Rifle animation previews packaged.')
