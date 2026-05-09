using System.Collections.Generic;
using System.Linq;
using UnifyCountry.Config;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UnifyCountry.UI
{
    [ExecuteAlways]
    public sealed class CardCollectionUi : MonoBehaviour
    {
        [System.Serializable]
        private sealed class CardPortraitEntry
        {
            public string cardId;
            public Sprite portrait;
        }

        private enum CardFilter
        {
            All,
            Player,
            Enemy,
            Neutral,
            Unit,
            Skill
        }

        private const string MainMenuSceneName = "SCN_MainMenu";
        private static readonly Vector2 ReferenceResolution = new Vector2(1600f, 900f);
        private static readonly Color BackgroundColor = new Color(0.18f, 0.28f, 0.25f);
        private static readonly Color PanelColor = new Color(0.94f, 0.84f, 0.62f);
        private static readonly Color PanelInnerColor = new Color(1f, 0.94f, 0.76f);
        private static readonly Color InkColor = new Color(0.17f, 0.1f, 0.06f);
        private static readonly Color MutedInkColor = new Color(0.36f, 0.24f, 0.14f);
        private static readonly Color PlayerColor = new Color(0.72f, 0.88f, 0.68f);
        private static readonly Color EnemyColor = new Color(0.94f, 0.55f, 0.5f);
        private static readonly Color NeutralColor = new Color(0.77f, 0.82f, 0.88f);
        private static readonly Color SkillColor = new Color(0.73f, 0.78f, 0.95f);

        [Header("Config")]
        [SerializeField] private TextAsset cardsCsv;
        [SerializeField] private TextAsset unitsCsv;
        [SerializeField] private TextAsset effectsCsv;

        [Header("Style")]
        [SerializeField] private Font uiFont;

        [Header("Card Art")]
        [SerializeField] private List<CardPortraitEntry> cardPortraits = new List<CardPortraitEntry>();

        private readonly List<CardRecord> cards = new List<CardRecord>();
        private readonly Dictionary<string, Sprite> portraitMap = new Dictionary<string, Sprite>();
        private Sprite roundedSprite;
        private RectTransform filterRoot;
        private RectTransform gridRoot;
        private Text countText;
        private CardFilter currentFilter = CardFilter.All;

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

        [ContextMenu("Rebuild Card Collection")]
        public void Build()
        {
            EnsureFont();
            ClearChildren(transform);
            EnsureCamera();
            EnsureEventSystem();
            LoadData();

            var canvas = CreateCanvas();
            BuildBackground(canvas.transform);
            BuildPanel(canvas.transform);
            RebuildCards();
        }

        private void EnsureFont()
        {
            if (uiFont == null)
                uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private void LoadData()
        {
            cards.Clear();
            cards.AddRange(PrototypeCsvDatabase.LoadCards(cardsCsv, unitsCsv, effectsCsv));

            portraitMap.Clear();
            foreach (var entry in cardPortraits)
            {
                if (entry != null && !string.IsNullOrWhiteSpace(entry.cardId) && entry.portrait != null)
                    portraitMap[entry.cardId] = entry.portrait;
            }
        }

        private void BuildBackground(Transform parent)
        {
            var background = CreateImage(parent, "Background", BackgroundColor);
            SetStretch(background.rectTransform);

            var sky = CreateImage(parent, "Sky Band", new Color(0.43f, 0.63f, 0.66f));
            SetRect(sky.rectTransform, new Vector2(0f, 0.73f), Vector2.one);

            var ground = CreateImage(parent, "Ground Band", new Color(0.22f, 0.35f, 0.22f));
            SetRect(ground.rectTransform, Vector2.zero, new Vector2(1f, 0.2f));
        }

        private void BuildPanel(Transform parent)
        {
            var panel = CreateImage(parent, "Card Collection Panel", PanelColor);
            panel.sprite = GetRoundedSprite();
            panel.type = Image.Type.Sliced;
            SetRect(panel.rectTransform, new Vector2(0.055f, 0.055f), new Vector2(0.945f, 0.945f));
            AddOutline(panel.gameObject, new Color(0.2f, 0.12f, 0.06f), new Vector2(4f, -4f));

            var title = CreateText(panel.transform, "\u5367\u864e\u85cf\u9f99", 42, TextAnchor.MiddleLeft, InkColor);
            title.fontStyle = FontStyle.Bold;
            SetRect(title.rectTransform, new Vector2(0.04f, 0.905f), new Vector2(0.28f, 0.985f));

            countText = CreateText(panel.transform, string.Empty, 22, TextAnchor.MiddleLeft, MutedInkColor);
            SetRect(countText.rectTransform, new Vector2(0.29f, 0.91f), new Vector2(0.47f, 0.975f));

            var backButton = CreateButton(panel.transform, "\u8fd4\u56de", new Color(0.78f, 0.23f, 0.16f), LoadMainMenu);
            SetRect(backButton.GetComponent<RectTransform>(), new Vector2(0.89f, 0.905f), new Vector2(0.965f, 0.975f));

            BuildFilters(panel.transform);
            BuildScrollGrid(panel.transform);
        }

        private void BuildFilters(Transform parent)
        {
            if (filterRoot == null)
            {
                var filterObject = new GameObject("Filters", typeof(RectTransform));
                filterObject.transform.SetParent(parent, false);
                filterRoot = filterObject.GetComponent<RectTransform>();
                SetRect(filterRoot, new Vector2(0.04f, 0.815f), new Vector2(0.62f, 0.885f));
            }

            ClearChildren(filterRoot);

            var filters = new[]
            {
                (CardFilter.All, "\u5168\u90e8"),
                (CardFilter.Player, "\u6211\u65b9"),
                (CardFilter.Enemy, "\u654c\u65b9"),
                (CardFilter.Neutral, "\u4e2d\u7acb"),
                (CardFilter.Unit, "\u5355\u4f4d"),
                (CardFilter.Skill, "\u8ba1\u7b56")
            };

            for (var i = 0; i < filters.Length; i++)
            {
                var filter = filters[i];
                var xMin = i * 0.164f;
                var button = CreateFilterButton(filterRoot, filter.Item2, filter.Item1, () =>
                {
                    currentFilter = filter.Item1;
                    BuildFilters(filterRoot.parent);
                    RebuildCards();
                });
                SetRect(button.GetComponent<RectTransform>(), new Vector2(xMin, 0f), new Vector2(xMin + 0.136f, 1f));
            }
        }

        private Button CreateFilterButton(Transform parent, string label, CardFilter filter, UnityEngine.Events.UnityAction onClick)
        {
            var selected = filter == currentFilter;
            var button = CreateButton(parent, label, GetFilterButtonColor(filter), onClick);

            if (selected)
            {
                AddOutline(button.gameObject, new Color(1f, 0.86f, 0.42f), new Vector2(4f, -4f));

                var marker = CreateImage(button.transform, "Selected Marker", new Color(1f, 0.86f, 0.42f));
                marker.raycastTarget = false;
                SetRect(marker.rectTransform, new Vector2(0.16f, 0.06f), new Vector2(0.84f, 0.14f));
            }

            return button;
        }

        private void BuildScrollGrid(Transform parent)
        {
            var viewportImage = CreateImage(parent, "Card Viewport", PanelInnerColor);
            viewportImage.sprite = GetRoundedSprite();
            viewportImage.type = Image.Type.Sliced;
            SetRect(viewportImage.rectTransform, new Vector2(0.035f, 0.045f), new Vector2(0.965f, 0.79f));
            AddOutline(viewportImage.gameObject, new Color(0.36f, 0.23f, 0.12f, 0.9f), new Vector2(2f, -2f));

            var mask = viewportImage.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            var contentObject = new GameObject("Card Grid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewportImage.transform, false);
            gridRoot = contentObject.GetComponent<RectTransform>();
            gridRoot.anchorMin = new Vector2(0f, 1f);
            gridRoot.anchorMax = new Vector2(1f, 1f);
            gridRoot.pivot = new Vector2(0.5f, 1f);
            gridRoot.anchoredPosition = Vector2.zero;
            gridRoot.sizeDelta = Vector2.zero;

            var grid = contentObject.GetComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(24, 24, 24, 24);
            grid.cellSize = new Vector2(215f, 318f);
            grid.spacing = new Vector2(20f, 20f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;

            var fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = viewportImage.gameObject.AddComponent<ScrollRect>();
            scroll.content = gridRoot;
            scroll.viewport = viewportImage.rectTransform;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 32f;
        }

        private void RebuildCards()
        {
            if (gridRoot == null)
                return;

            ClearChildren(gridRoot);

            var visibleCards = cards.Where(MatchesFilter)
                .OrderBy(card => card.Camp)
                .ThenBy(card => card.CardType)
                .ThenBy(card => card.Cost)
                .ThenBy(card => card.CardId)
                .ToList();

            if (countText != null)
                countText.text = string.Format("\u5171 {0} \u5f20\u724c", visibleCards.Count);

            foreach (var card in visibleCards)
                CreateCardView(gridRoot, card);
        }

        private bool MatchesFilter(CardRecord card)
        {
            switch (currentFilter)
            {
                case CardFilter.Player:
                    return card.Camp == CardCamp.Player;
                case CardFilter.Enemy:
                    return card.Camp == CardCamp.Enemy;
                case CardFilter.Neutral:
                    return card.Camp == CardCamp.Neutral;
                case CardFilter.Unit:
                    return card.CardType == CardType.Unit;
                case CardFilter.Skill:
                    return card.CardType == CardType.Skill;
                default:
                    return true;
            }
        }

        private void CreateCardView(Transform parent, CardRecord card)
        {
            var root = CreateImage(parent, card.CardId, GetCardColor(card));
            root.sprite = GetRoundedSprite();
            root.type = Image.Type.Sliced;
            AddOutline(root.gameObject, new Color(0.17f, 0.1f, 0.06f), new Vector2(3f, -3f));

            var cost = CreateBadge(root.transform, card.Cost.ToString(), new Color(0.25f, 0.56f, 0.88f));
            SetRect(cost, new Vector2(0.045f, 0.83f), new Vector2(0.24f, 0.965f));

            var name = CreateText(root.transform, card.CardName, 22, TextAnchor.MiddleCenter, InkColor);
            name.fontStyle = FontStyle.Bold;
            SetRect(name.rectTransform, new Vector2(0.24f, 0.84f), new Vector2(0.96f, 0.965f));

            var portrait = CreateImage(root.transform, "Portrait", new Color(0.98f, 0.88f, 0.62f));
            SetRect(portrait.rectTransform, new Vector2(0.08f, 0.48f), new Vector2(0.92f, 0.81f));
            AddOutline(portrait.gameObject, new Color(0.48f, 0.33f, 0.18f, 0.75f), new Vector2(1.5f, -1.5f));

            if (TryGetPortrait(card, out var portraitSprite))
            {
                portrait.sprite = portraitSprite;
                portrait.color = Color.white;
                portrait.preserveAspect = true;
            }
            else
            {
                var fallback = CreateText(portrait.transform, GetFallbackFace(card), 38, TextAnchor.MiddleCenter, Color.white);
                fallback.fontStyle = FontStyle.Bold;
                SetStretch(fallback.rectTransform);
            }

            var tags = CreateText(root.transform, GetTagsLine(card), 16, TextAnchor.MiddleCenter, MutedInkColor);
            SetRect(tags.rectTransform, new Vector2(0.08f, 0.39f), new Vector2(0.92f, 0.47f));

            var detail = CreateText(root.transform, GetDetailLine(card), 20, TextAnchor.MiddleCenter, InkColor);
            detail.fontStyle = FontStyle.Bold;
            SetRect(detail.rectTransform, new Vector2(0.08f, 0.305f), new Vector2(0.92f, 0.39f));

            var description = CreateDescriptionScroll(root.transform, GetEffectLine(card));
            SetRect(description, new Vector2(0.08f, 0.07f), new Vector2(0.92f, 0.29f));
        }

        private RectTransform CreateDescriptionScroll(Transform parent, string content)
        {
            var viewportObject = new GameObject("Description Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(ScrollRect));
            viewportObject.transform.SetParent(parent, false);
            var viewport = viewportObject.GetComponent<RectTransform>();

            var text = CreateText(viewportObject.transform, content, 15, TextAnchor.UpperLeft, InkColor);
            text.resizeTextForBestFit = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.lineSpacing = 1.08f;
            text.raycastTarget = true;

            var textRect = text.rectTransform;
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.offsetMin = new Vector2(0f, 0f);
            textRect.offsetMax = new Vector2(-10f, 0f);

            var fitter = text.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollbar = CreateVerticalScrollbar(viewportObject.transform);
            SetRect(scrollbar.GetComponent<RectTransform>(), new Vector2(0.96f, 0f), Vector2.one);

            var scroll = viewportObject.GetComponent<ScrollRect>();
            scroll.content = textRect;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 18f;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            return viewport;
        }

        private Scrollbar CreateVerticalScrollbar(Transform parent)
        {
            var track = CreateImage(parent, "Description Scrollbar", new Color(0.24f, 0.15f, 0.08f, 0.18f));
            track.raycastTarget = true;

            var slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
            slidingArea.transform.SetParent(track.transform, false);
            var slidingRect = slidingArea.GetComponent<RectTransform>();
            SetStretch(slidingRect);

            var handle = CreateImage(slidingArea.transform, "Handle", new Color(0.36f, 0.22f, 0.12f, 0.72f));
            handle.raycastTarget = true;
            SetStretch(handle.rectTransform);

            var scrollbar = track.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.targetGraphic = handle;
            scrollbar.handleRect = handle.rectTransform;
            scrollbar.size = 0.35f;

            return scrollbar;
        }

        private RectTransform CreateBadge(Transform parent, string value, Color color)
        {
            var badge = CreateImage(parent, "Cost", color);
            badge.sprite = GetRoundedSprite();
            badge.type = Image.Type.Sliced;
            AddOutline(badge.gameObject, new Color(0.11f, 0.09f, 0.08f), new Vector2(1.5f, -1.5f));

            var text = CreateText(badge.transform, value, 22, TextAnchor.MiddleCenter, Color.white);
            text.fontStyle = FontStyle.Bold;
            SetStretch(text.rectTransform);

            return badge.rectTransform;
        }

        private string GetTagsLine(CardRecord card)
        {
            return string.Format("{0} / {1}", GetCampLabel(card.Camp), GetCardTypeLabel(card.CardType));
        }

        private string GetDetailLine(CardRecord card)
        {
            if (card.CardType == CardType.Unit)
                return string.Format("\u653b {0}   \u8840 {1}", card.Attack, card.Hp);

            return GetCardTypeLabel(card.CardType);
        }

        private string GetEffectLine(CardRecord card)
        {
            if (card.CardType == CardType.Unit && card.Camp == CardCamp.Neutral)
                return string.Empty;

            if (card.Effects.Count == 0)
                return card.DescriptionKey;

            var effect = card.Effects[0];
            if (card.CardType == CardType.Skill)
                return effect.Description;

            return string.Format("{0}: {1}", effect.EffectName, effect.Description);
        }

        private Color GetCardColor(CardRecord card)
        {
            if (card.CardType == CardType.Skill)
                return SkillColor;

            switch (card.Camp)
            {
                case CardCamp.Player:
                    return PlayerColor;
                case CardCamp.Enemy:
                    return EnemyColor;
                default:
                    return NeutralColor;
            }
        }

        private Color GetFilterButtonColor(CardFilter filter)
        {
            return filter == currentFilter ? new Color(0.84f, 0.36f, 0.12f) : new Color(0.62f, 0.42f, 0.2f);
        }

        private static string GetCampLabel(CardCamp camp)
        {
            switch (camp)
            {
                case CardCamp.Player:
                    return "\u6211\u65b9";
                case CardCamp.Enemy:
                    return "\u654c\u65b9";
                case CardCamp.Neutral:
                    return "\u4e2d\u7acb";
                default:
                    return "\u65e0\u9635\u8425";
            }
        }

        private static string GetCardTypeLabel(CardType type)
        {
            switch (type)
            {
                case CardType.Unit:
                    return "\u5355\u4f4d";
                case CardType.Skill:
                    return "\u8ba1\u7b56";
                case CardType.Equipment:
                    return "\u88c5\u5907";
                case CardType.Power:
                    return "\u80fd\u529b";
                case CardType.Event:
                    return "\u4e8b\u4ef6";
                default:
                    return "\u5361\u724c";
            }
        }

        private string GetFallbackFace(CardRecord card)
        {
            return string.IsNullOrEmpty(card.CardName) ? "?" : card.CardName.Substring(0, 1);
        }

        private bool TryGetPortrait(CardRecord card, out Sprite portrait)
        {
            if (portraitMap.TryGetValue(card.CardId, out portrait))
                return true;

#if UNITY_EDITOR
            portrait = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                $"Assets/_Project/Art/Cards/Portraits/{(string.IsNullOrWhiteSpace(card.ArtId) ? card.CardId : card.ArtId)}.png");
            return portrait != null;
#else
            portrait = null;
            return false;
#endif
        }

        private Button CreateButton(Transform parent, string label, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var image = CreateImage(parent, label, color);
            image.sprite = GetRoundedSprite();
            image.type = Image.Type.Sliced;
            AddOutline(image.gameObject, new Color(0.15f, 0.08f, 0.04f), new Vector2(2f, -2f));

            var button = image.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = color,
                highlightedColor = Color.Lerp(color, Color.white, 0.18f),
                pressedColor = Color.Lerp(color, Color.black, 0.18f),
                selectedColor = color,
                disabledColor = new Color(0.48f, 0.44f, 0.4f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };
            button.targetGraphic = image;
            if (onClick != null)
                button.onClick.AddListener(onClick);

            var text = CreateText(image.transform, label, 21, TextAnchor.MiddleCenter, Color.white);
            text.fontStyle = FontStyle.Bold;
            SetStretch(text.rectTransform);

            return button;
        }

        private void LoadMainMenu()
        {
            SceneManager.LoadScene(MainMenuSceneName);
        }

        private Canvas CreateCanvas()
        {
            var canvasObject = new GameObject("Card Collection Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
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
            sceneCamera.backgroundColor = BackgroundColor;
            sceneCamera.orthographic = true;
            sceneCamera.orthographicSize = 5f;
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
                return;

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystem.transform.SetParent(transform, false);
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
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(10, size - 8);
            text.resizeTextMaxSize = size;
            return text;
        }

        private void AddOutline(GameObject target, Color color, Vector2 distance)
        {
            var outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
        }

        private Sprite GetRoundedSprite()
        {
            if (roundedSprite != null)
                return roundedSprite;

            const int size = 64;
            const int radius = 10;
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
                    var alpha = Mathf.Clamp01(radius + 1.5f - distance);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            roundedSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            return roundedSprite;
        }

        private void ClearChildren(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        private static void SetStretch(RectTransform rect)
        {
            SetRect(rect, Vector2.zero, Vector2.one);
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
