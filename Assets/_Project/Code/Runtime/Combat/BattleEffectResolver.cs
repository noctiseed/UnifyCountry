using System.Collections.Generic;
using UnifyCountry.Config;
using UnityEngine;

namespace UnifyCountry.Combat
{
    internal sealed class BattleEffectResolver
    {
        private readonly BattleState state;
        private readonly BattleDeck deck;
        private readonly BattleFormation formation;

        public BattleEffectResolver(BattleState state, BattleDeck deck, BattleFormation formation)
        {
            this.state = state;
            this.deck = deck;
            this.formation = formation;
        }

        public void SpawnCurrentWave(List<WaveSpawnRecord> waves, List<string> logLines)
        {
            if (waves == null || state.NextWaveIndex >= waves.Count)
                return;

            var wave = waves[state.NextWaveIndex];
            state.NextWaveIndex++;

            var spawnedNames = new List<string>();
            var spawnedUnits = new List<BattleUnit>();
            for (var row = 0; row < Mathf.Min(BattleFormation.FormationRows, wave.RowCardIds.Length); row++)
            {
                foreach (var cardId in wave.RowCardIds[row])
                {
                    var spawnSlot = formation.GetFirstEmptyEnemySlotInRow(row);
                    if (spawnSlot < 0)
                        break;

                    if (!state.CardMap.TryGetValue(cardId, out var card))
                        continue;

                    var unit = state.CreateUnit(card, CardCamp.Enemy);
                    state.EnemyUnits[spawnSlot] = unit;
                    spawnedUnits.Add(unit);
                    spawnedNames.Add($"{card.CardName}({BattleFormation.GetFormationRowName(row)})");
                }
            }

            if (spawnedNames.Count > 0)
                logLines.Add($"第 {state.CurrentLevelIndex + 1} 关第 {state.NextWaveIndex} 波出现：{string.Join("、", spawnedNames)}。");

            foreach (var unit in spawnedUnits)
            {
                var slotIndex = BattleFormation.GetUnitSlotIndex(state.EnemyUnits, unit);
                if (slotIndex >= 0)
                    TriggerEffects(unit, "OnPlay", BattleFormation.GetSlotRow(slotIndex), null, logLines);
            }
        }

        public void ResolveEnemyAttack(List<string> logLines)
        {
            for (var row = 0; row < BattleFormation.FormationRows; row++)
            {
                for (var column = 0; column < BattleFormation.MaxFormationSlots; column++)
                {
                    var attacker = state.EnemyUnits[BattleFormation.GetSlotIndex(row, column)];
                    if (attacker == null || attacker.IsDead)
                        continue;

                    var target = formation.GetPlayerFrontUnit(row);
                    if (target == null)
                    {
                        state.PlayerBaseHp = Mathf.Max(0, state.PlayerBaseHp - attacker.Attack);
                        logLines.Add($"{attacker.Name} 从{BattleFormation.GetFormationRowName(row)}攻击大本营，造成 {attacker.Attack} 点伤害。");
                        if (state.PlayerBaseHp <= 0)
                            return;

                        continue;
                    }

                    ResolveUnitAttack(attacker, row, target, logLines, "攻击");
                }
            }
        }

        public void ResolveEnemyUnitAttack(BattleUnit attacker, int row, List<string> logLines)
        {
            if (attacker == null || attacker.IsDead)
                return;

            var target = formation.GetPlayerFrontUnit(row);
            if (target == null)
            {
                state.PlayerBaseHp = Mathf.Max(0, state.PlayerBaseHp - attacker.Attack);
                logLines.Add($"{attacker.Name} 从{BattleFormation.GetFormationRowName(row)}攻击大本营，造成 {attacker.Attack} 点伤害。");
                return;
            }

            ResolveUnitAttack(attacker, row, target, logLines, "攻击");
        }

