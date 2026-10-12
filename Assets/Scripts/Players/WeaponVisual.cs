using UnityEngine;

namespace CyberExtraction.Players
{
    // Existing player/bot shooter entry point now installs the complete character.
    public class WeaponVisual : MonoBehaviour
    {
        void Awake()
        {
            if (GetComponent<PlayerCharacterVisual>() == null)
                gameObject.AddComponent<PlayerCharacterVisual>();
        }
    }
}
