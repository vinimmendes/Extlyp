using System;
using UnityEngine;

namespace CyberExtraction.Match
{
    // Cardinal bits N/E/S/W; diagonal bits NE/SE/SW/NW. Diagonals require both adjacent cardinals.
    public sealed class DungeonWallCatalog : ScriptableObject
    {
        public int[] Masks;
        public Sprite[] Walls;
        public Sprite DoorLeft,DoorMiddle,DoorRight;
        public const string ResourcePath="Environment/Dungeon/ModularV2/CatacombWalls";
        public static int Normalize(int mask)
        {
            int result=mask&15;
            for(int i=0;i<4;i++) if((mask&(1<<i))!=0 && (mask&(1<<((i+1)%4)))!=0 && (mask&(1<<(i+4)))!=0) result|=1<<(i+4);
            return result;
        }
        public static int MaskAt(int x,int y,Func<int,int,bool> stone)
        {
            int mask=0;
            if(stone(x,y+1)) mask|=1; if(stone(x+1,y)) mask|=2;
            if(stone(x,y-1)) mask|=4; if(stone(x-1,y)) mask|=8;
            if(stone(x+1,y+1)) mask|=16; if(stone(x+1,y-1)) mask|=32;
            if(stone(x-1,y-1)) mask|=64; if(stone(x-1,y+1)) mask|=128;
            return Normalize(mask);
        }
        public Sprite ForMask(int mask)
        {int index=Array.IndexOf(Masks,Normalize(mask)); return index>=0 && index<Walls.Length ? Walls[index] : null;}
    }
}