        public void ResolvePlayerAttack(List<string> logLines)
        {
            for (var row = 0; row < BattleFormation.FormationRows; row++)
            {
                for (var column = BattleFormation.MaxFormationSlots - 1; column >= 0; column--)
                {
                    var attacker = state.PlayerUnits[BattleFormation.GetSlotIndex(row, column)];
                    if (attacker == null || attacker.IsDead)
                        continue;

                    var target = formation.GetEnemyFrontUnit(row);
                    if (target == null)
                        continue;

                    ResolveUnitAttack(attacker, row, target, logLines, "反击");
                }
            }
        }

        public void ResolvePlayerUnitAttack(BattleUnit attacker, int row, List<string> logLines)
        {
            if (attacker == null || attacker.IsDead)
                return;

            var target = formation.GetEnemyFrontUnit(row);
            if (target == null)
                return;

            ResolveUnitAttack(attacker, row, target, logLines, "反击");
        }

        public void TriggerPlayerTurnStartEffects(List<string> logLines)
        {
            for (var row = 0; row < BattleFormation.FormationRows; row++)
            {
                for (var column = 0; column < BattleFormation.MaxFormationSlots; column++)
                {
                    var unit = state.PlayerUnits[BattleFormation.GetSlotIndex(row, column)];
                    if (unit == null || unit.IsDead)
                        continue;

                    TriggerEffects(unit, "OnTurnStart", row, null, logLines);
                }
            }
        }

        public void TriggerEffects(BattleUnit source, string timing, int row, BattleUnit currentTarget, List<string> logLines)
        {
            if (source == null || source.IsDead)
                return;

            foreach (var effect in source.Effects)
            {
                if (effect.Timing != timing)
                    continue;

                ResolveEffect(source, effect, row, currentTarget, logLines);
            }
        }

        public void ResolveSkillCardEffects(CardRecord card, BattleTarget target, List<string> logLines)
        {
            if (card.Effects.Count == 0)
            {
                logLines.Add($"「{card.CardName}」已释放，但效果尚未实现。");
                return;
            }

            foreach (var effect in card.Effects)
            {
                switch (effect.EffectType)
                {
                    case "DrawCards":
                        var drawn = deck.DrawCardsWithCount(effect.Value);
                        logLines.Add($"「{card.CardName}」抽取 {drawn} 张牌。");
                        break;
                    case "Heal":
                        foreach (var unit in ResolveSkillTargetUnits(target))
                        {
                            unit.Heal(effect.Value);
                            logLines.Add($"「{card.CardName}」治疗 {unit.Name} {effect.Value} 点。");
                        }
                        break;
                    case "HealAndGainRevival":
                        foreach (var unit in ResolveSkillTargetUnits(target))
                        {
                            unit.Heal(effect.Value);
                            unit.AddRevival(effect.SecondaryValue);
                            logLines.Add($"「{card.CardName}」治疗 {unit.Name} {effect.Value} 点，并使其获得 {effect.SecondaryValue} 层复苏。");
                        }
                        break;
                    case "BuffAttack":
                        foreach (var unit in ResolveSkillTargetUnits(target))
                        {
                            unit.AddAttack(effect.Value);
                            logLines.Add($"「{card.CardName}」使 {unit.Name} 攻击 +{effect.Value}。");
                        }
                        break;
                    case "BuffAttackAndMaxHp":
                        foreach (var unit in ResolveSkillTargetUnits(target))
                        {
                            unit.AddAttack(effect.Value);
                            unit.AddMaxHp(effect.SecondaryValue, true);
                            logLines.Add($"「{card.CardName}」使 {unit.Name} 攻击 +{effect.Value}，最大血量 +{effect.SecondaryValue}，并恢复 {effect.SecondaryValue} 点生命。");
                        }
                        break;
                    case "GainShield":
                        foreach (var unit in ResolveSkillTargetUnits(target))
                        {
                            unit.AddShield(effect.Value);
                            logLines.Add($"「{card.CardName}」使 {unit.Name} 获得 {effect.Value} 层护盾。");
                        }
                        break;
                    case "Damage":
                    case "BonusDamage":
                        foreach (var unit in ResolveSkillTargetUnits(target))
                            DealDamage(null, unit, effect.Value, target.Row, logLines, $"「{card.CardName}」命中 {unit.Name}", false);
                        break;
                    case "DamageGainEnergyOnKill":
                        foreach (var unit in ResolveSkillTargetUnits(target))
                        {
                            var wasAlive = unit != null && !unit.IsDead;
                            DealDamage(null, unit, effect.Value, target.Row, logLines, $"「{card.CardName}」命中 {unit.Name}", false);
                            if (wasAlive && unit != null && unit.IsDead)
                            {
                                state.CurrentEnergy += effect.SecondaryValue;
                                logLines.Add($"「{card.CardName}」击杀 {unit.Name}，获得 {effect.SecondaryValue} 点可使用费用。");
                            }
                        }
                        break;
                    default:
                        logLines.Add($"「{card.CardName}」的效果类型 {effect.EffectType} 尚未实现。");
                        break;
                }
            }
        }

