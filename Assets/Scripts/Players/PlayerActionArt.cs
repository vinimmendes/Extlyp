using System.Collections.Generic;
using UnityEngine;

namespace CyberExtraction.Players
{
    // Client presentation assets; each texture is at most 2048x2048.
    static class PlayerActionArt
    {
        const int Cell=256, Columns=8, FramesPerPage=64;
        static readonly Dictionary<string,Sprite[]> Cache=new Dictionary<string,Sprite[]>();
        public static Sprite[] Transition(int source,int target,out int first)
        {
            first=0;
            if(source==target || target==0) return null;
            if(target==3 && (source==1 || source==0))
            { first=source==0 ? 10 : 0; return PlayerCharacterArt.GetFrames(4); }
            if(target==2 && (source==1 || source==0))
            { first=source==0 ? 10 : 0; return Get("SwitchRifle",32); }
            if(source==3 && target==1) return Get("ShotgunToPistol",36);
            if(source==2 && target==1) return Get("RifleToPistol",36);
            if(source==3 && target==2) return Get("ShotgunToRifle",36);
            if(source==2 && target==3) return Get("RifleToShotgun",36);
            return null;
        }
        public static Sprite[] Reload(int pose)
        {
            return pose==6 ? Get("ReloadCrossbow",49) : pose==1 ? Get("ReloadPistol",37) : pose==2 ? Get("ReloadRifle",78) : pose==3 ? Get("ReloadShotgun",64) : null;
        }
        public static Sprite[] Get(string name,int count,float worldSize=3.4f,bool optional=false)
        {
            string key=name+"@"+worldSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if(Cache.TryGetValue(key,out var cached)) return cached;
            var frames=new Sprite[count];
            for(int page=0;page*FramesPerPage<count;page++)
            {
                int length=Mathf.Min(FramesPerPage,count-page*FramesPerPage);
                var texture=Resources.Load<Texture2D>("PlayerCharacter/Actions/"+name+"_"+(page+1));
                if(texture==null || texture.width!=Columns*Cell || texture.height!=((length+7)/8)*Cell)
                {
                    foreach(var sprite in frames) if(sprite!=null) Object.Destroy(sprite);
                    if(!optional) Debug.LogError("Atlas de animacao do personagem ausente/invalido: "+name+" pagina "+(page+1));
                    Cache[key]=null; return null;
                }
                for(int i=0;i<length;i++)
                {
                    var rect=new Rect(i%Columns*Cell,texture.height-(i/Columns+1)*Cell,Cell,Cell);
                    var sprite=Sprite.Create(texture,rect,new Vector2(.5f,.5f),Cell/worldSize,0,SpriteMeshType.FullRect);
                    sprite.name=name+"_"+(page*FramesPerPage+i+1);
                    frames[page*FramesPerPage+i]=sprite;
                }
            }
            Cache[key]=frames; return frames;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            foreach(var frames in Cache.Values) if(frames!=null) foreach(var sprite in frames) if(sprite!=null) Object.Destroy(sprite);
            Cache.Clear();
        }
    }
}
