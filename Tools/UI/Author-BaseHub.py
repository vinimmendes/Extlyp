"""Author only the preparation hub and ground-items view. Never rebuild the scene.

Uses the existing YAML author's definitions without executing its asset pipeline.
Refuses to overwrite either new prefab; later edits belong in the authored assets.
"""
from pathlib import Path
import ast, hashlib, json, math, re, uuid

ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / 'Assets/Resources/UI'
SCRIPTS = ROOT / 'Assets/Scripts/UI'
PACKAGE = next((ROOT / 'Library/PackageCache').glob('com.unity.ugui@*'))
refresh = repair = False
manifest = {}
BASE = '  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n'
source = (ROOT / 'Tools/UI/Author-PlayerInterface.py').read_text(encoding='utf-8-sig')
names = {'guid', 'metadata', 'script', 'pkg', 'Node', 'Doc', 'ref', 'seq', 'label', 'button'}
definitions = [n for n in ast.parse(source).body if isinstance(n, (ast.FunctionDef, ast.ClassDef)) and n.name in names]
exec(compile(ast.Module(body=definitions, type_ignores=[]), '<existing UI author definitions>', 'exec'))
generated_guid = guid

def guid(path):
    meta = ROOT / (str(path) + '.meta')
    return re.search(r'guid: (\w+)', meta.read_text())[1] if meta.exists() else generated_guid(path)

sprites = {name: re.search(r'guid: (\w+)', (ASSETS / 'Art' / (name + '.png.meta')).read_text())[1]
           for name in ['PanelSomber', 'PanelSimple']}
for name in ['BaseHubView', 'UguiBaseHub', 'GroundItemsView', 'UguiInventoryPresenter.Ground', 'ScrollSelectedIntoView']:
    metadata(SCRIPTS / (name + '.cs'))

def stretch(node):
    node.anchors = ((0, 0), (1, 1)); node.pivot = (.5, .5); node.box = (0, 0, 0, 0)

def frame(node):
    component = node.image('PanelSomber', ray=True, frame=True)
    node.components = [(t, i, data + ('  MenuStyle: 1\n  FillOpacity: 1\n' if i == component else '')) for t, i, data in node.components]

def text(doc, parent, name, caption, size, box):
    node, component = label(doc, parent, name, caption, size, box)
    node.components = [(t, i, data.replace('m_HorizontalAlignment: 2', 'm_HorizontalAlignment: 1').replace('m_VerticalAlignment: 512', 'm_VerticalAlignment: 256')) for t, i, data in node.components]
    return component

doc = Doc('BaseHub')
root = Node(doc, 'BaseHub', box=(0, 0, 1066, 600)); frame(root)
heading = text(doc, root, 'Heading', 'BASE', 28, (16, 12, 170, 40))
resources = text(doc, root, 'Resources', 'Recursos', 18, (196, 12, 660, 40))
_, options = button(doc, root, 'Options', 'Opções', (922, 8, 128, 48))
tabs = [button(doc, root, 'Tab' + str(i), name, (16 + i * 208, 60, 200, 48))[1]
        for i, name in enumerate(['Preparação', 'Inventário', 'Personagem', 'Comerciante', 'Atividades'])]
preparation = Node(doc, 'Preparation', root, (16, 116, 1034, 432))
# CanvasGroup is a built-in component, not a script GUID.
fade = doc.id(); preparation.components.append((225, fade, '  m_Enabled: 1\n  m_Alpha: 1\n  m_Interactable: 1\n  m_BlocksRaycasts: 1\n  m_IgnoreParentGroups: 0\n'))
equipment = Node(doc, 'Equipment', preparation, (0, 0, 290, 432)); frame(equipment)
text(doc, equipment, 'Title', 'EQUIPAMENTO', 23, (16, 16, 258, 32))
equipment_text = text(doc, equipment, 'EquipmentText', 'Equipamento', 18, (72, 58, 204, 288))
icons = [Node(doc, 'EquipmentIcon' + str(i), equipment, (18, 64 + i * 34, 42, 30)).image(preserve=True) for i in range(8)]
_, organize = button(doc, equipment, 'Organize', 'Organizar equipamento', (16, 366, 258, 50))
scenery = Node(doc, 'Scenery', preparation, (302, 0, 410, 432))
art = Node(doc, 'Forest', scenery); stretch(art); artwork = art.raw()
destination = Node(doc, 'Destination', preparation, (724, 0, 310, 432)); frame(destination)
map_title = text(doc, destination, 'MapTitle', 'FLORESTA VELHA', 25, (16, 16, 278, 54))
destination_text = text(doc, destination, 'DestinationText', 'EXPEDIÇÃO', 19, (16, 76, 278, 160))
buttons = {name: button(doc, destination, name, caption, (16, y, 278, 50))[1] for name, caption, y in [
    ('Map', 'Destino', 244), ('Bots', 'Bots', 300), ('Leave', 'Voltar', 300), ('Depart', 'PARTIR', 366),
    ('Cancel', 'Cancelar busca', 366), ('Retry', 'Tentar novamente', 366)]}
activities = Node(doc, 'Activities', root, (16, 116, 1034, 432)); frame(activities)
text(doc, activities, 'Title', 'ATIVIDADES DA BASE', 25, (24, 24, 900, 40))
actions = [button(doc, activities, 'Activity' + str(i), name, (24 + i * 328, 115, 320, 150))[1]
           for i, name in enumerate(['BANCADA', 'COZINHA', 'PESCA'])]
