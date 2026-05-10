using UnifyCountry.Map;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UnifyCountry.UI
{
    public sealed partial class PrototypeBattleUi
    {
        private const string MainMenuSceneName = "SCN_MainMenu";
        private const float RunMapPreviewContentWidth = 1080f;
        private const float RunMapPreviewContentHeight = 1160f;

        private void BuildRunMapButton(Transform parent)
        {
            var button = CreateTopMenuButton(parent, "地图");
            SetRect(button.GetComponent<RectTransform>(), new Vector2(0.025f, 0.915f), new Vector2(0.09f, 0.965f), Vector2.zero, Vector2.zero);
            button.onClick.AddListener(() => ShowRunMapPreview(parent));
        }

        private void BuildExitMenuButton(Transform parent)
        {
            var button = CreateTopMenuButton(parent, "菜单");
            SetRect(button.GetComponent<RectTransform>(), new Vector2(0.91f, 0.915f), new Vector2(0.975f, 0.965f), Vector2.zero, Vector2.zero);
            button.interactable = !isResolvingTurn;
            button.onClick.AddListener(() => ShowGameMenu(parent));
        }

        private Button CreateTopMenuButton(Transform parent, string label)
        {
            var image = CreateImage(parent, label, new Color(0.96f, 0.84f, 0.58f, 0.92f));
            image.sprite = GetRoundedButtonSprite();
            image.type = Image.Type.Sliced;

            var button = image.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = new Color(0.96f, 0.84f, 0.58f, 0.92f),
                highlightedColor = new Color(1f, 0.9f, 0.66f, 1f),
                pressedColor = new Color(0.76f, 0.56f, 0.3f, 1f),
                selectedColor = new Color(0.96f, 0.84f, 0.58f, 0.92f),
                disabledColor = new Color(0.56f, 0.5f, 0.42f, 0.65f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };

            var outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.28f, 0.18f, 0.09f, 0.55f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            var text = CreateText(image.transform, label, 18, TextAnchor.MiddleCenter, new Color(0.2f, 0.12f, 0.06f));
            text.fontStyle = FontStyle.Bold;
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            return button;
        }

        private void ShowGameMenu(Transform parent)
        {
            CloseExistingGameMenu(parent);

            var modalRoot = new GameObject("Game Menu Modal", typeof(RectTransform));
            modalRoot.transform.SetParent(parent, false);
            var modalRect = modalRoot.GetComponent<RectTransform>();
            SetRect(modalRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var dimmer = CreateImage(modalRoot.transform, "Dim Background", new Color(0f, 0f, 0f, 0.52f));
            SetRect(dimmer.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var panel = CreateImage(modalRoot.transform, "Game Menu Panel", new Color(0.96f, 0.88f, 0.66f));
            panel.sprite = GetRoundedButtonSprite();
            panel.type = Image.Type.Sliced;
            SetRect(panel.rectTransform, new Vector2(0.38f, 0.22f), new Vector2(0.62f, 0.78f), Vector2.zero, Vector2.zero);

            var outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.22f, 0.16f, 0.1f);
            outline.effectDistance = new Vector2(4f, -4f);

            var title = CreateText(panel.transform, "菜单", 34, TextAnchor.MiddleCenter, new Color(0.18f, 0.12f, 0.08f));
            SetRect(title.rectTransform, new Vector2(0.12f, 0.84f), new Vector2(0.88f, 0.96f), Vector2.zero, Vector2.zero);

            var saveButton = CreateButton(panel.transform, "保存游戏");
            SetRect(saveButton.GetComponent<RectTransform>(), new Vector2(0.17f, 0.66f), new Vector2(0.83f, 0.77f), Vector2.zero, Vector2.zero);
            saveButton.interactable = false;

            var loadButton = CreateButton(panel.transform, "加载存档");
            SetRect(loadButton.GetComponent<RectTransform>(), new Vector2(0.17f, 0.5f), new Vector2(0.83f, 0.61f), Vector2.zero, Vector2.zero);
            loadButton.interactable = false;

            var homeButton = CreateButton(panel.transform, "回到主页");
            SetRect(homeButton.GetComponent<RectTransform>(), new Vector2(0.17f, 0.34f), new Vector2(0.83f, 0.45f), Vector2.zero, Vector2.zero);
            homeButton.onClick.AddListener(ReturnToMainMenu);

            var quitButton = CreateButton(panel.transform, "退出游戏");
            SetRect(quitButton.GetComponent<RectTransform>(), new Vector2(0.17f, 0.18f), new Vector2(0.83f, 0.29f), Vector2.zero, Vector2.zero);
            quitButton.interactable = false;

            var closeButton = CreateButton(panel.transform, "关闭");
            SetRect(closeButton.GetComponent<RectTransform>(), new Vector2(0.31f, 0.045f), new Vector2(0.69f, 0.13f), Vector2.zero, Vector2.zero);
            closeButton.onClick.AddListener(() =>
            {
                if (Application.isPlaying)
                    Destroy(modalRoot);
                else
                    DestroyImmediate(modalRoot);
            });
        }

        private void ToggleGameMenuFromKeyboard()
        {
            if (isResolvingTurn)
                return;

            var canvas = GetComponentInChildren<Canvas>();
            if (canvas == null)
                return;

            if (CloseExistingGameMenu(canvas.transform))
                return;

            ShowGameMenu(canvas.transform);
        }

        private void ReturnToMainMenu()
        {
            StopAllCoroutines();
            SceneManager.LoadScene(MainMenuSceneName);
        }

        private void ShowRunMapPreview(Transform parent)
        {
            CloseExistingRunMapPreview(parent);

            var modalRoot = new GameObject("Run Map Preview Modal", typeof(RectTransform));
            modalRoot.transform.SetParent(parent, false);
            var modalRect = modalRoot.GetComponent<RectTransform>();
            SetRect(modalRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var dimmer = CreateImage(modalRoot.transform, "Dim Background", new Color(0f, 0f, 0f, 0.46f));
            SetRect(dimmer.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var panel = CreateImage(modalRoot.transform, "Run Map Preview Panel", new Color(0.88f, 0.75f, 0.48f));
            panel.sprite = GetRoundedButtonSprite();
            panel.type = Image.Type.Sliced;
            SetRect(panel.rectTransform, new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.86f), Vector2.zero, Vector2.zero);

            var outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.22f, 0.14f, 0.07f);
            outline.effectDistance = new Vector2(4f, -4f);

            var title = CreateText(panel.transform, "战线", 34, TextAnchor.MiddleCenter, new Color(0.18f, 0.1f, 0.05f));
            title.fontStyle = FontStyle.Bold;
            SetRect(title.rectTransform, new Vector2(0.34f, 0.88f), new Vector2(0.66f, 0.97f), Vector2.zero, Vector2.zero);

            var mapArea = CreateImage(panel.transform, "Map Area", new Color(0.72f, 0.58f, 0.34f, 0.35f));
            mapArea.sprite = GetRoundedButtonSprite();
            mapArea.type = Image.Type.Sliced;
            SetRect(mapArea.rectTransform, new Vector2(0.035f, 0.2f), new Vector2(0.965f, 0.86f), Vector2.zero, Vector2.zero);

            var viewport = CreateImage(mapArea.transform, "Viewport", Color.clear);
            viewport.gameObject.AddComponent<RectMask2D>();
            SetRect(viewport.rectTransform, new Vector2(0.025f, 0.035f), new Vector2(0.95f, 0.965f), Vector2.zero, Vector2.zero);

            var contentObject = new GameObject("Map Content", typeof(RectTransform));
            contentObject.transform.SetParent(viewport.transform, false);
            var content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, RunMapPreviewContentHeight);

            BuildRunMapPreviewLines(content);
            BuildRunMapPreviewNodes(content);

            var scrollRect = mapArea.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 36f;
            scrollRect.viewport = viewport.rectTransform;
            scrollRect.content = content;
            scrollRect.verticalScrollbar = CreateRunMapPreviewScrollbar(mapArea.transform);
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalNormalizedPosition = 1f;

            var routeText = "路线：尚未出征";
            if (RunSession.HasActiveRun && RunSession.Current.RouteHistory.Count > 0)
                routeText = "路线：" + string.Join("  /  ", RunSession.Current.RouteHistory.ToArray());

            var route = CreateText(panel.transform, routeText, 19, TextAnchor.MiddleLeft, new Color(0.22f, 0.14f, 0.08f));
            SetRect(route.rectTransform, new Vector2(0.045f, 0.095f), new Vector2(0.72f, 0.17f), Vector2.zero, Vector2.zero);

            var closeButton = CreateButton(panel.transform, "返回");
            SetRect(closeButton.GetComponent<RectTransform>(), new Vector2(0.78f, 0.08f), new Vector2(0.94f, 0.17f), Vector2.zero, Vector2.zero);
            closeButton.onClick.AddListener(() =>
            {
                if (Application.isPlaying)
                    Destroy(modalRoot);
                else
                    DestroyImmediate(modalRoot);
            });
        }

        private void BuildRunMapPreviewLines(Transform parent)
        {
            foreach (var node in RunSession.Nodes)
            {
                foreach (var nextNodeId in node.NextNodeIds)
                {
                    var next = RunSession.GetNode(nextNodeId);
                    if (next == null)
                        continue;

                    var color = GetRunMapPreviewRouteColor(node.NodeId, next.NodeId);
                    CreateRunMapPreviewLine(parent, node.Position, next.Position, color);
                }
            }
        }

        private Color GetRunMapPreviewRouteColor(string fromNodeId, string toNodeId)
        {
            if (!RunSession.HasActiveRun)
                return new Color(0.24f, 0.16f, 0.08f, 0.24f);

            var fromState = RunSession.GetNodeState(fromNodeId);
            var toState = RunSession.GetNodeState(toNodeId);
            if (fromState == RunMapNodeState.Completed && toState != RunMapNodeState.Locked)
                return new Color(0.35f, 0.18f, 0.08f, 0.9f);

            return new Color(0.24f, 0.16f, 0.08f, 0.24f);
        }

        private void CreateRunMapPreviewLine(Transform parent, Vector2 from, Vector2 to, Color color)
        {
            var line = CreateImage(parent, "Route", color);
            line.raycastTarget = false;
            var rect = line.rectTransform;
            rect.anchorMin = new Vector2((from.x + to.x) * 0.5f, (from.y + to.y) * 0.5f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(0.5f, 0.5f);

            var delta = to - from;
            var length = Mathf.Sqrt(delta.x * delta.x * RunMapPreviewContentWidth * RunMapPreviewContentWidth + delta.y * delta.y * RunMapPreviewContentHeight * RunMapPreviewContentHeight);
            rect.sizeDelta = new Vector2(length, 7f);
            rect.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(delta.y * RunMapPreviewContentHeight, delta.x * RunMapPreviewContentWidth) * Mathf.Rad2Deg);
        }

        private void BuildRunMapPreviewNodes(Transform parent)
        {
            foreach (var node in RunSession.Nodes)
                BuildRunMapPreviewNode(parent, node);
        }

        private void BuildRunMapPreviewNode(Transform parent, RunMapNodeDefinition node)
        {
            var state = RunSession.GetNodeState(node.NodeId);
            var isCampaign = node.NodeType == RunMapNodeType.Campaign;
            var isCurrent = RunSession.HasActiveRun && RunSession.Current.CurrentNodeId == node.NodeId;
            var color = isCampaign ? new Color(0.75f, 0.22f, 0.12f) : new Color(0.24f, 0.44f, 0.32f);

            if (state == RunMapNodeState.Completed)
                color = new Color(0.55f, 0.54f, 0.44f);
            else if (state == RunMapNodeState.Locked)
                color = new Color(0.2f, 0.2f, 0.2f, 0.4f);

            var root = CreateImage(parent, node.Title, color);
            root.sprite = GetRoundedButtonSprite();
            root.type = Image.Type.Sliced;
            root.rectTransform.anchorMin = node.Position;
            root.rectTransform.anchorMax = node.Position;
            root.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            root.rectTransform.sizeDelta = isCampaign ? new Vector2(166f, 86f) : new Vector2(136f, 70f);
            root.rectTransform.anchoredPosition = Vector2.zero;
            root.raycastTarget = false;

            var border = root.gameObject.AddComponent<Outline>();
            border.effectColor = isCurrent ? new Color(1f, 0.86f, 0.36f) : new Color(0.18f, 0.12f, 0.08f, 0.65f);
            border.effectDistance = isCurrent ? new Vector2(5f, -5f) : new Vector2(2f, -2f);

            var title = CreateText(root.transform, node.Title, isCampaign ? 21 : 18, TextAnchor.MiddleCenter, Color.white);
            title.fontStyle = FontStyle.Bold;
            SetRect(title.rectTransform, new Vector2(0.06f, 0.42f), new Vector2(0.94f, 0.9f), Vector2.zero, Vector2.zero);

            var label = isCurrent ? "当前位置" : GetRunMapPreviewNodeLabel(state);
            var subtitle = CreateText(root.transform, label, 14, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.68f));
            SetRect(subtitle.rectTransform, new Vector2(0.06f, 0.1f), new Vector2(0.94f, 0.42f), Vector2.zero, Vector2.zero);
        }

        private static string GetRunMapPreviewNodeLabel(RunMapNodeState state)
        {
            if (state == RunMapNodeState.Completed)
                return "已完成";

            if (state == RunMapNodeState.Available)
                return "可前往";

            return "未解锁";
        }

        private Scrollbar CreateRunMapPreviewScrollbar(Transform parent)
        {
            var scrollbarRoot = CreateImage(parent, "Map Preview Scrollbar", new Color(0.32f, 0.22f, 0.14f, 0.2f));
            SetRect(scrollbarRoot.rectTransform, new Vector2(0.974f, 0.04f), new Vector2(0.985f, 0.96f), Vector2.zero, Vector2.zero);

            var slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
            slidingArea.transform.SetParent(scrollbarRoot.transform, false);
            var slidingRect = slidingArea.GetComponent<RectTransform>();
            SetRect(slidingRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var handle = CreateImage(slidingArea.transform, "Handle", new Color(0.54f, 0.36f, 0.2f, 0.82f));
            SetRect(handle.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var scrollbar = scrollbarRoot.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.targetGraphic = handle;
            scrollbar.handleRect = handle.rectTransform;
            return scrollbar;
        }

        private static bool CloseExistingRunMapPreview(Transform parent)
        {
            if (parent == null)
                return false;

            var closed = false;
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (child == null || child.name != "Run Map Preview Modal")
                    continue;

                if (Application.isPlaying)
                    Object.Destroy(child.gameObject);
                else
                    Object.DestroyImmediate(child.gameObject);

                closed = true;
            }

            return closed;
        }

        private static bool CloseExistingGameMenu(Transform parent)
        {
            if (parent == null)
                return false;

            var closed = false;
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (child == null || child.name != "Game Menu Modal")
                    continue;

                if (Application.isPlaying)
                    Object.Destroy(child.gameObject);
                else
                    Object.DestroyImmediate(child.gameObject);

                closed = true;
            }

            return closed;
        }
    }
}
