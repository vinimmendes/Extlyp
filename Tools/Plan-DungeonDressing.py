"""Generate planning documents only; no Unity, runtime assets or gameplay tests.

Zones are authoring search areas, NOT accepted placements or collider bounds.
Geometry comes from the current canal survey; exclusions come from C# sources.
"""
from pathlib import Path
import html
import json
import re
import runpy

ROOT = Path(__file__).resolve().parents[1]
geo = runpy.run_path(str(ROOT/'Tools/Survey-CanalGeometry.py'))
rooms, links, cell = geo['rooms'], geo['links'], geo['cell']
out = ROOT/'Docs/Mapas/DecoracaoNarrativa'
out.mkdir(parents=True, exist_ok=True)
content = (ROOT/'Assets/Scripts/Match/DungeonContent.cs').read_text(encoding='utf-8-sig')
breakables = json.loads((ROOT/'Assets/Resources/Environment/Dungeon/Breakables/BreakablesCatalog.json').read_text(encoding='utf-8-sig'))

# These IDs describe compositions, not item IDs or new interactive objects.
families = {
 'memorial': ('Memorial apagado', ['CAT-A09','CAT-P03','CAT-D07','CAT-D12'], []),
 'sepultura': ('Sepultura violada', ['CAT-P01','CAT-P02','CAT-P10','CAT-D11'], ['N01']),
 'ossario': ('Ossário retomado por raízes', ['CAT-A08','CAT-P04','CAT-D01','CAT-D02','CAT-D06'], []),
 'ritual': ('Ritual interrompido', ['CAT-P06','CAT-D07','CAT-D11','CAT-D12'], ['N05']),
 'cela': ('Cela esquecida', ['CAT-A11','CAT-A12','CAT-D09','CAT-D10'], ['N06','N07']),
 'coveiros': ('Trabalho dos coveiros', ['CAT-P07','CAT-D10'], ['N02','N03','N04']),
 'enchente': ('Margem após a enchente', ['CAT-F09','CAT-D03','CAT-D08','CAT-P10'], ['N08','N09']),
 'expedicao': ('Expedição perdida', ['CAT-D09','CAT-D11'], ['N10','N11']),
 'colapso': ('Colapso antigo', ['CAT-A05','CAT-P08','CAT-P09','CAT-D12'], []),
}
new_art = [
 ('N01','Mortalha rasgada','piloto', '2×1', 'Pano funerário plano; não usar silhueta de cadáver ou monstro.'),
 ('N02','Caixão inacabado','piloto', '3×1', 'Volume em recesso ou bloqueio existente; distinto de baú.'),
 ('N03','Ferramentas de coveiro','piloto', '2×1', 'Pá e ferramentas em conjunto; sem brilho de pickup.'),
 ('N04','Padiola funerária','posterior', '3×1', 'Compor área de serviço; carro quebrado fica como variante posterior.'),
 ('N05','Incensário caído','posterior', '1×1', 'Sem fumaça ou luz nova.'),
 ('N06','Tigela e utensílios de cela','posterior', '1×1', 'Cerâmica fosca; sem estado de saque.'),
 ('N07','Palha deteriorada','posterior', '2×1', 'Decalque de baixo contraste.'),
 ('N08','Madeira encharcada encalhada','piloto', '3×1', 'Fragmentos junto à margem; não sugerir ponte.'),
 ('N09','Sedimentos e detritos de margem','piloto', '3×1', 'Decalque, sem ocultar o limite físico da água.'),
 ('N10','Mochila rasgada decorativa','posterior', '1×1', 'Vazia e danificada, distinta dos pickups.'),
 ('N11','Acampamento apagado','posterior', '2×2', 'Cinzas e utensílios; sem fogo, loot ou interação nova.'),
]
choices = ['memorial','sepultura','memorial','cela','ossario','ritual','memorial','enchente',
           'sepultura','memorial','cela','cela','enchente','enchente','memorial','memorial',
           'ritual','ossario','ossario','coveiros','ritual','ritual','sepultura','enchente']
secondary = {8:'colapso',10:'memorial',12:'colapso',16:'memorial',17:'sepultura',
             18:'sepultura',19:'colapso',20:'expedicao',21:'memorial',22:'expedicao',23:'colapso'}
pilot = {8,19,13}

