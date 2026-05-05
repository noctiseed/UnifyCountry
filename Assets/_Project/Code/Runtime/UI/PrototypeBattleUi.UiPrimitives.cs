using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
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
            var block = CreateImage(parent, title, color);
            block.sprite = GetRoundedButtonSprite();
            block.type = Image.Type.Sliced;

            var outline = block.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.22f, 0.16f, 0.1f);
            outline.effectDistance = new Vector2(4f, -4f);

            var label = CreateText(block.transform, title, 24, TextAnchor.MiddleCenter, new Color(0.18f, 0.12f, 0.08f));
            SetRect(label.rectTransform, new Vector2(0f, 0.84f), Vector2.one, Vector2.zero, Vector2.zero);

            var valueText = CreateText(block.transform, value, 34, TextAnchor.MiddleCenter, new Color(0.16f, 0.1f, 0.05f));
            SetRect(valueText.rectTransform, new Vector2(0.08f, 0.18f), new Vector2(0.92f, 0.68f), Vector2.zero, Vector2.zero);

            return block.rectTransform;
        }

        private Button CreateClickableInfoBlock(Transform parent, string title, string value, Color color, UnityAction onClick)
        {
            var block = CreateImage(parent, title, color);
            block.sprite = GetRoundedButtonSprite();
            block.type = Image.Type.Sliced;

            var outline = block.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.22f, 0.16f, 0.1f);
            outline.effectDistance = new Vector2(4f, -4f);

            var button = block.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = color,
                highlightedColor = Color.Lerp(color, Color.white, 0.22f),
                pressedColor = Color.Lerp(color, Color.black, 0.12f),
                selectedColor = color,
                disabledColor = new Color(0.48f, 0.44f, 0.4f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };

            if (onClick != null)
                button.onClick.AddListener(onClick);

            var label = CreateText(block.transform, title, 24, TextAnchor.MiddleCenter, new Color(0.18f, 0.12f, 0.08f));
            SetRect(label.rectTransform, new Vector2(0f, 0.84f), Vector2.one, Vector2.zero, Vector2.zero);

            var valueText = CreateText(block.transform, value, 34, TextAnchor.MiddleCenter, new Color(0.16f, 0.1f, 0.05f));
            SetRect(valueText.rectTransform, new Vector2(0.08f, 0.18f), new Vector2(0.92f, 0.68f), Vector2.zero, Vector2.zero);

            return button;
        }

        private RectTransform CreateLargeModal(Transform parent, string title, out RectTransform contentRoot, out RectTransform headerRoot)
        {
            var modalRoot = new GameObject($"{title} Modal", typeof(RectTransform));
            modalRoot.transform.SetParent(parent, false);
            var modalRect = modalRoot.GetComponent<RectTransform>();
            SetRect(modalRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var dimmer = CreateImage(modalRoot.transform, "Dim Background", new Color(0f, 0f, 0f, 0.94f));
            SetRect(dimmer.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var panel = CreateImage(modalRoot.transform, "Modal Panel", new Color(0.96f, 0.88f, 0.66f));
            panel.sprite = GetRoundedButtonSprite();
            panel.type = Image.Type.Sliced;
            var panelRect = panel.rectTransform;
            SetRect(panelRect, new Vector2(0.14f, 0.12f), new Vector2(0.86f, 0.88f), Vector2.zero, Vector2.zero);

            var panelOutline = panel.gameObject.AddComponent<Outline>();
            panelOutline.effectColor = new Color(0.22f, 0.16f, 0.1f);
            panelOutline.effectDistance = new Vector2(4f, -4f);

            var content = CreateImage(panel.transform, "Content", new Color(1f, 0.96f, 0.78f, 0.45f));
            content.sprite = GetRoundedButtonSprite();
            content.type = Image.Type.Sliced;
            SetRect(content.rectTransform, new Vector2(0.035f, 0.055f), new Vector2(0.965f, 0.875f), Vector2.zero, Vector2.zero);
            contentRoot = content.rectTransform;

            var header = new GameObject("Header", typeof(RectTransform));
            header.transform.SetParent(panel.transform, false);
            headerRoot = header.GetComponent<RectTransform>();
            SetRect(headerRoot, new Vector2(0.035f, 0.895f), new Vector2(0.965f, 0.985f), Vector2.zero, Vector2.zero);

            var titleText = CreateText(header.transform, title, 30, TextAnchor.MiddleCenter, new Color(0.18f, 0.12f, 0.08f));
            SetRect(titleText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            titleText.raycastTarget = false;

            var closeButton = CreateButton(header.transform, "X");
            SetRect(closeButton.GetComponent<RectTransform>(), new Vector2(0.94f, 0.12f), new Vector2(0.995f, 0.9f), Vector2.zero, Vector2.zero);
            closeButton.onClick.AddListener(() =>
            {
                if (Application.isPlaying)
                    Destroy(modalRoot);
                else
                    DestroyImmediate(modalRoot);
            });

            return modalRect;
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

            const int size = 128;
            const int radius = 28;
            const float antialiasWidth = 1.5f;
            var texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x < radius ? radius - x : x >= size - radius ? x - (size - radius - 1) : 0f;
                    var dy = y < radius ? radius - y : y >= size - radius ? y - (size - radius - 1) : 0f;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    var alpha = Mathf.Clamp01(radius + antialiasWidth - distance);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
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

        private RectTransform CreatePanel(Transform parent, string title, Color color, bool showTitle = true)
        {
            var panel = CreateImage(parent, title, color);
            panel.raycastTarget = false;

            var outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.22f, 0.16f, 0.1f);
            outline.effectDistance = new Vector2(4f, -4f);

            if (showTitle)
            {
                var label = CreateText(panel.transform, title, 26, TextAnchor.MiddleCenter, new Color(0.18f, 0.12f, 0.08f));
                SetRect(label.rectTransform, new Vector2(0f, 0.86f), Vector2.one, Vector2.zero, Vector2.zero);
            }

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

        private void ClearChildren(Transform parent)
        {
            if (parent == null)
                return;

            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }
    }
}
