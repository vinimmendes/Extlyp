using UnityEngine;

namespace CyberExtraction.Players
{
    // Preferencia local de controle; o servidor continua recebendo a direcao escolhida.
    public static class TouchAimSettings
    {
        const string Key = "touch_aim_follow_movement_v1";
        const string SwapKey = "touch_swap_aim_fire_v1";
        const string AutoFireKey = "touch_auto_fire_v1";
        const string ScreenAimKey = "touch_aim_anywhere_v1";
        const string AutoReloadKey = "touch_auto_reload_v1";
        static bool loaded, followMovement;
        static bool swapLoaded, swapAimFire;
        static bool autoFireLoaded, autoFire;
        static bool screenAimLoaded, aimAnywhere;
        static bool autoReloadLoaded, autoReload;

        public static bool FollowMovement
        {
            get
            {
                if (!loaded)
                {
                    followMovement = PlayerPrefs.GetInt(Key, 1) == 1;
                    loaded = true;
                }
                return followMovement;
            }
        }

        public static string Label => "Mira ao andar: " + (FollowMovement ? "acompanhar movimento" : "manter direção");

        public static bool SwapAimFire
        {
            get
            {
                if (!swapLoaded)
                {
                    swapAimFire = PlayerPrefs.GetInt(SwapKey, 0) == 1;
                    swapLoaded = true;
                }
                return swapAimFire;
            }
        }

        public static string SwapLabel => "Trocar Mira e Atirar: " + (SwapAimFire ? "ativado" : "desativado");

        public static bool AutoFire
        {
            get
            {
                if (!autoFireLoaded)
                {
                    autoFire = PlayerPrefs.GetInt(AutoFireKey, 0) == 1;
                    autoFireLoaded = true;
                }
                return autoFire;
            }
        }

        public static string AutoFireLabel => "Tiro automático no toque: " + (AutoFire ? "ativado" : "desativado");

        public static bool AimAnywhere
        {
            get
            {
                if (!screenAimLoaded)
                {
                    aimAnywhere = PlayerPrefs.GetInt(ScreenAimKey, 0) == 1;
                    screenAimLoaded = true;
                }
                return aimAnywhere;
            }
        }

        public static string ScreenAimLabel => "Mirar em área livre: " + (AimAnywhere ? "ativado" : "desativado");

        public static bool AutoReload
        {
            get
            {
                if (!autoReloadLoaded)
                {
                    autoReload = PlayerPrefs.GetInt(AutoReloadKey, 0) == 1;
                    autoReloadLoaded = true;
                }
                return autoReload;
            }
        }

        public static string AutoReloadLabel => "Recarga automática no toque: " + (AutoReload ? "ativada" : "desativada");

        public static void ToggleAutoReload()
        {
            autoReload = !AutoReload;
            autoReloadLoaded = true;
            PlayerPrefs.SetInt(AutoReloadKey, autoReload ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void ToggleScreenAim()
        {
            aimAnywhere = !AimAnywhere;
            screenAimLoaded = true;
            PlayerPrefs.SetInt(ScreenAimKey, aimAnywhere ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void ToggleAutoFire()
        {
            autoFire = !AutoFire;
            autoFireLoaded = true;
            PlayerPrefs.SetInt(AutoFireKey, autoFire ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void ToggleSwap()
        {
            swapAimFire = !SwapAimFire;
            swapLoaded = true;
            PlayerPrefs.SetInt(SwapKey, swapAimFire ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void Toggle()
        {
            followMovement = !FollowMovement;
            loaded = true;
            PlayerPrefs.SetInt(Key, followMovement ? 1 : 0);
            PlayerPrefs.Save();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { loaded = swapLoaded = autoFireLoaded = screenAimLoaded = autoReloadLoaded = false; }
    }
}
