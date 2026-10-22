"""Author the stage-7 ScriptableObjects without opening or rebuilding Unity.

Existing definitions and GUIDs are preserved on subsequent invocations.
"""
from pathlib import Path
import struct
import uuid

ROOT = Path(__file__).resolve().parents[2]
MODELS = [
    ('WatchmanHood', 'Capuz do Vigia', 3, (1, 2)),
    ('GuardianHelm', 'Elmo do Guardião', 4, (0, 2)),
    ('MarksmanMask', 'Máscara do Atirador', 1, (3, 4)),
    ('ScoutJerkin', 'Gibão do Batedor', 2, (1, 3)),
    ('FighterMail', 'Cota do Combatente', 0, (4, 2)),
    ('GuardianCuirass', 'Couraça do Guardião', 4, (0, 3)),
    ('MarksmanGloves', 'Luvas do Atirador', 1, (3, 2)),
    ('FighterGauntlets', 'Manoplas do Combatente', 0, (2, 4)),
    ('ExplorerGloves', 'Luvas do Explorador', 3, (1, 4)),
    ('WandererBoots', 'Botas do Andarilho', 2, (3, 1)),
    ('DuelistBoots', 'Botas do Duelista', 1, (0, 2)),
    ('GuardianGreaves', 'Grevas do Guardião', 4, (0, 2)),
    ('EyeMedallion', 'Medalhão do Olho', 3, (1, 4)),
    ('BreathTalisman', 'Talismã do Fôlego', 2, (0, 4)),
    ('FighterFang', 'Presa do Combatente', 0, (2, 3)),
    ('SteadyHandRing', 'Anel da Mão Firme', 1, (3, 2)),
    ('StrongPulseRing', 'Anel do Pulso Forte', 0, (4, 1)),
    ('PersistenceRing', 'Anel da Persistência', 4, (2, 3)),
]


def meta(path, folder=False):
    target = Path(str(path) + '.meta')
    if target.exists():
        return
    target.write_text('fileFormatVersion: 2\nguid: ' + uuid.uuid4().hex + '\n' +
                      ('folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
                       if folder else 'NativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'), encoding='utf-8')


def main():
    parent = ROOT / 'Assets/Resources/Items/Equipment'
    dest = parent / 'Definitions'
    dest.mkdir(parents=True, exist_ok=True)
    meta(parent, True)
    meta(dest, True)
    for i, (name, label, primary, secondary) in enumerate(MODELS):
        path = dest / (name + '.asset')
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
  m_Script: {{fileID: 11500000, guid: c0da08762aaa49a98c2fd2e36429ab13, type: 3}}
  m_Name: {name}
  m_EditorClassIdentifier: 
  Id: {35+i}
  DisplayName: "{label}"
  Category: 3
  Slot: {i//3+1}
  PrimaryAttribute: {primary}
  SecondaryPool: {struct.pack('<BB', *secondary).hex()}
  Icon: {{fileID: 0}}
  SellPrice: 0
''', encoding='utf-8')
        meta(path)
    print('18 equipment definitions authored; existing files preserved.')


if __name__ == '__main__':
    main()
