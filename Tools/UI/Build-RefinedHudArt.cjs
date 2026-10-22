/* Deterministic export/packing of editable UI vectors. Does not run Unity. */
const fs=require('fs'),path=require('path');
const sharp=require('C:/Users/Vinni/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const root=path.resolve(__dirname,'../..'), art=path.join(root,'Art/HudSombriaRefinada'), out=path.join(art,'Exports'), src=path.join(art,'Sources'), res=path.join(root,'Assets/Resources/UI/RefinedHud');
for(const p of [out,src,res])fs.mkdirSync(p,{recursive:true});
const paths={
 Heal:'<path fill-rule="evenodd" d="M23 15v-4h18v4h9a5 5 0 0 1 5 5v30a5 5 0 0 1-5 5H14a5 5 0 0 1-5-5V20a5 5 0 0 1 5-5h9zm5 0h8v-1h-8v1zm0 10v8h-8v8h8v8h8v-8h8v-8h-8v-8h-8z"/>',
 Swap:'<path d="M8 16h34v-7l14 12-14 12v-7H8zM56 48H22v7L8 43l14-12v7h34z"/>',
 Ammo:'<path d="M9 50V22l5-13 5 13v28zM27 50V22l5-13 5 13v28zM45 50V22l5-13 5 13v28z"/><path d="M9 54h10v4H9zM27 54h10v4H27zM45 54h10v4H45z"/>',
 Backpack:'<path fill-rule="evenodd" d="M23 14v-4a4 4 0 0 1 4-4h10a4 4 0 0 1 4 4v4h2a10 10 0 0 1 10 10v26a8 8 0 0 1-8 8H19a8 8 0 0 1-8-8V24a10 10 0 0 1 10-10h2zm5 0h8v-3h-8v3zM19 28h26v-5H19v5zm0 8v15h26V36H19zm5 5h16v5H24v-5z"/>',
 Sprint:'<circle cx="39" cy="11" r="6"/><path d="m28 20 9-3 9 11 10 1-1 6-14-2-7-8-5 13 13 7 3 15h-7l-4-11-14-7-3 10-11 7-4-6 9-7 5-19-5 1-5 10-6-3 7-15z"/>',
 Menu:'<path fill-rule="evenodd" d="m27 5 10 0 2 8 6 3 8-2 5 9-6 6v6l6 6-5 9-8-2-6 3-2 8H27l-2-8-6-3-8 2-5-9 6-6v-6l-6-6 5-9 8 2 6-3 2-8zm5 14a13 13 0 1 0 0 26 13 13 0 0 0 0-26zm0 7a6 6 0 1 1 0 12 6 6 0 0 1 0-12z"/>',
 Aim:'<path fill-rule="evenodd" d="M29 4h6v7a22 22 0 0 1 18 18h7v6h-7a22 22 0 0 1-18 18v7h-6v-7a22 22 0 0 1-18-18H4v-6h7a22 22 0 0 1 18-18V4zm6 13v7h-6v-7a16 16 0 0 0-12 12h7v6h-7a16 16 0 0 0 12 12v-7h6v7a16 16 0 0 0 12-12h-7v-6h7a16 16 0 0 0-12-12z"/><circle cx="32" cy="32" r="3"/>',
 Bullet:'<g transform="rotate(42 32 32)"><path d="M25 20C25 12 28 6 32 3c4 3 7 9 7 17v21H25V20zM25 44h14v6H25zM23 53h18v5H23z"/></g>',
 Arrow:'<path d="M32 4 54 53 32 43 10 53z"/>',
 Pistol:'<path d="M7 20h44v-4h5v14H29l-3 5h-7l7 21H12L5 31H2V20z"/><path fill-rule="evenodd" d="M23 29h15v13H25zm5 4v5h5v-5z"/>',
 Rifle:'<path d="m4 40 13-9 13-1 4-5h23v4H39l-4 6-15 3-9 9H3zM26 23h13v3H26zM32 19h4v11h-4z"/>',
 Shotgun:'<path d="m3 39 12-10h19l3-6h23v5H39v3h21v5H37l-4 4H20L8 48 3 46z"/><path d="M21 38h9v7h-5v-4h-4z"/>',
 Sword:'<path d="m8 45 28-28 20-9-9 20-28 28-5-5zM3 43l5-5 21 21-5 5z"/>',
 Axe:'<path d="m8 54 31-35 5 5L14 60zM35 10l11-7 15 15-7 13-17-3-8-10z"/>',
 Spear:'<path d="m6 54 31-31 4 4L10 58zM33 24 44 10l15-6-6 15-14 11z"/>',
 Chest:'<path fill-rule="evenodd" d="M9 17h46v37H9V17zm6 6v8h34v-8H15zm0 14v11h34V37H15zM28 27h8v16h-8z"/>',
 Exit:'<path fill-rule="evenodd" d="M12 7h32v50H12V7zm7 7v36h18V14H19z"/><path d="M31 28h17v-7l12 11-12 11v-7H31z"/>',
 Food:'<path d="M9 30h46l-7 22H16zM12 24h40v5H12zM20 6h5v14h-5zM31 2h5v18h-5zM42 6h5v14h-5z"/>',
 Spores:'<path d="M7 33a25 25 0 0 1 50 0zM26 38h12l5 19H21z"/>',
 Wind:'<path d="M5 19h37v6H5zM12 31h44v6H12zM5 43h30v6H5z"/>'
};
const svg=(body,name)=>{const box=name==='Rifle'?[16,36]:name==='Shotgun'?[18,34]:name==='Pistol'?[12,48]:[0,64];return `<svg xmlns="http://www.w3.org/2000/svg" width="128" height="${box[1]*2}" viewBox="0 ${box[0]} 64 ${box[1]}"><g fill="white">${body}</g></svg>`;};
const panel=(simple=false)=>`<svg xmlns="http://www.w3.org/2000/svg" width="128" height="96"><defs><linearGradient id="p" x2="0" y2="1"><stop stop-color="${simple?'#25372e':'#2b2c20'}"/><stop offset="1" stop-color="${simple?'#14251d':'#11170f'}"/></linearGradient></defs><rect x="1.5" y="1.5" width="125" height="93" rx="8" fill="url(#p)" stroke="${simple?'#90ac98':'#857352'}" stroke-width="2"/><rect x="4" y="4" width="120" height="88" rx="6" fill="none" stroke="#ffffff" opacity=".07"/></svg>`;
const circle=(fill,edge)=>`<svg xmlns="http://www.w3.org/2000/svg" width="192" height="192"><circle cx="96" cy="96" r="93" fill="${fill}" stroke="${edge}" stroke-width="4"/><circle cx="96" cy="96" r="88" fill="none" stroke="white" stroke-width="1" opacity=".08"/></svg>`;
async function main(){
 for(const [name,body] of Object.entries(paths)) {const s=svg(body,name);fs.writeFileSync(path.join(src,name+'.svg'),s); await sharp(Buffer.from(s)).png().toFile(path.join(out,name+'.png'));}
 for(const [name,s] of Object.entries({Panel:panel(),SimplePanel:panel(true),SimpleButton:circle('#1b2e24','#8baf99'),SimpleFire:circle('#263e2c','#afd0a2'),Move:circle('#181e16b0','#8c8163'),Knob:circle('#8e8866','#c8bc94')})) {fs.writeFileSync(path.join(src,name+'.svg'),s);await sharp(Buffer.from(s)).png().toFile(path.join(out,name+'.png'));}
 const manifest={Version:1,Slots:[],Themes:[{Somber:true,Button:'Button',Fire:'Fire',Panel:'Panel'},{Somber:false,Button:'SimpleButton',Fire:'SimpleFire',Panel:'SimplePanel'}]};
 for(const [atlas,names] of [[0,['Button','Fire','SimpleButton','SimpleFire','Panel','SimplePanel','Move','Knob']],[1,Object.keys(paths)]]){
  let x=8,y=8,row=0;const layers=[];
  for(const name of names){const file=path.join(out,name+'.png');const s=atlas===0?(name==='Fire'?256:name.includes('Panel')?128:192):128;const meta=await sharp(file).metadata();const h=atlas===1?meta.height:name.includes('Panel')?96:s;const buffer=await sharp(file).resize(s,h,{fit:'fill'}).png().toBuffer();if(x+s+8>1024){x=8;y+=row+16;row=0;}if(y+h+8>1024)throw Error('Atlas budget exceeded');layers.push({input:buffer,left:x,top:y});manifest.Slots.push({Name:name,Atlas:atlas,X:x,Y:1024-y-h,Width:s,Height:h,Border:name.includes('Panel')?12:0});x+=s+16;row=Math.max(row,h);}
  await sharp({create:{width:1024,height:1024,channels:4,background:'#00000000'}}).composite(layers).png().toFile(path.join(res,'Atlas'+atlas+'.png'));
 }
 fs.writeFileSync(path.join(res,'Visuals.json'),JSON.stringify(manifest,null,2));fs.writeFileSync(path.join(art,'manifest.json'),JSON.stringify(manifest,null,2));
 console.log('Exported '+Object.keys(paths).length+' symbols, layered surfaces and two 1024 atlases.');
}
main().catch(e=>{console.error(e);process.exitCode=1;});
