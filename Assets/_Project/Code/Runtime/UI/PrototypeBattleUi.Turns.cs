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

            yield return StartCoroutine(ResolveEnemyAttackRoutine(logLines));
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

            yield return new WaitForSeconds(0.75f);

            yield return StartCoroutine(ResolvePlayerAttackRoutine(logLines));
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

            currentEnergy = MaxEnergy;
            var drawCount = DrawCardsWithCount(CardsDrawnPerTurn);
            logLines.Add($"进入第 {turnNumber} 回合：敌方单位进场后暂不攻击，费用恢复到 {MaxEnergy}，从抽牌堆抽 {drawCount} 张牌。");
            TriggerPlayerTurnStartEffects(logLines);
        }

        private IEnumerator ResolveEnemyAttackRoutine(List<string> logLines)
        {
            var attackers = CollectAttackers(enemyUnits, false);
            foreach (var attacker in attackers)
            {
                if (attacker == null || attacker.IsDead)
                    continue;

                var row = battleFormation.GetEnemyUnitRow(attacker);
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
            var attackers = CollectAttackers(playerUnits, true);
            foreach (var attacker in attackers)
            {
                if (attacker == null || attacker.IsDead)
                    continue;

                var slotIndex = GetUnitSlotIndex(playerUnits, attacker);
                if (slotIndex < 0)
                    continue;

                var row = GetSlotRow(slotIndex);
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

        private List<BattleUnit> CollectAttackers(List<BattleUnit> units, bool playerSide)
        {
            var attackers = new List<BattleUnit>();
            foreach (var row in BattleFormation.GetRowsFrontToRear())
            {
                if (playerSide)
                {
                    for (var column = MaxFormationSlots - 1; column >= 0; column--)
                        AddAttacker(units, attackers, row, column);
                }
                else
                {
                    attackers.AddRange(battleFormation.GetAliveEnemyUnitsInRow(row));
                }
            }

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
