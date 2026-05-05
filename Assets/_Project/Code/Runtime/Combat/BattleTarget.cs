namespace UnifyCountry.Combat
{
    internal enum BattleTargetKind
    {
        None,
        Unit,
        Row
    }

    internal readonly struct BattleTarget
    {
        public BattleTarget(BattleTargetKind kind, bool playerSide, int row, BattleUnit unit)
        {
            Kind = kind;
            PlayerSide = playerSide;
            Row = row;
            Unit = unit;
        }

        public BattleTargetKind Kind { get; }
        public bool PlayerSide { get; }
        public int Row { get; }
        public BattleUnit Unit { get; }

        public static BattleTarget ForUnit(bool playerSide, int row, BattleUnit unit)
        {
            return new BattleTarget(BattleTargetKind.Unit, playerSide, row, unit);
        }

        public static BattleTarget ForRow(bool playerSide, int row)
        {
            return new BattleTarget(BattleTargetKind.Row, playerSide, row, null);
        }
    }
}