def wp(x,y): return [round(x*cell,4),round(y*cell,4)]
def rect_record(r):
    x,y,w,h=r
    return {'relativeCells':[x,y,w,h], 'worldUnits':[round(v*cell,4) for v in r]}

protected=[]
def protect(identifier, kind, point, radius):
    protected.append({'id':identifier,'kind':kind,'worldPosition':point,'radiusUnits':radius})
for i,r in enumerate(rooms[:8]): protect(f'S{i:02}','spawn',wp(*r[2:4]),3)
exits=[wp(rooms[14][2],rooms[14][3]),wp(rooms[18][2],rooms[18][3]),wp(56,-50)]
exits[0][1]=round(exits[0][1]+4,4); exits[1][1]=round(exits[1][1]+4,4)
for i,p in enumerate(exits): protect(f'E{i}','extraction',p,5)
mechanisms=[[round(p[0]-2.5,4),round(p[1]-2,4)] for p in exits[:2]]+[wp(52,-50),wp(47,-18)]
for i,p in enumerate(mechanisms): protect(f'M{i}','mechanism',p,2.8)
for i in [19,10]:
    p=wp(*rooms[i][2:4]); p[1]=round(p[1]+3,4)
    protect('Crank' if i==19 else 'Key','pedestal',p,3)
for i,(room,x,y) in enumerate(re.findall(r'new LootSlot\((\d+),(-?[\d.]+),(-?[\d.]+)',content)):
    ri=int(room); p=wp(*rooms[ri][2:4]); p[0]+=5*cell if ri==12 else 0
    protect(f'L{i:02}','loot',[round(p[0]+float(x),4),round(p[1]+float(y),4)],2.8)
for i,(room,x,y,kind) in enumerate(re.findall(r'new Encounter\((\d+),(-?[\d.]+),(-?[\d.]+),MonsterKind\.(\w+)',content)):
    p=wp(*rooms[int(room)][2:4])
    protect(f'F{i:02}',kind,[round(p[0]+float(x),4),round(p[1]+float(y),4)],2)

records=[]
for i,room in enumerate(rooms):
    name,sector,cx,cy,w,h=room; x,y,_,_=geo['room_rect'](room)
    # Opposing side bands delimit authoring searches; no automatic spawn here.
    side='oeste' if i%2==0 else 'leste'
    if i==13: side='oeste'
    zone=(x if side=='oeste' else x+w-3,y+2,3,h-4)
    key=choices[i]; title,reuse,new=families[key]
    notes=['Conferir bounds da arte existente e aberturas antes de fixar âncoras.']
    if i<8: notes+=['Conjunto pequeno lateral; centro e aproximação do spawn livres.']
    if i in [7,12,13,23]: notes+=['Conferir água; nenhuma colocação automática dentro de CanalReserved.']
    if i==13: notes+=['Piloto manual na margem oeste; preservar cais, embarque e saída sul.']
    if i in [14,18,19,10]: notes+=['Composição subordinada à extração ou pedestal existente.']
    records.append({'id':f'R{i:02}','roomIndex':i,'name':name,'sector':sector,
        'roomBounds':rect_record((x,y,w,h)), 'pilot':i in pilot,
        'primary':key,'secondaryOptional':secondary.get(i),
        'searchZone':dict(rect_record(zone),side=side,status='proposta; não é posição aprovada'),
        'envelopeCells': [2,1] if i<8 else [4,3],
        'reuseFamilies':reuse,'newArt':new,
        'breakableCandidateIds':[p['id'] for p in breakables['candidates'] if p['room']==i],
        'notes':notes})

corridors=[]
for i,(a,b) in enumerate(links):
    ax,ay=rooms[a][2:4]; bx,by=rooms[b][2:4]; bend=(ax,by) if a<8 else (bx,ay)
    key=choices[b] if a<8 else choices[a]
    corridors.append({'id':f'C{i:02}','rooms':[a,b],
        'relativeCellPolyline':[[ax,ay],list(bend),[bx,by]],'family':key,
        'targetGroups':1 if a<8 else 2,
        'placement':'Laterais fora das salas; excluir aberturas e cruzamentos; não sobrepor rotas compartilhadas.',
        'status':'trecho mapeado; âncoras finais pendentes'})
