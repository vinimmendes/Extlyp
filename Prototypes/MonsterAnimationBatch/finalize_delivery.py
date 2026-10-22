from pathlib import Path
import json
BASE=Path(__file__).resolve().parent; ROOT=BASE.parents[1]
names={'Chest':'Baú de viagem surrado','Mimic':'Imitador · Mordida rasteira','Charger':'Charger · Ombros ossudos','Listener':'Ouvinte · Escutador','Shell':'Carapaça · Enrolado','Herald':'Arauto · Costelas ressonantes','Devourer':'Devorador · Colhedor','Spore':'Esporífero · Bolsas dorsais','Mother':'Planta-mãe · Nó de raízes','AttackBulb':'Bulbo atacante','HealingBulb':'Bulbo curador','Minion':'Lacaio'}
rows=[]; total=0
for name,title in names.items():
    folder=BASE/name
    if (folder/'.rendering').exists(): raise RuntimeError(name+' ainda em renderização')
    data=json.loads((folder/'clips.json').read_text(encoding='utf-8'))
    required=[folder/(name+'.blend'),folder/'idle.png',folder/'perspective.png',folder/'contact.png']
    for clip in data['clips']:
        required.append(folder/(clip['name']+'.gif'))
        required.extend(folder/clip['name']/f'{f:03}.png' for f in clip['frames'])
    missing=[str(p) for p in required if not p.exists()]
    if missing: raise RuntimeError('Arquivos incompletos: '+str(missing))
    total+=len(data['clips'])
    rows.append('| '+title+' | '+', '.join(c['name'] for c in data['clips'])+' | [Modelo](../../Prototypes/MonsterAnimationBatch/'+name+'/'+name+'.blend) |')
summary='''# Monstros escolhidos e baú — animações Blender

Entrega em 12/10/2026: doze modelos originais construídos e animados individualmente no Blender 4.5, baseados nos visuais escolhidos pelo usuário. Geometria e materiais foram simplificados em relação às ilustrações; os GIFs vêm dos renders reais dos modelos, não de imagens IA deformadas.

[Galeria local](../../Prototypes/MonsterAnimationBatch/index.html) · [Catálogo dos clipes](../../Prototypes/MonsterAnimationBatch/CATALOGO.md)

| Modelo | Clipes | Arquivo editável |
| --- | --- | --- |
'''+ '\n'.join(rows)+'''

## Construção e revisão

Materiais procedurais e câmeras superior/perspectiva. Membros com dois ossos, keyframes FK e trajetória de apoio linear; retorno elevado separado. Os destinos respeitam o alcance mínimo/máximo dos membros. Controles separados para tampa, cabeça, orelha, costelas, pescoço, bolsas e valvas das plantas. Arquivos salvos na pose de repouso (501), com marcadores dos clipes.

Baú e Imitador usam a mesma construção. O disfarce esconde braços/dentes e utiliza o mesmo PNG fechado do baú comum, evitando até diferenças de ruído entre renderizações. A tampa gira na dobradiça traseira; o fechamento da mordida mantém espaço entre as pontas dos dentes.

Refinamentos após inspeção: laterais da tampa preenchidas, limite mínimo de alcance dos braços recolhidos, apoios do Charger durante preparação, concavidade/material da orelha do Ouvinte, arcos e pescoço suavizados, boca do Colhedor acima do chão, poros acima das bolsas, raízes afiladas e valvas abrindo para fora.

O bulbo atacante e o lacaio usam a mesma geometria-base, preservando continuidade no amadurecimento. A mãe fica estacionária; o curador pulsa a seiva. FX de broto gerado e emissão de cura são demonstrações opcionais, separáveis do corpo na futura exportação.

## Tempos e exportação

Timeline a 24 fps, caminhada 1–32 com fechamento do ciclo em 33. Charger: 3 s de preparação, 0,5 s de avanço, 2 s de recuperação. Arauto: 2 s de preparação, emissão/retorno em seguida. Esporífero: aproximadamente 1,2 s de inflação antes da contração. Alimentação do Colhedor inclui cerca de 2,5 s antes do crescimento/retorno. Amadurecimento do bulbo: 9 s. Ataques curtos precisam ser normalizados ao aviso/dano autoritativo na integração.

Clipes longos têm PNGs de avaliação amostrados a cada dois/seis frames; a animação editável mantém os keyframes em 24 fps. Os índices exatos estão em clips.json. Os GIFs respeitam esses intervalos e acrescentam pausas para avaliação; não são a cadência definitiva do jogo. Folhas de sprites são de avaliação, com células de 256 e páginas até 2048 x 2048. Na exportação de produção, renderizar todos os frames necessários, girar a frente para +X e configurar escala/camadas.

## Limites desta entrega

Modelos, clipes, PNGs, GIFs e catálogos conferidos; isso não valida execução Unity, física, rede ou importação de sprites. Nenhum teste/build/sessão de validação Unity executado. Assets ativos e regras do jogo não foram substituídos nesta etapa de animação Blender; integração destes novos modelos permanece para uma etapa posterior. Os modelos/animações antes integrados do protagonista, aranha, Corrosivo e Rastejante foram preservados.
'''
(ROOT/'Docs/Integracao/MONSTROS_BLENDER_2026-10-12.md').write_text(summary,encoding='utf-8')
refs={'Charger':'Charger','Ouvinte':'Listener','Carapaca':'Shell','Arauto':'Herald','Devorador':'Devourer','Esporifero':'Spore','Plantas':'Mother'}
for ref,name in refs.items():
    p=ROOT/'Art/MonsterProposals'/ref/'README.md'; text=p.read_text(encoding='utf-8')
    text=text.replace('Modelagem e animação pendentes.','Modelo e animações Blender criados em Prototypes/MonsterAnimationBatch/'+name+'. Integração ao jogo pendente.')
    text=text.replace('Modelagem e animações desta versão ainda pendentes.','Modelo e animações Blender criados em Prototypes/MonsterAnimationBatch/Charger. Integração ao jogo pendente.')
    p.write_text(text,encoding='utf-8')
