using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnifyCountry.Combat;
using UnifyCountry.Config;
using UnityEngine;

namespace UnifyCountry.UI
{
    public sealed partial class PrototypeBattleUi
    {
        private BattleLevelRecord CurrentLevel => battleState.CurrentLevel;

        private List<WaveSpawnRecord> CurrentWaves => battleState.CurrentWaves;

        private bool HasNextLevel => battleState.HasNextLevel;

        private void EndTurn()
        {
            if (isResolvingTurn || battleEnded)
                return;

            if (battlePhase == BattlePhase.InitialPrepare)
            {
                StartCoroutine(AdvanceFromInitialPrepareRoutine());
                return;
            }

            StartCoroutine(ResolvePlayerTurnRoutine());
        }

        private IEnumerator AdvanceFromInitialPrepareRoutine()
        {
            isResolvingTurn = true;

            var logLines = new List<string>();
            logLines.Add("准备阶段结束，敌方第一波即将进场。");
            DiscardHand(logLines);

            yield return StartCoroutine(AdvanceFormationsRoutine(logLines));

            StartNextPlayerTurn(logLines);
            CommitTurnLog(logLines);
            isResolvingTurn = false;
            RefreshTacticalViews();
            RefreshHud();
        }

        private IEnumerator ResolvePlayerTurnRoutine()
        {
            isResolvingTurn = true;

            var logLines = new List<string>();
            logLines.Add($"第 {turnNumber} 回合行动结束，进入战斗结算。");
            DiscardHand(logLines);
            RefreshTacticalViews();
            RefreshHud();

            yield return StartCoroutine(ResolveCombatRowsRoutine(logLines));
            if (playerBaseHp <= 0)
            {
                logLines.Add("大本营被攻破，战斗失败。");
                CommitTurnLog(logLines);
                battleEnded = true;
                battleWon = false;
                isResolvingTurn = false;
                BuildUi();
                yield break;
            }

            if (TryFinishBattleAfterBossDefeat(logLines))
                yield break;

            TriggerPlayerTurnEndEffects(logLines);
            ResolveEndOfTurnBuffs(logLines);
            RefreshUnitHealthViews();
            UpdateActiveTurnLog(logLines);
            yield return StartCoroutine(ResolveDeathsAndAdvanceRoutine(logLines));
            if (TryFinishBattleAfterBossDefeat(logLines))
                yield break;

            UpdateActiveTurnLog(logLines);

            var waves = CurrentWaves;
            if (CountEnemyUnits() == 0 && (waves == null || nextWaveIndex >= waves.Count))
            {
                logLines.Add(HasNextLevel ? $"第 {currentLevelIndex + 1} 关胜利！" : "战斗胜利！");
                CommitTurnLog(logLines);
                battleEnded = true;
                battleWon = true;
                isResolvingTurn = false;
                BuildUi();
                yield break;
            }

            StartNextPlayerTurn(logLines);
            CommitTurnLog(logLines);
            isResolvingTurn = false;
            RefreshTacticalViews();
            RefreshHud();
        }

        private void StartNextPlayerTurn(List<string> logLines)
        {
            turnNumber++;
            battlePhase = BattlePhase.PlayerAction;

            SpawnCurrentWave(logLines);

            firstUnitCardDrawRelicTriggeredThisTurn = false;
            var maxEnergyThisTurn = GetMaxEnergyThisTurn();
            currentEnergy = maxEnergyThisTurn;
            var drawCount = DrawCardsWithCount(CardsDrawnPerTurn);
            logLines.Add($"进入第 {turnNumber} 回合：敌方单位进场后暂不攻击，费用恢复到 {maxEnergyThisTurn}，从抽牌堆抽 {drawCount} 张牌。");
            TriggerPlayerTurnStartEffects(logLines);
        }

        private IEnumerator ResolveCombatRowsRoutine(List<string> logLines)
        {
            foreach (var row in BattleFormation.GetRowsFrontToRear())
            {
                yield return StartCoroutine(ResolveEnemyAttackRoutine(logLines, row));
                if (playerBaseHp <= 0 || activeBossDefeated)
                    yield break;

                yield return new WaitForSeconds(0.25f);

                yield return StartCoroutine(ResolvePlayerAttackRoutine(logLines, row));
                if (activeBossDefeated)
                    yield break;

                yield return new WaitForSeconds(0.25f);
            }
        }

        private IEnumerator ResolveEnemyAttackRoutine(List<string> logLines)
        {
            foreach (var row in BattleFormation.GetRowsFrontToRear())
            {
                yield return StartCoroutine(ResolveEnemyAttackRoutine(logLines, row));
                if (playerBaseHp <= 0 || activeBossDefeated)
                    yield break;
            }
        }

