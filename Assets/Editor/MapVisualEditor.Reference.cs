using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using CyberExtraction.Match;
using CyberExtraction.Core;

namespace CyberExtraction.EditorTools
{
    public sealed partial class MapVisualEditor
    {
        GameObject reference;
        readonly List<Object> referenceAssets=new List<Object>();
        readonly List<(Vector3 position,string text)> referenceLabels=new List<(Vector3,string)>();
        [MenuItem("Cyber Extraction/Avançado/Referências/Tileset modular")]
        public static void OpenReference()
        {
            if(!Application.isPlaying) { DungeonKitReference.Open(); return; }
            Open(); var window=GetWindow<MapVisualEditor>();
            if(!Application.isPlaying)
            {SessionState.SetBool("CyberExtraction.MapVisualReference",true); SessionState.SetBool("CyberExtraction.MapVisualPreview",true); EditorApplication.isPlaying=true;}
            else if(window.CanEdit()) window.ShowReference();
            else window.status="Abra uma prévia offline para ver a composição de encaixes.";
        }
        void ClearReference()
        {
            if(reference!=null) {reference.SetActive(false); DestroyImmediate(reference);}
            foreach(var asset in referenceAssets) if(asset!=null) DestroyImmediate(asset);
            referenceAssets.Clear(); referenceLabels.Clear(); reference=null;
        }
        void ShowReference()
        {
            var catalog=Resources.Load<DungeonWallCatalog>(DungeonWallCatalog.ResourcePath);
            if(catalog==null) {status="CatacombWalls.wallset ainda não foi importado."; return;}
            ClearReference(); reference=new GameObject("ModularWallReference");
            reference.transform.position=new Vector3(MapBuilder.Width+200,0,0);
            var grid=reference.AddComponent<Grid>(); grid.cellSize=Vector3.one*DungeonLayout.CellSize;
            var go=new GameObject("ReferenceWalls"); go.transform.SetParent(reference.transform,false);
            var map=go.AddComponent<Tilemap>(); var renderer=go.AddComponent<TilemapRenderer>(); renderer.sharedMaterial=SpriteFactory.SpriteMaterial;
            var cells=new HashSet<Vector2Int>();
            void Segment(int x,int y,int length,bool vertical)
            {for(int i=0;i<length;i++) cells.Add(new Vector2Int(x+(vertical ? 0 : i),y+(vertical ? i : 0)));}
            void Room(int x,int y,int width,int height)
            {Segment(x,y,width,false); Segment(x,y+height-1,width,false); Segment(x,y,height,true); Segment(x+width-1,y,height,true);}
            Room(0,0,13,10); Room(12,0,13,10); // shared wall
            Room(28,0,10,10); // isolated closed room
            // Four door orientations in the paired rooms.
            for(int i=0;i<4;i++) {cells.Remove(new Vector2Int(4+i,0)); cells.Remove(new Vector2Int(16+i,9)); cells.Remove(new Vector2Int(0,3+i)); cells.Remove(new Vector2Int(24,3+i));}
            // Narrow corridors and their corners, T and cross intersections.
            Room(0,14,25,5); Segment(10,18,8,true); Segment(14,18,8,true); Segment(5,25,15,false);
            Segment(28,16,10,false); Segment(32,12,11,true); cells.Add(new Vector2Int(40,18));
            // Every valid local neighbourhood, separated so samples cannot influence each other.
            var directions=new[]{Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left,new Vector2Int(1,1),new Vector2Int(1,-1),new Vector2Int(-1,-1),new Vector2Int(-1,1)};
            for(int i=0;i<catalog.Masks.Length;i++)
            {
                var center=new Vector2Int(i%8*5,32+i/8*5); cells.Add(center);
                for(int d=0;d<8;d++) if((catalog.Masks[i]&(1<<d))!=0) cells.Add(center+directions[d]);
                referenceLabels.Add((new Vector3((center.x+.5f)*DungeonLayout.CellSize,(center.y-1.5f)*DungeonLayout.CellSize,0),"Máscara "+catalog.Masks[i].ToString("X2")));
            }
            var tiles=new Dictionary<int,Tile>();
            foreach(var cell in cells)
            {
                int mask=DungeonWallCatalog.MaskAt(cell.x,cell.y,(x,y)=>cells.Contains(new Vector2Int(x,y)));
                if(!tiles.TryGetValue(mask,out var tile))
                {tile=ScriptableObject.CreateInstance<Tile>(); tile.sprite=catalog.ForMask(mask); tile.transform=Matrix4x4.Scale(new Vector3(DungeonLayout.CellSize,DungeonLayout.CellSize,1)); tile.colliderType=Tile.ColliderType.None; tiles[mask]=tile; referenceAssets.Add(tile);}
                map.SetTile(new Vector3Int(cell.x,cell.y,0),tile);
            }
            void Door(Vector2 start,bool vertical,bool reverse=false)
            {
                for(int i=0;i<4;i++)
                {
                    var piece=new GameObject("DoorFinish"); piece.transform.SetParent(reference.transform,false);
                    var p=start+(vertical ? Vector2.up : Vector2.right)*i;
                    piece.transform.localPosition=new Vector3((p.x+.5f)*DungeonLayout.CellSize,(p.y+.5f)*DungeonLayout.CellSize,0);
                    piece.transform.localRotation=Quaternion.Euler(0,0,(vertical ? 90 : 0)+(reverse ? 180 : 0)); piece.transform.localScale=Vector3.one*DungeonLayout.CellSize;
                    SpriteFactory.Setup(piece.AddComponent<SpriteRenderer>(),i==0 ? (reverse ? catalog.DoorRight : catalog.DoorLeft) : i==3 ? (reverse ? catalog.DoorLeft : catalog.DoorRight) : catalog.DoorMiddle,Color.white,1);
                }
            }
            Door(new Vector2(4,0),false); Door(new Vector2(16,9),false,true); Door(new Vector2(0,3),true,true); Door(new Vector2(24,3),true);
            var scene=SceneView.lastActiveSceneView!=null ? SceneView.lastActiveSceneView : GetWindow<SceneView>();
            scene.in2DMode=true; scene.Frame(new Bounds(reference.transform.position+new Vector3(21*DungeonLayout.CellSize,28*DungeonLayout.CellSize,0),new Vector3(48,68,1)),false); scene.Focus();
            status="Composição de referência: cômodos, parede compartilhada, portas, corredores e 47 vizinhanças. Não altera o mapa.";
        }
        void DrawReferenceLabels()
        {if(reference!=null) foreach(var label in referenceLabels) Handles.Label(reference.transform.TransformPoint(label.position),label.text);}
    }
}
