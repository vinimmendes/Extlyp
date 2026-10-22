param([switch]$NativeOnly)
$ErrorActionPreference='Stop'
$auditRoot=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$auditOutput=Join-Path $auditRoot 'Logs/auditoria/2026-10-16_083542_382c7a/etapa6'
$auditProject=Join-Path $auditRoot 'Builds/Auditoria/2026-10-16_083542_382c7a/ExecucaoEtapa6'
$auditStamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$auditLog=Join-Path $auditOutput ('editor-'+$auditStamp+'.log')
$auditMethod=if($NativeOnly) { 'Stage6Editor.NativeOnly' } else { 'Stage6Editor.Run' }
$auditArgs='-batchmode -nographics -quit -projectPath "'+$auditProject+'" -executeMethod '+$auditMethod+' -audit-output "'+$auditOutput+'" -logFile "'+$auditLog+'"'
$auditProcess=Start-Process -FilePath 'C:/Program Files/Unity/Hub/Editor/6000.0.84f1/Editor/Unity.exe' -ArgumentList $auditArgs -WindowStyle Hidden -PassThru
$auditProcess.Id | Set-Content (Join-Path $auditOutput 'editor.pid')
$auditProcess.WaitForExit()
@{pid=$auditProcess.Id;exit=$auditProcess.ExitCode;log=$auditLog} | ConvertTo-Json | Set-Content (Join-Path $auditOutput ('editor-'+$auditStamp+'-exit.json'))
Write-Output ('Unity exit: '+$auditProcess.ExitCode+' log='+$auditLog)
