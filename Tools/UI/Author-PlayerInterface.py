"""Author editable Unity UI prefabs/assets without starting the Unity Editor.
Run deliberately for initial authoring; never invoked by gameplay or automatic validation.
"""
from pathlib import Path
import hashlib,json,math,re,uuid,sys
from PIL import Image,ImageDraw,ImageChops
ROOT=Path(__file__).resolve().parents[2]
ASSETS=ROOT/'Assets/Resources/UI'
SCRIPTS=ROOT/'Assets/Scripts/UI'
PACKAGE=ROOT/'Library/PackageCache/com.unity.ugui@5d6a93c81cc4'
refresh='--refresh-authored' in sys.argv
repair='--repair-imported-transforms' in sys.argv
manifest_path=ROOT/'Tools/UI/initial-prefabs.json'
manifest=json.loads(manifest_path.read_text()) if refresh else {}
def guid(path): return uuid.uuid5(uuid.NAMESPACE_URL,'cyber-extraction-ui/'+str(path)).hex
def metadata(path,body='MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n'):
    meta=Path(str(path)+'.meta')
    if not meta.exists(): meta.write_text('fileFormatVersion: 2\nguid: '+guid(path.relative_to(ROOT))+'\n'+body,encoding='utf-8')
    return re.search(r'guid: (\w+)',meta.read_text()).group(1)
def script(name): return metadata(SCRIPTS/(name+'.cs'))
def pkg(name):
    path=next(PACKAGE.rglob(name+'.cs.meta'))
    return re.search(r'guid: (\w+)',path.read_text()).group(1)
def folder(path):
    if path.exists(): return
    path.mkdir(parents=True,exist_ok=True)
    metadata(path,'folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n')
for path in [ASSETS/'Art',ASSETS/'Prefabs']: folder(path)
for path in SCRIPTS.glob('*.cs'): metadata(path)
art=(SCRIPTS/'HudArt.cs').read_text(encoding='utf-8')
def make_icon(symbol):
    if symbol=='Rifle': body=re.search(r'case HudSymbol.Shotgun:(.*?)(?:break;)',art,re.S).group(1).replace('symbol==HudSymbol.Shotgun ? 10 : 6','6')
    else:
        body=re.search(r'case HudSymbol\.'+symbol+r':(.*?)(?:break;)',art,re.S).group(1).replace('symbol==HudSymbol.Shotgun ? 10 : 6','10')
    for pattern,values in [(r'for\(int x=30;x<=90;x\+=30\)\s*\{(.*?)\}',[30,60,90]),(r'for\(int x=42;x<=86;x\+=22\)\s*(p\.Path\(.*?\);)',[42,64,86]),(r'for\(int y=40;y<=88;y\+=24\)\s*(p\.Path\(.*?\);)',[40,64,88])]:
        m=re.search(pattern,body,re.S)
        if m:
            variable='y' if 'int y=' in m.group(0) else 'x'
            body=body[:m.start()]+' '.join(re.sub(r'\b'+variable+r'\b',str(v),m.group(1)) for v in values)+body[m.end():]
    mask=Image.new('L',(512,512)); d=ImageDraw.Draw(mask)
    for method,args in re.findall(r'p\.(Line|Etch|Path|Box|Ring|Solid|Cut)\(([^;]+?)\);',body):
        n=[float(eval(v,{'__builtins__':{}},{})) for v in args.split(',')]
        if method in ('Line','Etch'):
            x,y,u,v=n[:4]; width=n[4] if len(n)>4 else 3
            if method=='Line': d.line((x*4,y*4,u*4,v*4),fill=255,width=round(width*4))
            else:
                scratch=Image.new('L',mask.size); ImageDraw.Draw(scratch).line((x*4,y*4,u*4,v*4),fill=199,width=round(width*4))
                mask=ImageChops.multiply(mask,ImageChops.invert(scratch)); d=ImageDraw.Draw(mask)
        elif method=='Path': d.line([(n[i]*4,n[i+1]*4) for i in range(1,len(n),2)],fill=255,width=round(n[0]*4),joint='curve')
        elif method in ('Box','Cut'):
            x,y,w,h=n[:4]; stroke=n[4] if len(n)>4 else 3
            d.rectangle((x*4,y*4,(x+w)*4,(y+h)*4),fill=0 if method=='Cut' else None,outline=None if method=='Cut' else 255,width=round(stroke*4))
        elif method=='Ring':
            x,y,r=n[:3]; stroke=n[3] if len(n)>3 else 3; d.ellipse(((x-r)*4,(y-r)*4,(x+r)*4,(y+r)*4),outline=255,width=round(stroke*4))
        else: d.polygon([(n[i]*4,n[i+1]*4) for i in range(0,len(n),2)],fill=255)
    result=Image.new('RGBA',(256,256),'white'); result.putalpha(mask.resize((256,256),Image.Resampling.LANCZOS)); return result