message = text(doc, root, 'Message', '', 18, (16, 554, 1034, 40))
fields = dict(Panel=root.rect, Preparation=preparation.rect, Activities=activities.rect, Equipment=equipment.rect,
              Destination=destination.rect, Scenery=scenery.rect, Heading=heading, Resources=resources, Message=message,
              EquipmentText=equipment_text, DestinationText=destination_text, MapTitle=map_title, Artwork=artwork,
              Options=options, Organize=organize, PreparationFade=fade, **buttons)
hub_component = root.mono(script('BaseHubView'), ''.join(f'  {key}: {ref(value)}\n' for key, value in fields.items()) + seq('Tabs', tabs) + seq('ActivityButtons', actions) + seq('EquipmentIcons', icons))
# Extend the serializer's component-name table for the single native CanvasGroup.
doc_source = ast.get_source_segment(source, next(n for n in ast.parse(source).body if isinstance(n, ast.ClassDef) and n.name == 'Doc'))
doc_source = doc_source.replace("223:'Canvas'", "223:'Canvas',225:'CanvasGroup'")
namespace = dict(globals()); exec(doc_source, namespace)
doc.__class__ = namespace['Doc']
for node in doc.nodes:
    if node.name in ['Activities', 'Cancel', 'Retry', 'Leave', 'Bots']: node.active = False
    node.components = [(t, i, data.replace('m_Texture: {fileID: 0}', 'm_Texture: {fileID: 2800000, guid: 6b6c68982df34a0eb17ef1333684ab74, type: 3}') if i == artwork else data) for t, i, data in node.components]
hub_path = doc.save()

doc = Doc('GroundItems')
root = Node(doc, 'GroundItems'); stretch(root)
_, toggle = button(doc, root, 'ToggleGround', 'Itens no chão', (838, 124, 196, 48))
panel = Node(doc, 'GroundPanel', root, (16, 116, 1034, 432)); frame(panel)
heading = text(doc, panel, 'Heading', 'ITENS NO CHÃO', 20, (20, 14, 780, 66))
close_node, close = button(doc, panel, 'Close', 'Voltar', (-16, 16, 140, 48))
close_node.anchors = ((1, 1), (1, 1)); close_node.pivot = (1, 1)
scroll = Node(doc, 'Scroll', panel, (16, 90, -32, -106)); scroll.anchors = ((0, 0), (1, 1))
viewport = Node(doc, 'Viewport', scroll); stretch(viewport)
viewport.image(ray=True)
viewport.components = [(t, i, data.replace('m_CullTransparentMesh: 1', 'm_CullTransparentMesh: 0')) for t, i, data in viewport.components]
viewport.mono(pkg('RectMask2D'), '  m_Padding: {x: 0, y: 0, z: 0, w: 0}\n  m_Softness: {x: 0, y: 0}\n')
content = Node(doc, 'Content', viewport, (0, 0, 0, 0)); content.anchors = ((0, 1), (1, 1))
content.mono(pkg('VerticalLayoutGroup'), '''  m_Padding:
    m_Left: 0
    m_Right: 0
    m_Top: 0
    m_Bottom: 0
  m_ChildAlignment: 0
  m_Spacing: 8
  m_ChildForceExpandWidth: 1
  m_ChildForceExpandHeight: 0
  m_ChildControlWidth: 1
  m_ChildControlHeight: 1
  m_ChildScaleWidth: 0
  m_ChildScaleHeight: 0
  m_ReverseArrangement: 0
''')
content.mono(pkg('ContentSizeFitter'), '  m_HorizontalFit: 0\n  m_VerticalFit: 2\n')
row_node, row = button(doc, content, 'RecoverItem', 'Recuperar', (0, 0, 1000, 54)); row_node.active = False
row_node.mono(pkg('LayoutElement'), '  m_IgnoreLayout: 0\n  m_MinWidth: -1\n  m_MinHeight: 54\n  m_PreferredWidth: -1\n  m_PreferredHeight: 54\n  m_FlexibleWidth: 1\n  m_FlexibleHeight: 0\n  m_LayoutPriority: 1\n')
row_node.mono(script('ScrollSelectedIntoView'))
scroll_component = scroll.mono(pkg('ScrollRect'), f'''  m_Content: {ref(content.rect)}
  m_Horizontal: 0
  m_Vertical: 1
  m_MovementType: 2
  m_Elasticity: 0.1
  m_Inertia: 1
  m_DecelerationRate: 0.135
  m_ScrollSensitivity: 30
  m_Viewport: {ref(viewport.rect)}
  m_HorizontalScrollbar: {{fileID: 0}}
  m_VerticalScrollbar: {{fileID: 0}}
  m_OnValueChanged:
    m_PersistentCalls:
      m_Calls: []
''')
fields = dict(Panel=panel.rect, Content=content.rect, Heading=heading, Toggle=toggle, Close=close, Row=row, Scroll=scroll_component)
ground_component = root.mono(script('GroundItemsView'), ''.join(f'  {key}: {ref(value)}\n' for key, value in fields.items()))
ground_path = doc.save()
path = ASSETS / 'Prefabs/PlayerInterface.prefab'
current = path.read_text(encoding='utf-8-sig')
assert '  baseHub:' not in current and '  groundItems:' not in current
current = current.replace('  activities:', f'  baseHub: {{fileID: {hub_component}, guid: {metadata(hub_path)}, type: 3}}\n  groundItems: {{fileID: {ground_component}, guid: {metadata(ground_path)}, type: 3}}\n  activities:', 1)
path.write_text(current, encoding='utf-8')
print('Authored BaseHub and GroundItems; added two references to PlayerInterface.')
