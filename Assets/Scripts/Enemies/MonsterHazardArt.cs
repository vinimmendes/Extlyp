using UnityEngine;

namespace CyberExtraction.Enemies
{
    // Cached, original overhead ground effects. The diameter matches the gameplay radius.
    public static class MonsterHazardArt
    {
        static Sprite web, acid;
        public static Sprite Get(MonsterEffectKind kind)
        {
            if(kind==MonsterEffectKind.Web) return web!=null ? web : web=Create(true);
            return acid!=null ? acid : acid=Create(false);
        }
        static Sprite Create(bool isWeb)
        {
            const int size=256;
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false)
                { name=isWeb ? "SpiderWeb" : "AcidPool", filterMode=FilterMode.Bilinear, wrapMode=TextureWrapMode.Clamp };
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++) for(int x=0;x<size;x++)
            {
                Vector2 p=new Vector2((x+.5f)/size*2-1,(y+.5f)/size*2-1);
                float r=p.magnitude, angle=Mathf.Atan2(p.y,p.x);
                if(r>=1) continue;
                Color color;
                if(isWeb)
                {
                    float sector=angle/(Mathf.PI*2)*12;
                    float spoke=Mathf.Abs(Mathf.Sin(angle*6))*r;
                    float scallop=1-.09f*Mathf.Sin((sector-Mathf.Floor(sector))*Mathf.PI);
                    float ring=r/scallop*6;
                    float ringDistance=Mathf.Abs(ring-Mathf.Round(ring))/6;
                    float thread=Fade(.004f,.018f,Mathf.Min(spoke,ringDistance));
                    float edge=Fade(.91f,1,r);
                    color=Color.Lerp(new Color(.32f,.30f,.37f,.10f),new Color(.91f,.88f,.79f,.92f),thread);
                    color.a*=edge;
                }
                else
                {
                    float boundary=.91f+.035f*Mathf.Sin(angle*5)+.025f*Mathf.Sin(angle*9+1);
                    float edge=Fade(boundary-.045f,boundary,r);
                    float mottling=.5f+.25f*Mathf.Sin(p.x*19+p.y*11)+.25f*Mathf.Sin(p.y*27-p.x*8);
                    color=Color.Lerp(new Color(.13f,.28f,.025f,.88f),new Color(.48f,.67f,.08f,.90f),mottling);
                    float foam=Fade(.014f,.035f,Mathf.Abs(r-(boundary-.055f)));
                    color=Color.Lerp(color,new Color(.73f,.84f,.25f,.95f),foam*.65f);
                    for(int b=0;b<9;b++)
                    {
                        float a=b*2.39996f, d=.18f+.065f*b;
                        Vector2 center=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*d;
                        float bubble=Mathf.Abs(Vector2.Distance(p,center)-(.025f+(b%3)*.009f));
                        color=Color.Lerp(color,new Color(.80f,.90f,.34f,.95f),Fade(.004f,.011f,bubble));
                    }
                    color.a*=edge;
                }
                pixels[y*size+x]=color;
            }
            texture.SetPixels(pixels); texture.Apply(false,false);
            return Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),size,0,SpriteMeshType.FullRect);
        }
        static float Fade(float from,float to,float value) => 1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(from,to,value));
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            foreach(var sprite in new[]{web,acid}) if(sprite!=null) { Object.Destroy(sprite.texture); Object.Destroy(sprite); }
            web=acid=null;
        }
    }
}
