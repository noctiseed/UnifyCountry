using System.Collections.Generic;

namespace UnifyCountry.Roguelike
{
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
