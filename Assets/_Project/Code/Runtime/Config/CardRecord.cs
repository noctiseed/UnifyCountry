using System.Collections.Generic;

namespace UnifyCountry.Config
{
    public enum CardCamp
    {
        Player,
        Enemy
    }

    public enum UnitType
    {
        Hero,
        Soldier
    }

    public sealed class CardRecord
    {
        public string CardId;
        public string CardName;
        public string UnitId;
        public string UnitName;
        public UnitType UnitType;
        public int Hp;
        public int Attack;
        public int Cost;
        public CardCamp Camp;
        public string Faction;
        public int MaxCopiesInDeck;
        public string DescriptionKey;
    }

    public sealed class WaveSpawnRecord
    {
        public string WaveId;
        public int TurnIndex;
        public string SpawnTiming;
        public readonly List<string>[] RowCardIds =
        {
            new List<string>(),
            new List<string>(),
            new List<string>()
        };
        public string NoteKey;
    }

    public sealed class BattleLevelRecord
    {
        public string LevelId;
        public readonly List<WaveSpawnRecord> Waves = new List<WaveSpawnRecord>();
    }
}
