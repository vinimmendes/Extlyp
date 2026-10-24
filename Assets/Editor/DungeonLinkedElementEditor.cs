using System;
using CyberExtraction.Match;
using UnityEditor;
using UnityEngine;

namespace CyberExtraction.EditorTools
{
    // Selecting an extraction marker moves its assembly, including terrain and
    // path endpoints, rather than separating the trigger from its presentation.
    [CustomEditor(typeof(DungeonAuthoringElement))]
    public sealed class DungeonLinkedElementEditor : Editor
    {
        DungeonAuthoringElement element;
        bool linked,previousHidden;
        void OnEnable()
        {
            element=(DungeonAuthoringElement)target;
            linked=element.Kind==DungeonElementKind.Point && element.Id!=null && element.Id.StartsWith("Exit/",StringComparison.Ordinal);
            if(linked) { previousHidden=Tools.hidden; Tools.hidden=true; }
        }
        void OnDisable() { if(linked) Tools.hidden=previousHidden; }
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector(); if(!linked) return;
            EditorGUILayout.HelpBox("A posição desta extração move o conjunto vinculado, incluindo margens, mecanismos e percurso. Edite dimensões de cada peça separadamente.",MessageType.Info);
            EditorGUI.BeginChangeCheck(); Vector2 point=EditorGUILayout.Vector2Field("Mover extração",element.transform.position);
            if(EditorGUI.EndChangeCheck()) Move(point);
            if(GUILayout.Button("Selecionar conjunto vinculado")) Selection.activeGameObject=element.transform.parent.gameObject;
        }
        void Move(Vector2 point)
        {
            var group=element.transform.parent; if(group==null) return;
            Vector2 delta=point-(Vector2)element.transform.position;
            if(group.GetComponentsInChildren<DungeonTerrainSource>().Length>0) delta=new Vector2(Mathf.Round(delta.x/1.06f)*1.06f,Mathf.Round(delta.y/1.06f)*1.06f);
            Undo.RecordObject(group,"Mover extração vinculada"); group.position+=(Vector3)delta;
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(group.gameObject.scene); DungeonAuthoringLifecycle.Queue();
        }
        void OnSceneGUI()
        {
            if(!linked || Application.isPlaying) return;
            EditorGUI.BeginChangeCheck(); Vector3 point=Handles.PositionHandle(element.transform.position,Quaternion.identity);
            if(EditorGUI.EndChangeCheck()) Move(point);
        }
    }
}
