"""Create stable Unity metadata and the isolated authoring-scene entry point.
Never opens Unity, invokes Play, runs tests or changes a gameplay scene.
"""
from pathlib import Path
import re
import uuid

ROOT=Path(__file__).resolve().parents[1]
names=['DungeonMapDocument','DungeonMapPublication','DungeonArtCatalog','DungeonMapAuthoring',
       'DungeonAuthoringElement','DungeonTerrainSource','DungeonMapArt','DungeonArtLifetime','DungeonLegacySource']
editors=['DungeonArtImporter','DungeonAuthoringScene','DungeonAuthoringCompiler','DungeonAuthoringWindow','DungeonLegacyMigration','DungeonAuthoringProduction','DungeonKitReference','DungeonAuthoringLifecycle','DungeonCatalogEditing','DungeonLinkedElementEditor','DungeonArtPublication']
def meta(path,body):
    dest=Path(str(path)+'.meta')
    if not dest.exists(): dest.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n'+body,encoding='utf-8')
    return re.search(r'guid: ([a-f0-9]+)',dest.read_text(encoding='utf-8'))[1]
mono='''MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData:
  assetBundleName:
  assetBundleVariant:
'''
guids={}
for folder,items in [('Assets/Scripts/Match',names),('Assets/Editor',editors)]:
    for name in items: guids[name]=meta(ROOT/folder/(name+'.cs'),mono)
folder=ROOT/'Assets/Resources/Environment/Dungeon/ReworkComplete'
meta(folder,'folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n')
meta(folder/'CatacombArt.catacombkit','''ScriptedImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 2
  userData:
  assetBundleName:
  assetBundleVariant:
  script: {fileID: 11500000, guid: '''+guids['DungeonArtImporter']+''', type: 3}
''')
for path in folder.glob('*.json'):
    meta(path,'TextScriptImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n')
scene=ROOT/'Assets/Scenes/Catacumbas_Autoria.unity'
if not scene.exists():
    scene.parent.mkdir(parents=True,exist_ok=True)
    scene.write_text('''%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &1000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 1001}
  - component: {fileID: 1002}
  m_Layer: 0
  m_Name: Catacumbas_Autoria
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &1001
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1000}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!114 &1002
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1000}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: '''+guids['DungeonMapAuthoring']+''', type: 3}
  m_Name:
  m_EditorClassIdentifier:
  Document:
    version: 1
    width: 128
    height: 128
    cellSize: 1.06
    cells: []
    sectors: []
    rooms: []
    regions: []
    points: []
    visuals: []
    surfaces: []
    dressedRooms: []
    fidelity: []
  Catalog: {fileID: 0}
  PublishedPath: Assets/Resources/Environment/Dungeon/ReworkComplete/Published.json
  MigrationReport: Abrir pelo menu de autoria para materializar a cena fora do Play.
  MigrationReconciled: 0
''',encoding='utf-8')
meta(scene,'DefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n')
print('Metadata and isolated authoring scene prepared; no Unity invocation.')
