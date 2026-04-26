using System.Collections.Generic;
using System.Linq;
using UnifyCountry.Config;
using UnityEngine;
using UnityEngine.UI;

namespace UnifyCountry.UI
{
    public sealed class PrototypeBattleUi : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private TextAsset cardsCsv;
        [SerializeField] private TextAsset startingDeckCsv;
        [SerializeField] private TextAsset wavesCsv;

        [Header("Style")]
        [SerializeField] private Font uiFont;
        [SerializeField] private Vector2 referenceResolution = new Vector2(1600f, 900f);

        private readonly Color backgroundColor = new Color(0.94f, 0.89f, 0.73f);
        private readonly Color playerPanelColor = new Color(0.76f, 0.92f, 0.67f);
        private readonly Color enemyPanelColor = new Color(0.96f, 0.68f, 0.58f);
        private readonly Color handPanelColor = new Color(0.99f, 0.94f, 0.72f);
        private readonly Color heroCardColor = new Color(1f, 0.82f, 0.36f);
        private readonly Color soldierCardColor = new Color(0.66f, 0.88f, 1f);
        private readonly Color enemyCardColor = new Color(1f, 0.56f, 0.5f);

        [ContextMenu("Rebuild Preview UI")]
        public void Rebuild()
        {
            ClearChildren();

            if (uiFont == null)
                uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var cards = PrototypeCsvDatabase.LoadCards(cardsCsv);
            var cardMap = cards.ToDictionary(card => card.CardId);
            var startingDeck = PrototypeCsvDatabase.LoadStartingDeck(startingDeckCsv);
            var waveSlots = PrototypeCsvDatabase.LoadWaveSlots(wavesCsv);

            var canvas = CreateCanvas();
            CreateBackground(canvas.transform);

            var title = CreateText(canvas.transform, "三国卡牌战线 - 战斗原型", 38, TextAnchor.MiddleCenter, Color.white);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(760f, 56f));

            var playerPanel = CreatePanel(canvas.transform, "友方阵地", playerPanelColor);
            SetRect(playerPanel, new Vector2(0.02f, 0.28f), new Vector2(0.49f, 0.88f), Vector2.zero, Vector2.zero);
            BuildBoardSlots(playerPanel.transform, true, cards.Where(card => card.Camp == CardCamp.Player).Take(3).ToList());

            var enemyPanel = CreatePanel(canvas.transform, "敌方波次", enemyPanelColor);
            SetRect(enemyPanel, new Vector2(0.51f, 0.28f), new Vector2(0.98f, 0.88f), Vector2.zero, Vector2.zero);
            BuildEnemyWaves(enemyPanel.transform, waveSlots, cardMap);

            var handPanel = CreatePanel(canvas.transform, "第一回合手牌 / 初始牌库预览", handPanelColor);
            SetRect(handPanel, new Vector2(0.02f, 0.03f), new Vector2(0.82f, 0.25f), Vector2.zero, Vector2.zero);
            BuildHand(handPanel.transform, startingDeck, cardMap);

            var endTurnButton = CreateButton(canvas.transform, "结束回合");
            SetRect(endTurnButton.GetComponent<RectTransform>(), new Vector2(0.84f, 0.07f), new Vector2(0.98f, 0.21f), Vector2.zero, Vector2.zero);

