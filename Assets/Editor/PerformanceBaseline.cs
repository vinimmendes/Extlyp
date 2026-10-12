using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using CyberExtraction.Players;
using CyberExtraction.Items;
public static class PerformanceBaseline
{
    public static void Run()
    {
        var go=new GameObject("InventoryAllocationProbe"); var inv=go.AddComponent<PlayerInventory>();
        if(inv.Slots==null) inv.Slots=new Unity.Netcode.NetworkList<ItemStack>();
        for(int i=0;i<100;i++) inv.Count(ItemId.PistolAmmo);
        GC.Collect(); long before=GC.GetAllocatedBytesForCurrentThread(); var clock=Stopwatch.StartNew();
        int result=0; for(int i=0;i<100000;i++) result+=inv.Count(ItemId.PistolAmmo);
        clock.Stop(); long bytes=GC.GetAllocatedBytesForCurrentThread()-before;
        UnityEngine.Object.DestroyImmediate(go);
        string output="{\"calls\":100000,\"allocatedBytes\":"+bytes+",\"elapsedMs\":"+clock.Elapsed.TotalMilliseconds.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"result\":"+result+",\"scope\":\"Unity Editor inventory Count microbenchmark; not device FPS\"}";
        Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/inventory-benchmark.json",output); UnityEngine.Debug.Log("INVENTORY BENCHMARK "+output);
    }
}
