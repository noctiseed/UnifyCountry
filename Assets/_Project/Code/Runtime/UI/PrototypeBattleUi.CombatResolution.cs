using System.Collections.Generic;
using UnifyCountry.Combat;
using UnifyCountry.Config;

namespace UnifyCountry.UI
{
    public sealed partial class PrototypeBattleUi
    {
        private void SpawnCurrentWave(List<string> logLines)
        {
            var waveIndex = nextWaveIndex;
            var wave = CurrentWaves != null && waveIndex >= 0 && waveIndex < CurrentWaves.Count ? CurrentWaves[waveIndex] : null;
            var spawnedUnits = battleEffectResolver.SpawnCurrentWave(CurrentWaves, logLines);
            TrackBossWave(wave, spawnedUnits);
            ApplyWaveEntryRelics(spawnedUnits, logLines);
        }

        private void ApplyWaveEntryRelics(List<BattleUnit> spawnedUnits, List<string> logLines)
        {
            if (spawnedUnits == null || spawnedUnits.Count == 0)
                return;

            var hasFireOx = battleState.WaveEntryDamage > 0 || battleState.WaveEntryBurn > 0;
            if (!hasFireOx)
                return;

            foreach (var unit in spawnedUnits)
            {
                if (unit == null || unit.IsDead)
                    continue;

                var row = battleFormation.GetEnemyUnitRow(unit);
                if (battleState.WaveEntryDamage > 0)
                    battleEffectResolver.DealDamage(null, unit, battleState.WaveEntryDamage, row, logLines, $"火牛阵图冲击 {unit.Name}", false);

                if (battleState.WaveEntryBurn > 0 && !unit.IsDead)
                    unit.AddBurn(battleState.WaveEntryBurn);
            }

            if (battleState.WaveEntryBurn > 0)
                logLines.Add($"火牛阵图触发：本波敌人获得 {battleState.WaveEntryBurn} 层灼烧。");

            battleFormation.RemoveDeadUnitsFromFormation(logLines);
        }

        private void ResolveEnemyAttack(List<string> logLines)
        {
            battleEffectResolver.ResolveEnemyAttack(logLines);
        }

        private void ResolveEnemyUnitAttack(BattleUnit attacker, int row, List<string> logLines)
        {
            battleEffectResolver.ResolveEnemyUnitAttack(attacker, row, logLines);
        }

        private void ResolvePlayerAttack(List<string> logLines)
        {
            battleEffectResolver.ResolvePlayerAttack(logLines);
        }

        private void ResolvePlayerUnitAttack(BattleUnit attacker, int row, List<string> logLines)
        {
            battleEffectResolver.ResolvePlayerUnitAttack(attacker, row, logLines);
        }

        private void TriggerEffects(BattleUnit source, string timing, int row, BattleUnit currentTarget, List<string> logLines)
        {
            battleEffectResolver.TriggerEffects(source, timing, row, currentTarget, logLines);
        }

        private void DealDamage(BattleUnit source, BattleUnit target, int amount, int row, List<string> logLines, string actionText, bool triggerDamaged)
        {
            battleEffectResolver.DealDamage(source, target, amount, row, logLines, actionText, triggerDamaged);
        }

        private static int GetUnitSlotIndex(List<BattleUnit> units, BattleUnit target)
        {
            return BattleFormation.GetUnitSlotIndex(units, target);
        }

        private BattleUnit GetPlayerFrontUnit(int row)
        {
            return battleFormation.GetPlayerFrontUnit(row);
        }

        private BattleUnit GetEnemyFrontUnit(int row)
        {
            return battleFormation.GetEnemyFrontUnit(row);
        }

        private void TrackBossWave(WaveSpawnRecord wave, List<BattleUnit> spawnedUnits)
        {
            if (wave == null || spawnedUnits == null || !IsBossWave(wave))
                return;

            activeBossRuntimeId = -1;
            activeBossDefeated = false;
            foreach (var unit in spawnedUnits)
            {
                if (unit == null || unit.Camp != CardCamp.Enemy || unit.UnitType != UnitType.Hero)
                    continue;

                activeBossRuntimeId = unit.RuntimeId;
                break;
            }
        }

        private static bool IsBossWave(WaveSpawnRecord wave)
        {
            return wave != null
                && !string.IsNullOrWhiteSpace(wave.NoteKey)
                && wave.NoteKey.ToLowerInvariant().Contains("boss");
        }
    }
}
