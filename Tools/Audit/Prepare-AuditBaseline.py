"""Stage 1 only: inventory and verified source copy; never launch Unity or tests."""
from pathlib import Path
from datetime import datetime, timezone, timedelta
import hashlib
import json
import os
import re
import shutil
import subprocess
import uuid

ROOT = Path(__file__).resolve().parents[2]
SOURCE_DIRS = ('Assets', 'Packages', 'ProjectSettings', 'Tools', 'Docs', 'Art', 'Prototypes', 'Sites', '.agents')
SKIP_DIRS = {'.git', 'Library', 'Temp', 'obj', 'Build', 'Builds', 'Logs', 'UserSettings', 'Backups',
             '.cache', '.utmp', 'node_modules', '__pycache__', '.venv', '.vs', '.idea'}
SKIP_SUFFIXES = {'.keystore', '.jks', '.pfx', '.p12', '.pem', '.key'}
REPORT = 'Docs/Auditoria/ETAPA_1_PREPARACAO_2026-10-16.md'


def git(*args):
    return subprocess.check_output(['git', *args], cwd=ROOT)


def write_json(path, data):
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


def sha(path):
    digest = hashlib.sha256()
    with path.open('rb') as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b''):
            digest.update(chunk)
    return digest.hexdigest()


def reparse(path):
    return path.is_symlink() or bool(getattr(path.lstat(), 'st_file_attributes', 0) & 0x400)