        public void DealDamage(BattleUnit source, BattleUnit target, int amount, int row, List<string> logLines, string actionText, bool triggerDamaged)
        {
            if (target == null || target.IsDead || amount <= 0)
                return;

            if (source != null && source.Camp != target.Camp && target.TryConsumeShield())
            {
                logLines.Add($"{target.Name} 消耗 1 层护盾，抵挡了 {source.Name} 的本次攻击。");
                return;
            }

            if (source != null && source.Camp != target.Camp && target.TryConsumeAttackImmunity())
            {
                logLines.Add($"{target.Name} 免疫了 {source.Name} 的一次攻击。");
                return;
            }

            var resolvedDamage = ApplyBeforeDamagedEffects(target, source, amount, row, logLines);
            var hpBefore = target.CurrentHp;
            target.TakeDamage(resolvedDamage);
            var hpDamage = hpBefore - target.CurrentHp;
            var reducedText = resolvedDamage != amount ? $"（原始 {amount}）" : string.Empty;
            logLines.Add($"{actionText}，结算伤害 {resolvedDamage}{reducedText}，扣除生命 {hpDamage} 点。");

            if (triggerDamaged && hpDamage > 0 && !target.IsDead)
                TriggerEffects(target, "OnDamaged", row, source, logLines);
        }

        public List<BattleUnit> ResolveSkillTargetUnits(BattleTarget target)
        {
            var targets = new List<BattleUnit>();
            if (target.Kind == BattleTargetKind.Unit)
            {
                if (target.Unit != null && !target.Unit.IsDead)
                    targets.Add(target.Unit);
                return targets;
            }

            if (target.Kind != BattleTargetKind.Row)
                return targets;

            var units = target.PlayerSide ? state.PlayerUnits : state.EnemyUnits;
            for (var column = 0; column < BattleFormation.MaxFormationSlots; column++)
            {
                var unit = units[BattleFormation.GetSlotIndex(target.Row, column)];
                if (unit != null && !unit.IsDead)
                    targets.Add(unit);
            }

            return targets;
        }

        private void ResolveUnitAttack(BattleUnit attacker, int row, BattleUnit target, List<string> logLines, string verb)
        {
            if (attacker == null || attacker.IsDead)
                return;

            if (TryResolveReplaceAttack(attacker, row, logLines))
                return;

            var attack = attacker.Attack;
            DealDamage(attacker, target, attack, row, logLines, $"{attacker.Name} {verb}{BattleFormation.GetFormationRowName(row)} {target.Name}（攻击力 {attack}）", true);
            TriggerEffects(attacker, "OnAttack", row, target, logLines);
        }

        private bool TryResolveReplaceAttack(BattleUnit attacker, int row, List<string> logLines)
        {
            foreach (var effect in attacker.Effects)
            {
                if (effect.Timing != "OnAttack" || effect.EffectType != "ReplaceAttack")
                    continue;

                var targets = ResolveTargets(attacker, effect.TargetRule, row, null);
                if (targets.Count == 0)
                    return true;

                logLines.Add($"{attacker.Name} 触发「{effect.EffectName}」。");
                foreach (var target in targets)
                    DealDamage(attacker, target, effect.Value, row, logLines, $"{effect.EffectName} 命中{BattleFormation.GetFormationRowName(row)} {target.Name}", true);

                return true;
            }

            return false;
        }

