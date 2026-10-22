// Additional design states. This document is independent of Unity runtime.
const hudExtraStates={rifle:'Mosin',shotgun:'Escopeta',sword:'Espada',axe:'Machado',spear:'Lança',empty:'Sem munição',statuses:'Estados ativos',map:'Mapa ampliado',bright:'Cenário claro',dark:'Cenário escuro'};
for(const [value,label] of Object.entries(hudExtraStates)){const o=document.createElement('option');o.value=value;o.textContent=label;document.getElementById('state').appendChild(o);}
function finishHudPreview(){
 const stage=document.getElementById('stage'),state=document.getElementById('state').value;
 const selectImg=(suffix)=>[...stage.querySelectorAll('img')].find(i=>i.src.endsWith('/'+suffix+'.png'));
 const types={melee:'Sword',rifle:'Rifle',shotgun:'Shotgun',sword:'Sword',axe:'Axe',spear:'Spear'};
 const type=types[state],weapon=selectImg('Pistol')||selectImg('Axe');
 if(type&&weapon){weapon.src='assets/'+type+'.png';const melee=['Sword','Axe','Spear'].includes(type);const values=weapon.parentElement.querySelectorAll('.label');
 if(melee){values.forEach(e=>e.remove());weapon.style.left='26px';const fire=selectImg('Bullet')||selectImg('Sword');if(fire){fire.src='assets/'+type+'.png';fire.parentElement.querySelector('.label').textContent='ATACAR';}}
 else if(values.length){values[0].textContent=type==='Rifle'?'5':'2';values[1].textContent=type==='Rifle'?'30':'12';}}
 if(state==='empty'&&weapon){weapon.parentElement.querySelector('.label').textContent='0';weapon.parentElement.querySelector('.label').style.color='#ce826d';}
 if(state==='reload'){const ammo=selectImg('Ammo');if(ammo){const p=ammo.parentElement,side=parseFloat(p.style.width);Object.assign(ammo.style,{left:side*.27+'px',top:'7px',width:side*.46+'px',height:side*.46+'px'});const number=document.createElement('span');number.textContent='3 s';Object.assign(number.style,{position:'absolute',left:'0',top:side-21+'px',width:'100%',height:'18px',textAlign:'center',fontSize:'14px',fontWeight:'600'});p.appendChild(number);}}
 const inset=document.getElementById('cut').value==='left'?24:0;
 if(state==='statuses')for(const [i,icon,text] of [[0,'Food','Alimentado'],[1,'Spores','Esporos 24%']]){const e=document.createElement('div');e.className='ui panel';Object.assign(e.style,{left:(12+inset)+'px',top:(12+58+4*parseInt(stage.style.height)/600+6+22*i)+'px',width:'96px',height:'20px'});e.innerHTML='<img class="icon" src="assets/'+icon+'.png" style="left:0;top:0;width:14px;height:14px"><span style="position:absolute;left:17px;top:-3px;font-size:14px;white-space:nowrap">'+text+'</span>';stage.appendChild(e);}
 if(state==='bright'||state==='dark'){
 const bg=document.createElement('div');Object.assign(bg.style,{position:'absolute',inset:'0',background:"url('cenario-preview.png') center / cover",filter:'brightness('+(state==='bright'?1.9:.4)+')'});stage.prepend(bg);stage.querySelectorAll('.ui').forEach(e=>e.style.zIndex='1');}
 if(state==='map'){
 const W=parseInt(stage.style.width),H=parseInt(stage.style.height);const small=H<360||W<640,mw=small?88:W>=800&&H>=480?112:96;
 const map=stage.querySelector('.map'),svg=map.querySelector('svg').outerHTML;let h=H-24-168,w=Math.max(176,h+56);w=Math.min(w,2*(W-12-mw-8-48-8-W/2));h=Math.min(h,w-56);
 for(const e of [...stage.querySelectorAll('.ui')])if(!(e.classList.contains('panel')&&parseFloat(e.style.left)<160)||parseFloat(e.style.top)>80)e.remove();
 const full=document.createElement('div');full.className='ui panel';Object.assign(full.style,{left:(W-w)/2+'px',top:'12px',width:w+'px',height:(h+168)+'px'});
 full.innerHTML='<div style="text-align:center;font-size:14px;height:28px">N · CATACUMBAS DOS AFOGADOS</div><div style="height:'+(h+48)+'px;position:relative;margin:0 24px">'+svg.replace(/width="\d+"/,'width="'+(w-56)+'"').replace(/height="\d+"/,'height="'+h+'"')+'</div><div style="text-align:center;font-size:14px;line-height:14px;height:32px">Saídas · deslize para escolher<br>+ Pronta　× Fechada　? Incerta</div><div style="display:flex;gap:8px;overflow:hidden;height:48px"><div class="panel" style="flex:0 0 120px;display:flex;align-items:center;justify-content:center;font-size:14px;text-align:center">[1] Escadaria do Cortejo +</div><div class="panel" style="flex:0 0 120px;display:flex;align-items:center;justify-content:center;font-size:14px;text-align:center">2 · Poço dos Coveiros ?</div><div class="panel" style="flex:0 0 120px;font-size:14px">3 · Canal dos Defuntos ?</div></div>';
 stage.appendChild(full);
 // The gear stays a separate 48dp target beside the expanded map.
 const gear=document.createElement('div');gear.className='ui';Object.assign(gear.style,{left:(W-12-mw-8-48)+'px',top:'12px',width:'48px',height:'48px'});gear.innerHTML='<img class="plate" src="assets/Button.png"><img class="icon" src="assets/Menu.png" style="left:11px;top:11px;width:26px;height:26px">';stage.appendChild(gear);
 }
}
const originalHudRender=window.renderHud;
window.renderHud=(size,theme,state='normal',cut='none')=>{originalHudRender(size,theme,state,cut);finishHudPreview();};
for(const id of ['size','theme','state','cut']){const e=document.getElementById(id),previous=e.onchange;e.onchange=()=>{previous();finishHudPreview();};}
