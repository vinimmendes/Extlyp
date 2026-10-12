using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CyberExtraction.Diagnostics
{
    public class CampPreview : MonoBehaviour
    {
        IEnumerator Start()
        {
            yield return new WaitForSeconds(1);
            // A hidden graphics window has no OS focus. Use an explicit virtual
            // keyboard and keep focus resets disabled only for this opt-in fixture.
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            var player = GameObject.Find("CampPlayer");
            Vector3 before = player.transform.position;
            var keyboard = InputSystem.AddDevice<Keyboard>("PreviewKeyboard");
            CyberExtraction.Players.GameInput.SetFocus(true);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            yield return new WaitForSeconds(0.6f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForSeconds(0.2f);
            if (player.transform.position.y <= before.y + 0.5f)
            { Debug.LogError("CAMP PREVIEW FAIL: movimento local."); Application.Quit(1); yield break; }
            Directory.CreateDirectory("Logs");
            CaptureWorld();
            yield return new WaitForSeconds(0.5f);
            // The gate is now south of the cabin; use the current anchor in this opt-in fixture.
            player.GetComponent<Rigidbody2D>().position=new Vector2(2000,0)+CyberExtraction.Camp.CabinScenery.Gate+Vector2.up*1.4f;
            yield return new WaitForSeconds(.2f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
            yield return new WaitForSeconds(0.1f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            if (!CyberExtraction.Camp.CampController.Instance.ExpeditionPanelOpen)
            { Debug.LogError("CAMP PREVIEW FAIL: interacao no portao."); Application.Quit(1); yield break; }
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/camp-gate-preview.png"));
            yield return new WaitForSeconds(0.5f);
            CyberExtraction.Camp.CampController.Instance.ExpeditionPanelOpen=false;
            CyberExtraction.UI.InventoryWindow.Instance.OpenSafe();
            yield return new WaitForSeconds(.2f);
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/inventory-preview.png"));
            yield return new WaitForSeconds(.5f);
            Debug.Log("CAMP PREVIEW PASS: movimento, portao e capturas.");
            Application.Quit(0);
        }

        static void CaptureWorld()
        {
            var camera = Camera.main;
            var target = new RenderTexture(1280, 720, 24);
            target.Create();
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            texture.Apply();
            File.WriteAllBytes(Path.GetFullPath("Logs/camp-preview.png"), texture.EncodeToPNG());
            RenderTexture.active = previous;
            target.Release();
            Destroy(target); Destroy(texture);
        }
    }
}
