"""Asset metadata only. Never imports assets or launches Unity."""
from pathlib import Path
import hashlib
import re

ROOT = Path(__file__).resolve().parents[2]
def guid(p): return hashlib.md5(('CyberExtraction/refined-hud/'+p.relative_to(ROOT).as_posix()).encode()).hexdigest()
def write_meta(p, body):
    m=Path(str(p)+'.meta')
    if not m.exists(): m.write_text('fileFormatVersion: 2\nguid: '+guid(p)+'\n'+body,encoding='utf-8')

for name in ['HudLayout','HudMetrics','RefinedHudArt']:
    write_meta(ROOT/'Assets/Scripts/UI'/f'{name}.cs','MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
write_meta(ROOT/'Assets/Editor/HudMetricsSimulation.cs','MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
folder=ROOT/'Assets/Resources/UI/RefinedHud'
write_meta(folder,'folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
write_meta(folder/'Visuals.json','TextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
template=(ROOT/'Assets/Resources/UI/Art/Aim.png.meta').read_text()
for name in ['Atlas0.png','Atlas1.png']:
    p=folder/name; m=Path(str(p)+'.meta')
    if m.exists(): continue
    s=re.sub(r'guid: \w+', 'guid: '+guid(p), template,count=1)
    s=s.replace('maxTextureSize: 512','maxTextureSize: 1024').replace('spriteMode: 1','spriteMode: 0').replace('textureType: 8','textureType: 0')
    s=re.sub(r'(textureFormat: )-?\d+',r'\g<1>4',s)
    s=re.sub(r'(wrap[UVW]: )0',r'\g<1>1',s)
    s=s.replace('overridden: 0','overridden: 1')
    m.write_text(s,encoding='utf-8')
print('Metadata prepared: two uncompressed RGBA32 1024 atlases, no mipmaps/readback.')
