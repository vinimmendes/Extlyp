using System.Collections.Generic;
using UnityEngine;

namespace CyberExtraction.Combat
{
    // Presentation follows replicated health, without modifying the base/death tint.
    [DefaultExecutionOrder(300)]
    public sealed class DamageFlash : MonoBehaviour
    {
        Health health;
        float until;
        readonly Dictionary<SpriteRenderer,SpriteRenderer> overlays=new Dictionary<SpriteRenderer,SpriteRenderer>();
        void Awake() { health=GetComponent<Health>(); }
        void OnEnable() { if(health!=null) health.Current.OnValueChanged+=Changed; }
        void OnDisable()
        {
            if(health!=null) health.Current.OnValueChanged-=Changed;
            until=0;
            foreach(var overlay in overlays.Values) if(overlay!=null) overlay.enabled=false;
        }
        void Changed(int previous,int current)
        {
            if(current>=previous || !health.IsSpawned || !health.IsClient) return;
            until=Time.time+.24f;
            foreach(var source in GetComponentsInChildren<SpriteRenderer>(true))
            {
                if(source.name=="DamageFlashOverlay" || overlays.ContainsKey(source)) continue;
                var go=new GameObject("DamageFlashOverlay"); go.transform.SetParent(source.transform,false);
                var overlay=go.AddComponent<SpriteRenderer>(); overlay.enabled=false;
                overlays.Add(source,overlay);
            }
            GetComponent<VisibilityTarget>()?.Refresh();
        }
        void LateUpdate()
        {
            float strength=Time.time<until ? .8f*Mathf.Clamp01((until-Time.time)/.24f) : 0f;
            foreach(var entry in overlays)
            {
                var source=entry.Key; var overlay=entry.Value;
                if(overlay==null) continue;
                overlay.enabled=source!=null && source.enabled && source.gameObject.activeInHierarchy && strength>0f;
                if(!overlay.enabled) continue;
                overlay.sprite=source.sprite; overlay.sharedMaterial=source.sharedMaterial;
                overlay.sortingLayerID=source.sortingLayerID; overlay.sortingOrder=source.sortingOrder+1;
                overlay.flipX=source.flipX; overlay.flipY=source.flipY;
                overlay.color=new Color(1f,.08f,.06f,strength*source.color.a);
            }
        }
    }
}
