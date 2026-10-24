using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using CyberExtraction.Match;

namespace CyberExtraction.EditorTools
{
    public sealed partial class MapVisualEditor
    {
        sealed class Hit
        {
            public Transform Target;
            public Tilemap Layer;
            public Vector3Int Cell;
            public Sprite Sprite;
            public Matrix4x4 Matrix;
            public int SortingLayer,Order;
            public string AdditionId;
        }
        readonly List<Hit> hits=new List<Hit>();
        readonly Dictionary<Texture2D,Texture2D> alphaCopies=new Dictionary<Texture2D,Texture2D>();
        bool advanced;
        float thumbnailSize=96;
        int palettePage;
        bool hasPick;
        Ray lastPick;
        const int PageSize=60;
        MapVisualEditRuntime Runtime=>world!=null ? world.GetComponent<MapVisualEditRuntime>() : null;

        MapVisualEdit EnsureEdit(string label)
        {
            Undo.RecordObject(edits,label);
            var edit=Find();
            if(edit==null) { edit=new MapVisualEdit {Path=MapVisualEditRuntime.PathFor(world,target),Cell=layer!=null,Position=cell}; edits.Edits.Add(edit); }
            return edit;
        }
        void Choose(Hit hit)
        {
            selectedAddition=hit.AdditionId;
            target=hit.Target; layer=hit.Layer; cell=hit.Cell; selectedPalette=-1;
            status="Peça selecionada. Arraste diretamente no mapa ou clique numa miniatura para trocar.";
            Repaint(); SceneView.RepaintAll();
        }
        void FrameMap()
        {
            var scene=SceneView.lastActiveSceneView;
            if(scene!=null) {scene.in2DMode=true; scene.Frame(new Bounds(Vector3.zero,new Vector3(MapBuilder.Width,MapBuilder.Height,1)),false);}
        }
        void FrameSelection()
        {
            var hit=SelectedVisual(); var scene=SceneView.lastActiveSceneView;
            if(hit==null || scene==null) return;
            var bounds=WorldBounds(hit); bounds.Expand(Mathf.Max(bounds.size.x,bounds.size.y)*2+2);
            scene.in2DMode=true; scene.Frame(bounds,false);
        }
        Hit SelectedVisual()
        {
            if(target==null) return null;
            if(SelectedAddition!=null)
            {
                var additionRenderer=Runtime.AdditionVisual(selectedAddition);
                if(additionRenderer==null) return null;
                var hit=RendererHit(additionRenderer,world,null,default); hit.AdditionId=selectedAddition; return hit;
            }
            var edit=Find();
            var visual=edit!=null ? Runtime.VisualFor(edit) : null;
            if(visual!=null && visual.sprite!=null) return RendererHit(visual,target,layer,cell);
            if(layer!=null)
            {
                var sprite=layer.GetSprite(cell);
                return sprite!=null ? new Hit {Target=target,Layer=layer,Cell=cell,Sprite=sprite,Matrix=TileMatrix(layer,cell)} : null;
            }
            var renderer=target.GetComponent<SpriteRenderer>();
            return renderer!=null && renderer.sprite!=null ? RendererHit(renderer,target,null,default) : null;
        }
        static Matrix4x4 TileMatrix(Tilemap map,Vector3Int position)=>map.transform.localToWorldMatrix*
            Matrix4x4.Translate(map.CellToLocalInterpolated((Vector3)position+map.tileAnchor))*map.orientationMatrix*map.GetTransformMatrix(position);
        static Hit RendererHit(SpriteRenderer renderer,Transform source,Tilemap map,Vector3Int position)
        {
            // Simple map sprites use their authored bounds; sliced renderers use the requested size.
            var size=Vector3.one;
            if(renderer.drawMode!=SpriteDrawMode.Simple && renderer.sprite.bounds.size.x>0 && renderer.sprite.bounds.size.y>0)
                size=new Vector3(renderer.size.x/renderer.sprite.bounds.size.x,renderer.size.y/renderer.sprite.bounds.size.y,1);
            size=Vector3.Scale(size,new Vector3(renderer.flipX ? -1 : 1,renderer.flipY ? -1 : 1,1));
            return new Hit {Target=source,Layer=map,Cell=position,Sprite=renderer.sprite,
                Matrix=renderer.transform.localToWorldMatrix*Matrix4x4.Scale(size),SortingLayer=SortingLayer.GetLayerValueFromID(renderer.sortingLayerID),Order=renderer.sortingOrder};
        }
        static Bounds WorldBounds(Hit hit)
        {
            var b=hit.Sprite.bounds;
            var bounds=new Bounds(hit.Matrix.MultiplyPoint3x4(b.min),Vector3.zero);
            bounds.Encapsulate(hit.Matrix.MultiplyPoint3x4(b.max));
            bounds.Encapsulate(hit.Matrix.MultiplyPoint3x4(new Vector3(b.min.x,b.max.y,0)));
            bounds.Encapsulate(hit.Matrix.MultiplyPoint3x4(new Vector3(b.max.x,b.min.y,0)));
            return bounds;
        }
        void DrawSelectionOutline()
        {
            var hit=SelectedVisual(); if(hit==null) return;
            var b=hit.Sprite.bounds;
            var points=new[] {new Vector3(b.min.x,b.min.y),new Vector3(b.max.x,b.min.y),new Vector3(b.max.x,b.max.y),new Vector3(b.min.x,b.max.y),new Vector3(b.min.x,b.min.y)};
            for(int i=0;i<points.Length;i++) points[i]=hit.Matrix.MultiplyPoint3x4(points[i]);
            var previous=Handles.color; Handles.color=Color.cyan; Handles.DrawAAPolyLine(4,points); Handles.color=previous;
            Handles.Label(points[2],"  "+target.name+(layer!=null ? " "+cell : ""));
        }
        void PickVisible(Ray ray,bool select=true)
        {
            if(select) {lastPick=ray; hasPick=true;}
            hits.Clear();
            foreach(var map in world.GetComponentsInChildren<Tilemap>())
            {
                var renderer=map.GetComponent<TilemapRenderer>();
                if(renderer==null || !renderer.enabled || map.color.a<=.01f) continue;
                foreach(var position in map.cellBounds.allPositionsWithin)
                {
                    if(map.GetColor(position).a<=.01f) continue;
                    var sprite=map.GetSprite(position); if(sprite==null) continue;
                    var hit=new Hit {Target=map.transform,Layer=map,Cell=position,Sprite=sprite,Matrix=TileMatrix(map,position),
                        SortingLayer=SortingLayer.GetLayerValueFromID(renderer.sortingLayerID),Order=renderer.sortingOrder};
                    if(ContainsPixel(hit,ray)) hits.Add(hit);
                }
            }
            foreach(var renderer in world.GetComponentsInChildren<SpriteRenderer>())
            {
                if(!renderer.enabled || renderer.sprite==null || renderer.color.a<=.01f) continue;
                Transform source=renderer.transform; Tilemap map=null; Vector3Int position=default;
                if(Runtime.TryGetVisualSource(renderer.gameObject,out var edit))
                { source=Runtime.ResolveTarget(edit.Path); if(source==null) continue; map=edit.Cell ? source.GetComponent<Tilemap>() : null; position=edit.Position; }
                var hit=RendererHit(renderer,source,map,position);
                if(Runtime.TryGetAdditionSource(renderer.gameObject,out var addition)) {hit.AdditionId=addition.Id; hit.Target=world;}
                if(ContainsPixel(hit,ray)) hits.Add(hit);
            }
            hits.Sort((a,b)=> {int order=b.SortingLayer.CompareTo(a.SortingLayer); return order!=0 ? order : b.Order.CompareTo(a.Order);});
            if(select)
            {
                if(hits.Count>0) Choose(hits[0]);
                else {target=null; layer=null; selectedAddition=null; status="Nenhuma peça visível neste ponto."; Repaint(); SceneView.RepaintAll();}
            }
        }
        bool ContainsPixel(Hit hit,Ray ray)
        {
            var plane=new Plane(hit.Matrix.MultiplyVector(Vector3.forward),hit.Matrix.MultiplyPoint3x4(Vector3.zero));
            if(!plane.Raycast(ray,out float distance)) return false;
            var local=hit.Matrix.inverse.MultiplyPoint3x4(ray.GetPoint(distance)); var b=hit.Sprite.bounds;
            if(local.x<b.min.x || local.x>b.max.x || local.y<b.min.y || local.y>b.max.y) return false;
            var texture=hit.Sprite.texture; var rect=hit.Sprite.rect;
            int x=Mathf.Clamp((int)(rect.x+(local.x-b.min.x)/b.size.x*rect.width),0,texture.width-1);
            int y=Mathf.Clamp((int)(rect.y+(local.y-b.min.y)/b.size.y*rect.height),0,texture.height-1);
            try
            {
                if(!texture.isReadable)
                {
                    if(!alphaCopies.TryGetValue(texture,out var copy))
                    {
                        var previous=RenderTexture.active;
                        var render=RenderTexture.GetTemporary(texture.width,texture.height,0,RenderTextureFormat.ARGB32);
                        copy=new Texture2D(texture.width,texture.height,TextureFormat.RGBA32,false) {hideFlags=HideFlags.HideAndDontSave};
                        try { Graphics.Blit(texture,render); RenderTexture.active=render; copy.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0); copy.Apply(); }
                        catch { DestroyImmediate(copy); throw; }
                        finally { RenderTexture.active=previous; RenderTexture.ReleaseTemporary(render); }
                        alphaCopies.Add(texture,copy);
                    }
                    texture=copy;
                }
                return texture.GetPixel(x,y).a>.03f;
            }
            catch { return true; } // A bounds hit remains selectable if GPU readback is unavailable.
        }
        void ClearAlphaCopies()
        { foreach(var copy in alphaCopies.Values) if(copy!=null) DestroyImmediate(copy); alphaCopies.Clear(); }

