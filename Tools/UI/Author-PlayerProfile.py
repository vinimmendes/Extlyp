"""Add the profile prefab and one base tab, preserving all existing objects.
One-time asset authoring; does not launch Unity or rebuild scenes.
"""
from pathlib import Path
import ast, hashlib, json, math, re, uuid
ROOT=Path(__file__).resolve().parents[2]
ASSETS=ROOT/'Assets/Resources/UI'
SCRIPTS=ROOT/'Assets/Scripts/UI'
PACKAGE=next((ROOT/'Library/PackageCache').glob('com.unity.ugui@*'))
refresh=repair=False
manifest={}
BASE='  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n'
source=(ROOT/'Tools/UI/Author-PlayerInterface.py').read_text(encoding='utf-8-sig')
names={'guid','metadata','script','pkg','Node','Doc','ref','seq','label','button'}
exec(compile(ast.Module(body=[n for n in ast.parse(source).body if isinstance(n,(ast.FunctionDef,ast.ClassDef)) and n.name in names],type_ignores=[]),'<UI definitions>','exec'))
generated_guid=guid
def guid(path):
    meta=ROOT/(str(path)+'.meta')
    return re.search(r'guid: (\w+)',meta.read_text())[1] if meta.exists() else generated_guid(path)
sprites={n:re.search(r'guid: (\w+)',(ASSETS/'Art'/(n+'.png.meta')).read_text())[1] for n in ['PanelSomber','PanelSimple']}
for path in ['Camp/PlayerProfile','Camp/ProfileStatistics','Combat/Health.Profile','UI/UguiProfilePresenter','UI/PlayerProfileView']:
    metadata(ROOT/'Assets/Scripts'/(path+'.cs'))

if not (ASSETS/'Prefabs/PlayerProfile.prefab').exists():
    doc=Doc('PlayerProfile')
    root=Node(doc,'PlayerProfile',box=(16,116,1034,432)); root.image('PanelSomber',True,True)
    def text(parent,name,caption,size,box):
        node,component=label(doc,parent,name,caption,size,box)
        node.components=[(t,i,d.replace('m_HorizontalAlignment: 2','m_HorizontalAlignment: 1').replace('m_VerticalAlignment: 512','m_VerticalAlignment: 256')) for t,i,d in node.components]
        return component
    title=text(root,'Title','SEU PERFIL',25,(16,8,348,48))
    hint=text(root,'NameRules','Escolha um nome de 3 a 20 caracteres.',16,(16,58,1002,38))
    field=Node(doc,'PlayerNameInput',root,(372,8,401,48)); graphic=field.image('PanelSomber',True,True)
    area=Node(doc,'TextArea',field,(10,3,381,42)); area.mono(pkg('RectMask2D'),'  m_Padding: {x: 0, y: 0, z: 0, w: 0}\n  m_Softness: {x: 0, y: 0}\n')
    value=text(area,'Text','',23,(0,0,381,42))
    placeholder=text(area,'Placeholder','Escolha seu nome',21,(0,0,381,42))
    nameinput=field.mono(pkg('TMP_InputField'),f'''  m_Navigation:
    m_Mode: 3
  m_Transition: 0
  m_Interactable: 1
  m_TargetGraphic: {ref(graphic)}
  m_TextViewport: {ref(area.rect)}
  m_TextComponent: {ref(value)}
  m_Placeholder: {ref(placeholder)}
  m_ContentType: 0
  m_InputType: 0
  m_KeyboardType: 0
  m_LineType: 0
  m_CharacterValidation: 0
  m_CharacterLimit: 160
  m_Text:
  m_CaretBlinkRate: 0.85
  m_CaretWidth: 2
  m_SelectionColor: {{r: 0.5, g: 0.6, b: 0.4, a: 0.6}}
  m_ReadOnly: 0
  m_RichText: 0
  m_GlobalFontAsset: {{fileID: 11400000, guid: {guid('Assets/Resources/UI/Fonts/BarlowCondensed-Regular-SDF.asset')}, type: 2}}
  m_GlobalPointSize: 23
''')
    _,save=button(doc,root,'SaveName','Confirmar nome',(786,8,232,48))
    sections=[button(doc,root,'Section'+str(i),n,(16+i*200,100,196,48))[1] for i,n in enumerate(['Resumo','Expedições','Combate','Criaturas','Histórico'])]
    iconnode=Node(doc,'WeaponIcon',root,(20,154,52,52)); icon=iconnode.image(preserve=True)
    iconnode.components=[(t,i,d.replace('a: 0}','a: 1}')) for t,i,d in iconnode.components]
    favorite=text(root,'Favorite','ARMA PREFERIDA · Ainda não definida',21,(84,154,934,52))
    scrollnode=Node(doc,'StatisticsScroll',root,(16,214,1002,206))
    viewport=Node(doc,'Viewport',scrollnode,(0,0,1002,206)); viewport.image(ray=True)
    viewport.components=[(t,i,d.replace('m_CullTransparentMesh: 1','m_CullTransparentMesh: 0')) for t,i,d in viewport.components]
    viewport.mono(pkg('RectMask2D'),'  m_Padding: {x: 0, y: 0, z: 0, w: 0}\n  m_Softness: {x: 0, y: 0}\n')
    content=Node(doc,'Content',viewport,(0,0,990,206))
    body=text(content,'Statistics','Sincronizando perfil…',21,(4,0,982,206))
    scroll=scrollnode.mono(pkg('ScrollRect'),f'''  m_Content: {ref(content.rect)}
  m_Horizontal: 0
  m_Vertical: 1
  m_MovementType: 2
  m_Elasticity: 0.1
  m_Inertia: 1
  m_DecelerationRate: 0.135
  m_ScrollSensitivity: 28
  m_Viewport: {ref(viewport.rect)}
''')
    fields=dict(Panel=root.rect,Title=title,Hint=hint,Favorite=favorite,Body=body,NameInput=nameinput,Save=save,WeaponIcon=icon,Scroll=scroll)
    root.mono(script('PlayerProfileView'),''.join(f'  {k}: {ref(v)}\n' for k,v in fields.items())+seq('Sections',sections))
    doc.save()

