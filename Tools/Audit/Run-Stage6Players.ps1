param([switch]$VisibleClients,[ValidateRange(1,2)][int]$ClientCount=2)
$ErrorActionPreference='Stop'
$auditRoot=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$auditOutput=Join-Path $auditRoot 'Logs/auditoria/2026-10-16_083542_382c7a/etapa6'
$auditExe=Join-Path $auditOutput 'Player/CyberExtractionAudit.exe'
if(!(Test-Path -LiteralPath $auditExe)) { throw 'Audit executable missing' }
$auditProbe=[System.Net.Sockets.UdpClient]::new(19177)
$auditProbe.Dispose()
$auditProcesses=@()
try {
    $auditRoles=if($ClientCount -eq 1) { @('server','client1') } else { @('server','client1','client2') }
    foreach($auditRole in $auditRoles) {
        $auditExtra=if($auditRole -eq 'server') { '-batchmode -nographics -server -cluster-id audit-stage6-20261016 -camp-data "'+$auditOutput+'/runtime-data" -ready-file "'+$auditOutput+'/server-ready.txt"' } else { '-client -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -identity-file "'+$auditOutput+'/'+$auditRole+'-identity.json"' }
        $auditArgs='-audit-stage6 -audit-role '+$auditRole+' -audit-output "'+$auditOutput+'" -port 19177 -logFile "'+$auditOutput+'/'+$auditRole+'.log" '+$auditExtra
        $auditWindow=if($VisibleClients -and $auditRole -ne 'server') { 'Normal' } else { 'Hidden' }
        $auditProcess=Start-Process -FilePath $auditExe -ArgumentList $auditArgs -WindowStyle $auditWindow -PassThru
        $auditProcesses+=$auditProcess
        @{role=$auditRole;pid=$auditProcess.Id;started=(Get-Date).ToString('o');arguments=$auditArgs} | ConvertTo-Json -Compress | Add-Content (Join-Path $auditOutput 'processes.jsonl')
    }
    $auditDeadline=(Get-Date).AddMinutes(30)
    while(!(Test-Path -LiteralPath (Join-Path $auditOutput 'stop-players.txt')) -and (Get-Date) -lt $auditDeadline) {
        if(@($auditProcesses | Where-Object { !$_.HasExited }).Count -eq 0) { break }
        Start-Sleep -Seconds 1
    }
} finally {
    foreach($auditProcess in $auditProcesses) {
        if(!$auditProcess.HasExited) { Stop-Process -Id $auditProcess.Id }
        $auditProcess.WaitForExit()
        @{pid=$auditProcess.Id;exit=$auditProcess.ExitCode} | ConvertTo-Json -Compress | Add-Content (Join-Path $auditOutput 'exits.jsonl')
    }
}
