"""Author V2 imports and coverage, adding explicitly recorded production batches.

Run with the bundled Python. Uses preserved generated images; no Unity or builds.
Existing sprites are retained; new references only fill empty catalog entries.
"""
from pathlib import Path
import csv
import json
import re
import shutil
import uuid

ROOT = Path(__file__).resolve().parents[2]
GENERATED = Path('C:/Users/Vinni/.codex/generated_images/01a14ec1-3f32-7202-83b6-27ff99fb7d09')
PILOTS = {
    'Bandage': 'exec-b6dc1119-23fc-4396-8cbb-76b86e176795.png',
    'Pistol': 'exec-4adb75a1-1fb3-4a13-9c54-450d0d35c671.png',
    'GuardianHelm': 'exec-5c8d37ed-2767-4959-b02f-d59ee1641a33.png',
}
SOURCES = dict(PILOTS)
for manifest_path in sorted((ROOT / 'Art/ItemIconsV2').glob('lote-*.json')):
    manifest = json.loads(manifest_path.read_text(encoding='utf-8-sig'))
    for item, entry in manifest['Items'].items():
        if item in SOURCES:
            raise ValueError('Fonte duplicada de item: '+item)
        SOURCES[item] = entry['Source']

def write_new(path, content):
    if not path.exists():
        path.write_text(content, encoding='utf-8')

def guid(path):
    return re.search(r'^guid: (\w+)', path.read_text(encoding='utf-8-sig'), re.M)[1]

def folder(path):
    path.mkdir(parents=True, exist_ok=True)
    if path.is_relative_to(ROOT / 'Assets'):
        write_new(Path(str(path)+'.meta'), 'fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n')

resources = ROOT / 'Assets/Resources/Items/IconsV2'
originals = ROOT / 'Art/ItemIconsV2/Originals'
reports = ROOT / 'Docs/UI/ItemIconsV2'
for directory in (resources, originals, reports):
    folder(directory)

script_meta = ROOT / 'Assets/Scripts/Items/ItemVisualCatalog.cs.meta'
write_new(script_meta, 'fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n')
template = (ROOT / 'Assets/Resources/UI/Art/Aim.png.meta').read_text(encoding='utf-8-sig')
template = template.replace('wrapU: 0', 'wrapU: 1').replace('wrapV: 0', 'wrapV: 1').replace('wrapW: 0', 'wrapW: 1')
template = template.replace('spriteMeshType: 0', 'spriteMeshType: 1').replace('spriteGenerateFallbackPhysicsShape: 1', 'spriteGenerateFallbackPhysicsShape: 0')
# All platform sizes are 512 and compression is explicitly disabled.
template = template.replace('overridden: 0', 'overridden: 1')
for item, filename in SOURCES.items():
    source = GENERATED / filename
    if not source.exists():
        raise FileNotFoundError(source)
    for destination in (originals / (item+'.png'), resources / (item+'.png')):
        if not destination.exists():
            shutil.copyfile(source, destination)
    meta = re.sub(r'^guid: \w+', 'guid: '+uuid.uuid4().hex, template, flags=re.M)
    meta = re.sub(r'spriteID: \w+', 'spriteID: '+uuid.uuid4().hex, meta)
    write_new(resources / (item+'.png.meta'), meta)

catalog_source = (ROOT / 'Assets/Scripts/Items/ItemCatalog.cs').read_text(encoding='utf-8-sig')
items = [s.strip() for s in re.search(r'enum ItemId : byte\s*\{([^}]+)', catalog_source)[1].split(',')]
assert 59 <= len(items) < 255
asset = ROOT / 'Assets/Resources/ItemVisualCatalog.asset'
head = '''%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: SCRIPT, type: 3}
  m_Name: ItemVisualCatalog
  m_EditorClassIdentifier:
  Version: 1
  Entries:
'''.replace('SCRIPT', guid(script_meta))
for index, item in enumerate(items):
    icon = '{fileID: 21300000, guid: '+guid(resources / (item+'.png.meta'))+', type: 3}' if item in SOURCES else '{fileID: 0}'
    head += f'  - Id: {index}\n    InterfaceIcon: {icon}\n    WorldIcon: {{fileID: 0}}\n'
