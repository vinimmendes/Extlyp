from pathlib import Path
import zipfile,json
P=Path(__file__).resolve().parent
for filename in ['rig_and_contact.json','cloth_collision.json','framing.json']:
    report=json.loads((P/'validation'/filename).read_text())
    assert report.get('passed',True) is not False,filename
with zipfile.ZipFile(P/'warrior_v2_bundle.zip','w',zipfile.ZIP_DEFLATED) as z:
    for p in P.rglob('*'):
        if p.is_file() and (p.suffix in {'.blend','.gif','.png','.py','.md','.json','.mp4'} or p.name=='LICENSE'):
            z.write(p,p.relative_to(P))
print('Bundle saved:',(P/'warrior_v2_bundle.zip').stat().st_size)
