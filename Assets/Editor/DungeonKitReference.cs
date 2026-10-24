using System;
using System.IO;
using CyberExtraction.Core;
using CyberExtraction.Match;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace CyberExtraction.EditorTools
{
    public static class DungeonKitReference
    {
        public const string Path="Assets/Scenes/Catacumbas_Referencia_Encaixes.unity";
        [MenuItem("Cyber Extraction/Avançado/Referências/Catacumbas — encaixes")]
        public static void Open()
        {
            if(Application.isPlaying) throw new InvalidOperationException("Encaixes disponíveis fora do Play.");
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if(!File.Exists(Path)) Produce();
            EditorSceneManager.OpenScene(Path);
            var view=SceneView.lastActiveSceneView??EditorWindow.GetWindow<SceneView>();
            view.in2DMode=true; view.Frame(new Bounds(new Vector3(26,150,0),new Vector3(65,320,1)),false);
        }
        public static void Produce()
        {
            var catalog=Resources.Load<DungeonArtCatalog>(DungeonArtCatalog.ResourcePath);
            if(catalog==null) throw new InvalidOperationException("Kit ainda não importado.");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            var root=new GameObject("Todos_os_encaixes_47_mascaras"); SceneManager.MoveGameObjectToScene(root,scene);
            var grid=root.AddComponent<Grid>(); grid.cellSize=Vector3.one*1.06f;
            var directions=new[]{Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left,new Vector2Int(1,1),new Vector2Int(1,-1),new Vector2Int(-1,-1),new Vector2Int(-1,1)};
            int family=0;
            foreach(var state in new[]{"Dry","Damp","Cracked","Roots","Ornament","Ruin","Earth","Mixed","RootsFloor","DampFloor","FloodedFloor"})
            {
                var go=new GameObject(state); go.transform.SetParent(root.transform,false);
                var map=go.AddComponent<Tilemap>(); var renderer=go.AddComponent<TilemapRenderer>(); renderer.sharedMaterial=SpriteFactory.SpriteMaterial;
                if(family>=4) renderer.sharedMaterial=DungeonMapArt.SurfaceMaterial(state.Replace("Floor",""))??SpriteFactory.SpriteMaterial;
                int index=0;
                for(int mask=0;mask<256;mask++)
                {
                    if(DungeonWallCatalog.Normalize(mask)!=mask) continue;
                    var center=new Vector2Int(index%10*5,family*28+index/10*5); index++;
                    bool Solid(int x,int y)
                    {
                        var delta=new Vector2Int(x,y)-center; if(delta==Vector2Int.zero) return true;
                        for(int i=0;i<8;i++) if(delta==directions[i]) return (mask&(1<<i))!=0;
                        return false;
                    }
                    for(int y=center.y-1;y<=center.y+1;y++) for(int x=center.x-1;x<=center.x+1;x++)
                    {
                        if(!Solid(x,y)) continue;
                        int local=DungeonWallCatalog.MaskAt(x,y,Solid);
                        string id=family<4 ? "Wall_"+state+"_"+local.ToString("X2") : "Transition_"+state.Replace("Floor","")+"_"+local.ToString("X2");
                        map.SetTile(new Vector3Int(x,y,0),catalog.Find(id)?.Tile);
                    }
                }
                family++;
            }
            EditorSceneManager.SaveScene(scene,Path); EditorSceneManager.CloseScene(scene,true);
        }
    }
}
