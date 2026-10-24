using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CyberExtraction.Match
{
    public sealed class MapVisualEditRuntime : MonoBehaviour
    {
        sealed class Original
        {
            public MapVisualEdit Key;
            public Tilemap Map;
            public Matrix4x4 Matrix;
            public Color Color;
            public TileFlags Flags;
            public SpriteRenderer Renderer;
            public bool Enabled;
        }
        readonly List<Original> originals=new List<Original>();
        readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
        readonly Dictionary<GameObject,MapVisualEdit> visualSources=new Dictionary<GameObject,MapVisualEdit>();
        readonly Dictionary<GameObject,MapVisualAddition> additionSources=new Dictionary<GameObject,MapVisualAddition>();
        public bool TryGetAdditionSource(GameObject visual,out MapVisualAddition addition)=>additionSources.TryGetValue(visual,out addition);
        public SpriteRenderer AdditionVisual(string id)
        { foreach(var pair in additionSources) if(pair.Value.Id==id && pair.Key!=null) return pair.Key.GetComponent<SpriteRenderer>(); return null; }
        public bool TryGetVisualSource(GameObject visual,out MapVisualEdit edit)=>visualSources.TryGetValue(visual,out edit);
        public SpriteRenderer VisualFor(MapVisualEdit edit)
        { foreach(var pair in visualSources) if(pair.Value==edit && pair.Key!=null) return pair.Key.GetComponent<SpriteRenderer>(); return null; }
        public Transform ResolveTarget(string path)=>Resolve(path);
        public static string PathFor(Transform root,Transform target)
        {
            var parts=new List<string>();
            while(target!=null && target!=root)
            {
                // Name + ordinal among siblings of the same name survives unrelated additions.
                int ordinal=0;
                for(int i=0;i<target.GetSiblingIndex();i++) if(target.parent.GetChild(i).name==target.name) ordinal++;
                parts.Add(Uri.EscapeDataString(target.name)+"@"+ordinal); target=target.parent;
            }
            if(target!=root) return null;
            parts.Reverse(); return string.Join("/",parts);
        }
        Transform Resolve(string path)
        {
            var node=transform;
            if(string.IsNullOrEmpty(path)) return node;
            foreach(var part in path.Split('/'))
            {
                int separator=part.LastIndexOf('@');
                if(separator<0 || !int.TryParse(part.Substring(separator+1),out int ordinal)) return null;
                string name=Uri.UnescapeDataString(part.Substring(0,separator)); Transform found=null;
                foreach(Transform child in node) if(child.name==name && ordinal--==0) { found=child; break; }
                if(found==null) return null; node=found;
            }
            return node;
        }
        public static void Apply(Transform world,ExpeditionMap map)
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            var component=world.GetComponent<MapVisualEditRuntime>();
            if(component==null) component=world.gameObject.AddComponent<MapVisualEditRuntime>();
            component.Refresh(Resources.Load<MapVisualEdits>(MapVisualEdits.ResourcePath(map)));
        }
        public void Refresh(MapVisualEdits edits)
        {
            Restore();
            if(edits==null) return;
            foreach(var edit in edits.Edits)
            {
                var target=Resolve(edit.Path);
                if(target==null) { Debug.LogWarning("Correção visual sem alvo: "+edit.Path,this); continue; }
                Sprite replacement=null;
                if(edit.ReplaceSprite && !edit.Hidden)
                {
                    if(edit.Texture==null || edit.PixelsPerUnit<=0 || edit.SpriteRect.width<=0 || edit.SpriteRect.height<=0 ||
                        edit.SpriteRect.xMin<0 || edit.SpriteRect.yMin<0 || edit.SpriteRect.xMax>edit.Texture.width || edit.SpriteRect.yMax>edit.Texture.height)
                    { Debug.LogWarning("Sprite inválido na correção: "+edit.Path,this); continue; }
                    replacement=Sprite.Create(edit.Texture,edit.SpriteRect,edit.Pivot,edit.PixelsPerUnit,0,SpriteMeshType.FullRect);
                    owned.Add(replacement);
                }
                var delta=Matrix4x4.TRS(edit.Offset,Quaternion.Euler(0,0,edit.Rotation),new Vector3(edit.FlipX ? -1 : 1,edit.FlipY ? -1 : 1,1));
                if(edit.Cell)
                {
                    var map=target.GetComponent<Tilemap>(); if(map==null) continue;
                    var original=new Original {Key=edit,Map=map,Matrix=map.GetTransformMatrix(edit.Position),Color=map.GetColor(edit.Position),Flags=map.GetTileFlags(edit.Position)};
                    originals.Add(original);
                    // Render a separate sprite: TilemapCollider2D and the original tile remain untouched.
                    var sourceSprite=map.GetSprite(edit.Position);
                    if(sourceSprite==null && !edit.ReplaceSprite) Debug.LogWarning("Correção visual antiga aponta para célula sem tile: "+edit.Path+" "+edit.Position,this);
                    if(!edit.Hidden && (replacement!=null || sourceSprite!=null))
                    {
                        var visual=new GameObject("MapVisualOverride"); visual.layer=map.gameObject.layer; visual.transform.SetParent(map.transform,false);
                        var matrix=Matrix4x4.Translate(map.CellToLocalInterpolated((Vector3)edit.Position+map.tileAnchor))*map.orientationMatrix*delta*original.Matrix;
                        var x=(Vector3)matrix.GetColumn(0); var y=(Vector3)matrix.GetColumn(1);
                        float sign=Vector3.Cross(x,y).z<0 ? -1 : 1;
                        visual.transform.localPosition=matrix.GetColumn(3);
                        visual.transform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(x.y*sign,x.x*sign)*Mathf.Rad2Deg);
                        var size=Vector3.one;
                        if(replacement!=null && sourceSprite!=null) size=new Vector3(sourceSprite.bounds.size.x/replacement.bounds.size.x,sourceSprite.bounds.size.y/replacement.bounds.size.y,1);
                        visual.transform.localScale=Vector3.Scale(new Vector3(x.magnitude*sign,y.magnitude,1),size);
                        var copy=visual.AddComponent<SpriteRenderer>(); copy.sprite=replacement!=null ? replacement : sourceSprite;
                        copy.color=original.Color*map.color;
                        var renderer=map.GetComponent<TilemapRenderer>();
                        if(renderer!=null) { copy.sharedMaterial=renderer.sharedMaterial; copy.sortingLayerID=renderer.sortingLayerID; copy.sortingOrder=renderer.sortingOrder; copy.maskInteraction=renderer.maskInteraction; }
                        owned.Add(visual); visualSources[visual]=edit;
                    }
                    map.SetTileFlags(edit.Position,TileFlags.None);
                    map.SetColor(edit.Position,Color.clear);
                }
                else
                {
                    var renderer=target.GetComponent<SpriteRenderer>(); if(renderer==null) continue;
                    originals.Add(new Original {Key=edit,Renderer=renderer,Enabled=renderer.enabled});
                    // Renderer-only visual offsets use a child; the collider/authoritative transform stays unchanged.
                    var visual=new GameObject("MapVisualOverride"); visual.layer=target.gameObject.layer; visual.transform.SetParent(target,false);
                    visual.transform.localPosition=edit.Offset; visual.transform.localRotation=Quaternion.Euler(0,0,edit.Rotation);
                    var copy=visual.AddComponent<SpriteRenderer>();
                    copy.sprite=edit.ReplaceSprite ? replacement : renderer.sprite; copy.color=renderer.color;
                    copy.sharedMaterial=renderer.sharedMaterial; copy.sortingLayerID=renderer.sortingLayerID; copy.sortingOrder=renderer.sortingOrder;
                    copy.flipX=renderer.flipX^edit.FlipX; copy.flipY=renderer.flipY^edit.FlipY;
                    copy.drawMode=renderer.drawMode; copy.size=renderer.size; copy.maskInteraction=renderer.maskInteraction;
                    copy.spriteSortPoint=renderer.spriteSortPoint;
                    copy.enabled=renderer.enabled && !edit.Hidden; owned.Add(visual); visualSources[visual]=edit; renderer.enabled=false;
                }
            }
            if(edits.Additions!=null) foreach(var addition in edits.Additions)
            {
                var style=addition.Style;
                if(style==null || style.Hidden || style.Texture==null || style.PixelsPerUnit<=0 || style.SpriteRect.width<=0 || style.SpriteRect.height<=0 ||
                    style.SpriteRect.xMin<0 || style.SpriteRect.yMin<0 || style.SpriteRect.xMax>style.Texture.width || style.SpriteRect.yMax>style.Texture.height) continue;
                var sprite=Sprite.Create(style.Texture,style.SpriteRect,style.Pivot,style.PixelsPerUnit,0,SpriteMeshType.FullRect); owned.Add(sprite);
                var go=new GameObject("MapVisualCopy_"+addition.Id); go.layer=addition.Layer; go.transform.SetParent(transform,false);
                go.transform.localPosition=addition.Position+style.Offset;
                go.transform.localRotation=Quaternion.Euler(0,0,style.Rotation)*addition.Rotation;
                var ratio=addition.SpriteSize.x>0 && addition.SpriteSize.y>0 ? new Vector3(addition.SpriteSize.x/sprite.bounds.size.x,addition.SpriteSize.y/sprite.bounds.size.y,1) : Vector3.one;
                go.transform.localScale=Vector3.Scale(addition.Scale,ratio);
                var renderer=go.AddComponent<SpriteRenderer>(); renderer.sprite=sprite; renderer.color=addition.Color;
                renderer.flipX=style.FlipX; renderer.flipY=style.FlipY;
                renderer.sortingLayerID=addition.SortingLayer; renderer.sortingOrder=addition.SortingOrder;
                renderer.maskInteraction=addition.Mask; renderer.spriteSortPoint=addition.SortPoint;
                renderer.sharedMaterial=addition.Material!=null ? addition.Material : CyberExtraction.Core.SpriteFactory.SpriteMaterial;
                owned.Add(go); additionSources[go]=addition;
            }
        }
        void Restore()
        {
            foreach(var original in originals)
            {
                if(original.Map!=null)
                {
                    original.Map.SetTileFlags(original.Key.Position,TileFlags.None);
                    original.Map.SetColor(original.Key.Position,original.Color);
                    original.Map.SetTileFlags(original.Key.Position,original.Flags);
                }
                if(original.Renderer!=null) original.Renderer.enabled=original.Enabled;
            }
            originals.Clear();
            visualSources.Clear();
            additionSources.Clear();
            foreach(var asset in owned) if(asset!=null)
            { if(asset is GameObject go) go.SetActive(false); if(Application.isPlaying) Destroy(asset); else DestroyImmediate(asset); }
            owned.Clear();
        }
        void OnDestroy() { Restore(); }
    }
}
