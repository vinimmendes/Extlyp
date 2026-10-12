using System.Collections.Generic;
using UnityEngine;

namespace CyberExtraction.Combat
{
    // Marca quem so deve aparecer quando esta dentro do cone de visao (jogadores e monstro).
    public class VisibilityTarget : MonoBehaviour
    {
        public static readonly List<VisibilityTarget> All = new List<VisibilityTarget>();

        public bool ForceHidden;
        SpriteRenderer[] rends;
        public void Refresh() { rends = null; }

        private void OnEnable() { All.Add(this); }
        private void OnDisable() { All.Remove(this); }

        public void SetVisible(bool visible)
        {
            if (rends == null) rends = GetComponentsInChildren<SpriteRenderer>(true);
            visible = visible && !ForceHidden;
            foreach (var r in rends) if (r != null) r.enabled = visible;
        }
    }
}