            var hint = CreateText(canvas.transform, "准备阶段：英雄卡首回合必定到手。拖拽上阵会在下一版接入。", 20, TextAnchor.MiddleCenter, new Color(0.22f, 0.16f, 0.1f));
            SetRect(hint.rectTransform, new Vector2(0.18f, 0.895f), new Vector2(0.82f, 0.945f), Vector2.zero, Vector2.zero);
        }

        private void Awake()
        {
            if (transform.childCount == 0)
                Rebuild();
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

        private void BuildBoardSlots(Transform parent, bool playerSide, List<CardRecord> previewUnits)
        {
            for (var i = 0; i < 5; i++)
            {
                var slot = CreateImage(parent, $"Slot {i + 1}", new Color(1f, 1f, 1f, 0.38f));
                SetRect(slot.rectTransform, new Vector2(0.05f + i * 0.18f, 0.18f), new Vector2(0.19f + i * 0.18f, 0.72f), Vector2.zero, Vector2.zero);
                slot.gameObject.AddComponent<Outline>().effectColor = new Color(0.35f, 0.25f, 0.16f);

                var order = playerSide ? 5 - i : i + 1;
                var label = CreateText(slot.transform, playerSide ? $"承伤 {order}" : $"敌位 {order}", 18, TextAnchor.LowerCenter, new Color(0.23f, 0.16f, 0.1f));
                SetRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(0f, 8f), Vector2.zero);
            }

            for (var i = 0; i < previewUnits.Count; i++)
            {
                var index = Mathf.Min(i, 4);
                var unit = CreateUnitToken(parent, previewUnits[i], false);
                SetRect(unit, new Vector2(0.065f + index * 0.18f, 0.3f), new Vector2(0.175f + index * 0.18f, 0.65f), Vector2.zero, Vector2.zero);
            }
        }

        private void BuildEnemyWaves(Transform parent, List<List<string>> waveSlots, Dictionary<string, CardRecord> cardMap)
        {
            for (var i = 0; i < waveSlots.Count; i++)
            {
                var row = CreateImage(parent, $"Wave {i + 1}", new Color(1f, 1f, 1f, 0.28f));
                SetRect(row.rectTransform, new Vector2(0.06f, 0.62f - i * 0.24f), new Vector2(0.94f, 0.81f - i * 0.24f), Vector2.zero, Vector2.zero);

                var label = CreateText(row.transform, $"第 {i + 1} 波", 22, TextAnchor.MiddleLeft, new Color(0.22f, 0.12f, 0.1f));
                SetRect(label.rectTransform, new Vector2(0.03f, 0f), new Vector2(0.28f, 1f), Vector2.zero, Vector2.zero);

                for (var j = 0; j < waveSlots[i].Count; j++)
                {
                    if (!cardMap.TryGetValue(waveSlots[i][j], out var card))
                        continue;

                    var token = CreateUnitToken(row.transform, card, true);
                    SetRect(token, new Vector2(0.36f + j * 0.25f, 0.12f), new Vector2(0.56f + j * 0.25f, 0.88f), Vector2.zero, Vector2.zero);
                }
            }
        }

        private void BuildHand(Transform parent, Dictionary<string, int> startingDeck, Dictionary<string, CardRecord> cardMap)
        {
            var handCards = new List<CardRecord>();
            foreach (var entry in startingDeck)
            {
                if (!cardMap.TryGetValue(entry.Key, out var card))
                    continue;

                if (card.UnitType == UnitType.Hero)
                    handCards.Add(card);
            }

            foreach (var entry in startingDeck)
            {
                if (!cardMap.TryGetValue(entry.Key, out var card) || card.UnitType == UnitType.Hero)
                    continue;

                handCards.Add(card);
            }

            for (var i = 0; i < Mathf.Min(handCards.Count, 7); i++)
            {
                var cardView = CreateCard(parent, handCards[i], startingDeck[handCards[i].CardId]);
                SetRect(cardView, new Vector2(0.02f + i * 0.135f, 0.12f), new Vector2(0.13f + i * 0.135f, 0.78f), Vector2.zero, Vector2.zero);
            }
        }

        private RectTransform CreateCard(Transform parent, CardRecord card, int copies)
        {
            var color = card.UnitType == UnitType.Hero ? heroCardColor : soldierCardColor;
            var root = CreateImage(parent, card.CardName, color);
            root.gameObject.AddComponent<Outline>().effectColor = new Color(0.22f, 0.16f, 0.1f);

            var cost = CreateBadge(root.transform, card.Cost.ToString(), new Color(0.25f, 0.6f, 0.95f));
            SetRect(cost, new Vector2(0.03f, 0.72f), new Vector2(0.25f, 0.96f), Vector2.zero, Vector2.zero);

            var name = CreateText(root.transform, card.CardName, 22, TextAnchor.MiddleCenter, new Color(0.15f, 0.09f, 0.05f));
            SetRect(name.rectTransform, new Vector2(0.2f, 0.72f), new Vector2(0.96f, 0.96f), Vector2.zero, Vector2.zero);

            var portrait = CreateImage(root.transform, "Portrait", card.Camp == CardCamp.Enemy ? enemyCardColor : new Color(1f, 0.96f, 0.78f));
            SetRect(portrait.rectTransform, new Vector2(0.14f, 0.34f), new Vector2(0.86f, 0.69f), Vector2.zero, Vector2.zero);

            var face = CreateText(portrait.transform, string.IsNullOrEmpty(card.CardName) ? "?" : card.CardName.Substring(0, 1), 34, TextAnchor.MiddleCenter, Color.white);
            SetRect(face.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var stats = CreateText(root.transform, $"攻 {card.Attack}   血 {card.Hp}", 20, TextAnchor.MiddleCenter, new Color(0.2f, 0.12f, 0.08f));
            SetRect(stats.rectTransform, new Vector2(0.05f, 0.12f), new Vector2(0.95f, 0.31f), Vector2.zero, Vector2.zero);

            var copiesLabel = CreateText(root.transform, card.UnitType == UnitType.Hero ? "唯一" : $"x{copies}", 16, TextAnchor.MiddleCenter, Color.white);
            copiesLabel.color = Color.white;
            SetRect(copiesLabel.rectTransform, new Vector2(0.68f, 0.02f), new Vector2(0.96f, 0.16f), Vector2.zero, Vector2.zero);

            return root.rectTransform;
        }

        private RectTransform CreateUnitToken(Transform parent, CardRecord card, bool compact)
        {
            var root = CreateImage(parent, card.CardName, card.Camp == CardCamp.Enemy ? enemyCardColor : heroCardColor);
            root.gameObject.AddComponent<Outline>().effectColor = new Color(0.22f, 0.16f, 0.1f);

            var name = CreateText(root.transform, card.CardName, compact ? 17 : 18, TextAnchor.MiddleCenter, new Color(0.12f, 0.08f, 0.05f));
            SetRect(name.rectTransform, new Vector2(0f, 0.58f), Vector2.one, Vector2.zero, Vector2.zero);

            var stats = CreateText(root.transform, $"攻{card.Attack} / 血{card.Hp}", compact ? 15 : 16, TextAnchor.MiddleCenter, Color.white);
            SetRect(stats.rectTransform, new Vector2(0f, 0.08f), new Vector2(1f, 0.42f), Vector2.zero, Vector2.zero);

            return root.rectTransform;
        }

        private RectTransform CreateBadge(Transform parent, string value, Color color)
        {
            var badge = CreateImage(parent, "Badge", color);
            badge.gameObject.AddComponent<Outline>().effectColor = new Color(0.15f, 0.1f, 0.07f);

            var text = CreateText(badge.transform, value, 20, TextAnchor.MiddleCenter, Color.white);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            return badge.rectTransform;
        }

        private Button CreateButton(Transform parent, string label)
        {
            var image = CreateImage(parent, label, new Color(0.9f, 0.28f, 0.21f));
            image.gameObject.AddComponent<Outline>().effectColor = new Color(0.22f, 0.12f, 0.08f);

            var button = image.gameObject.AddComponent<Button>();
            var text = CreateText(image.transform, label, 28, TextAnchor.MiddleCenter, Color.white);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            return button;
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

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            rect.offsetMin = rect.offsetMin;
            rect.offsetMax = rect.offsetMax;
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
