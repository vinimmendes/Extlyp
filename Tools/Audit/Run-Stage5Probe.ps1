param(
    [string]$SourceRoot = (Join-Path $PSScriptRoot '../../Builds/Auditoria/2026-10-16_083542_382c7a/Projeto'),
    [string]$UnityRoot = 'C:/Program Files/Unity/Hub/Editor/6000.0.84f1/Editor',
    [string]$Filter = ''
)
$ErrorActionPreference = 'Stop'
$stageRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$stageSource = [IO.Path]::GetFullPath($SourceRoot)
$stageRun = [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0,6)
$stageOutput = Join-Path $stageRoot ('Logs/auditoria/2026-10-16_083542_382c7a/etapa5/' + $stageRun)
New-Item -ItemType Directory -Path $stageOutput | Out-Null
$stageDriver = Join-Path $stageOutput 'driver'
New-Item -ItemType Directory -Path $stageDriver | Out-Null
foreach ($stageTool in @('Stage5Probe.cs','Stage5Adapters.cs','Prepare-Stage5Fixtures.py','Run-Stage5Probe.ps1')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $stageTool) -Destination (Join-Path $stageDriver $stageTool)
}
$stagePython = 'C:/Users/Vinni/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
& $stagePython (Join-Path $stageDriver 'Prepare-Stage5Fixtures.py') $stageSource (Join-Path $stageOutput 'fixtures.json')
if ($LASTEXITCODE -ne 0) { throw 'Falha na extracao explicita de fixtures.' }
$stageNames = @('Core/GameConfig','Core/GameplayDefinition','Core/CharacterData','Core/CharacterAttributeRules','Core/CharacterProgressionRules','Items/InventoryGrid','Items/ItemCatalog','Items/WeaponCatalog','Items/EquipmentData','Items/EquipmentLoadout','Items/ItemDefinition','Items/EquipmentCatalog','Items/TreasureCatalog','Items/RecoveryRules','Items/LootTableDefinition','Items/LootTables','Items/LootRoller','Items/FishingGame','Camp/CampModels','Camp/CampTrade','Camp/CampProfileMigration','Camp/CampProgress','Camp/CampExperience','Camp/CampIdentity','Camp/SharedCampMemory','Camp/CampActivity','Bots/BotDifficulty','Match/MatchQueue')
$stageSources = @($stageNames | ForEach-Object { Join-Path $stageSource ('Assets/Scripts/' + $_ + '.cs') })
$stageSources += @((Join-Path $stageDriver 'Stage5Adapters.cs'),(Join-Path $stageDriver 'Stage5Probe.cs'))
$stageHashes = @($stageSources | ForEach-Object { [pscustomobject]@{path=$_;sha256=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant()} })
$stageManifest = (Get-Content (Join-Path $stageRoot 'Logs/auditoria/2026-10-16_083542_382c7a/manifest.json') -Raw | ConvertFrom-Json).files
foreach ($stageFixture in (Get-Content (Join-Path $stageOutput 'fixtures.json') -Raw | ConvertFrom-Json)) {
    $stageExpected = $stageManifest | Where-Object path -eq $stageFixture.source
    if ($stageFixture.sha256 -ne $stageExpected.sha256) { throw ('Asset nao corresponde ao manifesto: ' + $stageFixture.source) }
}
foreach ($stageHash in $stageHashes | Select-Object -First $stageNames.Count) {
    $stageRelative = $stageHash.path.Substring($stageSource.Length + 1).Replace('\','/')
    $stageExpected = $stageManifest | Where-Object path -eq $stageRelative
    if ($stageHash.sha256 -ne $stageExpected.sha256) { throw ('Fonte nao corresponde ao manifesto: ' + $stageRelative) }
}
$stageHashes | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $stageOutput 'source-hashes.json') -Encoding utf8
$stageRuntime = Get-ChildItem (Join-Path $UnityRoot 'Data/NetCoreRuntime/shared/Microsoft.NETCore.App') -Directory | Sort-Object {[version]$_.Name} -Descending | Select-Object -First 1
$stageDotnet = Join-Path $UnityRoot 'Data/NetCoreRuntime/dotnet.exe'
$stageCompiler = Join-Path $UnityRoot 'Data/DotNetSdkRoslyn/csc.dll'
$stageDll = Join-Path $stageOutput 'Stage5Probe.dll'
$stageArgs = @('/nologo','/target:exe','/langversion:9.0','/define:UNITY_EDITOR_WIN',('/out:"'+$stageDll+'"'))
foreach ($stageReference in Get-ChildItem -LiteralPath $stageRuntime.FullName -Filter '*.dll') {
    try { [Reflection.AssemblyName]::GetAssemblyName($stageReference.FullName) | Out-Null; $stageArgs += '/r:"'+$stageReference.FullName+'"' }
    catch [System.BadImageFormatException] {}
}
$stageArgs += $stageSources | ForEach-Object { '"'+$_+'"' }
$stageArgs | Set-Content (Join-Path $stageOutput 'compile.rsp') -Encoding utf8
& $stageDotnet $stageCompiler ('@'+(Join-Path $stageOutput 'compile.rsp')) 2>&1 | Tee-Object -FilePath (Join-Path $stageOutput 'compile.log')
$stageCompileExit = $LASTEXITCODE
if ($stageCompileExit -eq 0) {
    @{runtimeOptions=@{tfm='net6.0';framework=@{name='Microsoft.NETCore.App';version=$stageRuntime.Name}}} | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $stageOutput 'Stage5Probe.runtimeconfig.json') -Encoding utf8
    & $stageDotnet $stageDll (Join-Path $stageOutput 'fixtures.json') $stageOutput $Filter 2>&1 | Tee-Object -FilePath (Join-Path $stageOutput 'execution.log')
    $stageProbeExit = $LASTEXITCODE
} else { $stageProbeExit = $null }
[pscustomobject]@{source=$stageSource;output=$stageOutput;compile_exit=$stageCompileExit;probe_exit=$stageProbeExit;runtime=$stageRuntime.Name;scope='Actual frozen C# rules with explicit engine/JSON/RNG adapters; no Unity or server launched.'} | ConvertTo-Json | Set-Content (Join-Path $stageOutput 'run.json') -Encoding utf8
Write-Output ('Stage5 evidence: ' + $stageOutput)
Write-Output ('Compiler exit: ' + $stageCompileExit + '; probe exit: ' + $stageProbeExit)
if ($stageCompileExit -ne 0) { exit 3 }
exit $stageProbeExit