write_new(asset, head)
current = asset.read_text(encoding='utf-8-sig')
updated = current
for item in SOURCES:
    index = items.index(item)
    expected = '{fileID: 21300000, guid: '+guid(resources / (item+'.png.meta'))+', type: 3}'
    pattern = r'(^  - Id: '+str(index)+r'\n    InterfaceIcon: )(\{[^\n]+\})'
    match = re.search(pattern, updated, re.M)
    if match is None:
        raise ValueError('Entrada visual ausente: '+item)
    if match[2] not in ('{fileID: 0}', expected):
        raise ValueError('Referência visual já editada; preservada: '+item)
    updated = re.sub(pattern, lambda m: m[1]+expected, updated, flags=re.M)
if updated != current:
    asset.write_text(updated, encoding='utf-8')
integrated = set(map(int, re.findall(r'^  - Id: (\d+)\n    InterfaceIcon: \{fileID: 21300000', updated, re.M)))
write_new(Path(str(asset)+'.meta'), 'fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n')

# Report the actual old resource/definition or procedural fallback for every ID.
paths_by_guid = {}
for meta in (ROOT / 'Assets').rglob('*.meta'):
    match = re.search(r'^guid: (\w+)', meta.read_text(encoding='utf-8-sig'), re.M)
    if match:
        paths_by_guid[match[1]] = str(meta.relative_to(ROOT))[:-5].replace('\\', '/')
old = (ROOT / 'Assets/Resources/ItemIcons.asset').read_text(encoding='utf-8-sig')
old_refs = re.findall(r'guid: (\w+)', old.split('  icons:')[1])
names = re.findall(r'"([^"]*)"', re.search(r'string\[\] names = \{([^}]+)', catalog_source)[1])
melee_names = {'ShortSword': 'Espada curta', 'BattleAxe': 'Machado', 'Spear': 'Lança',
               'ImprovisedHandCrossbow':'Besta de mão improvisada', 'RustySword':'Espada enferrujada', 'CrossbowBolt':'Virotes'}
definition_names = {}
for definition in (ROOT / 'Assets/Resources/Items').rglob('*.asset'):
    content = definition.read_text(encoding='utf-8-sig')
    id_match = re.search(r'^  Id: (\d+)', content, re.M)
    display = re.search(r'^  DisplayName: (.+)', content, re.M)
    if id_match and display:
        text = display[1]
        definition_names[int(id_match[1])] = json.loads(text) if text.startswith('"') else text

with (reports / 'catalogo.csv').open('w', encoding='utf-8-sig', newline='') as stream:
    writer = csv.writer(stream)
    writer.writerow(['Id', 'Item', 'Nome', 'Lote', 'OrigemInterfaceAnterior', 'OrigemMundoPreservada', 'RecursoV2', 'Estado'])
    for index, item in enumerate(items):
        batch = 'I' if index>=59 else 'A' if index < 18 and index not in (6,7,14,15,16,17) else 'B' if index in (6,7,14,15,16,17,32,33,34) else 'C' if 18 <= index <= 25 else 'F' if 26 <= index <= 31 else 'D' if 35 <= index <= 52 else 'E'
        if index < 18:
            legacy = paths_by_guid[old_refs[index]]
        elif item == 'Lockpick':
            legacy = 'Assets/Resources/Items/Lockpick.png'
        elif 19 <= index <= 23:
            legacy = 'PassageToolArt.Get (raster procedural)'
        elif index in (24,25):
            legacy = 'DungeonToolArt.GetIcon / ExtractionsArt manifest'
        elif 26 <= index <= 31:
            legacy = 'SpriteFactory.Square (treinamento)'
        elif 32 <= index <= 34:
            legacy = 'MeleeWeaponArt.Get (128x128 procedural)'
        elif 35 <= index <= 52:
            legacy = 'EquipmentCatalog.Icon ou EquipmentArt.Get (128x128 procedural)'
        elif index>=59:
            legacy = 'ItemVisualCatalog.World explícito (novo item)'
        else:
            legacy = 'TreasureCatalog.Icon ou TreasureArt.Get (procedural)'
        world = 'DungeonToolArt.GetWorld / ExtractionsArt manifest' if index in (24,25) else legacy
        writer.writerow([index, item, names[index] if index < len(names) else definition_names.get(index,melee_names.get(item,item)), batch, legacy, world, 'Items/IconsV2/'+item, 'Integrado; avaliação prática pendente' if index in integrated else 'Produção pendente'])
print(f'V2: {len(items)} entradas, {len(integrated)} ícones integrados, {len(items)-len(integrated)} pendentes; artes anteriores preservadas.')
