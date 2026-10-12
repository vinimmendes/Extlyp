using System;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class EnvironmentSetup
{
    public const string Path="Assets/Resources/Environment/AbandonedProps.png";
    [MenuItem("Cyber Extraction/Cenario/Preparar sprites do ambiente")]
    public static void Prepare()
    {
        var importer=AssetImporter.GetAtPath(Path) as TextureImporter;
        if(importer==null) throw new Exception("Folha de sprites do ambiente ausente.");
        importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Multiple;
        importer.isReadable=true; importer.alphaIsTransparency=true; importer.mipmapEnabled=false;
        importer.npotScale=TextureImporterNPOTScale.None; importer.maxTextureSize=2048;
        importer.textureCompression=TextureImporterCompression.Uncompressed; importer.filterMode=FilterMode.Bilinear;
        importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Path); var pixels=texture.GetPixels32();
        if(texture.width%3!=0 || texture.height%2!=0) throw new Exception("Atlas deve ter uma grade 3 x 2.");
        string[] names={"ForestTree","DeadTree","Wardrobe","Bed","Table","Cabinet"};
        var factories=new SpriteDataProviderFactories(); factories.Init();
        var provider=factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
        var previous=provider.GetSpriteRects(); var rects=new SpriteRect[6];
        int w=texture.width/3,h=texture.height/2;
        for(int i=0;i<6;i++)
        {
            int left=i%3*w,bottom=(1-i/3)*h,minX=left+w,minY=bottom+h,maxX=left,maxY=bottom;
            for(int y=bottom;y<bottom+h;y++) for(int x=left;x<left+w;x++)
                if(pixels[y*texture.width+x].a>32) { minX=Math.Min(minX,x); minY=Math.Min(minY,y); maxX=Math.Max(maxX,x); maxY=Math.Max(maxY,y); }
            if(maxX<=minX || maxY<=minY) throw new Exception("Celula vazia: "+names[i]);
            var old=previous.FirstOrDefault(r=>r.name==names[i]);
            rects[i]=new SpriteRect { name=names[i],rect=new Rect(minX,minY,maxX-minX+1,maxY-minY+1),pivot=new Vector2(.5f,.5f),alignment=SpriteAlignment.Center,spriteID=old!=null ? old.spriteID : GUID.Generate() };
        }
        provider.SetSpriteRects(rects);
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));
        provider.Apply(); importer.isReadable=false; importer.SaveAndReimport();
        const string materialPath="Assets/Resources/Environment/AbandonedProps.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(material==null) { material=new Material(CyberExtraction.Core.SpriteFactory.SpriteMaterial); AssetDatabase.CreateAsset(material,materialPath); }
        material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Path); EditorUtility.SetDirty(material); AssetDatabase.SaveAssets();
        Debug.Log("ENVIRONMENT SPRITES PREPARED: 6 sprites, alpha bounds, original atlas preserved.");
    }
}