def sprite(name,image,border=0):
    path=ASSETS/'Art'/f'{name}.png'
    if not path.exists(): image.save(path)
    metadata(path,f'''TextureImporter:
  serializedVersion: 13
  mipmaps:
    enableMipMap: 0
    sRGBTexture: 1
  isReadable: 0
  textureType: 8
  textureShape: 1
  spriteMode: 1
  spriteMeshType: 0
  spritePixelsToUnits: 100
  spritePivot: {{x: 0.5, y: 0.5}}
  spriteAlignment: 0
  spriteBorder: {{x: {border}, y: {border}, z: {border}, w: {border}}}
  alphaIsTransparency: 1
  nPOTScale: 0
  maxTextureSize: 512
  textureSettings:
    filterMode: 1
    wrapU: 1
    wrapV: 1
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 512
    textureCompression: 0
    overridden: 0
''')
    return metadata(path)
symbols=['Heal','Swap','Ammo','Backpack','Bullet','Aim','Map','Menu','Food','Spores','Chest','Exit','Arrow','Pistol','Rifle','Shotgun','Wind']
sprites={s:sprite(s,make_icon(s)) for s in symbols}
for name,color,edge in [('PanelSomber',(19,23,17,190),(137,120,86,180)),('PanelSimple',(23,36,29,245),(97,179,135,220))]:
    im=Image.new('RGBA',(64,64)); p=im.load()
    for y in range(64):
        for x in range(64):
            corner=(x<3 and y<3) or (x>60 and y<3) or (x<3 and y>60) or (x>60 and y>60)
            if corner: continue
            if min(x,y,63-x,63-y)<2:
                variation=int(12*math.sin(x*.91+y*.71)) if name=='PanelSomber' else 0
                p[x,y]=tuple(max(0,min(255,c+variation)) for c in edge[:3])+(edge[3],)
            else:p[x,y]=color
    sprites[name]=sprite(name,im,8)
knob=Image.new('RGBA',(128,128)); kd=ImageDraw.Draw(knob); kd.ellipse((1,1,126,126),fill=(220,216,199,170),outline=(230,217,186,230),width=2); sprites['Knob']=sprite('Knob',knob)

