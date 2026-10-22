"""Read-only stage-4 inventory. Writes evidence only; does not import or run game code."""
from pathlib import Path
from collections import defaultdict
from datetime import datetime, timezone
import hashlib
import json
import os
import re

ROOT = Path(__file__).resolve().parents[2]
RUN = '2026-10-16_083542_382c7a'
BASE = ROOT / 'Builds' / 'Auditoria' / RUN / 'Projeto'
OUT = ROOT / 'Logs' / 'auditoria' / RUN / 'etapa4'
TEXT = {'.cs', '.py', '.ps1', '.bat', '.json', '.asset', '.prefab', '.unity', '.scenetemplate', '.shader', '.md', '.html', '.cjs', '.mjs', '.inputactions', '.asmdef', '.xml', '.meta', '.mat', '.spriteatlas', '.controller', '.anim', '.overridecontroller', '.rendertexture', '.preset', '.physicmaterial', '.physicsmaterial2d', '.shadervariants', '.fontsettings', '.terrainlayer', '.lighting'}


def reparse(path):
    return path.is_symlink() or bool(getattr(path.lstat(), 'st_file_attributes', 0) & 0x400)


def disk_inventory(folder):
    rows = defaultdict(lambda: {'files': 0, 'bytes': 0})
    skipped = []
    if not folder.exists():
        return {'present': False, 'groups': {}, 'skipped': []}
    if reparse(folder):
        return {'present': True, 'groups': {}, 'skipped': [folder.name]}
    for current, dirs, files in os.walk(folder, followlinks=False):
        keep = []
        for name in dirs:
            child = Path(current) / name
            if reparse(child):
                skipped.append(child.relative_to(ROOT).as_posix())
            else:
                keep.append(name)
        dirs[:] = keep
        for name in files:
            file = Path(current) / name
            if reparse(file):
                skipped.append(file.relative_to(ROOT).as_posix())
                continue
            relative = file.relative_to(folder)
            key = relative.parts[0] if len(relative.parts) > 1 else '(root files)'
            try:
                size = file.stat().st_size
            except OSError:
                skipped.append(file.relative_to(ROOT).as_posix())
                continue
            rows[key]['files'] += 1
            rows[key]['bytes'] += size
    return {'present': True, 'groups': dict(sorted(rows.items(), key=lambda row: -row[1]['bytes'])), 'skipped': skipped}


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    manifest = json.loads((OUT.parent / 'manifest.json').read_text(encoding='utf-8-sig'))['files']
    by_hash, by_root, by_resource = defaultdict(list), defaultdict(int), defaultdict(list)
    text_files = {}
    for entry in manifest:
        path = entry['path']
        by_root[path.split('/')[0]] += entry['bytes']
        if entry['bytes'] >= 4096 and not path.endswith('.meta'):
            by_hash[entry['sha256']].append(entry)
        file = BASE / path
        if file.suffix.lower() in TEXT and entry['bytes'] < 4 * 1024**2 and not path.startswith('.agents/'):
            text_files[path] = file.read_text(encoding='utf-8-sig', errors='replace')
        if '/Resources/' in path and not path.endswith('.meta'):
            key = path.split('/Resources/', 1)[1].rsplit('.', 1)[0]
            by_resource[key].append(path)
    duplicates = []
    for sha, entries in by_hash.items():
        if len(entries) > 1:
            duplicates.append({'sha256': sha, 'bytes_each': entries[0]['bytes'], 'redundant_bytes_upper_bound': entries[0]['bytes'] * (len(entries)-1), 'paths': [e['path'] for e in entries]})
    duplicates.sort(key=lambda r: -r['redundant_bytes_upper_bound'])
    resource_calls = []
    for path, content in text_files.items():
        for number, line in enumerate(content.splitlines(), 1):
            if 'Resources.Load' in line or 'Shader.Find' in line or 'AssetDatabase.LoadAsset' in line:
                resource_calls.append({'path': path, 'line': number, 'text': line.strip()})
    # An absent GUID edge is NOT proof of non-use: dynamic resource/name loading is separate.
    serialized = {p: t for p, t in text_files.items() if p.startswith(('Assets/', 'ProjectSettings/'))}
    refs = defaultdict(set)
    for path, content in serialized.items():
        # A meta's root GUID declares ownership; other GUIDs can be importer dependencies.
        reference_content = re.sub(r'(?m)^guid:\s*[0-9a-f]{32}[ \t]*\r?$', '', content) if path.endswith('.meta') else content
        for guid in re.findall(r'guid:\s*([0-9a-f]{32})', reference_content):
            refs[guid].add(path)
    guid_rows = []
    for entry in manifest:
        if not entry['path'].startswith('Assets/') or not entry['path'].endswith('.meta'):
            continue
        content = (BASE / entry['path']).read_text(encoding='utf-8-sig', errors='replace')
        match = re.search(r'^guid:\s*([0-9a-f]{32})', content, re.M)
        if match:
            guid_rows.append({'path': entry['path'][:-5], 'guid': match[1], 'serialized_references': sorted(refs.get(match[1], []))})
    drift = []
    for entry in manifest:
        file = ROOT / entry['path']
        if not file.is_file():
            drift.append({'path': entry['path'], 'status': 'missing'})
        elif hashlib.sha256(file.read_bytes()).hexdigest() != entry['sha256']:
            drift.append({'path': entry['path'], 'status': 'changed'})
    disk = {name: disk_inventory(ROOT / name) for name in ('Builds', 'Backups', 'Logs', '.utmp', '.cache', 'Library', 'obj', '.vs')}
    result = {'generated_at': datetime.now(timezone.utc).isoformat(), 'baseline': RUN,
              'method': 'Static metadata/text/hash scan; no Unity, build, gameplay test or deletion.',
              'manifest_files': len(manifest), 'source_bytes_by_root': dict(by_root),
              'duplicate_groups_ge_4k': duplicates, 'resources_key_collisions': {k: v for k, v in by_resource.items() if len(v) > 1},
              'dynamic_load_sites': resource_calls, 'asset_guid_references': guid_rows,
              'current_drift_vs_manifest': drift, 'local_disk_metadata': disk}
    (OUT / 'inventory.json').write_text(json.dumps(result, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    print(json.dumps({'manifest_files': len(manifest), 'duplicate_groups_ge_4k': len(duplicates),
                      'resource_key_collisions': len(result['resources_key_collisions']), 'dynamic_load_sites': len(resource_calls),
                      'drift_files': len(drift), 'disk_bytes': {k: sum(r['bytes'] for r in v['groups'].values()) for k, v in disk.items()}}, ensure_ascii=False))


if __name__ == '__main__':
    main()
