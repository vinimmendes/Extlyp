from pathlib import Path
from PIL import Image
BASE=Path(__file__).resolve().parent
OUT=BASE/'ShotgunSwitch'/'RuntimeExport'
RESOURCES=BASE.parents[1]/'Assets'/'Resources'/'PlayerCharacter'
for variant in ['Shotgun','Legs','SwitchShotgun']:
    sheet=Image.new('RGBA',(2048,1024))
    for index in range(32):
        frame=Image.open(OUT/variant/f'{index+1:03}.png').convert('RGBA')
        if frame.size!=(256,256): raise ValueError('Unexpected Blender frame dimensions')
        sheet.paste(frame,((index%8)*256,(index//8)*256))
    sheet.save(RESOURCES/(variant+'.png'))
print('Runtime shotgun, legs and upper-body transition atlases installed.')
