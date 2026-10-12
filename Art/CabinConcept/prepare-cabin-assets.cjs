// Copy source art and write import/bounds metadata. No raster content is edited.
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const sharp = require('C:/Users/Vinni/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const root = path.resolve(__dirname, '../..');
const folder = path.join(root, 'Assets/Resources/Cabin');
fs.mkdirSync(folder, {recursive:true});
const sources = {
 Bed: 'Assets/Art/Cabin/bed-topdown-v1.png',
 Stove: 'Assets/Art/Cabin/wood-stove-topdown-v1.png',
 BenchBroken: 'Assets/Art/Cabin/ammo-bench-broken-topdown-v2.png',
 BenchBrokenAngled: 'Assets/Art/Cabin/ammo-bench-broken-high-angle-v3.png',
 BenchImprovised: 'Assets/Art/Cabin/ammo-bench-improvised-topdown-v1.png',
 Player: 'Assets/Art/Player/player-rustic-idle-east-v1.png',
 Firewood: 'Assets/Art/Items/wood-icon-v1.png'
};
for (const [key,source] of Object.entries(sources)) {
 const destination = path.join(folder,key+'.png');
 if (!fs.existsSync(destination)) fs.copyFileSync(path.join(root,source),destination);
}
const entries=[];
(async()=>{
 for (const key of ['CabinBase','Bed','Stove','BenchBroken','BenchBrokenAngled','BenchImprovised','Player','Firewood','LakeLog','Chest']) {
  const file=path.join(folder,key+'.png');
  const {data,info}=await sharp(file).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  let minX=info.width,minY=info.height,maxX=-1,maxY=-1;
  for(let y=0;y<info.height;y++) for(let x=0;x<info.width;x++) {
   if(data[(y*info.width+x)*4+3]>32){minX=Math.min(minX,x);minY=Math.min(minY,y);maxX=Math.max(maxX,x);maxY=Math.max(maxY,y);}
  }
  if(maxX<minX) throw new Error('Empty asset '+key);
  if(key==='CabinBase'){minX=0;minY=0;maxX=info.width-1;maxY=info.height-1;}
  entries.push({key,x:minX/info.width,y:(info.height-1-maxY)/info.height,
   width:(maxX-minX+1)/info.width,height:(maxY-minY+1)/info.height});
  const meta=file+'.meta';
  if(!fs.existsSync(meta)) fs.writeFileSync(meta,`fileFormatVersion: 2\nguid: ${crypto.randomUUID().replaceAll('-','')}\nTextureImporter:\n  serializedVersion: 13\n  mipmaps:\n    enableMipMap: 0\n    sRGBTexture: 1\n  isReadable: 0\n  maxTextureSize: ${key==='CabinBase'?2048:1024}\n  textureSettings:\n    serializedVersion: 2\n    filterMode: 1\n    aniso: 1\n    wrapU: 1\n    wrapV: 1\n    wrapW: 1\n  nPOTScale: 0\n  textureType: 0\n  textureShape: 1\n  alphaUsage: 1\n  alphaIsTransparency: ${key==='CabinBase'?0:1}\n  platformSettings:\n  - serializedVersion: 4\n    buildTarget: DefaultTexturePlatform\n    maxTextureSize: ${key==='CabinBase'?2048:1024}\n    textureFormat: -1\n    textureCompression: 0\n    overridden: 0\n`);
  console.log(key+': bounds saved; original pixels preserved');
 }
 fs.writeFileSync(path.join(folder,'ArtBounds.json'),JSON.stringify({entries},null,2)+'\n');
})().catch(e=>{console.error(e);process.exitCode=1;});
