param([string]$UnityRoot='C:/Program Files/Unity/Hub/Editor/6000.0.84f1/Editor')
$ErrorActionPreference='Stop'
$identityRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$identityRuntime=Join-Path $UnityRoot 'Data/NetCoreRuntime/shared/Microsoft.NETCore.App/6.0.21'
$identityDotnet=Join-Path $UnityRoot 'Data/NetCoreRuntime/dotnet.exe'
$identityCompiler=Join-Path $UnityRoot 'Data/DotNetSdkRoslyn/csc.dll'
$identityOutput=Join-Path $identityRoot ('Logs/identity-device-probe/'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $identityOutput -Force | Out-Null
# Baseline que ainda armazena a identidade em PlayerPrefs, antes desta correcao.
git -C $identityRoot show 2d3a57c:Assets/Scripts/Camp/CampIdentity.cs | Set-Content -LiteralPath (Join-Path $identityOutput 'CampIdentity-before.cs') -Encoding UTF8
if($LASTEXITCODE -ne 0) { throw 'Baseline de identidade indisponivel.' }
foreach($identityVariant in @('before','after')) {
    $identitySource=if($identityVariant -eq 'before') { Join-Path $identityOutput 'CampIdentity-before.cs' } else { Join-Path $identityRoot 'Assets/Scripts/Camp/CampIdentity.cs' }
    $identityDll=Join-Path $identityOutput ($identityVariant+'.dll')
    $identityArgs=@('/nologo','/target:exe','/langversion:9.0',('/out:"'+$identityDll+'"'))
    foreach($identityRef in Get-ChildItem -LiteralPath $identityRuntime -Filter '*.dll') {
        try { [Reflection.AssemblyName]::GetAssemblyName($identityRef.FullName) | Out-Null; $identityArgs+='/r:"'+$identityRef.FullName+'"' }
        catch [System.BadImageFormatException] { }
    }
    $identityArgs+='"'+$identitySource+'"'
    $identityArgs+='"'+(Join-Path $PSScriptRoot 'CampIdentityDeviceProbe.cs')+'"'
    $identityRsp=Join-Path $identityOutput ($identityVariant+'.rsp')
    $identityArgs | Set-Content -LiteralPath $identityRsp -Encoding UTF8
    & $identityDotnet $identityCompiler ('@'+$identityRsp)
    if($LASTEXITCODE -ne 0) { throw ('Falha ao compilar prova '+$identityVariant) }
    @{runtimeOptions=@{tfm='net6.0';framework=@{name='Microsoft.NETCore.App';version='6.0.21'}}} | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $identityOutput ($identityVariant+'.runtimeconfig.json')) -Encoding UTF8
    & $identityDotnet $identityDll (Join-Path $identityOutput $identityVariant) | Tee-Object -FilePath (Join-Path $identityOutput ($identityVariant+'-results.log'))
    $identityExit=$LASTEXITCODE
    if($identityVariant -eq 'before' -and $identityExit -eq 0) { throw 'A falha esperada nao foi reproduzida na versao anterior.' }
    if($identityVariant -eq 'after' -and $identityExit -ne 0) { throw 'A prova da correcao falhou.' }
}
Write-Output ('Prova focada concluida: '+$identityOutput)