for identifier,name,r in [('CA01','Galeria longa do canal',geo['rects']['CanalDetour']),
                          ('CA02','Ponte da comporta',geo['rects']['CanalBridge']),
                          ('CA03','Rio e saída da barca',geo['rects']['CanalSouthRiver'])]:
    corridors.append({'id':identifier,'name':name,'bounds':rect_record(r),'family':'enchente',
        'targetGroups':0 if identifier!='CA01' else 2,
        'placement':'Somente projeto manual de margens; ponte, faixa de passagem e percurso do barco livres.',
        'status':'reserva existente preservada; sem autorização de colocação automática'})

data={'version':1,'stage':'planejamento, sem integração runtime','date':'2026-10-19',
      'coordinateSystem':'relativeCells: origem centro do mapa; worldUnits = cells × 1.06; y positivo norte',
      'sources':['DungeonLayout.cs','DungeonLayout.Canal.cs','DungeonContent.cs','DungeonDetails.cs','DungeonBreakableLayout.cs','BreakablesCatalog.json'],
      'placementStatus':'Zonas e envelopes são propostas. Não validam encaixe visual, física, fog ou navegação em Unity.',
      'rooms':records,'corridors':corridors,'compositions':families,
      'newArt':[dict(id=i,name=n,priority=p,envelopeCells=s,notes=t) for i,n,p,s,t in new_art],
      'protectedPoints':protected,'protectedCanalRectangles':{k:rect_record(v) for k,v in geo['rects'].items()},
      'breakableReservations':{'worldFootprint':breakables['worldFootprint'],'paddingUnits':.2,'candidates':breakables['candidates']},
      'boatSweep':{'startWorld':exits[2],'translationWorld':wp(0,-16),
                   'visualEnvelopeCells':[2,4], 'note':'Retângulo varrido e margem adicional a conferir com bounds após rotação; não usar antigo sweep horizontal.'}}
(out/'distribuicao.json').write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')

svg=['<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1100 1000">',
 '<rect width="1100" height="1000" fill="#141817"/>',
 '<g font-family="sans-serif" fill="#e7ddc5"><text x="28" y="32" font-size="22">Catacumbas — decoração narrativa / etapa 1</text>',
 '<text x="28" y="56" font-size="13">Planta de planejamento. Faixas cobre são áreas de busca, não posições aprovadas.</text></g>',
 '<g transform="translate(28 84) scale(6)">']
for cells,color in [(geo['dry'],'#4a504b'),(geo['water'],'#264c5c'),(geo['bridge'],'#98713d')]:
    for x,y in sorted(cells): svg.append(f'<rect x="{x+64}" y="{63-y}" width="1" height="1" fill="{color}"/>')
for c in corridors[:len(links)]:
    pts=' '.join(f'{x+64},{64-y}' for x,y in c['relativeCellPolyline'])
    svg.append(f'<polyline points="{pts}" fill="none" stroke="#889689" stroke-width=".18" stroke-dasharray=".6 .6"/>')
for p in protected:
    x,y=[v/cell for v in p['worldPosition']]
    svg.append(f'<circle cx="{x+64}" cy="{64-y}" r="{p["radiusUnits"]/cell}" fill="#be6464" fill-opacity=".10" stroke="#c47c76" stroke-width=".12"><title>{p["id"]}: {p["kind"]}</title></circle>')
for p in breakables['candidates']:
    svg.append(f'<circle cx="{p["cellX"]+.5}" cy="{127-p["cellY"]+.5}" r=".26" fill="#a0c879"/>')
for r in records:
    x,y,w,h=r['searchZone']['relativeCells']
    svg.append(f'<rect x="{x+64}" y="{64-y-h}" width="{w}" height="{h}" fill="#c79a55" fill-opacity=".30" stroke="#c79a55" stroke-width=".2"/>')
    _,_,cx,cy,_,_=rooms[r['roomIndex']]
    color='#ffe2a1' if r['pilot'] else '#f0e6ce'
    svg.append(f'<text x="{cx+64}" y="{64-cy}" text-anchor="middle" fill="{color}" font-family="sans-serif" font-size="1.7" font-weight="bold">{r["id"]}<title>{html.escape(r["name"])}</title></text>')
