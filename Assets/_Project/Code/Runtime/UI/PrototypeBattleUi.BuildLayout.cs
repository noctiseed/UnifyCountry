using System.Collections.Generic;
using UnifyCountry.Combat;
using UnifyCountry.Config;
using UnityEngine;
using UnityEngine.UI;

namespace UnifyCountry.UI
{
    public sealed partial class PrototypeBattleUi
    {
        private void BuildUi()
        {
            CancelSkillCast();
            ResetSkillTargetHandlers();
            RebuildCardPortraitMap();
            statusText = null;
            libraryCountText = null;
            drawPileCountText = null;
            energyCountText = null;
            discardPileCountText = null;
            battleLogText = null;
            playerBattleContent = null;
            enemyBattleContent = null;
            handContent = null;
            ClearChildren();
            unitViews.Clear();

            var canvas = CreateCanvas();
            EnsureEventSystem();
            CreateBackground(canvas.transform);

            var waves = CurrentWaves;
            var waveCount = waves == null ? 0 : waves.Count;
            var nextWaveLabel = waveCount == 0 ? "0 / 0" : $"{Mathf.Min(nextWaveIndex + 1, waveCount)} / {waveCount}";
            var turnLabel = battlePhase == BattlePhase.InitialPrepare ? "准备阶段" : $"第 {turnNumber} 回合";
            var status = CreateText(canvas.transform, $"第 {currentLevelIndex + 1} 关  |  {turnLabel}  |  下一波 {nextWaveLabel}", 22, TextAnchor.MiddleCenter, new Color(0.22f, 0.16f, 0.1f));
            SetRect(status.rectTransform, new Vector2(0.18f, 0.91f), new Vector2(0.82f, 0.97f), Vector2.zero, Vector2.zero);
            statusText = status;

            var playerPanel = CreatePanel(canvas.transform, "友方阵地", playerPanelColor, false);
            SetRect(playerPanel, new Vector2(0.02f, 0.31f), new Vector2(0.49f, 0.88f), Vector2.zero, Vector2.zero);
            playerBattleContent = CreateContentRoot(playerPanel.transform, "Player Battle Content");
            BuildPlayerBattleContent();

            var enemyPanel = CreatePanel(canvas.transform, "敌方阵地", enemyPanelColor, false);
            SetRect(enemyPanel, new Vector2(0.51f, 0.31f), new Vector2(0.98f, 0.88f), Vector2.zero, Vector2.zero);
            enemyBattleContent = CreateContentRoot(enemyPanel.transform, "Enemy Battle Content");
            BuildEnemyBattleContent();

            var libraryPanel = CreateClickableInfoBlock(canvas.transform, "牌库", library.Count.ToString(), new Color(0.7f, 0.9f, 0.78f), () => ShowCardPileModal(canvas.transform, "牌库", library));
            SetRect(libraryPanel.GetComponent<RectTransform>(), new Vector2(0.02f, 0.03f), new Vector2(0.085f, 0.28f), Vector2.zero, Vector2.zero);
            libraryCountText = GetInfoBlockValueText(libraryPanel.transform);

            var drawPilePanel = CreateClickableInfoBlock(canvas.transform, "抽牌堆", drawPile.Count.ToString(), new Color(0.72f, 0.84f, 0.95f), () => ShowCardPileModal(canvas.transform, "抽牌堆", drawPile));
            SetRect(drawPilePanel.GetComponent<RectTransform>(), new Vector2(0.095f, 0.03f), new Vector2(0.16f, 0.28f), Vector2.zero, Vector2.zero);
            drawPileCountText = GetInfoBlockValueText(drawPilePanel.transform);

            var maxEnergyThisTurn = battlePhase == BattlePhase.InitialPrepare ? InitialPrepareEnergy : MaxEnergy;
            var energyPanel = CreateInfoBlock(canvas.transform, "费用", $"{currentEnergy}/{maxEnergyThisTurn}", new Color(0.98f, 0.8f, 0.38f));
            SetRect(energyPanel, new Vector2(0.17f, 0.03f), new Vector2(0.235f, 0.28f), Vector2.zero, Vector2.zero);
            energyCountText = GetInfoBlockValueText(energyPanel.transform);

            var handPanel = CreatePanel(canvas.transform, "手牌", handPanelColor);
            SetRect(handPanel, new Vector2(0.25f, 0.03f), new Vector2(0.635f, 0.28f), Vector2.zero, Vector2.zero);
            handContent = CreateContentRoot(handPanel.transform, "Hand Content");
            BuildHand(handContent);

            var discardPilePanel = CreateClickableInfoBlock(canvas.transform, "弃牌堆", discardPile.Count.ToString(), new Color(0.78f, 0.72f, 0.88f), () => ShowCardPileModal(canvas.transform, "弃牌堆", discardPile));
            SetRect(discardPilePanel.GetComponent<RectTransform>(), new Vector2(0.65f, 0.03f), new Vector2(0.715f, 0.28f), Vector2.zero, Vector2.zero);
            discardPileCountText = GetInfoBlockValueText(discardPilePanel.transform);

            var logPanel = CreatePanel(canvas.transform, "战斗记录", new Color(0.93f, 0.84f, 0.64f));
            SetRect(logPanel, new Vector2(0.75f, 0.12f), new Vector2(0.98f, 0.28f), Vector2.zero, Vector2.zero);
            BuildBattleLog(logPanel.transform);

            if (!battleEnded)
            {
                var endTurnButton = CreateButton(canvas.transform, "结束回合");
                SetRect(endTurnButton.GetComponent<RectTransform>(), new Vector2(0.75f, 0.035f), new Vector2(0.86f, 0.1f), Vector2.zero, Vector2.zero);
                endTurnButton.interactable = !isResolvingTurn;
                endTurnButton.onClick.AddListener(EndTurn);
            }
            else if (HasNextLevel)
            {
                var nextLevelButton = CreateButton(canvas.transform, "下一关");
                SetRect(nextLevelButton.GetComponent<RectTransform>(), new Vector2(0.75f, 0.035f), new Vector2(0.86f, 0.1f), Vector2.zero, Vector2.zero);
                nextLevelButton.interactable = !isResolvingTurn;
                nextLevelButton.onClick.AddListener(StartNextLevel);
            }

            var resetButton = CreateButton(canvas.transform, "重开");
            SetRect(resetButton.GetComponent<RectTransform>(), new Vector2(battleEnded ? 0.87f : 0.88f, 0.035f), new Vector2(0.98f, 0.1f), Vector2.zero, Vector2.zero);
            resetButton.interactable = !isResolvingTurn;
            resetButton.onClick.AddListener(ResetBattle);

            if (battleEnded)
                BuildSettlementOverlay(canvas.transform);
        }

