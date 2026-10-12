// Executa regras reais do projeto fora da Unity. Adaptadores abaixo substituem
// somente os tipos de engine/rede usados na compilacao; nao simulam gameplay.
using System;
using System.Collections.Generic;
using System.Text.Json;
using CyberExtraction.Items;
using CyberExtraction.Camp;
using CyberExtraction.Bots;
using CyberExtraction.Match;

namespace UnityEngine
{
    public enum RuntimeInitializeLoadType { SubsystemRegistration }
    public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type) { } }
    public static class Mathf
    {
        public static float Clamp01(float value) => Math.Max(0,Math.Min(1,value));
        public static float Lerp(float a,float b,float t) => a+(b-a)*Clamp01(t);
        public static int RoundToInt(float value) => (int)Math.Round(value);
        public static int Max(int a,int b) => Math.Max(a,b);
    }
    public static class Application { public static bool isEditor=false; }
    public static class Debug { public static bool isDebugBuild=false; }
    public struct Color { public Color(float r, float g, float b) { } }
    public static class JsonUtility
    {
        public static string ToJson(object value) => throw new NotSupportedException("Serializacao Unity fora do escopo desta prova.");
        public static T FromJson<T>(string value) => throw new NotSupportedException("Serializacao Unity fora do escopo desta prova.");
    }
}
// Outside-engine fixture uses the current default recipe. Unity checks validate the actual asset.
namespace CyberExtraction.Core
{
    public class GameplayDefinition
    {
        public static readonly GameplayDefinition Current=new GameplayDefinition();
        public int Magazine=12,AmmoRecipeOutput=12,MetalRecipeCost=3,PowderRecipeCost=1,ComponentRecipeCost=1,ComponentPrice=2;
        public float ReloadSeconds=1.5f;
    }
}
namespace Unity.Netcode
{
    public interface IReaderWriter { }
    public interface INetworkSerializable { void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter; }
    public struct BufferSerializer<T> where T : IReaderWriter { public void SerializeValue<TValue>(ref TValue value) { } }
}

