using System.Collections.Generic;
using UnifyCountry.Map;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UnifyCountry.UI
{
    [ExecuteAlways]
    public sealed class RunMapUi : MonoBehaviour
    {
        private const string BattleSceneName = "SCN_BattlePrototype";
        private const string MainMenuSceneName = "SCN_MainMenu";

        private static readonly Color BackgroundTop = new Color(0.34f, 0.48f, 0.53f);
        private static readonly Color BackgroundBottom = new Color(0.16f, 0.24f, 0.22f);
        private static readonly Color MapPaper = new Color(0.88f, 0.75f, 0.48f);
        private static readonly Color Ink = new Color(0.18f, 0.12f, 0.07f);
        private static readonly Color CampaignColor = new Color(0.75f, 0.22f, 0.12f);
        private static readonly Color BranchColor = new Color(0.24f, 0.44f, 0.32f);
        private static readonly Color CompletedColor = new Color(0.55f, 0.54f, 0.44f);
        private static readonly Color LockedColor = new Color(0.2f, 0.2f, 0.2f, 0.4f);

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

        [ContextMenu("Rebuild Run Map")]
        public void Build()
        {
            EnsureFont();
            ClearChildren();
            EnsureCamera();
            EnsureEventSystem();

            if (Application.isPlaying && !RunSession.HasActiveRun)
                RunSession.BeginNewRun();

            canvas = CreateCanvas();
            BuildBackground(canvas.transform);
            BuildHeader(canvas.transform);
            BuildMapPanel(canvas.transform);
            BuildFooter(canvas.transform);
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
            var root = new GameObject("Run Map Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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
            var top = CreateImage(parent, "Sky", BackgroundTop);
            SetRect(top.rectTransform, new Vector2(0f, 0.42f), Vector2.one, Vector2.zero, Vector2.zero);

            var bottom = CreateImage(parent, "Ground", BackgroundBottom);
            SetRect(bottom.rectTransform, Vector2.zero, new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero);

            CreateBand(parent, "Rear Ridge", new Vector2(0f, 0.42f), new Vector2(1f, 0.62f), new Color(0.52f, 0.55f, 0.42f, 0.76f));
            CreateBand(parent, "Middle Ridge", new Vector2(0f, 0.27f), new Vector2(1f, 0.47f), new Color(0.28f, 0.42f, 0.3f, 0.9f));
            CreateBand(parent, "Front Ridge", Vector2.zero, new Vector2(1f, 0.25f), new Color(0.13f, 0.2f, 0.16f, 1f));
        }

        private void CreateBand(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var band = CreateImage(parent, name, color);
            SetRect(band.rectTransform, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        }

        private void BuildHeader(Transform parent)
        {
            var title = CreateText(parent, "远征路线", 44, TextAnchor.MiddleLeft, Color.white);
            title.fontStyle = FontStyle.Bold;
            SetRect(title.rectTransform, new Vector2(0.06f, 0.88f), new Vector2(0.38f, 0.97f), Vector2.zero, Vector2.zero);
            AddShadow(title.gameObject, new Color(0f, 0f, 0f, 0.55f), new Vector2(3f, -3f));

            var status = RunSession.HasActiveRun && RunSession.Current.IsRunComplete
                ? "吕布已破，天下震动"
                : "选择可用节点推进当前 Run";
            var subtitle = CreateText(parent, status, 22, TextAnchor.MiddleLeft, new Color(0.95f, 0.88f, 0.66f));
            SetRect(subtitle.rectTransform, new Vector2(0.28f, 0.885f), new Vector2(0.68f, 0.95f), Vector2.zero, Vector2.zero);

            var restartButton = CreateButton(parent, "新开一局");
            SetRect(restartButton.GetComponent<RectTransform>(), new Vector2(0.76f, 0.895f), new Vector2(0.86f, 0.955f), Vector2.zero, Vector2.zero);
            restartButton.onClick.AddListener(() =>
            {
                RunSession.BeginNewRun();
                Build();
            });

            var backButton = CreateButton(parent, "返回");
            SetRect(backButton.GetComponent<RectTransform>(), new Vector2(0.875f, 0.895f), new Vector2(0.94f, 0.955f), Vector2.zero, Vector2.zero);
            backButton.onClick.AddListener(() => SceneManager.LoadScene(MainMenuSceneName));
        }

        private void BuildMapPanel(Transform parent)
        {
            var panel = CreateImage(parent, "Route Map", MapPaper);
            panel.sprite = GetRoundedSprite();
            panel.type = Image.Type.Sliced;
            SetRect(panel.rectTransform, new Vector2(0.055f, 0.18f), new Vector2(0.945f, 0.85f), Vector2.zero, Vector2.zero);
            CreateBorder(panel.transform, new Color(0.3f, 0.18f, 0.08f), 4f);

            BuildRouteLines(panel.transform);
            BuildNodes(panel.transform);
        }

        private void BuildRouteLines(Transform parent)
        {
            foreach (var node in RunSession.Nodes)
            {
                foreach (var nextNodeId in node.NextNodeIds)
                {
                    var next = RunSession.GetNode(nextNodeId);
                    if (next == null)
                        continue;

                    var lineColor = GetRouteColor(node.NodeId, next.NodeId);
                    CreateLine(parent, node.Position, next.Position, lineColor);
                }
            }
        }

        private Color GetRouteColor(string fromNodeId, string toNodeId)
        {
            if (!RunSession.HasActiveRun)
                return new Color(0.24f, 0.16f, 0.08f, 0.25f);

            var fromState = RunSession.GetNodeState(fromNodeId);
            var toState = RunSession.GetNodeState(toNodeId);
            if (fromState == RunMapNodeState.Completed && toState != RunMapNodeState.Locked)
                return new Color(0.35f, 0.18f, 0.08f, 0.9f);

            return new Color(0.24f, 0.16f, 0.08f, 0.28f);
        }

        private void CreateLine(Transform parent, Vector2 from, Vector2 to, Color color)
        {
            var line = CreateImage(parent, "Route", color);
            line.raycastTarget = false;
            var rect = line.rectTransform;
            rect.anchorMin = new Vector2((from.x + to.x) * 0.5f, (from.y + to.y) * 0.5f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(0.5f, 0.5f);

            var delta = to - from;
            var length = Mathf.Sqrt(delta.x * delta.x * 1100f * 1100f + delta.y * delta.y * 560f * 560f);
            rect.sizeDelta = new Vector2(length, 8f);
            rect.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(delta.y * 560f, delta.x * 1100f) * Mathf.Rad2Deg);
        }

        private void BuildNodes(Transform parent)
        {
            foreach (var node in RunSession.Nodes)
                BuildNode(parent, node);
        }

        private void BuildNode(Transform parent, RunMapNodeDefinition node)
        {
            var state = RunSession.GetNodeState(node.NodeId);
            var isCampaign = node.NodeType == RunMapNodeType.Campaign;
            var color = state == RunMapNodeState.Completed ? CompletedColor : isCampaign ? CampaignColor : BranchColor;
            if (state == RunMapNodeState.Locked)
                color = LockedColor;

            var root = CreateImage(parent, node.Title, color);
            root.sprite = GetRoundedSprite();
            root.type = Image.Type.Sliced;
            root.rectTransform.anchorMin = node.Position;
            root.rectTransform.anchorMax = node.Position;
            root.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            root.rectTransform.sizeDelta = isCampaign ? new Vector2(190f, 104f) : new Vector2(158f, 84f);
            root.rectTransform.anchoredPosition = Vector2.zero;
            CreateBorder(root.transform, state == RunMapNodeState.Available ? new Color(1f, 0.86f, 0.36f) : new Color(0.18f, 0.12f, 0.08f, 0.65f), state == RunMapNodeState.Available ? 4f : 2f);

            var button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            button.targetGraphic = root;
            button.interactable = state == RunMapNodeState.Available && (!RunSession.HasActiveRun || !RunSession.Current.IsRunComplete);
            button.onClick.AddListener(() => SelectNode(node.NodeId));

            var title = CreateText(root.transform, node.Title, isCampaign ? 24 : 20, TextAnchor.MiddleCenter, Color.white);
            title.fontStyle = FontStyle.Bold;
            SetRect(title.rectTransform, new Vector2(0.06f, 0.42f), new Vector2(0.94f, 0.9f), Vector2.zero, Vector2.zero);

            var subtitle = CreateText(root.transform, GetNodeSubtitle(node, state), 15, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.68f));
            SetRect(subtitle.rectTransform, new Vector2(0.06f, 0.1f), new Vector2(0.94f, 0.42f), Vector2.zero, Vector2.zero);
        }

        private static string GetNodeSubtitle(RunMapNodeDefinition node, RunMapNodeState state)
        {
            if (state == RunMapNodeState.Completed)
                return "已完成";

            if (state == RunMapNodeState.Locked)
                return "未解锁";

            return node.Subtitle;
        }

        private void SelectNode(string nodeId)
        {
            if (!RunSession.TrySelectNode(nodeId, out var node))
                return;

            if (node.NodeType == RunMapNodeType.Campaign)
            {
                SceneManager.LoadScene(BattleSceneName);
                return;
            }

            Build();
        }

        private void BuildFooter(Transform parent)
        {
            var footer = CreateImage(parent, "Footer", new Color(0.09f, 0.08f, 0.06f, 0.72f));
            footer.sprite = GetRoundedSprite();
            footer.type = Image.Type.Sliced;
            SetRect(footer.rectTransform, new Vector2(0.055f, 0.045f), new Vector2(0.945f, 0.15f), Vector2.zero, Vector2.zero);

            var routeText = "路线：尚未出征";
            if (RunSession.HasActiveRun && RunSession.Current.RouteHistory.Count > 0)
                routeText = "路线：" + string.Join("  /  ", RunSession.Current.RouteHistory.ToArray());

            var text = CreateText(footer.transform, routeText, 20, TextAnchor.MiddleLeft, new Color(0.95f, 0.86f, 0.66f));
            SetRect(text.rectTransform, new Vector2(0.025f, 0.18f), new Vector2(0.62f, 0.82f), Vector2.zero, Vector2.zero);

            var tip = CreateText(footer.transform, "分支节点当前只记录选择，后续可接恢复、商店、删牌、遗物等奖励。", 19, TextAnchor.MiddleRight, new Color(0.88f, 0.78f, 0.58f));
            SetRect(tip.rectTransform, new Vector2(0.42f, 0.18f), new Vector2(0.975f, 0.82f), Vector2.zero, Vector2.zero);
        }

        private Button CreateButton(Transform parent, string label)
        {
            var root = CreateImage(parent, label, new Color(0.62f, 0.17f, 0.08f));
            root.sprite = GetRoundedSprite();
            root.type = Image.Type.Sliced;

            var button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            button.targetGraphic = root;

            var text = CreateText(root.transform, label, 18, TextAnchor.MiddleCenter, Color.white);
            text.fontStyle = FontStyle.Bold;
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
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
    }
}
