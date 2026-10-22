"""Read-only structural inspection of authored Unity YAML; no Unity/test session."""
from pathlib import Path
import re,json
ROOT=Path(__file__).resolve().parents[2]
base=ROOT/'Assets/Resources/UI/Prefabs'
files=sorted(base.glob('*.prefab'))
issues=[]
for path in files:
    source=path.read_text(encoding='utf-8')
    ids=set(re.findall(r'^--- !u!\d+ &(\d+)$',source,re.M))
    for match in re.finditer(r'\{fileID: (\d+)\}',source):
        target=match.group(1)
        if target!='0' and target not in ids: issues.append(path.name+': unresolved local fileID '+target)
    if re.search(r'^\s*\w+:\n\s*\[\]\s*$',source,re.M):issues.append(path.name+': empty lists must be inline for Unity YAML')
    transforms={m.group(1):m.group(2) for m in re.finditer(r'^--- !u!224 &(\d+)\n(RectTransform:\n.*?)(?=^--- !u!|\Z)',source,re.M|re.S)}
    for identifier,block in transforms.items():
        father=re.search(r'm_Father: \{fileID: (\d+)\}',block).group(1)
        children=re.search(r'm_Children:(.*?)(?=\n  m_Father:)',block,re.S).group(1)
        for child in re.findall(r'fileID: (\d+)',children):
            if child not in transforms:issues.append(path.name+': child '+child+' is not a RectTransform')
            elif 'm_Father: {fileID: '+identifier+'}' not in transforms[child]:issues.append(path.name+': inconsistent parent for '+child)
        if father!='0':
            if father not in transforms:issues.append(path.name+': missing parent '+father)
            else:
                siblings=re.search(r'm_Children:(.*?)(?=\n  m_Father:)',transforms[father],re.S).group(1)
                if '{fileID: '+identifier+'}' not in siblings:issues.append(path.name+': parent does not reference '+identifier)
    if 'm_text: "Curar\n×3"' in source:issues.append(path.name+': unescaped multiline caption')
    rects=re.findall(r'm_SizeDelta: \{x: ([\d.-]+), y: ([\d.-]+)\}',source)
    if not rects:issues.append(path.name+': no RectTransform geometry')
    print(path.name,len(ids),'serialized objects')
if len(files)!=10:issues.append('Expected root, 8 views and reusable ItemSlot (10 prefabs)')
hud=(base/'Hud.prefab').read_text(encoding='utf-8')
required={'Organizer':['Slider'], 'Expedition':['Portrait','Viewport','Content'],
          'Inventory':['DragVisual','DragIcon','DragCaption']}
for name,fields in required.items():
    source=(base/(name+'.prefab')).read_text(encoding='utf-8')
    for field in fields:
        if not re.search(r'^  '+field+r': \{fileID: [1-9]\d*\}$',source,re.M):
            issues.append(name+': missing required '+field+' reference')
expedition=(base/'Expedition.prefab').read_text(encoding='utf-8')
viewport_image=re.search(r'^--- !u!114 &1071\n.*?(?=^--- !u!|\Z)',expedition,re.M|re.S)
if viewport_image is None or '  m_PreserveAspect: 0' not in viewport_image.group(0):
    issues.append('Expedition: rectangular description mask must not preserve square sprite aspect')
cabin=(base/'Cabin.prefab').read_text(encoding='utf-8')
cabin_frame=re.search(r'^--- !u!114 &1004\n.*?(?=^--- !u!|\Z)',cabin,re.M|re.S).group(0)
if '  m_Enabled: 0\n' not in cabin_frame or '  m_RaycastTarget: 0\n' not in cabin_frame:
    issues.append('Cabin: full-screen container must not draw or intercept pointers')
if not re.search(r'  EnvironmentCards:\n  - \{fileID: 9001\}\n  - \{fileID: 9101\}',expedition):
    issues.append('Expedition: both editable environment cards must be wired')
if '  EnvironmentArtwork: {fileID: 9203}' not in expedition:
    issues.append('Expedition: approved selection artwork must be wired')
artwork=re.search(r'^--- !u!114 &9203\n.*?(?=^--- !u!|\Z)',expedition,re.M|re.S)
if artwork is None or '  m_RaycastTarget: 0' not in artwork.group(0):
    issues.append('Expedition: artwork must not intercept the independent buttons')
for field,targets in {'EnvironmentPortraits':[9303,9403], 'EnvironmentTitles':[9013,9113], 'EnvironmentDescriptions':[9023,9123]}.items():
    expected='  '+field+':\n'+''.join('  - {fileID: '+str(target)+'}\n' for target in targets)
    if expected not in expedition:issues.append('Expedition: separated entry '+field+' references missing')
if '  EnvironmentSubtitle: {fileID: 9503}' not in expedition:
    issues.append('Expedition: editable entry subtitle missing')
root_children=re.search(r'^--- !u!224 &1002\n.*?m_Children:(.*?)(?=\n  m_Father:)',expedition,re.M|re.S).group(1)
if root_children.find('9001')>root_children.find('1022'):
    issues.append('Expedition: cards must render before the action buttons')
for name in ['Expedition','Inventory','Activities']:
    source=(base/(name+'.prefab')).read_text(encoding='utf-8')
    for field in ['Backdrop','Surface']:
        if not re.search(r'^  '+field+r': \{fileID: [1-9]\d*\}$',source,re.M):issues.append(name+': missing live menu '+field)
for name,count in [('Inventory',18),('Activities',9),('ItemSlot',1)]:
    source=(base/(name+'.prefab')).read_text(encoding='utf-8')
    if len(re.findall(r'^  quantity: \{fileID: [1-9]\d*\}$',source,re.M))!=count:
        issues.append(name+': every item cell needs an independent quantity label')
activities=(base/'Activities.prefab').read_text(encoding='utf-8')
for field in ['ResourceIcons','ResourceValues','RecipeCards','RecipeIcons','RecipeTitles','RecipeDetails']:
    match=re.search(r'^  '+field+r':\n((?:  - \{fileID: \d+\}\n)+)',activities,re.M)
    if match is None or len(re.findall('fileID:',match.group(1)))!=3:issues.append('Activities: expected three live '+field)
if len(re.findall(r'^  - Control: \d+$',hud,re.M))!=16:issues.append('HUD must expose all 16 independent controls')
if issues:raise SystemExit('\n'.join(issues))
print('Structural inspection passed; native import/rendering remain unverified.')
