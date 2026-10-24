param([string]$Version='1.2.4',[int]$VersionCode=7)
$ErrorActionPreference='Stop'
if ($Version -notmatch '^[0-9A-Za-z.+-]+$' -or $VersionCode -lt 1) { throw 'Versao invalida.' }
$deliveryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$deliveryWorkspace=Join-Path $deliveryRoot 'Builds/ServerValidation'
$deliveryOutput=Join-Path $deliveryRoot "Builds/Releases/$Version"
$deliveryVersion=(Get-Content (Join-Path $deliveryRoot 'ProjectSettings/ProjectVersion.txt') | Where-Object { $_ -match '^m_EditorVersion: ' }).Substring(17).Trim()
$deliveryUnity=Join-Path ${env:ProgramFiles} "Unity/Hub/Editor/$deliveryVersion/Editor/Unity.exe"
if (-not (Test-Path -LiteralPath (Join-Path $deliveryWorkspace 'Assets/Scenes/Game.unity'))) { throw 'Prepare primeiro a copia atualizada do servidor.' }
New-Item -ItemType Directory -Force -Path $deliveryOutput | Out-Null
$deliveryLog=Join-Path $deliveryRoot "Logs/android-$Version-build.log"
$deliveryArguments='-batchmode -nographics -quit -projectPath "'+$deliveryWorkspace+'" -buildTarget Android -executeMethod ServerBuild.BuildAndroid -ce-build-version '+$Version+' -ce-android-version-code '+$VersionCode+' -logFile "'+$deliveryLog+'"'
$deliveryProcess=Start-Process -FilePath $deliveryUnity -ArgumentList $deliveryArguments -WorkingDirectory ([IO.Path]::GetDirectoryName($deliveryUnity)) -WindowStyle Hidden -PassThru
$null=$deliveryProcess.Handle
Write-Output "Compilando APK $Version, codigo $VersionCode. PID $($deliveryProcess.Id). Log: $deliveryLog"
$deliveryProcess.WaitForExit()
if ($deliveryProcess.ExitCode -ne 0) { throw "Android retornou codigo $($deliveryProcess.ExitCode). Consulte $deliveryLog" }
$deliveryApk=Join-Path $deliveryWorkspace 'Builds/Android/CyberExtraction.apk'
if (-not (Test-Path -LiteralPath $deliveryApk)) { throw 'APK nao foi gerado.' }
$deliveryDestination=Join-Path $deliveryOutput "CyberExtraction-$Version.apk"
if (Test-Path -LiteralPath $deliveryDestination) { throw 'APK de entrega ja existe; preserve-o antes de repetir.' }
Copy-Item -LiteralPath $deliveryApk -Destination $deliveryDestination
Write-Output "APK pronto: $deliveryDestination"
