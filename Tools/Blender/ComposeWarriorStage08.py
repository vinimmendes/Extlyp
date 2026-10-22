"""Assemble unchanged frame renders at equal scale for approval."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont

root=Path('C:/Users/Vinni/Documents/GitHub/Extlyp/Art/PlayerProposals/WarriorArtV3/stage08-approval')
canvas=Image.new('RGB',(1280,750),(25,28,32))
d=ImageDraw.Draw(canvas)
fontpath='C:/Windows/Fonts/segoeui.ttf'
title=ImageFont.truetype(fontpath,25)
small=ImageFont.truetype(fontpath,15)
for i,(name,label,description) in enumerate([
    ('warrior-v2-frame01-same-blender.png','V2 — base recebida','Render refeito no Blender 5.2.2; luz e materiais originais'),
    ('warrior-v3-frame01.png','V3 — etapas 3 a 8','Armadura, equipamentos, tecido, materiais, chama e luz')]):
    x=i*640
    image=Image.open(root/name).convert('RGBA')
    if image.size!=(640,640):raise ValueError('Expected original 640px frame')
    canvas.paste(image,(x,70),image)
    d.text((x+24,15),label,font=title,fill=(235,229,215))
    d.text((x+24,49),description,font=small,fill=(173,180,184))
d.line([(640,10),(640,711)],fill=(67,73,78),width=1)
d.text((24,723),'Mesmo quadro 1, câmera e escala • Cycles, 128 amostras e denoising • Sem recorte ou reposicionamento dos sprites',font=small,fill=(173,180,184))
dest=root/'comparacao-v2-v3-quadro01.png'
canvas.save(dest)
print(dest)