def main():
    now = datetime.now(timezone(timedelta(hours=-3)))
    run_id = now.strftime('%Y-%m-%d_%H%M%S') + '_' + uuid.uuid4().hex[:6]
    evidence = ROOT / 'Logs/auditoria' / run_id
    project = ROOT / 'Builds/Auditoria' / run_id / 'Projeto'
    evidence.mkdir(parents=True, exist_ok=False)
    status_before = git('status', '--porcelain=v1', '-z', '--untracked-files=all')
    (evidence / 'git-status-before.bin').write_bytes(status_before)
    (evidence / 'git-working-tree.patch').write_bytes(git('diff', '--binary', '--no-ext-diff'))
    (evidence / 'git-index.patch').write_bytes(git('diff', '--cached', '--binary', '--no-ext-diff'))
    candidates, skipped = [], []
    for directory in SOURCE_DIRS:
        parent = ROOT / directory
        if not parent.exists():
            continue
        if reparse(parent):
            skipped.append({'path': directory, 'reason': 'link/reparse point'})
            continue
        for current, directories, files in os.walk(parent, followlinks=False):
            keep = []
            for name in sorted(directories):
                child = Path(current) / name
                if name in SKIP_DIRS or reparse(child):
                    skipped.append({'path': child.relative_to(ROOT).as_posix(), 'reason': 'generated/private directory or link'})
                else:
                    keep.append(name)
            directories[:] = keep
            for name in sorted(files):
                path = Path(current) / name
                relative = path.relative_to(ROOT).as_posix()
                sensitive = path.suffix.lower() in SKIP_SUFFIXES or name.lower().startswith('.env') or name.lower() in ('identity.json', 'camp-records.json')
                if reparse(path) or sensitive or relative == REPORT:
                    skipped.append({'path': relative, 'reason': 'link/private material or generated stage-1 report'})
                else:
                    candidates.append(path)
    for path in ROOT.iterdir():
        if path.is_file() and not reparse(path) and (path.suffix.lower() in ('.md', '.ps1', '.bat') or path.name in ('.gitignore', '.gitattributes', '.vsconfig')):
            candidates.append(path)
    candidates.sort(key=lambda p: p.relative_to(ROOT).as_posix())
    total_bytes = sum(path.stat().st_size for path in candidates)
    if shutil.disk_usage(ROOT).free < total_bytes * 1.25 + 2 * 1024**3:
        raise RuntimeError('Insufficient disk space for an independent source copy.')
    project.mkdir(parents=True, exist_ok=False)
    manifest = []
    for path in candidates:
        relative = path.relative_to(ROOT).as_posix()
        before = path.stat()
        target = project / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(path, target)
        source_hash, target_hash = sha(path), sha(target)
        after = path.stat()
        if source_hash != target_hash or (before.st_size, before.st_mtime_ns) != (after.st_size, after.st_mtime_ns):
            raise RuntimeError('Source changed or copy mismatch: ' + relative)
        manifest.append({'path': relative, 'bytes': after.st_size, 'sha256': source_hash})
    manifest_hash = hashlib.sha256('\n'.join(f"{row['path']}\0{row['bytes']}\0{row['sha256']}" for row in manifest).encode()).hexdigest()
    tracked = set(git('ls-files', '-z').decode('utf-8').rstrip('\0').split('\0'))
    status_after = git('status', '--porcelain=v1', '-z', '--untracked-files=all')
    (evidence / 'git-status-after-copy.bin').write_bytes(status_after)
    if status_before != status_after:
        raise RuntimeError('Git state changed during snapshot; preserve this run and prepare a new baseline.')
    stats = {}
    for row in manifest:
        area = row['path'].split('/')[0]
        data = stats.setdefault(area, {'files': 0, 'bytes': 0})
        data['files'] += 1
        data['bytes'] += row['bytes']
    runtime = sorted((ROOT / 'Assets/Scripts').rglob('*.cs'))
    editor = sorted((ROOT / 'Assets/Editor').rglob('*.cs'))
    areas = {area.name: [p.relative_to(ROOT).as_posix() for p in sorted(area.rglob('*.cs'))]
             for area in sorted((ROOT / 'Assets/Scripts').iterdir()) if area.is_dir()}
    docs = sorted(p.relative_to(ROOT).as_posix() for p in (ROOT / 'Docs/Auditoria').glob('*') if p.is_file() and p.name != Path(REPORT).name)
    historical = (ROOT / 'Docs/Auditoria/AUDITORIA_SKILLS_2026-10-09.md').read_text(encoding='utf-8-sig')
    previous_findings = [{'id': item[0], 'title': item[1], 'status': 'A reavaliar; historico nao valida o codigo atual'}
                         for item in re.findall(r'^### (A\d+)\. (.+)$', historical, re.M)]
    packages = json.loads((ROOT / 'Packages/manifest.json').read_text(encoding='utf-8-sig'))['dependencies']
    version = (ROOT / 'ProjectSettings/ProjectVersion.txt').read_text().splitlines()[0].split(': ', 1)[1]
    def number(file, pattern):
        return int(re.search(pattern, (ROOT / file).read_text(encoding='utf-8-sig'))[1])
    summary = {
        'run_id': run_id, 'date_local': now.isoformat(), 'timezone': 'America/Sao_Paulo',
        'head': git('rev-parse', 'HEAD').decode().strip(), 'branch': git('branch', '--show-current').decode().strip(),
        'unity': version, 'dependencies': packages,
        'protocol': number('Assets/Scripts/Core/GameConfig.cs', r'NetworkProtocol\s*=\s*(\d+)'),
        'profile_schema': number('Assets/Scripts/Camp/CampProfileMigration.cs', r'CurrentVersion\s*=\s*(\d+)'),
        'shared_document': number('Assets/Scripts/Camp/SharedCampMemory.cs', r'CurrentVersion\s*=\s*(\d+)'),
        'snapshot_project': str(project), 'evidence': str(evidence), 'files_copied': len(manifest), 'bytes_copied': total_bytes,
        'tracked_files_copied': sum(row['path'] in tracked for row in manifest),
        'untracked_or_ignored_files_copied': sum(row['path'] not in tracked for row in manifest),
        'manifest_sha256': manifest_hash, 'runtime_scripts': len(runtime), 'editor_scripts': len(editor),
        'source_directories': stats, 'scene': 'Assets/Scenes/Game.unity',
        'unity_started': False, 'tests_run': False, 'builds_run': False, 'gameplay_changes': False,
        'git_state_unchanged_during_copy': True,
    }
    write_json(evidence / 'manifest.json', {'files': manifest, 'manifest_sha256': manifest_hash})
    write_json(evidence / 'inventory.json', {'summary': summary, 'systems': areas, 'historical_documents': docs,
                                           'historical_findings': previous_findings, 'excluded_paths': skipped})
    context = {
        'project': str(project), 'cluster_id': 'audit-' + run_id,
        'server_port_proposed': 19177, 'port_reserved': False,
        'data_directory': str(evidence / 'runtime-data'),
        'client_identity_files': [str(evidence / f'client-{i}-identity.json') for i in (1, 2)],
        'note': 'Reserved names only; no identities, saves, sessions or servers created. Verify port availability at stage 6.',
        'unity_editor': 'C:/Program Files/Unity/Hub/Editor/6000.0.84f1/Editor/Unity.exe',
        'future_test_policy': 'Stage 5: grouped C# probes. Stage 6: one Unity opening and reused Windows server/two clients.',
    }
    write_json(evidence / 'execution-context.json', context)
    write_json(evidence / 'summary.json', summary)
    rows = '\n'.join(f"| {name} | {data['files']} | {data['bytes']/1024**2:.2f} |" for name, data in stats.items())
    systems = '\n'.join(f"| `{name}` | {len(files)} |" for name, files in areas.items())
    report = f'''# Auditoria completa — etapa 1: preparação e mapeamento

Data: {now.isoformat()} · America/Sao_Paulo. Execução: `{run_id}`.

**Etapa 1 concluída.** Inventário e cópia independente conferida por SHA-256; nenhum teste, compilação, Unity, build, limpeza, migração de save ou alteração de gameplay executado.

## Base registrada

- Branch `{summary['branch']}`, HEAD `{summary['head']}`. A base inclui o conteúdo físico atual, inclusive modificações e arquivos não rastreados; não é apenas uma cópia do commit.
- Unity {version}; NGO {packages['com.unity.netcode.gameobjects']}; Transport {packages['com.unity.transport']}; URP {packages['com.unity.render-pipelines.universal']}; Input System {packages['com.unity.inputsystem']}.
- Protocolo {summary['protocol']}, perfil {summary['profile_schema']}, documento compartilhado {summary['shared_document']}.
- Cena habilitada: `Assets/Scenes/Game.unity`, com Input Actions registrado em EditorBuildSettings.
- {len(runtime)} scripts de runtime e {len(editor)} scripts de editor. Os arquivos de editor não foram executados.

## Preservação e cópia isolada

- Projeto: [{project.name}]({project.as_posix()}).
- Evidências: [pasta da execução]({evidence.as_posix()}).
- {len(manifest)} arquivos, {total_bytes/1024**3:.3f} GiB lógicos; {summary['tracked_files_copied']} rastreados e {summary['untracked_or_ignored_files_copied']} não rastreados/ignorados copiados.
- SHA-256 do inventário: `{manifest_hash}`. Cada arquivo possui hash próprio e foi comparado com a cópia; estado Git permaneceu igual durante a captura.
- `git-working-tree.patch`, `git-index.patch` e os estados Git inicial/final estão nas evidências locais. Exclusões locais permanecem registradas no estado Git e não foram reintroduzidas na cópia.
- A cópia usa arquivos independentes, sem hardlinks, sem `.git`, caches, builds anteriores, backups, dados de usuário ou credenciais identificáveis. A lista detalhada de exclusões consta em `inventory.json`.
- Não é backup dos saves reais, nem backup de toda a máquina. Arte, protótipos, histórico, ferramentas e fontes atuais estão incluídos nas raízes inventariadas; caches não são necessários para preservar essas fontes.
- O contexto futuro define cluster exclusivo, nomes de identidades separados e pasta própria para dados. Esses arquivos de identidade e saves ainda não foram criados. A porta proposta deve ser conferida antes do uso.
- O script de preparação faz parte da cópia. Este relatório e o plano administrativo não constituem validação do jogo; o relatório desta execução é produzido depois da captura.

| Raiz | Arquivos copiados | MiB lógicos |
| --- | ---: | ---: |
{rows}

## Mapa dos sistemas

| Pasta em Assets/Scripts | Scripts |
| --- | ---: |
{systems}

- **Core:** configurações, atributos, progressão e serviços compartilhados.
- **Camp:** identidade, comunicação, transações, persistência, atividades e comércio. Fluxo: identidade → sessão autenticada → comando/revisão → repositório → snapshot.
- **Items:** pilhas, identidade, equipamento, armas, recuperação, catálogos e tabelas de loot. Resultados sorteados pertencem ao servidor; assets armazenam configuração.
- **Players/Combat:** entradas, movimento, visão, combate, saúde, coleta e recipientes. Fluxo: intenção → validação no servidor → estado replicado.
- **Match:** fila, mapas, spawns, interesse por cliente, objetivos, extrações e ciclo da expedição. Fronteira crítica: base → reserva/commit → partida → extração/perda/rollback.
- **Enemies/Bots:** percepção, navegação, habilidades, decisões e recompensas.
- **UI:** HUD, menus e presenters uGUI; IMGUI permanece em ferramentas/minigames. Recursos visuais incluem carregamento por strings e caminhos compostos.
- **Diagnostics/Editor/Tools:** telemetria, verificadores e automação. Atualidade e efeitos desses verificadores serão revisados antes dos testes agrupados.

O inventário enumera cada script por sistema. Este mapa identifica áreas e fronteiras; não afirma que as implementações estejam corretas.

## Histórico e cobertura seguinte

- Auditoria inicial de 09/10: {len(previous_findings)} achados registrados para reavaliação. Nenhum foi marcado como resolvido ou recorrente nesta etapa.
- Implementação e correções de 10/10: evidências históricas, sem comprovação automática para o código atual.
- Revisão de obsolescência e limpeza de 12/10: referências para a etapa 4. Valores de espaço e recomendações antigas precisam de medição atual.
- Integrações das etapas 1–9 de equipamentos/atributos/loot: especificação e limites para confrontar na auditoria; não substituem testes.
- Todos os documentos históricos identificados, IDs dos achados anteriores e caminhos de ferramentas estão em `inventory.json`.

| Etapa | Situação | Entrega prevista |
| --- | --- | --- |
| 1 — Preparar e mapear | Concluída | Este relatório, inventário, hashes e cópia isolada |
| 2 — Dados, segurança e economia | Não iniciada | Revisão de autoridade, conservação, persistência, comércio e XP |
| 3 — Gameplay e interface | Não iniciada | Revisão de combate, IA, mapas, visão, animações e controles |
| 4 — Possíveis reduções | Não iniciada | Candidatos com dependências, confiança e benefício, sem apagar |
| 5 — Provas fora da Unity | Não iniciada | Casos C# agrupados usando regras reais |
| 6 — Unity e jogo integrado | Não iniciada | Uma abertura Unity e campanha com servidor/dois clientes reutilizados |
| 7 — Consolidar resultados | Não iniciada | Achados, cobertura, limitações e fila de correções/limpeza |

## Limites e retomada

Nenhum binário antigo ou log anterior valida esta base. A cópia ainda não foi importada ou compilada. Referências, RPCs, física, UI, desempenho e Android não foram validados. Nenhum arquivo foi classificado como seguro para exclusão.

Na etapa 2, usar a base desta execução e seus hashes. Alterações posteriores devem ser registradas como uma nova revisão da base, sem misturar evidências de versões diferentes. Ferramentas de teste adicionadas posteriormente devem ser identificadas separadamente do conteúdo auditado. Saves normais e scripts de início do servidor real não devem ser usados pelos testes isolados.
'''
    (ROOT / REPORT).write_text(report, encoding='utf-8')
    print(json.dumps({'run_id': run_id, 'snapshot': str(project), 'evidence': str(evidence),
                      'files': len(manifest), 'gib': round(total_bytes/1024**3, 3),
                      'runtime_scripts': len(runtime), 'editor_scripts': len(editor),
                      'historical_findings': len(previous_findings), 'report': REPORT}, ensure_ascii=False))


if __name__ == '__main__':
    main()
