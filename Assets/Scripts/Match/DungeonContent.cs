using UnityEngine;
using CyberExtraction.Items;
using CyberExtraction.Enemies;

namespace CyberExtraction.Match
{
    public static class DungeonContent
    {
        public const float InteractionRange=2.3f;
        public static Vector2 CrankPedestal=>DungeonMapPublication.Point("CrankPedestal",DungeonLayout.Rooms[19].Center+new Vector2(0,3));
        public static Vector2 KeyPedestal=>DungeonMapPublication.Point("KeyPedestal",DungeonLayout.Rooms[10].Center+new Vector2(0,3));
        public static Vector2 ShortcutControl=>DungeonLayout.CanalControl;
        public static Vector2 Mechanism(int id) => DungeonMapPublication.Point("Mechanism/"+id,id==3 ? ShortcutControl : id==2 ? DungeonLayout.CanalPoint(52,-50) : DungeonLayout.ExitPositions[id]+new Vector2(-2.5f,-2));
        public static float Duration(int id) => id==0 ? 6 : 4;
        public static string Action(int id) => id==0 ? "Erguer grade" : id==1 ? "Instalar manivela" : id==2 ? "Soltar corrente da barca" : "Liberar atalho da comporta";
        public static ItemId RequiredItem(int id) => id==1 ? ItemId.DungeonCrank : ItemId.DungeonKey;
        public static string ExitStatus(int id)
        {
            var mm=MatchManager.Instance;
            if(mm==null || id<0 || id>2) return "Não verificada";
            if((mm.ActiveExtractions.Value&(1<<id))!=0) return "Pronta";
            // Global guidance reports readiness; timed actions belong to the nearby interaction.
            return id==0 ? "Grade fechada" : id==1 ? "Requer manivela · Câmara dos Contrapesos" : "Barca presa";
        }
        public readonly struct LootSlot
        {
            public readonly int Room;
            public readonly Vector2 Offset;
            public readonly bool Valuable;
            public readonly LootRisk Risk;
            public readonly int Id;
            public LootSlot(int room,float x,float y,bool valuable=false,LootRisk risk=LootRisk.Low,int id=-1)
            { Room=room; Offset=new Vector2(x,y); Valuable=valuable; Risk=risk; Id=id; }
            public Vector2 Position => DungeonMapPublication.Point("Loot/"+Id,DungeonLayout.ExplorationPoints[Room]+Offset);
        }
        public static readonly LootSlot[] Loot={
            new LootSlot(8,-2,-1),new LootSlot(9,-2,-1),new LootSlot(10,-2,-1),new LootSlot(11,-2,-1),
            new LootSlot(12,-2,-1),new LootSlot(13,-2,-1),new LootSlot(14,-2,-1),new LootSlot(15,-2,-1),
            new LootSlot(16,-2,-1),new LootSlot(17,-2,-1),new LootSlot(18,-2,-1),new LootSlot(19,-2,-1),
            new LootSlot(20,-2,-1),new LootSlot(21,-2,-1),new LootSlot(22,-2,-1),new LootSlot(23,-2,-1),
            new LootSlot(8,2,-4),new LootSlot(10,2,-4),new LootSlot(12,0,-4,true,LootRisk.Medium),new LootSlot(19,2,-4,true,LootRisk.Medium),
            new LootSlot(20,2,-4,true,LootRisk.High),new LootSlot(21,2,-4,true,LootRisk.High),new LootSlot(22,2,-4,true,LootRisk.High),new LootSlot(23,2,-4,true,LootRisk.High)};
        public readonly struct Encounter
        {
            public readonly int Room;
            public readonly Vector2 Offset;
            public readonly MonsterKind Kind;
            public readonly int Id;
            public Encounter(int room,float x,float y,MonsterKind kind,int id=-1) { Room=room; Offset=new Vector2(x,y); Kind=kind; Id=id; }
            public Vector2 Position => DungeonMapPublication.Point("Encounter/"+Id,DungeonLayout.Rooms[Room].Center+Offset);
        }
        public static readonly Encounter[] Encounters={
            new Encounter(18,-4,-3,MonsterKind.Weaver),new Encounter(18,4,-4,MonsterKind.Weaver),
            new Encounter(17,-2,2,MonsterKind.Weaver),new Encounter(17,4,-4,MonsterKind.Weaver),
            new Encounter(14,3,8,MonsterKind.Weaver),new Encounter(14,3,-5,MonsterKind.Weaver),
            new Encounter(23,-4,-3,MonsterKind.Weaver),new Encounter(23,0,5,MonsterKind.Weaver),
            new Encounter(8,-2,-2,MonsterKind.Weaver),new Encounter(19,-4,-3,MonsterKind.Weaver),
            new Encounter(10,-3,0,MonsterKind.Stalker),new Encounter(11,1,4,MonsterKind.Stalker),
            new Encounter(9,0,-4,MonsterKind.Stalker),new Encounter(22,-4,-2,MonsterKind.Stalker),new Encounter(15,2,3,MonsterKind.Stalker),
            new Encounter(19,3,-3,MonsterKind.Shell),new Encounter(21,0,-2,MonsterKind.Shell),new Encounter(12,-5,0,MonsterKind.Shell)};
        static DungeonContent()
        {
            for(int i=0;i<Loot.Length;i++)
            { var p=Loot[i]; Loot[i]=new LootSlot(p.Room,p.Offset.x,p.Offset.y,p.Valuable,p.Risk,i); }
            for(int i=0;i<Encounters.Length;i++)
            { var p=Encounters[i]; Encounters[i]=new Encounter(p.Room,p.Offset.x,p.Offset.y,p.Kind,i); }
        }
    }
}