svg+=['<path d="M120 114 L120 127" stroke="#f1ce92" stroke-width=".6"/><path d="M119 125 L120 127 L121 125" fill="none" stroke="#f1ce92" stroke-width=".4"/>','</g>',
 '<g font-family="sans-serif" fill="#e7ddc5" font-size="12">',
 '<text x="28" y="884">R00–R23: salas · anéis vermelhos: reservas · pontos verdes: 64 candidatos de quebráveis</text>',
 '<text x="28" y="906">Pilotos: R08 Galeria das urnas / R19 Contrapesos / R13 Embarcadouro. Saída do barco: sul.</text>',
 '<text x="28" y="928">Canal reservado: intervenção manual de margem. Não preencher ponte, cais ou trajetória.</text>',
 '<text x="28" y="950">Aberturas, arte existente e linhas de combate exigem conferência posterior. Nenhuma geometria alterada.</text>']
for r in records:
    i=r['roomIndex']; svg.append(f'<text x="810" y="{102+i*29}" font-size="11">{r["id"]} {html.escape(r["name"])}</text>')
svg+=['</g></svg>']
(out/'planta.svg').write_text('\n'.join(svg),encoding='utf-8')

md=['# Decoração narrativa — etapa 1: distribuição\n',
 'Levantamento de 19/10/2026. **Planejamento concluído; nenhuma composição nova integrada.**\n',
 'Arquivos: [planta](planta.svg), [catálogo com coordenadas e reservas](distribuicao.json). Regeneração: `Tools/Plan-DungeonDressing.py`.\n',
 '## Leitura e limites\n',
 'A planta usa a geometria atual do canal. R00–R23 são os índices reais das salas. C00–C27 são ligações autoradas; CA01–CA03 representam a galeria, ponte e rio acrescentados pelo canal. Ligações podem compartilhar piso: não contar cada linha como corredor físico independente.\n',
 'Faixas cobre delimitam áreas iniciais de busca junto às paredes, e podem intersectar reservas: não autorizam instanciar a composição inteira ali. Coordenadas finais, orientação e bounds de cada peça serão definidos nos pilotos. Envelopes máximos propostos: 2×1 células nas entradas, 4×3 nas salas; reduzir ou omitir se não couber.\n',
 'A geometria é um levantamento estático em células. Não inclui bounds de todos os sprites, nem comprova leitura sob fog ou física. Não reduzir reservas para cumprir metas.\n',
 '## Distribuição por sala\n',
 '| ID | Sala | Composição principal | Secundária opcional | Área de busca | Piloto |',
 '|---|---|---|---|---|---|']
for r in records:
    md.append(f'| {r["id"]} | {r["name"]} | {families[r["primary"]][0]} | {families[r["secondaryOptional"]][0] if r["secondaryOptional"] else "—"} | lateral {r["searchZone"]["side"]} | {"Sim" if r["pilot"] else "—"} |')
md+=['\nAs secundárias são alternativas de enriquecimento, não cotas. R20 e R22 são as únicas candidatas a expedição perdida; evitar repetir o mesmo conjunto. Entradas mantêm centros livres e não anunciam saídas ou loot inexistentes.\n',
 '## Pilotos e reaproveitamento\n',
 '- **R08:** compor sobre sarcófago/cobertura já existente ou recesso. Tampa, cerâmica e tecido existentes; produzir mortalha N01. Não criar outro volume atravessável no chão livre.',
 '- **R19:** mesa e correntes existentes; produzir caixão N02 e ferramentas N03. Reservar pedestal da manivela, loot e os dois encontros; padiola N04 somente depois do piloto.',
 '- **R13:** marcas de inundação, umidade e cerâmica existentes; produzir madeira N08 e sedimentos N09. Intervenção manual na margem oeste; a faixa proposta NÃO contorna a proteção de `CanalReserved`.\n',
 '## Famílias existentes e lacunas\n',
 '| Composição | Famílias existentes a reaproveitar | Arte nova |', '|---|---|---|']
for k,(name,reuse,new) in families.items(): md.append(f'| {name} | {", ".join(reuse)} | {", ".join(new) or "Nenhuma obrigatória"} |')
md+=['\nIDs CAT referem-se ao catálogo atual, com arquivos verificados em Resources. Reaproveitamento técnico não substitui inspeção visual dos recortes e das variantes antes do piloto.\n',
 '## Produção adicional proposta\n', '| ID de planejamento | Peça | Prioridade | Envelope em células | Regra |', '|---|---|---|---|---|']
for i,n,p,s,t in new_art: md.append(f'| {i} | {n} | {p} | {s} | {t} |')
md+=['\nN01–N11 são IDs de planejamento, ainda sem sprites ou caminhos runtime. O piloto requer cinco famílias novas (N01, N02, N03, N08 e N09). Preservar pintura e leve inclinação aprovadas, sem introduzir câmera isométrica; volumes coerentes com a projeção superior existente. Não girar inscrições espelhadas.\n',
 '## Corredores\n', '| ID | Ligação / área | Família | Meta opcional |', '|---|---|---|---|']