        private RectTransform CreateContentRoot(Transform parent, string name)
        {
            var contentObject = new GameObject(name, typeof(RectTransform));
            contentObject.transform.SetParent(parent, false);
            var rect = contentObject.GetComponent<RectTransform>();
            SetRect(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return rect;
        }

        private static Text GetInfoBlockValueText(Transform block)
        {
            var texts = block == null ? null : block.GetComponentsInChildren<Text>();
            return texts == null || texts.Length == 0 ? null : texts[texts.Length - 1];
        }

        private void BuildPlayerBattleContent()
        {
            if (playerBattleContent == null)
                return;

            BuildPlayerBase(playerBattleContent);
            BuildBoard(playerBattleContent, true, playerUnits);
        }

        private void BuildEnemyBattleContent()
        {
            if (enemyBattleContent == null)
                return;

            BuildBoard(enemyBattleContent, false, enemyUnits);
            BuildUpcomingWaveHint(enemyBattleContent);
        }

        private void RefreshHud()
        {
            var waves = CurrentWaves;
            var waveCount = waves == null ? 0 : waves.Count;
            var nextWaveLabel = waveCount == 0 ? "0 / 0" : $"{Mathf.Min(nextWaveIndex + 1, waveCount)} / {waveCount}";
            var turnLabel = battlePhase == BattlePhase.InitialPrepare ? "准备阶段" : $"第 {turnNumber} 回合";
            if (statusText != null)
                statusText.text = $"第 {currentLevelIndex + 1} 关  |  {turnLabel}  |  下一波 {nextWaveLabel}";

            if (libraryCountText != null)
                libraryCountText.text = library.Count.ToString();
            if (drawPileCountText != null)
                drawPileCountText.text = drawPile.Count.ToString();
            if (discardPileCountText != null)
                discardPileCountText.text = discardPile.Count.ToString();
            if (energyCountText != null)
            {
                var maxEnergyThisTurn = battlePhase == BattlePhase.InitialPrepare ? InitialPrepareEnergy : MaxEnergy;
                energyCountText.text = $"{currentEnergy}/{maxEnergyThisTurn}";
            }

            RefreshBattleLogText();
        }

        private void RefreshTacticalViews()
        {
            unitViews.Clear();
            ResetSkillTargetHandlers();

            ClearChildren(playerBattleContent);
            BuildPlayerBattleContent();

            ClearChildren(enemyBattleContent);
            BuildEnemyBattleContent();

            ClearChildren(handContent);
            if (handContent != null)
                BuildHand(handContent);
        }

        private void BuildBattleLog(Transform parent)
        {
            var scrollRoot = CreateImage(parent, "Battle Log Scroll", new Color(1f, 0.96f, 0.78f, 0.35f));
            SetRect(scrollRoot.rectTransform, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.72f), Vector2.zero, Vector2.zero);

            var scrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 24f;

            var viewport = CreateImage(scrollRoot.transform, "Viewport", Color.clear);
            viewport.gameObject.AddComponent<RectMask2D>();
            SetRect(viewport.rectTransform, new Vector2(0.02f, 0.04f), new Vector2(0.9f, 0.96f), Vector2.zero, Vector2.zero);

            var contentObject = new GameObject("Content", typeof(RectTransform), typeof(Text), typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewport.transform, false);

            var content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);

