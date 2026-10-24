"""Record the agent's static art critique; never approves a room or publishes."""
from pathlib import Path
import json

root=Path(__file__).resolve().parents[1]
path=root/'Art/Catacumbas/ReworkCompleto/Comparacoes/revisao.json'
review=json.loads(path.read_text(encoding='utf-8'))
notes={
0:('Conferência final do foco do sino e sua relação com o apoio de recepção ainda aberta.','Rever reentrância, placa e cera como um conjunto antes de encerrar o vestíbulo.'),
1:('Conferência final de profundidade do nicho e fragmentos abaixo da baia vazia ainda aberta.','Manter urnas desmontáveis e o vazio legível; concentrar cinzas e quebra na origem.'),
2:('O memorial da entrada está mais simples que o arranjo de placas do conceito.','Recompor a relação entre placa preservada, placa deslocada e resíduos de raspagem.'),
3:('A grade precisa de leitura mais clara como grade recolhida no apoio.','Rever desenho, inclinação e fixação; não resolver apenas aumentando a peça atual.'),
4:('A ruptura de raízes ainda pede integração de terra, juntas e fragmentos.','Conferir origem na parede e continuidade até as pontas, mantendo entrada livre.'),
5:('Conferência final dos apoios e vestígios de vigília ainda aberta.','Variar alturas e disposição das velas sem repetir um arranjo de exposição.'),
6:('O memorial lateral ainda não reproduz toda a hierarquia arquitetônica do cortejo.','Rever relevo lateral e ligação com a ornamentação de passagem, sem criar cobertura nova.'),
7:('O depósito de madeira junto à margem ainda é fraco como foco da entrada do canal.','Recompor madeira, corda e marca de nível ligados à margem real.'),
8:('A quebra noroeste e seu rastro ainda são menos expressivos que no conceito.','O ritmo dos nichos e as urnas foram ajustados; reabrir densidade e continuidade de cinzas, cacos e circulação desgastada.'),
9:('A sequência de nomes apagados e o dano no lado leste ainda precisam de acabamento.','Recompor hierarquia de placas e raspagem, lascas diretamente abaixo e ferramenta ligada ao trabalho.'),
10:('Fixações e grade ainda precisam se integrar melhor à alvenaria.','Rever apoios das grades e argolas como cena de permanência; conservar chave e acesso ao pedestal.'),
11:('O armazenamento lateral foi recuperado, mas o conjunto de correntes norte continua esparso.','Produzir ou recompor fixações, correntes suspensas e corrosão sem repetir argolas embutidas no decal de ferrugem.'),
12:('Marcas minerais e umidade foram conectadas; resta aproximar seu acabamento do conceito.','Comparar contraste, espessura e desgaste das linhas de nível, sem criar água física nova. Base vazada já substitui a base lisa.'),
13:('Revisão final de transporte funerário, desgaste e ligação com o cais ainda aberta.','Conferir rastro até o embarque, madeira e cordas como operação única; manter percurso real da barca e sua pegada.'),
14:('Revisão final da ornamentação dirigida à escadaria ainda aberta.','Conferir escala e desgaste dos painéis, término junto à extração e bordas transitáveis.'),
15:('Os painéis foram ampliados, mas continuidade, fragmentos e proporção final ainda exigem comparação.','Rever proporção das figuras e encontros dos relevos, preservando a interrupção do vão norte e a fratura sul.'),
16:('Apoios já estão ancorados em reentrâncias, mas a repetição dos grupos ainda é visível.','Variar quantidade, alturas e posição de velas, trechos vazios, tecido e cera; preservar o centro silencioso.'),
17:('Falta fechar a integração do dano noroeste e dos pequenos vestígios do abrigo.','A relação abrigo/caixão e o leito de 2,0 × 0,8 u estão preservados. Refinar depósitos e encaixes sem ampliar os objetos para copiar proporções incorretas do conceito.'),
18:('O tronco ainda encobre demais os nichos oeste e não tem a mesma curva de invasão do conceito.','Replanejar pegada visual, deslocamento e curva das raízes para revelar nichos pressionados e restos associados. Não mudar colisão ou obstruir o poço.'),
19:('As roldanas recuperaram proporção e moldura, mas faltam continuidade das cordas e acabamento do trabalho.','Recompor o conjunto norte como mecanismo contínuo e aproximar ferragens, ferramenta e resíduos dos apoios existentes.'),
20:('O vestígio de piso agora é legível; grupos de vigília ainda repetem o mesmo módulo.','Equilibrar desgaste do desenho com circulação e variar apoios/velas/tecidos sem preencher o centro com objetos.'),
21:('O piso encontra o altar nordeste, mas as placas norte ainda não têm a arquitetura ritual do conceito.','Produzir módulos rituais próprios e integrar fragmentos ao altar; preservar dimensão física dos apoios e passagens.'),
22:('Compartimentos vazios e apoios foram corrigidos; tecidos ainda parecem dobrados em vez de pendentes.','Produzir tecido desmontável com queda apropriada e integrar portas, vasos e pano ao armazenamento violado. Não fechar a passagem leste real.'),
23:('Base vazada e resíduos contínuos estão montados; falta fechar acabamento junto ao escoamento.','Rever linha mineral, sedimento e madeira a partir do dreno real, mantendo centro transitável.')}
md=['# Revisão de fidelidade da montagem atual','',
    'Inspeção estática pelo agente. Nenhuma sala foi aprovada automaticamente. As comparações se referem ao SHA-256 `'+review['sourceSha256']+'` da captura nativa. As pendências abaixo mantêm a publicação bloqueada.','',
    'Correções desta iteração: escala global dos materiais de transição, recuperação de compartimentos vazios, bases de cisterna vazadas, vestígio cerimonial da R20, proporções de roldanas/relevos, nichos desmontáveis e ligação dos depósitos à arquitetura.','',
    '| Sala | Desvio / pendência | Causa e replanejamento |','| --- | --- | --- |']
for item in review['rooms']:
    pending,replan=notes[item['room']]
    item.update(resolved=False,pendingDeviations=pending,replan=replan)
    md.append(f'| R{item["room"]:02d} | {pending} | {replan} |')
path.write_text(json.dumps(review,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
(root/'Docs/Mapas/ReworkCompleto/REVISAO_MONTAGEM_ATUAL.md').write_text('\n'.join(md)+'\n',encoding='utf-8')
print('24 fichas de revisão registradas; publicação continua bloqueada.')