p=ROOT/'Art/ChestProposals/README.md'; text=p.read_text(encoding='utf-8').replace('Modelagem e integração pendentes, sem substituição dos assets do jogo nesta etapa.','Modelo e abertura/fechamento Blender criados em Prototypes/MonsterAnimationBatch/Chest. Integração ao jogo pendente.'); p.write_text(text,encoding='utf-8')
p=ROOT/'Art/MonsterProposals/Imitador/README.md'; text=p.read_text(encoding='utf-8'); text='Status em 12/10/2026: opção 1 da segunda rodada modelada e animada em Prototypes/MonsterAnimationBatch/Mimic. Integração ao jogo pendente; histórico de escolhas abaixo.\n\n'+text; p.write_text(text,encoding='utf-8')
p=ROOT/'Docs/Integracao/MONSTROS_PROXIMAS_ANIMACOES_2026-10-12.md'; text=p.read_text(encoding='utf-8').replace('Modelagem e animação pendentes.','Modelagem e animações Blender concluídas; integração destes novos modelos ao jogo pendente.').replace('Modelagem e integração pendentes.','Modelagem/abertura Blender concluídas; integração ao jogo pendente.'); text=text.replace('Escolha da criatura revelada pendente.','Criatura revelada escolhida: **1 — Mordida rasteira**, modelada e animada no Blender.'); text='Entrega posterior concluída: todos os modelos escolhidos e o baú foram animados. Veja [MONSTROS_BLENDER_2026-10-12.md](MONSTROS_BLENDER_2026-10-12.md). Integração dos novos modelos ao jogo ainda pendente.\n\n'+text; p.write_text(text,encoding='utf-8')
print('Delivery complete:',len(names),'editable models,',total,'clips. Documentation updated.')
