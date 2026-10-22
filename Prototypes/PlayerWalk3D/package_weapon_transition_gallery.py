from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
def package(base):
    labels={'ShotgunToPistol':'Shotgun → pistola','RifleToPistol':'Rifle → pistola','ShotgunToRifle':'Shotgun → rifle','RifleToShotgun':'Rifle → shotgun'}
    font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',20); frames=[]
    for index in range(1,37):
        view=Image.new('RGB',(840,920),(27,29,26)); draw=ImageDraw.Draw(view)
        for j,(name,label) in enumerate(labels.items()):
            x=(j%2)*420; y=(j//2)*460
            f=Image.open(base/name/'frames'/f'{index:03}.png').convert('RGBA').resize((420,420),Image.Resampling.LANCZOS)
            view.paste(f,(x,y+40),f); draw.text((x+12,y+8),label,font=font,fill=(224,224,213))
        frames.append(view)
    frames[0].save(base/'trocas-comparacao.gif',save_all=True,append_images=frames[1:],duration=[500]+[42]*34+[700],loop=0,disposal=2)
if __name__=='__main__':
    package(Path(__file__).resolve().parent/'WeaponTransitions')
    print('Four weapon transitions packaged for visual comparison.')
