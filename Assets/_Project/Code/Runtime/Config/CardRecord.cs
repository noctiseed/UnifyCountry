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
}
