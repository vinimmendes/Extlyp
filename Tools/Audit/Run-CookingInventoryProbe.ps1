param([switch]$ExpectBug,[string]$UnityRoot='C:\Program Files\Unity\Hub\Editor\6000.0.84f1\Editor')
$ErrorActionPreference='Stop'
$cookInventoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$cookInventoryOutput=Join-Path $cookInventoryRoot 'Logs/cooking-inventory-audit'
New-Item -ItemType Directory -Force -Path $cookInventoryOutput | Out-Null
$cookInventoryRuntime=Get-ChildItem (Join-Path $UnityRoot 'Data/NetCoreRuntime/shared/Microsoft.NETCore.App') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$cookInventoryArgs=@('/nologo','/target:exe','/langversion:9.0',('/out:"'+(Join-Path $cookInventoryOutput 'CookingInventoryProbe.dll')+'"'))
$cookInventoryArgs+=Get-ChildItem -LiteralPath $cookInventoryRuntime.FullName -Filter '*.dll' | ForEach-Object {
    try { [Reflection.AssemblyName]::GetAssemblyName($_.FullName) | Out-Null; '/r:"'+$_.FullName+'"' }
    catch [System.BadImageFormatException] { }
}
$cookInventoryArgs+=@('Tools/Audit/CookingInventoryProbe.cs','Assets/Scripts/Items/InventoryGrid.cs','Assets/Scripts/Items/ItemCatalog.cs','Assets/Scripts/Items/FishingGame.cs','Assets/Scripts/Camp/CampModels.cs','Assets/Scripts/Camp/CampActivity.cs','Assets/Scripts/Camp/CampTestResources.cs') | ForEach-Object { '"'+(Join-Path $cookInventoryRoot $_)+'"' }
$cookInventoryResponse=Join-Path $cookInventoryOutput 'compile.rsp'
$cookInventoryArgs | Set-Content -LiteralPath $cookInventoryResponse
$cookInventoryDotnet=Join-Path $UnityRoot 'Data/NetCoreRuntime/dotnet.exe'
& $cookInventoryDotnet (Join-Path $UnityRoot 'Data/DotNetSdkRoslyn/csc.dll') ('@'+$cookInventoryResponse)
if($LASTEXITCODE -ne 0) { throw 'Cooking inventory compilation failed.' }
@{ runtimeOptions=@{ tfm='net6.0'; framework=@{ name='Microsoft.NETCore.App'; version=$cookInventoryRuntime.Name } } } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $cookInventoryOutput 'CookingInventoryProbe.runtimeconfig.json')
$cookInventoryRunArgs=@((Join-Path $cookInventoryOutput 'CookingInventoryProbe.dll'))
if($ExpectBug) { $cookInventoryRunArgs+='--expect-bug' }
$cookInventoryEvidence=if($ExpectBug) { 'before.txt' } else { 'after.txt' }
& $cookInventoryDotnet @cookInventoryRunArgs | Tee-Object -FilePath (Join-Path $cookInventoryOutput $cookInventoryEvidence)
if($LASTEXITCODE -ne 0) { throw 'Cooking inventory probe failed.' }