BASE='  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n'
class Node:
    def __init__(self,doc,name,parent=None,box=(0,0,100,100)):
        self.doc,self.name,self.parent,self.box=doc,name,parent,box; self.children=[]; self.components=[]; self.pivot=(0,1); self.rotation=0; self.active=True; self.anchors=((0,1),(0,1))
        self.go=doc.id();self.rect=doc.id();self.components.append((224,self.rect,'')); doc.nodes.append(self)
        if parent: parent.children.append(self)
    def mono(self,g,fields=''):
        n=self.doc.id();self.components.append((114,n,f'  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {{fileID: 11500000, guid: {g}, type: 3}}\n  m_Name:\n  m_EditorClassIdentifier:\n'+fields));return n
    def renderer(self):
        if any(t==222 for t,_,_ in self.components):return
        self.components.append((222,self.doc.id(),'  m_CullTransparentMesh: 1\n'))
    def image(self,name=None,ray=False,frame=False,circle=False,prominent=False,preserve=None):
        self.renderer(); s=sprites.get(name,'')
        fields=f'''  m_Material: {{fileID: 0}}
  m_Color: {{r: 1, g: 1, b: 1, a: {1 if name or frame else 0}}}
  m_RaycastTarget: {int(ray)}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {{fileID: {21300000 if s else 0}{', guid: '+s+', type: 3' if s else ''}}}
  m_Type: {1 if frame and not circle else 0}
  m_PreserveAspect: {int(bool(name) and not frame) if preserve is None else int(preserve)}
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
'''
        if frame:fields+=f'  Circle: {int(circle)}\n  Prominent: {int(prominent)}\n'
        return self.mono(script('HudFrameGraphic') if frame else pkg('Image'),fields)
    def raw(self,ray=False):
        self.renderer();return self.mono(pkg('RawImage'),f'  m_Material: {{fileID: 0}}\n  m_Color: {{r: 1, g: 1, b: 1, a: 1}}\n  m_RaycastTarget: {int(ray)}\n  m_Maskable: 1\n  m_Texture: {{fileID: 0}}\n  m_UVRect: {{x: 0, y: 0, width: 1, height: 1}}\n')
    def text(self,text='',size=14,bold=False):
        self.renderer(); fontguid=guid('Assets/Resources/UI/Fonts/BarlowCondensed-'+('SemiBold' if bold else 'Regular')+'-SDF.asset')
        return self.mono(pkg('TextMeshProUGUI'),f'''  m_Material: {{fileID: 0}}
  m_Color: {{r: 0.9, g: 0.85, b: 0.73, a: 1}}
  m_RaycastTarget: 0
  m_Maskable: 1
  m_text: {json.dumps(text,ensure_ascii=False)}
  m_fontAsset: {{fileID: 11400000, guid: {fontguid}, type: 2}}
  m_sharedMaterial: {{fileID: 0}}
  m_fontColor: {{r: 0.9, g: 0.85, b: 0.73, a: 1}}
  m_fontSize: {size}
  m_fontSizeBase: {size}
  m_fontWeight: {600 if bold else 400}
  m_enableAutoSizing: 0
  m_fontSizeMin: 9
  m_fontSizeMax: 28
  m_fontStyle: 0
  m_HorizontalAlignment: 2
  m_VerticalAlignment: 512
  m_textAlignment: 65535
  m_enableWordWrapping: 1
  m_textWrappingMode: 1
  m_overflowMode: 0
  m_isRichText: 0
  m_margin: {{x: 2, y: 0, z: 2, w: 0}}
''')
    def button(self,graphic):
        return self.mono(pkg('Button'),f'''  m_Navigation:
    m_Mode: 3
    m_WrapAround: 0
    m_SelectOnUp: {{fileID: 0}}
    m_SelectOnDown: {{fileID: 0}}
    m_SelectOnLeft: {{fileID: 0}}
    m_SelectOnRight: {{fileID: 0}}
  m_Transition: 1
  m_Colors:
    m_NormalColor: {{r: 1, g: 1, b: 1, a: 1}}
    m_HighlightedColor: {{r: 1, g: 1, b: 1, a: 1}}
    m_PressedColor: {{r: 0.7, g: 0.7, b: 0.7, a: 1}}
    m_SelectedColor: {{r: 1, g: 1, b: 1, a: 1}}
    m_DisabledColor: {{r: 0.6, g: 0.6, b: 0.6, a: 0.6}}
    m_ColorMultiplier: 1
    m_FadeDuration: 0.1
  m_Interactable: 1
  m_TargetGraphic: {{fileID: {graphic}}}
  m_OnClick:
    m_PersistentCalls:
      m_Calls: []
''')
class Doc:
    def __init__(self,name):self.name=name;self.nodes=[];self.counter=1000
    def id(self):self.counter+=1;return self.counter
    def save(self):
        out='%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'
        for n in self.nodes:
            out+=f'--- !u!1 &{n.go}\nGameObject:\n'+BASE+f'  serializedVersion: 6\n  m_Component:\n'+''.join(f'  - component: {{fileID: {i}}}\n' for _,i,_ in n.components)+f'  m_Layer: 5\n  m_Name: {json.dumps(n.name,ensure_ascii=False)}\n  m_TagString: Untagged\n  m_Icon: {{fileID: 0}}\n  m_NavMeshLayer: 0\n  m_StaticEditorFlags: 0\n  m_IsActive: {int(n.active)}\n'
            for t,i,data in n.components:
                out+=f'--- !u!{t} &{i}\n'+{224:'RectTransform',114:'MonoBehaviour',222:'CanvasRenderer',223:'Canvas'}[t]+':\n'+BASE+f'  m_GameObject: {{fileID: {n.go}}}\n'
                if t==224:
                    x,y,w,h=n.box
                    out+=f'  m_LocalRotation: {{x: 0, y: 0, z: {math.sin(math.radians(n.rotation)/2)}, w: {math.cos(math.radians(n.rotation)/2)}}}\n  m_LocalPosition: {{x: 0, y: 0, z: 0}}\n  m_LocalScale: {{x: 1, y: 1, z: 1}}\n  m_ConstrainProportionsScale: 0\n'
                    out+=('  m_Children:\n'+''.join(f'  - {{fileID: {c.rect}}}\n' for c in n.children)) if n.children else '  m_Children: []\n'
                    out+=f'  m_Father: {{fileID: {n.parent.rect if n.parent else 0}}}\n  m_AnchorMin: {{x: {n.anchors[0][0]}, y: {n.anchors[0][1]}}}\n  m_AnchorMax: {{x: {n.anchors[1][0]}, y: {n.anchors[1][1]}}}\n  m_AnchoredPosition: {{x: {x}, y: {-y}}}\n  m_SizeDelta: {{x: {w}, y: {h}}}\n  m_Pivot: {{x: {n.pivot[0]}, y: {n.pivot[1]}}}\n'
                else:out+=data
        path=ASSETS/'Prefabs'/f'{self.name}.prefab'
        if repair:
            # Unity's failed YAML import replaced affected leaf transforms with
            # default geometry. Restore only that exact failure signature.
            source=path.read_text(encoding='utf-8')
            pattern=r'^--- !u!224 &(\d+)\nRectTransform:\n.*?(?=^--- !u!|\Z)'
            authored={m.group(1):m.group(0) for m in re.finditer(pattern,out,re.M|re.S)}
            count=0
            def restore(match):
                nonlocal count
                block=match.group(0)
                signature=['  m_Children: []','  m_AnchorMin: {x: 0.5, y: 0.5}',
                           '  m_AnchorMax: {x: 0.5, y: 0.5}',
                           '  m_AnchoredPosition: {x: 0, y: 0}',
                           '  m_SizeDelta: {x: 100, y: 100}',
                           '  m_Pivot: {x: 0.5, y: 0.5}']
                if all(s in block for s in signature) and match.group(1) in authored:
                    count+=1
                    return authored[match.group(1)]
                return block
            repaired=re.sub(pattern,restore,source,flags=re.M|re.S)
            # Fields following rejected empty lists were reset during import.
            # Restore only null view references, never replace configured ones.
            component_pattern=r'^--- !u!114 &(\d+)\nMonoBehaviour:\n.*?(?=^--- !u!|\Z)'
            views={m.group(1):m.group(0) for m in re.finditer(component_pattern,out,re.M|re.S)
                   if 'guid: '+script('UguiPrefabView')+',' in m.group(0)}
            def restore_references(match):
                block=match.group(0)
                intended=views.get(match.group(1))
                if intended:
                    for field,target in re.findall(r'^  (\w+): \{fileID: ([1-9]\d*)\}$',intended,re.M):
                        block=re.sub(r'^  '+field+r': \{fileID: 0\}$',
                                     '  '+field+': {fileID: '+target+'}',block,flags=re.M)
                return block
            repaired=re.sub(component_pattern,restore_references,repaired,flags=re.M|re.S)
            repaired=re.sub(r'^(\s*)m_Children:\n\s*\[\]\s*\n',r'\1m_Children: []\n',repaired,flags=re.M)
            if repaired!=source:
                import tempfile
                backup=Path(tempfile.gettempdir())/'CyberExtraction-UI-import-recovery'
                backup.mkdir(exist_ok=True)
                original=backup/(path.name+'.before-repair')
                if not original.exists():original.write_bytes(path.read_bytes())
                path.write_text(repaired,encoding='utf-8')
            print(path.name+': restored '+str(count)+' failed-import transforms; other components preserved')
            return path
        if path.exists() and (not refresh or manifest.get(str(path.relative_to(ROOT)))!=hashlib.sha256(path.read_bytes()).hexdigest()): raise RuntimeError('Refusing to overwrite edited prefab: '+str(path))
        path.write_text(out,encoding='utf-8');metadata(path,'PrefabImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n');return path
