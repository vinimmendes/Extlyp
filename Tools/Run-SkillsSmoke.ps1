param([string]$Project = '', [int]$Port = 18177)
$ErrorActionPreference = 'Stop'
$smokeRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ([string]::IsNullOrEmpty($Project)) { $Project = Join-Path $smokeRoot 'Builds/SkillsValidation' }
$smokeProject = [IO.Path]::GetFullPath($Project)
if (-not $smokeProject.StartsWith((Join-Path $smokeRoot 'Builds') + [IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) { throw 'Smoke requires an isolated checkout under Builds.' }
$smokeServer = Join-Path $smokeProject 'Builds/Server/CyberExtractionServer.exe'
$smokeClient = Join-Path $smokeProject 'Builds/Client/CyberExtraction.exe'
$smokeManifestPath = Join-Path (Split-Path -Parent $smokeServer) 'build-manifest.json'
$smokeProtocol = if (Test-Path -LiteralPath $smokeManifestPath) { (Get-Content -LiteralPath $smokeManifestPath -Raw | ConvertFrom-Json).protocol } else { $null }
$smokeLogDir = Join-Path $smokeRoot 'Logs/skills-implementation/smoke'
New-Item -ItemType Directory -Force -Path $smokeLogDir | Out-Null
$smokeRunId = [Guid]::NewGuid().ToString('N')
$smokeReady = Join-Path $smokeLogDir "$smokeRunId.ready"
$smokeServerLog = Join-Path $smokeLogDir "$smokeRunId-server.log"
$smokeClientLog = Join-Path $smokeLogDir "$smokeRunId-client.log"
$smokeData = Join-Path $smokeLogDir "$smokeRunId-data"
$smokeKey = Join-Path $smokeLogDir "$smokeRunId-identity.json"
$smokeServerArgs = '-batchmode -nographics -server -allow-test -skills-smoke-server -metrics -port ' + $Port + ' -cluster-id smoke-' + $smokeRunId + ' -camp-data "' + $smokeData + '" -ready-file "' + $smokeReady + '" -logFile "' + $smokeServerLog + '"'
$smokeServerProcess = Start-Process -FilePath $smokeServer -ArgumentList $smokeServerArgs -WorkingDirectory $smokeProject -WindowStyle Hidden -PassThru
$smokeClientProcess = $null
try {
    $smokeDeadline = [DateTime]::UtcNow.AddSeconds(45)
    while (-not (Test-Path -LiteralPath $smokeReady)) {
        $smokeServerProcess.Refresh()
        if ($smokeServerProcess.HasExited -or [DateTime]::UtcNow -gt $smokeDeadline) { throw "Server not ready: $smokeServerLog" }
        Start-Sleep -Milliseconds 200
    }
    $smokeClientArgs = '-batchmode -nographics -client -skills-smoke -port ' + $Port + ' -identity-file "' + $smokeKey + '" -logFile "' + $smokeClientLog + '"'
    $smokeClientProcess = Start-Process -FilePath $smokeClient -ArgumentList $smokeClientArgs -WorkingDirectory $smokeProject -WindowStyle Hidden -PassThru
    Write-Output "Smoke server/client: $($smokeServerProcess.Id)/$($smokeClientProcess.Id). Logs: $smokeLogDir"
    if (-not $smokeClientProcess.WaitForExit(210000)) { throw "Client timeout: $smokeClientLog" }
    if ($smokeClientProcess.ExitCode -ne 0 -or -not (Select-String -LiteralPath $smokeClientLog -Pattern 'SKILLS SMOKE CLIENT PASS' -Quiet)) { throw "Client failed: $smokeClientLog" }
    if (-not (Select-String -LiteralPath $smokeServerLog -Pattern 'SKILLS SMOKE SERVER PASS' -Quiet)) { throw "Server checks failed: $smokeServerLog" }
    @{ passed=$true; clientLog=$smokeClientLog; serverLog=$smokeServerLog; protocol=$smokeProtocol; scope='Two Windows processes, isolated camp fixture; AI damage disabled for deterministic transaction test' } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $smokeLogDir 'result.json') -Encoding UTF8
    Write-Output 'SKILLS SMOKE PASS'
} finally {
    foreach ($smokeProcess in @($smokeClientProcess,$smokeServerProcess)) {
        if ($null -ne $smokeProcess) { $smokeProcess.Refresh(); if (-not $smokeProcess.HasExited) { Stop-Process -Id $smokeProcess.Id } }
    }
}
