"""Local, reviewable catalog of finished Blender animation assets."""
from pathlib import Path
import json,html
BASE=Path(__file__).resolve().parent
models={'Chest':'Baú de viagem surrado','Mimic':'Imitador · Mordida rasteira','Charger':'Charger · Ombros ossudos','Listener':'Ouvinte · Escutador','Shell':'Carapaça · Enrolado','Herald':'Arauto · Costelas ressonantes','Devourer':'Devorador · Colhedor','Spore':'Esporífero · Bolsas dorsais','Mother':'Planta-mãe · Nó de raízes','AttackBulb':'Bulbo atacante · Nó de raízes','HealingBulb':'Bulbo curador · Nó de raízes','Minion':'Lacaio · Nó de raízes'}
preferred={'Chest':'Open','Mimic':'Reveal','Charger':'Charge','Listener':'Listen','Shell':'Ram','Herald':'Shout','Devourer':'Feed','Spore':'Spores','Mother':'Spawn','AttackBulb':'Mature','HealingBulb':'Heal','Minion':'Walk'}
actions={'Open':'Abrir','Close':'Fechar','Disguise':'Disfarce fechado','Reveal':'Revelação','Bite':'Mordida','Walk':'Caminhada','Charge':'Investida','Listen':'Escutar','Ram':'Ataque frontal','Shout':'Grito','Feed':'Alimentação','Growth':'Crescimento','Spores':'Liberar esporos','Attack':'Ataque','Spawn':'Gerar bulbo','Breathe':'Respiração','Mature':'Amadurecimento','Heal':'Cura'}
cards=[]; markdown=['# Animações Blender — catálogo','', 'Modelos originais simplificados a partir dos conceitos escolhidos. Prévias vêm de renders Blender; integração destes novos modelos ao jogo ainda não realizada.','']
for name,title in models.items():
    folder=BASE/name
    ready=(folder/'clips.json').exists() and not (folder/'.rendering').exists() and (folder/(preferred[name]+'.gif')).exists()
    if not ready:
        cards.append('<section><h2>'+html.escape(title)+'</h2><p>Em preparação</p></section>'); continue
    data=json.loads((folder/'clips.json').read_text(encoding='utf-8')); options=[]; links=[]
    for clip in data['clips']:
        label=clip['name']; file=name+'/'+label+'.gif'
        options.append('<option value="'+file+'"'+(' selected' if label==preferred[name] else '')+'>'+actions.get(label,label)+'</option>')
        links.append('['+actions.get(label,label)+']('+file+')')
    cards.append('<section><h2>'+html.escape(title)+'</h2><img loading="lazy" src="'+name+'/'+preferred[name]+'.gif" alt="'+html.escape(title)+'"><select aria-label="Animação de '+html.escape(title)+'" onchange="this.previousElementSibling.src=this.value">'+''.join(options)+'</select><a href="'+name+'/'+name+'.blend">Modelo Blender</a><a href="'+name+'/contact.png">Comparar poses</a></section>')
    markdown+=['## '+title,'','[Modelo editável]('+name+'/'+name+'.blend) · '+' · '.join(links),'']
body='<!doctype html><html lang="pt-BR"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Animações Blender</title><style>body{background:#171a17;color:#e4e0d1;font:16px Segoe UI,sans-serif;margin:24px}h1{font-size:28px}p{max-width:900px;line-height:1.5}main{display:grid;grid-template-columns:repeat(auto-fit,minmax(310px,1fr));gap:20px}section{background:#252923;border:1px solid #48483c;border-radius:8px;padding:16px}h2{font-size:19px;margin:0 0 12px}img{width:100%;display:block;border-radius:5px}select{display:block;width:100%;padding:10px;background:#363c32;color:#e4e0d1;border:1px solid #666;margin:12px 0}a{color:#d9c183;display:inline-block;margin-right:14px}</style><h1>Modelos e animações Blender</h1><p>Escolha o clipe em cada cartão. Modelos editáveis, com materiais procedurais e controles de animação. As formas aproximam as referências aprovadas; a arte foi simplificada para a modelagem. Prévias superiores e de perspectiva disponíveis nos arquivos de poses.</p><main>'+''.join(cards)+'</main></html>'
(BASE/'index.html').write_text(body,encoding='utf-8'); (BASE/'CATALOGO.md').write_text('\n'.join(markdown),encoding='utf-8')
print('Local catalog updated.')
