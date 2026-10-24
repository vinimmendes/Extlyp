using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using CyberExtraction.Match;

namespace CyberExtraction.EditorTools
{
    // The common painted material is sampled into exact, code-authored modular templates.
    // Output is an imported asset with stable subasset names, not transient Play-mode textures.
    [ScriptedImporter(1,"wallset")]
    public sealed class DungeonWallSetImporter : ScriptedImporter
    {
        [Serializable] sealed class Definition {public string material;}
        const int Size=128,Pad=2,Pitch=Size+Pad*2,Columns=8;
        public override void OnImportAsset(AssetImportContext context)
        {
            var definition=JsonUtility.FromJson<Definition>(System.IO.File.ReadAllText(context.assetPath));
            context.DependsOnSourceAsset(definition.material);
            var material=AssetDatabase.LoadAssetAtPath<Texture2D>(definition.material);
            if(material==null || !material.isReadable) throw new InvalidOperationException("Material de paredes ausente ou sem Read/Write.");
            var source=material.GetPixels32(); var masks=new List<int>();
            for(int i=0;i<256;i++) if(DungeonWallCatalog.Normalize(i)==i) masks.Add(i);
            int rows=Mathf.CeilToInt((masks.Count+3)/(float)Columns);
            var atlas=new Texture2D(Columns*Pitch,rows*Pitch,TextureFormat.RGBA32,false) {name="CatacombWalls_V2_Atlas",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color32[atlas.width*atlas.height];
            for(int tile=0;tile<masks.Count+3;tile++)
                for(int y=-Pad;y<Size+Pad;y++) for(int x=-Pad;x<Size+Pad;x++)
                {
                    int px=Mathf.Clamp(x,0,Size-1),py=Mathf.Clamp(y,0,Size-1);
                    // Mirrored periodic sampling makes opposite module edges identical.
                    int sx=(int)((.2f+Mathf.Min(px,Size-1-px)*.5f/(Size-1))*(material.width-1));
                    int sy=(int)((.2f+Mathf.Min(py,Size-1-py)*.5f/(Size-1))*(material.height-1));
                    Color color=source[sy*material.width+sx];
                    if(tile<masks.Count) color=WallPixel(color,px,py,masks[tile]);
                    else color=DoorPixel(color,px,py,tile-masks.Count);
                    int ax=(tile%Columns)*Pitch+Pad+x,ay=(tile/Columns)*Pitch+Pad+y;
                    pixels[ay*atlas.width+ax]=color;
                }
            atlas.SetPixels32(pixels); atlas.Apply(false,false);
            context.AddObjectToAsset("atlas",atlas);
            var catalog=ScriptableObject.CreateInstance<DungeonWallCatalog>(); catalog.name="CatacombWalls_V2";
            catalog.Masks=masks.ToArray(); catalog.Walls=new Sprite[masks.Count];
            for(int i=0;i<masks.Count+3;i++)
            {
                string name=i<masks.Count ? "Wall_"+masks[i].ToString("X2") : new[]{"Door_Left","Door_Middle","Door_Right"}[i-masks.Count];
                var sprite=Sprite.Create(atlas,new Rect((i%Columns)*Pitch+Pad,(i/Columns)*Pitch+Pad,Size,Size),new Vector2(.5f,.5f),Size,0,SpriteMeshType.FullRect);
                sprite.name=name; context.AddObjectToAsset(name,sprite);
                if(i<masks.Count) catalog.Walls[i]=sprite;
                else if(i==masks.Count) catalog.DoorLeft=sprite; else if(i==masks.Count+1) catalog.DoorMiddle=sprite; else catalog.DoorRight=sprite;
            }
            context.AddObjectToAsset("catalog",catalog); context.SetMainObject(catalog);
        }
        static Color WallPixel(Color surface,int x,int y,int mask)
        {
            const float band=20;
            float edge=1000;
            if((mask&1)==0) edge=Mathf.Min(edge,Size-1-y);
            if((mask&2)==0) edge=Mathf.Min(edge,Size-1-x);
            if((mask&4)==0) edge=Mathf.Min(edge,y);
            if((mask&8)==0) edge=Mathf.Min(edge,x);
            for(int i=0;i<4;i++) if((mask&(1<<i))!=0 && (mask&(1<<((i+1)%4)))!=0 && (mask&(1<<(i+4)))==0)
            {
                float dx=(i==0 || i==1) ? Size-1-x : x,dy=(i==0 || i==3) ? Size-1-y : y;
                edge=Mathf.Min(edge,Mathf.Sqrt(dx*dx+dy*dy));
            }
            // Fixed-width cut stone edge and short contact groove; no side-dependent cast lighting.
            float luminance=surface.grayscale;
            Color top=Color.Lerp(surface,new Color(.32f,.33f,.30f),.22f);
            Color rim=Color.Lerp(new Color(.47f,.45f,.39f),new Color(.65f,.62f,.53f),luminance);
            Color result=edge<2 ? rim*.55f : edge<band-2 ? rim : edge<band+2 ? top*.5f : top;
            result.a=1; return result;
        }
        static Color DoorPixel(Color surface,int x,int y,int part)
        {
            bool jamb=(part==0 && x<20) || (part==2 && x>=Size-20);
            if(!jamb && y<Size-20) return Color.clear;
            var result=Color.Lerp(surface,new Color(.57f,.54f,.46f),.45f); result.a=1; return result;
        }
    }
}
