# Focused script probe: all process discovery, launches and stops are mocked.
# No Unity session, server executable or live player data is used.
$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$global:launchFixture = $null
function Get-CimInstance { param($ClassName,$Filter) return $global:launchFixture.Existing }
function Start-Process {
    param($FilePath,$ArgumentList,$WorkingDirectory,$WindowStyle,[switch]$PassThru)
    if ($FilePath -ne $global:launchFixture.Executable) { throw 'Tentativa de abrir executavel real.' }
    $ready = [regex]::Match($ArgumentList,'-ready-file "([^"]+)"').Groups[1].Value
    $global:launchFixture.Count++
    $process = [pscustomobject]@{
        Id=90000+$global:launchFixture.Count; HasExited=$false
        Clock=[Diagnostics.Stopwatch]::StartNew(); Ready=$ready
    }
    $process | Add-Member ScriptMethod Refresh {
        if ($global:launchFixture.Fail) { $this.HasExited=$true; return }
        if ($this.Clock.Elapsed.TotalMilliseconds -ge 500 -and -not (Test-Path -LiteralPath $this.Ready)) {
            $global:launchFixture.CountAtReady.Add($global:launchFixture.Count)
            Set-Content -LiteralPath $this.Ready -Value $this.Id
        }
    }
    return $process
}
function Stop-Process { throw 'O iniciador nao deveria encerrar processos.' }
$results = New-Object 'System.Collections.Generic.List[object]'
foreach ($scenario in @('Parallel','AlreadyRunning','Partial','WrongCluster','FailedProcess','Timeout')) {
    $fixture = Join-Path $project ('.utmp/ServerStartup/Fixtures/' + $scenario + '-' + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Force -Path $fixture | Out-Null
    $source = Join-Path $project 'Start-Server.ps1'
    Copy-Item -LiteralPath $source -Destination (Join-Path $fixture 'Start-Server.ps1')
    if ($scenario -eq 'Timeout') {
        $scriptPath = Join-Path $fixture 'Start-Server.ps1'
        $scriptText = [IO.File]::ReadAllText($scriptPath).Replace('AddSeconds(30)', 'AddSeconds(0.1)')
        [IO.File]::WriteAllText($scriptPath, $scriptText)
    }
    $helper = @'
function Get-ServerChannel {
    param($Channel)
    [pscustomobject]@{ Executable=(Join-Path $PSScriptRoot 'Mock.exe'); Port=7777; Cluster='fixture'; Logs=(Join-Path $PSScriptRoot 'Logs'); Data=$null }
}
'@
    Set-Content (Join-Path $fixture 'Server-Channel.ps1') -Value $helper
    $executable = Join-Path $fixture 'Mock.exe'
    Set-Content -LiteralPath $executable -Value 'not executable'
    $global:launchFixture = [pscustomobject]@{
        Count=0; Executable=$executable; Existing=@(); Fail=($scenario -eq 'FailedProcess')
        CountAtReady=(New-Object 'System.Collections.Generic.List[int]')
    }
    if($scenario -in @('AlreadyRunning','Partial','WrongCluster')) {
        $existingCount=if($scenario -eq 'AlreadyRunning') {3} else {1}
        $cluster=if($scenario -eq 'WrongCluster') {'other'} else {'fixture'}
        $global:launchFixture.Existing=@(foreach($index in 0..($existingCount-1)) {
            [pscustomobject]@{ ExecutablePath=$executable; CommandLine=('-server -port '+(7777+$index)+' -cluster-id '+$cluster); ProcessId=(80000+$index) }
        })
    }
    $clock=[Diagnostics.Stopwatch]::StartNew()
    $failed=$false
    try { & (Join-Path $fixture 'Start-Server.ps1') -Channel Stable | Out-Null }
    catch {
        if($scenario -eq 'WrongCluster' -and $_.Exception.Message -like '*outra identidade*') { $failed=$true }
        elseif($scenario -eq 'FailedProcess' -and $_.Exception.Message -like '*encerrou durante*') { $failed=$true }
        elseif($scenario -eq 'Timeout' -and $_.Exception.Message -like '*nao confirmou prontidao*') { $failed=$true }
        else { throw }
    }
    if($scenario -in @('WrongCluster','FailedProcess','Timeout') -and -not $failed) { throw 'Falha nao detectada.' }
    $expected=if($scenario -in @('AlreadyRunning','WrongCluster')) {0} elseif($scenario -eq 'Partial') {2} else {3}
    if($global:launchFixture.Count -ne $expected) { throw "Quantidade incorreta em $scenario" }
    if($scenario -eq 'Parallel' -and @($global:launchFixture.CountAtReady | Where-Object {$_ -ne 3}).Count -gt 0) { throw 'Salas ainda serializadas.' }
    $results.Add([pscustomobject]@{ Scenario=$scenario; Seconds=$clock.Elapsed.TotalSeconds; Starts=$global:launchFixture.Count; ExpectedFailure=$failed })
}
$results | ConvertTo-Json | Set-Content (Join-Path $project '.utmp/ServerStartup/launcher-evidence.json') -Encoding UTF8
$results | ConvertTo-Json -Compress

