"""Finish only the UI assets authored by this task; preserve existing source/assets."""
from pathlib import Path
import re,uuid,json,hashlib
ROOT=Path(__file__).resolve().parents[2]
def meta(path):
    target=Path(str(path)+'.meta')
    if not target.exists():target.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid5(uuid.NAMESPACE_URL,'cyber-extraction-ui/'+str(path.relative_to(ROOT))).hex+'\nMonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n',encoding='utf-8')
for path in (ROOT/'Assets/Scripts/UI').glob('*.cs'):meta(path)
meta(ROOT/'Assets/Editor/PlayerUiAssetPreparation.cs')
for path in (ROOT/'Assets/Scripts/UI').glob('UguiGameHud*.cs'):
    source=path.read_text(encoding='utf-8');source=re.sub(r'\n(?:[ \t]*\n){3,}','\n\n',source)
    path.write_text(source,encoding='utf-8')
paths=list((ROOT/'Assets/Resources/UI/Prefabs').glob('*.prefab'))
(ROOT/'Tools/UI/initial-prefabs.json').write_text(json.dumps({str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest() for p in paths},indent=2),encoding='utf-8')
print('Metadata and authoring manifest complete.')
