using System;
using UnityEditor;
using UnityEngine;
using CyberExtraction.Match;

namespace CyberExtraction.EditorTools
{
    public sealed partial class MapVisualEditor
    {
        string selectedAddition;
        bool showGrid=true,snapGrid,directDrag,handleDrag;
        int subdivision,dragGroup=-1,dragControl;
        Vector2 objectOrigin,objectSpacing=Vector2.one;
        Vector3 dragStartPointer,dragStartPosition;
        string GridKey=>"CyberExtraction.MapEditor.Grid."+(byte)selectedMap+".";
        UnityEngine.Tilemaps.Tilemap SelectionGrid()=>layer!=null ? layer : SelectedAddition!=null && !string.IsNullOrEmpty(SelectedAddition.GridPath) ? Runtime.ResolveTarget(SelectedAddition.GridPath)?.GetComponent<UnityEngine.Tilemaps.Tilemap>() : target==null && world!=null ? world.GetComponentInChildren<UnityEngine.Tilemaps.Tilemap>() : null;
        Vector3 SelectionAnchor()=>SelectedAddition!=null ? SelectedAddition.Position : layer!=null ? layer.GetCellCenterLocal(cell) : Vector3.zero;
        Vector3 SelectionPosition()=>target.TransformPoint(SelectionAnchor()+(Find()?.Offset ?? Vector3.zero));
        Matrix4x4 GridMatrix()
        {
            float fraction=1f/(1<<subdivision);
            var grid=SelectionGrid();
            if(grid!=null)
            {
                var origin=grid.GetCellCenterWorld(Vector3Int.zero);
                var x=(grid.GetCellCenterWorld(Vector3Int.right)-origin)*fraction;
                var y=(grid.GetCellCenterWorld(Vector3Int.up)-origin)*fraction;
                var matrix=Matrix4x4.identity;
                matrix.SetColumn(0,new Vector4(x.x,x.y,x.z,0)); matrix.SetColumn(1,new Vector4(y.x,y.y,y.z,0));
                matrix.SetColumn(2,new Vector4(grid.transform.forward.x,grid.transform.forward.y,grid.transform.forward.z,0));
                matrix.SetColumn(3,new Vector4(origin.x,origin.y,origin.z,1)); return matrix;
            }
            return Matrix4x4.TRS(new Vector3(objectOrigin.x,objectOrigin.y,0),Quaternion.identity,new Vector3(objectSpacing.x*fraction,objectSpacing.y*fraction,1));
        }
        Vector3 ConstrainPosition(Vector3 point,bool shift,bool force=false)
        {
            float z=SelectionPosition().z;
            if(force || (snapGrid^shift))
            {
                var matrix=GridMatrix(); var local=matrix.inverse.MultiplyPoint3x4(point);
                local.x=Mathf.Round(local.x); local.y=Mathf.Round(local.y); point=matrix.MultiplyPoint3x4(local);
            }
            point.z=z; return point;
        }
        void LoadGridPreferences()
        {
            showGrid=EditorPrefs.GetBool(GridKey+"visible",true); snapGrid=EditorPrefs.GetBool(GridKey+"snap",false);
            subdivision=Mathf.Clamp(EditorPrefs.GetInt(GridKey+"subdivision",0),0,2);
            objectOrigin=new Vector2(EditorPrefs.GetFloat(GridKey+"x",0),EditorPrefs.GetFloat(GridKey+"y",0));
            objectSpacing=new Vector2(Mathf.Max(.01f,EditorPrefs.GetFloat(GridKey+"width",1)),Mathf.Max(.01f,EditorPrefs.GetFloat(GridKey+"height",1)));
        }
        void DrawGridControls()
        {
            EditorGUI.BeginChangeCheck();
            using(new EditorGUILayout.HorizontalScope())
            {showGrid=GUILayout.Toggle(showGrid,"Mostrar grade"); snapGrid=GUILayout.Toggle(snapGrid,"Encaixar na grade");}
            subdivision=EditorGUILayout.Popup("Passo",subdivision,new[]{"1 célula","½ célula","¼ célula"});
            if(SelectionGrid()==null)
            {objectOrigin=EditorGUILayout.Vector2Field("Origem da grade",objectOrigin); objectSpacing=EditorGUILayout.Vector2Field("Espaçamento",objectSpacing); objectSpacing.x=Mathf.Max(.01f,objectSpacing.x); objectSpacing.y=Mathf.Max(.01f,objectSpacing.y);}
            else EditorGUILayout.LabelField("Grade da camada selecionada · Shift inverte o encaixe");
            if(EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(GridKey+"visible",showGrid); EditorPrefs.SetBool(GridKey+"snap",snapGrid); EditorPrefs.SetInt(GridKey+"subdivision",subdivision);
                EditorPrefs.SetFloat(GridKey+"x",objectOrigin.x); EditorPrefs.SetFloat(GridKey+"y",objectOrigin.y);
                EditorPrefs.SetFloat(GridKey+"width",objectSpacing.x); EditorPrefs.SetFloat(GridKey+"height",objectSpacing.y); SceneView.RepaintAll();
            }
        }
        void AlignSelection()
        {
            if(target==null) return;
            var point=ConstrainPosition(SelectionPosition(),false,true);
            var edit=EnsureEdit("Alinhar peça à grade"); edit.Offset=target.InverseTransformPoint(point)-SelectionAnchor(); Changed();
        }
        void DeleteSelection()
        {
            if(target==null) return;
            if(SelectedAddition!=null)
            {Undo.RecordObject(edits,"Deletar cópia visual"); edits.Additions.Remove(SelectedAddition); selectedAddition=null; target=null; layer=null;}
            else {var edit=EnsureEdit("Deletar peça visual"); edit.Hidden=true;}
            Changed();
        }
        void DuplicateSelection()
        {
            var hit=SelectedVisual(); if(hit==null || Find()?.Hidden==true) return;
            var style=new MapVisualEdit(); if(!CaptureDuplicate(style,hit.Sprite)) return;
            var source=SelectedAddition!=null ? Runtime.AdditionVisual(selectedAddition) : Find()!=null ? Runtime.VisualFor(Find()) : target.GetComponent<SpriteRenderer>();
            if(source==null) source=target.GetComponent<SpriteRenderer>();
            var matrix=world.worldToLocalMatrix*hit.Matrix;
            var x=(Vector3)matrix.GetColumn(0); var y=(Vector3)matrix.GetColumn(1); float sign=Vector3.Cross(x,y).z<0 ? -1 : 1;
            Undo.RecordObject(edits,"Duplicar peça visual");
            var addition=new MapVisualAddition {Id=Guid.NewGuid().ToString("N"),Style=style,
                GridPath=SelectedAddition!=null ? SelectedAddition.GridPath : layer!=null ? MapVisualEditRuntime.PathFor(world,layer.transform) : null,
                SpriteSize=hit.Sprite.bounds.size,
                Position=(Vector3)matrix.GetColumn(3)+world.InverseTransformVector(GridMatrix().MultiplyVector(Vector3.right)),
                Rotation=Quaternion.Euler(0,0,Mathf.Atan2(x.y*sign,x.x*sign)*Mathf.Rad2Deg),Scale=new Vector3(x.magnitude*sign,y.magnitude,1),
                Color=source!=null ? source.color : layer!=null ? layer.GetColor(cell)*layer.color : Color.white,
                Layer=source!=null ? source.gameObject.layer : layer.gameObject.layer,
                Mask=source!=null ? source.maskInteraction : layer.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>().maskInteraction,
                SortPoint=source!=null ? source.spriteSortPoint : SpriteSortPoint.Center,
                SortingLayer=source!=null ? source.sortingLayerID : layer.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>().sortingLayerID,
                SortingOrder=source!=null ? source.sortingOrder : layer.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>().sortingOrder};
            var material=source!=null ? source.sharedMaterial : layer.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>().sharedMaterial;
            if(EditorUtility.IsPersistent(material)) addition.Material=material;
            if(edits.Additions==null) edits.Additions=new System.Collections.Generic.List<MapVisualAddition>();
            edits.Additions.Add(addition); selectedAddition=addition.Id; target=world; layer=null; Changed(); FrameSelection();
        }
        bool CaptureDuplicate(MapVisualEdit style,Sprite sprite)
        {
            if(EditorUtility.IsPersistent(sprite.texture)) return Capture(style,sprite);
            // Persist generated source textures (plain walls/crates included) without depending on Play objects.
            Texture2D readable=null; var previous=RenderTexture.active;
            var render=RenderTexture.GetTemporary(sprite.texture.width,sprite.texture.height,0,RenderTextureFormat.ARGB32);
            try
            {
                readable=new Texture2D(sprite.texture.width,sprite.texture.height,TextureFormat.RGBA32,false);
                Graphics.Blit(sprite.texture,render); RenderTexture.active=render;
                readable.ReadPixels(new Rect(0,0,render.width,render.height),0,0); readable.Apply();
                byte[] png=readable.EncodeToPNG(); string hash;
                using(var sha=System.Security.Cryptography.SHA256.Create()) hash=BitConverter.ToString(sha.ComputeHash(png)).Replace("-","").ToLowerInvariant();
                string folder="Assets/Resources/MapEdits/Generated";
                if(!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Resources/MapEdits","Generated");
                string path=folder+"/"+hash+".png";
                if(!System.IO.File.Exists(path))
                {
                    System.IO.File.WriteAllBytes(path,png); AssetDatabase.ImportAsset(path);
                    var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                    importer.textureType=TextureImporterType.Default; importer.alphaIsTransparency=true;
                    importer.textureCompression=TextureImporterCompression.Uncompressed; importer.mipmapEnabled=false;
                    importer.npotScale=TextureImporterNPOTScale.None; importer.wrapMode=sprite.texture.wrapMode;
                    importer.filterMode=sprite.texture.filterMode; importer.maxTextureSize=Mathf.Max(32,Mathf.NextPowerOfTwo(Mathf.Max(readable.width,readable.height)));
                    importer.SaveAndReimport();
                }
                style.Texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path); style.SpriteRect=sprite.rect;
                style.Pivot=new Vector2(sprite.pivot.x/sprite.rect.width,sprite.pivot.y/sprite.rect.height); style.PixelsPerUnit=sprite.pixelsPerUnit;
                return style.Texture!=null;
            }
            catch(Exception error) {status="Não foi possível persistir a cópia: "+error.Message; return false;}
            finally {RenderTexture.active=previous; RenderTexture.ReleaseTemporary(render); if(readable!=null) DestroyImmediate(readable);}
        }
        void BeginHandleDrag()
        {
            if(handleDrag) return; handleDrag=true; BeginDragUndo();
        }
        void BeginDragUndo()
        {if(dragGroup>=0) return; Undo.IncrementCurrentGroup(); dragGroup=Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Mover peça visual");}
        void BeginDirectDrag(Event e)
        {
            if(target==null) return;
            dragStartPosition=SelectionPosition();
            var plane=new Plane(Vector3.forward,dragStartPosition);
            var ray=HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if(!plane.Raycast(ray,out float distance)) return;
            dragStartPointer=ray.GetPoint(distance); directDrag=true;
            dragControl=GUIUtility.GetControlID("MapVisualDirectDrag".GetHashCode(),FocusType.Passive); GUIUtility.hotControl=dragControl;
        }
        void EndDrag()
        {
            if(dragGroup>=0) {Undo.FlushUndoRecordObjects(); Undo.CollapseUndoOperations(dragGroup);}
            if(directDrag && GUIUtility.hotControl==dragControl) GUIUtility.hotControl=0;
            dragGroup=-1; directDrag=false; handleDrag=false;
        }
        void CancelDrag()
        {
            if(dragGroup>=0) {Undo.FlushUndoRecordObjects(); Undo.RevertAllDownToGroup(dragGroup);}
            dragGroup=-1; if(directDrag || handleDrag) GUIUtility.hotControl=0; directDrag=false; handleDrag=false;
        }
        bool ManipulateScene(SceneView scene)
        {
            var e=Event.current;
            if((directDrag || handleDrag) && e.type==EventType.KeyDown && e.keyCode==KeyCode.Escape)
            {CancelDrag(); Reload(); e.Use(); return true;}
            if((directDrag || handleDrag) && e.type==EventType.MouseUp && e.button==0)
            {bool consume=directDrag; EndDrag(); if(consume) {e.Use(); return true;}}
            if(directDrag && e.type==EventType.MouseDrag && target!=null)
            {
                var plane=new Plane(Vector3.forward,dragStartPosition); var ray=HandleUtility.GUIPointToWorldRay(e.mousePosition);
                if(plane.Raycast(ray,out float distance))
                {
                    BeginDragUndo(); var edit=EnsureEdit("Mover peça visual");
                    var point=ConstrainPosition(dragStartPosition+ray.GetPoint(distance)-dragStartPointer,e.shift);
                    edit.Offset=target.InverseTransformPoint(point)-SelectionAnchor(); Changed();
                }
                e.Use(); return true;
            }
            if(target==null || EditorGUIUtility.editingTextField) return false;
            if(e.type==EventType.KeyDown && e.keyCode==KeyCode.Delete) {DeleteSelection(); e.Use(); return true;}
            if((e.type==EventType.ValidateCommand || e.type==EventType.ExecuteCommand) && e.commandName=="Duplicate")
            {if(e.type==EventType.ExecuteCommand) DuplicateSelection(); e.Use(); return true;}
            if(e.type==EventType.KeyDown && (e.control || e.command) && e.keyCode==KeyCode.D) {DuplicateSelection(); e.Use(); return true;}
            return false;
        }
        void DrawMapGrid(SceneView scene)
        {
            if(!showGrid || scene.camera==null) return;
            var matrix=GridMatrix(); var inverse=matrix.inverse;
            var plane=new Plane(matrix.MultiplyVector(Vector3.forward),matrix.MultiplyPoint3x4(Vector3.zero));
            Vector2 min=new Vector2(float.PositiveInfinity,float.PositiveInfinity),max=new Vector2(float.NegativeInfinity,float.NegativeInfinity);
            for(int i=0;i<4;i++)
            {
                var ray=scene.camera.ViewportPointToRay(new Vector3(i%2,i/2,0));
                if(!plane.Raycast(ray,out float distance)) return;
                var p=inverse.MultiplyPoint3x4(ray.GetPoint(distance)); min=Vector2.Min(min,p); max=Vector2.Max(max,p);
            }
            var a=scene.camera.WorldToScreenPoint(matrix.MultiplyPoint3x4(Vector3.zero));
            var b=scene.camera.WorldToScreenPoint(matrix.MultiplyPoint3x4(Vector3.right));
            float pixels=Vector2.Distance(a,b); int stride=1;
            while((pixels*stride<12 || (max.x-min.x+max.y-min.y)/stride>400) && stride<1048576) stride*=2;
            var previous=Handles.color; Handles.color=new Color(.5f,.75f,.85f,.23f);
            for(float x=Mathf.Floor(min.x/stride)*stride;x<=max.x;x+=stride) Handles.DrawLine(matrix.MultiplyPoint3x4(new Vector3(x,min.y,0)),matrix.MultiplyPoint3x4(new Vector3(x,max.y,0)));
            for(float y=Mathf.Floor(min.y/stride)*stride;y<=max.y;y+=stride) Handles.DrawLine(matrix.MultiplyPoint3x4(new Vector3(min.x,y,0)),matrix.MultiplyPoint3x4(new Vector3(max.x,y,0)));
            Handles.color=previous;
        }
    }
}
