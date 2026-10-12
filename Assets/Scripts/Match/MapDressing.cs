using UnityEngine;
using System.Collections.Generic;
using CyberExtraction.Core;

namespace CyberExtraction.Match
{
    // Original top-down blockout art. Only the large map receives this dressing.
    // Low furniture blocks movement, while tall furniture and trunks also occlude sight.
    public partial class MapBuilder
    {
        public readonly struct SearchableFurniture
        {
            public readonly Vector2 Position;
            public readonly float Angle;
            public readonly byte Kind;
            public SearchableFurniture(Vector2 position,float angle,byte kind) { Position=position; Angle=angle; Kind=kind; }
        }
        public static readonly List<SearchableFurniture> FurniturePoints=new List<SearchableFurniture>();
        static readonly Color oldWood=new Color(.30f,.23f,.16f), woodEdge=new Color(.15f,.12f,.09f), rust=new Color(.37f,.22f,.13f);
        void DressAbandonedHouse(Vector2 c,int variant,bool mirrored)
        {
            Vector2 At(float x,float y) => c+new Vector2(mirrored ? -x : x,y);
            for(int plank=0;plank<9;plank++)
                Block("WornFloorboard",At(0,1+plank),new Vector2(variant==2 ? 11.6f : variant==0 && (plank==0 || plank==8) ? 5.6f : variant==0 && (plank==1 || plank==7) ? 7.6f : 8.6f,.035f),new Color(.13f,.12f,.10f,.6f),-7);
            // Cottage, workshop, storehouse and collapsed house: different furniture/cover.
            // The central aisle and chest at c+(0,4) remain unobstructed in all variants.
            if(variant==0)
            {
                Furniture("OldWardrobe",At(-2.6f,7.5f),new Vector2(1.7f,1.3f),0);
                Furniture("OldBed",At(2.9f,5.8f),new Vector2(1.7f,2.7f),1);
                Furniture("OldTable",At(-2.9f,3.7f),new Vector2(1.6f,1.3f),2);
            }
            else if(variant==1)
            {
                Furniture("WorkshopBench",At(-2.8f,6.8f),new Vector2(2.1f,1.3f),2);
                Furniture("RustyCabinet",At(2.8f,7.9f),new Vector2(1.4f,1.1f),3);
                Block("WorkshopCrate",At(2.9f,3.4f),new Vector2(1.3f,1.1f),oldWood,-3,true);
            }
            else if(variant==2)
            {
                Furniture("StoreWardrobe",At(-4.4f,7.5f),new Vector2(1.7f,1.3f),0);
                Furniture("RustyCabinet",At(4.3f,7.5f),new Vector2(1.4f,1.1f),3);
                Block("StoreCrate",At(-3.4f,3.8f),new Vector2(1.5f,1.5f),oldWood,-3,true);
                Block("StoreCrate",At(3.4f,4.8f),new Vector2(1.5f,1.5f),oldWood,-3,true);
            }
            else
            {
                Furniture("OldBed",At(-3.2f,6.4f),new Vector2(1.7f,2.7f),1);
                Furniture("OldTable",At(3.1f,7.7f),new Vector2(1.5f,1.1f),2);
                Block("FallenDoor",At(3,2.4f),new Vector2(1.5f,.35f),oldWood,-4).transform.rotation=Quaternion.Euler(0,0,mirrored ? -25 : 25);
            }
            Block("BrokenChair",At(-3.1f,2.2f),new Vector2(.65f,.6f),woodEdge,-3);
            Block("ChairBack",At(-3.1f,2.5f),new Vector2(.72f,.14f),oldWood,-2);
            for(int debris=0;debris<3;debris++)
                Block("Debris",At(2.2f+debris*.6f,3+debris*.18f),new Vector2(.3f,.12f),rust,-4).transform.rotation=Quaternion.Euler(0,0,debris*37+variant*9);
        }
        void Furniture(string name,Vector2 center,Vector2 size,int kind)
        {
            int choice=(int)((uint)Mathf.RoundToInt(center.x*17+center.y*31)^ (uint)LayoutSeed)%4;
            choice=Mathf.Abs(choice);
            float angle=kind==0 ? (choice%2)*180f : kind==3 ? (choice%2)*90f :
                kind==1 ? (choice%2)*90f : choice==0 ? 0f : choice==1 ? 90f : choice==2 ? 18f : -18f;
            if(kind==0 || kind==3)
            {
                // Network loot fixture owns both the visual and the solid collider.
                FurniturePoints.Add(new SearchableFurniture(center,angle,(byte)(kind==0 ? 1 : 2)));
                var obstacle=Block(name,center,size,woodEdge,-4,true);
                obstacle.transform.rotation=Quaternion.Euler(0,0,angle);
                obstacle.layer=kind==0 ? GameConfig.WallLayer : GameConfig.LowObstacleLayer;
                obstacle.GetComponent<SpriteRenderer>().enabled=false;
                obstacle.AddComponent<FurnitureObstacle>();
                return;
            }
            var root=Block(name,center,size,woodEdge,-4,true);
            root.transform.rotation=Quaternion.Euler(0,0,angle);
            if(kind==1 || kind==2) root.layer=GameConfig.LowObstacleLayer;
            var art=EnvironmentSprites.Get(kind==0 ? "Wardrobe" : kind==1 ? "Bed" : kind==2 ? "Table" : "Cabinet");
            if(art!=null)
            {
                root.GetComponent<SpriteRenderer>().enabled=false;
                Detail(root,"Art",Vector2.zero,new Vector2(1/art.bounds.size.x,1/art.bounds.size.y),Color.white,-3,art);
                return;
            }
            Detail(root,"Surface",Vector2.zero,new Vector2(.88f,.85f),kind==3 ? new Color(.27f,.29f,.25f) : oldWood,-3);
            if(kind==0 || kind==3)
            {
                Detail(root,"Doors",Vector2.zero,new Vector2(.025f,.8f),woodEdge,-2);
                Detail(root,"HandleLeft",new Vector2(-.08f,0),new Vector2(.045f,.17f),rust,-1);
                Detail(root,"HandleRight",new Vector2(.08f,0),new Vector2(.045f,.17f),rust,-1);
                Detail(root,"Scrape",new Vector2(-.2f,.2f),new Vector2(.25f,.025f),new Color(.44f,.36f,.23f),-2);
            }
            else if(kind==1)
            {
                Detail(root,"StainedMattress",new Vector2(0,-.05f),new Vector2(.78f,.68f),new Color(.40f,.37f,.26f),-2);
                Detail(root,"OldPillow",new Vector2(0,.27f),new Vector2(.55f,.2f),new Color(.51f,.48f,.36f),-1);
                Detail(root,"TornBlanket",new Vector2(.03f,-.2f),new Vector2(.73f,.29f),new Color(.24f,.29f,.22f),-1);
            }
            else
            {
                for(int j=0;j<3;j++) Detail(root,"TableSeam",new Vector2(0,-.25f+j*.25f),new Vector2(.82f,.025f),woodEdge,-2);
                Detail(root,"OldDish",new Vector2(.22f,.18f),new Vector2(.17f,.2f),new Color(.44f,.43f,.32f),-1,SpriteFactory.Circle);
            }
        }
        void Detail(GameObject parent,string name,Vector2 at,Vector2 size,Color color,int order,Sprite sprite=null)
        {
            var go=new GameObject(name); go.transform.SetParent(parent.transform,false); go.transform.localPosition=at; go.transform.localScale=new Vector3(size.x,size.y,1);
            var renderer=go.AddComponent<SpriteRenderer>();
            SpriteFactory.Setup(renderer,sprite!=null ? sprite : SpriteFactory.Square,color,order);
            if(sprite!=null && sprite.texture.name=="AbandonedProps" && EnvironmentSprites.Material!=null) renderer.sharedMaterial=EnvironmentSprites.Material;
        }
        void DressForest()
        {
            // System.Random avoids disturbing gameplay's random sequence; identical on client/server.
            var random=new System.Random(82371);
            Physics2D.SyncTransforms();
            foreach(var site in largeSites)
                for(int attempt=0,placed=0;attempt<14 && placed<4;attempt++)
                {
                    float angle=(float)(random.NextDouble()*Mathf.PI*2),distance=18+(float)random.NextDouble()*12;
                    var point=site+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*distance;
                    if(!TreeSiteClear(point)) continue;
                    Tree(point,random.Next(4)); placed++; Physics2D.SyncTransforms();
                }
        }
        public static bool TreeSiteClear(Vector2 point)
        {
            if(Mathf.Abs(point.x)>Width/2-4 || Mathf.Abs(point.y)>Height/2-4 || Physics2D.OverlapCircle(point,2,GameConfig.WallMask)) return false;
            foreach(var spawn in SpawnPoints) if(Vector2.Distance(point,spawn)<18) return false;
            foreach(var exit in ExtractionPoints) if(Vector2.Distance(point,exit)<8) return false;
            foreach(var site in Sites)
            {
                if(Mathf.Abs(point.x-site.x)<12 && point.y-site.y>-12 && point.y-site.y<15) return false;
                if(Vector2.Distance(point,site)<5) return false;
            }
            for(int i=0;i<Sites.Length;i+=5)
                if(Mathf.Abs(point.x-(Sites[i].x-15))<8 && Mathf.Abs(point.y-Sites[i].y)<11) return false;
            return true;
        }
        void Tree(Vector2 point,int variant,float artScale=3.5f)
        {
            var tree=new GameObject(variant==0 ? "DeadTree" : "ForestTree"); tree.transform.SetParent(world.transform,false); tree.transform.position=point;
            tree.layer=GameConfig.LowObstacleLayer; tree.AddComponent<CircleCollider2D>().radius=.48f;
            var art=EnvironmentSprites.Get(variant==0 ? "DeadTree" : "ForestTree");
            if(art!=null)
            {
                Detail(tree,"Art",Vector2.zero,new Vector2(artScale/art.bounds.size.x,artScale/art.bounds.size.y),variant==2 ? new Color(.85f,.9f,.8f) : Color.white,-3,art);
                return;
            }
            Detail(tree,"Roots",Vector2.zero,new Vector2(1.4f,.65f),woodEdge,-4,SpriteFactory.Polygon(5));
            Detail(tree,"Trunk",Vector2.zero,new Vector2(.7f,.85f),oldWood,-3,SpriteFactory.Circle);
            if(variant!=0)
            {
                Color leaves=variant==1 ? new Color(.16f,.22f,.13f) : variant==2 ? new Color(.22f,.26f,.15f) : new Color(.25f,.22f,.12f);
                Detail(tree,"Canopy",new Vector2(.1f,.3f),new Vector2(3.5f,3.2f),leaves,-3,SpriteFactory.Polygon(7));
                Detail(tree,"LeafCluster",new Vector2(-.55f,.8f),new Vector2(1.8f,1.6f),new Color(leaves.r+.035f,leaves.g+.025f,leaves.b),-2,SpriteFactory.Polygon(6));
            }
            for(int branch=0;branch<3;branch++)
            {
                Detail(tree,"Branch",new Vector2((branch-1)*.42f,.35f+branch*.1f),new Vector2(.15f,1.35f),oldWood,-1);
                tree.transform.GetChild(tree.transform.childCount-1).localRotation=Quaternion.Euler(0,0,(branch-1)*48);
            }
        }
    }
}
