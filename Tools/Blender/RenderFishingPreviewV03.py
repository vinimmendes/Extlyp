"""Render just the revised horizontal tracking segment and the theme still."""
from pathlib import Path
p=Path('C:/Users/Vinni/Documents/GitHub/Extlyp/Tools/Blender/RenderFishingPreviewV02.py')
code=p.read_text(encoding='utf-8').replace('v02','v03').replace('V02','V03')
code=code.replace('range(1,217)','range(117,190)')
exec(compile(code,str(p)+' [horizontal-v03]','exec'))
