param([string]$UnityRoot='C:\Program Files\Unity\Hub\Editor\6000.0.84f1\Editor')
$ErrorActionPreference='Stop'
$cookingRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$cookingOutput=Join-Path $cookingRoot 'Logs/cooking-audit'
New-Item -ItemType Directory -Force -Path $cookingOutput | Out-Null
$cookingRuntime=Get-ChildItem (Join-Path $UnityRoot 'Data/NetCoreRuntime/shared/Microsoft.NETCore.App') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$cookingArgs=@('/nologo','/target:exe','/langversion:9.0',('/out:"'+(Join-Path $cookingOutput 'CookingProbe.dll')+'"'))
$cookingArgs+=Get-ChildItem -LiteralPath $cookingRuntime.FullName -Filter '*.dll' | ForEach-Object {
    try { [Reflection.AssemblyName]::GetAssemblyName($_.FullName) | Out-Null; '/r:"'+$_.FullName+'"' }
    catch [System.BadImageFormatException] { }
}
$cookingArgs+=@('Tools/Audit/CookingProbe.cs','Assets/Scripts/Items/FishingGame.cs','Assets/Scripts/Camp/CampActivity.cs','Assets/Scripts/UI/CookingPresentation.cs') | ForEach-Object { '"'+(Join-Path $cookingRoot $_)+'"' }
$cookingResponse=Join-Path $cookingOutput 'compile.rsp'
$cookingArgs | Set-Content -LiteralPath $cookingResponse
$cookingDotnet=Join-Path $UnityRoot 'Data/NetCoreRuntime/dotnet.exe'
& $cookingDotnet (Join-Path $UnityRoot 'Data/DotNetSdkRoslyn/csc.dll') ('@'+$cookingResponse)
if($LASTEXITCODE -ne 0) { throw 'Cooking probe compilation failed.' }
@{ runtimeOptions=@{ tfm='net6.0'; framework=@{ name='Microsoft.NETCore.App'; version=$cookingRuntime.Name } } } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $cookingOutput 'CookingProbe.runtimeconfig.json')
& $cookingDotnet (Join-Path $cookingOutput 'CookingProbe.dll') | Tee-Object -FilePath (Join-Path $cookingOutput 'results.txt')
if($LASTEXITCODE -ne 0) { throw 'Cooking probe failed.' }
