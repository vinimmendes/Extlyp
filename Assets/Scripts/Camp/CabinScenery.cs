using UnityEngine;
using CyberExtraction.Core;

namespace CyberExtraction.Camp
{
    // Coordinates are local to the 44 x 24.75 painted map, origin in its center.
    public sealed class CabinScenery
    {
        public static readonly Vector2 Size=new Vector2(44,24.75f);
        public static readonly Vector2 Depot=new Vector2(-8.4f,7.0f), Gate=new Vector2(-1.5f,-8.1f),
            Fishing=new Vector2(8.4f,.2f), Bench=new Vector2(-11.8f,2.6f), Stove=new Vector2(-2.3f,7.0f),
            Spawn=new Vector2(-6.3f,-4.5f);
        public const float PlayerHeight=1.85f, PlayerRadius=.65f;
        readonly Transform parent;
        GameObject log;

        public CabinScenery(Transform parent)
        {
            this.parent=parent;
            CabinArt.Place(parent,"CabinBase",Vector2.zero,Size,-10,false);
            // Full painted wall bands, bounded at their feet where timber meets floor/earth.
            // Pixel coordinates refer to the unchanged 1672 x 941 CabinBase artwork.
            Wall("CabinBack",286,90,858,174);
            Wall("CabinLeft",286,90,340,560);
            Wall("CabinRight",808,90,858,560);
            Wall("CabinFrontLeft",286,475,553,558);
            Wall("CabinFrontRight",654,475,858,558);
            Prop("Bed",new Vector2(-12.0f,5.7f),new Vector2(2.0f,4.0f),new Vector2(1.8f,3.9f));
            Prop("Chest",Depot,new Vector2(2.6f,1.7f),new Vector2(2.4f,1.5f));
            Prop("BenchBrokenAngled",Bench,new Vector2(3.5f,2.0f),new Vector2(3.3f,1.0f));
            Prop("Stove",Stove,new Vector2(2.3f,1.8f),new Vector2(2.1f,1.6f));
            CabinArt.Place(parent,"Firewood",Stove+new Vector2(-1.9f,0),new Vector2(1.2f,.9f),1);
            // A cosmetic log only: the new cutting/reward progression belongs to the next step.
            log=CabinArt.Place(parent,"LakeLog",new Vector2(9.5f,.9f),new Vector2(5.0f,1.6f),1);
            if(log!=null) log.transform.localRotation=Quaternion.Euler(0,0,-20);
            // Restrict water with a polygon, rather than the old rectangular river/clamp.
            var water=new GameObject("PondCollision"); water.transform.SetParent(parent,false); water.layer=GameConfig.WallLayer;
            water.AddComponent<PolygonCollider2D>().points=new[] {
                new Vector2(14.0f,2.0f),new Vector2(11.3f,4.2f),new Vector2(11.0f,7.6f),
                new Vector2(14.0f,10.8f),new Vector2(21.5f,11.8f),new Vector2(21.5f,.5f),new Vector2(17.0f,-.8f) };
            // Fence is painted continuously in the base; these simple segments supply collision.
            Segment("FenceLeft",new Vector2(-18.1f,8.6f),new Vector2(-17.2f,-4.3f));
            Segment("FenceLowerLeft",new Vector2(-17.2f,-4.3f),new Vector2(-4.7f,-7.7f));
            Segment("FenceLowerRight",new Vector2(1.3f,-8.0f),new Vector2(19.8f,-10.7f));
            Solid("GateClosed",Gate,new Vector2(4.4f,.6f));
            Solid("MailboxPost",new Vector2(3.0f,-7.0f),new Vector2(.8f,.8f));
        }

        void Prop(string key,Vector2 at,Vector2 artSize,Vector2 collisionSize)
        { CabinArt.Place(parent,key,at,artSize,2); Solid(key+"Collision",at,collisionSize); }
        void Wall(string name,float left,float top,float right,float bottom)
        {
            Vector2 min=new Vector2((left/1672f-.5f)*Size.x,(.5f-bottom/941f)*Size.y);
            Vector2 max=new Vector2((right/1672f-.5f)*Size.x,(.5f-top/941f)*Size.y);
            Solid(name,(min+max)*.5f,max-min);
        }
        void Solid(string name,Vector2 at,Vector2 size)
        {
            var go=new GameObject(name); go.transform.SetParent(parent,false); go.transform.localPosition=at;
            go.layer=GameConfig.WallLayer; go.AddComponent<BoxCollider2D>().size=size;
        }
        void Segment(string name,Vector2 a,Vector2 b)
        {
            Vector2 delta=b-a; var go=new GameObject(name); go.transform.SetParent(parent,false); go.transform.localPosition=(a+b)*.5f;
            go.transform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
            go.layer=GameConfig.WallLayer; go.AddComponent<BoxCollider2D>().size=new Vector2(delta.magnitude,.45f);
        }
        public void Refresh(bool fishingAvailable) { if(log!=null) log.SetActive(!fishingAvailable); }
    }
}
