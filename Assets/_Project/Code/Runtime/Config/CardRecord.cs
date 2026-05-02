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

    public enum CardType
    {
        Unit,
        Skill,
        Equipment,
        Power,
        Event
    }

    public sealed class CardRecord
    {
        public string CardId;
        public string CardName;
        public CardType CardType;
        public int Cost;
        public CardCamp Camp;
        public string Faction;
        public string Rarity;
        public int MaxCopiesInDeck;
        public string ArtId;
        public string EffectId;
        public string DescriptionKey;
        public UnitRecord Unit;

        public string UnitId => Unit == null ? string.Empty : Unit.UnitId;
        public string UnitName => Unit == null ? CardName : Unit.UnitName;
        public UnitType UnitType => Unit == null ? UnifyCountry.Config.UnitType.Soldier : Unit.UnitType;
        public int Hp => Unit == null ? 0 : Unit.Hp;
        public int Attack => Unit == null ? 0 : Unit.Attack;
    }

    public sealed class UnitRecord
    {
        public string CardId;
        public string UnitId;
        public string UnitName;
        public UnitType UnitType;
        public int Hp;
        public int Attack;
        public string Role;
        public string Tags;
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