def ref(i):return f'{{fileID: {i}}}'
def seq(name,ids):return ('  '+name+':\n'+''.join('  - '+ref(i)+'\n' for i in ids)) if ids else '  '+name+': []\n'
def label(doc,parent,name,text='',size=14,box=(0,0,100,24),bold=False):
    n=Node(doc,name,parent,box);return n,n.text(text,size,bold)
def button(doc,parent,name,text='',box=(0,0,200,40)):
    n=Node(doc,name,parent,box);g=n.image('PanelSomber',True,True);b=n.button(g);caption,_=label(doc,n,'Caption',text,17,(0,0,-12,-6));caption.anchors=((0,0),(1,1));caption.pivot=(.5,.5);return n,b
def view(doc,root,fields):return root.mono(script('UguiPrefabView'),fields)
def slot(doc,parent,name):
    n=Node(doc,name,parent,(0,0,100,100));g=n.image('PanelSomber',True,True);n.button(g)
    icon=Node(doc,'Icon',n,(8,4,84,45));im=icon.image('Chest')
    cap,t=label(doc,n,'Caption','—',12,(3,51,94,45))
    icon.anchors=((.06,.51),(.94,.96));icon.box=(0,0,0,0);icon.pivot=(.5,.5)
    cap.anchors=((.03,.03),(.97,.48));cap.box=(0,0,0,0);cap.pivot=(.5,.5)
    component=n.mono(script('UguiInventorySlot'),f'  icon: {ref(im)}\n  caption: {ref(t)}\n  Grid: 0\n  Slot: 0\n')
    return n,component
