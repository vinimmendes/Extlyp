$ErrorActionPreference = 'Stop'
$serverExecutable = Join-Path $PSScriptRoot 'Builds/Server/CyberExtractionServer.exe'
$serversToStop = @(Get-CimInstance Win32_Process -Filter "Name = 'CyberExtractionServer.exe'" |
    Where-Object { $_.ExecutablePath -eq $serverExecutable -and $_.CommandLine -match '(?i)(^|\s)-server(\s|$)' })
if ($serversToStop.Count -eq 0) {
    Write-Output 'Todas as salas ja estao desligadas.'
    return
}
foreach ($serverToStop in $serversToStop) {
    Stop-Process -Id $serverToStop.ProcessId
}
Write-Output 'Todas as salas foram desligadas.'
