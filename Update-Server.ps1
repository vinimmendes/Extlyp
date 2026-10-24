$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Server-SourceFingerprint.ps1')
$updateProjectRoot = [IO.Path]::GetFullPath($PSScriptRoot).TrimEnd('\')
$updateOutputDirectory = Join-Path $updateProjectRoot 'Builds/ServerDevelopment'
$updateExecutable = Join-Path $updateOutputDirectory 'CyberExtractionServer.exe'
$updateStampPath = Join-Path $updateOutputDirectory 'source.sha256'
$updateClock = [Diagnostics.Stopwatch]::StartNew()
Write-Output 'Conferindo os arquivos do servidor de desenvolvimento...'
$updateExpectedFingerprint = Get-ServerSourceFingerprint -ProjectDirectory $updateProjectRoot
Write-Output ("Conferencia dos fontes: {0:N1}s." -f $updateClock.Elapsed.TotalSeconds)
if ((Test-Path -LiteralPath $updateExecutable) -and (Test-Path -LiteralPath $updateStampPath) -and
    (Get-Content -LiteralPath $updateStampPath -Raw).Trim() -eq $updateExpectedFingerprint) {
    Write-Output 'O executavel do servidor esta atualizado.'
    return
}

$updateVersionFile = Join-Path $updateProjectRoot 'ProjectSettings/ProjectVersion.txt'
$updateVersionLine = Get-Content -LiteralPath $updateVersionFile | Where-Object { $_ -match '^m_EditorVersion: ' } | Select-Object -First 1
$updateUnityVersion = $updateVersionLine.Substring('m_EditorVersion: '.Length).Trim()
$updateUnityExecutable = Join-Path ${env:ProgramFiles} "Unity/Hub/Editor/$updateUnityVersion/Editor/Unity.exe"
if (-not (Test-Path -LiteralPath $updateUnityExecutable)) {
    throw "Unity $updateUnityVersion nao encontrada. Instale essa versao pelo Unity Hub para atualizar o servidor."
}

# Reutiliza o cache de compilacao, sem disputar o projeto aberto no editor.
$updateWorkspace = [IO.Path]::GetFullPath((Join-Path $updateProjectRoot 'Builds/ServerValidation'))
$updateBuildsRoot = [IO.Path]::GetFullPath((Join-Path $updateProjectRoot 'Builds')).TrimEnd('\') + '\'
if (-not $updateWorkspace.StartsWith($updateBuildsRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Pasta de compilacao fora do projeto.'
}
New-Item -ItemType Directory -Force -Path $updateWorkspace | Out-Null
Write-Output 'Sincronizando a copia de compilacao...'
foreach ($updateSourceDirectory in @('Assets', 'Packages', 'ProjectSettings')) {
    $updateSource = [IO.Path]::GetFullPath((Join-Path $updateProjectRoot $updateSourceDirectory)).TrimEnd('\')
    $updateDestination = [IO.Path]::GetFullPath((Join-Path $updateWorkspace $updateSourceDirectory)).TrimEnd('\')
    if (-not $updateDestination.StartsWith($updateWorkspace + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Destino de sincronizacao invalido.'
    }
    New-Item -ItemType Directory -Force -Path $updateDestination | Out-Null
    $updateSourceFiles = @{}
    foreach ($updateSourceFile in Get-ChildItem -LiteralPath $updateSource -File -Recurse) {
        $updateRelative = $updateSourceFile.FullName.Substring($updateSource.Length + 1)
        $updateSourceFiles[$updateRelative] = $true
        $updateTarget = Join-Path $updateDestination $updateRelative
        $updateNeedsCopy = -not (Test-Path -LiteralPath $updateTarget)
        if (-not $updateNeedsCopy) {
            $updateNeedsCopy = (Get-FileHash -LiteralPath $updateSourceFile.FullName).Hash -ne (Get-FileHash -LiteralPath $updateTarget).Hash
        }
        if ($updateNeedsCopy) {
            New-Item -ItemType Directory -Force -Path ([IO.Path]::GetDirectoryName($updateTarget)) | Out-Null
            Copy-Item -LiteralPath $updateSourceFile.FullName -Destination $updateTarget -Force
        }
    }
    # Remove apenas arquivos da copia de compilacao que ja foram excluidos da origem.
    foreach ($updateObsoleteFile in Get-ChildItem -LiteralPath $updateDestination -File -Recurse) {
        $updateRelative = $updateObsoleteFile.FullName.Substring($updateDestination.Length + 1)
        if (-not $updateSourceFiles.ContainsKey($updateRelative)) {
            $updateResolvedFile = [IO.Path]::GetFullPath($updateObsoleteFile.FullName)
            if (-not $updateResolvedFile.StartsWith($updateDestination + '\', [StringComparison]::OrdinalIgnoreCase)) {
                throw 'Arquivo obsoleto fora da copia de compilacao.'
            }
            Remove-Item -LiteralPath $updateResolvedFile -Force
        }
    }
}

$updateLogsDirectory = Join-Path $updateProjectRoot 'Logs'
New-Item -ItemType Directory -Force -Path $updateLogsDirectory | Out-Null
$updateLogPath = Join-Path $updateLogsDirectory 'server-development-update.log'
Write-Output 'Compilando o servidor de desenvolvimento. A versao estavel continua rodando...'
$updateArguments = '-batchmode -nographics -quit -projectPath "' + $updateWorkspace +
    '" -buildTarget Win64 -executeMethod ServerBuild.BuildServer -logFile "' + $updateLogPath + '"'
if ($env:CE_BUILD_VERSION) {
    if ($env:CE_BUILD_VERSION -notmatch '^[0-9A-Za-z.+-]+$') { throw 'Versao de build invalida.' }
    $updateArguments += ' -ce-build-version ' + $env:CE_BUILD_VERSION
}
$updateUnityProcess = Start-Process -FilePath $updateUnityExecutable -ArgumentList $updateArguments -WorkingDirectory ([IO.Path]::GetDirectoryName($updateUnityExecutable)) -WindowStyle Hidden -PassThru
$null = $updateUnityProcess.Handle
$updateUnityProcess.WaitForExit()
if ($updateUnityProcess.ExitCode -ne 0) {
    throw "A compilacao retornou codigo $($updateUnityProcess.ExitCode). O servidor anterior foi preservado. Consulte $updateLogPath"
}
$updateBuiltDirectory = Join-Path $updateWorkspace 'Builds/ServerDevelopment'
if (-not (Test-Path -LiteralPath (Join-Path $updateBuiltDirectory 'CyberExtractionServer.exe'))) {
    throw 'A Unity nao gerou o executavel esperado.'
}
if ((Get-ServerSourceFingerprint -ProjectDirectory $updateProjectRoot) -ne $updateExpectedFingerprint) {
    throw 'O projeto mudou durante a compilacao. Execute Update-Server.ps1 novamente para compilar o codigo mais recente.'
}

# So interrompe a versao anterior depois que a nova compilacao terminou com sucesso.
$updateRunningServers = @(Get-CimInstance Win32_Process -Filter "Name = 'CyberExtractionServer.exe'" |
    Where-Object { $_.ExecutablePath -eq $updateExecutable -and $_.CommandLine -match '(?i)(^|\s)-server(\s|$)' })
foreach ($updateRunningServer in $updateRunningServers) {
    $updatePreviousProcess = Get-Process -Id $updateRunningServer.ProcessId -ErrorAction SilentlyContinue
    if ($updatePreviousProcess) {
        Stop-Process -Id $updateRunningServer.ProcessId
        $updatePreviousProcess.WaitForExit(30000) | Out-Null
        if (-not $updatePreviousProcess.HasExited) { throw 'O servidor anterior ainda esta encerrando. Tente iniciar novamente.' }
    }
}
New-Item -ItemType Directory -Force -Path $updateOutputDirectory | Out-Null
Get-ChildItem -LiteralPath $updateBuiltDirectory | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $updateOutputDirectory -Recurse -Force
}
Set-Content -LiteralPath $updateStampPath -Value $updateExpectedFingerprint -Encoding ASCII
Write-Output 'Servidor de desenvolvimento atualizado. Versao estavel e APK preservados.'
Write-Output ("Atualizacao concluida em {0:N1}s." -f $updateClock.Elapsed.TotalSeconds)