def grid(doc,parent,name,count):
    n=Node(doc,name,parent,(20,60,312,312))
    n.mono(pkg('GridLayoutGroup'),'  m_Padding: {m_Left: 0, m_Right: 0, m_Top: 0, m_Bottom: 0}\n  m_ChildAlignment: 0\n  m_StartCorner: 0\n  m_StartAxis: 0\n  m_CellSize: {x: 98, y: 98}\n  m_Spacing: {x: 6, y: 6}\n  m_Constraint: 1\n  m_ConstraintCount: 3\n')
    ids=[slot(doc,n,f'Slot{i}')[1] for i in range(count)]
    return n,ids

definitions={}
layout=json.loads((ASSETS/'HudLayoutDefaults.json').read_text())
controls=['Health','Food','Spores','Clock','Map','Menu','Weapon','Heal','Swap','Reload','Backpack','Move','Aim','Fire','Interaction','Minimap']
doc=Doc('Hud');root=Node(doc,'Hud',box=(0,0,1066,600));els=[];mapnode=None
for entry in layout['Controls']:
    c=entry['Control'];p=entry['Placement'];w,h=p['Size']['x'],p['Size']['y'];x=1066*p['Anchor']['x']+p['Offset']['x'];y=600*p['Anchor']['y']+p['Offset']['y']
    n=Node(doc,controls[c],root,(x,y,w,h));hit=n.image(ray=True);b=n.button(hit);n.mono(script('UguiHudPointer'),f'  Control: {c}\n')
    visual=Node(doc,'Artwork',n,(0,0,w,h));bg=Node(doc,'Frame',visual,(0,0,w,h));circ=c in range(7,14);bgid=bg.image('PanelSomber',frame=True,circle=circ,prominent=c==13)
    selection=bg.mono(pkg('Outline'),'  m_EffectColor: {r: 0.67, g: 0.47, b: 0.29, a: 1}\n  m_EffectDistance: {x: 1, y: -1}\n  m_UseGraphicAlpha: 1\n')
    inner=Node(doc,'InnerRing',visual,(w*.29,h*.29,w*.42,h*.42));innerid=inner.image('PanelSomber',frame=True,circle=True)
    ic=Node(doc,'Icon',visual,(w*.22,h*.22,w*.56,h*.56));iid=ic.image(symbols[min(c,len(symbols)-1)])
    title,tid=label(doc,visual,'Title',controls[c],14,(0,h+3,w,24));main,mid=label(doc,visual,'Value','',22,(0,0,w,h),True);secondary,sid=label(doc,visual,'Detail','',12,(0,h+25,w,24));note,nid=label(doc,visual,'Context','',11,(0,h+3,w,48))
    track=Node(doc,'BarTrack',visual,(10,h-12,w-20,7));trackid=track.image();bar=Node(doc,'Bar',visual,(10,h-12,w-20,7));barid=bar.image()
    knob=Node(doc,'Knob',visual,(w*.36,h*.36,w*.28,h*.28));kid=knob.image('Knob')
    if circ:
        ic.box=(w*.28,h*.20,w*.44,h*.44) if c==13 else (w*.225,h*.225,w*.55,h*.55)
        title.box=(6,h*.67,w-12,h*.22) if c==13 else (w/2-38,h+3,76,40)
    elif c==0:title.box=(12,5,206,18)
    elif c==3:
        title.box=(9,35,57,18);main.box=(5,4,122,30);secondary.box=(82,35,46,18);ic.box=(71.5,42.5,15,15);note.box=(-65,61,262,48)
    elif c==6:
        ic.box=(24,34,34,34);title.box=(47,4,93,16);main.box=(47,19,93,26);secondary.box=(47,46,93,16)
    elif c in (1,2):ic.box=(7,3.5,18,18);title.box=(31,3,w-35,19)
    elif c in (4,5):ic.box=(9,h/2-11,22,22);title.box=(37,0,w-41,h)
    elif c==14:ic.box=(10,11,21,21);title.box=(37,3,w-41,h-10)
    if c==0:track.box=bar.box=(12,24,206,10)
    inner.active=knob.active=c==11;ic.active=c not in (0,11,15);note.active=c==3;track.active=bar.active=c in (0,3,14)
    if c in (3,6):ic.pivot=(.5,.5)
    if c==6:ic.rotation=20
    expected=['VIDA   86 / 120','Bem alimentado','Esporos 24%','Extração','Mapa','Menu','PISTOLA','Curar\n×3','Trocar','Recarregar','Mochila','Mover','','ATIRAR','Abrir baú · 2 s','N']
    for i,(t,component,data) in enumerate(title.components):
        if t==114 and 'm_fontSize:' in data:
            fs=12 if c in (1,2,3,6) else 16 if c==13 else 13 if circ or c==14 else 14
            data=re.sub(r'  m_text: .*\n',lambda m:'  m_text: '+json.dumps(expected[c],ensure_ascii=False)+'\n',data)
            data=re.sub(r'  m_fontSize(?:Base)?: \d+',lambda m:m.group(0).split(':')[0]+': '+str(fs),data)
            title.components[i]=(t,component,data)
    for target,value in [(main,'03:42' if c==3 else '12 / 12' if c==6 else ''),(secondary,'280 u' if c==3 else 'Reserva 12' if c==6 else '')]:
        for i,(t,component,data) in enumerate(target.components):
            if t==114 and 'm_text:' in data:target.components[i]=(t,component,re.sub(r'  m_text: .*\n','  m_text: '+json.dumps(value)+'\n',data))
    els.append(f'  - Control: {c}\n'+''.join('    '+name+': '+ref(i)+'\n' for name,i in [('Root',n.rect),('Visual',visual.rect),('Background',bgid),('InnerRing',innerid),('Icon',iid),('Bar',barid),('BarTrack',trackid),('Knob',kid),('Hit',hit),('Selection',selection),('Title',tid),('Main',mid),('Secondary',sid),('Note',nid),('Button',b)]))
    if c==15:mapnode=visual