path=ASSETS/'Prefabs/BaseHub.prefab'
source=path.read_text(encoding='utf-8')
if 'ProfileTab' not in source:
    objects={int(m[2]):(int(m[1]),m[3]) for m in re.finditer(r'^--- !u!(\d+) &(\d+)\n([\s\S]*?)(?=^--- !u!|\Z)',source,re.M)}
    tabs=re.search(r'  Tabs:\n((?:  - \{fileID: \d+\}\n)+)',source)
    component=int(re.search(r'fileID: (\d+)',tabs[1])[1])
    go=int(re.search(r'm_GameObject: \{fileID: (\d+)\}',objects[component][1])[1]); ids=set()
    def visit(go):
        ids.add(go)
        for comp in map(int,re.findall(r'component: \{fileID: (\d+)\}',objects[go][1])):
            ids.add(comp)
            if objects[comp][0]==224:
                for child in re.findall(r'fileID: (\d+)',objects[comp][1].split('  m_Children:',1)[1].split('  m_Father:',1)[0]):
                    visit(int(re.search(r'm_GameObject: \{fileID: (\d+)\}',objects[int(child)][1])[1]))
    visit(go)
    mapping={old:max(objects)+100+i for i,old in enumerate(sorted(ids))}
    rect=next(i for i in ids if objects[i][0]==224 and f'm_GameObject: {{fileID: {go}}}' in objects[i][1])
    parent=int(re.search(r'm_Father: \{fileID: (\d+)\}',objects[rect][1])[1])
    added=''
    for old in sorted(ids):
        kind,body=objects[old]
        body=re.sub(r'fileID: (\d+)(?=\})',lambda m:'fileID: '+str(mapping.get(int(m[1]),int(m[1]))),body)
        if old==go: body=re.sub(r'  m_Name: .*','  m_Name: ProfileTab',body)
        if '  m_text:' in body: body=re.sub(r'  m_text: .*','  m_text: Perfil',body)
        added+=f'--- !u!{kind} &{mapping[old]}\n'+body
    source=source.replace(objects[parent][1],objects[parent][1].replace('  m_Father:',f'  - {{fileID: {mapping[rect]}}}\n  m_Father:',1),1)
    source=source.replace(tabs[0],tabs[0]+f'  - {{fileID: {mapping[component]}}}\n',1)
    path.write_text(source+added,encoding='utf-8')
print('Profile prefab and base tab authored; existing objects preserved.')
