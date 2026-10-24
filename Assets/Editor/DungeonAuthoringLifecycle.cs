using CyberExtraction.Match;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CyberExtraction.EditorTools
{
    [InitializeOnLoad]
    public static class DungeonAuthoringLifecycle
    {
        static bool queued;
        static DungeonAuthoringLifecycle()
        {
            EditorSceneManager.sceneOpened+=SceneOpened;
            Undo.undoRedoPerformed+=Queue;
            Queue();
        }
        static void SceneOpened(Scene scene,OpenSceneMode mode)=>Queue();
        public static void Queue()
        {
            if(queued || Application.isPlaying) return;
            queued=true;
            EditorApplication.delayCall+=()=>
            {
                queued=false; if(Application.isPlaying) return;
                foreach(var root in Object.FindObjectsByType<DungeonMapAuthoring>(FindObjectsSortMode.None))
                    if(root.Catalog!=null && root.Document?.cells?.Length==16384)
                    {
                        foreach(var point in root.GetComponentsInChildren<DungeonAuthoringElement>())
                            if(point.Kind==DungeonElementKind.Point && point.Id!=null && point.Id.StartsWith("Exit/")) point.transform.hideFlags|=HideFlags.NotEditable;
                        DungeonAuthoringScene.RefreshArt(root);
                    }
            };
        }
    }
}
