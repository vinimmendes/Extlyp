using System;
using UnityEngine;

namespace CyberExtraction.Items
{
    // Visual resources only. Tool ownership and recovery remain server rules.
    static class DungeonToolArt
    {
        [Serializable] sealed class Manifest { public Entry[] assets; }
        [Serializable] sealed class Entry { public string id,resource; public float x,y,width,height; }
        static readonly Sprite[] cache=new Sprite[4];
        static readonly bool[] attempted=new bool[4];
        public static Sprite GetIcon(ItemId item) => Get(item,true);
        public static Sprite GetWorld(ItemId item) => Get(item,false);
        static Sprite Get(ItemId item,bool icon)
        {
            if(item!=ItemId.DungeonCrank && item!=ItemId.DungeonKey) return null;
            int index=(icon ? 2 : 0)+(item==ItemId.DungeonKey ? 1 : 0);
            if(attempted[index]) return cache[index];
            attempted[index]=true;
            var json=Resources.Load<TextAsset>("Environment/Dungeon/Exits/ExtractionsArt");
            if(json==null) return null;
            var manifest=JsonUtility.FromJson<Manifest>(json.text);
            if(manifest==null || manifest.assets==null) return null;
            string id="CAT-E"+(13+index)+"_v01";
            foreach(var entry in manifest.assets)
            {
                if(entry.id!=id) continue;
                var texture=Resources.Load<Texture2D>(entry.resource); if(texture==null) return null;
                int x=Mathf.Clamp(Mathf.FloorToInt(entry.x*texture.width),0,texture.width-1);
                int y=Mathf.Clamp(Mathf.FloorToInt(entry.y*texture.height),0,texture.height-1);
                int w=Mathf.Clamp(Mathf.CeilToInt(entry.width*texture.width),1,texture.width-x);
                int h=Mathf.Clamp(Mathf.CeilToInt(entry.height*texture.height),1,texture.height-y);
                return cache[index]=Sprite.Create(texture,new Rect(x,y,w,h),new Vector2(.5f,.5f),128,0,SpriteMeshType.FullRect);
            }
            return null;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            for(int i=0;i<cache.Length;i++)
            { if(cache[i]!=null) UnityEngine.Object.Destroy(cache[i]); cache[i]=null; attempted[i]=false; }
        }
    }
}
