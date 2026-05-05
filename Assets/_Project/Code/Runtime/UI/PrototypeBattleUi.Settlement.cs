using System.Collections.Generic;
using System.Linq;
using UnifyCountry.Config;
using UnityEngine;
using UnityEngine.UI;

namespace UnifyCountry.UI
{
    public sealed partial class PrototypeBattleUi
    {
        private void BuildSettlementOverlay(Transform parent)
        {
            var overlay = new GameObject("Settlement Overlay", typeof(RectTransform));
            overlay.transform.SetParent(parent, false);
            var overlayRect = overlay.GetComponent<RectTransform>();
            SetRect(overlayRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var dimmer = CreateImage(overlay.transform, "Dim Background", new Color(0f, 0f, 0f, 0.58f));
            SetRect(dimmer.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var panel = CreateImage(overlay.transform, "Settlement Panel", new Color(0.96f, 0.88f, 0.66f));
            panel.sprite = GetRoundedButtonSprite();
            panel.type = Image.Type.Sliced;
            SetRect(panel.rectTransform, new Vector2(0.22f, 0.18f), new Vector2(0.78f, 0.78f), Vector2.zero, Vector2.zero);

            var outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.22f, 0.16f, 0.1f);
            outline.effectDistance = new Vector2(4f, -4f);

            if (settlementStep == SettlementStep.Reward && battleWon)
                BuildRewardSettlement(panel.transform);
            else
                BuildResultSettlement(panel.transform);
        }

        private void BuildResultSettlement(Transform parent)
        {
            var levelName = $"第 {currentLevelIndex + 1} 关";

            var levelText = CreateText(parent, levelName, 24, TextAnchor.MiddleCenter, new Color(0.2f, 0.13f, 0.08f));
            SetRect(levelText.rectTransform, new Vector2(0.12f, 0.78f), new Vector2(0.88f, 0.9f), Vector2.zero, Vector2.zero);

            var titleColor = battleWon ? new Color(0.42f, 0.62f, 0.18f) : new Color(0.72f, 0.16f, 0.12f);
            var title = CreateText(parent, battleWon ? "战斗胜利" : "战斗失败", 44, TextAnchor.MiddleCenter, titleColor);
            SetRect(title.rectTransform, new Vector2(0.12f, 0.62f), new Vector2(0.88f, 0.78f), Vector2.zero, Vector2.zero);

            var summary = battleWon ? "敌军已溃败，选择奖励后继续远征。" : "大本营被攻破，整备后可以重新挑战。";
            var summaryText = CreateText(parent, summary, 22, TextAnchor.MiddleCenter, new Color(0.22f, 0.14f, 0.08f));
            SetRect(summaryText.rectTransform, new Vector2(0.12f, 0.52f), new Vector2(0.88f, 0.62f), Vector2.zero, Vector2.zero);

            BuildSettlementStats(parent);

            if (battleWon)
            {
                var continueButton = CreateButton(parent, "继续");
                SetRect(continueButton.GetComponent<RectTransform>(), new Vector2(0.36f, 0.08f), new Vector2(0.64f, 0.18f), Vector2.zero, Vector2.zero);
                continueButton.onClick.AddListener(() =>
                {
                    settlementStep = SettlementStep.Reward;
                    EnsureSettlementRewards();
                    BuildUi();
                });
            }
            else
            {
                var retryButton = CreateButton(parent, "重新挑战");
                SetRect(retryButton.GetComponent<RectTransform>(), new Vector2(0.22f, 0.08f), new Vector2(0.47f, 0.18f), Vector2.zero, Vector2.zero);
                retryButton.onClick.AddListener(ResetBattle);

                var restartButton = CreateButton(parent, "返回第一关");
                SetRect(restartButton.GetComponent<RectTransform>(), new Vector2(0.53f, 0.08f), new Vector2(0.78f, 0.18f), Vector2.zero, Vector2.zero);
                restartButton.onClick.AddListener(RestartRun);
            }
        }

        private void BuildSettlementStats(Transform parent)
        {
            var aliveHeroes = playerUnits.Count(unit => unit != null && !unit.IsDead && unit.UnitType == UnitType.Hero);
            var totalCards = library.Count + drawPile.Count + discardPile.Count + hand.Count;

            var turnBlock = CreateInfoBlock(parent, "回合", turnNumber.ToString(), new Color(0.98f, 0.8f, 0.38f));
            SetRect(turnBlock, new Vector2(0.16f, 0.28f), new Vector2(0.34f, 0.46f), Vector2.zero, Vector2.zero);

            var baseBlock = CreateInfoBlock(parent, "大本营", $"{Mathf.Max(0, playerBaseHp)}/{PlayerBaseMaxHp}", new Color(0.7f, 0.9f, 0.78f));
            SetRect(baseBlock, new Vector2(0.41f, 0.28f), new Vector2(0.59f, 0.46f), Vector2.zero, Vector2.zero);

            var deckBlock = CreateInfoBlock(parent, "牌库", totalCards.ToString(), new Color(0.72f, 0.84f, 0.95f));
            SetRect(deckBlock, new Vector2(0.66f, 0.28f), new Vector2(0.84f, 0.46f), Vector2.zero, Vector2.zero);

            var heroText = CreateText(parent, $"存活英雄：{aliveHeroes}", 20, TextAnchor.MiddleCenter, new Color(0.22f, 0.14f, 0.08f));
            SetRect(heroText.rectTransform, new Vector2(0.2f, 0.21f), new Vector2(0.8f, 0.27f), Vector2.zero, Vector2.zero);
        }

        private void BuildRewardSettlement(Transform parent)
        {
            EnsureSettlementRewards();

            var title = CreateText(parent, rewardClaimed ? "奖励已加入牌库" : "选择一张卡牌加入牌库", 34, TextAnchor.MiddleCenter, new Color(0.2f, 0.13f, 0.08f));
            SetRect(title.rectTransform, new Vector2(0.08f, 0.82f), new Vector2(0.92f, 0.94f), Vector2.zero, Vector2.zero);

            var hint = CreateText(parent, "两张己方英雄单位卡，一张计谋卡", 20, TextAnchor.MiddleCenter, new Color(0.38f, 0.25f, 0.14f));
            SetRect(hint.rectTransform, new Vector2(0.12f, 0.73f), new Vector2(0.88f, 0.81f), Vector2.zero, Vector2.zero);

            for (var i = 0; i < settlementRewardOptions.Count; i++)
                BuildRewardOption(parent, settlementRewardOptions[i], i);

            if (rewardClaimed)
            {
                var selected = selectedRewardIndex >= 0 && selectedRewardIndex < settlementRewardOptions.Count ? settlementRewardOptions[selectedRewardIndex] : null;
                var text = CreateText(parent, selected == null ? "已完成选择" : $"已加入：{selected.CardName}", 22, TextAnchor.MiddleCenter, new Color(0.42f, 0.62f, 0.18f));
                SetRect(text.rectTransform, new Vector2(0.2f, 0.17f), new Vector2(0.8f, 0.25f), Vector2.zero, Vector2.zero);
            }

            var buttonLabel = rewardClaimed ? (HasNextLevel ? "前往下一关" : "重新开始") : "确认选择";
            var confirmButton = CreateButton(parent, buttonLabel);
            SetRect(confirmButton.GetComponent<RectTransform>(), new Vector2(0.36f, 0.06f), new Vector2(0.64f, 0.16f), Vector2.zero, Vector2.zero);
            confirmButton.interactable = rewardClaimed || selectedRewardIndex >= 0;
            confirmButton.onClick.AddListener(() =>
            {
                if (!rewardClaimed)
                {
                    ClaimSelectedReward();
                    BuildUi();
                    return;
                }

                if (HasNextLevel)
                    StartNextLevel();
                else
                    RestartRun();
            });
        }

        private void BuildRewardOption(Transform parent, CardRecord card, int index)
        {
            var root = CreateCardBase(parent, card);
            SetRect(root.rectTransform, new Vector2(0.16f + index * 0.24f, 0.28f), new Vector2(0.32f + index * 0.24f, 0.68f), Vector2.zero, Vector2.zero);

            var button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            button.targetGraphic = root;
            button.interactable = !rewardClaimed;
            button.onClick.AddListener(() =>
            {
                selectedRewardIndex = index;
                BuildUi();
            });

            if (selectedRewardIndex == index)
            {
                CreateBorder(root.transform, new Color(0.18f, 0.58f, 0.95f), 7f);
                var label = CreateText(root.transform, rewardClaimed ? "已加入" : "已选择", 18, TextAnchor.MiddleCenter, Color.white);
                SetRect(label.rectTransform, new Vector2(0.18f, 0.01f), new Vector2(0.82f, 0.11f), Vector2.zero, Vector2.zero);
            }
        }

        private void EnsureSettlementRewards()
        {
            if (settlementRewardOptions.Count > 0)
                return;

            var heroOptions = cardMap.Values
                .Where(card => card.CardType == CardType.Unit && card.UnitType == UnitType.Hero && card.Camp == CardCamp.Player)
                .ToList();
            Shuffle(heroOptions);

            for (var i = 0; i < heroOptions.Count && settlementRewardOptions.Count < 2; i++)
                settlementRewardOptions.Add(heroOptions[i]);

            var tacticOptions = cardMap.Values
                .Where(card => card.CardType == CardType.Skill && card.Camp == CardCamp.Player)
                .ToList();
            Shuffle(tacticOptions);

            if (tacticOptions.Count > 0)
                settlementRewardOptions.Add(tacticOptions[0]);

            if (settlementRewardOptions.Count >= 3)
                return;

            var fallbackOptions = cardMap.Values
                .Where(card => card.Camp == CardCamp.Player && !settlementRewardOptions.Contains(card))
                .ToList();
            Shuffle(fallbackOptions);

            for (var i = 0; i < fallbackOptions.Count && settlementRewardOptions.Count < 3; i++)
                settlementRewardOptions.Add(fallbackOptions[i]);
        }

        private void ClaimSelectedReward()
        {
            if (rewardClaimed || selectedRewardIndex < 0 || selectedRewardIndex >= settlementRewardOptions.Count)
                return;

            var card = settlementRewardOptions[selectedRewardIndex];
            if (runDeckCounts.ContainsKey(card.CardId))
                runDeckCounts[card.CardId]++;
            else
                runDeckCounts[card.CardId] = 1;

            rewardClaimed = true;
            AddBattleLogEntry($"奖励加入牌库：{card.CardName}。");
        }

        private void RestartRun()
        {
            currentLevelIndex = 0;
            runDeckInitialized = false;
            runDeckCounts.Clear();
            ResetBattle();
        }
    }
}
