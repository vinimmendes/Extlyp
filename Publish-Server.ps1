# Explicit promotion only: this script never builds, stops or starts a server.
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Server-Channel.ps1')
$stable = Get-ServerChannel Stable
$development = Get-ServerChannel Development
foreach ($required in @('CyberExtractionServer.exe', 'build-manifest.json', 'source.sha256')) {
    if (-not (Test-Path -LiteralPath (Join-Path $development.Directory $required) -PathType Leaf)) {
        throw "Candidato incompleto: $required. Execute Update-Server.ps1 primeiro."
    }
}
function Assert-StableStopped {
    $running = @(Get-CimInstance Win32_Process -Filter "Name = 'CyberExtractionServer.exe'" |
        Where-Object { $_.ExecutablePath -eq $stable.Executable })
    if ($running.Count -gt 0) { throw 'A versao estavel esta em uso. Agende a troca e encerre suas salas explicitamente antes de publicar.' }
}
Assert-StableStopped
$buildRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'Builds')).TrimEnd('\')
$releaseId = [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
$staging = Join-Path $buildRoot "ServerReleases/$releaseId-candidate"
$backup = Join-Path $buildRoot "ServerReleases/$releaseId-previous"
foreach ($target in @($staging, $backup, $stable.Directory)) {
    if (-not [IO.Path]::GetFullPath($target).StartsWith($buildRoot + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Destino de publicacao fora de Builds.'
    }
}
# Stage a complete snapshot; refuse a candidate being changed during the copy.
New-Item -ItemType Directory -Path $staging -Force | Out-Null
$sourceFiles = @(Get-ChildItem -LiteralPath $development.Directory -File -Recurse)
foreach ($file in $sourceFiles) {
    $relative = $file.FullName.Substring($development.Directory.Length + 1)
    $target = Join-Path $staging $relative
    $before = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
    New-Item -ItemType Directory -Force -Path ([IO.Path]::GetDirectoryName($target)) | Out-Null
    Copy-Item -LiteralPath $file.FullName -Destination $target
    if ($before -ne (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -or
        $before -ne (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash) {
        throw 'O candidato mudou durante a copia. A versao estavel foi preservada.'
    }
}
Assert-StableStopped
$previousMoved = $false
try {
    if (Test-Path -LiteralPath $stable.Directory) {
        Move-Item -LiteralPath $stable.Directory -Destination $backup
        $previousMoved = $true
    }
    Move-Item -LiteralPath $staging -Destination $stable.Directory
} catch {
    if ($previousMoved -and -not (Test-Path -LiteralPath $stable.Directory)) {
        Move-Item -LiteralPath $backup -Destination $stable.Directory
    }
    throw
}
Write-Output "Candidato promovido. Versao anterior preservada em $backup. Dados dos jogadores preservados."
Write-Output 'Inicie a estavel com Iniciar-Servidor.bat e distribua clientes compativeis com esta versao.'
