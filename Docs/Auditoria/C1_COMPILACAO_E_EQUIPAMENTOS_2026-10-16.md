# C1 — conferência das correções existentes

Situação: **concluída por revisão estática**, sem compilação, testes, Unity ou builds. Nenhuma alteração de código ou asset foi necessária. Validação nativa da revisão atual permanece para T1, após as correções.

Escopo: E6-01 e E6-02 do relatório consolidado. Conferidos manifesto e versão: Unity 6000.0.84f1, NGO 2.13.3, Transport 2.7.4. Aplicadas as orientações locais de C#, ScriptableObjects e investigação; exemplos de Unity 6.3 não foram usados para atualizar a engine.

## Código já corrigido

- Monster.cs, linhas 253 e 290: os argumentos finais das duas chamadas InVision usam local.GetComponent<PlayerInventory>(). A assinatura em BotController.cs:149 espera PlayerInventory; não recebe mais NetworkObject nesses pontos.
- PlayerEquipment.cs: CancelEquipmentServerRpc e ExchangeEquipmentServerRpc possuem o sufixo exigido pelo atributo ServerRpc. Os chamadores do mesmo arquivo usam os nomes novos; a busca em fontes C# de Assets/Tools não encontrou os nomes antigos CancelEquipmentRpc/ExchangeEquipmentRpc.
- Nenhuma migração de rede, renomeação adicional ou reconstrução de prefab foi aplicada.

## Dados dos equipamentos

ItemDefinition.SecondaryPool é AttributeId[]. CharacterData declara AttributeId com tipo subjacente byte. Os 18 assets atuais têm dois bytes serializados (quatro dígitos hexadecimais), em vez dos dois inteiros de quatro bytes da base antiga.

| Modelo | ID | Slot | Atributo principal | Pool serializado |
| --- | ---: | --- | ---: | --- |
| WatchmanHood | 35 | Cabeça | 3 | 0102 |
| GuardianHelm | 36 | Cabeça | 4 | 0002 |
| MarksmanMask | 37 | Cabeça | 1 | 0304 |
| ScoutJerkin | 38 | Torso | 2 | 0103 |
| FighterMail | 39 | Torso | 0 | 0402 |
| GuardianCuirass | 40 | Torso | 4 | 0003 |
| MarksmanGloves | 41 | Mãos | 1 | 0302 |
| FighterGauntlets | 42 | Mãos | 0 | 0204 |
| ExplorerGloves | 43 | Mãos | 3 | 0104 |
| WandererBoots | 44 | Pés | 2 | 0301 |
| DuelistBoots | 45 | Pés | 1 | 0002 |
| GuardianGreaves | 46 | Pés | 4 | 0002 |
| EyeMedallion | 47 | Amuleto | 3 | 0104 |
| BreathTalisman | 48 | Amuleto | 2 | 0004 |
| FighterFang | 49 | Amuleto | 0 | 0203 |
| SteadyHandRing | 50 | Anel | 1 | 0302 |
| StrongPulseRing | 51 | Anel | 0 | 0401 |
| PersistenceRing | 52 | Anel | 4 | 0203 |

Atributos: 0 força, 1 destreza, 2 vigor, 3 percepção, 4 vitalidade; inteligência não é usada nesses pools. Pela leitura dos arquivos, cada pool tem dois secundários distintos, diferentes do principal. IDs 35–52, categoria Equipment e slots seguem os 18 modelos esperados por EquipmentCatalog, com três modelos por slot. Todos têm DisplayName preenchido.

Os assets apontam para o GUID c0da08762aaa49a98c2fd2e36429ab13, correspondente ao meta de ItemDefinition.cs. Não foram regravados assets nem metas.

## Gerador já corrigido

Tools/Items/create_equipment_definitions.py contém os mesmos 18 modelos, IDs derivados de 35+i e slots de i//3+1. SecondaryPool usa struct.pack('<BB', *secondary).hex(), compatível com os dois valores byte declarados no código.

O gerador preserva definições existentes e arquivos meta. Portanto, sua correção evita criar novos pools no formato antigo, mas não conserta automaticamente um asset antigo já existente. Nesta revisão, os 18 arquivos lidos já estão no formato corrigido. **O gerador não foi executado.**

## Entrega e limites

Alterados somente este registro e o acompanhamento do plano. Alterações locais de código e assets já existentes foram preservadas. E6-01/E6-02 ficam como **correções presentes, conferidas estaticamente**, sem alegar que o projeto atual inteiro compila ou carrega nativamente.

T1 deverá conferir compilação, geração dos RPCs e carregamento dos 18 modelos na Unity. Logs/builds antigos não substituem essa validação futura.

Próxima etapa: **C2 — impedir escritas de uma sala sem posse**, limitada ao código atual e sem testes. C2 não foi iniciada nesta entrega.