viewport=Node(doc,'MapViewport',mapnode,(3,26,172,101));viewport.mono(pkg('RectMask2D'),'  m_Padding: {x: 0, y: 0, z: 0, w: 0}\n  m_Softness: {x: 0, y: 0}\n')
atlas=Node(doc,'Atlas',viewport,(0,0,172,101));atlasid=atlas.raw();player=Node(doc,'Player',viewport,(76,40,20,20));playerid=player.image('Arrow')
player.pivot=(.5,.5)
exits=[]
for i in range(10):
    n=Node(doc,f'Exit{i}',viewport,(0,0,16,20));graphic=n.image('Exit',True);n.button(graphic);label(doc,n,'Knowledge','?',12,(15,0,13,18));exits.append(n.rect)
sectors=[label(doc,viewport,'Sector'+str(i),'',12,(0,0,110,20))[1] for i in range(6)]
vid=view(doc,root,'  Elements:\n'+''.join(els)+f'  Panel: {ref(root.rect)}\n  Viewport: {ref(viewport.rect)}\n  MapImage: {ref(atlasid)}\n  PlayerMarker: {ref(playerid)}\n'+seq('ExitMarkers',exits)+seq('SectorLabels',sectors))
definitions['hud']=(doc.save(),vid)
for name in ['Menus','Organizer','Results','Inventory','Expedition','Cabin','Activities']:
    d=Doc(name);r=Node(d,name,box=(0,0,470,460));r.image('PanelSomber',True,True)
    h,hi=label(d,r,'Heading',name.upper(),25,(16,16,438,40),True);body,bi=label(d,r,'Body','',17,(20,65,430,65));detail,di=label(d,r,'Detail','',12,(20,400,430,44))
    bs=[];grids=[];slots=[];extra=''
    if name=='Menus':
        bs=[button(d,r,'Option'+str(i),'Option', (20,110+i*47,430,40))[1] for i in range(8)]
    elif name=='Organizer':
        r.box=(0,0,350,212)
        for title,box in [('Previous',(12,55,35,30)),('Next',(303,55,35,30)),('Restore',(12,131,326,27)),('Save',(12,168,104,30)),('Cancel',(122,168,104,30)),('Defaults',(232,168,106,30)),('Toggle',(85,-41,180,30))]: bs.append(button(d,r,title,{'Previous':'‹','Next':'›','Restore':'Restaurar este elemento','Save':'Salvar','Cancel':'Cancelar','Defaults':'Padrão','Toggle':'Mostrar / ocultar ajustes'}[title],box)[1])
        track=Node(d,'SizeSlider',r,(110,97,218,20));graphic=track.image(ray=True);handle=Node(d,'Handle',track,(0,0,16,20));hg=handle.image('PanelSomber');sl=track.mono(pkg('Slider'),f'  m_Interactable: 1\n  m_TargetGraphic: {ref(hg)}\n  m_FillRect: {{fileID: 0}}\n  m_HandleRect: {ref(handle.rect)}\n  m_Direction: 0\n  m_MinValue: 0.6\n  m_MaxValue: 2\n  m_WholeNumbers: 0\n  m_Value: 1\n  m_OnValueChanged:\n    m_PersistentCalls:\n      m_Calls: []\n');extra+=f'  Slider: {ref(sl)}\n'
    elif name=='Results':
        r.box=(0,0,460,165);body.box=(16,18,428,85);bs=[button(d,r,'Return','Voltar ao acampamento',(30,113,400,40))[1]]
    elif name=='Inventory':
        r.box=(0,0,938,432);h.box=(0,8,469,40);body.box=(469,8,469,40);detail.box=(10,380,918,44)
        bs=[button(d,r,'Close','Fechar',(833,8,90,35))[1]]
        for i in range(2):g,s=grid(d,r,'Grid'+str(i),9);grids.append(g.rect);slots+=s
        ghost=Node(d,'DragVisual',r,(0,0,80,60));ghost.image('PanelSomber',frame=True);gi=Node(d,'Item',ghost,(4,3,50,50));gii=gi.image('Chest');gc,gci=label(d,ghost,'Count','',13,(56,15,24,25));extra+=f'  DragVisual: {ref(ghost.rect)}\n  DragIcon: {ref(gii)}\n  DragCaption: {ref(gci)}\n'
    elif name=='Expedition':
        r.box=(0,0,960,460);portrait=Node(d,'ForestPortrait',r,(24,24,382,340));pi=portrait.raw();extra+=f'  Portrait: {ref(pi)}\n';bs=[button(d,r,'Action'+str(i),'Action',(430,215+i*47,506,40))[1] for i in range(5)]
        scroll=Node(d,'MessageScroll',r,(430,85,506,112));vp=Node(d,'Viewport',scroll,(0,0,506,112));vp.image('PanelSomber',True,preserve=False);vp.mono(pkg('Mask'),'  m_ShowMaskGraphic: 0\n')
        content=Node(d,'Content',vp,(0,0,506,112))
        content.mono(pkg('VerticalLayoutGroup'),'  m_Padding: {m_Left: 0, m_Right: 0, m_Top: 0, m_Bottom: 0}\n  m_ChildAlignment: 0\n  m_Spacing: 0\n  m_ChildForceExpandWidth: 1\n  m_ChildForceExpandHeight: 0\n  m_ChildControlWidth: 1\n  m_ChildControlHeight: 1\n')
        content.mono(pkg('ContentSizeFitter'),'  m_HorizontalFit: 0\n  m_VerticalFit: 2\n')
        r.children.remove(body);body.parent=content;content.children.append(body);body.box=(0,0,506,112)
        scroll.mono(pkg('ScrollRect'),f'  m_Content: {ref(content.rect)}\n  m_Horizontal: 0\n  m_Vertical: 1\n  m_MovementType: 2\n  m_Elasticity: 0.1\n  m_Inertia: 1\n  m_DecelerationRate: 0.135\n  m_ScrollSensitivity: 20\n  m_Viewport: {ref(vp.rect)}\n  m_HorizontalScrollbar: {{fileID: 0}}\n  m_VerticalScrollbar: {{fileID: 0}}\n  m_OnValueChanged:\n    m_PersistentCalls:\n      m_Calls: []\n')
        extra+=f'  Viewport: {ref(vp.rect)}\n  Content: {ref(content.rect)}\n'
    elif name=='Cabin':
        # A HUD container is transparent; only its individual controls draw frames.
        r.components=[(t,i,data.replace('  m_Enabled: 1\n','  m_Enabled: 0\n').replace('  m_RaycastTarget: 1\n','  m_RaycastTarget: 0\n') if t==114 and 'm_RaycastTarget:' in data else data) for t,i,data in r.components]
        r.box=(0,0,1066,600);h.box=(373,16,320,38);body.box=(14,14,260,78);detail.box=(20,445,1026,40)
        bs=[button(d,r,'Interact','Interagir',(358,526,350,50))[1],button(d,r,'Backpack','Mochila',(904,530,148,50))[1]]
    else:
        r.box=(0,0,810,390);h.box=(10,8,790,40);detail.box=(10,290,790,40)
        labels=['Munição de pistola','Munição de rifle','Cartuchos','Melhorar reciclador','Adquirir componente','Voltar']
        boxes=[(15+i*260,75,250,60) for i in range(3)]+[(15,155,780,55),(15,225,780,50),(20,335,770,45)]
        bs=[button(d,r,'Action'+str(i),t,box)[1] for i,(t,box) in enumerate(zip(labels,boxes))];g,s=grid(d,r,'FishGrid',9);grids=[g.rect];slots=s
    fields=f'  Elements: []\n  Panel: {ref(r.rect)}\n  Heading: {ref(hi)}\n  Body: {ref(bi)}\n  Detail: {ref(di)}\n'+seq('Buttons',bs)+seq('Grids',grids)+seq('Slots',slots)+extra
    vi=view(d,r,fields);definitions[name.lower()]=(d.save(),vi)
