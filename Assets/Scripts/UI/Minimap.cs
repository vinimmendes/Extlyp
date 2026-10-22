using UnityEngine;
using Unity.Netcode;
using CyberExtraction.Match;
using CyberExtraction.Players;
using CyberExtraction.Enemies;

namespace CyberExtraction.UI
{
    // Local terrain window in the corner, full geography when expanded. No enemy positions.
    [DefaultExecutionOrder(-175)]
    public class Minimap : MonoBehaviour
    {
        bool expanded;
        static Minimap instance;
        Texture2D terrain;
        int terrainVersion=-1;
        bool terrainSomber;
        ExpeditionMap terrainMap;
        readonly MinimapPointer playerPointer=new MinimapPointer(),exitPointer=new MinimapPointer();
        float heading,headingVelocity;
        bool headingReady;
        Rect worldView;
        int handledBack=-1;
        public static bool IsExpanded=>instance!=null && instance.expanded;
        public static bool ConsumedBack=>instance!=null && instance.handledBack==Time.frameCount;
        public static void Toggle() { if(instance!=null && Visible) instance.expanded=!instance.expanded; }
        public static Texture2D Atlas { get { if(instance==null) return null; instance.BuildTerrain(); return instance.terrain; } }
        void Awake() { instance=this; }
        void OnDestroy() { if(instance==this) instance=null; if(terrain!=null) Destroy(terrain); playerPointer.Dispose(); exitPointer.Dispose(); }
        public static Rect CornerBounds=>HudLayout.Bounds(HudControl.Minimap);
        Rect PanelBounds()
        {
            if(!expanded) return CornerBounds;
            float height=UiLayout.Height*.72f,width=height*MapBuilder.Width/MapBuilder.Height;
            return new Rect((UiLayout.Width-width)/2,(UiLayout.Height-height)/2-15,width,height+45);
        }
        static bool Visible=>!HudLayoutEditor.IsOpen && !ExpeditionMenu.IsOpen && !CyberExtraction.Camp.CampController.Active && !GameOptionsMenu.IsOpen &&
            !TestLabPanel.IsOpen && !CampTestPanel.IsOpen && !InventoryWindow.IsOpen && !LockpickWindow.IsOpen &&
            MatchManager.Instance!=null && MapBuilder.Current!=ExpeditionMap.Test;
        public static bool ContainsScreenPoint(Vector2 point)=>instance!=null && Visible &&
            (instance.expanded || CornerBounds.Contains(UiLayout.Point(point)));
        void Update()
        {
            if(!Visible) { expanded=false; headingReady=false; return; }
            var manager=NetworkManager.Singleton;
            var player=manager!=null && manager.SpawnManager!=null ? manager.SpawnManager.GetLocalPlayerObject() : null;
            if(player!=null)
            {
                float target=90-player.transform.eulerAngles.z;
                if(!headingReady) { heading=target; headingReady=true; }
                else heading=Mathf.SmoothDampAngle(heading,target,ref headingVelocity,.12f,Mathf.Infinity,Time.unscaledDeltaTime);
            }
            if(expanded && GameInput.BackPressed) { expanded=false; handledBack=Time.frameCount; return; }
            if(GameInput.MapPressed) expanded=!expanded;
        }
        Vector2 Project(Vector2 point,Rect area)=>new Vector2(area.x+(point.x-worldView.xMin)/worldView.width*area.width,
            area.y+(worldView.yMax-point.y)/worldView.height*area.height);
        static void PaintRoute(Color[] pixels,int width,int height,Vector2 a,Vector2 b,Color color,float thickness)
        {
            var delta=b-a; float radius=thickness*.5f;
            int x0=Mathf.Max(0,Mathf.FloorToInt(Mathf.Min(a.x,b.x)-radius-1)),x1=Mathf.Min(width-1,Mathf.CeilToInt(Mathf.Max(a.x,b.x)+radius+1));
            int y0=Mathf.Max(0,Mathf.FloorToInt(Mathf.Min(a.y,b.y)-radius-1)),y1=Mathf.Min(height-1,Mathf.CeilToInt(Mathf.Max(a.y,b.y)+radius+1));
            for(int y=y0;y<=y1;y++) for(int x=x0;x<=x1;x++)
            {
                var point=new Vector2(x+.5f,y+.5f);
                float t=delta.sqrMagnitude>0 ? Mathf.Clamp01(Vector2.Dot(point-a,delta)/delta.sqrMagnitude) : 0;
                float alpha=Mathf.Clamp01(radius+.5f-Vector2.Distance(point,a+delta*t))*color.a;
                int index=y*width+x; var result=Color.Lerp(pixels[index],color,alpha); result.a=pixels[index].a; pixels[index]=result;
            }
        }
        void BuildTerrain()
        {
            if(MapBuilder.Current==ExpeditionMap.Dungeon) { BuildDungeonTerrain(); return; }
            if(terrain!=null && terrainMap==MapBuilder.Current && terrainVersion==LargeMapPlan.GeometryVersion && terrainSomber==HudTheme.Somber) return;
            if(terrain!=null) Destroy(terrain);
            const int width=256,height=384; var pixels=new Color[width*height];
            for(int y=0;y<height;y++) for(int x=0;x<width;x++)
            {
                var point=new Vector2((x+.5f)/width*MapBuilder.Width-MapBuilder.Width/2,(y+.5f)/height*MapBuilder.Height-MapBuilder.Height/2);
                float forest=Mathf.PerlinNoise((point.x+613)*.047f,(point.y+811)*.047f);
                float grain=Mathf.PerlinNoise(x*.57f,y*.57f),clearing=0;
                foreach(var site in MapBuilder.Sites) clearing=Mathf.Max(clearing,1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(10,32,Vector2.Distance(point,site))));
                float light=(forest>.51f ? .055f : .09f)+grain*.004f+clearing*.06f;
                pixels[y*width+x]=new Color(light*1.02f,light,light*.88f,.96f);
            }
            // Bake the actual route geometry into the same atlas as the terrain.
            // The viewport crops one texture; roads cannot move or rotate the UI clip.
            var routeColor=HudTheme.Somber ? new Color(.23f,.225f,.195f,.8f) : new Color(.25f,.34f,.27f,.85f);
            foreach(var route in LargeMapPlan.Routes) for(int p=1;p<route.Points.Length;p++)
            {
                var a=route.Points[p-1]; var b=route.Points[p];
                a=new Vector2((a.x/MapBuilder.Width+.5f)*width,(a.y/MapBuilder.Height+.5f)*height);
                b=new Vector2((b.x/MapBuilder.Width+.5f)*width,(b.y/MapBuilder.Height+.5f)*height);
                PaintRoute(pixels,width,height,a,b,routeColor,Mathf.Max(1,route.Width/MapBuilder.Width*width));
            }
            terrain=new Texture2D(width,height,TextureFormat.RGBA32,false) {name="Known forest cartography",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
            terrain.SetPixels(pixels); terrain.Apply(false,true); terrainVersion=LargeMapPlan.GeometryVersion; terrainSomber=HudTheme.Somber; terrainMap=MapBuilder.Current;
        }
        void BuildDungeonTerrain()
        {
            if(terrain!=null && terrainMap==ExpeditionMap.Dungeon && terrainVersion==DungeonLayout.GeometryVersion && terrainSomber==HudTheme.Somber) return;
            if(terrain!=null) Destroy(terrain);
            const int size=256; var pixels=new Color[size*size];
            for(int y=0;y<size;y++) for(int x=0;x<size;x++)
            {
                int cx=x*DungeonLayout.GridWidth/size,cy=y*DungeonLayout.GridHeight/size;
                bool floor=DungeonLayout.IsFloor(cx,cy);
                int sector=DungeonLayout.SectorAt(cx,cy);
                bool wall=!floor && (DungeonLayout.IsFloor(Mathf.Max(0,cx-1),cy) || DungeonLayout.IsFloor(Mathf.Min(DungeonLayout.GridWidth-1,cx+1),cy) || DungeonLayout.IsFloor(cx,Mathf.Max(0,cy-1)) || DungeonLayout.IsFloor(cx,Mathf.Min(DungeonLayout.GridHeight-1,cy+1)));
                pixels[y*size+x]=floor ? (DungeonLayout.IsCanalBridge(cx,cy) ? new Color(.60f,.46f,.29f) : sector==4 ? new Color(.23f,.34f,.34f) : HudTheme.Somber ? new Color(.31f,.33f,.25f) : new Color(.36f,.45f,.38f)) : DungeonLayout.IsWater(cx,cy) ? new Color(.10f,.21f,.28f) : wall ? new Color(.55f,.51f,.39f) : new Color(.045f,.065f,.045f);
            }
            terrain=new Texture2D(size,size,TextureFormat.RGBA32,false) { name="Catacumbas — planta",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave };
            terrain.SetPixels(pixels); terrain.Apply(false,true); terrainMap=ExpeditionMap.Dungeon; terrainSomber=HudTheme.Somber; terrainVersion=DungeonLayout.GeometryVersion;
        }
        void OnGUI()
        {
            if(UguiGameHud.Active) return;
            if(!Visible) return;
            var nm=NetworkManager.Singleton;
            if(nm==null || !nm.IsConnectedClient || nm.SpawnManager==null) return;
            var player=nm.SpawnManager.GetLocalPlayerObject(); if(player==null) return;
            var inventory=player.GetComponent<PlayerInventory>();
            using var frame=UiLayout.Begin();
            var panel=PanelBounds(); HudArt.Panel(panel);
            var area=expanded ? new Rect(panel.x+7,panel.y+26,panel.width-14,panel.height-54) : new Rect(panel.x+3,panel.y+3,panel.width-6,panel.height-6);
            Vector2 position=player.transform.position;
            if(expanded) worldView=new Rect(-MapBuilder.Width/2,-MapBuilder.Height/2,MapBuilder.Width,MapBuilder.Height);
            else
            {
                float viewWidth=Mathf.Min(230,MapBuilder.Width),viewHeight=Mathf.Min(MapBuilder.Height,viewWidth*area.height/area.width);
                var center=new Vector2(Mathf.Clamp(position.x,-MapBuilder.Width/2+viewWidth/2,MapBuilder.Width/2-viewWidth/2),
                    Mathf.Clamp(position.y,-MapBuilder.Height/2+viewHeight/2,MapBuilder.Height/2-viewHeight/2));
                worldView=new Rect(center.x-viewWidth/2,center.y-viewHeight/2,viewWidth,viewHeight);
            }
            var evt=Event.current;
            if(evt.type==EventType.MouseUp && evt.button==0 && panel.Contains(evt.mousePosition))
            {
                int selected=-1; float nearest=22;
                if(expanded && inventory!=null) for(int i=0;i<MapBuilder.ExitCount;i++)
                { float distance=Vector2.Distance(Project(MapBuilder.ExitPosition(i),area),evt.mousePosition); if(distance<nearest) { nearest=distance; selected=i; } }
                if(selected>=0) inventory.RequestExitDestination(selected); else expanded=!expanded;
                evt.Use();
            }
            BuildTerrain();
            GUI.BeginGroup(area);
            var localArea=new Rect(0,0,area.width,area.height);
            var uv=new Rect((worldView.xMin+MapBuilder.Width/2)/MapBuilder.Width,(worldView.yMin+MapBuilder.Height/2)/MapBuilder.Height,worldView.width/MapBuilder.Width,worldView.height/MapBuilder.Height);
            var old=GUI.color; GUI.color=Color.white; GUI.DrawTextureWithTexCoords(localArea,terrain,uv,true); GUI.color=old;
            foreach(var site in MapBuilder.Sites)
            { var p=Project(site,localArea); HudTheme.Fill(new Rect(p.x-2,p.y-2,4,4),new Color(.34f,.31f,.24f)); }
            if(expanded && MapBuilder.Current==ExpeditionMap.Dungeon)
            {
                for(int i=0;i<DungeonLayout.SectorLabelRooms.Length;i++)
                { var p=Project(DungeonLayout.Rooms[DungeonLayout.SectorLabelRooms[i]].Center,localArea); GUI.Label(new Rect(p.x-55,p.y-25,110,20),DungeonLayout.SectorNames[i],HudTheme.Label(11,TextAnchor.MiddleCenter)); }
            }
            if(expanded && MapBuilder.Current==ExpeditionMap.Large) foreach(var sector in LargeMapPlan.Sectors)
            { var p=Project(sector.Center,localArea); GUI.Label(new Rect(p.x-55,p.y-25,110,20),sector.Name,HudTheme.Label(12,TextAnchor.MiddleCenter)); }
            // Geographic exits stay on the atlas; use the full map for distant exits.
            for(int i=0;i<MapBuilder.ExitCount;i++)
            {
                var positionOfExit=MapBuilder.ExitPosition(i); bool selected=inventory!=null && inventory.SelectedExit.Value==i;
                if(!expanded && !worldView.Contains(positionOfExit)) continue;
                int bit=1<<i; bool known=inventory!=null && (inventory.KnownActiveExits.Value&bit)!=0;
                bool unavailable=inventory!=null && (inventory.KnownInactiveExits.Value&bit)!=0;
                var p=Project(positionOfExit,localArea); bool outside=!worldView.Contains(positionOfExit);
                HudArt.Icon(new Rect(p.x-8,p.y-10,16,20),HudSymbol.Exit,known ? HudTheme.Good : MapBuilder.Current==ExpeditionMap.Dungeon ? HudTheme.Accent : unavailable ? HudTheme.Danger : HudTheme.Text);
                if(expanded || unavailable || !known)
                    GUI.Label(new Rect(p.x+7,p.y-9,13,18),MapBuilder.Current==ExpeditionMap.Dungeon ? (known ? "+" : (i+1).ToString()) : known ? "+" : unavailable ? "×" : "?",HudTheme.Label(12,TextAnchor.MiddleCenter));
                if(outside && selected) exitPointer.Draw(new Rect(p.x-4,p.y+9,8,8),HudTheme.Accent,
                    90-Mathf.Atan2(positionOfExit.y-position.y,positionOfExit.x-position.x)*Mathf.Rad2Deg);
            }
            var here=Project(position,localArea);
            here.x=Mathf.Clamp(here.x,10,localArea.width-10); here.y=Mathf.Clamp(here.y,10,localArea.height-10);
            playerPointer.Draw(new Rect(here.x-10,here.y-10,20,20),HudTheme.Text,heading);
            GUI.EndGroup();
            if(expanded)
            {
                GUI.Label(new Rect(panel.x,panel.y+2,panel.width,22),"N · "+MapBuilder.MapName(MapBuilder.Current).ToUpperInvariant(),HudTheme.Label(15,TextAnchor.MiddleCenter));
                GUI.Label(new Rect(panel.x,panel.yMax-25,panel.width,20),MapBuilder.Current==ExpeditionMap.Dungeon ? "1 Escada · 2 Poço · 3 Canal · + Pronta" : "+ Aberta   × Fechada   ? Incerta",HudTheme.Label(12,TextAnchor.MiddleCenter));
            }
            else
            {
                HudArt.Icon(new Rect(panel.center.x-4,panel.y+5,8,8),HudSymbol.Arrow,HudTheme.Text);
                GUI.Label(new Rect(panel.center.x-9,panel.y+14,18,18),"N",HudTheme.Label(12,TextAnchor.MiddleCenter));
                if(MapBuilder.Current==ExpeditionMap.Dungeon)
                { GUI.Label(new Rect(panel.x+2,panel.yMax-24,panel.width-4,22),"Catacumbas",HudTheme.Label(10,TextAnchor.MiddleCenter)); return; }
                var wind=SporeEncounter.Wind;
                string direction=wind.sqrMagnitude<.001f ? "Calmo" : Mathf.Abs(wind.y)<Mathf.Abs(wind.x)*.4142f ? (wind.x>0 ? "L" : "O") :
                    Mathf.Abs(wind.x)<Mathf.Abs(wind.y)*.4142f ? (wind.y>0 ? "N" : "S") : (wind.y>0 ? "N" : "S")+(wind.x>0 ? "E" : "O");
                HudArt.Icon(new Rect(panel.xMax-40,panel.yMax-44,28,20),HudSymbol.Wind,HudTheme.Muted);
                GUI.Label(new Rect(panel.xMax-46,panel.yMax-26,40,25),"Vento\n"+direction,HudTheme.Label(10,TextAnchor.MiddleCenter));
            }
        }
    }
}
