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
                    if (sh != null) unlit = new Material(sh);
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
                    if (shader != null) spriteMaterial = new Material(shader);
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
    }
}
