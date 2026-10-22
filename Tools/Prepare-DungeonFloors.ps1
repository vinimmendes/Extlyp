param([Parameter(Mandatory=$true)][string]$Manifest)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$sourceRoot=Join-Path $projectRoot 'Art/Catacumbas/Pisos/Fontes'
$runtimeRoot=Join-Path $projectRoot 'Assets/Resources/Environment/Dungeon/Floors'
$assets=Get-Content -LiteralPath $Manifest -Raw | ConvertFrom-Json
New-Item -ItemType Directory -Force -Path $sourceRoot,$runtimeRoot | Out-Null
foreach($folder in @((Split-Path $runtimeRoot -Parent),$runtimeRoot)) {
    $folderMeta=$folder+'.meta'
    if(-not (Test-Path -LiteralPath $folderMeta)) {
        $folderGuid=[guid]::NewGuid().ToString('N')
        "fileFormatVersion: 2`nguid: $folderGuid`nfolderAsset: yes`nDefaultImporter:`n  externalObjects: {}`n  userData: `n  assetBundleName: `n  assetBundleVariant: " | Set-Content -LiteralPath $folderMeta -Encoding utf8
    }
}
foreach($asset in $assets) {
    if($asset.id -notmatch '^CAT-F(0[1-9]|10)_v[0-9]{2}$') { throw "ID invalido: $($asset.id)" }
    $source=if([System.IO.Path]::IsPathRooted($asset.source)) {$asset.source} else {Join-Path $projectRoot $asset.source}
    if(-not (Test-Path -LiteralPath $source)) { throw "Fonte ausente: $source" }
    foreach($folder in @($sourceRoot,$runtimeRoot)) {
        $destination=Join-Path $folder ($asset.id+'.png')
        if(Test-Path -LiteralPath $destination) {
            if((Get-FileHash -LiteralPath $destination).Hash -ne (Get-FileHash -LiteralPath $source).Hash) { throw "Preservando arquivo existente: $destination" }
        } else { Copy-Item -LiteralPath $source -Destination $destination }
    }
    $family=[int]$asset.id.Substring(5,2)
    $maxSize=if($family -eq 7 -or $family -ge 9) {256} else {128}
    $wrap=if($family -le 5 -or $family -eq 7) {0} else {1}
    $meta=Join-Path $runtimeRoot ($asset.id+'.png.meta')
    if(-not (Test-Path -LiteralPath $meta)) {
        $guid=[guid]::NewGuid().ToString('N')
        @"
fileFormatVersion: 2
guid: $guid
TextureImporter:
  serializedVersion: 13
  mipmaps:
    enableMipMap: 0
    sRGBTexture: 1
  isReadable: 0
  maxTextureSize: $maxSize
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    wrapU: $wrap
    wrapV: $wrap
    wrapW: $wrap
  nPOTScale: 0
  textureType: 0
  textureShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: $maxSize
    textureFormat: -1
    textureCompression: 0
    overridden: 0
"@ | Set-Content -LiteralPath $meta -Encoding utf8
    }
}
$cards=foreach($asset in $assets) {
    $id=$asset.id
    $family=[int]$id.Substring(5,2)
    $previewSize=if($family -eq 7) {'256px 256px'} elseif($family -ge 9) {'256px 128px'} else {'128px 128px'}
    $prompt=[System.Net.WebUtility]::HtmlEncode($asset.prompt)
    "<article><h2>$id</h2><div class='sample' style='background-image:url(Fontes/$id.png);background-size:$previewSize'></div><details><summary>Prompt</summary><p>$prompt</p></details></article>"
}
@"
<!doctype html><html lang="pt-BR"><meta charset="utf-8"><title>Catacumbas — pisos e água</title>
<style>body{margin:32px;background:#171a1c;color:#d6cdbb;font:16px system-ui}main{display:grid;grid-template-columns:repeat(auto-fit,minmax(260px,1fr));gap:24px}article{padding:16px;background:#292d2e;border-radius:8px}h2{font-size:16px}.sample{height:256px;background-size:128px 128px;background-repeat:repeat;background-color:#555b58}details{margin-top:12px}p{line-height:1.5}</style>
<h1>Catacumbas — pisos e água</h1><p>40 variantes CAT-F01 a CAT-F10. Repetição de amostras de arte; não é captura da Unity. Decalques são exibidos sobre fundo cinza. Fontes preservadas; o jogo ajusta a pegada conforme o catálogo.</p>
<main>$($cards -join "`n")</main></html>
"@ | Set-Content -LiteralPath (Join-Path (Split-Path $sourceRoot -Parent) 'index.html') -Encoding utf8
Write-Output "Assets preparados: $($assets.Count). Fontes preservadas; importacao reduzida explicitamente a 128 px por celula."