class AuditProbe
{
    static readonly List<object> Results = new List<object>();
    static int failures;
    static void Check(string name, bool expected, string evidence)
    {
        Results.Add(new { name, status = expected ? "CONFIRMED" : "UNEXPECTED", evidence });
        if (!expected) failures++;
    }
    static int Main()
    {
        var bag = InventoryGrid.Starter();
        Check("starter_equipment", bag.Count(ItemId.Pistol) == 1 && bag.Count(ItemId.PistolAmmo) == 24,
            "Uma pistola e 24 tiros de reserva, distribuidos sob limite de 20.");
        var full = new InventoryGrid();
        for (int i = 0; i < full.Slots.Length; i++) full.Slots[i] = new ItemStack(ItemId.Wood, 20);
        Check("inventory_full_atomic", !full.Add(new ItemStack(ItemId.Metal, 1)) && full.Count(ItemId.Wood) == 180,
            "Adicionar metal a mochila cheia falha sem consumir ou sobrescrever madeira.");
        Check("negative_remove_rejected", !full.Remove(ItemId.Wood, -1) && full.Count(ItemId.Wood) == 180,
            "Remocao negativa rejeitada.");
        var destination = new InventoryGrid();
        Check("pistol_transfer_preserves_rounds", bag.MoveTo(0, destination, 0) && destination.Slots[0].Data == 12 && bag.Count(ItemId.Pistol) == 0,
            "Transferencia conserva os tiros da pistola e remove a origem.");
        var craft = new CampProgress();
        craft.Chest.Add(new ItemStack(ItemId.Metal, 3)); craft.Chest.Add(new ItemStack(ItemId.Gunpowder, 1)); craft.Chest.Add(new ItemStack(ItemId.AmmoComponent, 1));
        Check("craft_cost_and_output", craft.CraftAmmo() && craft.Count(ItemId.PistolAmmo) == 36 && craft.Count(ItemId.Metal) == 0,
            "Receita consome recursos e acrescenta exatamente 12 tiros aos 24 iniciais.");
        var blockedCraft = new CampProgress { Pack = full.Copy(), Chest = full.Copy() };
        blockedCraft.Chest.Slots[0] = new ItemStack(ItemId.Metal, 20); blockedCraft.Chest.Slots[1] = new ItemStack(ItemId.Gunpowder, 20); blockedCraft.Chest.Slots[2] = new ItemStack(ItemId.AmmoComponent, 20);
        Check("craft_full_rollback", !blockedCraft.CraftAmmo() && blockedCraft.Count(ItemId.Metal) == 20 && blockedCraft.Count(ItemId.Gunpowder) == 20 && blockedCraft.Count(ItemId.AmmoComponent) == 20,
            "Receita sem espaco desfaz o consumo de todos os ingredientes.");
        var repair = new CampProgress(); repair.Deposit(20, 9, 0);
        foreach(var ammo in new[] {ItemId.PistolAmmo,ItemId.RifleAmmo,ItemId.ShotgunShell})
        {
            var p=new CampProgress { Pack=new InventoryGrid() };
            p.Chest.Add(new ItemStack(ItemId.Metal,3)); p.Chest.Add(new ItemStack(ItemId.Gunpowder,1)); p.Chest.Add(new ItemStack(ItemId.AmmoComponent,1));
            var action=new CampAction { Sequence=1,Kind=CampActionKind.CraftAmmo,Result=(int)ammo };
            Check("ammo_recipe_"+ammo,p.Apply(action) && p.Count(ammo)==WeaponCatalog.RecipeOutput(ammo) && p.Count(ItemId.Metal)==0,"Receita real consome custos e entrega a municao correta.");
            Check("ammo_replay_"+ammo,!p.Apply(action),"Repetir receita nao duplica municao.");
        }
        Check("invalid_ammo_recipe",!new CampProgress().CraftAmmo(ItemId.Mosin),"Arma nao pode ser usada como receita de municao.");
        var rifleBag=new InventoryGrid(); var rifleChest=new InventoryGrid();
        Check("rifle_transfer_loaded",rifleBag.Add(new ItemStack(ItemId.Mosin,2,0,3)) && rifleBag.Slots[0].Quantity==1 && rifleBag.MoveTo(0,rifleChest,8) && rifleChest.Slots[8].Data==3,"Rifles nao empilham e conservam carregador ao transferir.");
        Check("repair_insufficient_atomic", !repair.RepairPier() && repair.Count(ItemId.Wood) == 20 && repair.Count(ItemId.Nails) == 9,
            "Reparo sem pregos suficientes conserva os materiais.");
        var food = new CampProgress { WellFed = true, FoodHealth = 25 };
        Check("food_buff_consumed_once", food.ConsumeFoodBuff() == 25 && food.ConsumeFoodBuff() == 0,
            "Buff de alimento consumido uma unica vez.");
        var camp = new CampProgress { PierRepaired = true };
        Check("forged_fishing_rejected", !camp.Apply(new CampAction { Sequence=1,Kind=CampActionKind.CatchFish,Duration=4 }), "Cliente nao pode declarar uma captura.");
        var command=new CampAction { Sequence=1,Kind=CampActionKind.Transfer,Source=0,Target=4 };
        Check("valid_sequenced_transfer", camp.Apply(command), "Comando de sequencia correta aceito.");
        Check("immediate_replay_rejected", !camp.Apply(command), "Comando ja aplicado rejeitado.");
        for(int i=0;i<256;i++)
            if(!camp.Apply(new CampAction { Sequence=camp.Revision+1,Kind=CampActionKind.Transfer,Source=i%2==0 ? 4 : 0,Target=i%2==0 ? 0 : 4 })) throw new Exception("Preparacao da prova falhou.");
        Check("old_replay_rejected", !camp.Apply(command), "Sequencia antiga continua rejeitada apos expirar historico.");
        var cook=new CampProgress { Pack=new InventoryGrid() }; cook.Pack.Add(new ItemStack(ItemId.FishCommon,1));
        Check("forged_perfect_cooking_rejected", !cook.Apply(new CampAction { Sequence=1,Kind=CampActionKind.CookFish,Source=0,Result=3,Duration=16 }), "Cliente nao escolhe qualidade de cozimento.");
        Check("invalid_cooking_result_rejected", !cook.CompleteCooking(new CampAction { Sequence=1,Kind=CampActionKind.CookFish,Source=0,Result=999,Duration=16 }), "Resultado fora de 0..3 rejeitado mesmo na conclusao interna.");
        Check("server_cooking_completion", cook.CompleteCooking(new CampAction { Sequence=1,Kind=CampActionKind.CookFish,Source=0,Result=3,Duration=16 }), "Conclusao interna de atividade valida preserva recompensa.");
        var queue = new MatchQueue();
        queue.SecondsLeft(1, 100);
        Check("queue_deadline", !queue.ShouldStart(1, 129.1) && queue.ShouldStart(1, 130), "Espera de 30 segundos preservada.");
        Check("bot_extraction_boundary", !BotDecision.CanExtract(61) && BotDecision.CanExtract(60), "Extracao de bot liberada somente no ultimo minuto.");
        Check("bot_eligible_loot_selection", BotDecision.Select(new BotSituation { Active = true, KnownLoot = true, HealthRatio = 1, SecondsLeft = 180 }, BotProfile.For(BotDifficulty.Medium)) == BotState.Collect,
            "A politica escolhe Collect para KnownLoot elegivel; a capacidade e filtrada pela percepcao do bot.");
        Console.WriteLine(JsonSerializer.Serialize(new { scope = "regras C# reais com receita default de fixture; sem Unity, fisica, transporte, serializacao ou dispositivo", failures, results = Results }, new JsonSerializerOptions { WriteIndented = true }));
        return failures == 0 ? 0 : 1;
    }
}
