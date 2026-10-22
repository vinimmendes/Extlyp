"""Create an isolated Unity execution project; never edit the frozen source."""
import hashlib, json, pathlib, shutil
ROOT = pathlib.Path(__file__).resolve().parents[2]
BASE = ROOT / 'Builds/Auditoria/2026-10-16_083542_382c7a/Projeto'
COPY = BASE.parent / 'ExecucaoEtapa6'
OUT = ROOT / 'Logs/auditoria/2026-10-16_083542_382c7a/etapa6'
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
OUT.mkdir(parents=True, exist_ok=True)
if COPY.exists(): raise SystemExit('Execution folder already exists; refusing overwrite')
manifest = json.loads((OUT.parent / 'manifest.json').read_text(encoding='utf-8-sig'))
drift=[]
for row in manifest['files']:
    old=BASE/row['path']; current=ROOT/row['path']
    if sha(old)!=row['sha256']: raise RuntimeError('Frozen file changed: '+row['path'])
    if not current.exists() or sha(current)!=row['sha256']:
        drift.append({'path':row['path'],'baseline':row['sha256'],'current':sha(current) if current.exists() else None})
for folder in ['Assets','Packages','ProjectSettings']:
    shutil.copytree(BASE/folder,COPY/folder)
monster=COPY/'Assets/Scripts/Enemies/Monster.cs'
old=monster.read_text(encoding='utf-8-sig')
new=old.replace('BotController.InVision(transform.position,local.transform.position,local.transform.eulerAngles.z,local)', 'BotController.InVision(transform.position,local.transform.position,local.transform.eulerAngles.z,local.GetComponent<PlayerInventory>())')
# Whitespace differs between call sites; copy only the verified external type correction.
if new==old:
    import re
    new=re.sub(r'(BotController\.InVision\([^\n]*?),\s*local\)',r'\1,local.GetComponent<PlayerInventory>())',old)
if new.count('local.GetComponent<PlayerInventory>()')-old.count('local.GetComponent<PlayerInventory>()')!=2:
    raise RuntimeError('Expected exactly two type corrections')
monster.write_text(new,encoding='utf-8')
rpc=COPY/'Assets/Scripts/Players/PlayerEquipment.cs'
rpc.write_text(rpc.read_text(encoding='utf-8-sig').replace('CancelEquipmentRpc','CancelEquipmentServerRpc').replace('ExchangeEquipmentRpc','ExchangeEquipmentServerRpc'),encoding='utf-8')
import re
for asset in (COPY/'Assets/Resources/Items/Equipment/Definitions').glob('*.asset'):
    text=asset.read_text(encoding='utf-8-sig')
    packed=re.search(r'SecondaryPool: ([0-9a-f]+)',text)
    if packed and len(packed.group(1))==16:
        old=packed.group(1)
        asset.write_text(text.replace('SecondaryPool: '+old,'SecondaryPool: '+old[:2]+old[8:10]),encoding='utf-8')
for source,target in [('Stage6Runtime.cs','Assets/AuditStage6/Stage6Runtime.cs'),('Stage6Editor.cs','Assets/Editor/Stage6Editor.cs')]:
    p=COPY/target; p.parent.mkdir(parents=True,exist_ok=True); shutil.copy2(ROOT/'Tools/Audit'/source,p)
evidence={'execution_project':str(COPY),'baseline_verified_files':len(manifest['files']),'current_drift':drift,
          'overlay':'Two Monster.InVision arguments NetworkObject -> PlayerInventory; two equipment RPC methods/calls receive ServerRpc suffix; 18 equipment SecondaryPool arrays packed as two bytes (these corrections already exist in working project); test harness added only to execution copy',
          'source_hashes':{r['path']:r['sha256'] for r in manifest['files'] if r['path'].startswith(('Assets/','Packages/','ProjectSettings/'))}}
(OUT/'preparation.json').write_text(json.dumps(evidence,indent=2),encoding='utf-8')
print(json.dumps({'project':str(COPY),'verified':len(manifest['files']),'drift_count':len(drift),'evidence':str(OUT)}))
