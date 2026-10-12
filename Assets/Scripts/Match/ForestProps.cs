using UnityEngine;
using CyberExtraction.Core;

namespace CyberExtraction.Match
{
    // Original provisional props, while the existing environment sprites dress interiors/forest.
    public static class ForestProps
    {
        public static SpriteRenderer Part(Transform parent,string name,Vector2 position,Vector2 size,Color color,bool solid)
        {
            var go=new GameObject(name); go.transform.SetParent(parent,false); go.transform.localPosition=position; go.transform.localScale=new Vector3(size.x,size.y,1);
            var renderer=go.AddComponent<SpriteRenderer>(); SpriteFactory.Setup(renderer,SpriteFactory.Square,color,-2);
            if(solid) { go.layer=GameConfig.LowObstacleLayer; go.AddComponent<BoxCollider2D>(); }
            return renderer;
        }
        public static void Vehicle(Transform parent,Vector2 position,bool truck)
        {
            var solid=new GameObject("VehicleCollision"); solid.transform.SetParent(parent,false); solid.transform.localPosition=position;
            solid.layer=GameConfig.LowObstacleLayer; solid.AddComponent<BoxCollider2D>().size=new Vector2(truck ? 5.7f : 3,2.2f);
            Color wood=new Color(.38f,.29f,.17f);
            Part(parent,truck ? "TruckBed" : "CartBed",position,new Vector2(truck ? 4 : 3,2.2f),wood,false);
            for(int i=0;i<5;i++) Part(parent,"WoodPlank",position+new Vector2(0,-.9f+i*.45f),new Vector2(truck ? 3.8f : 2.8f,.08f),wood*.7f,false);
            if(truck) { Part(parent,"Cab",position+new Vector2(2.5f,0),new Vector2(1.7f,2.3f),new Color(.25f,.32f,.27f),false); Part(parent,"Windshield",position+new Vector2(2.8f,0),new Vector2(.45f,1.7f),new Color(.09f,.17f,.17f),false); }
            else foreach(float y in new[]{-.6f,.6f}) Part(parent,"CartShaft",position+new Vector2(2.4f,y),new Vector2(2,.12f),wood,false);
            foreach(float x in new[]{-1f,1f}) foreach(float y in new[]{-1.3f,1.3f}) Part(parent,"Wheel",position+new Vector2(x,y),new Vector2(.8f,.3f),new Color(.09f,.10f,.08f),false);
        }
        public static void Boat(Transform parent,bool canoe)
        {
            var hull=Part(parent,canoe ? "Canoe" : "Boat",Vector2.zero,new Vector2(canoe ? 4 : 5,2),new Color(.35f,.26f,.16f),false);
            hull.sprite=SpriteFactory.Polygon(8);
            Part(parent,"HullInterior",Vector2.zero,new Vector2(canoe ? 3 : 4,1.4f),new Color(.15f,.17f,.12f),false);
            foreach(float x in new[]{-1f,1f}) Part(parent,"BoatSeat",new Vector2(x,0),new Vector2(.25f,1.5f),new Color(.46f,.35f,.21f),false);
            Part(parent,"Oar",new Vector2(0,1.2f),new Vector2(3,.13f),new Color(.53f,.41f,.25f),false);
        }
    }
}
