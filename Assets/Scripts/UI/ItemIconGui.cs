using UnityEngine;
using CyberExtraction.Items;

namespace CyberExtraction.UI
{
    public static class ItemIconGui
    {
        public static void Draw(Rect rect, ItemId item)
        {
            var sprite = ItemIconLibrary.Get(item);
            if (sprite == null) { GUI.Label(rect, ItemCatalog.Icon(item)); return; }
            var pixels = sprite.rect;
            float fit = Mathf.Min(rect.width / pixels.width, rect.height / pixels.height);
            var target = new Rect(rect.center.x - pixels.width * fit / 2,
                rect.center.y - pixels.height * fit / 2, pixels.width * fit, pixels.height * fit);
            var uv = new Rect(pixels.x / sprite.texture.width, pixels.y / sprite.texture.height,
                pixels.width / sprite.texture.width, pixels.height / sprite.texture.height);
            var color = GUI.color;
            GUI.color = GUI.enabled ? Color.white : new Color(1, 1, 1, .45f);
            GUI.DrawTextureWithTexCoords(target, sprite.texture, uv, true);
            GUI.color = color;
        }

        public static bool Button(Rect rect, ItemId item, string text, GUIStyle style)
        {
            bool clicked = GUI.Button(rect, GUIContent.none, style);
            float size = Mathf.Min(rect.height - 8, rect.width * .36f);
            Draw(new Rect(rect.x + 4, rect.center.y - size / 2, size, size), item);
            GUI.Label(new Rect(rect.x + size + 8, rect.y + 2, rect.width - size - 12, rect.height - 4),
                text, new GUIStyle(GUI.skin.label) { fontSize = style.fontSize, alignment = TextAnchor.MiddleCenter, wordWrap = true });
            return clicked;
        }
    }
}
