param([string]$UnityRoot='C:/Program Files/Unity/Hub/Editor/6000.0.84f1/Editor')
$ErrorActionPreference='Stop'
# Compile the real capacity policy and grid-shape validator only. Item content
# validation is stubbed; this probe does not exercise Unity serialization/login.
Add-Type -Path (Join-Path $UnityRoot 'Data/DotNetSdkRoslyn/Microsoft.CodeAnalysis.dll')
Add-Type -Path (Join-Path $UnityRoot 'Data/DotNetSdkRoslyn/Microsoft.CodeAnalysis.CSharp.dll')
$sourcePath=Join-Path $PSScriptRoot '../../Assets/Scripts/Camp/CampProfileMigration.cs'
$tree=[Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText([IO.File]::ReadAllText($sourcePath))
$methods=@($tree.GetRoot().DescendantNodes() | Where-Object {
    $_.GetType().Name -eq 'MethodDeclarationSyntax' -and $_.Identifier.ValueText -in @('ExpectedEscrowSlots','ValidateGridShape')
} | ForEach-Object ToFullString)
if($methods.Count -ne 2) { throw 'Capacity methods missing.' }
$harness=@'
using System;
public struct ItemStack {}
public class InventoryGrid { public ItemStack[] Slots; }
public static class ItemCatalog { public const int InventorySlots=9,BackpackSlots=12; }
public static class EscrowProbe {
static void ValidateItemValues(ItemStack stack) {}
public static bool Accepts(int schema,int state,int capacity) {
try { ValidateGridShape(new InventoryGrid { Slots=capacity<0 ? null : new ItemStack[capacity] },ExpectedEscrowSlots(schema,state,capacity)); return true; }
catch(InvalidOperationException) { return false; }
}
'@
Add-Type -TypeDefinition ($harness+($methods -join "`n")+'}')
$count=0
foreach($schema in @(4,5)) {
    foreach($state in @(0,1,2)) {
        foreach($capacity in @(-1,0,8,9,12,16)) {
            $expected=if($schema -lt 5) { $capacity -eq 9 } else { $capacity -eq 12 -or ($state -eq 0 -and $capacity -eq 9) }
            if([EscrowProbe]::Accepts($schema,$state,$capacity) -ne $expected) { throw "Unexpected escrow acceptance: schema=$schema state=$state capacity=$capacity" }
            $count++
        }
    }
}
Write-Output "Escrow capacity: $count cases passed against real capacity methods. Unity serialization and login not exercised."
