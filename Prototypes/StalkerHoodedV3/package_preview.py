"""Presentation of unmodified Blender renders and original concept."""
from PIL import Image, ImageDraw, ImageFont
from pathlib import Path
BASE=Path(__file__).resolve().parent
ROOT=BASE.parent.parent
font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',27)
small=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',19)
bg=(23,25,26)
def place(canvas,source,box,crop_alpha=True):
    im=source.convert('RGBA')
    if crop_alpha:
        bounds=im.getchannel('A').getbbox()
        if bounds:im=im.crop(bounds)
    im.thumbnail((box[2],box[3]),Image.Resampling.LANCZOS)
    x=box[0]+(box[2]-im.width)//2;y=box[1]+(box[3]-im.height)//2
    canvas.paste(im,(x,y),im)
sheet=Image.new('RGB',(1650,1000),bg);draw=ImageDraw.Draw(sheet)
for x,label in [(30,'Referência conceitual'),(575,'Modelo Blender V3'),(1130,'Vista superior')]:
    draw.text((x,24),label,font=font,fill=(230,225,212))
concept=Image.open(ROOT/'Art/MonsterProposals/Espreitador/espreitador-encapuzado-conceito-v2.png')
place(sheet,concept.crop((0,0,930,1024)),(20,90,530,840),False)
place(sheet,Image.open(BASE/'tres-quartos-v3.png'),(570,90,530,840))
place(sheet,Image.open(BASE/'superior-v3.png'),(1130,120,490,470))
top=Image.open(BASE/'superior-v3.png').convert('RGBA');top.thumbnail((256,256),Image.Resampling.LANCZOS)
sheet.paste(top,(1250,660),top)
draw.text((1180,610),'Leitura na célula de 256 pixels',font=small,fill=(192,188,179))
draw.text((30,960),'Modelo estático · Blender 5.2.2 · materiais e malhas editáveis · sem integração Unity',font=small,fill=(180,180,177))
sheet.save(BASE/'prancha-conceito-v3.png')
comparison=Image.new('RGB',(1200,1000),bg);d=ImageDraw.Draw(comparison)
d.text((30,20),'Modelo anterior V2',font=font,fill=(225,220,205));d.text((630,20),'Modelo revisado V3',font=font,fill=(225,220,205))
place(comparison,Image.open(BASE.parent/'StalkerHoodedV2/pose-tres-quartos.png'),(20,85,560,865))
place(comparison,Image.open(BASE/'tres-quartos-v3.png'),(620,85,560,865))
comparison.save(BASE/'comparacao-v2-v3.png')
print('Presentation sheets saved.')
