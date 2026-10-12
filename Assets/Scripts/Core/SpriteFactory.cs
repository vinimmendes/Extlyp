using UnityEngine;

namespace CyberExtraction.Core
{
    // Cria sprites simples por codigo, para o jogo rodar sem nenhuma arte.
    public static class SpriteFactory
    {
        static Sprite square, circle;
        static Texture2D circleTex;
        static Material unlit;
        static Material spriteMaterial;
        static bool ownsUnlit, ownsSpriteMaterial;
        static readonly System.Collections.Generic.Dictionary<int, Sprite> polygons = new System.Collections.Generic.Dictionary<int, Sprite>();
        public static Sprite Polygon(int sides)
        {
            if (polygons.TryGetValue(sides, out var sprite)) return sprite;
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y=0;y<size;y++) for (int x=0;x<size;x++)
            {
                Vector2 p = new Vector2((x+.5f)/size-.5f,(y+.5f)/size-.5f);
                bool inside = true;
                for (int i=0;i<sides;i++)
                {
                    float a=i*Mathf.PI*2/sides, b=(i+1)*Mathf.PI*2/sides;
                    Vector2 v=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*.5f, w=new Vector2(Mathf.Cos(b),Mathf.Sin(b))*.5f;
                    if ((w.x-v.x)*(p.y-v.y)-(w.y-v.y)*(p.x-v.x)<0) { inside=false; break; }
                }
                texture.SetPixel(x,y,inside ? Color.white : Color.clear);
            }
            texture.Apply();
            sprite=Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),size);
            polygons.Add(sides,sprite); return sprite;
        }

        public static Sprite Square
        {
            get
            {
                if (square == null)
                {
                    var t = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                    var px = new Color[16];
                    for (int i = 0; i < px.Length; i++) px[i] = Color.white;
                    t.SetPixels(px);
                    t.filterMode = FilterMode.Point;
                    t.Apply();
                    square = Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
                }
                return square;
            }
        }

        public static Texture2D CircleTexture
        {
            get
            {
                if (circleTex == null)
                {
                    const int n = 64;
                    circleTex = new Texture2D(n, n, TextureFormat.RGBA32, false);
                    float c = (n - 1) * 0.5f;
                    for (int y = 0; y < n; y++)
                        for (int x = 0; x < n; x++)
                        {
                            float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                            float a = Mathf.Clamp01(c + 0.5f - d);
                            circleTex.SetPixel(x, y, new Color(1, 1, 1, a));
                        }
                    circleTex.wrapMode = TextureWrapMode.Clamp;
                    circleTex.Apply();
                }
                return circleTex;
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (circle == null)
                    circle = Sprite.Create(CircleTexture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64f, 0, SpriteMeshType.FullRect);
                return circle;
            }
        }

        // Material sem iluminacao (criado pela ferramenta de setup em Resources/UnlitSprite)
        public static Material Unlit
        {
            get
            {
                if (unlit == null) unlit = Resources.Load<Material>("UnlitSprite");
                if (unlit == null)
                {
                    var sh = Shader.Find("Sprites/Default");
                    if (sh != null) { unlit = new Material(sh); ownsUnlit=true; }
                }
                return unlit;
            }
        }

        // SpriteRenderer usa o shader 2D da URP e recebe a textura de cada sprite.
        // O material da malha de escuridao e das linhas permanece separado.
        public static Material SpriteMaterial
        {
            get
            {
                if (spriteMaterial == null) spriteMaterial = Resources.Load<Material>("UnlitSprite2D");
                if (spriteMaterial == null)
                {
                    var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                    if (shader != null) { spriteMaterial = new Material(shader); ownsSpriteMaterial=true; }
                    else
                    {
                        Debug.LogError("Shader de sprites URP 2D nao encontrado. Verifique o template Universal 2D e execute Cyber Extraction > Atualizar material dos sprites.");
                        spriteMaterial = Unlit;
                    }
                }
                return spriteMaterial;
            }
        }

        public static void Setup(SpriteRenderer sr, Sprite sprite, Color color, int order)
        {
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            var m = SpriteMaterial;
            if (m != null) sr.sharedMaterial = m;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            if(square!=null) { Object.Destroy(square.texture); Object.Destroy(square); }
            if(circle!=null) Object.Destroy(circle);
            if(circleTex!=null) Object.Destroy(circleTex);
            foreach(var value in polygons.Values) if(value!=null) { Object.Destroy(value.texture); Object.Destroy(value); }
            polygons.Clear(); if(ownsSpriteMaterial && spriteMaterial!=null) Object.Destroy(spriteMaterial); if(ownsUnlit && unlit!=null) Object.Destroy(unlit);
            square=circle=null; circleTex=null; unlit=spriteMaterial=null; ownsUnlit=ownsSpriteMaterial=false;
        }
    }
}