        private IEnumerator ResolveEnemyAttackRoutine(List<string> logLines, int row)
        {
            var attackers = CollectEnemyAttackers(row);
            foreach (var attacker in attackers)
            {
                if (attacker == null || attacker.IsDead)
                    continue;

                var target = GetPlayerFrontUnit(row);
                var healthBefore = CaptureCombatHealthSnapshot();
                yield return StartCoroutine(PlayAttackMotion(attacker, target, target == null));

                ResolveEnemyUnitAttack(attacker, row, logLines);
                RefreshUnitHealthViews();
                yield return StartCoroutine(PlayDamageReactions(healthBefore, target, target == null));
                UpdateActiveTurnLog(logLines);
                yield return StartCoroutine(ResolveDeathsAndAdvanceRoutine(logLines));
                if (playerBaseHp <= 0)
                    yield break;
                if (activeBossDefeated)
                    yield break;
            }
        }

        private IEnumerator ResolvePlayerAttackRoutine(List<string> logLines)
        {
            foreach (var row in BattleFormation.GetRowsFrontToRear())
            {
                yield return StartCoroutine(ResolvePlayerAttackRoutine(logLines, row));
                if (activeBossDefeated)
                    yield break;
            }
        }

        private IEnumerator ResolvePlayerAttackRoutine(List<string> logLines, int row)
        {
            var attackers = CollectPlayerAttackers(row);
            foreach (var attacker in attackers)
            {
                if (attacker == null || attacker.IsDead)
                    continue;

                var slotIndex = GetUnitSlotIndex(playerUnits, attacker);
                if (slotIndex < 0)
                    continue;

                if (GetSlotRow(slotIndex) != row)
                    continue;

                var target = GetEnemyFrontUnit(row);
                var healthBefore = CaptureCombatHealthSnapshot();
                yield return StartCoroutine(PlayAttackMotion(attacker, target, false));

                ResolvePlayerUnitAttack(attacker, row, logLines);
                RefreshUnitHealthViews();
                yield return StartCoroutine(PlayDamageReactions(healthBefore, target, false));
                UpdateActiveTurnLog(logLines);
                yield return StartCoroutine(ResolveDeathsAndAdvanceRoutine(logLines));
                if (activeBossDefeated)
                    yield break;
            }
        }

        private bool TryFinishBattleAfterBossDefeat(List<string> logLines)
        {
            if (!activeBossDefeated)
                return false;

            logLines.Add(HasNextLevel ? $"第 {currentLevelIndex + 1} 关 Boss 阵亡，胜利！" : "Boss 阵亡，战斗胜利！");
            CommitTurnLog(logLines);
            battleEnded = true;
            battleWon = true;
            activeBossRuntimeId = -1;
            isResolvingTurn = false;
            BuildUi();
            return true;
        }

        private List<BattleUnit> CollectEnemyAttackers(int row)
        {
            var attackers = new List<BattleUnit>();
            attackers.AddRange(battleFormation.GetAliveEnemyUnitsInRow(row));
            return attackers;
        }

        private List<BattleUnit> CollectPlayerAttackers(int row)
        {
            var attackers = new List<BattleUnit>();
            for (var column = MaxFormationSlots - 1; column >= 0; column--)
                AddAttacker(playerUnits, attackers, row, column);

            return attackers;
        }

        private static void AddAttacker(List<BattleUnit> units, List<BattleUnit> attackers, int row, int column)
        {
            var unit = units[GetSlotIndex(row, column)];
            if (unit != null && !unit.IsDead)
                attackers.Add(unit);
        }

        private void TriggerPlayerTurnStartEffects(List<string> logLines)
        {
            battleEffectResolver.TriggerPlayerTurnStartEffects(logLines);
        }

        private void TriggerPlayerTurnEndEffects(List<string> logLines)
        {
            battleEffectResolver.TriggerPlayerTurnEndEffects(logLines);
        }

        private int DrawCardsWithCount(int count)
        {
            return battleDeck.DrawCardsWithCount(count);
        }

        private int GetMaxEnergyThisTurn()
        {
            return battlePhase == BattlePhase.InitialPrepare
                ? InitialPrepareEnergy
                : MaxEnergy + battleState.FormalTurnMaxEnergyBonus;
        }

        private void AddBattleLogEntry(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return;

            battleLogHistory.Add(line);
            battleLog = ComposeBattleLog(null);
            RefreshBattleLogText();
        }

        private void UpdateActiveTurnLog(List<string> activeLines)
        {
            battleLog = ComposeBattleLog(activeLines);
            RefreshBattleLogText();
        }

        private void CommitTurnLog(List<string> activeLines)
        {
            if (activeLines != null)
                battleLogHistory.AddRange(activeLines.Where(line => !string.IsNullOrWhiteSpace(line)));

            battleLog = ComposeBattleLog(null);
            RefreshBattleLogText();
        }

        private void RefreshBattleLogText()
        {
            if (battleLogText != null)
                battleLogText.text = battleLog;
        }

        private string ComposeBattleLog(List<string> activeLines)
        {
            var lines = new List<string>();
            if (battleLogHistory.Count > 0)
                lines.AddRange(battleLogHistory);

            if (activeLines != null && activeLines.Count > 0)
            {
                if (lines.Count > 0)
                    lines.Add(string.Empty);

                lines.AddRange(activeLines);
            }

            return lines.Count == 0 ? InitialBattleLog : string.Join("\n", lines);
        }

        private void DiscardHand(List<string> logLines)
        {
            battleDeck.DiscardHand(logLines);
        }

    }
}
