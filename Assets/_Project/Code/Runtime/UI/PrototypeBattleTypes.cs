using UnifyCountry.Config;
using UnityEngine;

namespace UnifyCountry.UI
{
    internal readonly struct FormationMove
    {
        public FormationMove(int unitRuntimeId, int fromSlotIndex, int toSlotIndex)
        {
            UnitRuntimeId = unitRuntimeId;
            FromSlotIndex = fromSlotIndex;
            ToSlotIndex = toSlotIndex;
        }

        public int UnitRuntimeId { get; }
        public int FromSlotIndex { get; }
        public int ToSlotIndex { get; }
    }

    internal sealed class BattleUnit
    {
        private readonly CardRecord card;

        public BattleUnit(CardRecord card, int runtimeId)
        {
            this.card = card;
            RuntimeId = runtimeId;
            CurrentHp = card.Hp;
        }

        public int RuntimeId { get; }
        public string Name => card.CardName;
        public int Attack => card.Attack;
        public int MaxHp => card.Hp;
        public CardCamp Camp => card.Camp;
        public int CurrentHp { get; private set; }
        public bool IsDead => CurrentHp <= 0;

        public void TakeDamage(int amount)
        {
            CurrentHp = Mathf.Max(0, CurrentHp - amount);
        }
    }
}
