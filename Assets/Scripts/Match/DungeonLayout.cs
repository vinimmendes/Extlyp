using System.Collections.Generic;
using UnityEngine;

namespace CyberExtraction.Match
{
    // Authored, single-level medieval blockout. No seed changes its geography.
    // Coordinates are expressed in cells; the area report fixes CellSize.
    public static partial class DungeonLayout
    {
        public const int GridWidth=128, GridHeight=128;
        public const float CellSize=1.06f;
        public const float SurveyStep=.25f, PlayerRadius=.4f;
        public readonly struct Room
        {
            public readonly string Name;
            public readonly int Sector;
            public readonly RectInt Cells;
            public Room(string name,int sector,int x,int y,int w,int h)
            { Name=name; Sector=sector; Cells=new RectInt(x-w/2+64,y-h/2+64,w,h); }
            public Room(string name,int sector,RectInt cells)
            { Name=name; Sector=sector; Cells=cells; }
            public Vector2 Center => ToWorld(Cells.center);
        }
        public static readonly string[] SectorNames={"Vestíbulos dos Enlutados","Galeria dos Nomes Raspados","Ossário das Raízes",
            "Claustro das Velas Mortas","Cisternas do Pranto","Prisão dos Penitentes","Capela Invertida","Câmara dos Contrapesos"};
        public static readonly Room[] Rooms={
            new Room("Entrada do sino",0,-52,40,10,10), new Room("Entrada das urnas",0,-40,52,10,10),
            new Room("Entrada dos nomes",0,40,52,10,10), new Room("Entrada das grades",0,52,40,10,10),
            new Room("Entrada das raízes",0,-52,-40,10,10), new Room("Entrada das velas",0,-40,-52,10,10),
            new Room("Entrada do cortejo",0,16,-56,10,10), new Room("Entrada do canal",0,52,-16,10,10),
            new Room("Galeria das urnas",1,-24,42,20,16),new Room("Nomes raspados",1,0,44,18,16),
            new Room("Celas dos penitentes",5,24,42,20,16),new Room("Sala das correntes",5,44,20,18,16),
            new Room("Salão das águas",4,44,-4,18,20),new Room("Embarcadouro funerário",4,44,-48,20,16),
            new Room("Escadaria do cortejo",1,24,-44,18,16),new Room("Galeria dos relevos",1,0,-44,18,16),
            new Room("Claustro das velas",3,-24,-42,20,16),new Room("Ossário inferior",2,-44,-20,18,16),
            new Room("Ossário das raízes",2,-44,4,18,20),new Room("Câmara dos contrapesos",7,-28,24,20,16),
            new Room("Pátio das vigílias",3,-10,0,18,20),new Room("Capela invertida",6,12,0,20,20),
            new Room("Sacristia partida",6,12,24,18,16),new Room("Cisterna dos lamentos",4,8,-24,16,16)};
        static readonly Room[] originalRooms=(Room[])Rooms.Clone();
        // Each edge is an orthogonal corridor, with an authored bend rather than a diagonal shortcut.
        public static readonly int[,] Links={
            {0,18},{1,8},{2,10},{3,11},{4,17},{5,16},{6,14},{7,12},
            {8,9},{9,10},{10,11},{11,12},{14,15},{15,16},{16,17},{17,18},{18,19},{19,8},
            {19,20},{20,21},{21,12},{21,22},{22,9},{22,10},{20,23},{23,15},{20,16},{18,20}};
        public static readonly Vector2[] Spawns=MakeSpawns();
        public static readonly Vector2[] ExplorationPoints=MakeExplorationPoints();
        public static readonly int[] ExitRooms={14,18,13};
        public static readonly int[] SectorLabelRooms={0,9,18,16,12,10,21,19};
        public static readonly string[] ExitNames={"Escadaria do Cortejo","Poço dos Coveiros","Canal dos Defuntos"};
        public static readonly Vector2[] ExitPositions={Rooms[14].Center+new Vector2(0,4),Rooms[18].Center+new Vector2(0,4),CanalPoint(56,-50)};
        static readonly Vector2[] originalExits=(Vector2[])ExitPositions.Clone();
        // Stone rim of the 3 x 3 stair sprite, centred one unit above the exit.
        // The northern edge stays open; physics and navigation share these bounds.
        public static Rect StairBorder(int side)
        {
            var centre=ExitPositions[0]+Vector2.up;
            const float half=1.5f, thickness=.36f;
            var original=side==2
                ? new Rect(centre.x-half,centre.y-half,half*2,thickness)
                : new Rect(centre.x+(side==0 ? -half : half-thickness),centre.y-half,thickness,half*2);
            return DungeonMapPublication.Region("StairBorder/"+side,original);
        }
        static readonly bool[] floor=new bool[GridWidth*GridHeight];
        static readonly int[] sectors=new int[GridWidth*GridHeight];
        static readonly bool[] lowCover=new bool[GridWidth*GridHeight];
        static readonly bool[] waterCells=new bool[GridWidth*GridHeight];
        public static readonly List<RectInt> Covers=new List<RectInt>();
        public static readonly List<RectInt> Pillars=new List<RectInt>();
        static readonly Dictionary<long,int[]> routes=new Dictionary<long,int[]>();
        static readonly Dictionary<int,bool[]> navigationMasks=new Dictionary<int,bool[]>();
        static bool shortcutOpen;
        static string appliedContent;
        public static int GeometryVersion { get; private set; }
        public static void SetShortcutOpen(bool open)
        { if(shortcutOpen==open) return; shortcutOpen=open; GeometryVersion++; routes.Clear(); navigationMasks.Clear(); }
        static readonly int[] dx={1,0,-1,0},dy={0,1,0,-1};

