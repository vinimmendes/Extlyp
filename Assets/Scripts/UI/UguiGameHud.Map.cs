using UnityEngine;
using UnityEngine.UI;
using CyberExtraction.Match;
using Text=TMPro.TextMeshProUGUI;
namespace CyberExtraction.UI
{
    public sealed partial class UguiGameHud
    {
        Rect ExpandedMap()
        {
            float u=HudMetrics.Unit,top=12*u;
            float aspect=(float)MapBuilder.Width/Mathf.Max(1,MapBuilder.Height);
            float h=Mathf.Max(24*u,UiLayout.Height-top-12*u-168*u);
            float w=Mathf.Min(UiLayout.Width-24*u,Mathf.Max(176*u,h*aspect+56*u));
            var gear=HudLayout.HitBounds(HudControl.Menu);
            // With the default gear on the right, the expanded map sits beside
            // it. A custom central gear instead reserves a strip above the map.
            float beside=2*(gear.x-8*u-UiLayout.Width/2);
            if(beside>=152*u) w=Mathf.Min(w,beside);
            else { top=gear.yMax+8*u; h=Mathf.Max(24*u,UiLayout.Height-top-12*u-168*u); w=Mathf.Min(UiLayout.Width-24*u,Mathf.Max(176*u,h*aspect+56*u)); }
            h=Mathf.Min(h,(w-56*u)/aspect);
            return new Rect((UiLayout.Width-w)/2,top,w,h+168*u);
        }
        void RefreshMap(bool edit)
        {
            var n=elements[HudControl.Minimap]; if(!n.Root.gameObject.activeSelf) return;
            float u=HudMetrics.Unit; var size=n.Previous.size; bool full=Minimap.IsExpanded && !edit;
            mapExitList.gameObject.SetActive(full);
            Place((RectTransform)mapExitList.transform,new Rect(4*u,size.y-56*u,size.x-8*u,48*u));
            mapExitContent.sizeDelta=new Vector2(MapBuilder.ExitCount*128*u-8*u,48*u);
            if(full && !mapWasExpanded)
            {
                float target=player!=null ? Mathf.Max(0,player.SelectedExit.Value)*128*u : 0;
                mapExitContent.anchoredPosition=new Vector2(-Mathf.Min(target,Mathf.Max(0,mapExitContent.sizeDelta.x-size.x+8*u)),0);
            }
            mapWasExpanded=full;
            var area=new Rect(4*u,full ? 32*u : 18*u,size.x-8*u,size.y-(full ? 120*u : 22*u));
            Place(mapViewport,area);
            float pad=full ? 24*u : 0;
            var plot=new Rect(pad,pad,area.width-2*pad,area.height-2*pad);
            Place(mapImage.rectTransform,plot);
            mapImage.texture=edit ? Texture2D.blackTexture : Minimap.Atlas;
            Vector2 position=player!=null ? (Vector2)player.transform.position : Vector2.zero;
            Rect world;
            if(full) world=new Rect(-MapBuilder.Width/2f,-MapBuilder.Height/2f,MapBuilder.Width,MapBuilder.Height);
            else
            {
                float w=Mathf.Min(64,MapBuilder.Width),h=Mathf.Min(MapBuilder.Height,w*area.height/area.width);
                var center=new Vector2(Mathf.Clamp(position.x,-MapBuilder.Width/2f+w/2,MapBuilder.Width/2f-w/2),Mathf.Clamp(position.y,-MapBuilder.Height/2f+h/2,MapBuilder.Height/2f-h/2));
                world=new Rect(center-new Vector2(w,h)*.5f,new Vector2(w,h));
            }
            mapImage.uvRect=edit ? new Rect(0,0,1,1) : new Rect((world.xMin+MapBuilder.Width/2f)/MapBuilder.Width,(world.yMin+MapBuilder.Height/2f)/MapBuilder.Height,world.width/MapBuilder.Width,world.height/MapBuilder.Height);
            Vector2 Project(Vector2 p)=>new Vector2(plot.x+(p.x-world.xMin)/world.width*plot.width,plot.y+(world.yMax-p.y)/world.height*plot.height);
            for(int i=0;i<sectorLabels.Count;i++)
            {
                bool dungeon=MapBuilder.Current==ExpeditionMap.Dungeon;
                var label=sectorLabels[i]; bool visible=i<(dungeon ? DungeonLayout.SectorNames.Length : LargeMapPlan.Sectors.Length);
                label.gameObject.SetActive(full && visible && plot.width>=300*u); if(!visible) continue;
                label.text=dungeon ? DungeonLayout.SectorNames[i] : LargeMapPlan.Sectors[i].Name;
                var p=Project(dungeon ? DungeonLayout.Rooms[DungeonLayout.SectorLabelRooms[i]].Center : LargeMapPlan.Sectors[i].Center);
                Place(label.rectTransform,new Rect(p.x-55*u,p.y-22*u,110*u,20*u)); label.color=HudTheme.Text; label.enableAutoSizing=false; label.fontSize=14*u;
            }
            var here=Project(position); here.x=Mathf.Clamp(here.x,7*u,area.width-7*u); here.y=Mathf.Clamp(here.y,7*u,area.height-7*u);
            mapPlayer.sprite=RefinedHudArt.Get("Arrow"); mapPlayer.rectTransform.anchoredPosition=new Vector2(here.x,-here.y);
            mapPlayer.rectTransform.sizeDelta=Vector2.one*14*u; mapPlayer.rectTransform.localEulerAngles=new Vector3(0,0,edit ? 0 : -mapHeading); mapPlayer.color=HudTheme.Text;
            for(int i=0;i<exitMarkers.Count;i++)
            {
                var marker=exitMarkers[i]; if(i>=MapBuilder.ExitCount) { marker.gameObject.SetActive(false); exitArtwork[i].gameObject.SetActive(false); continue; }
                var exit=MapBuilder.ExitPosition(i); marker.gameObject.SetActive(!edit && (full || world.Contains(exit)));
                exitArtwork[i].gameObject.SetActive(!edit && (full || world.Contains(exit)));
                var p=Project(exit); float side=18*u;
                // Geographic symbols never move to make room for touch targets.
                // Full-map selection lives in a separate non-overlapping row.
                var markerParent=full ? mapExitContent : mapViewport;
                if(marker.parent!=markerParent) marker.SetParent(markerParent,false);
                marker.anchorMin=marker.anchorMax=marker.pivot=new Vector2(0,1);
                float bw=120*u;
                Place(marker,full ? new Rect(i*128*u,0,bw,48*u) : new Rect(p.x-side/2,p.y-side/2,side,side));
                var button=exitChoiceButtons[i]; button.interactable=full;
                int bit=1<<i; bool known=player!=null && (player.KnownActiveExits.Value&bit)!=0;
                bool closed=player!=null && (player.KnownInactiveExits.Value&bit)!=0;
                var image=exitChoiceImages[i]; image.sprite=full ? RefinedHudArt.Surface(false) : null; image.type=full ? Image.Type.Sliced : Image.Type.Simple;
                image.pixelsPerUnitMultiplier=12/HudMetrics.Dp(4); image.color=full ? Color.white : Color.clear; image.raycastTarget=full;
                var artwork=exitArtwork[i]; artwork.sprite=RefinedHudArt.Get("Exit"); artwork.color=known ? HudTheme.Good : closed ? HudTheme.Danger : HudTheme.Muted;
                if(full)
                {
                    artwork.transform.SetParent(mapViewport,false); artwork.rectTransform.anchorMin=artwork.rectTransform.anchorMax=new Vector2(0,1); artwork.rectTransform.pivot=new Vector2(.5f,.5f);
                    artwork.rectTransform.anchoredPosition=new Vector2(p.x,-p.y);
                }
                else { artwork.transform.SetParent(marker,false); artwork.rectTransform.anchorMin=artwork.rectTransform.anchorMax=artwork.rectTransform.pivot=new Vector2(.5f,.5f); artwork.rectTransform.anchoredPosition=Vector2.zero; }
                artwork.rectTransform.sizeDelta=Vector2.one*18*u;
                var badge=exitMapLabels[i]; badge.gameObject.SetActive(full); badge.text=(i+1).ToString(); badge.fontSize=14*u; badge.color=HudTheme.Text; badge.enableAutoSizing=false; badge.overflowMode=TMPro.TextOverflowModes.Overflow;
                badge.rectTransform.anchorMin=badge.rectTransform.anchorMax=badge.rectTransform.pivot=new Vector2(.5f,.5f);
                badge.rectTransform.anchoredPosition=new Vector2(12*u,0); badge.rectTransform.sizeDelta=new Vector2(16*u,20*u);
                var label=exitChoiceLabels[i]; label.text=full ? (player!=null && player.SelectedExit.Value==i ? "["+(i+1)+"] " : (i+1)+" · ")+MapBuilder.ExitName(i)+" "+(closed ? "×" : known ? "+" : "?") : closed ? "×" : known ? "+" : "?";
                label.color=HudTheme.Text; label.enableAutoSizing=false; label.fontSize=14*u;
                label.raycastTarget=false;
                label.rectTransform.anchorMin=label.rectTransform.anchorMax=new Vector2(.5f,.5f);
                label.rectTransform.pivot=new Vector2(.5f,.5f); label.rectTransform.anchoredPosition=full ? Vector2.zero : new Vector2(8*u,0); label.rectTransform.sizeDelta=full ? new Vector2(bw-8*u,46*u) : new Vector2(14*u,20*u);
                label.textWrappingMode=full ? TMPro.TextWrappingModes.Normal : TMPro.TextWrappingModes.NoWrap;
                label.overflowMode=TMPro.TextOverflowModes.Overflow;
                // Only the marker symbol is enlarged by its existing child fit; labels remain live TMP.
            }
            n.Title.gameObject.SetActive(true); n.Title.text=full ? "N · "+MapBuilder.MapName(MapBuilder.Current).ToUpperInvariant() : "N";
            Place(n.Title.rectTransform,new Rect(4*u,0,size.x-8*u,(full ? 30 : 18)*u)); n.Title.fontSize=14*u;
            n.Title.textWrappingMode=full ? TMPro.TextWrappingModes.Normal : TMPro.TextWrappingModes.NoWrap;
            n.Secondary.gameObject.SetActive(full);
            n.Secondary.text="Saídas · deslize para escolher\n+ "+(MapBuilder.Current==ExpeditionMap.Dungeon ? "Pronta" : "Aberta")+"   × Fechada   ? Incerta";
            Place(n.Secondary.rectTransform,new Rect(4*u,size.y-88*u,size.x-8*u,29*u)); n.Secondary.fontSize=14*u;
            n.Secondary.textWrappingMode=TMPro.TextWrappingModes.Normal;
            n.Icon.gameObject.SetActive(false);
            n.Note.gameObject.SetActive(full && MapBuilder.Current==ExpeditionMap.Large);
            n.Note.text="Vento "+WindName(CyberExtraction.Enemies.SporeEncounter.Wind);
            Place(n.Note.rectTransform,new Rect(4*u,32*u,size.x-8*u,18*u));
        }
        static string WindName(Vector2 wind)=>wind.sqrMagnitude<.001f ? "Calmo" : Mathf.Abs(wind.y)<Mathf.Abs(wind.x)*.4142f ? (wind.x>0 ? "L" : "O") : Mathf.Abs(wind.x)<Mathf.Abs(wind.y)*.4142f ? (wind.y>0 ? "N" : "S") : (wind.y>0 ? "N" : "S")+(wind.x>0 ? "E" : "O");
    }
}
