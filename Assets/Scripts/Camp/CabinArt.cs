using System;
using System.Collections.Generic;
using UnityEngine;
using CyberExtraction.Core;

namespace CyberExtraction.Camp
{
    // Runtime rectangles ignore existing source-art slicings and preserve the PNGs.
    public static class CabinArt
    {
        [Serializable] class Entry { public string key; public float x, y, width, height; }
        [Serializable] class Manifest { public Entry[] entries; }
        class Asset { public Sprite sprite; public Material material; }
        static Manifest manifest;
        static readonly Dictionary<string, Asset> assets = new Dictionary<string, Asset>();

        static Asset Get(string key)
        {
            if(assets.TryGetValue(key,out var cached)) return cached;
            var texture=Resources.Load<Texture2D>("Cabin/"+key);
            if(texture==null) { Debug.LogWarning("Arte da cabana ausente: "+key); return null; }
            if(manifest==null)
            {
                var json=Resources.Load<TextAsset>("Cabin/ArtBounds");
                manifest=json!=null ? JsonUtility.FromJson<Manifest>(json.text) : new Manifest();
            }
            Rect rect=new Rect(0,0,texture.width,texture.height);
            if(manifest.entries!=null) foreach(var entry in manifest.entries)
                if(entry.key==key) { rect=new Rect(entry.x*texture.width,entry.y*texture.height,entry.width*texture.width,entry.height*texture.height); break; }
            var asset=new Asset { sprite=Sprite.Create(texture,rect,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect) };
            asset.sprite.name="Cabin_"+key;
            if(SpriteFactory.SpriteMaterial!=null)
            {
                asset.material=new Material(SpriteFactory.SpriteMaterial);
                asset.material.mainTexture=texture;
            }
            assets.Add(key,asset); return asset;
        }

        public static GameObject Place(Transform parent,string key,Vector2 position,Vector2 size,int order,bool preserveAspect=true)
        {
            var asset=Get(key); if(asset==null) return null;
            var go=new GameObject(key); go.transform.SetParent(parent,false);
            go.transform.localPosition=position;
            if(preserveAspect)
            {
                float scale=Mathf.Min(size.x/asset.sprite.bounds.size.x,size.y/asset.sprite.bounds.size.y);
                go.transform.localScale=new Vector3(scale,scale,1);
            }
            else go.transform.localScale=new Vector3(size.x/asset.sprite.bounds.size.x,size.y/asset.sprite.bounds.size.y,1);
            var renderer=go.AddComponent<SpriteRenderer>();
            SpriteFactory.Setup(renderer,asset.sprite,Color.white,order);
            if(asset.material!=null) renderer.sharedMaterial=asset.material;
            return go;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            foreach(var asset in assets.Values)
            {
                if(asset.sprite!=null) UnityEngine.Object.Destroy(asset.sprite);
                if(asset.material!=null) UnityEngine.Object.Destroy(asset.material);
            }
            assets.Clear(); manifest=null;
        }
    }
}
