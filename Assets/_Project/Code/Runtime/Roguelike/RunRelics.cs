using System.Collections.Generic;

namespace UnifyCountry.Roguelike
{
    public enum RunRelicCategory
    {
        Resource,
        Sustain,
        Counter,
        Entry
    }

    public sealed class RunRelicDefinition
    {
        public string RelicId;
        public string Name;
        public RunRelicCategory Category;
        public string IconText;
        public string EffectText;
    }

    public static class RunRelicIds
    {
        public const string ImperialSeal = "RELIC_001";
        public const string IronArmyOrder = "RELIC_002";
        public const string TaipingTalisman = "RELIC_003";
        public const string IronCaltrops = "RELIC_004";
        public const string FireOxFormation = "RELIC_005";
    }

    public sealed class RunRelicModifiers
    {
        public int FormalTurnMaxEnergyBonus;
        public bool DrawOnFirstUnitCardEachTurn;
        public int RevivalHealBonusPerStack;
        public int ThornsDamageBonusPerStack;
        public int WaveEntryDamage;
        public int WaveEntryBurn;
    }

    public static class RunRelicRules
    {
        public static readonly IReadOnlyList<RunRelicDefinition> Catalog = new List<RunRelicDefinition>
        {
            new RunRelicDefinition
            {
                RelicId = RunRelicIds.ImperialSeal,
                Name = "传国玉玺",
                Category = RunRelicCategory.Resource,
                IconText = "玺",
                EffectText = "正式回合的费用上限 +1。初始准备回合仍为 5 费。"
            },
            new RunRelicDefinition
            {
                RelicId = RunRelicIds.IronArmyOrder,
                Name = "铁甲军令",
                Category = RunRelicCategory.Resource,
                IconText = "令",
                EffectText = "每个正式回合第一次打出单位牌后，抽 1 张牌。"
            },
            new RunRelicDefinition
            {
                RelicId = RunRelicIds.TaipingTalisman,
                Name = "太平符箓",
                Category = RunRelicCategory.Sustain,
                IconText = "符",
                EffectText = "每层复苏额外回复 1 点生命。"
            },
            new RunRelicDefinition
            {
                RelicId = RunRelicIds.IronCaltrops,
                Name = "铁蒺藜",
                Category = RunRelicCategory.Counter,
                IconText = "蒺",
                EffectText = "每层荆棘额外造成 1 点反伤。"
            },
            new RunRelicDefinition
            {
                RelicId = RunRelicIds.FireOxFormation,
                Name = "火牛阵图",
                Category = RunRelicCategory.Entry,
                IconText = "阵",
                EffectText = "每波敌人入场时，对该波所有敌人造成 1 点伤害，并施加 1 层灼烧。"
            }
        };

        public static RunRelicModifiers BuildModifiers(IEnumerable<string> relicIds)
        {
            var modifiers = new RunRelicModifiers();
            if (relicIds == null)
                return modifiers;

            foreach (var relicId in relicIds)
            {
                switch (relicId)
                {
                    case RunRelicIds.ImperialSeal:
                        modifiers.FormalTurnMaxEnergyBonus += 1;
                        break;
                    case RunRelicIds.IronArmyOrder:
                        modifiers.DrawOnFirstUnitCardEachTurn = true;
                        break;
                    case RunRelicIds.TaipingTalisman:
                        modifiers.RevivalHealBonusPerStack += 1;
                        break;
                    case RunRelicIds.IronCaltrops:
                        modifiers.ThornsDamageBonusPerStack += 1;
                        break;
                    case RunRelicIds.FireOxFormation:
                        modifiers.WaveEntryDamage += 1;
                        modifiers.WaveEntryBurn += 1;
                        break;
                }
            }

            return modifiers;
        }
    }
}
