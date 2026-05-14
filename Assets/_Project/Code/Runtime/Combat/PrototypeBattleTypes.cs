using UnifyCountry.Config;
using UnityEngine;
using System.Collections.Generic;

namespace UnifyCountry.Combat
{
    internal enum BattleBuffType
    {
        Shield,
        Armor,
        AttackImmunity,
        Revival,
        Burn,
        Thorns
    }

    internal sealed class BattleBuff
    {
        public BattleBuff(BattleBuffType type, int stacks)
        {
            Type = type;
            Stacks = Mathf.Max(0, stacks);
        }

        public BattleBuffType Type { get; }
        public int Stacks { get; set; }
    }

    internal readonly struct FormationMove
    {
        public FormationMove(int unitRuntimeId, int fromSlotIndex, int toSlotIndex, bool playerSide)
        {
            UnitRuntimeId = unitRuntimeId;
            FromSlotIndex = fromSlotIndex;
            ToSlotIndex = toSlotIndex;
            PlayerSide = playerSide;
        }

        public int UnitRuntimeId { get; }
        public int FromSlotIndex { get; }
        public int ToSlotIndex { get; }
        public bool PlayerSide { get; }
    }

    internal sealed class BattleUnit
    {
        private readonly CardRecord card;
        private readonly List<BattleBuff> buffs = new List<BattleBuff>();
        private int maxHp;

        public BattleUnit(CardRecord card, int runtimeId, CardCamp camp)
        {
            this.card = card;
            RuntimeId = runtimeId;
            Camp = camp;
            maxHp = card.Hp;
            CurrentHp = card.Hp;
            CurrentAttack = card.Attack;
        }

        public int RuntimeId { get; }
        public string Name => card.CardName;
        public string UnitId => card.UnitId;
        public UnitType UnitType => card.UnitType;
        public int Attack => CurrentAttack;
        public int BaseAttack => card.Attack;
        public int MaxHp => maxHp;
        public CardCamp Camp { get; }
        public IReadOnlyList<EffectRecord> Effects => card.Effects;
        public int CurrentAttack { get; private set; }
        public int CurrentHp { get; private set; }
        public int FormationRow { get; set; } = -1;
        public int Shield => GetBuffStacks(BattleBuffType.Shield);
        public int Armor => GetBuffStacks(BattleBuffType.Armor);
        public int AttackImmunityCharges => GetBuffStacks(BattleBuffType.AttackImmunity);
        public int Revival => GetBuffStacks(BattleBuffType.Revival);
        public int Burn => GetBuffStacks(BattleBuffType.Burn);
        public int Thorns => GetBuffStacks(BattleBuffType.Thorns);
        public bool IsDead => CurrentHp <= 0;

        public void Heal(int amount)
        {
            CurrentHp = Mathf.Min(MaxHp, CurrentHp + Mathf.Max(0, amount));
        }

        public void AddAttack(int amount)
        {
            CurrentAttack = Mathf.Max(0, CurrentAttack + amount);
        }

        public void AddMaxHp(int amount, bool healByAmount)
        {
            var resolvedAmount = Mathf.Max(0, amount);
            if (resolvedAmount <= 0)
                return;

            maxHp += resolvedAmount;
            if (healByAmount)
                CurrentHp = Mathf.Min(maxHp, CurrentHp + resolvedAmount);
        }

        public void AddShield(int amount)
        {
            AddBuff(BattleBuffType.Shield, amount);
        }

        public void AddArmor(int amount)
        {
            AddBuff(BattleBuffType.Armor, amount);
        }

        public int ConsumeArmor(int damage)
        {
            var buff = GetBuff(BattleBuffType.Armor);
            var resolvedDamage = Mathf.Max(0, damage);
            if (buff == null || buff.Stacks <= 0 || resolvedDamage <= 0)
                return 0;

            var absorbed = Mathf.Min(buff.Stacks, resolvedDamage);
            buff.Stacks -= absorbed;
            if (buff.Stacks == 0)
                buffs.Remove(buff);

            return absorbed;
        }

        public bool TryConsumeShield()
        {
            return TryConsumeBuff(BattleBuffType.Shield);
        }

        public void AddAttackImmunity(int charges)
        {
            AddBuff(BattleBuffType.AttackImmunity, charges);
        }

        public bool TryConsumeAttackImmunity()
        {
            return TryConsumeBuff(BattleBuffType.AttackImmunity);
        }

        public void AddRevival(int stacks)
        {
            AddBuff(BattleBuffType.Revival, stacks);
        }

        public void AddBurn(int stacks)
        {
            AddBuff(BattleBuffType.Burn, stacks);
        }

        public void AddThorns(int stacks)
        {
            AddBuff(BattleBuffType.Thorns, stacks);
        }

        public int ResolveRevival(int healBonusPerStack = 0)
        {
            var buff = GetBuff(BattleBuffType.Revival);
            if (buff == null || buff.Stacks <= 0 || IsDead)
                return 0;

            var healAmount = buff.Stacks * (1 + Mathf.Max(0, healBonusPerStack));
            var hpBefore = CurrentHp;
            Heal(healAmount);
            buff.Stacks = Mathf.Max(0, buff.Stacks - 1);
            if (buff.Stacks == 0)
                buffs.Remove(buff);

            return CurrentHp - hpBefore;
        }

        public void DecayBurn()
        {
            var buff = GetBuff(BattleBuffType.Burn);
            if (buff == null || buff.Stacks <= 0)
                return;

            buff.Stacks = Mathf.Max(0, buff.Stacks - 1);
            if (buff.Stacks == 0)
                buffs.Remove(buff);
        }

        public int TakeDamage(int amount)
        {
            var remaining = Mathf.Max(0, amount);
            if (remaining <= 0)
                return 0;

            var hpBefore = CurrentHp;
            CurrentHp = Mathf.Max(0, CurrentHp - remaining);
            return hpBefore - CurrentHp;
        }

        private void AddBuff(BattleBuffType type, int stacks)
        {
            var resolvedStacks = Mathf.Max(0, stacks);
            if (resolvedStacks <= 0)
                return;

            var buff = GetBuff(type);
            if (buff == null)
                buffs.Add(new BattleBuff(type, resolvedStacks));
            else
                buff.Stacks += resolvedStacks;
        }

        private bool TryConsumeBuff(BattleBuffType type)
        {
            var buff = GetBuff(type);
            if (buff == null || buff.Stacks <= 0)
                return false;

            buff.Stacks--;
            if (buff.Stacks == 0)
                buffs.Remove(buff);

            return true;
        }

        private int GetBuffStacks(BattleBuffType type)
        {
            var buff = GetBuff(type);
            return buff == null ? 0 : buff.Stacks;
        }

        private BattleBuff GetBuff(BattleBuffType type)
        {
            for (var i = 0; i < buffs.Count; i++)
            {
                if (buffs[i].Type == type)
                    return buffs[i];
            }

            return null;
        }
    }
}
