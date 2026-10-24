param(
    [ValidateSet('Stable', 'Development')][string]$Channel = 'Stable',
    [ValidateRange(0, 65533)][int]$Port = 0,
    [ValidateRange(1, 3)][int]$Rooms = 3,
    [switch]$SkipUpdate
)
$ErrorActionPreference = 'Stop'
$startupClock = [Diagnostics.Stopwatch]::StartNew()
. (Join-Path $PSScriptRoot 'Server-Channel.ps1')
$configuration = Get-ServerChannel $Channel
if ($Port -eq 0) { $Port = $configuration.Port }
if (($Channel -eq 'Development' -and $Port -lt 7780 -and $Port + $Rooms - 1 -ge 7777) -or
    ($Channel -eq 'Stable' -and $Port -lt 17780 -and $Port + $Rooms - 1 -ge 17777)) {
    throw 'As portas escolhidas invadem o intervalo reservado ao outro canal.'
}
if ($Channel -eq 'Development' -and -not $SkipUpdate) { & (Join-Path $PSScriptRoot 'Update-Server.ps1') }
$serverExecutable = $configuration.Executable
if (-not (Test-Path -LiteralPath $serverExecutable)) {
    throw "Executavel de $Channel ausente. Prepare uma versao de desenvolvimento e use Publish-Server.ps1 para promover a estavel."
}
$clusterId = $configuration.Cluster
$serverLogDirectory = $configuration.Logs
New-Item -ItemType Directory -Force -Path $serverLogDirectory | Out-Null
$existingServers = @(Get-CimInstance Win32_Process -Filter "Name = 'CyberExtractionServer.exe'" |
    Where-Object { $_.ExecutablePath -eq $serverExecutable -and $_.CommandLine -match '(?i)(^|\s)-server(\s|$)' })
$pendingRooms = New-Object 'System.Collections.Generic.List[object]'
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
        throw "A porta $roomPort ja possui uma sala com outra identidade. Encerre-a explicitamente antes de iniciar."
    }
    $roomLogName = if ($roomIndex -eq 1) { 'server.log' } else { "server-room-$roomIndex.log" }
    $roomLogPath = Join-Path $serverLogDirectory $roomLogName
    $roomReadyPath = Join-Path $serverLogDirectory "room-$roomIndex.ready"
    if (Test-Path -LiteralPath $roomReadyPath) { Remove-Item -LiteralPath $roomReadyPath -Force }
    $roomArguments = '-batchmode -nographics -server -port ' + $roomPort + ' -room ' + $roomIndex +
        ' -cluster-id ' + $clusterId + ' -ready-file "' + $roomReadyPath + '" -logFile "' + $roomLogPath + '"'
    if ($configuration.Data) {
        New-Item -ItemType Directory -Force -Path $configuration.Data | Out-Null
        $roomArguments += ' -camp-data "' + $configuration.Data + '"'
    }
    $roomProcess = Start-Process -FilePath $serverExecutable -ArgumentList $roomArguments -WorkingDirectory $PSScriptRoot -WindowStyle Hidden -PassThru
    $pendingRooms.Add([pscustomobject]@{
        Index=$roomIndex; Port=$roomPort; Process=$roomProcess; Ready=$roomReadyPath; Log=$roomLogPath
        Clock=[Diagnostics.Stopwatch]::StartNew(); Deadline=[DateTime]::UtcNow.AddSeconds(30)
    })
    Write-Output "Abrindo sala $roomIndex em UDP $roomPort..."
}
# Launch every missing room before waiting, so engine startup is not serialized.
while ($pendingRooms.Count -gt 0) {
    for ($pendingIndex=$pendingRooms.Count-1; $pendingIndex -ge 0; $pendingIndex--) {
        $pending = $pendingRooms[$pendingIndex]
        $pending.Process.Refresh()
        if ($pending.Process.HasExited) { throw "A sala $($pending.Index) encerrou durante a inicializacao. Consulte $($pending.Log)" }
        if (Test-Path -LiteralPath $pending.Ready) {
            Write-Output ("Sala {0} pronta em {1:N1}s. Processo: {2}. Porta UDP: {3}." -f $pending.Index,$pending.Clock.Elapsed.TotalSeconds,$pending.Process.Id,$pending.Port)
            $pendingRooms.RemoveAt($pendingIndex)
        } elseif ([DateTime]::UtcNow -ge $pending.Deadline) {
            throw "A sala $($pending.Index) nao confirmou prontidao. Consulte $($pending.Log)"
        }
    }
    if ($pendingRooms.Count -gt 0) { Start-Sleep -Milliseconds 200 }
}
Write-Output ("Inicializacao de {0} concluida em {1:N1}s." -f $Channel,$startupClock.Elapsed.TotalSeconds)
Write-Output "Canal ${Channel}: ate $Rooms partidas simultaneas. Encerre suas salas pela opcao 2 deste console."
