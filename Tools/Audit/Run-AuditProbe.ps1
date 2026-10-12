param([string]$UnityRoot = 'C:\Program Files\Unity\Hub\Editor\6000.0.84f1\Editor')
$ErrorActionPreference = 'Stop'
$auditProject = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$auditOutput = Join-Path $auditProject 'Logs/skills-audit'
New-Item -ItemType Directory -Force -Path $auditOutput | Out-Null
$auditDotnet = Join-Path $UnityRoot 'Data/NetCoreRuntime/dotnet.exe'
$auditCompiler = Join-Path $UnityRoot 'Data/DotNetSdkRoslyn/csc.dll'
$auditRuntime = Get-ChildItem (Join-Path $UnityRoot 'Data/NetCoreRuntime/shared/Microsoft.NETCore.App') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$auditArgs = @('/nologo', '/target:exe', '/langversion:9.0', ('/out:' + (Join-Path $auditOutput 'AuditProbe.dll')))
$auditArgs += Get-ChildItem -LiteralPath $auditRuntime.FullName -Filter '*.dll' | ForEach-Object {
    try { [Reflection.AssemblyName]::GetAssemblyName($_.FullName) | Out-Null; '/r:' + $_.FullName }
    catch [System.BadImageFormatException] { } # Bibliotecas nativas nao sao referencias C#.
}
$auditArgs += @('Tools/Audit/AuditProbe.cs', 'Assets/Scripts/Items/InventoryGrid.cs', 'Assets/Scripts/Items/ItemCatalog.cs', 'Assets/Scripts/Items/WeaponCatalog.cs', 'Assets/Scripts/Camp/CampModels.cs', 'Assets/Scripts/Bots/BotDifficulty.cs', 'Assets/Scripts/Core/GameConfig.cs', 'Assets/Scripts/Match/MatchQueue.cs') | ForEach-Object { Join-Path $auditProject $_ }
& $auditDotnet $auditCompiler @auditArgs
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar as provas da auditoria.' }
@{ runtimeOptions = @{ tfm = 'net6.0'; framework = @{ name = 'Microsoft.NETCore.App'; version = $auditRuntime.Name } } } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $auditOutput 'AuditProbe.runtimeconfig.json') -Encoding UTF8
& $auditDotnet (Join-Path $auditOutput 'AuditProbe.dll') | Tee-Object -FilePath (Join-Path $auditOutput 'rules-evidence.json')
if ($LASTEXITCODE -ne 0) { throw 'Resultado inesperado nas provas da auditoria.' }
