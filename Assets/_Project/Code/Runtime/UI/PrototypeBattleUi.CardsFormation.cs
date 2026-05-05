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
            return card != null
                && card.CardType == CardType.Unit
                && card.Unit != null
                && !isResolvingTurn
                && currentEnergy >= card.Cost;
        }

        internal void PlayCardAt(CardRecord card, int insertIndex)
        {
            if (card == null || card.Camp != CardCamp.Player || card.CardType != CardType.Unit || card.Unit == null)
                return;

            if (isResolvingTurn)
                return;

            if (CountPlayerUnits() >= TotalFormationSlots)
            {
                AddBattleLogEntry("友方阵地已满，无法继续上阵。");
                BuildUi();
                return;
            }

            if (currentEnergy < card.Cost)
            {
                AddBattleLogEntry($"费用不足：{card.CardName} 需要 {card.Cost} 点费用。");
                BuildUi();
                return;
            }

            insertIndex = Mathf.Clamp(insertIndex, 0, TotalFormationSlots - 1);
            if (playerUnits[insertIndex] != null)
            {
                AddBattleLogEntry("该阵地位置已有单位。");
                BuildUi();
                return;
            }

            if (!hand.Remove(card))
                return;

            currentEnergy -= card.Cost;
            var unit = battleState.CreateUnit(card);
            playerUnits[insertIndex] = unit;

            var logLines = new List<string> { $"{card.CardName} 上阵，消耗 {card.Cost} 点费用。" };
            TriggerEffects(unit, "OnPlay", GetSlotRow(insertIndex), null, logLines);
            CommitTurnLog(logLines);
            BuildUi();
        }

        internal void PlayCardInGap(CardRecord card, int gapIndex)
        {
            if (card == null || card.Camp != CardCamp.Player || card.CardType != CardType.Unit || card.Unit == null)
                return;

            if (isResolvingTurn)
                return;

            if (CountPlayerUnits() >= TotalFormationSlots)
            {
                AddBattleLogEntry("友方阵地已满，无法继续上阵。");
                BuildUi();
                return;
            }

            if (currentEnergy < card.Cost)
            {
                AddBattleLogEntry($"费用不足：{card.CardName} 需要 {card.Cost} 点费用。");
                BuildUi();
                return;
            }

            if (!hand.Remove(card))
                return;

            var unit = battleState.CreateUnit(card);
            if (!TryInsertPlayerUnitAtGap(unit, gapIndex))
            {
                hand.Add(card);
                AddBattleLogEntry("当前军阵插入位置不可用。");
                BuildUi();
                return;
            }

            currentEnergy -= card.Cost;
            var slotIndex = GetUnitSlotIndex(playerUnits, unit);
            var logLines = new List<string> { $"{card.CardName} 插入阵地，消耗 {card.Cost} 点费用。" };
            TriggerEffects(unit, "OnPlay", slotIndex >= 0 ? GetSlotRow(slotIndex) : DecodeGapRow(gapIndex), null, logLines);
            CommitTurnLog(logLines);
            BuildUi();
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

        private int GetFirstEmptyEnemySlotInRow(int row)
        {
            return battleFormation.GetFirstEmptyEnemySlotInRow(row);
        }

        private int CountPlayerUnits()
        {
            return battleFormation.CountPlayerUnits();
        }

        private int CountEnemyUnits()
        {
            return battleFormation.CountEnemyUnits();
        }
    }
}
