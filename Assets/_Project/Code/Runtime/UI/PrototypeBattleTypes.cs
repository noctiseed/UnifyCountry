using UnifyCountry.Config;
using UnityEngine;
using System.Collections.Generic;

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
            CurrentAttack = card.Attack;
        }

        public int RuntimeId { get; }
        public string Name => card.CardName;
        public string UnitId => card.UnitId;
        public int Attack => CurrentAttack;
        public int BaseAttack => card.Attack;
        public int MaxHp => card.Hp;
        public CardCamp Camp => card.Camp;
        public IReadOnlyList<EffectRecord> Effects => card.Effects;
        public int CurrentAttack { get; private set; }
        public int CurrentHp { get; private set; }
        public int Shield { get; private set; }
        public int AttackImmunityCharges { get; private set; }
        public bool IsDead => CurrentHp <= 0;

        public void Heal(int amount)
        {
            CurrentHp = Mathf.Min(MaxHp, CurrentHp + Mathf.Max(0, amount));
        }

        public void AddAttack(int amount)
        {
            CurrentAttack = Mathf.Max(0, CurrentAttack + amount);
        }

        public void AddShield(int amount)
        {
            Shield = Mathf.Max(0, Shield + amount);
        }

        public void AddAttackImmunity(int charges)
        {
            AttackImmunityCharges = Mathf.Max(0, AttackImmunityCharges + charges);
        }

        public bool TryConsumeAttackImmunity()
        {
            if (AttackImmunityCharges <= 0)
                return false;

            AttackImmunityCharges--;
            return true;
        }

        public int TakeDamage(int amount)
        {
            var remaining = Mathf.Max(0, amount);
            if (remaining <= 0)
                return 0;

            var shieldBlocked = Mathf.Min(Shield, remaining);
            Shield -= shieldBlocked;
            remaining -= shieldBlocked;

            var hpBefore = CurrentHp;
            CurrentHp = Mathf.Max(0, CurrentHp - remaining);
            return hpBefore - CurrentHp;
        }
    }
}
