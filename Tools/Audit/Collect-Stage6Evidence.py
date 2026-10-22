"""Summarize recorded observations; this does not rerun game scenarios."""
import hashlib,json,pathlib,collections
ROOT=pathlib.Path(__file__).resolve().parents[2]
OUT=ROOT/'Logs/auditoria/2026-10-16_083542_382c7a/etapa6'
COPY=ROOT/'Builds/Auditoria/2026-10-16_083542_382c7a/ExecucaoEtapa6'
def read(name):return json.loads((OUT/(name+'.json')).read_text(encoding='utf-8-sig'))
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
manifest=json.loads((OUT.parent/'manifest.json').read_text(encoding='utf-8-sig'))
changes=[]; known=set()
for row in manifest['files']:
    if not row['path'].startswith(('Assets/','Packages/','ProjectSettings/')):continue
    known.add(row['path']); f=COPY/row['path']; h=sha(f) if f.exists() else None
    if h!=row['sha256']:changes.append(dict(path=row['path'],before=row['sha256'],after=h))
added=[dict(path=f.relative_to(COPY).as_posix(),sha256=sha(f)) for folder in ['Assets','Packages','ProjectSettings'] for f in (COPY/folder).rglob('*') if f.is_file() and f.relative_to(COPY).as_posix() not in known]
(OUT/'execution-source-differences.json').write_text(json.dumps(dict(changed=changes,added=added),indent=2))
def human(s,owner=3):return next(p for p in s['players'] if p['human'] and p['owner']==owner)
checks=[]
def check(name,condition,evidence):checks.append(dict(name=name,result='PASS' if condition else 'FAIL',evidence=evidence))
s=read('match-start')['server']; positions=[(p['x'],p['y']) for p in s['players']]
check('Two humans and six bots enter real matchmaking',sum(p['human'] for p in s['players'])==2 and len(s['players'])==8,'match-start.json')
check('All eight participants have distinct initial positions',len(set(positions))==8,'match-start.json')
check('Fire RPC consumes one round and replicates sequence',all(human(read('after-fire')[r])['rounds']==11 and human(read('after-fire')[r])['shots']==1 for r in ['server','client1']),'after-fire.json')
check('Reload RPC restores magazine',all(human(read('after-reload')[r])['rounds']==12 for r in ['server','client1']),'after-reload.json')
add_result=next((OUT/'campaign2/server').glob('*-add-result.json'))
slots0=[json.loads(x) for x in human(json.loads(add_result.read_text())['state'])['slots']]
slots1=[json.loads(x) for x in human(read('after-drop')['server'])['slots']]
check('Drop RPC empties occupied slot zero',slots0[0]['Quantity']==3 and slots1[0]['Quantity']==0,'campaign2/server/*-add-result.json then after-drop.json (world pickup not separately counted)')
s=read('closed-chest'); chest=max(s['server']['chests'],key=lambda c:c['id'])
check('E2-06 reproduced: closed chest contents reach observing client',any(c['id']==chest['id'] and c['open']==0 and c['contents']==chest['contents'] and c['contents'] for c in s['client1']['chests']),'closed-chest.json')
check('Far client does not receive that chest',not any(c['id']==chest['id'] for c in s['client2']['chests']),'closed-chest.json')
check('E3-01 reproduced: mouse wins over north gamepad aim',abs(human(read('aim-gamepad-and-mouse')['server'])['angle']-180)<1,'aim-gamepad-and-mouse.json; injected stick north=90 degrees')
check('Movement reaches server and owner',abs(human(read('after-move')['server'])['x']-human(read('after-move')['client1'])['x'])<.1 and human(read('after-move')['server'])['x']>human(read('after-fire')['server'])['x']+1,'after-move.json')
check('Administrative bank reaches Extracted state',all(human(read('after-bank')[r])['status']=='Extracted' for r in ['server','client1']),'after-bank.json; does not validate zone/crank interactions or injected failure')
check('Lethal damage reaches Dead state and ends match',human(read('after-death')['server'],4)['status']=='Dead' and read('after-death')['server']['state']=='Ended','after-death.json')
check('Both clients reconnect and synchronize camp',all(read('reconnected')[r]['connected'] and read('reconnected')[r]['synchronized'] and read('reconnected')[r]['camp'] for r in ['client1','client2']),'reconnected.json')
before=read('visibility-before')['client1']; chest=max(before['chests'],key=lambda c:c['id'])
check('Visibility hides then re-shows same closed chest',not any(c['id']==chest['id'] for c in read('visibility-hidden')['chests']) and any(c['id']==chest['id'] and c['open']==0 and c['contents']==chest['contents'] for c in read('visibility-shown')['chests']),'visibility-before/hidden/shown.json; server teleports used')
check('Sprint spends stamina',0<next(p for p in read('sprint')['server']['players'] if p['human'])['stamina']<100,'sprint.json; owner value may lag')
binaries={str(p.relative_to(OUT)):sha(p) for p in [OUT/'Player/CyberExtractionAudit.exe',OUT/'Player/CyberExtractionAudit_Data/Managed/Assembly-CSharp.dll',OUT/'campaign1/Player/CyberExtractionAudit_Data/Managed/Assembly-CSharp.dll'] if p.exists()}
result=dict(checks=checks,counts=dict(collections.Counter(x['result'] for x in checks)),binaries=binaries,
            limits=['No Android device tests','No performance benchmark','No two-room lease/fault injection','No failure after bank or XP persistence failure','No complete AI/melee/hazard/navigation campaign','No full shadow/gait/minimap quality acceptance','Windows graphics capture succeeded only with server and one visible client'])
(OUT/'observations-summary.json').write_text(json.dumps(result,indent=2))
print(json.dumps(dict(counts=result['counts'],source_changes=len(changes),added=len(added),changed_paths=[c['path'] for c in changes])))
