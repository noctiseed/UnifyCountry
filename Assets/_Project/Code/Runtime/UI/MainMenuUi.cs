using System.Collections.Generic;
using UnifyCountry.Map;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UnifyCountry.UI
{
    [ExecuteAlways]
    public sealed class MainMenuUi : MonoBehaviour
    {
        private const string RunMapSceneName = "SCN_RunMap";
        private const string CardCollectionSceneName = "SCN_CardCollection";
        private static readonly Color BackgroundTop = new Color(0.47f, 0.68f, 0.74f);
        private static readonly Color BackgroundBottom = new Color(0.22f, 0.36f, 0.31f);
        private static readonly Color PaperColor = new Color(0.93f, 0.62f, 0.13f);
        private static readonly Color PaperShadow = new Color(0.46f, 0.22f, 0.05f);
        private static readonly Color WoodColor = new Color(0.63f, 0.19f, 0.06f);
        private static readonly Color GoldColor = new Color(0.82f, 0.61f, 0.25f);

        [SerializeField] private Font uiFont;

        private Canvas canvas;
        private Sprite roundedSprite;

        private void Awake()
        {
            if (Application.isPlaying)
                Build();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
                Build();
        }

        [ContextMenu("Rebuild Main Menu")]
        public void Build()
        {
            EnsureFont();
            ClearChildren();
            EnsureCamera();
            EnsureEventSystem();

            canvas = CreateCanvas();
            BuildBackground(canvas.transform);
            BuildScrollBoard(canvas.transform);
        }

        private void EnsureFont()
        {
            if (uiFont == null)
                uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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

        private void EnsureCamera()
        {
            if (Camera.main != null)
                return;

            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.transform.SetParent(transform, false);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var sceneCamera = cameraObject.GetComponent<Camera>();
            sceneCamera.clearFlags = CameraClearFlags.SolidColor;
            sceneCamera.backgroundColor = BackgroundTop;
            sceneCamera.orthographic = true;
            sceneCamera.orthographicSize = 5f;
        }

        private void EnsureEventSystem()
        {
            var eventSystems = FindObjectsOfType<EventSystem>();
            if (eventSystems.Length > 0)
            {
                for (var i = 1; i < eventSystems.Length; i++)
                {
                    if (Application.isPlaying)
                        Destroy(eventSystems[i].gameObject);
                    else
                        DestroyImmediate(eventSystems[i].gameObject);
                }

                return;
            }

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystem.transform.SetParent(transform, false);
        }

        private Canvas CreateCanvas()
        {
            var root = new GameObject("Main Menu Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);

            var result = root.GetComponent<Canvas>();
            result.renderMode = RenderMode.ScreenSpaceOverlay;
            result.sortingOrder = 100;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;

            return result;
        }

        private void BuildBackground(Transform parent)
        {
            var top = CreateImage(parent, "Sky Wash", BackgroundTop);
            SetRect(top.rectTransform, new Vector2(0f, 0.48f), Vector2.one, Vector2.zero, Vector2.zero);

            var bottom = CreateImage(parent, "Distant Land", BackgroundBottom);
            SetRect(bottom.rectTransform, Vector2.zero, new Vector2(1f, 0.58f), Vector2.zero, Vector2.zero);

            CreateLandscapeBand(parent, "Rear Mountains", new Vector2(0f, 0.43f), new Vector2(1f, 0.67f), new Color(0.58f, 0.66f, 0.48f, 0.7f));
            CreateLandscapeBand(parent, "Middle Hills", new Vector2(0f, 0.28f), new Vector2(1f, 0.48f), new Color(0.32f, 0.49f, 0.31f, 0.82f));
            CreateLandscapeBand(parent, "Front Ground", new Vector2(0f, 0f), new Vector2(1f, 0.22f), new Color(0.2f, 0.29f, 0.21f, 0.95f));
        }

        private void CreateLandscapeBand(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var band = CreateImage(parent, name, color);
            SetRect(band.rectTransform, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        }

        private void BuildScrollBoard(Transform parent)
        {
            var shadow = CreateImage(parent, "Board Shadow", new Color(0.06f, 0.025f, 0.01f, 0.38f));
            shadow.sprite = GetRoundedSprite();
            shadow.type = Image.Type.Sliced;
            SetRect(shadow.rectTransform, new Vector2(0.18f, 0.13f), new Vector2(0.82f, 0.84f), new Vector2(18f, -18f), Vector2.zero);

            var board = CreateImage(parent, "Menu Board", PaperColor);
            board.sprite = GetRoundedSprite();
            board.type = Image.Type.Sliced;
            SetRect(board.rectTransform, new Vector2(0.17f, 0.15f), new Vector2(0.81f, 0.86f), Vector2.zero, Vector2.zero);
            CreateBorder(board.transform, PaperShadow, 6f);

            BuildFrame(board.transform);
            BuildMenuColumns(board.transform);
            BuildDecorations(board.transform);
        }

        private void BuildFrame(Transform parent)
        {
            CreateFrameBand(parent, "Top Wood", new Vector2(-0.01f, 0.92f), new Vector2(1.01f, 1.02f), WoodColor);
            CreateFrameBand(parent, "Bottom Wood", new Vector2(-0.01f, -0.02f), new Vector2(1.01f, 0.08f), WoodColor);
            CreateFrameBand(parent, "Left Wood", new Vector2(-0.025f, -0.02f), new Vector2(0.035f, 1.02f), WoodColor);
            CreateFrameBand(parent, "Right Wood", new Vector2(0.965f, -0.02f), new Vector2(1.025f, 1.02f), WoodColor);

            var corners = new[]
            {
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, 0f),
                new Vector2(1f, 0f)
            };

            foreach (var corner in corners)
            {
                var plate = CreateImage(parent, "Corner Plate", GoldColor);
                plate.sprite = GetRoundedSprite();
                plate.type = Image.Type.Sliced;
                plate.rectTransform.anchorMin = corner;
                plate.rectTransform.anchorMax = corner;
                plate.rectTransform.pivot = corner;
                plate.rectTransform.sizeDelta = new Vector2(84f, 84f);
                plate.rectTransform.anchoredPosition = Vector2.zero;
                CreateBorder(plate.transform, new Color(0.36f, 0.22f, 0.08f), 3f);
            }
        }

        private void CreateFrameBand(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var band = CreateImage(parent, name, color);
            SetRect(band.rectTransform, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            CreateBorder(band.transform, new Color(0.29f, 0.08f, 0.025f), 3f);
        }

        private void BuildMenuColumns(Transform parent)
        {
            var items = new List<MenuItem>
            {
                new MenuItem("\u4e00\u7edf\u5929\u4e0b", MenuAction.StartGame),
                new MenuItem("\u7fa4\u96c4\u5e76\u8d77", MenuAction.Disabled),
                new MenuItem("\u91cd\u63d0\u5f80\u4e8b", MenuAction.Disabled),
                new MenuItem("\u7ea6\u6cd5\u4e09\u7ae0", MenuAction.Disabled),
                new MenuItem("\u5367\u864e\u85cf\u9f99", MenuAction.OpenCardCollection),
                new MenuItem("\u5f52\u9690\u5c71\u6797", MenuAction.Disabled)
            };

            const float right = 0.84f;
            const float columnWidth = 0.105f;
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var xMax = right - i * columnWidth;
                var xMin = xMax - 0.08f;
                var button = CreateMenuButton(parent, item);
                SetRect(button.GetComponent<RectTransform>(), new Vector2(xMin, 0.16f), new Vector2(xMax, 0.84f), Vector2.zero, Vector2.zero);
            }
        }

        private Button CreateMenuButton(Transform parent, MenuItem item)
        {
            var root = CreateImage(parent, item.Label, item.Enabled ? new Color(0.86f, 0.36f, 0.06f, 0.4f) : new Color(0.82f, 0.46f, 0.15f, 0.18f));
            root.sprite = GetRoundedSprite();
            root.type = Image.Type.Sliced;

            var button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            button.targetGraphic = root;
            button.interactable = item.Enabled;

            if (item.Action == MenuAction.StartGame)
                button.onClick.AddListener(StartGame);
            else if (item.Action == MenuAction.OpenCardCollection)
                button.onClick.AddListener(OpenCardCollection);

            var label = CreateText(root.transform, ToVerticalText(item.Label), item.Enabled ? 42 : 38, TextAnchor.MiddleCenter, item.Enabled ? Color.white : new Color(1f, 0.86f, 0.6f, 0.7f));
            label.fontStyle = FontStyle.Bold;
            label.lineSpacing = 1.16f;
            SetRect(label.rectTransform, new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.96f), Vector2.zero, Vector2.zero);
            AddShadow(label.gameObject, item.Enabled ? new Color(0.26f, 0.06f, 0.02f, 0.86f) : new Color(0.36f, 0.16f, 0.04f, 0.45f), new Vector2(3f, -3f));

            if (item.Enabled)
                CreateBorder(root.transform, new Color(1f, 0.86f, 0.42f), 3f);

            return button;
        }

        private void BuildDecorations(Transform parent)
        {
            var seal = CreateImage(parent, "Red Seal", new Color(0.72f, 0.06f, 0.03f, 0.9f));
            seal.sprite = GetRoundedSprite();
            seal.type = Image.Type.Sliced;
            SetRect(seal.rectTransform, new Vector2(0.08f, 0.12f), new Vector2(0.18f, 0.28f), Vector2.zero, Vector2.zero);
            var sealText = CreateText(seal.transform, "\u4ee4", 52, TextAnchor.MiddleCenter, new Color(1f, 0.88f, 0.62f));
            sealText.fontStyle = FontStyle.Bold;
            SetRect(sealText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var dice = CreateImage(parent, "Dice", new Color(0.96f, 0.93f, 0.86f));
            dice.sprite = GetRoundedSprite();
            dice.type = Image.Type.Sliced;
            SetRect(dice.rectTransform, new Vector2(0.82f, 0.77f), new Vector2(0.91f, 0.9f), Vector2.zero, Vector2.zero);
            CreateBorder(dice.transform, new Color(0.2f, 0.16f, 0.12f), 3f);
            var dot = CreateText(dice.transform, "\u25cf\n  \u25cf", 18, TextAnchor.MiddleCenter, new Color(0.12f, 0.08f, 0.06f));
            SetRect(dot.rectTransform, new Vector2(0.12f, 0.1f), new Vector2(0.88f, 0.9f), Vector2.zero, Vector2.zero);
        }

        private void StartGame()
        {
            RunSession.BeginNewRun();
            SceneManager.LoadScene(RunMapSceneName);
        }

        private void OpenCardCollection()
        {
            SceneManager.LoadScene(CardCollectionSceneName);
        }

        private static string ToVerticalText(string value)
        {
            return string.Join("\n", new List<char>(value.ToCharArray()).ConvertAll(character => character.ToString()).ToArray());
        }

        private Image CreateImage(Transform parent, string name, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            var image = obj.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private Text CreateText(Transform parent, string content, int size, TextAnchor alignment, Color color)
        {
            var obj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            var text = obj.GetComponent<Text>();
            text.text = content;
            text.font = uiFont;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void AddShadow(GameObject target, Color color, Vector2 distance)
        {
            var shadow = target.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
        }

        private void CreateBorder(Transform target, Color color, float distance)
        {
            var outline = target.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(distance, -distance);
        }

        private Sprite GetRoundedSprite()
        {
            if (roundedSprite != null)
                return roundedSprite;

            var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            for (var y = 0; y < texture.height; y++)
            {
                for (var x = 0; x < texture.width; x++)
                    texture.SetPixel(x, y, Color.white);
            }

            texture.Apply();
            roundedSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(5f, 5f, 5f, 5f));
            return roundedSprite;
        }

        private readonly struct MenuItem
        {
            public MenuItem(string label, MenuAction action)
            {
                Label = label;
                Action = action;
            }

            public string Label { get; }
            public MenuAction Action { get; }
            public bool Enabled => Action != MenuAction.Disabled;
        }

        private enum MenuAction
        {
            Disabled,
            StartGame,
            OpenCardCollection
        }
    }
}
