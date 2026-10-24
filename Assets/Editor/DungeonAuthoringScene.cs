using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CyberExtraction.Core;
using CyberExtraction.Match;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CyberExtraction.EditorTools
{
    public static class DungeonAuthoringScene
    {
        public const string ScenePath="Assets/Scenes/Catacumbas_Autoria.unity";
        public static DungeonMapAuthoring Open()
        {
            if(Application.isPlaying) throw new InvalidOperationException("A autoria funciona fora do Play.");
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return null;
            if(File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath);
                var existing=UnityEngine.Object.FindFirstObjectByType<DungeonMapAuthoring>();
                if(existing==null) throw new InvalidOperationException("Cena de autoria sem raiz; arquivo preservado para recuperação.");
                if(!File.ReadAllText(ScenePath).Contains("floorTint:")) existing.Document.floorTint=new Color(.82f,.85f,.86f,1);
                if(existing.Document==null || existing.Document.cells==null || existing.Document.cells.Length!=16384)
                { Initialize(existing); EditorSceneManager.SaveScene(existing.gameObject.scene); }
                else RefreshArt(existing);
                return existing;
            }
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=new GameObject("Catacumbas_Autoria").AddComponent<DungeonMapAuthoring>();
            Initialize(root);
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene,ScenePath);
            return root;
        }
        static void Initialize(DungeonMapAuthoring root)
        {
            root.Catalog=AssetDatabase.LoadAssetAtPath<DungeonArtCatalog>("Assets/Resources/"+DungeonArtCatalog.ResourcePath+".catacombkit");
            root.MigratedCatalog=Resources.Load<DungeonArtCatalog>(DungeonArtCatalog.MigrationPath);
            if(root.Catalog==null) throw new InvalidOperationException("Aguarde a importação do catálogo CatacombArt.catacombkit.");
            root.Document=Seed(); EnsurePhysicalTiles(root); Materialize(root);
        }
        [Serializable] sealed class Recipes
        {
            public DungeonVisualRecord[] visuals;
            public DungeonSurfaceRecord[] surfaces;
            public int[] dressedRooms;
            public DungeonPointRecord[] pointOverrides;
        }
        public static DungeonMapDocument Seed()
        {
            var published=DungeonMapPublication.Current;
            if(published!=null) return JsonUtility.FromJson<DungeonMapDocument>(JsonUtility.ToJson(published));
            var d=new DungeonMapDocument {cells=new int[16384],sectors=new int[16384]};
            for(int y=0;y<128;y++) for(int x=0;x<128;x++)
            {
                int i=y*128+x;
                d.cells[i]=DungeonLayout.IsLowCover(x,y) ? 3 : DungeonLayout.IsBaseFloor(x,y) ? 1 : DungeonLayout.IsWater(x,y) ? 2 : 0;
                d.sectors[i]=DungeonLayout.SectorAt(x,y);
            }
            d.rooms=DungeonLayout.Rooms.Select((r,i)=>new DungeonRoomRecord {id=i,name=r.Name,sector=r.Sector,x=r.Cells.x,y=r.Cells.y,width=r.Cells.width,height=r.Cells.height}).ToArray();
            var points=new List<DungeonPointRecord>();
            void Point(string id,Vector2 position)=>points.Add(new DungeonPointRecord {id=id,position=position});
            for(int i=0;i<8;i++) Point("Spawn/"+i,DungeonLayout.Spawns[i]);
            for(int i=0;i<3;i++) Point("Exit/"+i,DungeonLayout.ExitPositions[i]);
            for(int i=0;i<4;i++) Point("Mechanism/"+i,DungeonContent.Mechanism(i));
            for(int i=0;i<24;i++) Point("Exploration/"+i,DungeonLayout.ExplorationPoints[i]);
            for(int i=0;i<DungeonContent.Loot.Length;i++) Point("Loot/"+i,DungeonContent.Loot[i].Position);
            for(int i=0;i<DungeonContent.Encounters.Length;i++) Point("Encounter/"+i,DungeonContent.Encounters[i].Position);
            foreach(var p in DungeonBreakableLayout.Load().candidates)
                points.Add(new DungeonPointRecord {id="Breakable/"+p.id,position=p.Position,family=p.family,variant=p.variant,angle=p.angle});
            Point("CrankPedestal",DungeonContent.CrankPedestal); Point("KeyPedestal",DungeonContent.KeyPedestal);
            Point("BoatEnd",DungeonLayout.ExitPositions[2]+DungeonLayout.CanalDeparture);
            Point("LiftEnd",DungeonLayout.ExitPositions[1]+Vector2.up*2);
            d.points=points.ToArray();
            var regions=new List<DungeonRectRecord>();
            void Region(string id,Rect r)=>regions.Add(new DungeonRectRecord {id=id,x=r.x,y=r.y,width=r.width,height=r.height});
            foreach(var pair in new[]{("CanalArea",DungeonLayout.CanalArea),("CanalEastRiver",DungeonLayout.CanalEastRiver),
                ("CanalSouthRiver",DungeonLayout.CanalSouthRiver),("CanalBridge",DungeonLayout.CanalBridge),
                ("CanalDetour",DungeonLayout.CanalDetour),("CanalBoarding",DungeonLayout.CanalBoarding)})
                Region(pair.Item1,new Rect(pair.Item2.x,pair.Item2.y,pair.Item2.width,pair.Item2.height));
            for(int i=0;i<3;i++) Region("StairBorder/"+i,DungeonLayout.StairBorder(i));
            for(int i=0;i<DungeonLayout.Pillars.Count;i++)
            { var r=DungeonLayout.Pillars[i]; Region("Pillar/"+i,new Rect(r.x,r.y,r.width,r.height)); }
            for(int i=0;i<DungeonLayout.Covers.Count;i++)
            { var r=DungeonLayout.Covers[i]; Region("Cover/"+i,new Rect(r.x,r.y,r.width,r.height)); }
            d.regions=regions.ToArray();
            d.fidelity=d.rooms.Select(r=>new DungeonFidelityRecord {room=r.id,concept="Art/Catacumbas/ReworkCompleto/Conceito-"+new[]{"Vestibulos","Galerias","Ossarios","Claustro","Cisternas","Prisao","Capela","Contrapesos"}[r.sector]+"-v1.png",pendingDeviations="Produzir e comparar a montagem real antes de encerrar esta sala."}).ToArray();
            d.legacyMigration="pending";
            var recipeSource=Resources.Load<TextAsset>("Environment/Dungeon/ReworkComplete/AuthoringRecipes");
            if(recipeSource!=null)
            {
                var recipes=JsonUtility.FromJson<Recipes>(recipeSource.text);
                d.visuals=recipes.visuals??Array.Empty<DungeonVisualRecord>(); d.surfaces=recipes.surfaces??Array.Empty<DungeonSurfaceRecord>();
                d.dressedRooms=recipes.dressedRooms??Array.Empty<int>();
                foreach(var change in recipes.pointOverrides??Array.Empty<DungeonPointRecord>())
                { var point=Array.Find(d.points,p=>p.id==change.id);if(point!=null) point.position=change.position; }
            }
            return d;
        }
        static void EnsurePhysicalTiles(DungeonMapAuthoring root)
        {
            const string folder="Assets/Art/Catacumbas/Authoring";
            Directory.CreateDirectory(folder); AssetDatabase.Refresh();
            var tiles=new Tile[4]; var colors=new[]{new Color(.12f,.13f,.13f),new Color(.55f,.5f,.4f),new Color(.08f,.35f,.45f),new Color(.75f,.55f,.2f)};
            for(int i=0;i<4;i++)
            {
                string path=folder+"/Physical_"+i+".asset";
                tiles[i]=AssetDatabase.LoadAssetAtPath<Tile>(path);
                if(tiles[i]!=null) continue;
                var tile=ScriptableObject.CreateInstance<Tile>(); tile.name="Physical_"+i; tile.colliderType=Tile.ColliderType.None;
                AssetDatabase.CreateAsset(tile,path);
                var texture=new Texture2D(2,2,TextureFormat.RGBA32,false) {name="Color_"+i,filterMode=FilterMode.Point};
                texture.SetPixels(new[]{colors[i],colors[i],colors[i],colors[i]}); texture.Apply();
                var sprite=Sprite.Create(texture,new Rect(0,0,2,2),Vector2.one*.5f,2); sprite.name="PhysicalSprite_"+i;
                AssetDatabase.AddObjectToAsset(texture,tile); AssetDatabase.AddObjectToAsset(sprite,tile); tile.sprite=sprite;
                EditorUtility.SetDirty(tile); tiles[i]=tile;
            }
            root.StoneTile=tiles[0]; root.FloorTile=tiles[1]; root.WaterTile=tiles[2]; root.CoverTile=tiles[3]; AssetDatabase.SaveAssets();
        }
        static GameObject Child(Transform parent,string name,Vector2 position)
        { var go=new GameObject(name); go.transform.SetParent(parent,false); go.transform.position=position; return go; }
        static Tilemap Terrain(DungeonMapAuthoring root,Transform parent,string name,int priority)
        {
            var grid=Child(parent,name+"_Grid",Vector2.one*(-64*1.06f)); grid.transform.localScale=Vector3.one*1.06f; grid.AddComponent<Grid>();
            var go=Child(grid.transform,name,grid.transform.position); go.transform.localScale=Vector3.one;
            var map=go.AddComponent<Tilemap>(); var render=go.AddComponent<TilemapRenderer>(); render.enabled=false;
            go.AddComponent<DungeonTerrainSource>().Priority=priority; return map;
        }
        static Tile For(DungeonMapAuthoring root,int kind)=>kind==0 ? root.StoneTile : kind==1 ? root.FloorTile : kind==2 ? root.WaterTile : root.CoverTile;
        public static GameObject PlaceCover(DungeonMapAuthoring root,DungeonArtEntry entry,Vector2 position,int room,bool requireFloor=true)
        {
            int w=Mathf.Max(1,Mathf.RoundToInt(entry.WorldSize.x/1.06f)),h=Mathf.Max(1,Mathf.RoundToInt(entry.WorldSize.y/1.06f));
            int x=Mathf.RoundToInt(position.x/1.06f+64-w*.5f),y=Mathf.RoundToInt(position.y/1.06f+64-h*.5f);
            if(x<0 || y<0 || x+w>128 || y+h>128) throw new InvalidOperationException("A cobertura precisa caber na grade.");
            if(requireFloor)
            {
                var data=DungeonAuthoringCompiler.Capture(root,out var errors);
                if(data==null) throw new InvalidOperationException(string.Join("\n",errors));
                for(int yy=y;yy<y+h;yy++) for(int xx=x;xx<x+w;xx++)
                    if(data.cells[yy*128+xx]!=1) throw new InvalidOperationException("Coloque a nova cobertura sobre piso livre ou edite a cobertura existente.");
            }
            var center=new Vector2((x+w*.5f-64)*1.06f,(y+h*.5f-64)*1.06f);
            var group=Child(root.transform,entry.Id+" — mover cobertura",center);
            var map=Terrain(root,group.transform,"PegadaFisica",2);
            for(int yy=y;yy<y+h;yy++) for(int xx=x;xx<x+w;xx++) map.SetTile(new Vector3Int(xx,yy,0),root.CoverTile);
            var marker=Child(group.transform,"Limites da cobertura",center).AddComponent<DungeonAuthoringElement>();
            marker.Kind=DungeonElementKind.Region;marker.Id="Cover/"+Guid.NewGuid().ToString("N");marker.CellCoordinates=true;marker.Size=new Vector2(w,h)*1.06f;
            var visual=Place(root,entry,center);visual.transform.SetParent(group.transform,true);visual.GetComponent<DungeonAuthoringElement>().Room=room;
            return group;
        }
        public static DungeonTerrainSource CreateSurface(DungeonMapAuthoring root,int order,string family="")
        {
            var map=Terrain(root,root.transform,"Superficie_"+order,0); var source=map.GetComponent<DungeonTerrainSource>();
            source.VisualSurface=true; source.SurfaceOrder=order; source.SurfaceFamily=family;
            map.color=order<=-6 ? root.Document.floorTint : Color.white;
            var render=map.GetComponent<TilemapRenderer>(); render.enabled=true; render.sortingOrder=order; render.sharedMaterial=DungeonMapArt.SurfaceMaterial(family)??SpriteFactory.SpriteMaterial;
            return source;
        }
        public static void Materialize(DungeonMapAuthoring root)
        {
            var d=root.Document;
            var functional=Child(root.transform,"Functional",Vector2.zero).transform;
            var groups=new Dictionary<string,Transform>();
            Transform Assembly(string id,Vector2 position)
            {
                if(!groups.TryGetValue(id,out var group)) { group=Child(functional,id,position).transform; groups.Add(id,group); }
                return group;
            }
            var exits=d.points.Where(p=>p.id.StartsWith("Exit/")).ToDictionary(p=>p.id,p=>p.position);
            var canal=Assembly("Canal — mover conjunto",exits["Exit/2"]);
            var stair=Assembly("Escadaria — mover conjunto",exits["Exit/0"]);
            var well=Assembly("Poço — mover conjunto",exits["Exit/1"]);
            var baseMap=Terrain(root,root.transform,"Terreno",0);
            var canalMap=Terrain(root,canal,"TerrenoCanal",1);
            var canalCells=new HashSet<int>();
            foreach(var r in d.regions.Where(r=>r.id=="CanalArea" || r.id=="CanalDetour"))
                for(int y=(int)r.y;y<r.y+r.height;y++) for(int x=(int)r.x;x<r.x+r.width;x++) canalCells.Add(y*128+x);
            for(int y=0;y<128;y++) for(int x=0;x<128;x++)
            {
                int i=y*128+x; (canalCells.Contains(i) ? canalMap : baseMap).SetTile(new Vector3Int(x,y,0),For(root,d.cells[i]));
            }
            foreach(var r in d.regions)
            {
                bool cells=r.id.StartsWith("Canal") || r.id.StartsWith("Pillar/") || r.id.StartsWith("Cover/");
                var bounds=cells ? new Rect((r.x-64)*1.06f,(r.y-64)*1.06f,r.width*1.06f,r.height*1.06f) : r.Rect;
                var parent=r.id.StartsWith("Canal") ? canal : r.id.StartsWith("StairBorder/") ? stair : root.transform;
                if(r.id.StartsWith("Cover/") || r.id.StartsWith("Pillar/"))
                {
                    bool inCanal=canalCells.Contains((int)r.y*128+(int)r.x);
                    parent=Child(inCanal ? canal : root.transform,r.id+" — mover conjunto",bounds.center).transform;
                    var obstacle=Terrain(root,parent,"PegadaFisica",2);
                    for(int y=(int)r.y;y<r.y+r.height;y++) for(int x=(int)r.x;x<r.x+r.width;x++)
                    {
                        (inCanal ? canalMap : baseMap).SetTile(new Vector3Int(x,y,0),root.FloorTile);
                        obstacle.SetTile(new Vector3Int(x,y,0),r.id.StartsWith("Cover/") ? root.CoverTile : root.StoneTile);
                    }
                }
                var marker=Child(parent,r.id,bounds.center).AddComponent<DungeonAuthoringElement>();
                marker.Id=r.id; marker.Kind=DungeonElementKind.Region; marker.CellCoordinates=cells; marker.Size=bounds.size;
            }
            foreach(var p in d.points)
            {
                var parent=p.id=="Exit/2" || p.id=="Mechanism/2" || p.id=="Mechanism/3" || p.id=="BoatEnd" ? canal :
                    p.id=="Exit/0" || p.id=="Mechanism/0" ? stair : p.id=="Exit/1" || p.id=="Mechanism/1" || p.id=="LiftEnd" ? well : functional;
                var marker=Child(parent,p.id,p.position).AddComponent<DungeonAuthoringElement>(); marker.Id=p.id; marker.Kind=DungeonElementKind.Point;
                if(p.id.StartsWith("Exit/")) marker.transform.hideFlags|=HideFlags.NotEditable;
                if(p.id.StartsWith("Breakable/") && !string.IsNullOrEmpty(p.family))
                {
                    marker.Family=p.family; marker.Variant=p.variant;
                    var entry=BreakableEntry(root,p.family,p.variant);
                    if(entry?.Sprite!=null)
                    {
                        var render=marker.gameObject.AddComponent<SpriteRenderer>(); SpriteFactory.Setup(render,entry.Sprite,Color.white,-5);
                        marker.transform.localScale=new Vector3(.9f/entry.Sprite.bounds.size.x,.9f/entry.Sprite.bounds.size.y,1);
                        marker.transform.rotation=Quaternion.Euler(0,0,p.angle);
                        marker.ImportedSpriteSize=entry.Sprite.bounds.size;
                    }
                }
            }
            foreach(var r in d.rooms)
            {
                var marker=Child(root.transform,"Sala_"+r.id.ToString("00"),new Vector2((r.x+r.width*.5f-64)*1.06f,(r.y+r.height*.5f-64)*1.06f)).AddComponent<DungeonAuthoringElement>();
                marker.Kind=DungeonElementKind.Room; marker.Room=r.id; marker.RoomName=r.name; marker.Sector=r.sector; marker.Size=new Vector2(r.width,r.height)*1.06f; marker.Id="Room/"+r.id;
            }
            foreach(var v in d.visuals) Place(root,root.FindArt(v.asset),v.position,v);
            foreach(var v in d.functionalVisuals) PlaceFunctional(root,v);
            foreach(var group in d.surfaces.GroupBy(s=>(s.order,family:DungeonMapArt.SurfaceFamily(s.asset))))
            {
                var map=CreateSurface(root,group.Key.order,group.Key.family).GetComponent<Tilemap>();
                foreach(var s in group) { var p=new Vector3Int(s.x,s.y,0); var tile=root.FindArt(s.asset)?.Tile; map.SetTile(p,tile); map.SetTileFlags(p,TileFlags.None); map.SetColor(p,s.color); map.SetTransformMatrix(p,Matrix4x4.TRS(Vector3.zero,Quaternion.Euler(0,0,s.angle),new Vector3(s.flipX ? -1 : 1,s.flipY ? -1 : 1,1))*(tile!=null ? tile.transform : Matrix4x4.identity)); }
            }
            RefreshArt(root);
            PreviewBreakables(root);
            var legacy=Resources.Load<MapVisualEdits>(MapVisualEdits.ResourcePath(ExpeditionMap.Dungeon));
            int count=legacy==null ? 0 : legacy.Edits.Count+(legacy.Additions?.Count??0);
            root.MigrationReport=count==0 ? "Nenhum ajuste anterior a conciliar." : count+" ajustes anteriores preservados em Map_2.asset. Use a lista de migração antes de publicar.";
            root.MigrationReconciled=count==0;
        }
        public static void PreviewBreakables(DungeonMapAuthoring root)
        {
            var active=DungeonBreakables.ActiveCandidates(root.PreviewSeed,DungeonBreakableLayout.Load());
            foreach(var marker in root.GetComponentsInChildren<DungeonAuthoringElement>())
                if(marker.Kind==DungeonElementKind.Point && marker.Id.StartsWith("Breakable/") && int.TryParse(marker.Id.Substring(10),out int id))
                { var render=marker.GetComponent<SpriteRenderer>(); if(render!=null) render.enabled=id>=0 && id<active.Length && active[id]; }
        }
        public static DungeonArtEntry BreakableEntry(DungeonMapAuthoring root,string family,int variant)
        {
            string slot="Breakable_"+family+"_Intact_"+variant;
            var replacement=Array.Find(root.Document.artReplacements,r=>r.slot==slot);
            return root.FindArt(replacement!=null ? replacement.asset : slot);
        }
        static void RefreshBreakableArt(DungeonMapAuthoring root)
        {
            foreach(var marker in root.GetComponentsInChildren<DungeonAuthoringElement>())
            {
                if(marker.Kind!=DungeonElementKind.Point || !marker.Id.StartsWith("Breakable/")) continue;
                var render=marker.GetComponent<SpriteRenderer>(); var entry=BreakableEntry(root,marker.Family,marker.Variant);
                if(render==null || entry?.Sprite==null || render.sprite!=entry.Sprite) continue;
                Vector2 old=marker.ImportedSpriteSize; var scale=marker.transform.lossyScale;
                if(old==Vector2.zero)
                {
                    // One-time migration from the first native scene: its original
                    // cropped PNGs are preserved, so their exact prior bounds remain known.
                    var previous=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Environment/Dungeon/ReworkComplete/Breakable_"+marker.Family+"_Intact_"+marker.Variant+".png");
                    old=previous!=null ? new Vector2(previous.width/128f,previous.height/128f) : (Vector2)entry.Sprite.bounds.size;
                }
                if(Mathf.Abs(Mathf.Abs(scale.x)*old.x-.9f)<.001f && Mathf.Abs(Mathf.Abs(scale.y)*old.y-.9f)<.001f)
                {
                    var local=marker.transform.localScale;
                    marker.transform.localScale=new Vector3(local.x*old.x/entry.Sprite.bounds.size.x,local.y*old.y/entry.Sprite.bounds.size.y,local.z);
                    marker.ImportedSpriteSize=entry.Sprite.bounds.size;
                }
            }
        }
        public static GameObject Place(DungeonMapAuthoring root,DungeonArtEntry entry,Vector2 position,DungeonVisualRecord record=null)
        {
            if(entry?.Sprite==null) throw new InvalidOperationException("Selecione uma peça do catálogo.");
            var go=Child(root.transform,entry.Id,position);
            var marker=go.AddComponent<DungeonAuthoringElement>(); marker.Kind=DungeonElementKind.Visual; marker.AssetId=entry.Id;
            marker.ImportedSpriteSize=entry.Sprite.bounds.size;
            marker.Id=record?.id??Guid.NewGuid().ToString("N"); marker.Room=record?.room??-1; marker.Group=record?.group??entry.Family;
            marker.Hidden=record?.hidden??false;
            var render=go.AddComponent<SpriteRenderer>(); SpriteFactory.Setup(render,entry.Sprite,record?.color??Color.white,record?.order??entry.Order);
            render.enabled=!marker.Hidden;
            var size=record?.size??entry.WorldSize;
            go.transform.localScale=new Vector3(size.x/entry.Sprite.bounds.size.x,size.y/entry.Sprite.bounds.size.y,1);
            go.transform.rotation=Quaternion.Euler(0,0,record?.angle??0); render.flipX=record?.flipX??false; render.flipY=record?.flipY??false;
            if(record?.room==13 || record?.group=="Canal")
            { var canal=root.transform.Find("Functional/Canal — mover conjunto"); if(canal!=null) go.transform.SetParent(canal,true); }
            if(record!=null && !string.IsNullOrEmpty(record.group))
            {
                Transform parent=go.transform.parent;
                if(record.group.StartsWith("Point/"))
                {
                    var point=root.GetComponentsInChildren<DungeonAuthoringElement>().FirstOrDefault(p=>p.Kind==DungeonElementKind.Point && p.Id==record.group.Substring(6));
                    if(point!=null) parent=point.transform;
                }
                foreach(var region in root.GetComponentsInChildren<DungeonAuthoringElement>())
                    if(region.Kind==DungeonElementKind.Region && region.Id.StartsWith("Cover/"))
                    {
                        Vector2 center=region.transform.position;
                        if(new Rect(center-region.Size*.5f,region.Size).Contains(position)) { parent=region.transform.parent; break; }
                    }
                var group=parent.Cast<Transform>().FirstOrDefault(t=>t.name==record.group);
                if(group==null) group=Child(parent,record.group,position).transform;
                go.transform.SetParent(group,true);
            }
            return go;
        }
        public static GameObject PlaceFunctional(DungeonMapAuthoring root,DungeonVisualRecord record)
        {
            var entry=root.FindArt(record.asset);
            var origin=Array.Find(root.Document.points,p=>p.id=="Exit/2").position;
            var end=Array.Find(root.Document.points,p=>p.id=="BoatEnd").position;
            float angle=record.carrier ? Vector2.SignedAngle(Vector2.right,end-origin) : 0;
            var position=record.carrier ? origin+(Vector2)(Quaternion.Euler(0,0,angle)*(record.position-origin)) : record.position;
            var go=Place(root,entry,position,record); var marker=go.GetComponent<DungeonAuthoringElement>();
            marker.Kind=DungeonElementKind.FunctionalVisual; marker.Carrier=record.carrier; marker.Hidden=record.hidden;
            go.transform.rotation=Quaternion.Euler(0,0,record.angle+angle);
            string name=record.room==14 ? "Escadaria — mover conjunto" : record.room==18 ? "Poço — mover conjunto" : "Canal — mover conjunto";
            var parent=root.transform.Find("Functional/"+name); if(parent!=null) go.transform.SetParent(parent,true);
            go.GetComponent<SpriteRenderer>().enabled=!record.hidden && record.group!="CAT-E13_v01" && record.group!="CAT-E12_v02";
            return go;
        }
        public static void RefreshArt(DungeonMapAuthoring root)
        {
            RefreshBreakableArt(root);
            RefreshVisualBounds(root);
            // Apply the same legacy composition repair in the editable scene.
            // Saving captures these changes; publishing still requires review.
            if(root.VotivePresentationRevision<2)
            {
                var elements=root.GetComponentsInChildren<DungeonAuthoringElement>();
                foreach(var element in elements)
                {
                    if(element.Kind!=DungeonElementKind.Visual) continue;
                    var renderer=element.GetComponent<SpriteRenderer>();
                    if(renderer?.sprite==null) continue;
                    var size=Vector2.Scale(renderer.sprite.bounds.size,element.transform.lossyScale);
                    if(renderer.enabled && DungeonVotivePresentation.ObsoleteLedge(element.Id,element.Group,element.AssetId,size))
                    { Undo.RecordObject(renderer,"Corrigir borda duplicada da vigília");renderer.enabled=false;EditorSceneManager.MarkSceneDirty(root.gameObject.scene); }
                    if(renderer.enabled && DungeonVotivePresentation.GeneratedNorthCloth(element.Id,element.Group,element.AssetId) &&
                        elements.Any(o=>o.Id=="R"+element.Room.ToString("00")+"_MourningCloth" && o.AssetId=="HangingMourningCloth" && o.GetComponent<SpriteRenderer>()?.enabled==true))
                    { Undo.RecordObject(renderer,"Remover tecido duplicado da vigília");renderer.enabled=false;EditorSceneManager.MarkSceneDirty(root.gameObject.scene); }
                    if(DungeonVotivePresentation.ProjectedCloth(element.Id,element.Group,element.AssetId) && Quaternion.Angle(element.transform.rotation,Quaternion.identity)>.001f)
                    { Undo.RecordObject(element.transform,"Corrigir projeção do tecido");element.transform.rotation=Quaternion.identity;EditorSceneManager.MarkSceneDirty(root.gameObject.scene); }
                }
                // Later edits and Undo/Redo must not be overwritten on refresh.
                root.VotivePresentationRevision=2;EditorUtility.SetDirty(root);EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            }
            foreach(var source in root.GetComponentsInChildren<DungeonTerrainSource>())
                if(source.VisualSurface)
                {
                    source.GetComponent<Tilemap>().color=source.SurfaceOrder<=-6 ? root.Document.floorTint : Color.white;
                    source.GetComponent<TilemapRenderer>().sharedMaterial=DungeonMapArt.SurfaceMaterial(source.SurfaceFamily)??SpriteFactory.SpriteMaterial;
                }
            var data=DungeonAuthoringCompiler.Capture(root,out var errors);
            if(data==null) return;
            var old=root.transform.Find("DungeonPublishedArt"); if(old!=null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            // Manual objects and surfaces already exist as editable native scene objects.
            data.visuals=Array.Empty<DungeonVisualRecord>(); data.surfaces=Array.Empty<DungeonSurfaceRecord>();
            var preview=DungeonMapArt.Build(root.transform,data,root.Catalog);
            if(preview!=null) foreach(var id in new[]{"CrankPedestal","KeyPedestal"})
            {
                var point=root.GetComponentsInChildren<DungeonAuthoringElement>().FirstOrDefault(e=>e.Kind==DungeonElementKind.Point && e.Id==id);
                string slot=id=="CrankPedestal" ? "CAT-E13_v01" : "CAT-E14_v01";
                var replacement=Array.Find(root.Document.artReplacements,r=>r.slot==slot);
                var entry=root.FindArt(replacement!=null ? replacement.asset : slot);
                if(point==null || entry?.Sprite==null) continue;
                var icon=Child(preview.transform,"Previa_funcional_"+id,point.transform.position);
                icon.transform.localScale=Vector3.one*(.8f/Mathf.Max(entry.Sprite.bounds.size.x,entry.Sprite.bounds.size.y));
                SpriteFactory.Setup(icon.AddComponent<SpriteRenderer>(),entry.Sprite,Color.white,0);
            }
            if(preview!=null) foreach(var node in preview.GetComponentsInChildren<Transform>()) node.gameObject.hideFlags=HideFlags.DontSaveInEditor|HideFlags.NotEditable;
        }
        [Serializable] sealed class OldBounds { public OldBound[] entries; }
        [Serializable] sealed class OldBound { public string asset; public float width,height; }
        static void RefreshVisualBounds(DungeonMapAuthoring root)
        {
            const string migration="Art/Catacumbas/ReworkCompleto/limites-anteriores.json";
            var old=File.Exists(migration) ? JsonUtility.FromJson<OldBounds>(File.ReadAllText(migration)).entries : Array.Empty<OldBound>();
            foreach(var element in root.GetComponentsInChildren<DungeonAuthoringElement>())
            {
                if(element.Kind!=DungeonElementKind.Visual && element.Kind!=DungeonElementKind.FunctionalVisual) continue;
                var render=element.GetComponent<SpriteRenderer>(); var entry=root.FindArt(element.AssetId);
                if(render==null || entry?.Sprite==null || render.sprite!=entry.Sprite) continue;
                Vector2 previous=element.ImportedSpriteSize,current=entry.Sprite.bounds.size;
                if(previous==Vector2.zero)
                {
                    var known=Array.Find(old,b=>b.asset==element.AssetId);
                    previous=known!=null ? new Vector2(known.width,known.height) : current;
                }
                var record=Array.Find(element.Kind==DungeonElementKind.Visual ? root.Document.visuals : root.Document.functionalVisuals,v=>v.id==element.Id);
                var scale=element.transform.lossyScale;
                Vector2 previousWorld=new Vector2(Mathf.Abs(scale.x)*previous.x,Mathf.Abs(scale.y)*previous.y);
                if(record!=null && Vector2.Distance(previousWorld,record.size)<.001f && Vector2.Distance(previous,current)>.0001f)
                {
                    var local=element.transform.localScale;
                    element.transform.localScale=new Vector3(local.x*previous.x/current.x,local.y*previous.y/current.y,local.z);
                }
                element.ImportedSpriteSize=current;
            }
        }
    }
}
