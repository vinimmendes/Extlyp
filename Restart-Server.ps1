$ErrorActionPreference = 'Stop'
$restartLogs = Join-Path $PSScriptRoot 'Logs'
New-Item -ItemType Directory -Force -Path $restartLogs | Out-Null
$restartLog = Join-Path $restartLogs 'server-restart.log'
$restartHash = [Security.Cryptography.SHA256]::Create()
try {
    $restartKey = ([BitConverter]::ToString($restartHash.ComputeHash([Text.Encoding]::UTF8.GetBytes($PSScriptRoot.ToLowerInvariant())))).Replace('-', '').Substring(0,20)
} finally { $restartHash.Dispose() }
$restartMutex = New-Object System.Threading.Mutex($false, ('Local\CyberExtractionRestart_' + $restartKey))
$restartAcquired = $false
try {
    try { $restartAcquired = $restartMutex.WaitOne(0) }
    catch [System.Threading.AbandonedMutexException] { $restartAcquired = $true }
    if (-not $restartAcquired) { return }
    'Reiniciando as salas locais...' | Set-Content -LiteralPath $restartLog -Encoding UTF8
    # Preserve the running rooms when the candidate cannot be built.
    & (Join-Path $PSScriptRoot 'Update-Server.ps1') *>> $restartLog
    & (Join-Path $PSScriptRoot 'Stop-Server.ps1') *>> $restartLog
    & (Join-Path $PSScriptRoot 'Start-Server.ps1') *>> $restartLog
    'Reinicio concluido.' | Add-Content -LiteralPath $restartLog -Encoding UTF8
} catch {
    ('Falha no reinicio: ' + $_.Exception.Message) | Add-Content -LiteralPath $restartLog -Encoding UTF8
    exit 1
} finally {
    if ($restartAcquired) { $restartMutex.ReleaseMutex() }
    $restartMutex.Dispose()
}
