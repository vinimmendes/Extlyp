param([string]$Project = '', [int]$Port = 18377, [switch]$Graphics, [switch]$Visible, [int]$Width = 1280, [int]$Height = 720)
$ErrorActionPreference = 'Stop'
$labRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ([string]::IsNullOrEmpty($Project)) { $Project = Join-Path $labRoot 'Builds/SkillsValidation' }
$labProject = [IO.Path]::GetFullPath($Project)
if (-not $labProject.StartsWith((Join-Path $labRoot 'Builds') + [IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) { throw 'Use a copia isolada em Builds.' }
$labLogs = Join-Path $labRoot 'Logs/test-lab'
New-Item -ItemType Directory -Force -Path $labLogs | Out-Null
$labRun = [Guid]::NewGuid().ToString('N')
$labReady = Join-Path $labLogs "$labRun.ready"
$labServerLog = Join-Path $labLogs "$labRun-server.log"
$labClientLog = Join-Path $labLogs "$labRun-client.log"
$labServerArgs = '-batchmode -nographics -server -allow-test -port ' + $Port + ' -cluster-id lab-' + $labRun + ' -camp-data "' + (Join-Path $labLogs "$labRun-data") + '" -ready-file "' + $labReady + '" -logFile "' + $labServerLog + '"'
$labServerProcess = Start-Process -FilePath (Join-Path $labProject 'Builds/ServerDevelopment/CyberExtractionServer.exe') -ArgumentList $labServerArgs -WorkingDirectory $labProject -WindowStyle Hidden -PassThru
$labClientProcess = $null
try {
    $labDeadline = [DateTime]::UtcNow.AddSeconds(45)
    while (-not (Test-Path -LiteralPath $labReady)) {
        $labServerProcess.Refresh()
        if ($labServerProcess.HasExited -or [DateTime]::UtcNow -gt $labDeadline) { throw "Servidor nao iniciou: $labServerLog" }
        Start-Sleep -Milliseconds 200
    }
    $labGraphicsArgs = if ($Graphics) { '-screen-fullscreen 0 -screen-width ' + $Width + ' -screen-height ' + $Height + ' -lab-capture "' + (Join-Path $labLogs 'Screenshots') + '" ' } else { '-batchmode -nographics ' }
    $labClientArgs = $labGraphicsArgs + '-client -lab-smoke -port ' + $Port + ' -identity-file "' + (Join-Path $labLogs "$labRun-key.json") + '" -logFile "' + $labClientLog + '"'
    $labWindowStyle = if ($Visible) { 'Normal' } else { 'Hidden' }
    $labClientProcess = Start-Process -FilePath (Join-Path $labProject 'Builds/Client/CyberExtraction.exe') -ArgumentList $labClientArgs -WorkingDirectory $labProject -WindowStyle $labWindowStyle -PassThru
    Write-Output "Teste do laboratorio iniciado. Logs: $labLogs"
    if (-not $labClientProcess.WaitForExit(170000)) { throw "Cliente excedeu o prazo: $labClientLog" }
    if ($labClientProcess.ExitCode -ne 0 -or -not (Select-String -LiteralPath $labClientLog -Pattern 'TEST LAB SMOKE PASS' -Quiet)) { throw "Teste falhou: $labClientLog" }
    @{ passed=$true; protocol=13; clientLog=$labClientLog; serverLog=$labServerLog } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $labLogs 'result.json') -Encoding UTF8
    Write-Output 'TEST LAB SMOKE PASS'
} finally {
    foreach ($labProcess in @($labClientProcess,$labServerProcess)) {
        if ($null -ne $labProcess) { $labProcess.Refresh(); if (-not $labProcess.HasExited) { Stop-Process -Id $labProcess.Id } }
    }
}
