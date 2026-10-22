using UnityEngine;
using CyberExtraction.Core;

namespace CyberExtraction.Match
{
    public partial class MapBuilder
    {
        const float TerrainCell=5f;
        public static Vector2 RuinPoint(int index,Vector2 offset)
        {
            float angle=RuinHeading(index)*Mathf.Deg2Rad;
            return largeSites[index]+new Vector2(offset.x*Mathf.Cos(angle)-offset.y*Mathf.Sin(angle),offset.x*Mathf.Sin(angle)+offset.y*Mathf.Cos(angle));
        }
        public static float RuinHeading(int index)
        { float[] headings={0,90,-18,23,180}; return headings[(int)((RuinLayoutKey(index)>>4)%5)]; }
        void BuildPlannedLarge()
        {
            LargeMapPlan.Build(LayoutSeed);
            PlaceRuinSites();
            largeLoot=MakeLoot();
            int columns=(int)(Width/TerrainCell),rows=(int)(Height/TerrainCell);
            var passable=new bool[columns,rows];
            for(int y=0;y<rows;y++) for(int x=0;x<columns;x++)
            {
                Vector2 p=new Vector2(-Width/2+(x+.5f)*TerrainCell,-Height/2+(y+.5f)*TerrainCell);
                passable[x,y]=OpenTerrain(p);
            }
            var random=new System.Random(LayoutSeed^81931);
            // Solid forest/rock masses explain the boundary. They are low sight obstacles.
            for(int y=0;y<rows;y++)
            {
                for(int x=0;x<columns;)
                {
                    if(passable[x,y]) { x++; continue; }
                    int first=x; while(x<columns && !passable[x,y]) x++;
                    Vector2 p=new Vector2(-Width/2+(first+x)*TerrainCell*.5f,-Height/2+(y+.5f)*TerrainCell);
                    var mass=Block("ImpenetrableForest",p,new Vector2((x-first)*TerrainCell,TerrainCell),new Color(.075f,.11f,.065f),-9,true);
                    mass.layer=GameConfig.LowObstacleLayer;
                    for(int cell=first;cell<x;cell+=2)
                    {
                        bool edge=cell>0 && passable[cell-1,y] || cell<columns-1 && passable[cell+1,y] ||
                            y>0 && passable[cell,y-1] || y<rows-1 && passable[cell,y+1];
                        if(!edge) continue;
                        Vector2 at=new Vector2(-Width/2+(cell+.5f)*TerrainCell,p.y);
                        Tree(at,random.Next(4),7+(float)random.NextDouble()*3);
                    }
                }
            }
            // Visible rock escarpments at the exterior; movement cannot leave the forest.
            foreach(var boundary in new[]{new Vector3(0,Height/2+2,0),new Vector3(0,-Height/2-2,0),new Vector3(-Width/2-2,0,1),new Vector3(Width/2+2,0,1)})
            {
                var edge=Block("NaturalEscarpment",boundary,new Vector2(boundary.z==0 ? Width+8 : 4,boundary.z==0 ? 4 : Height+8),new Color(.23f,.25f,.20f),-6,true);
                edge.layer=GameConfig.LowObstacleLayer;
                edge.GetComponent<SpriteRenderer>().enabled=false;
                float length=boundary.z==0 ? Width : Height;
                for(float along=-length*.5f;along<=length*.5f;along+=6)
                {
                    Vector2 at=boundary.z==0 ? new Vector2(along,boundary.y) : new Vector2(boundary.x,along);
                    var rock=Block("BoundaryRock",at,new Vector2(6+(float)random.NextDouble()*3,5+(float)random.NextDouble()*3),new Color(.23f,.25f,.20f),-6);
                    rock.GetComponent<SpriteRenderer>().sprite=SpriteFactory.Polygon(7);
                    rock.transform.rotation=Quaternion.Euler(0,0,random.Next(180));
                }
            }
            foreach(var route in LargeMapPlan.Routes)
            {
                for(int p=1;p<route.Points.Length;p++) GroundPath(route.Points[p-1],route.Points[p],route.Width,new Color(.24f,.23f,.16f));
                BuildRouteCondition(route);
            }
            for(int i=0;i<largeSpawns.Length;i++) House(largeSpawns[i],LargeMapPlan.Sectors[i].Center.y>largeSpawns[i].y ? 1 : -1);
            for(int i=0;i<largeSites.Length;i++)
            {
                var group=new GameObject("Ruin_"+i); group.transform.SetParent(world.transform,false); group.transform.position=largeSites[i];
                int furnitureStart=FurniturePoints.Count;
                int children=world.transform.childCount;
                BuildRuin(largeSites[i],i);
                var added=new System.Collections.Generic.List<Transform>();
                for(int j=children;j<world.transform.childCount;j++) added.Add(world.transform.GetChild(j));
                foreach(var child in added) child.SetParent(group.transform,true);
                float heading=RuinHeading(i); group.transform.rotation=Quaternion.Euler(0,0,heading);
                for(int j=furnitureStart;j<FurniturePoints.Count;j++)
                {
                    var old=FurniturePoints[j];
                    FurniturePoints[j]=new SearchableFurniture(RuinPoint(i,old.Position-largeSites[i]),old.Angle+heading,old.Kind,old.Source,old.Risk);
                }
            }
            Physics2D.SyncTransforms();
            DressSectorLandmarks();
            Physics2D.SyncTransforms();
            foreach(var sector in LargeMapPlan.Sectors)
                for(int attempt=0,placed=0;attempt<20 && placed<5;attempt++)
                {
                    float angle=(float)random.NextDouble()*Mathf.PI*2;
                    Vector2 at=sector.Center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*(35+(float)random.NextDouble()*10);
                    bool onRoad=false;
                    foreach(var route in LargeMapPlan.Routes)
                        for(int p=1;p<route.Points.Length;p++) if(LargeMapPlan.SegmentDistance(at,route.Points[p-1],route.Points[p])<8) onRoad=true;
                    if(onRoad || !TreeSiteClear(at)) continue;
                    Tree(at,random.Next(4),5+(float)random.NextDouble()*3); placed++; Physics2D.SyncTransforms();
                }
            for(int i=0;i<LargeMapPlan.Exits.Length;i++)
            {
                var exit=LargeMapPlan.Exits[i]; GroundPath(LargeMapPlan.Sectors[exit.Sector].Center,exit.Position,7,new Color(.24f,.22f,.16f));
                var go=new GameObject("Exit_"+i+"_"+exit.Name); go.transform.SetParent(world.transform,false); go.transform.position=exit.Position;
                var zone=go.AddComponent<BoxCollider2D>(); zone.size=new Vector2(5,4); zone.isTrigger=true;
                go.AddComponent<ExtractionZone>().ExitId=i;
                go.AddComponent<ForestExtractionMarker>().Initialize(i);
            }
            Physics2D.SyncTransforms();
            foreach(var loot in CyberExtraction.Combat.LootPickup.All)
                if(loot!=null && loot.IsSpawned) loot.RebindFurnitureObstacle();
        }
        bool OpenTerrain(Vector2 point)
        {
            foreach(var site in largeSites) if(Vector2.Distance(point,site)<16) return true;
            foreach(var sector in LargeMapPlan.Sectors)
            {
                Vector2 d=point-sector.Center;
                if(d.x*d.x/(55*55)+d.y*d.y/(51*51)<1) return true;
            }
            foreach(var spawn in largeSpawns) if(Vector2.Distance(point,spawn)<12) return true;
            foreach(var route in LargeMapPlan.Routes)
                for(int p=1;p<route.Points.Length;p++)
                    if(LargeMapPlan.SegmentDistance(point,route.Points[p-1],route.Points[p])<route.Width*.5f+TerrainCell*.7f) return true;
            foreach(var exit in LargeMapPlan.Exits)
                if(LargeMapPlan.SegmentDistance(point,LargeMapPlan.Sectors[exit.Sector].Center,exit.Position)<7) return true;
            return false;
        }
        void PlaceRuinSites()
        {
            var random=new System.Random(LayoutSeed^27319);
            for(int sector=0;sector<LargeMapPlan.Sectors.Length;sector++)
            {
                var candidates=new System.Collections.Generic.List<Vector2>();
                double phase=random.NextDouble()*360;
                for(int i=0;i<72;i++)
                {
                    double angle=(phase+i*5)*System.Math.PI/180;
                    for(int radial=0;radial<3;radial++)
                    {
                        // Half-unit positions keep placement stable between desktop and mobile.
                        float radius=48+radial*6;
                        Vector2 point=LargeMapPlan.Sectors[sector].Center+new Vector2(
                            (float)(System.Math.Round(System.Math.Cos(angle)*radius*2)/2),
                            (float)(System.Math.Round(System.Math.Sin(angle)*radius*2)/2));
                        if(Mathf.Abs(point.x)>Width/2-14 || Mathf.Abs(point.y)>Height/2-14 || point.x>158 && point.y>-202 && point.y<-58) continue;
                        bool clear=true;
                        foreach(var spawn in largeSpawns) if(Vector2.Distance(point,spawn)<22) clear=false;
                        for(int placed=0;placed<sector*4;placed++) if(Vector2.Distance(point,largeSites[placed])<40) clear=false;
                        foreach(var route in LargeMapPlan.Routes)
                        {
                            if(Vector2.Distance(point,route.Gate)<29) clear=false;
                            for(int p=1;p<route.Points.Length;p++) if(LargeMapPlan.SegmentDistance(point,route.Points[p-1],route.Points[p])<14) clear=false;
                        }
                        foreach(var exit in LargeMapPlan.Exits)
                            if(LargeMapPlan.SegmentDistance(point,LargeMapPlan.Sectors[exit.Sector].Center,exit.Position)<14) clear=false;
                        if(clear) candidates.Add(point);
                    }
                }
                var selected=new System.Collections.Generic.List<Vector2>();
                if(!ChooseSites(candidates,selected,0)) throw new System.InvalidOperationException("Setor sem espaço para quatro ruínas fora dos caminhos: "+LargeMapPlan.Sectors[sector].Name);
                for(int i=0;i<4;i++) largeSites[sector*4+i]=selected[i];
            }
        }
        static bool ChooseSites(System.Collections.Generic.List<Vector2> candidates,System.Collections.Generic.List<Vector2> selected,int start)
        {
            if(selected.Count==4) return true;
            for(int i=start;i<candidates.Count;i++)
            {
                bool clear=true; foreach(var point in selected) if(Vector2.Distance(point,candidates[i])<24) clear=false;
                if(!clear) continue;
                selected.Add(candidates[i]);
                if(ChooseSites(candidates,selected,i+1)) return true;
                selected.RemoveAt(selected.Count-1);
            }
            return false;
        }
        void GroundPath(Vector2 from,Vector2 to,float width,Color color)
        {
            Vector2 delta=to-from;
            var road=Block("ForestTrail",(from+to)*.5f,new Vector2(delta.magnitude,width),color,-8);
            road.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
            terrainRoads.Add(new TerrainRoad(from,to,width,road.GetComponent<SpriteRenderer>()));
        }
        void BuildRouteCondition(ForestRoute route)
        {
            if(route.Condition==RouteCondition.Free) return;
            var go=new GameObject("RouteCondition_"+route.Id); go.transform.SetParent(world.transform,false); go.transform.position=route.Gate;
            go.transform.rotation=Quaternion.Euler(0,0,route.Angle);
            if(route.Condition==RouteCondition.Horde)
            {
                ForestProps.Part(go.transform,"Carcass",new Vector2(0,1),new Vector2(2,1.2f),new Color(.24f,.10f,.07f),false);
                return;
            }
            bool gated=route.Condition==RouteCondition.Gated;
            float span=route.Width+TerrainCell*3f;
            if(route.Condition==RouteCondition.Narrow)
            {
                ForestProps.Part(go.transform,"SpilledLogs",new Vector2(0,span*.3f),new Vector2(3,span*.45f),new Color(.29f,.23f,.14f),true);
                return;
            }
            var obstacle=ForestProps.Part(go.transform,gated ? "PassageBarrier" : "RoadBlock",Vector2.zero,new Vector2(2,span),new Color(.26f,.27f,.22f),true);
            if(gated && (route.RequiredItem==CyberExtraction.Items.ItemId.ServiceKey || route.RequiredItem==CyberExtraction.Items.ItemId.DepotKey || route.RequiredItem==CyberExtraction.Items.ItemId.Crowbar)) obstacle.gameObject.layer=GameConfig.WallLayer;
            if(gated)
            {
                if(route.RequiredItem==CyberExtraction.Items.ItemId.BoltCutters)
                    for(float p=-span*.5f;p<span*.5f;p+=1) ForestProps.Part(go.transform,"WireMesh",new Vector2(0,p),new Vector2(.18f,.95f),new Color(.57f,.59f,.51f),false);
                else for(float p=-span*.4f;p<span*.5f;p+=2) ForestProps.Part(go.transform,"BarrierDetail",new Vector2(.2f,p),new Vector2(.4f,1.5f),oldWood,false);
                go.AddComponent<ForestPassage>().Initialize(route.Id,obstacle.gameObject);
            }
            else if(route.From==0 || route.From==1 || route.To==1)
            {
                var truck=new GameObject("OverturnedTruck"); truck.transform.SetParent(go.transform,false);
                truck.transform.localRotation=Quaternion.Euler(0,0,90);
                ForestProps.Vehicle(truck.transform,Vector2.zero,true);
                for(int n=-3;n<=3;n++) if(Mathf.Abs(n)>1)
                    ForestProps.Part(go.transform,"SpilledCargo",new Vector2(.25f,n*span*.13f),new Vector2(2.2f,2.3f),oldWood,false);
            }
            else for(int n=-2;n<=2;n++) ForestProps.Part(go.transform,"FallenTimber",new Vector2(.2f,n*span*.18f),new Vector2(2.5f,span*.2f),oldWood,false);
        }
        void DressSectorLandmarks()
        {
            for(int sector=0;sector<LargeMapPlan.Sectors.Length;sector++)
            {
                Vector2 c=LargeMapPlan.Sectors[sector].Center;
                if(sector==0)
                    for(int i=0;i<5;i++)
                    {
                        Vector2 at=c+new Vector2(35,-8+i*3);
                        if(!LandmarkClear(at,4)) at=c+new Vector2(5,15+i*3);
                        if(!LandmarkClear(at,4)) continue;
                        var log=Block("LogPile",at,new Vector2(7,1.8f),oldWood,-3,true); log.layer=GameConfig.LowObstacleLayer;
                        Physics2D.SyncTransforms();
                    }
                if(sector==1)
                    for(int i=0;i<3;i++)
                    {
                        Vector2 at=c+new Vector2(-5+i*5,31);
                        if(!LandmarkClear(at,2)) continue;
                        Block("SupplyCrate",at,new Vector2(3,2),new Color(.28f,.31f,.25f),-3,true); Physics2D.SyncTransforms();
                    }
                if(sector==3)
                {
                    var lake=Block("DeepLake",new Vector2(196,-130),new Vector2(45,120),new Color(.06f,.18f,.19f),-7,true);
                    lake.GetComponent<SpriteRenderer>().sprite=SpriteFactory.Polygon(16); lake.layer=GameConfig.LowObstacleLayer;
                }
                if(sector==4)
                    for(int i=0;i<6;i++)
                    {
                        var field=Block("CropRow",c+new Vector2(-6+i*2,-7),new Vector2(.6f,13),new Color(.28f,.25f,.14f),-7);
                        field.transform.rotation=Quaternion.Euler(0,0,-12);
                    }
                if(sector==5) Block("CentralRelic",c+new Vector2(0,12),new Vector2(4,3),new Color(.36f,.35f,.29f),-3);
            }
        }
        bool LandmarkClear(Vector2 point,float radius)
        {
            if(Physics2D.OverlapCircle(point,radius,GameConfig.WallMask)!=null) return false;
            foreach(var route in LargeMapPlan.Routes)
                for(int p=1;p<route.Points.Length;p++) if(LargeMapPlan.SegmentDistance(point,route.Points[p-1],route.Points[p])<radius+1) return false;
            foreach(var exit in LargeMapPlan.Exits)
                if(LargeMapPlan.SegmentDistance(point,LargeMapPlan.Sectors[exit.Sector].Center,exit.Position)<radius+1) return false;
            return true;
        }
    }
}
