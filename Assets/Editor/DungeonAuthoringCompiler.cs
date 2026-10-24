using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CyberExtraction.Match;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CyberExtraction.EditorTools
{
    public static class DungeonAuthoringCompiler
    {
        public static DungeonMapDocument Capture(DungeonMapAuthoring root,out List<string> errors)
        {
            errors=new List<string>();
            if(root==null || root.Catalog==null) { errors.Add("Raiz/catálogo de autoria ausente."); return null; }
            if(root.transform.position!=Vector3.zero || root.transform.rotation!=Quaternion.identity || root.transform.localScale!=Vector3.one)
            { errors.Add("A raiz do mapa precisa manter origem, rotação e escala padrão."); return null; }
            if(root.MigratedCatalog!=null) DungeonCatalogEditing.Rehash(root.MigratedCatalog);
            var data=JsonUtility.FromJson<DungeonMapDocument>(JsonUtility.ToJson(root.Document));
            if(root.FunctionalIdentity==null || root.FunctionalIdentity.Length==0)
            {
                root.FunctionalIdentity=(root.Document.functionalVisuals??Array.Empty<DungeonVisualRecord>()).Select(v=>v.id).Where(id=>!string.IsNullOrEmpty(id)).ToArray();
                EditorUtility.SetDirty(root);
            }
            data.requiredFunctionalKeys=root.FunctionalIdentity;
            data.artHash=DungeonArtCatalog.CombinedHash(root.Catalog,root.MigratedCatalog);
            data.artResource=null;
            data.cells=new int[16384]; data.contentHash=null;
            var tileEntries=root.ArtEntries.Where(e=>e.Tile!=null).ToDictionary(e=>(TileBase)e.Tile,e=>e);
            var surfaces=new List<DungeonSurfaceRecord>();
            var sceneRoots=root.gameObject.scene.GetRootGameObjects();
            foreach(var collider in sceneRoots.SelectMany(o=>o.GetComponentsInChildren<Collider2D>()))
                if((collider.gameObject.hideFlags&HideFlags.DontSaveInEditor)==0 && !collider.CompareTag("EditorOnly"))
                    errors.Add("Colisão fora da grade de autoria: "+collider.name+". Represente a pegada na camada física antes de publicar.");
            foreach(var source in sceneRoots.SelectMany(o=>o.GetComponentsInChildren<DungeonTerrainSource>()).OrderBy(s=>s.Priority))
            {
                if(!Cardinal(source.transform)) { errors.Add("Terreno fora de rotação ortogonal: "+source.name); continue; }
                var map=source.GetComponent<Tilemap>();
                var scale=source.transform.lossyScale;
                if(Mathf.Abs(Mathf.Abs(scale.x)-1.06f)>.001f || Mathf.Abs(Mathf.Abs(scale.y)-1.06f)>.001f)
                { errors.Add("Mantenha a escala física de 1,06; pinte a pegada em vez de esticar o terreno: "+source.name); continue; }
                foreach(var p in map.cellBounds.allPositionsWithin)
                {
                    var tile=map.GetTile(p); if(tile==null) continue;
                    var position=map.GetCellCenterWorld(p);
                    float fx=position.x/1.06f+64-.5f, fy=position.y/1.06f+64-.5f;
                    int x=Mathf.RoundToInt(fx),y=Mathf.RoundToInt(fy);
                    if(Mathf.Abs(fx-x)>.001f || Mathf.Abs(fy-y)>.001f || x<0 || y<0 || x>=128 || y>=128)
                    { errors.Add("Célula física desalinhada/fora do mapa: "+source.name); break; }
                    if(source.VisualSurface)
                    {
                        if(!tileEntries.TryGetValue(tile,out var art)) { errors.Add("Tile visual fora do catálogo: "+tile.name); continue; }
                        var matrix=map.transform.localToWorldMatrix*map.orientationMatrix*map.GetTransformMatrix(p)*art.Tile.transform.inverse;
                        Vector3 axis=matrix.GetColumn(0),up=matrix.GetColumn(1); float sign=Vector3.Cross(axis,up).z<0 ? -1 : 1;
                        if(Mathf.Abs(axis.magnitude-1.06f)>.001f || Mathf.Abs(up.magnitude-1.06f)>.001f) errors.Add("Superfície esticada: "+source.name);
                        surfaces.Add(new DungeonSurfaceRecord {x=x,y=y,asset=art.Id,order=source.SurfaceOrder,color=(Color)(Color32)map.GetColor(p),
                            angle=CleanAngle(Mathf.Atan2(axis.y*sign,axis.x*sign)*Mathf.Rad2Deg),flipX=sign<0});
                    }
                    else
                    {
                        int kind=tile==root.StoneTile ? 0 : tile==root.FloorTile ? 1 : tile==root.WaterTile ? 2 : tile==root.CoverTile ? 3 : -1;
                        if(kind<0) { errors.Add("Tile físico desconhecido: "+tile.name); continue; }
                        data.cells[y*128+x]=kind;
                    }
                }
            }
            var points=new List<DungeonPointRecord>(); var regions=new List<DungeonRectRecord>();var regionIds=new HashSet<string>();
            var rooms=new List<DungeonRoomRecord>(); var visuals=new List<DungeonVisualRecord>(); var functional=new List<DungeonVisualRecord>(); var ids=new HashSet<string>();
            var elements=sceneRoots.SelectMany(o=>o.GetComponentsInChildren<DungeonAuthoringElement>()).ToArray();
            var boatOrigin=elements.FirstOrDefault(e=>e.Kind==DungeonElementKind.Point && e.Id=="Exit/2");
            var boatEnd=elements.FirstOrDefault(e=>e.Kind==DungeonElementKind.Point && e.Id=="BoatEnd");
            float boatAngle=boatOrigin!=null && boatEnd!=null ? Vector2.SignedAngle(Vector2.right,boatEnd.transform.position-boatOrigin.transform.position) : -90;
            foreach(var element in elements)
            {
                if(element.Kind!=DungeonElementKind.Visual && string.IsNullOrWhiteSpace(element.Id))
                { errors.Add("Elemento funcional/físico sem identidade: "+element.name);continue; }
                if(element.Kind==DungeonElementKind.Visual || element.Kind==DungeonElementKind.FunctionalVisual)
                {
                    var renderer=element.GetComponent<SpriteRenderer>(); var art=root.FindArt(element.AssetId);
                    if(renderer==null || art?.Sprite==null) { errors.Add("Peça sem sprite persistente: "+element.name); continue; }
                    if(renderer.sprite!=art.Sprite) { errors.Add("Registre a arte substituta no catálogo antes de publicar: "+element.name); continue; }
                    if(renderer.sharedMaterial!=CyberExtraction.Core.SpriteFactory.SpriteMaterial)
                    { errors.Add("Material individual não representado na definição: "+element.name+". Edite o material compartilhado de sprites e a cor da peça.");continue; }
                    if(string.IsNullOrEmpty(element.Id) || !ids.Add(element.Id))
                    {
                        if(element.Kind==DungeonElementKind.FunctionalVisual) { errors.Add("Identidade de peça funcional repetida: "+element.Id); continue; }
                        // Ctrl+D duplicates component data; give the new visual an independent stable ID.
                        Undo.RecordObject(element,"Identidade da cópia"); element.Id=Guid.NewGuid().ToString("N"); ids.Add(element.Id); EditorUtility.SetDirty(element);
                    }
                    var scale=element.transform.lossyScale;
                    var visual=new DungeonVisualRecord {id=element.Id,asset=element.AssetId,room=element.Room,group=element.Group,
                        position=element.transform.position,size=new Vector2(Mathf.Abs(scale.x)*renderer.sprite.bounds.size.x,Mathf.Abs(scale.y)*renderer.sprite.bounds.size.y),
                        angle=element.transform.eulerAngles.z,flipX=renderer.flipX^(scale.x<0),flipY=renderer.flipY^(scale.y<0),order=renderer.sortingOrder,color=(Color)(Color32)renderer.color,
                        hidden=element.Kind==DungeonElementKind.FunctionalVisual ? element.Hidden : element.Hidden || !renderer.enabled,carrier=element.Carrier};
                    if(element.Kind==DungeonElementKind.FunctionalVisual)
                    {
                        if(element.Carrier && boatOrigin!=null)
                        { visual.position=(Vector2)boatOrigin.transform.position+(Vector2)(Quaternion.Euler(0,0,-boatAngle)*(visual.position-(Vector2)boatOrigin.transform.position)); visual.angle-=boatAngle; }
                        visual.angle=CleanAngle(visual.angle); visual.position=Clean(visual.position); visual.size=Clean(visual.size); functional.Add(visual);
                    }
                    else { visual.angle=CleanAngle(visual.angle); visual.position=Clean(visual.position); visual.size=Clean(visual.size); visuals.Add(visual); }
                    continue;
                }
                if(element.Kind==DungeonElementKind.Point)
                {
                    points.Add(new DungeonPointRecord {id=element.Id,position=Clean(element.transform.position),family=element.Family,variant=element.Variant,angle=CleanAngle(element.transform.eulerAngles.z)});
                    if(element.Id.StartsWith("Breakable/"))
                    {
                        var renderer=element.GetComponent<SpriteRenderer>();
                        if(renderer?.sprite!=null)
                        {
                            if(renderer.sprite!=DungeonAuthoringScene.BreakableEntry(root,element.Family,element.Variant)?.Sprite)
                                errors.Add("Use o banco de estados funcionais para trocar a arte do quebrável: "+element.Id);
                            var size=Vector2.Scale(renderer.sprite.bounds.size,element.transform.lossyScale);
                            if(Mathf.Abs(Mathf.Abs(size.x)-.9f)>.001f || Mathf.Abs(Mathf.Abs(size.y)-.9f)>.001f)
                                errors.Add("Preservar a pegada de 0,90 do quebrável: "+element.Id);
                        }
                    }
                    continue;
                }
                if(!Cardinal(element.transform)) { errors.Add("Região/sala fora de rotação ortogonal: "+element.Id); continue; }
                var b=Bounds(element);
                if(element.Kind==DungeonElementKind.Room)
                {
                    float x=b.x/1.06f+64,y=b.y/1.06f+64,w=b.width/1.06f,h=b.height/1.06f;
                    if(!Integer(x) || !Integer(y) || !Integer(w) || !Integer(h)) errors.Add("Limites da sala desalinhados: "+element.Room);
                    rooms.Add(new DungeonRoomRecord {id=element.Room,sector=element.Sector,name=element.RoomName,x=Mathf.RoundToInt(x),y=Mathf.RoundToInt(y),width=Mathf.RoundToInt(w),height=Mathf.RoundToInt(h)});
                }
                else
                {
                    if(!regionIds.Add(element.Id) && (element.Id.StartsWith("Cover/") || element.Id.StartsWith("Pillar/")))
                    {
                        Undo.RecordObject(element,"Identidade da cobertura duplicada");
                        element.Id=element.Id.Substring(0,element.Id.IndexOf('/')+1)+Guid.NewGuid().ToString("N");regionIds.Add(element.Id);EditorUtility.SetDirty(element);
                    }
                    if(element.CellCoordinates)
                    {
                        b=new Rect(b.x/1.06f+64,b.y/1.06f+64,b.width/1.06f,b.height/1.06f);
                        if(!Integer(b.x) || !Integer(b.y) || !Integer(b.width) || !Integer(b.height)) errors.Add("Região desalinhada: "+element.Id);
                    }
                    regions.Add(new DungeonRectRecord {id=element.Id,x=b.x,y=b.y,width=b.width,height=b.height});
                }
            }
            data.points=points.OrderBy(p=>p.id,StringComparer.Ordinal).ToArray(); data.regions=regions.OrderBy(r=>r.id,StringComparer.Ordinal).ToArray();
            data.rooms=rooms.OrderBy(r=>r.id).ToArray(); data.visuals=visuals.OrderBy(v=>v.id,StringComparer.Ordinal).ToArray();
            data.functionalVisuals=functional.OrderBy(v=>v.id,StringComparer.Ordinal).ToArray();
            data.surfaces=surfaces.OrderBy(s=>s.order).ThenBy(s=>s.y).ThenBy(s=>s.x).ToArray();
            data.legacyMigration=root.MigrationReconciled ? "reconciled" : "pending";
            if(data.sectors==null || data.sectors.Length!=16384) data.sectors=new int[16384];
            foreach(var r in data.rooms)
                for(int y=Mathf.Max(0,r.y);y<Mathf.Min(128,r.y+r.height);y++) for(int x=Mathf.Max(0,r.x);x<Mathf.Min(128,r.x+r.width);x++) data.sectors[y*128+x]=r.sector;
            return errors.Count==0 ? data : null;
        }
        static bool Cardinal(Transform t)=>Mathf.Abs(Mathf.DeltaAngle(t.eulerAngles.z,Mathf.Round(t.eulerAngles.z/90)*90))<.001f && Mathf.Abs(t.eulerAngles.x)<.001f && Mathf.Abs(t.eulerAngles.y)<.001f;
        static Vector2 Clean(Vector2 v)=>new Vector2(Mathf.Round(v.x*10000)/10000,Mathf.Round(v.y*10000)/10000);
        static float CleanAngle(float angle) { float clean=Mathf.Round(Mathf.Repeat(angle,360)*10000)/10000; return clean>=359.9999f ? 0 : clean; }
        static bool Integer(float n)=>Mathf.Abs(n-Mathf.Round(n))<.001f;
        static Rect Bounds(DungeonAuthoringElement e)
        {
            Vector2 half=e.Size*.5f; var min=new Vector2(float.PositiveInfinity,float.PositiveInfinity); var max=-min;
            foreach(var p in new[]{new Vector2(-half.x,-half.y),new Vector2(half.x,-half.y),new Vector2(half.x,half.y),new Vector2(-half.x,half.y)})
            { Vector2 world=e.transform.TransformPoint(p); min=Vector2.Min(min,world); max=Vector2.Max(max,world); }
            return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }
        public static bool Save(DungeonMapAuthoring root,bool publish,out List<string> errors)
        {
            var data=Capture(root,out errors);
            if(data==null)
            {
                if(publish || root==null) return false;
                // The native scene is the draft source, including incomplete work.
                // An invalid compilation must never prevent preserving that work.
                EditorUtility.SetDirty(root); EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
                return EditorSceneManager.SaveScene(root.gameObject.scene);
            }
            if(publish)
            {
                var legacy=Resources.Load<MapVisualEdits>(MapVisualEdits.ResourcePath(ExpeditionMap.Dungeon));
                if(legacy!=null && (legacy.Edits.Count+(legacy.Additions?.Count??0))>0 &&
                    root.LegacySourceHash!=DungeonCatalogEditing.SourceFingerprint(AssetDatabase.GetAssetPath(legacy)))
                    errors.Add("Os ajustes anteriores mudaram ou não foram conciliados nesta autoria.");
                errors.AddRange(data.Problems(true));
                foreach(var v in data.visuals.Concat(data.functionalVisuals))
                    if(root.FindArt(v.asset)?.Sprite==null) errors.Add("Sprite ausente: "+v.asset);
                foreach(var s in data.surfaces)
                {
                    var entry=root.FindArt(s.asset);
                    if(entry?.Tile==null) errors.Add("Tile ausente: "+s.asset);
                    else if(entry.Structural && data.cells[s.y*128+s.x]!=0) errors.Add("Parede visual sem pegada de pedra: "+s.x+","+s.y);
                }
                foreach(var r in data.artReplacements)
                    if(root.FindArt(r.slot)?.Sprite==null || root.FindArt(r.asset)?.Sprite==null) errors.Add("Estado de extração ausente: "+r.slot);
                foreach(var f in data.fidelity)
                    if(!File.Exists(f.concept) || !File.Exists(f.comparison)) errors.Add("Evidência visual ausente: R"+f.room.ToString("00"));
                if(data.functionalVisuals.Length==0) errors.Add("Materialize os conjuntos de extração antes de publicar.");
                foreach(var id in Enumerable.Range(0,36).Select(i=>"Floor_Limestone_"+i.ToString("00")).Concat(new[]{"LowCoverStone"}))
                    if(root.Catalog.Find(id)==null) errors.Add("Kit incompleto: "+id);
                foreach(var state in new[]{"Dry","Damp","Cracked","Roots"})
                    for(int mask=0;mask<256;mask++) if(DungeonWallCatalog.Normalize(mask)==mask && root.Catalog.Find("Wall_"+state+"_"+mask.ToString("X2"))==null)
                        errors.Add("Encaixe ausente: "+state+"/"+mask);
                CheckRoutes(data,errors);
                if(errors.Count>0) return false;
                try { data.artResource=DungeonArtPublication.Export(root,data.artHash); }
                catch(Exception e) { errors.Add(e.Message);return false; }
            }
            Undo.RecordObject(root,"Salvar autoria das Catacumbas"); root.Document=data; EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            if(!EditorSceneManager.SaveScene(root.gameObject.scene)) { errors.Add("Cena não salva."); return false; }
            if(publish)
            {
                data.contentHash=data.Fingerprint();
                string full=Path.GetFullPath(root.PublishedPath),expected=Path.GetFullPath("Assets/Resources/Environment/Dungeon/ReworkComplete/Published.json");
                if(full!=expected) { errors.Add("Destino de publicação fora do caminho do mapa."); return false; }
                Directory.CreateDirectory(Path.GetDirectoryName(full));
                string temporary=full+".pending"; File.WriteAllText(temporary,JsonUtility.ToJson(data,true));
                if(File.Exists(full)) File.Replace(temporary,full,full+".previous"); else File.Move(temporary,full);
                AssetDatabase.ImportAsset(root.PublishedPath);
                DungeonMapPublication.Invalidate(); DungeonLayout.EnsurePublishedContent();
            }
            return true;
        }
        static void CheckRoutes(DungeonMapDocument data,List<string> errors)
        {
            if(data.cells==null || data.cells.Length!=16384 || data.points==null) return;
            var start=data.points.FirstOrDefault(p=>p.id=="Spawn/0"); if(start==null) return;
            int Index(Vector2 p) { int x=Mathf.FloorToInt(p.x/1.06f+64),y=Mathf.FloorToInt(p.y/1.06f+64); return x<0 || y<0 || x>=128 || y>=128 ? -1 : y*128+x; }
            int first=Index(start.position); if(first<0 || data.cells[first]!=1) return;
            var reached=new bool[16384]; var queue=new Queue<int>(); queue.Enqueue(first); reached[first]=true;
            while(queue.Count>0)
            {
                int i=queue.Dequeue(),x=i%128,y=i/128;
                foreach(var n in new[]{x>0 ? i-1 : -1,x<127 ? i+1 : -1,y>0 ? i-128 : -1,y<127 ? i+128 : -1})
                    if(n>=0 && !reached[n] && data.cells[n]==1) { reached[n]=true; queue.Enqueue(n); }
            }
            foreach(var p in data.points.Where(p=>p.id.StartsWith("Spawn/") || p.id.StartsWith("Exit/") || p.id.StartsWith("Mechanism/")))
            { int i=Index(p.position); if(i<0 || !reached[i]) errors.Add("Ponto sem acesso pelo terreno fechado: "+p.id); }
        }
    }
}
