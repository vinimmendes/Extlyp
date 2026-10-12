using UnityEngine;

namespace CyberExtraction.Match
{
    public partial class MapBuilder
    {
        public static uint RuinLayoutKey(int index)
        {
            uint key=unchecked((uint)LayoutSeed+(uint)index*0x9E3779B9u);
            unchecked { key^=key>>16;key*=0x85EBCA6Bu;key^=key>>13;key*=0xC2B2AE35u;key^=key>>16; }
            return key;
        }

        void BuildRuin(Vector2 center,int index)
        {
            uint key=RuinLayoutKey(index);
            int variant=(int)(key%4); bool mirrored=(key&4)!=0;
            float width=variant==1 ? 9 : variant==2 ? 12 : 10;
            Vector2 At(float x,float y) => center+new Vector2(mirrored ? -x : x,y);
            Color wall=new Color(.30f,.29f,.25f);
            Color floor=variant==1 ? new Color(.21f,.23f,.19f) : variant==2 ? new Color(.23f,.22f,.18f) : new Color(.24f,.22f,.20f);
            var floorObject=Block("RuinFloor",At(0,5),new Vector2(width,9),floor,-8);
            if(variant==0)
            {
                // Faceted rounded cottage, with the same clear front entrance.
                var corners=new[]{new Vector2(-5,3),new Vector2(-5,7),new Vector2(-3,9),
                    new Vector2(3,9),new Vector2(5,7),new Vector2(5,3),new Vector2(3,1)};
                for(int j=0;j<corners.Length-1;j++) RuinWall(At(corners[j].x,corners[j].y),At(corners[j+1].x,corners[j+1].y),wall);
                RuinWall(At(-3,1),At(-5,3),wall);
                // Replaces square corner floor portions with an inset octagonal floor.
                floorObject.GetComponent<SpriteRenderer>().sprite=CyberExtraction.Core.SpriteFactory.Polygon(8);
            }
            else if(variant==2 || variant==3)
            {
                // Broken rear opening adds a second way out; central chest aisle stays clear.
                float length=(width-3)/2;
                Block("RuinBack",At(-(3+length)/2,9),new Vector2(length,.6f),wall,-4,true);
                Block("RuinBack",At((3+length)/2,9),new Vector2(length,.6f),wall,-4,true);
            }
            else Block("RuinBack",At(0,9),new Vector2(width,.6f),wall,-4,true);
            if(variant!=0) Block("RuinLeft",At(-width/2,variant==1 ? 7 : variant==3 ? 3 : 5),
                new Vector2(.6f,variant==1 || variant==3 ? 4 : 8),wall,-4,true);
            if(variant!=3 && variant!=0)
                Block("RuinRight",At(width/2,variant==0 ? 7 : variant==1 ? 3 : 5),
                    new Vector2(.6f,variant==2 ? 8 : 4),wall,-4,true);
            // Every template has an open front aisle wider than the player's body.
            float frontLength=((variant==0 ? 6 : width)-3.6f)/2;
            Block("BrokenFrontWall",At(-(3.6f+frontLength)/2,1),new Vector2(frontLength,.6f),wall,-4,true);
            if(variant!=3) Block("BrokenFrontWall",At((3.6f+frontLength)/2,1),new Vector2(frontLength,.6f),wall,-4,true);
            Block("Trail",At(0,-5),new Vector2(3,12),new Color(.22f,.20f,.16f),-9);
            Block("Cover",At(8,variant==1 ? 2 : 5),new Vector2(variant==2 ? 3 : 2,2),oldWood,-3,true);
            DressAbandonedHouse(center,variant,mirrored);
        }
        void RuinWall(Vector2 from,Vector2 to,Color color)
        {
            Vector2 delta=to-from;
            var wall=Block("RuinWall",(from+to)*.5f,new Vector2(delta.magnitude,.6f),color,-4,true);
            wall.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
        }
    }
}
