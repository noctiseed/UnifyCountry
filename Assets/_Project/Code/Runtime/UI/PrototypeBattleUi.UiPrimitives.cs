using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnifyCountry.UI
{
    public sealed partial class PrototypeBattleUi
    {
        private void CreateBorder(Transform parent, Color color, float thickness)
        {
            CreateBorderPart(parent, "Border Top", color, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -thickness), Vector2.zero);
            CreateBorderPart(parent, "Border Bottom", color, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, thickness));
            CreateBorderPart(parent, "Border Left", color, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(thickness, 0f));
            CreateBorderPart(parent, "Border Right", color, new Vector2(1f, 0f), Vector2.one, new Vector2(-thickness, 0f), Vector2.zero);
        }

        private void CreateBorderPart(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var border = CreateImage(parent, name, color);
            border.raycastTarget = false;
            border.rectTransform.anchorMin = anchorMin;
            border.rectTransform.anchorMax = anchorMax;
            border.rectTransform.offsetMin = offsetMin;
            border.rectTransform.offsetMax = offsetMax;
            border.transform.SetAsLastSibling();
        }

        private RectTransform CreateBadge(Transform parent, string value, Color color)
        {
            var badge = CreateImage(parent, "Badge", color);
            badge.gameObject.AddComponent<Outline>().effectColor = new Color(0.15f, 0.1f, 0.07f);

            var text = CreateText(badge.transform, value, 20, TextAnchor.MiddleCenter, Color.white);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            return badge.rectTransform;
        }

        private RectTransform CreateInfoBlock(Transform parent, string title, string value, Color color)
        {
            var block = CreatePanel(parent, title, color);

            var valueText = CreateText(block.transform, value, 34, TextAnchor.MiddleCenter, new Color(0.16f, 0.1f, 0.05f));
            SetRect(valueText.rectTransform, new Vector2(0.08f, 0.18f), new Vector2(0.92f, 0.68f), Vector2.zero, Vector2.zero);

            return block;
        }

        private Button CreateButton(Transform parent, string label)
        {
            var image = CreateImage(parent, label, new Color(0.84f, 0.24f, 0.18f));
            image.sprite = GetRoundedButtonSprite();
            image.type = Image.Type.Sliced;

            var button = image.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = new Color(0.84f, 0.24f, 0.18f),
                highlightedColor = new Color(0.96f, 0.36f, 0.26f),
                pressedColor = new Color(0.64f, 0.15f, 0.12f),
                selectedColor = new Color(0.84f, 0.24f, 0.18f),
                disabledColor = new Color(0.48f, 0.44f, 0.4f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };

            var outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.16f, 0.08f, 0.05f, 0.75f);
            outline.effectDistance = new Vector2(2f, -2f);

            var text = CreateText(image.transform, label, 21, TextAnchor.MiddleCenter, Color.white);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            return button;
        }

        private Sprite GetRoundedButtonSprite()
        {
            if (roundedButtonSprite != null)
                return roundedButtonSprite;

            const int size = 32;
            const int radius = 9;
            var texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x < radius ? radius - x : x >= size - radius ? x - (size - radius - 1) : 0;
                    var dy = y < radius ? radius - y : y >= size - radius ? y - (size - radius - 1) : 0;
                    var inside = dx * dx + dy * dy <= radius * radius;
                    pixels[y * size + x] = inside ? Color.white : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            roundedButtonSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            return roundedButtonSprite;
        }

        private Image CreateImage(Transform parent, string name, Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);

            var image = gameObject.GetComponent<Image>();
            image.color = color;

            return image;
        }

        private Text CreateText(Transform parent, string value, int size, TextAnchor alignment, Color color)
        {
            var gameObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            gameObject.transform.SetParent(parent, false);

            var text = gameObject.GetComponent<Text>();
            text.text = value;
            text.font = uiFont;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(10, size - 8);
            text.resizeTextMaxSize = size;

            return text;
        }

        private Canvas CreateCanvas()
        {
            var canvasObject = new GameObject("Prototype Battle Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null)
                return;

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Object.DontDestroyOnLoad(eventSystem);
        }

        private void CreateBackground(Transform parent)
        {
            var background = CreateImage(parent, "Background", backgroundColor);
            SetRect(background.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var skyBand = CreateImage(parent, "Top Color Band", new Color(0.25f, 0.55f, 0.72f));
            SetRect(skyBand.rectTransform, new Vector2(0f, 0.88f), Vector2.one, Vector2.zero, Vector2.zero);
        }

        private RectTransform CreatePanel(Transform parent, string title, Color color)
        {
            var panel = CreateImage(parent, title, color);
            panel.raycastTarget = false;

            var outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.22f, 0.16f, 0.1f);
            outline.effectDistance = new Vector2(4f, -4f);

            var label = CreateText(panel.transform, title, 26, TextAnchor.MiddleCenter, new Color(0.18f, 0.12f, 0.08f));
            SetRect(label.rectTransform, new Vector2(0f, 0.86f), Vector2.one, Vector2.zero, Vector2.zero);

            return panel.rectTransform;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;

            if (anchorMin == anchorMax)
            {
                rect.anchoredPosition = anchoredPosition;
                rect.sizeDelta = sizeDelta;
            }
            else
            {
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
        }

        private void ClearChildren()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }
    }
}
