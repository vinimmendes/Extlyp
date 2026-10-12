// Asset preparation only: preserve existing settings, GUIDs and source screenshot.
const fs=require('fs'),path=require('path'),crypto=require('crypto');
const root=path.resolve(__dirname,'..');
const guid=()=>crypto.randomUUID().replaceAll('-','');
const script=path.join(root,'Assets/Scripts/Core/ServerConnectionSettings.cs');
if(!fs.existsSync(script+'.meta')) fs.writeFileSync(script+'.meta',`fileFormatVersion: 2\nguid: ${guid()}\nMonoImporter:\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n`);
const scriptGuid=fs.readFileSync(script+'.meta','utf8').match(/^guid: (\w+)/m)[1];
const settings=path.join(root,'Assets/Resources/ServerConnectionSettings.asset');
if(!fs.existsSync(settings)) fs.writeFileSync(settings,`%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: ${scriptGuid}, type: 3}\n  m_Name: ServerConnectionSettings\n  m_EditorClassIdentifier: \n  address: 127.0.0.1\n  basePort: 7777\n`);
if(!fs.existsSync(settings+'.meta')) fs.writeFileSync(settings+'.meta',`fileFormatVersion: 2\nguid: ${guid()}\nNativeFormatImporter:\n  mainObjectFileID: 11400000\n`);
const image=path.join(root,'Assets/Resources/UI/FlorestaVelha.png');
fs.mkdirSync(path.dirname(image),{recursive:true});
const folderMeta=path.dirname(image)+'.meta';
if(!fs.existsSync(folderMeta)) fs.writeFileSync(folderMeta,`fileFormatVersion: 2\nguid: ${guid()}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);
if(!fs.existsSync(image)) fs.copyFileSync(path.join(root,'Docs/Auditoria/Evidencias/MapaLaboratorio/floresta.png'),image);
if(!fs.existsSync(image+'.meta')) fs.writeFileSync(image+'.meta',`fileFormatVersion: 2\nguid: ${guid()}\nTextureImporter:\n  serializedVersion: 13\n  mipmaps:\n    enableMipMap: 0\n    sRGBTexture: 1\n  isReadable: 0\n  textureType: 0\n  textureShape: 1\n  nPOTScale: 0\n  maxTextureSize: 1024\n  textureSettings:\n    serializedVersion: 2\n    filterMode: 1\n    wrapU: 1\n    wrapV: 1\n    wrapW: 1\n  platformSettings:\n  - serializedVersion: 4\n    buildTarget: DefaultTexturePlatform\n    maxTextureSize: 1024\n    textureFormat: -1\n    textureCompression: 0\n    overridden: 0\n`);
for(const name of ['ClientEnvironment','ExpeditionMenu']) {
 const file=path.join(root,'Assets/Scripts/UI',name+'.cs');
 if(!fs.existsSync(file+'.meta')) fs.writeFileSync(file+'.meta',`fileFormatVersion: 2\nguid: ${guid()}\nMonoImporter:\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n`);
}
console.log('Expedition menu assets prepared; existing configuration and originals preserved.');