        static void Thumbnail(Rect area,Texture2D texture,Rect pixels)
        {
            EditorGUI.DrawRect(area,new Color(.12f,.13f,.14f));
            for(int y=0;y<area.height;y+=12) for(int x=0;x<area.width;x+=12)
                if((x/12+y/12)%2==0) EditorGUI.DrawRect(new Rect(area.x+x,area.y+y,Mathf.Min(12,area.width-x),Mathf.Min(12,area.height-y)),new Color(.2f,.21f,.22f));
            if(texture==null || pixels.width<=0 || pixels.height<=0) return;
            float scale=Mathf.Min((area.width-8)/pixels.width,(area.height-8)/pixels.height);
            var fit=new Rect(area.center.x-pixels.width*scale/2,area.center.y-pixels.height*scale/2,pixels.width*scale,pixels.height*scale);
            GUI.DrawTextureWithTexCoords(fit,texture,new Rect(pixels.x/texture.width,pixels.y/texture.height,pixels.width/texture.width,pixels.height/texture.height));
        }
        static void Thumbnail(Rect area,Sprite sprite) { if(sprite!=null) Thumbnail(area,sprite.texture,sprite.rect); }
        void DrawVisualSelection()
        {
            if(hits.Count>1)
            {
                EditorGUILayout.LabelField("Peças sobrepostas no ponto clicado — escolha a desejada",EditorStyles.boldLabel);
                int columns=Mathf.Max(1,Mathf.FloorToInt((position.width-30)/60));
                for(int row=0;row<hits.Count;row+=columns)
                    using(new EditorGUILayout.HorizontalScope())
                        for(int col=0;col<columns && row+col<hits.Count;col++)
                        {
                            var hit=hits[row+col]; var rect=GUILayoutUtility.GetRect(52,52,GUILayout.Width(52),GUILayout.Height(52));
                            Thumbnail(rect,hit.Sprite);
                            if(hit.Target==target && hit.Layer==layer && (layer==null || hit.Cell==cell))
                                EditorGUI.DrawRect(new Rect(rect.x,rect.y,rect.width,3),Color.cyan);
                            if(GUI.Button(rect,new GUIContent("",hit.Target.name),GUIStyle.none)) Choose(hit);
                        }
            }
            if(target==null) {EditorGUILayout.LabelField("Clique numa peça do mapa para selecioná-la.",EditorStyles.boldLabel); return;}
            var visual=SelectedVisual();
            using(new EditorGUILayout.HorizontalScope())
            {
                var rect=GUILayoutUtility.GetRect(128,128,GUILayout.Width(128),GUILayout.Height(128));
                if(visual!=null) Thumbnail(rect,visual.Sprite);
                using(new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField("PEÇA SELECIONADA",EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(target.name);
                    if(layer!=null) EditorGUILayout.LabelField("Célula "+cell);
                    if(GUILayout.Button("Aproximar / localizar (F)")) FrameSelection();
                    using(new EditorGUILayout.HorizontalScope())
                    {
                        if(GUILayout.Button("↶ 90°")) {var edit=EnsureEdit("Girar peça"); edit.Rotation-=90; Changed();}
                        if(GUILayout.Button("↷ 90°")) {var edit=EnsureEdit("Girar peça"); edit.Rotation+=90; Changed();}
                    }
                    using(new EditorGUILayout.HorizontalScope())
                    {
                        if(GUILayout.Button("Espelhar ↔")) {var edit=EnsureEdit("Espelhar peça"); edit.FlipX=!edit.FlipX; Changed();}
                        if(GUILayout.Button("Espelhar ↕")) {var edit=EnsureEdit("Espelhar peça"); edit.FlipY=!edit.FlipY; Changed();}
                    }
                }
            }
            using(new EditorGUILayout.HorizontalScope())
            {
                if(GUILayout.Button("Duplicar (Ctrl+D)")) DuplicateSelection();
                if(GUILayout.Button("Deletar (Delete)")) {DeleteSelection(); GUIUtility.ExitGUI();}
                if(GUILayout.Button("Alinhar à grade")) AlignSelection();
            }
            using(new EditorGUILayout.HorizontalScope())
            {
                var edit=Find();
                if(GUILayout.Button(edit!=null && edit.Hidden ? "Mostrar peça" : "Ocultar peça")) {edit=EnsureEdit("Ocultar/mostrar peça"); edit.Hidden=!edit.Hidden; Changed();}
                using(new EditorGUI.DisabledScope(edit==null || SelectedAddition!=null))
                    if(GUILayout.Button("Restaurar original")) {Undo.RecordObject(edits,"Restaurar peça"); edits.Edits.Remove(edit); Changed();}
            }
        }
        void DrawVisualPalette()
        {
            EditorGUILayout.Space(); EditorGUILayout.LabelField("ESCOLHA A PEÇA SUBSTITUTA",EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            filter=EditorGUILayout.TextField("Buscar",filter);
            thumbnailSize=EditorGUILayout.Slider("Tamanho das miniaturas",thumbnailSize,64,160);
            if(EditorGUI.EndChangeCheck()) palettePage=0;
            var visible=new List<int>();
            for(int i=0;i<palette.Count;i++) if(palette[i]!=null && SpriteName(palette[i]).IndexOf(filter,StringComparison.OrdinalIgnoreCase)>=0) visible.Add(i);
            int pages=Mathf.Max(1,Mathf.CeilToInt(visible.Count/(float)PageSize)); palettePage=Mathf.Clamp(palettePage,0,pages-1);
            using(new EditorGUILayout.HorizontalScope())
            {
                using(new EditorGUI.DisabledScope(palettePage==0)) if(GUILayout.Button("Anterior")) palettePage--;
                GUILayout.Label((palettePage+1)+" / "+pages+" · "+visible.Count+" peças");
                using(new EditorGUI.DisabledScope(palettePage>=pages-1)) if(GUILayout.Button("Próxima")) palettePage++;
            }
            int columns=Mathf.Max(1,Mathf.FloorToInt((position.width-30)/(thumbnailSize+8)));
            for(int start=palettePage*PageSize;start<Mathf.Min(visible.Count,(palettePage+1)*PageSize);start+=columns)
                using(new EditorGUILayout.HorizontalScope())
                    for(int col=0;col<columns && start+col<visible.Count && start+col<(palettePage+1)*PageSize;col++)
                    {
                        int index=visible[start+col]; var sprite=palette[index];
                        using(new EditorGUILayout.VerticalScope(GUILayout.Width(thumbnailSize)))
                        {
                            var area=GUILayoutUtility.GetRect(thumbnailSize,thumbnailSize,GUILayout.Width(thumbnailSize),GUILayout.Height(thumbnailSize));
                            Thumbnail(area,sprite);
                            if(index==selectedPalette) GUI.Box(area,GUIContent.none,EditorStyles.helpBox);
                            if(GUI.Button(area,new GUIContent("",SpriteName(sprite)),GUIStyle.none))
                            {
                                if(target==null) status="Selecione primeiro a peça que deseja substituir, clicando no mapa.";
                                else
                                {
                                    var edit=EnsureEdit("Substituir peça");
                                    if(Capture(edit,sprite)) {edit.ReplaceSprite=true; edit.Hidden=false; selectedPalette=index; Changed();}
                                }
                            }
                            GUILayout.Label(string.IsNullOrEmpty(sprite.name) ? sprite.texture.name : sprite.name,EditorStyles.miniLabel,GUILayout.Width(thumbnailSize));
                        }
                    }
        }
        void DrawAdvanced()
        {
            if(target!=null)
            {
                var edit=Find();
                if(edit!=null)
                {
                    Undo.RecordObject(edits,"Ajustar peça"); EditorGUI.BeginChangeCheck();
                    edit.Offset=EditorGUILayout.Vector3Field("Deslocamento",edit.Offset);
                    edit.Rotation=EditorGUILayout.FloatField("Rotação",edit.Rotation);
                    var sprite=(Sprite)EditorGUILayout.ObjectField("Sprite do projeto",null,typeof(Sprite),false);
                    if(sprite!=null && Capture(edit,sprite)) edit.ReplaceSprite=true;
                    if(EditorGUI.EndChangeCheck()) Changed();
                }
            }
            for(int i=0;i<edits.Edits.Count;i++)
            {
                var edit=edits.Edits[i];
                using(new EditorGUILayout.HorizontalScope())
                {
                    if(GUILayout.Button(edit.Path+(edit.Cell ? " "+edit.Position : "")))
                    { selectedAddition=null; target=Runtime.ResolveTarget(edit.Path); layer=target!=null && edit.Cell ? target.GetComponent<Tilemap>() : null; cell=edit.Position; FrameSelection(); }
                    if(GUILayout.Button("Remover",GUILayout.Width(70))) {Undo.RecordObject(edits,"Remover correção"); edits.Edits.RemoveAt(i); Changed(); GUIUtility.ExitGUI();}
                }
            }
            if(edits.Additions!=null) foreach(var addition in edits.Additions)
                if(GUILayout.Button("Localizar cópia "+addition.Id)) {selectedAddition=addition.Id; target=world; layer=null; FrameSelection();}
        }
    }
}
