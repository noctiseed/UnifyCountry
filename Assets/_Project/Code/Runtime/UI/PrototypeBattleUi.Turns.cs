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
            DiscardUnplayedInitialHeroes(logLines);

            yield return StartCoroutine(AdvanceFormationsRoutine(logLines));

            StartNextPlayerTurn(logLines);
            CommitTurnLog(logLines);
            isResolvingTurn = false;
            BuildUi();
        }

        private IEnumerator ResolvePlayerTurnRoutine()
        {
            isResolvingTurn = true;

            var logLines = new List<string>();
            logLines.Add($"第 {turnNumber} 回合行动结束，进入战斗结算。");
            DiscardHand(logLines);

            ResolveEnemyAttack(logLines);
            UpdateActiveTurnLog(logLines);
            BuildUi();
            if (playerBaseHp <= 0)
            {
                logLines.Add("大本营被攻破，战斗失败。");
                CommitTurnLog(logLines);
                battleEnded = true;
                isResolvingTurn = false;
                BuildUi();
                yield break;
            }

            yield return new WaitForSeconds(0.75f);

            ResolvePlayerAttack(logLines);
            AppendDeathLogs(logLines);

            yield return StartCoroutine(AdvanceFormationsRoutine(logLines));
            ResolveEndOfTurnBuffs(logLines);
            UpdateActiveTurnLog(logLines);
            BuildUi();

            var waves = CurrentWaves;
            if (CountEnemyUnits() == 0 && (waves == null || nextWaveIndex >= waves.Count))
            {
                logLines.Add(HasNextLevel ? $"第 {currentLevelIndex + 1} 关胜利！" : "战斗胜利！");
                CommitTurnLog(logLines);
                battleEnded = true;
                isResolvingTurn = false;
                BuildUi();
                yield break;
            }

            StartNextPlayerTurn(logLines);
            CommitTurnLog(logLines);
            isResolvingTurn = false;
            BuildUi();
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

        private void TriggerPlayerTurnStartEffects(List<string> logLines)
        {
            battleEffectResolver.TriggerPlayerTurnStartEffects(logLines);
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

        private void DiscardUnplayedInitialHeroes(List<string> logLines)
        {
            battleDeck.DiscardUnplayedInitialHeroes(logLines);
        }
    }
}
