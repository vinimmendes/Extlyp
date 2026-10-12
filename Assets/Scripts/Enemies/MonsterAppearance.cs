using UnityEngine;
using CyberExtraction.Core;
using CyberExtraction.Combat;
using CyberExtraction.UI;

namespace CyberExtraction.Enemies
{
    // Same geometry for common loot chests and the unopened mimic.
    public static class ChestAppearance
    {
        public static GameObject Build(Transform parent, Color wood)
        {
            var root = new GameObject("ChestAppearance"); root.transform.SetParent(parent,false);
            root.transform.localScale = new Vector3(1/parent.lossyScale.x,1/parent.lossyScale.y,1);
            Part(root.transform,"Wood",SpriteFactory.Square,Vector2.zero,new Vector2(1.5f,1.2f),wood,5);
            Part(root.transform,"Lid",SpriteFactory.Square,new Vector2(0,.25f),new Vector2(1.5f,.15f),wood*.8f,6);
            foreach (float x in new[] { -.5f,.5f }) Part(root.transform,"Band",SpriteFactory.Square,new Vector2(x,0),new Vector2(.12f,1.2f),Color.gray,6);
            Part(root.transform,"Lock",SpriteFactory.Square,Vector2.zero,new Vector2(.2f,.25f),new Color(.65f,.65f,.6f),7);
            return root;
        }
        public static SpriteRenderer Part(Transform parent,string name,Sprite sprite,Vector2 pos,Vector2 size,Color color,int order=6,float angle=0)
        {
            var go=new GameObject(name); go.transform.SetParent(parent,false);
            go.transform.localPosition=pos; go.transform.localScale=new Vector3(size.x,size.y,1); go.transform.localRotation=Quaternion.Euler(0,0,angle);
            var sr=go.AddComponent<SpriteRenderer>(); SpriteFactory.Setup(sr,sprite,color,order); return sr;
        }
    }

