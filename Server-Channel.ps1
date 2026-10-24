function Get-ServerChannel {
    param([ValidateSet('Stable', 'Development')][string]$Channel = 'Stable')
    $development = $Channel -eq 'Development'
    $relative = if ($development) { 'Builds/ServerDevelopment' } else { 'Builds/Server' }
    $logs = if ($development) { 'Logs/Development' } else { 'Logs' }
    $algorithm = [Security.Cryptography.SHA256]::Create()
    try {
        # Stable keeps its existing identity and persistentDataPath saves.
        $identity = [IO.Path]::GetFullPath($PSScriptRoot).ToLowerInvariant()
        if ($development) { $identity += '|development' }
        $cluster = ([BitConverter]::ToString($algorithm.ComputeHash([Text.Encoding]::UTF8.GetBytes($identity)))).Replace('-', '').Substring(0, 20)
    } finally { $algorithm.Dispose() }
    [pscustomobject]@{
        Name = $Channel
        Executable = Join-Path $PSScriptRoot "$relative/CyberExtractionServer.exe"
        Directory = Join-Path $PSScriptRoot $relative
        Logs = Join-Path $PSScriptRoot $logs
        Port = $(if ($development) { 17777 } else { 7777 })
        Cluster = $cluster
        Data = $(if ($development) { Join-Path $PSScriptRoot 'Builds/ServerDevelopmentData' } else { $null })
    }
}