            var text = contentObject.GetComponent<Text>();
            text.text = battleLog;
            text.font = uiFont;
            text.fontSize = 16;
            text.alignment = TextAnchor.UpperLeft;
            text.color = new Color(0.18f, 0.12f, 0.08f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.resizeTextForBestFit = false;
            battleLogText = text;

            var fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollbar = CreateVerticalScrollbar(scrollRoot.transform);
            scrollRect.viewport = viewport.rectTransform;
            scrollRect.content = content;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalNormalizedPosition = 0f;
        }

        private Scrollbar CreateVerticalScrollbar(Transform parent)
        {
            var scrollbarRoot = CreateImage(parent, "Scrollbar", new Color(0.32f, 0.22f, 0.14f, 0.25f));
            SetRect(scrollbarRoot.rectTransform, new Vector2(0.92f, 0.04f), new Vector2(0.98f, 0.96f), Vector2.zero, Vector2.zero);

            var slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
            slidingArea.transform.SetParent(scrollbarRoot.transform, false);
            var slidingRect = slidingArea.GetComponent<RectTransform>();
            SetRect(slidingRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var handle = CreateImage(slidingArea.transform, "Handle", new Color(0.54f, 0.36f, 0.2f, 0.85f));
            SetRect(handle.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var scrollbar = scrollbarRoot.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.targetGraphic = handle;
            scrollbar.handleRect = handle.rectTransform;
            return scrollbar;
        }

        private Scrollbar CreateSlimVerticalScrollbar(Transform parent)
        {
            var scrollbarRoot = CreateImage(parent, "Slim Scrollbar", new Color(0.32f, 0.22f, 0.14f, 0.16f));
            SetRect(scrollbarRoot.rectTransform, new Vector2(0.975f, 0.04f), new Vector2(0.985f, 0.96f), Vector2.zero, Vector2.zero);

            var slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
            slidingArea.transform.SetParent(scrollbarRoot.transform, false);
            var slidingRect = slidingArea.GetComponent<RectTransform>();
            SetRect(slidingRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var handle = CreateImage(slidingArea.transform, "Handle", new Color(0.54f, 0.36f, 0.2f, 0.72f));
            SetRect(handle.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var scrollbar = scrollbarRoot.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.targetGraphic = handle;
            scrollbar.handleRect = handle.rectTransform;
            return scrollbar;
        }
    }
}
