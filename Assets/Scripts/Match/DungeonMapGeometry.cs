using UnityEngine;
using CyberExtraction.Core;

namespace CyberExtraction.Match
{
    public partial class MapBuilder
    {
        static readonly Color[] dungeonColors={new Color(.24f,.23f,.22f),new Color(.29f,.27f,.23f),
            new Color(.25f,.25f,.20f),new Color(.30f,.28f,.23f),new Color(.19f,.26f,.27f),
            new Color(.26f,.24f,.25f),new Color(.32f,.27f,.25f),new Color(.29f,.25f,.20f)};
        void BuildDungeon()
        {
            var bridgeColliders=new System.Collections.Generic.List<Collider2D>();
            var coverPlaceholders=new System.Collections.Generic.List<SpriteRenderer>();
            // Merge horizontal cells: geometry is the exact same mask used by navigation/cartography.
            for(int y=0;y<DungeonLayout.GridHeight;y++)
            {
                int x=0;
                while(x<DungeonLayout.GridWidth)
                {
                    bool floor=DungeonLayout.IsBaseFloor(x,y),cover=DungeonLayout.IsLowCover(x,y),water=DungeonLayout.IsWater(x,y);
                    bool bridgeCell=DungeonLayout.IsCanalBridge(x,y);
                    int sector=DungeonLayout.SectorAt(x,y),first=x++;
                    while(x<DungeonLayout.GridWidth && DungeonLayout.IsBaseFloor(x,y)==floor && DungeonLayout.IsLowCover(x,y)==cover &&
                        DungeonLayout.IsWater(x,y)==water &&
                        DungeonLayout.IsCanalBridge(x,y)==bridgeCell &&
                        (!floor || DungeonLayout.SectorAt(x,y)==sector)) x++;
                    if(cover) continue; // Drawn once below, with its own low-obstacle collider.
                    var r=DungeonLayout.WorldRect(new RectInt(first,y,x-first,1));
                    var block=Block(floor ? "DungeonFloor" : water ? "DungeonWater" : "DungeonStone",r.center,r.size,
                        floor ? dungeonColors[Mathf.Max(0,sector)] : water ? new Color(.09f,.19f,.23f) : new Color(.075f,.075f,.08f),floor || water ? -8 : -9,!floor);
                    if(water && !floor)
                    {
                        block.layer=GameConfig.LowObstacleLayer;
                        if(bridgeCell) bridgeColliders.Add(block.GetComponent<Collider2D>());
                    }
                }
            }
            foreach(var cells in DungeonLayout.Covers)
            {
                var r=DungeonLayout.WorldRect(cells);
                var cover=Block("DungeonSarcophagus",r.center,r.size,new Color(.39f,.36f,.30f),-4,true);
                cover.layer=GameConfig.LowObstacleLayer;
                coverPlaceholders.Add(cover.GetComponent<SpriteRenderer>());
                if(DungeonMapPublication.Current==null) Block("SarcophagusLid",r.center,r.size*.75f,new Color(.25f,.23f,.21f),-3);
            }
            if(DungeonMapPublication.Current==null) foreach(var room in DungeonLayout.Rooms)
            {
                // Floor inlay is an orientation cue; it has no collision or pickup behaviour.
                Block("RoomThreshold_"+room.Name,room.Center+Vector2.down*3,new Vector2(2,.25f),dungeonColors[room.Sector]*1.5f,-7);
            }
            if(DungeonMapPublication.Current==null) for(int i=0;i<DungeonLayout.Spawns.Length;i++)
                Block("Entrance_"+(i+1),DungeonLayout.Spawns[i],new Vector2(2.5f,2.5f),new Color(.38f,.34f,.24f),-7);
            SpriteRenderer platform=null,boat=null;
            // Build on both server and clients, independently of optional rendered art.
            // Low obstacles preserve sight over the stone railing.
            for(int side=0;side<3;side++)
            {
                var bounds=DungeonLayout.StairBorder(side);
                var rim=new GameObject(side==2 ? "CortejoStairBottomCollision" : side==0 ? "CortejoStairLeftCollision" : "CortejoStairRightCollision");
                rim.transform.SetParent(world.transform,false);
                rim.transform.position=bounds.center;
                rim.layer=GameConfig.LowObstacleLayer;
                rim.AddComponent<BoxCollider2D>().size=bounds.size;
            }
            for(int i=0;i<DungeonLayout.ExitPositions.Length;i++)
            {
                var c=DungeonLayout.ExitPositions[i];
                if(i==0) for(int step=0;step<5;step++)
                    Block("CortejoStep",c+Vector2.up*step*.5f,new Vector2(3,.3f),new Color(.45f,.42f,.35f),-6);
                else if(i==1)
                {
                    // The dark opening stays inside the stone rim, including its transparent outer corners.
                    Block("CoveirosShaft",c,new Vector2(2.8f,2.8f),Color.black,-7);
                    platform=Block("CoveirosPlatform",c,new Vector2(2.5f,2.5f),new Color(.38f,.27f,.17f),-6).GetComponent<SpriteRenderer>();
                }
                else
                {
                    boat=Block("FuneralBoat",c,new Vector2(4,2)*DungeonLayout.CellSize,new Color(.37f,.25f,.16f),-6).GetComponent<SpriteRenderer>();
                }
                var zone=new GameObject("DungeonExtraction_"+i); zone.transform.SetParent(world.transform,false); zone.transform.position=c;
                if(i==2)
                {
                    var trigger=zone.AddComponent<BoxCollider2D>(); trigger.isTrigger=true;
                    trigger.size=new Vector2(3,1.2f)*DungeonLayout.CellSize;
                }
                else { var trigger=zone.AddComponent<CircleCollider2D>(); trigger.isTrigger=true; trigger.radius=1.6f; }
                zone.AddComponent<ExtractionZone>().ExitId=i;
            }
            for(int i=0;i<4;i++) Block("DungeonMechanism_"+i,DungeonContent.Mechanism(i),new Vector2(.8f,.8f),new Color(.65f,.49f,.25f),-5);
            Block("CrankPedestal",DungeonContent.CrankPedestal,new Vector2(1.5f,1.5f),new Color(.42f,.35f,.25f),-7);
            Block("KeyPedestal",DungeonContent.KeyPedestal,new Vector2(1.5f,1.5f),new Color(.42f,.35f,.25f),-7);
            var gate=Block("CortejoGrade",DungeonLayout.ExitPositions[0],new Vector2(3,.3f),new Color(.6f,.6f,.53f),-3);
            var chain=Block("FuneralChain",DungeonContent.Mechanism(2)+Vector2.right*.7f,new Vector2(2,.16f),Color.gray,-3);
            var crossing=DungeonLayout.WorldRect(DungeonLayout.CanalBridge);
            var bridge=Block("OpenedCanalCrossing",crossing.center,crossing.size,new Color(.34f,.26f,.17f),-6);
            bridge.SetActive(false);
            world.AddComponent<DungeonPresentation>().Initialize(platform,boat,gate,chain,bridge,bridgeColliders.ToArray());
            if(DungeonMapPublication.Current!=null)
            {
                if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null)
                {
                    world.GetComponent<DungeonPresentation>().SetArt(DungeonExtractionArt.Build(world.transform));
                    if(DungeonMapArt.Build(world.transform,DungeonMapPublication.Current)!=null)
                        foreach(var cover in coverPlaceholders) cover.enabled=false;
                }
            }
            else
            {
                DungeonTerrain.Build(world.transform);
                DungeonArchitecture.Build(world.transform);
                DungeonProps.Build(world.transform);
                DungeonSectorArt.Build(world.transform);
                world.GetComponent<DungeonPresentation>().SetArt(DungeonExtractionArt.Build(world.transform));
                DungeonNarrativePilot.Build(world.transform);
                DungeonDetails.Build(world.transform);
                if(catacombOssuaryReworkPilot) DungeonOssuaryRework.Build(world.transform);
            }
        }
    }
}
