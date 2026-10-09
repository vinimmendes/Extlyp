param(
    [ValidateRange(1, 65533)][int]$Port = 7777,
    [ValidateRange(1, 3)][int]$Rooms = 3
)
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'Update-Server.ps1')
$serverExecutable = Join-Path $PSScriptRoot 'Builds/Server/CyberExtractionServer.exe'
if (-not (Test-Path -LiteralPath $serverExecutable)) {
    throw 'Executavel do servidor nao encontrado apos a atualizacao.'
}
$clusterAlgorithm = [Security.Cryptography.SHA256]::Create()
try {
    $clusterBytes = [Text.Encoding]::UTF8.GetBytes([IO.Path]::GetFullPath($PSScriptRoot).ToLowerInvariant())
    $clusterId = ([BitConverter]::ToString($clusterAlgorithm.ComputeHash($clusterBytes))).Replace('-', '').Substring(0, 20)
} finally { $clusterAlgorithm.Dispose() }
$serverLogDirectory = Join-Path $PSScriptRoot 'Logs'
New-Item -ItemType Directory -Force -Path $serverLogDirectory | Out-Null
$existingServers = @(Get-CimInstance Win32_Process -Filter "Name = 'CyberExtractionServer.exe'" |
    Where-Object { $_.ExecutablePath -eq $serverExecutable -and $_.CommandLine -match '(?i)(^|\s)-server(\s|$)' })
foreach ($roomIndex in 1..$Rooms) {
    $roomPort = $Port + $roomIndex - 1
    $existingRoom = $existingServers | Where-Object {
        $_.CommandLine -match ('(?i)(^|\s)-port\s+' + $roomPort + '(\s|$)')
    } | Select-Object -First 1
    if ($existingRoom -and $existingRoom.CommandLine -match ('(?i)(^|\s)-cluster-id\s+' + $clusterId + '(\s|$)')) {
        Write-Output "Sala $roomIndex ja esta ligada. Processo: $($existingRoom.ProcessId). Porta UDP: $roomPort."
        continue
    }
    if ($existingRoom) {
        $previousRoomProcess = Get-Process -Id $existingRoom.ProcessId -ErrorAction SilentlyContinue
        if ($previousRoomProcess) {
            Stop-Process -Id $existingRoom.ProcessId
            $previousRoomProcess.WaitForExit(30000) | Out-Null
            if (-not $previousRoomProcess.HasExited) { throw 'A sala anterior ainda esta encerrando.' }
        }
    }
    $roomLogName = if ($roomIndex -eq 1) { 'server.log' } else { "server-room-$roomIndex.log" }
    $roomLogPath = Join-Path $serverLogDirectory $roomLogName
    $roomArguments = '-batchmode -nographics -server -port ' + $roomPort + ' -room ' + $roomIndex +
        ' -cluster-id ' + $clusterId + ' -logFile "' + $roomLogPath + '"'
    $roomProcess = Start-Process -FilePath $serverExecutable -ArgumentList $roomArguments -WorkingDirectory $PSScriptRoot -WindowStyle Hidden -PassThru
    Write-Output "Sala $roomIndex iniciada. Processo: $($roomProcess.Id). Porta UDP: $roomPort."
}
Write-Output "Ate $Rooms partidas simultaneas. Para encerrar todas as salas, abra Parar-Servidor.bat."