        private void ResolveEffect(BattleUnit source, EffectRecord effect, int row, BattleUnit currentTarget, List<string> logLines)
        {
            switch (effect.EffectType)
            {
                case "Heal":
                    foreach (var target in ResolveTargets(source, effect.TargetRule, row, currentTarget))
                    {
                        target.Heal(effect.Value);
                        logLines.Add($"{source.Name} 触发「{effect.EffectName}」，治疗 {target.Name} {effect.Value} 点。");
                    }
                    break;
                case "BuffAttack":
                    foreach (var target in ResolveTargets(source, effect.TargetRule, row, currentTarget))
                    {
                        target.AddAttack(effect.Value);
                        logLines.Add($"{source.Name} 触发「{effect.EffectName}」，{target.Name} 攻击 +{effect.Value}。");
                    }
                    break;
                case "GainShield":
                    foreach (var target in ResolveTargets(source, effect.TargetRule, row, currentTarget))
                    {
                        target.AddShield(effect.Value);
                        logLines.Add($"{source.Name} 触发「{effect.EffectName}」，{target.Name} 获得 {effect.Value} 层护盾。");
                    }
                    break;
                case "DrawCards":
                    var drawn = deck.DrawCardsWithCount(effect.Value);
                    logLines.Add($"{source.Name} 触发「{effect.EffectName}」，额外抽 {drawn} 张牌。");
                    break;
                case "BonusDamage":
                case "Damage":
                    foreach (var target in ResolveTargets(source, effect.TargetRule, row, currentTarget))
                        DealDamage(source, target, effect.Value, row, logLines, $"{source.Name} 触发「{effect.EffectName}」命中 {target.Name}", false);
                    break;
            }
        }

        private int ApplyBeforeDamagedEffects(BattleUnit target, BattleUnit source, int damage, int row, List<string> logLines)
        {
            var resolvedDamage = damage;
            foreach (var effect in target.Effects)
            {
                if (effect.Timing != "BeforeDamaged" || effect.EffectType != "DamageCap")
                    continue;

                if (resolvedDamage > effect.Value)
                {
                    resolvedDamage = effect.Value;
                    logLines.Add($"{target.Name} 触发「{effect.EffectName}」，本次伤害最多受到 {effect.Value} 点。");
                }
            }

            return Mathf.Max(0, resolvedDamage);
        }

        private List<BattleUnit> ResolveTargets(BattleUnit source, string targetRule, int row, BattleUnit currentTarget)
        {
            var targets = new List<BattleUnit>();
            switch (targetRule)
            {
                case "Self":
                    AddTarget(targets, source);
                    break;
                case "CurrentTarget":
                case "Attacker":
                    AddTarget(targets, currentTarget);
                    break;
                case "AllyFrontSameRow":
                    AddTarget(targets, source.Camp == CardCamp.Player ? formation.GetPlayerFrontUnit(row) : formation.GetEnemyFrontUnit(row));
                    break;
                case "AllyAllSameRow":
                    AddRowTargets(targets, source.Camp == CardCamp.Player ? state.PlayerUnits : state.EnemyUnits, row);
                    break;
                case "EnemyAllSameRow":
                    AddRowTargets(targets, source.Camp == CardCamp.Player ? state.EnemyUnits : state.PlayerUnits, row);
                    break;
                default:
                    AddTarget(targets, currentTarget);
                    break;
            }

            return targets;
        }

        private static void AddTarget(List<BattleUnit> targets, BattleUnit target)
        {
            if (target != null && !target.IsDead && !targets.Contains(target))
                targets.Add(target);
        }

        private static void AddRowTargets(List<BattleUnit> targets, List<BattleUnit> units, int row, BattleUnit excluded = null)
        {
            for (var column = 0; column < BattleFormation.MaxFormationSlots; column++)
            {
                var unit = units[BattleFormation.GetSlotIndex(row, column)];
                if (unit != null && unit != excluded && !unit.IsDead)
                    AddTarget(targets, unit);
            }
        }
    }
}
