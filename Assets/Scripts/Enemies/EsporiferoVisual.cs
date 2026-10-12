using UnityEngine;
using CyberExtraction.Core;
using CyberExtraction.Combat;

namespace CyberExtraction.Enemies
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Esporifero))]
    public class EsporiferoVisual : MonoBehaviour
    {
        public Sprite idleSprite;
        SpriteRenderer body;
        Esporifero creature;

        void Start()
        {
            body = GetComponent<SpriteRenderer>();
            creature = GetComponent<Esporifero>();
            // Start runs after ShapeVisual.Awake, which supplies the fallback shape.
            if (idleSprite != null)
                SpriteFactory.Setup(body, idleSprite, Color.white, 5);
            // Art faces down; Monster and NetworkTransform use +X as forward.
            // Rotate the sprite on a child, keeping the collision and navigation root intact.
            if (idleSprite != null)
            {
                var art = new GameObject("EsporiferoArt");
                art.transform.SetParent(transform, false);
                art.transform.localRotation = Quaternion.Euler(0, 0, 90);
                var child = art.AddComponent<SpriteRenderer>();
                SpriteFactory.Setup(child, idleSprite, Color.white, 5);
                body.sprite = null;
                body = child;
                GetComponent<VisibilityTarget>()?.Refresh();
            }
        }

        void Update()
        {
            if (body == null) return;
            bool warning = creature.IsSpawned && creature.IsPreparing;
            float pulse = warning ? .5f + .5f * Mathf.Sin(Time.time * 18f) : 0;
            body.color = Color.Lerp(Color.white, new Color(.76f, .77f, .36f), pulse * .65f);
            float breath = 1f + Mathf.Sin(Time.time * (warning ? 12f : 2f)) * (warning ? .04f : .01f);
            if (body.transform != transform) body.transform.localScale = new Vector3(breath, breath, 1);
        }
    }
}