for c in corridors:
    label=' ↔ '.join(f'R{r:02}' for r in c['rooms']) if 'rooms' in c else c['name']
    md.append(f'| {c["id"]} | {label} | {families[c["family"]][0]} | {c["targetGroups"]} |')
md+=['\nNos corredores, adaptar a família a detalhes rasos de 1–2 células nas bordas: placas, cerâmica, cera, sedimentos ou raízes; não inserir caixões/móveis altos na passagem. Sem conjuntos nos cruzamentos e acessos às portas. Excluir os trechos dentro das salas para não duplicar sua decoração. CA01 admite somente projeto manual lateral; CA02/CA03 permanecem livres de grupos no trajeto.\n',
 '## Reservas registradas\n',
 f'- {len(protected)} pontos: 8 spawns, 3 extrações, 4 mecanismos, 2 pedestais, 24 pontos de loot e 18 encontros. Raios extraídos da política de `DungeonDetails`: 3 / 5 / 2,8 / 3 / 2,8 / 2 unidades, respectivamente.',
 '- Preservar os 64 candidatos de quebráveis, inclusive os não ativados na partida: pegada 0,9 u + folga total de 0,2 u. Permanecem 48 objetos ativos e chance de loot de 10%; não alterar balanceamento.',
 '- Proteger aproximações de portas (regra `AtDoorApproach`), cruzamentos, coberturas/pilares e bounds dos renderizadores já colocados. Esses elementos exigem conferência por peça, não aparecem todos como círculos na planta.',
 '- `CanalArea` e `CanalDetour` continuam reservados à colocação automática. A nova decoração de margens deverá ter âncoras próprias e checagens explícitas; não remover `CanalReserved` globalmente.',
 '- Barca: proa e percurso para sul, deslocamento de 16 células, envelope visual aproximado 2×4 células após rotação. Reservar toda a varredura e conferir bounds reais. O sweep horizontal antigo em `DungeonDetails.Reserved` é legado redundante da reserva ampla; não reutilizá-lo como modelo da nova rota.',
 '- Nenhum collider, novo bloqueio de visão, interação, luz ou cobertura será criado para acomodar decoração. Volumes somente em áreas já bloqueadas/recessos; piso livre recebe decalques planos.\n',
 '## Integração prevista, sem executar nesta etapa\n',
 'Coordenar `DungeonDetails`, `DungeonProps` e `DungeonSectorArt` com um registro comum de ocupação, incluindo composições autoradas antes do preenchimento opcional. Usar IDs estáveis, sem novos NetworkObjects cosméticos ou sorteio divergente por cliente. Falta de espaço omite a peça; falha de asset continua diagnosticável. Os marcos atuais continuam; substituições pontuais devem retirar apenas duplicatas identificadas.\n',
 '## Evidência e próximo passo\n',
 'Conferidos por leitura: geometria atual, ligações, pontos de conteúdo, candidatos de quebráveis, famílias e caminhos de assets. Gerados planta e JSON fora da Unity. Nenhum teste automatizado, Unity, build ou alteração de runtime foi iniciado.\n',
 'A etapa 2 montará os três pilotos, fixará âncoras individuais e permitirá avaliar escala, densidade, encaixe e legibilidade antes de replicar o restante. Referências: Darkwood (atmosfera), Bullet Echo (combate), catacumbas de Roma e Paris (organização funerária).\n']
(out/'ETAPA_1.md').write_text('\n'.join(md)+'\n',encoding='utf-8')
missing=[]
for _,reuse,_ in families.values():
    for identifier in reuse:
        if not list((ROOT/'Assets/Resources/Environment/Dungeon').rglob(identifier+'_v*.png')):
            missing.append(identifier)
if missing: raise ValueError('Famílias sem arquivos: '+str(sorted(set(missing))))
print(json.dumps({'rooms':len(records),'authoredLinks':len(links),'canalAreas':3,
                  'protectedPoints':len(protected),'breakableCandidates':len(breakables['candidates']),
                  'pilotRooms':sorted(pilot),'newArtFamilies':len(new_art),'missingReuseFamilies':missing},ensure_ascii=False))
