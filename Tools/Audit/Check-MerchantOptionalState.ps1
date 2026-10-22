param([string]$UnityRoot='C:/Program Files/Unity/Hub/Editor/6000.0.84f1/Editor')
$ErrorActionPreference='Stop'
# Exercise the actual optional-state classifier without starting Unity or touching
# any save. The empty ItemStack stub supplies only the unrelated offer field type;
# this probe does not simulate JsonUtility or validate complete inventories.
$merchantSourcePath=Join-Path $PSScriptRoot '../../Assets/Scripts/Camp/MerchantCatalog.cs'
Add-Type -Path (Join-Path $UnityRoot 'Data/DotNetSdkRoslyn/Microsoft.CodeAnalysis.dll')
Add-Type -Path (Join-Path $UnityRoot 'Data/DotNetSdkRoslyn/Microsoft.CodeAnalysis.CSharp.dll')
$merchantTree=[Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText([IO.File]::ReadAllText($merchantSourcePath))
$merchantNames=@('MerchantGroup','MerchantOffer','MerchantState')
$merchantDeclarations=@($merchantTree.GetRoot().DescendantNodes() | Where-Object {
    ($_.GetType().Name -eq 'ClassDeclarationSyntax' -or $_.GetType().Name -eq 'EnumDeclarationSyntax') -and
    $merchantNames -contains $_.Identifier.ValueText
} | ForEach-Object ToFullString)
if($merchantDeclarations.Count -ne 3) { throw 'Tipos reais de estado do comerciante nao encontrados.' }
Add-Type -TypeDefinition ('using System; using CyberExtraction.Items; namespace CyberExtraction.Items { public struct ItemStack {} } namespace CyberExtraction.Camp {'+($merchantDeclarations -join "`n")+'}')
$merchantCases=@(
    @{ Name='null optional field'; State=$null; Absent=$true },
    @{ Name='materialized empty inline object'; State=[CyberExtraction.Camp.MerchantState]::new(); Absent=$true },
    @{ Name='materialized empty array'; State=[CyberExtraction.Camp.MerchantState]@{Offers=[CyberExtraction.Camp.MerchantOffer[]]@()}; Absent=$true },
    @{ Name='cycle without stock must still be validated'; State=[CyberExtraction.Camp.MerchantState]@{Cycle='cycle'}; Absent=$false },
    @{ Name='stock without cycle must still be validated'; State=[CyberExtraction.Camp.MerchantState]@{Offers=[CyberExtraction.Camp.MerchantOffer[]]@([CyberExtraction.Camp.MerchantOffer]::new())}; Absent=$false },
    @{ Name='restock marker without stock must still be validated'; State=[CyberExtraction.Camp.MerchantState]@{LastRestockedExpedition='expedition'}; Absent=$false },
    @{ Name='complete stock is present'; State=[CyberExtraction.Camp.MerchantState]@{Cycle='cycle';Offers=[CyberExtraction.Camp.MerchantOffer[]]::new(12)}; Absent=$false },
    @{ Name='wrong stock count is present, never silently erased'; State=[CyberExtraction.Camp.MerchantState]@{Cycle='cycle';Offers=[CyberExtraction.Camp.MerchantOffer[]]::new(11)}; Absent=$false }
)
foreach($merchantCase in $merchantCases) {
    if([CyberExtraction.Camp.MerchantState]::IsAbsent($merchantCase.State) -ne $merchantCase.Absent) { throw ('Failed: '+$merchantCase.Name) }
}
Write-Output ('Merchant optional state: '+$merchantCases.Count+' cases passed against the real classifier. Unity serialization and live login not exercised.')
