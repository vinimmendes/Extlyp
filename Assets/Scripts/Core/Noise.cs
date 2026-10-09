using System;
using UnityEngine;

namespace CyberExtraction.Core
{
    // Barulho global: tiros chamam a atencao do monstro (so o servidor escuta).
    public static class Noise
    {
        public static event Action<Vector2, float> OnNoise;
        public static void Emit(Vector2 position, float radius) => OnNoise?.Invoke(position, radius);
    }
}
