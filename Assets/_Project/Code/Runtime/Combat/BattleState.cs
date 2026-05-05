using System.Collections.Generic;
using UnifyCountry.Config;

namespace UnifyCountry.Combat
{
    internal enum BattlePhase
    {
        InitialPrepare,
        PlayerAction
    }

    internal sealed class BattleState
    {
        public Dictionary<string, CardRecord> CardMap { get; set; } = new Dictionary<string, CardRecord>();
        public List<BattleLevelRecord> Levels { get; set; } = new List<BattleLevelRecord>();
        public List<CardRecord> Library { get; } = new List<CardRecord>();
        public List<CardRecord> DrawPile { get; } = new List<CardRecord>();
        public List<CardRecord> DiscardPile { get; } = new List<CardRecord>();
        public List<CardRecord> Hand { get; } = new List<CardRecord>();
        public List<BattleUnit> PlayerUnits { get; } = new List<BattleUnit>();
        public List<BattleUnit> EnemyUnits { get; } = new List<BattleUnit>();

        public int TurnNumber { get; set; }
        public int CurrentLevelIndex { get; set; }
        public int NextWaveIndex { get; set; }
        public int NextUnitRuntimeId { get; set; } = 1;
        public int CurrentEnergy { get; set; }
        public int PlayerBaseHp { get; set; }
        public BattlePhase BattlePhase { get; set; } = BattlePhase.InitialPrepare;

        public BattleLevelRecord CurrentLevel => Levels.Count == 0 ? null : Levels[CurrentLevelIndex];
        public List<WaveSpawnRecord> CurrentWaves => CurrentLevel == null ? null : CurrentLevel.Waves;
        public bool HasNextLevel => CurrentLevelIndex + 1 < Levels.Count;

        public BattleUnit CreateUnit(CardRecord card)
        {
            return new BattleUnit(card, NextUnitRuntimeId++);
        }

        public void ClearBattleCollections()
        {
            Library.Clear();
            DrawPile.Clear();
            DiscardPile.Clear();
            Hand.Clear();
            PlayerUnits.Clear();
            EnemyUnits.Clear();
        }

        public void EnsureFormationSlots()
        {
            while (PlayerUnits.Count < BattleFormation.TotalFormationSlots)
                PlayerUnits.Add(null);

            while (EnemyUnits.Count < BattleFormation.TotalFormationSlots)
                EnemyUnits.Add(null);
        }
    }
}
