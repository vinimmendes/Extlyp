"""Compare an isolated Unity build copy with an explicit Git revision, including LFS bytes."""
import argparse, hashlib, json, re, subprocess
from pathlib import Path

p=argparse.ArgumentParser(); p.add_argument('--revision',required=True); p.add_argument('--workspace',required=True); p.add_argument('--output',required=True); a=p.parse_args()
root=Path(__file__).resolve().parents[1]; workspace=Path(a.workspace).resolve()
entries=subprocess.check_output(['git','ls-tree','-rz',a.revision,'--','Assets','Packages','ProjectSettings'],cwd=root).split(b'\0')
proc=subprocess.Popen(['git','cat-file','--batch'],cwd=root,stdin=subprocess.PIPE,stdout=subprocess.PIPE)
bad=[]; imported=[]; configured=[]; checked=0; expected_paths=set()
try:
    for entry in entries:
        if not entry: continue
        header,name=entry.split(b'\t',1); _,kind,oid=header.split(); name=name.decode('utf-8')
        if kind!=b'blob': continue
        expected_paths.add(name)
        proc.stdin.write(oid+b'\n'); proc.stdin.flush()
        response=proc.stdout.readline().split(); n=int(response[2]); expected=proc.stdout.read(n); proc.stdout.read(1)
        path=workspace/name
        if not path.is_file(): bad.append(name+' (missing)'); continue
        actual=path.read_bytes(); checked+=1
        if expected.startswith(b'version https://git-lfs.github.com/spec/v1\n'):
            match=re.search(rb'oid sha256:([a-f0-9]+)',expected)
            if not match or hashlib.sha256(actual).hexdigest().encode()!=match[1]: bad.append(name)
        elif actual==expected or b'\0' not in expected[:8000] and actual.replace(b'\r\n',b'\n')==expected.replace(b'\r\n',b'\n'):
            pass
        elif name.endswith('.meta'):
            guid=lambda x: re.search(rb'(?m)^guid: ([a-f0-9]+)',x)
            left,right=guid(expected),guid(actual)
            if not left or not right or left[1]!=right[1]: bad.append(name)
            else: imported.append(name)
        elif name=='ProjectSettings/ProjectSettings.asset': configured.append(name)
        else: bad.append(name)
finally:
    proc.stdin.close(); proc.wait()
for folder in ['Assets','Packages']:
    for file in (workspace/folder).rglob('*'):
        if file.is_file() and file.relative_to(workspace).as_posix() not in expected_paths: bad.append(file.relative_to(workspace).as_posix()+' (unexpected)')
result={'revision':a.revision,'checkedFiles':checked,'sourceMatches':not bad,'mismatches':bad,'unityImportedMetadata':imported,'configuredPlayerSettings':configured}
Path(a.output).write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps({'checkedFiles':checked,'sourceMatches':not bad,'mismatches':bad[:20],'importedMetadataFiles':len(imported),'configuredSettings':configured}))
if bad: raise SystemExit(1)
