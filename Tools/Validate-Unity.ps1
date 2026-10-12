param([string]$Method = 'ImplementationChecks.Run', [switch]$Build, [switch]$Baseline, [switch]$Graphics)
$ErrorActionPreference = 'Stop'
$validationRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$validationFolderName = if ($Baseline) { 'SkillsBaseline' } else { 'SkillsValidation' }
$validationProject = Join-Path $validationRoot "Builds/$validationFolderName"
New-Item -ItemType Directory -Force -Path $validationProject | Out-Null
foreach ($validationDir in @('Assets','Packages','ProjectSettings')) {
    $validationSource = Join-Path $validationRoot $validationDir
    $validationDestination = Join-Path $validationProject $validationDir
    New-Item -ItemType Directory -Force -Path $validationDestination | Out-Null
    Copy-Item -LiteralPath $validationSource -Destination $validationProject -Recurse -Force
}
if ($Build) { $Method = 'ServerBuild.BuildWindowsBoth' }
$validationRunStamp = [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')
$validationMethodName = $Method.Replace('.', '-')
$validationLog = Join-Path $validationRoot "Logs/skills-implementation/$validationFolderName-$validationMethodName-$validationRunStamp.log"
$validationUnity = 'C:\Program Files\Unity\Hub\Editor\6000.0.84f1\Editor\Unity.exe'
$validationGraphics = if ($Graphics) { '' } else { '-nographics ' }
$validationArgs = '-batchmode ' + $validationGraphics + '-quit -projectPath "' + $validationProject + '" -buildTarget Win64 -executeMethod ' + $Method + ' -logFile "' + $validationLog + '" -skills-test -identity-file "' + (Join-Path $validationProject 'Logs/test-identity.json') + '"'
$validationProcess = Start-Process -FilePath $validationUnity -ArgumentList $validationArgs -WindowStyle Hidden -PassThru
Write-Output "Unity validation process: $($validationProcess.Id). Log: $validationLog"
$validationProcess.WaitForExit()
if ($validationProcess.ExitCode -ne 0) { throw "Unity validation failed: $validationLog" }
Write-Output 'Unity validation completed.'
