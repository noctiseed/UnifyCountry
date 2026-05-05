using System.Collections.Generic;
using UnifyCountry.Combat;
using UnifyCountry.Config;

namespace UnifyCountry.UI
{
    public sealed partial class PrototypeBattleUi
    {
        private void SpawnCurrentWave(List<string> logLines)
        {
            battleEffectResolver.SpawnCurrentWave(CurrentWaves, logLines);
        }

        private void ResolveEnemyAttack(List<string> logLines)
        {
            battleEffectResolver.ResolveEnemyAttack(logLines);
        }

        private void ResolvePlayerAttack(List<string> logLines)
        {
            battleEffectResolver.ResolvePlayerAttack(logLines);
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
    }
}
