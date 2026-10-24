param([string]$UnityRoot='C:/Program Files/Unity/Hub/Editor/6000.0.84f1/Editor')
$ErrorActionPreference='Stop'
$probeRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$probeOutput=Join-Path $probeRoot 'Logs/profile-counter-probe'
New-Item -ItemType Directory -Force -Path $probeOutput | Out-Null
$runtime=Get-ChildItem (Join-Path $UnityRoot 'Data/NetCoreRuntime/shared/Microsoft.NETCore.App') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$arguments=@('/nologo','/target:exe','/langversion:9.0',('/out:"'+(Join-Path $probeOutput 'ProfileCounterProbe.dll')+'"'))
foreach($reference in Get-ChildItem -LiteralPath $runtime.FullName -Filter '*.dll') {
    try { [Reflection.AssemblyName]::GetAssemblyName($reference.FullName) | Out-Null; $arguments+='/r:"'+$reference.FullName+'"' }
    catch [System.BadImageFormatException] { }
}
foreach($source in @('Tools/Audit/ProfileCounterProbe.cs','Assets/Scripts/Camp/PlayerProfile.cs','Assets/Scripts/Camp/ProfileStatistics.cs')) {
    $arguments+='"'+(Join-Path $probeRoot $source)+'"'
}
$response=Join-Path $probeOutput 'compile.rsp'
$arguments | Set-Content -LiteralPath $response -Encoding UTF8
$dotnet=Join-Path $UnityRoot 'Data/NetCoreRuntime/dotnet.exe'
& $dotnet (Join-Path $UnityRoot 'Data/DotNetSdkRoslyn/csc.dll') ('@'+$response)
if($LASTEXITCODE -ne 0) { throw 'Falha ao compilar prova do contador.' }
@{runtimeOptions=@{tfm='net6.0';framework=@{name='Microsoft.NETCore.App';version=$runtime.Name}}} | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $probeOutput 'ProfileCounterProbe.runtimeconfig.json') -Encoding UTF8
& $dotnet (Join-Path $probeOutput 'ProfileCounterProbe.dll') | Tee-Object -FilePath (Join-Path $probeOutput 'results.log')
if($LASTEXITCODE -ne 0) { throw 'Falha na prova do contador.' }
