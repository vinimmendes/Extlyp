param([string]$UnityRoot = 'C:\Program Files\Unity\Hub\Editor\6000.0.84f1\Editor')
$ErrorActionPreference='Stop'
$fishingRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$fishingOutput=Join-Path $fishingRoot 'Logs/fishing-audit'
New-Item -ItemType Directory -Force -Path $fishingOutput | Out-Null
$fishingRuntime=Get-ChildItem (Join-Path $UnityRoot 'Data/NetCoreRuntime/shared/Microsoft.NETCore.App') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$fishingArgs=@('/nologo','/target:exe','/langversion:9.0',('/out:"'+(Join-Path $fishingOutput 'FishingProbe.dll')+'"'))
$fishingArgs+=Get-ChildItem -LiteralPath $fishingRuntime.FullName -Filter '*.dll' | ForEach-Object {
    try { [Reflection.AssemblyName]::GetAssemblyName($_.FullName) | Out-Null; '/r:"'+$_.FullName+'"' }
    catch [System.BadImageFormatException] { }
}
$fishingArgs+=@('Tools/Audit/FishingProbe.cs','Assets/Scripts/Items/FishingGame.cs','Assets/Scripts/UI/FishingPresentation.cs') | ForEach-Object { '"'+(Join-Path $fishingRoot $_)+'"' }
$fishingResponse=Join-Path $fishingOutput 'compile.rsp'
$fishingArgs | Set-Content -LiteralPath $fishingResponse
$fishingDotnet=Join-Path $UnityRoot 'Data/NetCoreRuntime/dotnet.exe'
& $fishingDotnet (Join-Path $UnityRoot 'Data/DotNetSdkRoslyn/csc.dll') ('@'+$fishingResponse)
if($LASTEXITCODE -ne 0) { throw 'Fishing probe compilation failed.' }
@{ runtimeOptions=@{ tfm='net6.0'; framework=@{ name='Microsoft.NETCore.App'; version=$fishingRuntime.Name } } } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $fishingOutput 'FishingProbe.runtimeconfig.json')
& $fishingDotnet (Join-Path $fishingOutput 'FishingProbe.dll') | Tee-Object -FilePath (Join-Path $fishingOutput 'results.txt')
if($LASTEXITCODE -ne 0) { throw 'Fishing probe failed.' }
