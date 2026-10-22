"""Author stage-8 loot tables and treasures; preserve existing files and GUIDs."""
from pathlib import Path
import uuid

ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / 'Assets'


def meta(path, folder=False, script=False):
    target = Path(str(path) + '.meta')
    if target.exists():
        return
    importer = ('folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n' if folder else
                'MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n' if script else
                'NativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n')
    target.write_text('fileFormatVersion: 2\nguid: ' + uuid.uuid4().hex + '\n' + importer, encoding='utf-8')


def folder(path):
    path.mkdir(parents=True, exist_ok=True)
    if path.parent != ASSETS and not Path(str(path.parent) + '.meta').exists():
        folder(path.parent)
    meta(path, folder=True)


def guid(path):
    return next(line.split(': ')[1] for line in Path(str(path) + '.meta').read_text().splitlines() if line.startswith('guid:'))


def asset(path, script, name, body):
    if not path.exists():
        path.write_text(f'''%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {guid(script)}, type: 3}}
  m_Name: {name}
  m_EditorClassIdentifier:
{body}''', encoding='utf-8')
    meta(path)


def entries(map_id, source):
    result = []

    def add(item, category, weight, minimum=1, maximum=1, themes=15):
        result.append((item, category, weight, minimum, maximum, themes))

    # Preserve the old base quantity intervals, without a container-rarity multiplier.
    for item, weight, lo, hi in [(0, 20, 3, 7), (1, 20, 3, 8), (3, 20, 2, 5), (4, 12, 1, 2), (5, 12, 1, 3)]:
        themes = (5 if item == 0 else 3 if item in (1, 3) else 9) if source == 4 else 15
        add(item, 0, weight, lo, hi, themes)
    for item, weight, lo, hi in [(2, 20, 1, 2), (7, 50, 6, 12), (15, 30, 3, 5), (17, 20, 2, 4)]:
        add(item, 1, weight, lo, hi)
    if source != 4:
        add(18, 1, 8)
        if map_id == 1:
            for item in (19, 20, 21):
                add(item, 1, 3)
    # Ground and monsters cannot produce weapons/equipment; supply containers have their own pool.
    if source in (2, 3):
        for item in range(35, 53):
            add(item, 2, 1)
        weights = [30, 15, 15, 18, 10, 12] if map_id == 1 else [15, 10, 10, 30, 15, 20]
        for item, weight in zip((6, 14, 16, 32, 33, 34), weights):
            add(item, 3, weight)
    if source != 1:
        for item, weight in zip(range(53, 56) if map_id == 2 else range(56, 59), (75, 20, 5)):
            add(item, 4, weight)
    return result


def main():
    for name in ('LootTableDefinition', 'LootTables', 'LootRoller', 'TreasureCatalog', 'TreasureArt'):
        meta(ASSETS / 'Scripts/Items' / (name + '.cs'), script=True)
    dest = ASSETS / 'Resources/Items/Treasure/Definitions'
    folder(dest)
    treasures = [('AncientCoin', 'Moeda antiga', 10), ('Reliquary', 'Relicário', 30), ('FuneraryIdol', 'Ídolo funerário', 80),
                 ('ValuableScrap', 'Sucata valiosa', 10), ('AntiqueObject', 'Objeto antigo', 30), ('CollectorsDocument', 'Documento de coleção', 80)]
    for i, (name, label, price) in enumerate(treasures):
        asset(dest / (name + '.asset'), ASSETS / 'Scripts/Items/ItemDefinition.cs', name,
              f'  Id: {53+i}\n  DisplayName: "{label}"\n  Category: 4\n  Slot: 0\n  PrimaryAttribute: 0\n  SecondaryPool: []\n  Icon: {{fileID: 0}}\n  SellPrice: {price}\n')
    dest = ASSETS / 'Resources/Items/Loot/Tables'
    folder(dest)
    # presence, min/max stacks, guarantee equipment, materials/supplies/equipment/weapons/treasure weights
    sources = [('Ground', 70, 1, 1, 0, (55, 35, 0, 0, 10)),
               ('Supplies', 100, 2, 2, 0, (30, 70, 0, 0, 0)),
               ('CommonContainer', 90, 2, 3, 0, (30, 30, 20, 5, 15)),
               ('ValuableContainer', 100, 3, 4, 1, (20, 30, 0, 10, 40)),
               ('Monster', 60, 1, 1, 0, (60, 25, 0, 0, 15))]
    for map_id, map_name in ((1, 'Forest'), (2, 'Dungeon')):
        for source, (source_name, presence, lo, hi, guarantee, weights) in enumerate(sources):
            body = f'  Map: {map_id}\n  Source: {source}\n  PresencePercent: {presence}\n  MinimumStacks: {lo}\n  MaximumStacks: {hi}\n  GuaranteeEquipment: {guarantee}\n  Categories:\n'
            for category, weight in enumerate(weights):
                body += f'  - Category: {category}\n    Weight: {weight}\n'
            body += '  Entries:\n'
            for item, category, weight, minimum, maximum, themes in entries(map_id, source):
                body += f'  - Item: {item}\n    Category: {category}\n    Weight: {weight}\n    Minimum: {minimum}\n    Maximum: {maximum}\n    Themes: {themes}\n'
            body += '  Rarities:\n'
            for common, uncommon, rare in ((80, 19, 1), (60, 35, 5), (35, 50, 15)):
                body += f'  - Common: {common}\n    Uncommon: {uncommon}\n    Rare: {rare}\n'
            name = map_name + '_' + source_name
            asset(dest / (name + '.asset'), ASSETS / 'Scripts/Items/LootTableDefinition.cs', name, body)
    print('10 loot tables and 6 treasure definitions authored; existing files preserved.')


if __name__ == '__main__':
    main()