    public partial class Monster
    {
        GameObject appearance, disguise;
        Transform throat, mouth;
        byte appearanceState = 255;
        void RebuildAppearance(byte previous,byte current) { BuildAppearance(); }
        void BuildAppearance()
        {
            if (appearance != null) { appearance.SetActive(false); Destroy(appearance); }
            if (disguise != null) { disguise.SetActive(false); Destroy(disguise); }
            GetComponent<SpriteRenderer>().color = Kind == MonsterKind.Charger ? new Color(.85f,.25f,.25f) : Color.clear;
            throat = mouth = null;
            appearance = new GameObject("MonsterAppearance"); appearance.transform.SetParent(transform,false);
            Color purple = new Color(.45f,.18f,.7f), red = new Color(.7f,.1f,.15f);
            switch (Kind)
            {
                case MonsterKind.Listener:
                    Part("Body",SpriteFactory.Circle,Vector2.zero,Vector2.one,purple);
                    foreach (float side in new[] {-1f,1f})
                    {
                        Line(new Vector2(.2f,side*.3f),new Vector2(.85f,side*.65f),purple,.07f);
                        Part("Antenna",SpriteFactory.Circle,new Vector2(.85f,side*.65f),Vector2.one*.12f,purple);
                    }
                    Eyes(new Color(.7f,.7f,.55f),.055f); break;
                case MonsterKind.Shell:
                    Part("Rear",SpriteFactory.Circle,Vector2.zero,new Vector2(1.4f,1.15f),new Color(.8f,.36f,.08f));
                    Part("Shell",SpriteFactory.Polygon(6),new Vector2(.2f,0),new Vector2(1.4f,1.45f),Color.gray);
                    foreach (float y in new[] {-.4f,0,.4f}) Part("FrontPlate",SpriteFactory.Polygon(4),new Vector2(.58f,y),new Vector2(.65f,.65f),new Color(.68f,.69f,.7f),0,7);
                    break;
                case MonsterKind.Weaver:
                    for(int side=-1;side<=1;side+=2) for(int i=0;i<4;i++)
                    { float x=(i-1.5f)*.28f; var knee=new Vector2(x*1.5f,side*.58f); Line(new Vector2(x,side*.2f),knee,purple,.06f); Line(knee,new Vector2(x*1.9f-.15f,side*.85f),purple,.055f); }
                    Part("Abdomen",SpriteFactory.Circle,Vector2.zero,new Vector2(.85f,.7f),purple); Eyes(Color.white,.07f); break;
                case MonsterKind.Herald:
                    Part("Body",SpriteFactory.Circle,Vector2.zero,Vector2.one,new Color(1,.3f,.55f));
                    throat=Part("Throat",SpriteFactory.Circle,new Vector2(.4f,0),Vector2.one*.55f,new Color(.8f,.02f,.4f),0,7).transform;
                    Eyes(Color.white,.09f); break;
                case MonsterKind.Mimic:
                    disguise=ChestAppearance.Build(transform,new Color(.5f,.35f,.2f));
                    Part("Body",SpriteFactory.Polygon(6),Vector2.zero,new Vector2(1.4f,1.1f),new Color(.25f,.16f,.11f));
                    mouth=Part("Jaw",SpriteFactory.Square,new Vector2(.35f,0),new Vector2(.7f,.8f),new Color(.14f,.02f,.03f),0,7).transform;
                    Teeth(.35f,.3f);
                    for(int side=-1;side<=1;side+=2) for(int i=0;i<3;i++)
                        Line(new Vector2((i-1)*.4f,side*.3f),new Vector2((i-1)*.65f-.15f,side*.8f),new Color(.4f,.22f,.14f),.1f);
                    Eyes(new Color(1,.7f,.05f),.12f); break;
                case MonsterKind.Acid:
                    Part("Body",SpriteFactory.Polygon(3),Vector2.zero,new Vector2(1.3f,1.3f),new Color(.45f,.8f,.05f));
                    Part("AcidMouth",SpriteFactory.Circle,new Vector2(.48f,0),Vector2.one*.23f,Color.yellow,0,7); break;
                case MonsterKind.Stalker:
                    Part("Body",SpriteFactory.Polygon(4),Vector2.zero,new Vector2(1.55f,.7f),new Color(.06f,.15f,.32f));
                    Eyes(Color.cyan,.1f); break;
                case MonsterKind.Devourer:
                    Part("Body",SpriteFactory.Circle,Vector2.zero,Vector2.one*1.2f,red);
                    mouth=Part("Mouth",SpriteFactory.Circle,Vector2.zero,Vector2.one*.8f,new Color(.1f,.01f,.02f),0,7).transform;
                    for(int i=0;i<10;i++)
                    { float a=i*Mathf.PI*2/10; Part("Tooth",SpriteFactory.Polygon(3),new Vector2(Mathf.Cos(a),Mathf.Sin(a))*.34f,Vector2.one*.17f,new Color(.9f,.85f,.7f),a*Mathf.Rad2Deg+180,8); }
                    break;
            }
            appearanceState=255;
            GetComponent<VisibilityTarget>()?.Refresh();
            UpdateAppearance();
        }
        SpriteRenderer Part(string name,Sprite sprite,Vector2 pos,Vector2 size,Color color,float angle=0,int order=6)
            => ChestAppearance.Part(appearance.transform,name,sprite,pos,size,color,order,angle);
        void Line(Vector2 a,Vector2 b,Color color,float width)
        { var d=b-a; Part("Limb",SpriteFactory.Square,(a+b)*.5f,new Vector2(d.magnitude,width),color,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg,5); }
        void Eyes(Color color,float size)
        { foreach(float y in new[] {-.16f,.16f}) Part("Eye",SpriteFactory.Circle,new Vector2(.3f,y),Vector2.one*size,color,0,9); }
        void Teeth(float x,float y)
        { for(int i=0;i<4;i++) foreach(float side in new[] {-1f,1f}) Part("Tooth",SpriteFactory.Polygon(3),new Vector2(x-.24f+i*.16f,side*y),Vector2.one*.15f,new Color(.9f,.85f,.7f),-side*90,8); }
        void UpdateAppearance()
        {
            if (appearance==null) return;
            if (appearanceState != AbilityState.Value)
            {
                appearanceState=AbilityState.Value;
                appearance.SetActive(!Disguised);
                if(disguise!=null) disguise.SetActive(Disguised);
                if(bodyCollider!=null && Kind==MonsterKind.Mimic) bodyCollider.isTrigger=Disguised;
            }
            if(throat!=null) throat.localScale=Vector3.one*(AbilityState.Value==1 ? .6f+.3f*(1+Mathf.Sin(Time.time*8)) : .55f);
            if(Kind==MonsterKind.Devourer) appearance.transform.localScale=Vector3.one*(1+Growth.Value*.08f);
            if(mouth!=null && AbilityState.Value==1) mouth.localScale=Vector3.one*(.75f+.1f*Mathf.Sin(Time.time*10));
            if(disguise!=null && Disguised) disguise.transform.localPosition=new Vector3(Time.time%12<.15f ? Mathf.Sin(Time.time*80)*.025f : 0,0,0);
        }
        void DrawAbilityWarning()
        {
            if(IsServer || !IsSpawned || Kind==MonsterKind.Charger || Disguised || AbilityState.Value!=1 || Camera.main==null) return;
            var local=InventoryWindow.Local(); if(local==null || !BotVisible(local)) return;
            var p=Camera.main.WorldToScreenPoint(transform.position); if(p.z<0 || p.x<0 || p.x>Screen.width || p.y<0 || p.y>Screen.height) return;
            string label=Kind==MonsterKind.Devourer ? "ALIMENTANDO" : Kind==MonsterKind.Herald ? "GRITO!" : "PREPARANDO";
            var old=GUI.matrix; GUI.matrix=Matrix4x4.identity;
            GUI.Box(new Rect(p.x-85,Screen.height-p.y-54,170,26),label);
            GUI.matrix=old;
        }
        bool BotVisible(CyberExtraction.Players.PlayerInventory local) => CyberExtraction.Bots.BotController.InVision(local.transform.position,local.transform.eulerAngles.z,transform.position) &&
            Physics2D.Linecast(local.transform.position,transform.position,GameConfig.VisionMask).collider==null;
    }
}