# Reusable inventory cell prefab, also editable independently.
d=Doc('ItemSlot');r,si=slot(d,None,'ItemSlot');d.save()
d=Doc('PlayerInterface');r=Node(d,'PlayerInterface',box=(0,0,1066,600))
r.components.append((223,d.id(),'  m_Enabled: 1\n  serializedVersion: 3\n  m_RenderMode: 0\n  m_Camera: {fileID: 0}\n  m_PlaneDistance: 100\n  m_PixelPerfect: 0\n  m_ReceivesEvents: 1\n  m_OverrideSorting: 0\n  m_OverridePixelPerfect: 0\n  m_SortingBucketNormalizedSize: 0\n  m_VertexColorAlwaysGammaSpace: 0\n  m_AdditionalShaderChannelsFlag: 25\n  m_SortingLayerID: 0\n  m_SortingOrder: 100\n  m_TargetDisplay: 0\n'))
canvas=r.components[-1][1];scaler=r.mono(pkg('CanvasScaler'),'  m_UiScaleMode: 0\n  m_ReferencePixelsPerUnit: 100\n  m_ScaleFactor: 1\n  m_ReferenceResolution: {x: 1066, y: 600}\n  m_ScreenMatchMode: 0\n  m_MatchWidthOrHeight: 1\n  m_PhysicalUnit: 3\n  m_FallbackScreenDPI: 96\n  m_DefaultSpriteDPI: 96\n  m_DynamicPixelsPerUnit: 1\n')
r.mono(pkg('GraphicRaycaster'),'  m_IgnoreReversedGraphics: 1\n  m_BlockingObjects: 0\n  m_BlockingMask:\n    serializedVersion: 2\n    m_Bits: 4294967295\n')
safe=Node(d,'SafeArea',r,(0,0,1066,600));fields=f'  safeArea: {ref(safe.rect)}\n  canvas: {ref(canvas)}\n  scaler: {ref(scaler)}\n'
for k,(path,vi) in definitions.items():
    field={'menus':'menus','organizer':'organizer','results':'results','activities':'activities'}.get(k,k)
    fields+=f'  {field}: {{fileID: {vi}, guid: {metadata(path)}, type: 3}}\n'
r.mono(script('PlayerInterfaceRoot'),fields)
rootpath=d.save()
# Runtime loads the canonical root from UI/Prefabs/PlayerInterface.
print('Authored 10 screen/control prefabs, root prefab and '+str(len(sprites))+' reusable sprites.')
