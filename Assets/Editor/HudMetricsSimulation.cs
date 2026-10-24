#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using CyberExtraction.UI;

// Manual tooling only: never opens Unity or starts Play mode.
public static class HudMetricsSimulation
{
    [MenuItem("Cyber Extraction/Avançado/Simulação de HUD/320 dp · pequeno")]
    static void Small()=>HudMetrics.Simulate(new Vector2(0,320));
    [MenuItem("Cyber Extraction/Avançado/Simulação de HUD/360 dp · padrão")]
    static void Standard()=>HudMetrics.Simulate(new Vector2(0,360));
    [MenuItem("Cyber Extraction/Avançado/Simulação de HUD/480 dp · amplo")]
    static void Large()=>HudMetrics.Simulate(new Vector2(0,480));
    [MenuItem("Cyber Extraction/Avançado/Simulação de HUD/Automática")]
    static void Automatic()=>HudMetrics.Simulate(Vector2.zero);
}
#endif
