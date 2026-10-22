using UnityEngine;

namespace CyberExtraction.UI
{
    // Rotate the symbol pixels, never the GUI matrix or its clipping rectangle.
    public sealed class MinimapPointer : System.IDisposable
    {
        const int Size=48;
        Texture2D texture;
        readonly Color[] pixels=new Color[Size*Size];
        int previousAngle=int.MinValue;
        static readonly Vector2[] outline={new Vector2(0,-.375f),new Vector2(.32f,.336f),new Vector2(0,.1875f),new Vector2(-.32f,.336f)};
        public Texture2D Texture(float angle)
        {
            int heading=Mathf.RoundToInt(Mathf.Repeat(angle,360));
            if(texture==null) texture=new Texture2D(Size,Size,TextureFormat.RGBA32,false) {name="Minimap local heading",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
            if(heading==previousAngle) return texture;
            previousAngle=heading;
            float radians=heading*Mathf.Deg2Rad,c=Mathf.Cos(radians),s=Mathf.Sin(radians);
            for(int y=0;y<Size;y++) for(int x=0;x<Size;x++)
            {
                int coverage=0;
                for(int sy=0;sy<2;sy++) for(int sx=0;sx<2;sx++)
                {
                    var q=new Vector2((x+.25f+sx*.5f)/Size-.5f,.5f-(y+.25f+sy*.5f)/Size);
                    var point=new Vector2(q.x*c+q.y*s,-q.x*s+q.y*c);
                    bool inside=false;
                    for(int i=0,j=outline.Length-1;i<outline.Length;j=i++)
                    {
                        var a=outline[i]; var b=outline[j];
                        if((a.y>point.y)!=(b.y>point.y) && point.x<(b.x-a.x)*(point.y-a.y)/(b.y-a.y)+a.x) inside=!inside;
                    }
                    if(inside) coverage++;
                }
                pixels[y*Size+x]=new Color(1,1,1,coverage*.25f);
            }
            texture.SetPixels(pixels); texture.Apply(false,false); return texture;
        }
        public void Draw(Rect rect,Color tint,float angle)
        { var old=GUI.color; GUI.color=tint; GUI.DrawTexture(rect,Texture(angle),ScaleMode.ScaleToFit,true); GUI.color=old; }
        public void Dispose() { if(texture!=null) Object.Destroy(texture); texture=null; }
    }
}
