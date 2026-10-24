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
    public sealed class DungeonAuthoringWindow : EditorWindow
    {
        DungeonMapAuthoring root;
        DungeonArtEntry selected;
        string search="",status="";
        int tab,room=17,page,stateSlot;
        Vector2 scroll;
        DungeonCellKind brush=DungeonCellKind.Floor;
        DungeonTerrainSource target;
        bool painting,stroke,queued,paintFlipX,paintFlipY,eraseArt,autoTransitions=true;
        int paintQuarterTurns;
        Texture2D concept;
        string conceptPath;
        Sprite customSprite;
        string customLabel="Peça autorada";
        Vector2 customSize=Vector2.one*1.06f;
        bool customTile;
        readonly List<string> errors=new List<string>();
        [MenuItem("Cyber Extraction/Mapas/Catacumbas — autoria fora do Play", false, 10)]
        public static void Open()=>GetWindow<DungeonAuthoringWindow>("Catacumbas · Autoria");
        void OnEnable() { SceneView.duringSceneGui+=SceneGUI; Undo.undoRedoPerformed+=QueueRefresh; }
        void OnDisable()
        {
            SceneView.duringSceneGui-=SceneGUI; Undo.undoRedoPerformed-=QueueRefresh;
            if(concept!=null) DestroyImmediate(concept);
        }
        void OnGUI()
        {
            if(Application.isPlaying) { EditorGUILayout.HelpBox("Encerre o Play para editar a cena de autoria.",MessageType.Info); return; }
            if(root==null) root=FindFirstObjectByType<DungeonMapAuthoring>();
            if(GUILayout.Button("Abrir / criar cena de autoria"))
            {
                try { root=DungeonAuthoringScene.Open(); if(root!=null) FrameRoom(); }
                catch(Exception e) { status=e.Message; Debug.LogException(e); }
            }
            if(root==null) { EditorGUILayout.HelpBox("A cena de autoria é separada da partida. Todas as alterações são feitas fora do Play.",MessageType.Info); return; }
            if(root.Document?.cells==null || root.Document.cells.Length!=16384) { EditorGUILayout.HelpBox("Use Abrir / criar para preparar esta cena com o catálogo e o terreno.",MessageType.Info); return; }
            using(new EditorGUILayout.HorizontalScope())
            {
                if(GUILayout.Button("Salvar rascunho")) Save(false);
                if(GUILayout.Button("Publicar mapa")) Save(true);
                if(GUILayout.Button("Atualizar montagem")) DungeonAuthoringScene.RefreshArt(root);
            }
            room=EditorGUILayout.IntSlider("Sala",room,0,23);
            if(GUILayout.Button("Enquadrar sala / ver conceito")) FrameRoom();
            tab=GUILayout.Toolbar(tab,new[]{"Assets","Terreno físico","Fidelidade","Migração"});
            scroll=EditorGUILayout.BeginScrollView(scroll);
            if(tab==0) Assets(); else if(tab==1) Physics(); else if(tab==2) Fidelity(); else Migration();
            if(!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status,MessageType.Info);
            foreach(var error in errors.Take(30)) EditorGUILayout.HelpBox(error,MessageType.Error);
            if(errors.Count>30) EditorGUILayout.LabelField("Mais "+(errors.Count-30)+" pendências. Consulte o Console.");
            EditorGUILayout.EndScrollView();
        }
        void Assets()
        {
            EditorGUI.BeginChangeCheck(); var floorTint=EditorGUILayout.ColorField("Tonalidade do piso",root.Document.floorTint);
            if(EditorGUI.EndChangeCheck()) { Undo.RecordObject(root,"Tonalidade do piso"); root.Document.floorTint=floorTint; EditorUtility.SetDirty(root); EditorSceneManager.MarkSceneDirty(root.gameObject.scene); DungeonAuthoringScene.RefreshArt(root); }
            search=EditorGUILayout.TextField("Buscar",search);
            if(root.Catalog==null) return;
            var entries=root.ArtEntries.Where(e=>string.IsNullOrWhiteSpace(search) || (e.Id+" "+e.DisplayName+" "+e.Family+" "+e.Sector+" "+e.State).IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0).ToArray();
            int pages=Mathf.Max(1,Mathf.CeilToInt(entries.Length/36f)); page=Mathf.Clamp(page,0,pages-1);
            using(new EditorGUILayout.HorizontalScope())
            { if(GUILayout.Button("◀",GUILayout.Width(35))) page=Mathf.Max(0,page-1); GUILayout.Label((page+1)+" / "+pages+" · "+entries.Length+" assets"); if(GUILayout.Button("▶",GUILayout.Width(35))) page=Mathf.Min(pages-1,page+1); }
            for(int row=0;row<9;row++)
                using(new EditorGUILayout.HorizontalScope())
                    for(int col=0;col<4;col++)
                    {
                        int i=page*36+row*4+col; if(i>=entries.Length) break;
                        var entry=entries[i];
                        using(new EditorGUILayout.VerticalScope(GUILayout.Width(85)))
                        {
                            var preview=AssetPreview.GetAssetPreview(entry.Sprite)??AssetPreview.GetMiniThumbnail(entry.Sprite);
                            if(GUILayout.Button(new GUIContent(preview,entry.Id+" · "+entry.WorldSize),GUILayout.Width(80),GUILayout.Height(65)))
                            { selected=entry; painting=true; status=entry.Id+": clique na Scene View para colocar. Escape encerra."; }
                            GUILayout.Label(entry.DisplayName??entry.Id,EditorStyles.wordWrappedMiniLabel,GUILayout.Width(82));
                        }
                    }
            if(selected!=null)
            {
                GUILayout.Label("Selecionado: "+selected.Id);
                if(selected.Tile!=null)
                {
                    eraseArt=EditorGUILayout.Toggle("Apagar arte pintada",eraseArt);
                    autoTransitions=EditorGUILayout.Toggle("Conectar família de transição",autoTransitions);
                    paintQuarterTurns=EditorGUILayout.IntSlider("Rotação do tile (90°)",paintQuarterTurns,0,3);
                    paintFlipX=EditorGUILayout.Toggle("Espelhar horizontal",paintFlipX);
                    paintFlipY=EditorGUILayout.Toggle("Espelhar vertical",paintFlipY);
                }
                if(GUILayout.Button("Colocar no centro da sala")) Place(RoomCenter());
                if(GUILayout.Button("Usar na seleção visual")) ReplaceSelection();
                var states=root.ArtEntries.Where(e=>e.Family=="ExtractionStates" || e.Family=="BreakableStates").ToArray();
                if(states.Length>0)
                {
                    stateSlot=EditorGUILayout.Popup("Estado funcional",Mathf.Clamp(stateSlot,0,states.Length-1),states.Select(s=>s.Id).ToArray());
                    if(GUILayout.Button("Substituir este estado pela arte selecionada")) SetState(states[stateSlot].Id,selected.Id);
                    if(GUILayout.Button("Restaurar arte deste estado")) SetState(states[stateSlot].Id,null);
                }
            }
            if(GUILayout.Button("Disponibilizar prefabs individuais no Project")) ExportPrefabs();
            EditorGUILayout.LabelField("Conjuntos desmontáveis",EditorStyles.boldLabel);
            foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{root.PrefabFolder}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid),name=Path.GetFileNameWithoutExtension(path);
                if(!name.StartsWith("Conjunto_") || (!string.IsNullOrWhiteSpace(search) && name.IndexOf(search,StringComparison.OrdinalIgnoreCase)<0)) continue;
                if(GUILayout.Button("Colocar "+name))
                {
                    var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),root.gameObject.scene);
                    instance.transform.SetParent(root.transform,true); instance.transform.position=RoomCenter();
                    foreach(var marker in instance.GetComponentsInChildren<DungeonAuthoringElement>()) { marker.Room=room; marker.Id=Guid.NewGuid().ToString("N"); }
                    Undo.RegisterCreatedObjectUndo(instance,"Colocar conjunto"); Selection.activeGameObject=instance;
                    EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
                }
            }
            EditorGUILayout.HelpBox("Os sprites e tiles persistentes também estão dentro de CatacombArt.catacombkit. Os objetos podem ser editados pelas ferramentas normais da Scene View e Inspector.",MessageType.Info);
            EditorGUILayout.LabelField("Adicionar arte do Project",EditorStyles.boldLabel);
            customSprite=(Sprite)EditorGUILayout.ObjectField("Sprite",customSprite,typeof(Sprite),false);
            customLabel=EditorGUILayout.TextField("Nome",customLabel); customSize=EditorGUILayout.Vector2Field("Dimensões (unidades)",customSize);
            customTile=EditorGUILayout.Toggle("Disponibilizar como tile",customTile);
            if(GUILayout.Button("Registrar na paleta"))
            {
                try { DungeonCatalogEditing.Add(root,customSprite,customLabel,customSize,customTile); status="Arte registrada; todas as variantes anteriores foram preservadas."; }
                catch(Exception e) { status=e.Message; }
            }
        }
        void Physics()
        {
            EditorGUILayout.HelpBox("Esta aba altera a geometria real. Piso, pedra, água e cobertura baixa são tipos distintos. Mova os pais ‘mover conjunto’ para manter peças vinculadas.",MessageType.Info);
            brush=(DungeonCellKind)EditorGUILayout.EnumPopup("Tipo físico",brush);
            EditorGUI.BeginChangeCheck(); int previewSeed=EditorGUILayout.IntField("Semente da prévia",root.PreviewSeed);
            if(EditorGUI.EndChangeCheck()) { Undo.RecordObject(root,"Semente da prévia"); root.PreviewSeed=previewSeed; DungeonAuthoringScene.PreviewBreakables(root); }
            target=(DungeonTerrainSource)EditorGUILayout.ObjectField("Camada física",target,typeof(DungeonTerrainSource),true);
            if(target==null || target.VisualSurface) target=root.GetComponentsInChildren<DungeonTerrainSource>().FirstOrDefault(s=>!s.VisualSurface && s.Priority==0);
            painting=GUILayout.Toggle(painting,"Pintar na Scene View","Button");
            if(GUILayout.Button("Mostrar / ocultar máscara física"))
                foreach(var source in root.GetComponentsInChildren<DungeonTerrainSource>().Where(s=>!s.VisualSurface))
                { var render=source.GetComponent<TilemapRenderer>(); render.enabled=!render.enabled; render.sortingOrder=30; }
            if(GUILayout.Button("Selecionar conjunto da extração desta sala"))
            {
                string name=room==14 ? "Escadaria — mover conjunto" : room==18 ? "Poço — mover conjunto" : "Canal — mover conjunto";
                var group=root.transform.Find("Functional/"+name); if(group!=null) Selection.activeGameObject=group.gameObject;
            }
        }
        void Fidelity()
        {
            var record=root.Document.fidelity.FirstOrDefault(f=>f.room==room);
            if(record==null) { EditorGUILayout.HelpBox("Ficha de sala ausente.",MessageType.Error); return; }
            EditorGUI.BeginChangeCheck();
            string comparison=EditorGUILayout.TextField("Comparação (arquivo)",record.comparison);
            EditorGUILayout.LabelField("Desvios pendentes"); string deviations=EditorGUILayout.TextArea(record.pendingDeviations??"",GUILayout.MinHeight(65));
            EditorGUILayout.LabelField("Causa e replanejamento"); string replan=EditorGUILayout.TextArea(record.replan??"",GUILayout.MinHeight(65));
            if(EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(root,"Replanejar fidelidade"); record.comparison=comparison; record.pendingDeviations=deviations; record.replan=replan;
                record.resolved=false; EditorUtility.SetDirty(root); EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            }
            LoadConcept(record.concept);
            if(concept!=null) { float w=Mathf.Min(position.width-35,550); GUILayout.Label(concept,GUILayout.Width(w),GUILayout.Height(w*concept.height/concept.width)); }
            if(GUILayout.Button("Abrir conceito em tamanho original") && File.Exists(record.concept)) EditorUtility.OpenWithDefaultApp(Path.GetFullPath(record.concept));
            if(GUILayout.Button("Registrar comparação resolvida"))
            {
                var data=DungeonAuthoringCompiler.Capture(root,out var problems);
                if(data==null || !string.IsNullOrWhiteSpace(record.pendingDeviations) || string.IsNullOrWhiteSpace(record.comparison) || !File.Exists(record.comparison))
                { status="Registre uma comparação existente e resolva os desvios antes de encerrar a sala."; return; }
                Undo.RecordObject(root,"Registrar comparação da sala"); record.comparisonHash=data.RoomFingerprint(room); record.resolved=true;
                EditorUtility.SetDirty(root); EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            }
            var snapshot=DungeonAuthoringCompiler.Capture(root,out var captureProblems);
            bool current=record.resolved && snapshot!=null && record.comparisonHash==snapshot.RoomFingerprint(room);
            EditorGUILayout.HelpBox(current ? "Comparação registrada para esta montagem." : "Etapa aberta: comparar, replanejar e ajustar até se aproximar do conceito.",current ? MessageType.Info : MessageType.Warning);
        }
        void Migration()
        {
            EditorGUILayout.HelpBox(root.MigrationReport??"Ajustes anteriores preservados.",MessageType.Info);
            var legacy=Resources.Load<MapVisualEdits>(MapVisualEdits.ResourcePath(ExpeditionMap.Dungeon));
            if(GUILayout.Button("Preservar ajustes anteriores como referências editáveis"))
            {
                try { DungeonLegacyMigration.Archive(root); status="Ajustes preservados na coleção de referência, sem alterar o arquivo original."; }
                catch(Exception e) { status=e.Message; Debug.LogException(e); }
            }
            if(GUILayout.Button("Copiar referência selecionada para a montagem"))
            {
                var reference=Selection.activeGameObject!=null ? Selection.activeGameObject.GetComponent<DungeonLegacySource>() : null;
                if(reference!=null)
                {
                    var copy=Instantiate(reference.gameObject,root.transform); copy.SetActive(true);
                    var marker=copy.GetComponent<DungeonAuthoringElement>(); if(marker!=null) marker.Id=Guid.NewGuid().ToString("N");
                    Undo.RegisterCreatedObjectUndo(copy,"Usar referência anterior"); Selection.activeGameObject=copy;
                }
            }
            if(legacy!=null)
            {
                if(GUILayout.Button("Selecionar asset original preservado")) Selection.activeObject=legacy;
                foreach(var edit in legacy.Edits)
                {
                    EditorGUILayout.LabelField(edit.Path,EditorStyles.wordWrappedMiniLabel);
                    EditorGUILayout.LabelField("Célula "+edit.Position+" · deslocamento "+edit.Offset);
                }
                EditorGUILayout.LabelField("Cópias anteriores: "+(legacy.Additions?.Count??0));
            }
            EditorGUI.BeginChangeCheck();
            string report=EditorGUILayout.TextArea(root.MigrationReport??"",GUILayout.MinHeight(85));
            bool reconciled=EditorGUILayout.Toggle("Ajustes conciliados",root.MigrationReconciled);
            if(EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(root,"Conciliar ajustes antigos"); root.MigrationReport=report; root.MigrationReconciled=reconciled;
                EditorUtility.SetDirty(root); EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            }
        }
        void LoadConcept(string path)
        {
            if(conceptPath==path) return;
            if(concept!=null) DestroyImmediate(concept); concept=null; conceptPath=path;
            if(!string.IsNullOrEmpty(path) && File.Exists(path)) { concept=new Texture2D(2,2); concept.LoadImage(File.ReadAllBytes(path)); }
        }
        void Save(bool publish)
        {
            errors.Clear(); bool success=DungeonAuthoringCompiler.Save(root,publish,out var problems); errors.AddRange(problems);
            status=success ? publish ? "Mapa publicado. A próxima sessão usa este conteúdo." : problems.Count>0 ? "Rascunho nativo salvo com pendências; a publicação anterior foi preservada." : "Rascunho salvo; publicação anterior preservada." : "Há pendências; a publicação anterior foi preservada.";
            if(problems.Count>0) Debug.LogWarning(string.Join("\n",problems));
        }
        Vector2 RoomCenter()
        { var r=root.Document.rooms.First(r=>r.id==room); return new Vector2((r.x+r.width*.5f-64)*1.06f,(r.y+r.height*.5f-64)*1.06f); }
        void FrameRoom()
        {
            if(root==null) return;
            var r=root.Document.rooms.First(r=>r.id==room);
            var view=SceneView.lastActiveSceneView; if(view!=null) { view.in2DMode=true; view.Frame(new Bounds(RoomCenter(),new Vector3((r.width+3)*1.06f,(r.height+3)*1.06f,1)),false); }
        }
        void Place(Vector2 position)
        {
            if(selected==null) return;
            if(selected.Family=="BreakableStates") { ReplaceSelection(); return; }
            if(selected.Tile!=null)
            {
                int order=selected.Family=="Walls" ? -4 : -6;
                string family=DungeonMapArt.SurfaceFamily(selected.Id);
                var sources=root.GetComponentsInChildren<DungeonTerrainSource>().Where(s=>s.VisualSurface && s.SurfaceOrder==order).ToArray();
                var source=sources.FirstOrDefault(s=>s.SurfaceFamily==family);
                if(source==null) source=DungeonAuthoringScene.CreateSurface(root,order,family);
                var map=source.GetComponent<Tilemap>();
                foreach(var other in sources.Where(s=>s!=source))
                {
                    var otherMap=other.GetComponent<Tilemap>();var otherCell=otherMap.WorldToCell(position);
                    if(otherMap.GetTile(otherCell)==null) continue;
                    Undo.RegisterCompleteObjectUndo(otherMap,"Substituir família do piso"); otherMap.SetTile(otherCell,null);
                }
                Undo.RegisterCompleteObjectUndo(map,"Pintar arte"); map.SetTile(map.WorldToCell(position),eraseArt ? null : selected.Tile);
                var cell=map.WorldToCell(position); map.SetTileFlags(cell,TileFlags.None);
                map.SetTransformMatrix(cell,Matrix4x4.TRS(Vector3.zero,Quaternion.Euler(0,0,paintQuarterTurns*90),new Vector3(paintFlipX ? -1 : 1,paintFlipY ? -1 : 1,1))*selected.Tile.transform);
                if(selected.Family=="Walls")
                {
                    var physical=root.GetComponentsInChildren<DungeonTerrainSource>().First(s=>!s.VisualSurface && s.Priority==0).GetComponent<Tilemap>();
                    Undo.RegisterCompleteObjectUndo(physical,"Pegada da parede"); physical.SetTile(physical.WorldToCell(position),eraseArt ? root.FloorTile : root.StoneTile);
                }
                if(autoTransitions && selected.Id.StartsWith("Transition_"))
                {
                    string prefix=selected.Id.Substring(0,selected.Id.LastIndexOf('_')+1);
                    var tiles=root.ArtEntries.Where(a=>a.Tile!=null && a.Id.StartsWith(prefix)).ToDictionary(a=>(TileBase)a.Tile,a=>a.Id);
                    var neighbors=new[]{Vector3Int.up,Vector3Int.right,Vector3Int.down,Vector3Int.left,new Vector3Int(1,1,0),new Vector3Int(1,-1,0),new Vector3Int(-1,-1,0),new Vector3Int(-1,1,0)};
                    bool Connected(Vector3Int p)=>map.GetTile(p)!=null && tiles.ContainsKey(map.GetTile(p));
                    for(int y=-1;y<=1;y++) for(int x=-1;x<=1;x++)
                    {
                        var p=cell+new Vector3Int(x,y,0); if(!Connected(p)) continue;
                        int mask=0; for(int i=0;i<8;i++) if(Connected(p+neighbors[i])) mask|=1<<i;
                        mask=DungeonWallCatalog.Normalize(mask); map.SetTile(p,root.FindArt(prefix+mask.ToString("X2")).Tile);
                        map.SetTileFlags(p,TileFlags.None); map.SetTransformMatrix(p,Matrix4x4.identity);
                    }
                }
            }
            else
            {
                GameObject go;
                if(selected.PhysicalKind==DungeonCellKind.LowCover)
                { try { go=DungeonAuthoringScene.PlaceCover(root,selected,position,room); } catch(InvalidOperationException e) { status=e.Message;return; } }
                else { go=DungeonAuthoringScene.Place(root,selected,position); go.GetComponent<DungeonAuthoringElement>().Room=room; }
                Undo.RegisterCreatedObjectUndo(go,"Colocar decoração"); Selection.activeGameObject=go;
            }
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        }
        void ReplaceSelection()
        {
            var element=Selection.activeGameObject!=null ? Selection.activeGameObject.GetComponent<DungeonAuthoringElement>() : null;
            if(element!=null && element.Kind==DungeonElementKind.Point && element.Id.StartsWith("Breakable/") && selected.Family=="BreakableStates")
            {
                var parts=selected.Id.Split('_');
                if(parts[2]!="Intact") { status="A peça inicia intacta. Edite a arte quebrada pelo banco de estados funcionais."; return; }
                var breakableRender=element.GetComponent<SpriteRenderer>();
                Undo.RecordObjects(new UnityEngine.Object[]{element,element.transform,breakableRender},"Trocar variante do quebrável");
                element.Family=parts[1]; element.Variant=int.Parse(parts[3]); breakableRender.sprite=selected.Sprite;
                element.ImportedSpriteSize=selected.Sprite.bounds.size;
                element.transform.localScale=new Vector3(.9f/selected.Sprite.bounds.size.x,.9f/selected.Sprite.bounds.size.y,1);
                return;
            }
            if(element==null || (element.Kind!=DungeonElementKind.Visual && element.Kind!=DungeonElementKind.FunctionalVisual)) { status="Selecione uma peça visual da autoria."; return; }
            var render=element.GetComponent<SpriteRenderer>(); var old=render.sprite;
            Undo.RecordObjects(new UnityEngine.Object[]{element,render,element.transform},"Trocar arte");
            var scale=element.transform.localScale;
            element.transform.localScale=new Vector3(scale.x*old.bounds.size.x/selected.Sprite.bounds.size.x,scale.y*old.bounds.size.y/selected.Sprite.bounds.size.y,scale.z);
            render.sprite=selected.Sprite; element.AssetId=selected.Id;
        }
        void SetState(string slot,string asset)
        {
            Undo.RecordObject(root,"Editar estado de extração");
            var list=new List<DungeonArtReplacement>(root.Document.artReplacements??Array.Empty<DungeonArtReplacement>());
            list.RemoveAll(r=>r.slot==slot); if(asset!=null) list.Add(new DungeonArtReplacement {slot=slot,asset=asset});
            root.Document.artReplacements=list.OrderBy(r=>r.slot,StringComparer.Ordinal).ToArray();
            foreach(var element in root.GetComponentsInChildren<DungeonAuthoringElement>())
            {
                if(element.Kind==DungeonElementKind.Point && element.Id.StartsWith("Breakable/") && slot=="Breakable_"+element.Family+"_Intact_"+element.Variant)
                {
                    var next=root.FindArt(asset??slot); var render=element.GetComponent<SpriteRenderer>();
                    if(next?.Sprite!=null && render!=null)
                    { Undo.RecordObjects(new UnityEngine.Object[]{element,render,element.transform},"Atualizar estado do quebrável"); render.sprite=next.Sprite; element.ImportedSpriteSize=next.Sprite.bounds.size; element.transform.localScale=new Vector3(.9f/next.Sprite.bounds.size.x,.9f/next.Sprite.bounds.size.y,1); }
                }
                if(element.Kind==DungeonElementKind.FunctionalVisual && element.Group==slot)
                {
                    var next=root.FindArt(asset??slot); var render=element.GetComponent<SpriteRenderer>(); if(next?.Sprite==null || render==null) continue;
                    Undo.RecordObjects(new UnityEngine.Object[]{element,render,element.transform},"Atualizar prévia do estado");
                    var scale=element.transform.localScale; var old=render.sprite;
                    element.transform.localScale=new Vector3(scale.x*old.bounds.size.x/next.Sprite.bounds.size.x,scale.y*old.bounds.size.y/next.Sprite.bounds.size.y,scale.z);
                    element.AssetId=next.Id; render.sprite=next.Sprite;
                }
            }
            EditorUtility.SetDirty(root); EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        }
        void SceneGUI(SceneView view)
        {
            if(root==null || Application.isPlaying) return;
            var e=Event.current;
            if(e.type==EventType.KeyDown && e.keyCode==KeyCode.Escape) { painting=false; stroke=false; e.Use(); Repaint(); return; }
            if(e.rawType==EventType.MouseUp) { stroke=false; QueueRefresh(); }
            if(!painting || (tab!=0 && tab!=1) || e.alt) return;
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            var ray=HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if(!new Plane(Vector3.forward,Vector3.zero).Raycast(ray,out float distance)) return;
            Vector2 p=ray.GetPoint(distance);
            if(e.button!=0 || (e.type!=EventType.MouseDown && e.type!=EventType.MouseDrag)) return;
            if(tab==0 && (e.type==EventType.MouseDown || selected?.Tile!=null)) Place(p);
            else if(tab==1 && target!=null)
            {
                var map=target.GetComponent<Tilemap>();
                if(!stroke) { Undo.RegisterCompleteObjectUndo(map,"Pintar terreno físico"); stroke=true; }
                map.SetTile(map.WorldToCell(p),brush==DungeonCellKind.Stone ? root.StoneTile : brush==DungeonCellKind.Floor ? root.FloorTile : brush==DungeonCellKind.Water ? root.WaterTile : root.CoverTile);
                EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            }
            e.Use(); view.Repaint();
        }
        void QueueRefresh()
        {
            if(queued) return; queued=true;
            EditorApplication.delayCall+=()=>{ queued=false; if(root!=null && !Application.isPlaying) { DungeonAuthoringScene.RefreshArt(root); Repaint(); } };
        }
        void ExportPrefabs()
        {
            int count=ExportPrefabs(root);
            status=count+" prefabs novos disponibilizados; prefabs existentes preservados.";
        }
        public static int ExportPrefabs(DungeonMapAuthoring root)
        {
            string folder=root.PrefabFolder; Directory.CreateDirectory(folder); AssetDatabase.Refresh(); int count=0;
            foreach(var entry in root.ArtEntries.Where(e=>!e.Structural && e.Tile==null))
            {
                string path=folder+"/"+entry.Id+".prefab"; if(File.Exists(path)) continue;
                var go=entry.PhysicalKind==DungeonCellKind.LowCover ? DungeonAuthoringScene.PlaceCover(root,entry,Vector2.zero,-1,false) : DungeonAuthoringScene.Place(root,entry,Vector2.zero);
                foreach(var element in go.GetComponentsInChildren<DungeonAuthoringElement>()) if(element.Kind==DungeonElementKind.Visual) element.Id="";
                PrefabUtility.SaveAsPrefabAsset(go,path); DestroyImmediate(go); count++;
            }
            foreach(var group in root.GetComponentsInChildren<DungeonAuthoringElement>().Where(e=>e.Kind==DungeonElementKind.Visual).GroupBy(e=>"R"+e.Room.ToString("00")+"_"+e.Group))
            {
                if(group.Count()<2) continue;
                string name=string.Concat(group.Key.Select(c=>char.IsLetterOrDigit(c) || c=='_' || c=='-' ? c : '_'));
                string path=folder+"/Conjunto_"+name+".prefab"; if(File.Exists(path)) continue;
                var center=Vector3.zero; foreach(var element in group) center+=element.transform.position; center/=group.Count();
                var holder=new GameObject("Conjunto_"+name); holder.transform.position=center;
                foreach(var element in group)
                {
                    var copy=Instantiate(element.gameObject,element.transform.position,element.transform.rotation);
                    copy.transform.localScale=element.transform.lossyScale; copy.transform.SetParent(holder.transform,true);
                    copy.GetComponent<DungeonAuthoringElement>().Id="";
                }
                holder.transform.position=Vector3.zero; PrefabUtility.SaveAsPrefabAsset(holder,path); DestroyImmediate(holder); count++;
            }
            AssetDatabase.SaveAssets(); return count;
        }
    }
}
