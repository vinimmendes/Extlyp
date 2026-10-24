param([ValidateSet('Stable', 'Development')][string]$Channel = 'Stable')
$ErrorActionPreference = 'Stop'

function Invoke-ServerConsoleCommand {
    param([string]$Command)
    try {
        switch ($Command) {
            '1' { & (Join-Path $PSScriptRoot 'Start-Server.ps1') -Channel $Channel }
            '2' { & (Join-Path $PSScriptRoot 'Stop-Server.ps1') -Channel $Channel }
            '3' {
                $global:LASTEXITCODE = 0
                & (Join-Path $PSScriptRoot 'Restart-Server.ps1') -Channel $Channel
                if ($LASTEXITCODE -ne 0) { throw 'Consulte o log de reinicio do canal.' }
                Write-Host 'Reinicio concluido.' -ForegroundColor Green
            }
            default { Write-Host 'Opcao invalida. Digite 1, 2 ou 3.' -ForegroundColor Yellow }
        }
    } catch {
        Write-Host ('Falha: ' + $_.Exception.Message) -ForegroundColor Red
        Write-Host 'O console continua aberto. Voce pode tentar novamente.' -ForegroundColor Yellow
    }
}

Write-Host "Cyber Extraction - Servidor $Channel" -ForegroundColor Cyan
Write-Host 'Fechar esta janela deixa as salas ligadas. Use 2 para desligar.'

while ($true) {
    Write-Host ''
    Write-Host '1 - abre'
    Write-Host '2 - fecha'
    Write-Host '3 - reinicia'
    Write-Host ''
    $serverConsoleInput = Read-Host 'Digite 1, 2 ou 3'
    if ($null -eq $serverConsoleInput) { break }
    Invoke-ServerConsoleCommand ($serverConsoleInput.Trim().ToLowerInvariant())
}
