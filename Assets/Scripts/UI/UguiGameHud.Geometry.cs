using UnityEngine;
using UnityEngine.UI;
using Text=TMPro.TextMeshProUGUI;
using Element=CyberExtraction.UI.UguiHudElement;
namespace CyberExtraction.UI
{
    public sealed partial class UguiGameHud
    {
        static void Place(RectTransform r,Rect box)
        { r.anchoredPosition=new Vector2(box.x,-box.y); r.sizeDelta=box.size; }
        static bool Circle(HudControl c)=>c==HudControl.Move || c==HudControl.Aim || c==HudControl.Fire || c==HudControl.Heal || c==HudControl.Swap || c==HudControl.Reload || c==HudControl.Backpack || c==HudControl.Sprint || c==HudControl.Menu;
        void Geometry(Element n,Rect r)
        {
            var hit=HudLayout.HitBounds(n.Control);
            if(n.Control==HudControl.Minimap && Minimap.IsExpanded && !HudLayoutEditor.IsOpen) hit=r;
            Place(n.Root,hit); n.Visual.anchoredPosition=new Vector2(r.x-hit.x,-(r.y-hit.y));
            n.Visual.sizeDelta=r.size; n.Visual.localScale=Vector3.one; n.Previous=r;
            n.Background.RasterArtwork=true; n.Background.sprite=n.Control==HudControl.Move ? RefinedHudArt.Get("Move") : RefinedHudArt.Surface(Circle(n.Control),n.Control==HudControl.Fire);
            n.Background.type=Circle(n.Control) ? Image.Type.Simple : Image.Type.Sliced;
            n.Background.pixelsPerUnitMultiplier=12/HudMetrics.Dp(4); n.Background.color=Color.white;
            Place(n.Background.rectTransform,new Rect(Vector2.zero,r.size));
            n.Background.SetMaterialDirty(); n.Background.SetVerticesDirty();
            n.InnerRing.gameObject.SetActive(false); n.Knob.gameObject.SetActive(n.Control==HudControl.Move);
            n.Knob.sprite=RefinedHudArt.Get("Knob"); n.Knob.color=Color.white;
            n.Knob.rectTransform.pivot=new Vector2(0,1);
            if(n.Icon is HudActionIconGraphic native) native.RasterArtwork=true;
            n.Icon.type=Image.Type.Simple; n.Icon.preserveAspect=true;
            n.Icon.rectTransform.anchorMin=n.Icon.rectTransform.anchorMax=new Vector2(0,1);
            n.Icon.rectTransform.pivot=new Vector2(0,1); n.Icon.rectTransform.localEulerAngles=Vector3.zero;
            n.Icon.sprite=RefinedHudArt.Icon(n.Control==HudControl.Clock ? HudSymbol.Arrow : Symbol(n.Control));
            n.Icon.color=HudTheme.Text; n.Icon.SetMaterialDirty(); n.Icon.SetVerticesDirty();
            n.Icon.gameObject.SetActive(n.Control!=HudControl.Health && n.Control!=HudControl.Stamina && n.Control!=HudControl.Move && n.Control!=HudControl.Minimap);
            n.Title.gameObject.SetActive(false); n.Main.gameObject.SetActive(false); n.Secondary.gameObject.SetActive(false); n.Note.gameObject.SetActive(false);
            n.Bar.gameObject.SetActive(n.Control==HudControl.Health || n.Control==HudControl.Stamina || n.Control==HudControl.Interaction);
            n.BarTrack.gameObject.SetActive(n.Bar.gameObject.activeSelf);
            foreach(var t in new[] {n.Title,n.Main,n.Secondary,n.Note})
            {
                t.font=t==n.Main ? bold : regular; t.fontWeight=TMPro.FontWeight.Regular;
                t.enableAutoSizing=false; t.fontSize=HudMetrics.Dp(t==n.Main ? 22 : 14);
                t.margin=Vector4.zero; t.overflowMode=TMPro.TextOverflowModes.Overflow;
                t.textWrappingMode=TMPro.TextWrappingModes.NoWrap;
                t.alignment=TMPro.TextAlignmentOptions.Center; t.color=HudTheme.Text;
            }
            n.Button.targetGraphic=n.Background; n.Button.transition=Selectable.Transition.ColorTint;
            var colors=ColorBlock.defaultColorBlock; colors.normalColor=Color.white; colors.highlightedColor=colors.selectedColor=new Color(1,1,1,1);
            colors.pressedColor=new Color(.66f,.68f,.62f,1); colors.disabledColor=new Color(.47f,.49f,.45f,.8f); colors.fadeDuration=.08f; n.Button.colors=colors;
            float u=HudMetrics.Unit,w=r.width/u,h=r.height/u;
            void Box(RectTransform t,float x,float y,float width,float height)=>Place(t,new Rect(x*u,y*u,width*u,height*u));
            Box(n.Icon.rectTransform,w*.22f,h*.22f,w*.56f,h*.56f);
            if(n.Control==HudControl.Health || n.Control==HudControl.Stamina)
            {
                n.Title.gameObject.SetActive(true); n.Main.gameObject.SetActive(true);
                n.Title.fontSize=n.Main.fontSize=16*u;
                n.Title.alignment=TMPro.TextAlignmentOptions.MidlineLeft; n.Main.alignment=TMPro.TextAlignmentOptions.MidlineRight;
                Box(n.Title.rectTransform,6,0,55,21); Box(n.Main.rectTransform,58,0,w-64,21);
            }
            else if(n.Control==HudControl.Clock)
            {
                n.Main.gameObject.SetActive(true); n.Secondary.gameObject.SetActive(true);
                Box(n.Main.rectTransform,4,1,w-8,28); Box(n.Secondary.rectTransform,w/2-3,29,w/2-3,20);
                // Rotation around the arrow centre, independent of image dimensions.
                n.Icon.rectTransform.pivot=new Vector2(.5f,.5f); Box(n.Icon.rectTransform,w/2-17,39,12,12);
            }
            else if(n.Control==HudControl.Weapon)
            {
                n.Main.gameObject.SetActive(true); n.Secondary.gameObject.SetActive(true);
                Box(n.Icon.rectTransform,8,7,44,26); Box(n.Main.rectTransform,57,-1,w-63,25); Box(n.Secondary.rectTransform,57,22,w-63,17);
            }
            else if(n.Control==HudControl.Heal)
            {
                n.Title.gameObject.SetActive(true); n.Title.font=bold; n.Title.fontSize=14*u;
                Box(n.Icon.rectTransform,w*.27f,7,w*.46f,h*.46f); Box(n.Title.rectTransform,0,h-21,w,18);
            }
            else if(n.Control==HudControl.Fire)
            {
                n.Title.gameObject.SetActive(true); n.Title.font=bold; n.Title.fontSize=12*u;
                Box(n.Icon.rectTransform,w*.25f,h*.16f,w*.5f,h*.46f); Box(n.Title.rectTransform,5,h*.66f,w-10,18);
            }
            else if(n.Control==HudControl.Food || n.Control==HudControl.Spores)
            {
                n.Title.gameObject.SetActive(true); Box(n.Icon.rectTransform,3,3,14,14);
                n.Title.alignment=TMPro.TextAlignmentOptions.MidlineLeft; Box(n.Title.rectTransform,21,0,w-24,h);
            }
            else if(n.Control==HudControl.Interaction)
            {
                n.Title.gameObject.SetActive(true); Box(n.Icon.rectTransform,8,(h-24)/2,24,24);
                n.Title.textWrappingMode=TMPro.TextWrappingModes.Normal; Box(n.Title.rectTransform,38,3,w-45,h-10);
            }
            if(n.Control==HudControl.Heal || n.Control==HudControl.Reload)
            {
                if(n.Wait==null)
                {
                    var go=new GameObject("Cooldown",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
                    go.transform.SetParent(n.Visual,false); go.transform.SetSiblingIndex(n.Background.transform.GetSiblingIndex()+1);
                    n.Wait=go.GetComponent<Image>(); n.Wait.rectTransform.anchorMin=n.Wait.rectTransform.anchorMax=n.Wait.rectTransform.pivot=new Vector2(0,1);
                    n.Wait.raycastTarget=false; n.Wait.type=Image.Type.Filled; n.Wait.fillMethod=Image.FillMethod.Radial360;
                    n.Wait.fillOrigin=2; n.Wait.fillClockwise=true;
                    n.Wait.gameObject.SetActive(false);
                }
                n.Wait.sprite=RefinedHudArt.Get("Move"); n.Wait.color=new Color(0,0,0,.65f); Place(n.Wait.rectTransform,new Rect(Vector2.zero,r.size));
            }
        }
        void CompactClockContext(Element clock)
        {
            float u=HudMetrics.Unit; var size=clock.Visual.sizeDelta; bool hasNote=!string.IsNullOrEmpty(clock.Note.text);
            // The note shares the clock surface and grows down only while contextual.
            clock.Note.textWrappingMode=TMPro.TextWrappingModes.Normal;
            float height=hasNote ? clock.Note.GetPreferredValues(clock.Note.text,size.x-12*u,0).y+8*u : 0;
            clock.Note.gameObject.SetActive(hasNote); Place(clock.Note.rectTransform,new Rect(6*u,size.y,size.x-12*u,height));
            Place(clock.Background.rectTransform,new Rect(0,0,size.x,size.y+height));
        }
        static HudSymbol Symbol(HudControl c)=>c==HudControl.Sprint ? HudSymbol.Sprint : c==HudControl.Food ? HudSymbol.Food : c==HudControl.Spores ? HudSymbol.Spores : c==HudControl.Heal ? HudSymbol.Heal : c==HudControl.Swap ? HudSymbol.Swap : c==HudControl.Reload ? HudSymbol.Ammo : c==HudControl.Backpack ? HudSymbol.Backpack : c==HudControl.Fire ? HudSymbol.Bullet : c==HudControl.Aim ? HudSymbol.Aim : c==HudControl.Menu ? HudSymbol.Menu : c==HudControl.Weapon ? HudSymbol.Pistol : HudSymbol.Chest;
    }
}
