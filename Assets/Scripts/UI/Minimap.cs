using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using CyberExtraction.Core;
using CyberExtraction.Match;

namespace CyberExtraction.UI
{
    // Mapa esquematico: terreno conhecido, saidas e somente o jogador local.
    public class Minimap : MonoBehaviour
    {
        bool expanded;
        static Minimap instance;
        void Awake() { instance=this; }
        void OnDestroy() { if(instance==this) instance=null; }
        public static Rect CornerBounds
        {
            get
            {
                float height = 145;
                float width = height * MapBuilder.Width / MapBuilder.Height;
                return new Rect(UiLayout.Width-width-14,70,width,height+45);
            }
        }
        Rect PanelBounds()
        {
            if(!expanded) return CornerBounds;
            float height=Mathf.Min(UiLayout.Height*.72f,UiLayout.Width*.72f*MapBuilder.Height/MapBuilder.Width),width=height*MapBuilder.Width/MapBuilder.Height;
            return new Rect((UiLayout.Width-width)/2,(UiLayout.Height-height)/2-20,width,height+55);
        }
        static bool Visible=>!ExpeditionMenu.IsOpen && !Camp.CampController.Active && !GameOptionsMenu.IsOpen && !TestLabPanel.IsOpen && !InventoryWindow.IsOpen && !LockpickWindow.IsOpen && MatchManager.Instance!=null && MapBuilder.Current==ExpeditionMap.Large;
        public static bool ContainsScreenPoint(Vector2 point) => instance!=null && Visible &&
            instance.PanelBounds().Contains(UiLayout.Point(point));
        void Update()
        {
            if(Keyboard.current!=null && CyberExtraction.Players.GameInput.MapPressed) expanded=!expanded;
            if(MapBuilder.Current!=ExpeditionMap.Large || MatchManager.Instance==null) expanded=false;
        }
        Vector2 Project(Vector2 point,Rect area) => new Vector2(area.x+(point.x/MapBuilder.Width+.5f)*area.width,
            area.y+(.5f-point.y/MapBuilder.Height)*area.height);
        void Dot(Vector2 point,Rect area,Color color,float size)
        {
            Vector2 position=Project(point,area); var old=GUI.color; GUI.color=color;
            GUI.DrawTexture(new Rect(position.x-size/2,position.y-size/2,size,size),Texture2D.whiteTexture); GUI.color=old;
        }
        void Route(Vector2 from,Vector2 to,Rect area)
        {
            Vector2 a=Project(from,area),b=Project(to,area),delta=b-a;
            var matrix=GUI.matrix; var color=GUI.color;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg,a);
            GUI.color=new Color(.40f,.39f,.26f);
            GUI.DrawTexture(new Rect(a.x,a.y-1,delta.magnitude,expanded ? 2 : 1),Texture2D.whiteTexture);
            GUI.matrix=matrix; GUI.color=color;
        }
        void OnGUI()
        {
            if(!Visible) return;
            using var themeFrame=UiLayout.Begin();
            if(LockpickWindow.IsOpen || TestLabPanel.IsOpen) return;
            var nm=NetworkManager.Singleton;
            if(nm==null || !nm.IsClient || !nm.IsConnectedClient || MapBuilder.Current!=ExpeditionMap.Large || InventoryWindow.IsOpen || MatchManager.Instance==null) return;
            var player=nm.SpawnManager.GetLocalPlayerObject(); if(player==null) return;
            var oldMatrix=GUI.matrix; GUI.matrix=UiLayout.Matrix;
            Rect panel=PanelBounds();
            GUI.Box(new Rect(panel.x-8,panel.y-8,panel.width+16,panel.height+16),"");
            Rect area=new Rect(panel.x,panel.y+25,panel.width,panel.height-55);
            var oldColor=GUI.color; GUI.color=new Color(.11f,.20f,.14f); GUI.DrawTexture(area,Texture2D.whiteTexture); GUI.color=oldColor;
            foreach(var route in LargeMapPlan.Routes)
                for(int p=1;p<route.Points.Length;p++) Route(route.Points[p-1],route.Points[p],area);
            for(int i=0;i<MapBuilder.Sites.Length;i++)
            {
                Dot(MapBuilder.Sites[i],area,new Color(.6f,.5f,.35f),expanded ? 5 : 2);
            }
            Dot(new Vector2(190,-130),area,new Color(.2f,.45f,.65f),expanded ? 14 : 5);
            if(expanded) foreach(var sector in LargeMapPlan.Sectors)
            {
                var p=Project(sector.Center,area);
                GUI.Label(new Rect(p.x-48,p.y-25,96,22),sector.Name,HudTheme.Label(13,TextAnchor.MiddleCenter));
            }
            foreach(var spawn in MapBuilder.SpawnPoints) Dot(spawn,area,Color.gray,expanded ? 7 : 4);
            var inventory=player.GetComponent<CyberExtraction.Players.PlayerInventory>();
            for(int i=0;i<LargeMapPlan.Exits.Length;i++)
            {
                int bit=1<<i;
                bool known=inventory!=null && (inventory.KnownActiveExits.Value&bit)!=0;
                bool unavailable=inventory!=null && (inventory.KnownInactiveExits.Value&bit)!=0;
                var exit=LargeMapPlan.Exits[i];
                Color color=known ? HudTheme.Good : unavailable ? HudTheme.Danger : HudTheme.Muted;
                Dot(exit.Position,area,color,expanded ? 10 : 6);
                if(expanded)
                {
                    var p=Project(exit.Position,area);
                    GUI.Label(new Rect(p.x-10,p.y-13,20,26),known ? "+" : unavailable ? "×" : "?",HudTheme.Label(17,TextAnchor.MiddleCenter));
                }
            }
            Vector2 position=player.transform.position;
            Dot(position,area,Color.white,expanded ? 10 : 7);
            float angle=player.transform.eulerAngles.z*Mathf.Deg2Rad;
            var facing=position+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*(expanded ? 15 : 25);
            Dot(facing,area,Color.yellow,expanded ? 5 : 3);
            var style=new GUIStyle(GUI.skin.label) { fontSize=expanded ? Mathf.Clamp((int)(panel.width/12),13,22) : 15,alignment=TextAnchor.MiddleCenter,wordWrap=true };
            GUI.Label(new Rect(panel.x,panel.y-2,panel.width,24),expanded ? "N · FLORESTA VELHA" : "Mapa [M]",style);
            GUI.Label(new Rect(panel.x,panel.yMax-30,panel.width,30),expanded ? "+ Aberta   × Fechada   ? Incerta" : "? Incerta",HudTheme.Label(14,TextAnchor.MiddleCenter));
            var evt=Event.current;
            if(evt.type==EventType.MouseUp && evt.button==0 && panel.Contains(evt.mousePosition))
            {
                int selected=-1; float nearest=22;
                if(expanded && inventory!=null)
                    for(int i=0;i<LargeMapPlan.Exits.Length;i++)
                    {
                        float distance=Vector2.Distance(Project(LargeMapPlan.Exits[i].Position,area),evt.mousePosition);
                        if(distance<nearest) { nearest=distance; selected=i; }
                    }
                if(selected>=0) inventory.RequestExitDestination(selected);
                else expanded=!expanded;
                evt.Use();
            }
            GUI.matrix=oldMatrix;
        }
    }
}
