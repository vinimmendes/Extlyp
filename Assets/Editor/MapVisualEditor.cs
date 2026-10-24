using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using CyberExtraction.Match;
using Unity.Netcode;

namespace CyberExtraction.EditorTools
{
    public sealed partial class MapVisualEditor : EditorWindow
    {
        [SerializeField] ExpeditionMap selectedMap=ExpeditionMap.Dungeon;
        MapVisualEdits edits;
        Transform world,target;
        Tilemap layer;
        Vector3Int cell;
        bool pick=true;
        Vector2 scroll;
        string filter="",status="";
        readonly List<Sprite> palette=new List<Sprite>();
        int selectedPalette=-1;
        [MenuItem("Cyber Extraction/Mapas/Editor visual (Play)", false, 11)]
        public static void Open()
        { var window=GetWindow<MapVisualEditor>("Editor de mapas"); window.minSize=new Vector2(380,420); window.Show(); }
        void OnEnable() { SceneView.duringSceneGui+=SceneGUI; Undo.undoRedoPerformed+=Reload; EditorApplication.playModeStateChanged+=PlayChanged; }
        void OnDisable() { CancelDrag(); ClearReference(); ClearAlphaCopies(); SceneView.duringSceneGui-=SceneGUI; Undo.undoRedoPerformed-=Reload; EditorApplication.playModeStateChanged-=PlayChanged; }
        void PlayChanged(PlayModeStateChange state)
        {
            CancelDrag(); ClearReference(); ClearAlphaCopies(); hits.Clear(); hasPick=false; selectedAddition=null; world=null; target=null; layer=null; palette.Clear(); Repaint();
            if(state==PlayModeStateChange.ExitingPlayMode && edits!=null) AssetDatabase.SaveAssetIfDirty(edits);
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("CyberExtraction.MapVisualPreview",false))
                EditorApplication.delayCall+=()=>
                {
                    if(this==null || !Application.isPlaying) return;
                    if(MapBuilder.Instance==null) new GameObject("MapEditorPreview").AddComponent<MapBuilder>();
                    LoadMap();
                    if(SessionState.GetBool("CyberExtraction.MapVisualReference",false)) {SessionState.SetBool("CyberExtraction.MapVisualReference",false); ShowReference();}
                };
        }
        bool CanEdit()
        {
            if(!Application.isPlaying || MapBuilder.Instance==null) return false;
            var network=NetworkManager.Singleton;
            return network==null || !network.IsListening || network.IsServer && MatchManager.Instance!=null && MatchManager.Instance.Sandbox && network.ConnectedClientsIds.Count<=1;
        }
        void OnGUI()
        {
            if(!Application.isPlaying && GUILayout.Button("Catacumbas · autoria visual e física fora do Play")) DungeonAuthoringWindow.Open();
            EditorGUILayout.HelpBox("Edição visual persistente pela Scene View em 2D. Colisões, navegação e regras de jogo são preservadas. As correções ficam em assets, mesmo ao sair do Play.",MessageType.Info);
            selectedMap=(ExpeditionMap)EditorGUILayout.EnumPopup("Mapa",selectedMap);
            if(!Application.isPlaying)
            {
                using(new EditorGUI.DisabledScope(EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode))
                    if(GUILayout.Button("Iniciar prévia offline (Play)"))
                    { SessionState.SetBool("CyberExtraction.MapVisualPreview",true); EditorApplication.isPlaying=true; }
            }
            using(new EditorGUI.DisabledScope(!CanEdit()))
                if(GUILayout.Button("Carregar mapa para edição")) LoadMap();
            if(!CanEdit()) { EditorGUILayout.HelpBox("Disponível em Play offline ou como host de testes sem outros jogadores. Não edita partidas normais ou clientes conectados a outro servidor.",MessageType.Info); return; }
            if(world==null || MapBuilder.Instance.EditingWorld!=world || MapBuilder.Current!=selectedMap)
            { EditorGUILayout.HelpBox("Carregue o mapa selecionado para começar.",MessageType.Info); return; }
            using(new EditorGUILayout.HorizontalScope())
            {
                pick=GUILayout.Toggle(pick,"Selecionar no mapa",EditorStyles.miniButton);
                if(GUILayout.Button("Ver mapa inteiro")) FrameMap();
                if(GUILayout.Button("Salvar")) { AssetDatabase.SaveAssetIfDirty(edits); status="Ajustes salvos no projeto."; }
            }
            if(GUILayout.Button("Encaixes · ver composição de referência")) ShowReference();
            EditorGUILayout.HelpBox("Clique e arraste a peça na Scene View. Shift inverte o encaixe; Escape cancela. Delete apaga, Ctrl+D duplica e F aproxima. Escolha uma miniatura para substituir a arte.",MessageType.None);
            scroll=EditorGUILayout.BeginScrollView(scroll);
            DrawGridControls();
            DrawVisualSelection();
            DrawVisualPalette();
            if(!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status,MessageType.Info);
            advanced=EditorGUILayout.Foldout(advanced,"Avançado / correções salvas");
            if(advanced) DrawAdvanced();
            EditorGUILayout.EndScrollView();
        }
        void LoadMap()
        {
            CancelDrag(); ClearReference(); ClearAlphaCopies(); hasPick=false; selectedAddition=null;
            if(edits!=null) AssetDatabase.SaveAssetIfDirty(edits);
            MapBuilder.Configure(selectedMap);
            world=MapBuilder.Instance.EditingWorld; target=null; layer=null; cell=default;
            string folder="Assets/Resources/MapEdits";
            if(!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Resources","MapEdits");
            string path="Assets/Resources/"+MapVisualEdits.ResourcePath(selectedMap)+".asset";
            edits=AssetDatabase.LoadAssetAtPath<MapVisualEdits>(path);
            if(edits==null) { edits=CreateInstance<MapVisualEdits>(); edits.Map=selectedMap; AssetDatabase.CreateAsset(edits,path); AssetDatabase.SaveAssetIfDirty(edits); }
            MapVisualEditRuntime.Apply(world,selectedMap); RebuildPalette();
            LoadGridPreferences();
            var scene=SceneView.lastActiveSceneView!=null ? SceneView.lastActiveSceneView : GetWindow<SceneView>();
            scene.Show(); scene.in2DMode=true; scene.Frame(new Bounds(Vector3.zero,new Vector3(MapBuilder.Width,MapBuilder.Height,1)),false); scene.Focus();
            hits.Clear(); status="Clique diretamente na peça que deseja mudar, na Scene View.";
        }
        void RebuildPalette()
        {
            palette.Clear(); selectedPalette=-1;
            var seen=new HashSet<Sprite>();
            foreach(var behaviour in world.GetComponentsInChildren<MonoBehaviour>(true))
                if(behaviour is IMapSpritePalette source)
                    foreach(var sprite in source.PaletteSprites) if(sprite!=null && seen.Add(sprite)) palette.Add(sprite);
            foreach(var map in world.GetComponentsInChildren<Tilemap>(true))
                foreach(var position in map.cellBounds.allPositionsWithin)
                { var sprite=map.GetSprite(position); if(sprite!=null && seen.Add(sprite)) palette.Add(sprite); }
            foreach(var renderer in world.GetComponentsInChildren<SpriteRenderer>(true))
                if(renderer.name!="MapVisualOverride" && !renderer.name.StartsWith("MapVisualCopy_",StringComparison.Ordinal) && !Runtime.TryGetVisualSource(renderer.gameObject,out _) && !Runtime.TryGetAdditionSource(renderer.gameObject,out _) && renderer.sprite!=null && seen.Add(renderer.sprite)) palette.Add(renderer.sprite);
        }
        MapVisualAddition SelectedAddition=>edits!=null && edits.Additions!=null ? edits.Additions.Find(a=>a.Id==selectedAddition) : null;
        MapVisualEdit Find()=>SelectedAddition!=null ? SelectedAddition.Style : edits.Edits.Find(e=>e.Matches(MapVisualEditRuntime.PathFor(world,target),layer!=null,cell));
        static string SpriteName(Sprite sprite)=>string.IsNullOrEmpty(sprite.name) ? sprite.texture.name+" · "+sprite.rect : sprite.name;
        bool Capture(MapVisualEdit edit,Sprite sprite)
        {
            if(!EditorUtility.IsPersistent(sprite.texture)) { status="Esta textura é gerada em memória. Escolha uma textura importada do projeto para persistir a troca."; return false; }
            edit.Texture=sprite.texture; edit.SpriteRect=sprite.rect; edit.Pivot=new Vector2(sprite.pivot.x/sprite.rect.width,sprite.pivot.y/sprite.rect.height); edit.PixelsPerUnit=sprite.pixelsPerUnit; return true;
        }
        void Changed() { EditorUtility.SetDirty(edits); Reload(); }
        void Reload()
        {
            if(CanEdit() && world!=null && edits!=null)
            {
                world.GetComponent<MapVisualEditRuntime>().Refresh(edits);
                if(!string.IsNullOrEmpty(selectedAddition) && SelectedAddition==null) {selectedAddition=null; target=null; layer=null;}
                if(hasPick && (Event.current==null || Event.current.type!=EventType.MouseDrag)) PickVisible(lastPick,false);
                SceneView.RepaintAll();
            }
            Repaint();
        }
        void SceneGUI(SceneView scene)
        {
            if(!CanEdit() || world==null || edits==null || MapBuilder.Current!=selectedMap || MapBuilder.Instance.EditingWorld!=world) return;
            DrawReferenceLabels();
            if(ManipulateScene(scene)) return;
            var e=Event.current;
            DrawMapGrid(scene);
            if(target!=null && target.IsChildOf(world))
            {
                DrawSelectionOutline();
                if(e.type==EventType.KeyDown && e.keyCode==KeyCode.F) { FrameSelection(); e.Use(); }
                Vector3 anchor=SelectionAnchor();
                var edit=Find();
                if(edit==null || !edit.Hidden)
                {
                    EditorGUI.BeginChangeCheck();
                    var point=Handles.PositionHandle(target.TransformPoint(anchor+(edit?.Offset ?? Vector3.zero)),target.rotation);
                    if(EditorGUI.EndChangeCheck())
                    {
                        BeginHandleDrag(); edit=EnsureEdit("Mover peça visual");
                        point=ConstrainPosition(point,e.shift); edit.Offset=target.InverseTransformPoint(point)-anchor; Changed();
                    }
                }
            }
            if(!pick) return;
            if(e.type==EventType.Layout) HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            if(e.type==EventType.MouseDown && e.button==0 && !e.alt && GUIUtility.hotControl==0)
            { PickVisible(HandleUtility.GUIPointToWorldRay(e.mousePosition)); BeginDirectDrag(e); e.Use(); }
        }

    }
    [InitializeOnLoad]
    static class MapVisualPreviewSession
    {
        static MapVisualPreviewSession()
        {
            // Also clear the session flag if the author closes the editor window during Play.
            EditorApplication.playModeStateChanged+=state=>
            { if(state==PlayModeStateChange.EnteredEditMode) {SessionState.SetBool("CyberExtraction.MapVisualPreview",false); SessionState.SetBool("CyberExtraction.MapVisualReference",false);} };
        }
    }
}
