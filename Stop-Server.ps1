param([ValidateSet('Stable', 'Development')][string]$Channel = 'Stable')
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Server-Channel.ps1')
$serverExecutable = (Get-ServerChannel $Channel).Executable
$serversToStop = @(Get-CimInstance Win32_Process -Filter "Name = 'CyberExtractionServer.exe'" |
    Where-Object { $_.ExecutablePath -eq $serverExecutable -and $_.CommandLine -match '(?i)(^|\s)-server(\s|$)' })
if ($serversToStop.Count -eq 0) {
    Write-Output 'Todas as salas ja estao desligadas.'
    return
}
foreach ($serverToStop in $serversToStop) {
    $stoppingRoom = Get-Process -Id $serverToStop.ProcessId -ErrorAction SilentlyContinue
    if ($stoppingRoom) {
        Stop-Process -Id $serverToStop.ProcessId
        $stoppingRoom.WaitForExit(30000) | Out-Null
        if (-not $stoppingRoom.HasExited) { throw 'Uma sala ainda esta encerrando. Tente novamente.' }
    }
}
Write-Output 'Todas as salas foram desligadas.'
