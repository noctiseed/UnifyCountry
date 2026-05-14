using System.Collections.Generic;
using UnifyCountry.Combat;
using UnifyCountry.Config;
using UnityEngine;

namespace UnifyCountry.UI
{
    public sealed partial class PrototypeBattleUi
    {
        private void PlayCard(CardRecord card)
        {
            PlayCardFromHand(card, null);
        }

        internal bool CanDragCard(CardRecord card)
        {
            return IsPlayableUnitCard(card)
                && !isResolvingTurn
                && currentEnergy >= card.Cost;
        }

        internal void PlayCardAt(CardRecord card, int insertIndex)
        {
            if (!IsPlayableUnitCard(card))
                return;

            if (isResolvingTurn)
                return;

            if (CountPlayerUnits() >= TotalFormationSlots)
            {
                AddBattleLogEntry("友方阵地已满，无法继续上阵。");
                RefreshHud();
                return;
            }

            if (currentEnergy < card.Cost)
            {
                AddBattleLogEntry($"费用不足：{card.CardName} 需要 {card.Cost} 点费用。");
                RefreshHud();
                return;
            }

            insertIndex = Mathf.Clamp(insertIndex, 0, TotalFormationSlots - 1);
            if (playerUnits[insertIndex] != null)
            {
                AddBattleLogEntry("该阵地位置已有单位。");
                RefreshHud();
                return;
            }

            if (!hand.Remove(card))
                return;

            currentEnergy -= card.Cost;
            var unit = battleState.CreateUnit(card, CardCamp.Player);
            playerUnits[insertIndex] = unit;

            var logLines = new List<string> { $"{card.CardName} 上阵，消耗 {card.Cost} 点费用。" };
            TriggerEffects(unit, "OnPlay", GetSlotRow(insertIndex), null, logLines);
            TryTriggerFirstUnitCardDrawRelic(logLines);
            CommitTurnLog(logLines);
            RefreshTacticalViews();
            RefreshHud();
        }

        internal void PlayCardInGap(CardRecord card, int gapIndex)
        {
            if (!IsPlayableUnitCard(card))
                return;

            if (isResolvingTurn)
                return;

            if (CountPlayerUnits() >= TotalFormationSlots)
            {
                AddBattleLogEntry("友方阵地已满，无法继续上阵。");
                RefreshHud();
                return;
            }

            if (currentEnergy < card.Cost)
            {
                AddBattleLogEntry($"费用不足：{card.CardName} 需要 {card.Cost} 点费用。");
                RefreshHud();
                return;
            }

            if (!hand.Remove(card))
                return;

            var unit = battleState.CreateUnit(card, CardCamp.Player);
            if (!TryInsertPlayerUnitAtGap(unit, gapIndex))
            {
                hand.Add(card);
                AddBattleLogEntry("当前军阵插入位置不可用。");
                RefreshHud();
                return;
            }

            currentEnergy -= card.Cost;
            var slotIndex = GetUnitSlotIndex(playerUnits, unit);
            var logLines = new List<string> { $"{card.CardName} 插入阵地，消耗 {card.Cost} 点费用。" };
            TriggerEffects(unit, "OnPlay", slotIndex >= 0 ? GetSlotRow(slotIndex) : DecodeGapRow(gapIndex), null, logLines);
            TryTriggerFirstUnitCardDrawRelic(logLines);
            CommitTurnLog(logLines);
            RefreshTacticalViews();
            RefreshHud();
        }

        private static bool IsPlayableUnitCard(CardRecord card)
        {
            return card != null
                && card.CardType == CardType.Unit
                && card.Unit != null
                && (card.Camp == CardCamp.Player || card.UnitType == UnitType.Soldier);
        }

        private bool TryInsertPlayerUnitAtGap(BattleUnit unit, int gapIndex)
        {
            return battleFormation.TryInsertPlayerUnitAtGap(unit, gapIndex);
        }

        private List<int> GetOccupiedPlayerSlotsInRow(int row)
        {
            return battleFormation.GetOccupiedPlayerSlotsInRow(row);
        }

        private int GetFirstEmptyPlayerSlot()
        {
            return battleFormation.GetFirstEmptyPlayerSlot();
        }

        private int CountPlayerUnits()
        {
            return battleFormation.CountPlayerUnits();
        }

        private int CountEnemyUnits()
        {
            return battleFormation.CountEnemyUnits();
        }

        private void TryTriggerFirstUnitCardDrawRelic(List<string> logLines)
        {
            if (battlePhase != BattlePhase.PlayerAction || !battleState.DrawOnFirstUnitCardEachTurn || firstUnitCardDrawRelicTriggeredThisTurn)
                return;

            firstUnitCardDrawRelicTriggeredThisTurn = true;
            var drawn = DrawCardsWithCount(1);
            if (drawn > 0)
                logLines.Add($"铁甲军令触发：本回合第一次打出单位牌，抽 {drawn} 张牌。");
            else
                logLines.Add("铁甲军令触发：本回合第一次打出单位牌，但抽牌堆没有可抽的牌。");
        }
    }
}
