param([string]$UnityExe='C:/Program Files/Unity/Hub/Editor/6000.0.84f1/Editor/Unity.exe',[string]$WorkerProject='')
$ErrorActionPreference='Stop'
$t2Root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$t2Id=[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')+'-'+[Guid]::NewGuid().ToString('N').Substring(0,6)
$t2Output=Join-Path $t2Root ('Logs/pos-auditoria/T2Shared/'+$t2Id)
$t2Project=Join-Path $t2Root ('Builds/PosAuditoria/T2Shared/'+$t2Id)
if($WorkerProject) {
    $t2Project=[IO.Path]::GetFullPath($WorkerProject)
    $t2Allowed=[IO.Path]::GetFullPath((Join-Path $t2Root 'Builds/PosAuditoria/T2Shared'))+[IO.Path]::DirectorySeparatorChar
    if(!$t2Project.StartsWith($t2Allowed,[StringComparison]::OrdinalIgnoreCase)) { throw 'Worker must stay inside isolated T2Shared projects.' }
}
New-Item -ItemType Directory -Force -Path $t2Output,(Join-Path $t2Project 'Assets/Editor'),(Join-Path $t2Project 'Packages'),(Join-Path $t2Project 'ProjectSettings') | Out-Null
function Copy-T2Tree([string]$Source,[string]$Destination) {
    foreach($t2Entry in Get-ChildItem -LiteralPath $Source -File -Recurse) {
        $t2Target=Join-Path $Destination $t2Entry.FullName.Substring($Source.Length+1)
        New-Item -ItemType Directory -Force -Path ([IO.Path]::GetDirectoryName($t2Target)) | Out-Null
        Copy-Item -LiteralPath $t2Entry.FullName -Destination $t2Target
    }
}
Copy-T2Tree (Join-Path $t2Root 'Assets/Scripts') (Join-Path $t2Project 'Assets/Scripts')
foreach($t2File in @('PostAuditT2SharedChecks.cs','PostAuditT2SharedChecks.cs.meta')) {
    Copy-Item -LiteralPath (Join-Path $t2Root ('Assets/Editor/'+$t2File)) -Destination (Join-Path $t2Project ('Assets/Editor/'+$t2File))
}
foreach($t2File in @('manifest.json','packages-lock.json')) {
    Copy-Item -LiteralPath (Join-Path $t2Root ('Packages/'+$t2File)) -Destination (Join-Path $t2Project ('Packages/'+$t2File))
}
Copy-Item -LiteralPath (Join-Path $t2Root 'ProjectSettings/ProjectVersion.txt') -Destination (Join-Path $t2Project 'ProjectSettings/ProjectVersion.txt')
# Only native equipment definitions are needed by the profile rules. No art/scenes copied.
New-Item -ItemType Directory -Force -Path (Join-Path $t2Project 'Assets/Resources/Items/Equipment') | Out-Null
Copy-T2Tree (Join-Path $t2Root 'Assets/Resources/Items/Equipment/Definitions') (Join-Path $t2Project 'Assets/Resources/Items/Equipment/Definitions')
foreach($t2File in @('Gameplay.asset','Gameplay.asset.meta')) {
    Copy-Item -LiteralPath (Join-Path $t2Root ('Assets/Resources/'+$t2File)) -Destination (Join-Path $t2Project ('Assets/Resources/'+$t2File))
}
git -C $t2Root status --short | Set-Content (Join-Path $t2Output 'status-before.txt')
$t2Hashes=Get-ChildItem -LiteralPath (Join-Path $t2Root 'Assets/Scripts') -Recurse -Filter '*.cs' | ForEach-Object {
    [pscustomobject]@{path=$_.FullName.Substring($t2Root.Length+1);sha256=(Get-FileHash -LiteralPath $_.FullName).Hash}
}
$t2Hashes | ConvertTo-Json | Set-Content (Join-Path $t2Output 'source-hashes.json')
$t2Cluster='auditT2-'+$t2Id
function Start-T2Role([string]$Role,[string]$Project) {
    $t2Arguments='-batchmode -nographics -projectPath "'+$Project+'" -executeMethod PostAuditT2SharedChecks.Run -t2-output "'+$t2Output+'" -t2-cluster '+$t2Cluster+' -t2-role '+$Role+' -logFile "'+(Join-Path $t2Output ($Role+'-editor.log'))+'"'
    Start-Process -FilePath $UnityExe -ArgumentList $t2Arguments -WindowStyle Hidden -PassThru
}
$t2Processes=@()
try {
    Write-Output ('T2 evidence: '+$t2Output)
    $t2A=Start-T2Role 'A' $t2Root; $t2Processes+= $t2A
    $t2B=Start-T2Role 'B' $t2Project; $t2Processes+= $t2B
    $t2Limit=[DateTime]::UtcNow.AddMinutes(8)
    while(!$t2A.HasExited -or !$t2B.HasExited) {
        if(($t2A.HasExited -and $t2A.ExitCode -ne 0) -or ($t2B.HasExited -and $t2B.ExitCode -ne 0)) { throw 'A T2 worker failed; inspect its report/log.' }
        if([DateTime]::UtcNow -gt $t2Limit) { throw 'T2 process timeout; inspect logs.' }
        Start-Sleep -Seconds 2
    }
    $t2A.WaitForExit(); $t2B.WaitForExit()
    if($t2A.ExitCode -ne 0 -or $t2B.ExitCode -ne 0) { throw ('T2 failed: A='+$t2A.ExitCode+' B='+$t2B.ExitCode) }
    foreach($t2Role in @('A','B')) {
        if(!(Get-Content (Join-Path $t2Output ($t2Role+'-result.json')) -Raw | ConvertFrom-Json).Success) { throw ($t2Role+' failed') }
    }
    $t2C=Start-T2Role 'Restart' $t2Root; $t2Processes+=$t2C
    $t2Limit=[DateTime]::UtcNow.AddMinutes(3)
    while(!$t2C.HasExited) {
        if([DateTime]::UtcNow -gt $t2Limit) { throw 'T2 restart timeout' }
        Start-Sleep -Seconds 2
    }
    $t2C.WaitForExit()
    if($t2C.ExitCode -ne 0 -or !(Get-Content (Join-Path $t2Output 'Restart-result.json') -Raw | ConvertFrom-Json).Success) { throw 'Restart failed' }
    Write-Output 'T2 shared processes and restart passed.'
}
finally {
    foreach($t2Process in $t2Processes) { if(!$t2Process.HasExited) { Stop-Process -Id $t2Process.Id } }
}
