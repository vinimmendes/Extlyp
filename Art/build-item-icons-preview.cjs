// Build a labeled review board without changing the generated PNG assets.
const path = require('path');
const fs = require('fs');
const sharp = require('C:/Users/Vinni/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const root = path.resolve(__dirname, '..');
const items = [
 ['wood','Madeira'],['nails','Pregos'],['bandage','Bandagem'],
 ['metal','Metal'],['gunpowder','Pólvora'],['ammo-component','Componentes'],
 ['pistol','Pistola'],['pistol-ammo','Munição de pistola'],
 ['fish-common','Peixe comum'],['fish-rare','Peixe raro'],['fish-large','Peixe grande'],
 ['cooked-fish','Peixe assado'],['burnt-fish','Peixe queimado'],['raw-fish','Peixe cru'],
 ['bolt-rifle','Rifle de ferrolho'],['rifle-ammo','Munição de rifle'],
 ['double-barrel-shotgun','Espingarda'],['shotgun-shell','Cartucho de espingarda']
];
(async () => {
 const tileW = 210, tileH = 220, columns = 6;
 const overlays = [];
 for (let i=0; i<items.length; i++) {
  const [slug,name] = items[i];
  const file = path.join(root, 'Assets/Art/Items', `${slug}-icon-v1.png`);
  const meta = await sharp(file).metadata();
  if (!meta.hasAlpha) throw new Error(`Missing alpha: ${slug}`);
  const icon = await sharp(file).resize(185,185,{fit:'contain',background:{r:0,g:0,b:0,alpha:0}}).png().toBuffer();
  const x = (i%columns)*tileW, y = Math.floor(i/columns)*tileH;
  overlays.push({input:icon,left:x+12,top:y+5});
  const label = Buffer.from(`<svg width="210" height="30"><text x="105" y="21" text-anchor="middle" font-family="Arial" font-size="15" fill="#e9e2d2">${name}</text></svg>`);
  overlays.push({input:label,left:x,top:y+187});
  console.log(`${slug}: ${meta.width}x${meta.height}, alpha`);
 }
 const outDir = path.join(root,'Art/ItemIcons');
 fs.mkdirSync(outDir,{recursive:true});
 await sharp({create:{width:columns*tileW,height:3*tileH,channels:4,background:'#262723'}}).composite(overlays).png().toFile(path.join(outDir,'items-preview-v1.png'));
 console.log('Review board saved: Art/ItemIcons/items-preview-v1.png');
})().catch(error => { console.error(error); process.exitCode=1; });
