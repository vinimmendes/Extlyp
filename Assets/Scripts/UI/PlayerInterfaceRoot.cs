using UnityEngine;

namespace CyberExtraction.UI
{
    public sealed class PlayerInterfaceRoot : MonoBehaviour
    {
        [SerializeField] RectTransform safeArea;
        [SerializeField] Canvas canvas;
        [SerializeField] UnityEngine.UI.CanvasScaler scaler;
        [SerializeField] UguiPrefabView hud,menus,organizer,results,inventory,expedition,cabin,activities;
        [SerializeField] BaseHubView baseHub;
        public BaseHubView BaseHub {get;private set;}
        public RectTransform SafeArea=>safeArea;
        public Canvas Canvas=>canvas;
        public UnityEngine.UI.CanvasScaler Scaler=>scaler;
        public UguiPrefabView Hud {get;private set;}
        public UguiPrefabView Menus {get;private set;}
        public UguiPrefabView Organizer {get;private set;}
        public UguiPrefabView Results {get;private set;}
        public UguiPrefabView Inventory {get;private set;}
        public UguiPrefabView Expedition {get;private set;}
        public UguiPrefabView Cabin {get;private set;}
        public UguiPrefabView Activities {get;private set;}
        public void Initialize()
        {
            UiFontAssets.PrepareForUi();
            BaseHub=Instantiate(baseHub,safeArea,false); UiFontAssets.Apply(BaseHub.gameObject);
            Hud=Spawn(hud); Menus=Spawn(menus); Organizer=Spawn(organizer); Results=Spawn(results);
            Inventory=Spawn(inventory); Expedition=Spawn(expedition); Cabin=Spawn(cabin); Activities=Spawn(activities);
        }
        UguiPrefabView Spawn(UguiPrefabView prefab)
        {
            if(prefab==null) throw new System.InvalidOperationException("Prefab da interface nao configurado.");
            var view=Instantiate(prefab,safeArea,false); UiFontAssets.Apply(view.gameObject); return view;
        }
    }
}