        static DungeonLayout()
        { RebuildPublishedContent(); }
        public static void EnsurePublishedContent()
        { if(appliedContent!=DungeonMapPublication.ContentHash) RebuildPublishedContent(); }
        static void RebuildPublishedContent()
        {
            System.Array.Copy(originalRooms,Rooms,Rooms.Length);
            System.Array.Copy(originalExits,ExitPositions,ExitPositions.Length);
            var starts=MakeSpawns(); System.Array.Copy(starts,Spawns,starts.Length);
            var exploration=MakeExplorationPoints(); System.Array.Copy(exploration,ExplorationPoints,exploration.Length);
            System.Array.Clear(floor,0,floor.Length); System.Array.Clear(lowCover,0,lowCover.Length); System.Array.Clear(waterCells,0,waterCells.Length);
            Covers.Clear(); Pillars.Clear(); routes.Clear(); navigationMasks.Clear(); shortcutOpen=false;
            for(int i=0;i<sectors.Length;i++) sectors[i]=-1;
            for(int i=0;i<Rooms.Length;i++) Paint(Rooms[i].Cells,Rooms[i].Sector);
            for(int i=0;i<Links.GetLength(0);i++)
            {
                var a=Rooms[Links[i,0]].Cells.center; var b=Rooms[Links[i,1]].Cells.center;
                // Spawn approaches turn outside the antechamber, keeping paired starts screened.
                if(Links[i,0]<8)
                {
                    var bend=new Vector2(a.x,b.y);
                    Corridor(a,bend); Corridor(bend,b);
                }
                else
                {
                    var bend=new Vector2(b.x,a.y);
                    Corridor(a,bend); Corridor(bend,b);
                }
            }
            // Full-height architectural pillars and low sarcophagi are separate masks.
            for(int i=8;i<Rooms.Length;i++)
            {
                var c=Rooms[i].Cells.center;
                AddObstacle(new RectInt((int)c.x-6,(int)c.y-4,2,2),i%3==0);
                AddObstacle(new RectInt((int)c.x+4,(int)c.y+2,2,2),false);
            }
            BuildCanalGeometry();
            ApplyPublication();
            appliedContent=DungeonMapPublication.ContentHash;
            GeometryVersion++;
        }
        static void ApplyPublication()
        {
            var data=DungeonMapPublication.Current;
            if(data==null) return;
            foreach(var r in data.rooms) Rooms[r.id]=new Room(r.name,r.sector,new RectInt(r.x,r.y,r.width,r.height));
            Covers.Clear(); Pillars.Clear();
            for(int i=0;i<floor.Length;i++)
            {
                floor[i]=data.cells[i]==(int)DungeonCellKind.Floor;
                waterCells[i]=data.cells[i]==(int)DungeonCellKind.Water;
                lowCover[i]=data.cells[i]==(int)DungeonCellKind.LowCover;
                sectors[i]=data.sectors[i];
            }
            Covers.AddRange(DungeonMapArt.CoverRectangles(data.cells));
            foreach(var r in data.regions)
                if(r.id.StartsWith("Pillar/")) Pillars.Add(new RectInt(Mathf.RoundToInt(r.x),Mathf.RoundToInt(r.y),Mathf.RoundToInt(r.width),Mathf.RoundToInt(r.height)));
            for(int i=0;i<Spawns.Length;i++) Spawns[i]=DungeonMapPublication.Point("Spawn/"+i,Rooms[i].Center);
            for(int i=0;i<ExplorationPoints.Length;i++) ExplorationPoints[i]=DungeonMapPublication.Point("Exploration/"+i,Rooms[i].Center);
            for(int i=0;i<ExitPositions.Length;i++) ExitPositions[i]=DungeonMapPublication.Point("Exit/"+i,ExitPositions[i]);
            GeometryVersion++; routes.Clear(); navigationMasks.Clear();
        }
        static Vector2[] MakeSpawns()
        { var result=new Vector2[8]; for(int i=0;i<8;i++) result[i]=Rooms[i].Center; return result; }
        static Vector2[] MakeExplorationPoints()
        { var result=new Vector2[Rooms.Length]; for(int i=0;i<result.Length;i++) result[i]=Rooms[i].Center+(i==12 ? Vector2.right*5*CellSize : Vector2.zero); return result; }
        public static Vector2 ToWorld(Vector2 cell) => (cell-new Vector2(64,64))*CellSize;
        public static Rect WorldRect(RectInt r) => new Rect(ToWorld(r.position),new Vector2(r.width,r.height)*CellSize);
        public static int Index(int x,int y) => x<0 || y<0 || x>=GridWidth || y>=GridHeight ? -1 : y*GridWidth+x;
        public static int CellAt(Vector2 p) => Index(Mathf.FloorToInt(p.x/CellSize+64),Mathf.FloorToInt(p.y/CellSize+64));
        public static bool IsBaseFloor(int x,int y) { int i=Index(x,y); return i>=0 && floor[i]; }
        public static bool IsFloor(int x,int y) { int i=Index(x,y); return i>=0 && (floor[i] || shortcutOpen && IsCanalBridge(x,y)); }
        public static bool IsFloor(Vector2 p) { int i=CellAt(p); return i>=0 && IsFloor(i%GridWidth,i/GridWidth); }
        public static bool IsLowCover(int x,int y) { int i=Index(x,y); return i>=0 && lowCover[i]; }
        public static bool IsWater(int x,int y) { int i=Index(x,y); return i>=0 && waterCells[i]; }
        public static int SectorAt(int x,int y) { int i=Index(x,y); return i>=0 ? sectors[i] : -1; }
        static void Paint(RectInt rect,int sector)
        {
            foreach(var p in rect.allPositionsWithin)
            { int i=Index(p.x,p.y); if(i<0) continue; floor[i]=true; if(sectors[i]<0) sectors[i]=sector; }
        }
        static void Corridor(Vector2 a,Vector2 b)
        {
            Paint(new RectInt(Mathf.RoundToInt(Mathf.Min(a.x,b.x))-2,Mathf.RoundToInt(Mathf.Min(a.y,b.y))-2,
                Mathf.RoundToInt(Mathf.Abs(a.x-b.x))+4,Mathf.RoundToInt(Mathf.Abs(a.y-b.y))+4),0);
        }
        static void AddObstacle(RectInt rect,bool tall)
        {
            if(tall) Pillars.Add(rect); else Covers.Add(rect);
            foreach(var p in rect.allPositionsWithin)
            { int i=Index(p.x,p.y); floor[i]=false; lowCover[i]=!tall; }
        }
        static bool[] NavigationMask(int radiusKey)
        {
            if(navigationMasks.TryGetValue(radiusKey,out var mask)) return mask;
            mask=new bool[floor.Length]; float radius=radiusKey*.01f;
            int reach=Mathf.CeilToInt(radius/CellSize);
            for(int i=0;i<mask.Length;i++)
            {
                if(!IsFloor(i%GridWidth,i/GridWidth)) continue;
                int x=i%GridWidth,y=i/GridWidth; bool clear=true;
                var centre=ToWorld(new Vector2(x+.5f,y+.5f));
                for(int side=0;side<3;side++)
                {
                    var rim=StairBorder(side);
                    float dx=Mathf.Max(0,Mathf.Max(rim.xMin-centre.x,centre.x-rim.xMax));
                    float dy=Mathf.Max(0,Mathf.Max(rim.yMin-centre.y,centre.y-rim.yMax));
                    if(dx*dx+dy*dy<=radius*radius) { clear=false; break; }
                }
                for(int oy=-reach;oy<=reach && clear;oy++) for(int ox=-reach;ox<=reach;ox++)
                {
                    if(IsFloor(x+ox,y+oy)) continue;
                    float a=Mathf.Max(0,(Mathf.Abs(ox)-.5f)*CellSize),b=Mathf.Max(0,(Mathf.Abs(oy)-.5f)*CellSize);
                    if(a*a+b*b<=radius*radius) { clear=false; break; }
                }
                mask[i]=clear;
            }
            navigationMasks[radiusKey]=mask; return mask;
        }
        static int NearestNode(Vector2 point,bool[] mask)
        {
            int cell=CellAt(point); if(cell>=0 && mask[cell]) return cell;
            float best=float.PositiveInfinity; int result=-1;
            for(int i=0;i<mask.Length;i++)
            {
                if(!mask[i]) continue;
                float distance=(ToWorld(new Vector2(i%GridWidth+.5f,i/GridWidth+.5f))-point).sqrMagnitude;
                if(distance<best) { result=i; best=distance; }
            }
            return result;
        }
        // Reverse breadth-first fields are shared by destination and body radius.
        // Local physics navigation remains responsible for connecting the body to cell centres.
        public static bool TryWaypoint(Vector2 from,Vector2 goal,float radius,out Vector2 point)
        {
            point=from; int radiusKey=Mathf.CeilToInt(radius*100); var mask=NavigationMask(radiusKey);
            int start=NearestNode(from,mask),end=NearestNode(goal,mask);
            if(start<0 || end<0) return false;
            if(start!=CellAt(from))
            { point=ToWorld(new Vector2(start%GridWidth+.5f,start/GridWidth+.5f)); return true; }
            var next=RouteField(end,radiusKey,mask);
            if(next[start]<0) return false;
            int target=next[start]; point=ToWorld(new Vector2(target%GridWidth+.5f,target/GridWidth+.5f));
            if(start==end && end==CellAt(goal)) point=goal;
            return true;
        }
        static int[] RouteField(int end,int radiusKey,bool[] mask)
        {
            long key=(long)radiusKey*floor.Length+end;
            if(!routes.TryGetValue(key,out var next))
            {
                if(routes.Count>=32) routes.Clear();
                next=new int[floor.Length]; for(int i=0;i<next.Length;i++) next[i]=-1;
                var queue=new Queue<int>(); queue.Enqueue(end); next[end]=end;
                while(queue.Count>0)
                {
                    int current=queue.Dequeue(),x=current%GridWidth,y=current/GridWidth;
                    for(int d=0;d<4;d++)
                    {
                        int n=Index(x+dx[d],y+dy[d]); if(n<0 || !mask[n] || next[n]>=0) continue;
                        next[n]=current; queue.Enqueue(n);
                    }
                }
                routes[key]=next;
            }
            return next;
        }
        public static float TravelDistance(Vector2 from,Vector2 goal)
        {
            var mask=NavigationMask(44); int start=NearestNode(from,mask),end=NearestNode(goal,mask);
            if(start<0 || end<0) return float.PositiveInfinity;
            var next=RouteField(end,44,mask); if(next[start]<0) return float.PositiveInfinity;
            float distance=Vector2.Distance(from,ToWorld(new Vector2(start%GridWidth+.5f,start/GridWidth+.5f)))+
                Vector2.Distance(goal,ToWorld(new Vector2(end%GridWidth+.5f,end/GridWidth+.5f)));
            int current=start;
            for(int guard=0;current!=end && guard<floor.Length;guard++) { current=next[current]; distance+=CellSize; }
            return current==end ? distance : float.PositiveInfinity;
        }
    }
}
