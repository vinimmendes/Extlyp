function Get-ServerSourceFingerprint {
    param([Parameter(Mandatory = $true)][string]$ProjectDirectory)
    $fingerprintRoot = [IO.Path]::GetFullPath($ProjectDirectory).TrimEnd('\')
    $fingerprintRows = New-Object 'System.Collections.Generic.List[string]'
    foreach ($sourceDirectory in @('Assets', 'Packages', 'ProjectSettings')) {
        $sourcePath = Join-Path $fingerprintRoot $sourceDirectory
        foreach ($sourceFile in (Get-ChildItem -LiteralPath $sourcePath -File -Recurse | Sort-Object FullName)) {
            $relativePath = $sourceFile.FullName.Substring($fingerprintRoot.Length + 1).Replace('\', '/')
            $fileHash = (Get-FileHash -LiteralPath $sourceFile.FullName -Algorithm SHA256).Hash
            $fingerprintRows.Add($relativePath + ':' + $fileHash)
        }
    }
    $fingerprintBytes = [Text.Encoding]::UTF8.GetBytes(($fingerprintRows -join "`n"))
    $fingerprintAlgorithm = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($fingerprintAlgorithm.ComputeHash($fingerprintBytes))).Replace('-', '') }
    finally { $fingerprintAlgorithm.Dispose() }
}
