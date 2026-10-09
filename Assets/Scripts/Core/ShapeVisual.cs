using UnityEngine;

namespace CyberExtraction.Core
{
    // Visual de forma simples (circulo/quadrado) aplicado em runtime.
    [RequireComponent(typeof(SpriteRenderer))]
    public class ShapeVisual : MonoBehaviour
    {
        public enum Shape { Circle, Square }

        public Shape shape = Shape.Circle;
        public Color color = Color.white;
        public int sortingOrder = 0;

        private void Awake()
        {
            var sr = GetComponent<SpriteRenderer>();
            SpriteFactory.Setup(sr, shape == Shape.Circle ? SpriteFactory.Circle : SpriteFactory.Square, color, sortingOrder);
        }
    }
}
