using System;
using Tanks.Core.Helpers;
using Tanks.Shared.Adapters;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Items.Pickups;
using Tanks.Shared.Stats;
using Tanks.Shared.Tests.Adapters;
using Tanks.Shared.Weapons;
using Unity.Mathematics;

namespace Tanks.Shared.Tests.Items
{
    public sealed class MockHealth : IHealth, IDamageable, IHealable
    {
        public PositiveFloatOrZero Value { get; private set; } = 100;
        public PositiveFloat MaxHealth { get; private set; } = 100;

        public void SetMaxHealth(PositiveFloat value)
        {
            MaxHealth = value;
            if (Value > MaxHealth) Value = MaxHealth.Value;
        }

        public void Damage(PositiveFloat amount, IEntity from = null)
        {
            Value = math.max(Value - amount, 0);
        }

        public void Heal(PositiveFloat amount)
        {
            Value = math.min(Value + amount, MaxHealth);
        }

        public void SetCurrentHealth(PositiveFloatOrZero value)
        {
            Value = math.clamp(value, 0, MaxHealth);
        }
    }

    public sealed class MockStatOwner : IStatOwner
    {
        private readonly StatLogic _logic = new StatLogic();

        public void AddStat(
            StatType type, StatValueType valueType, float value, float duration = -1)
        {
            _logic.AddStat(type, valueType, value, duration);
        }

        public float GetStat(StatType type, float baseValue)
        {
            return _logic.GetStat(type, baseValue);
        }
    }

    public sealed class MockItemOwner : IItemOwner
    {
        public MockItemOwner()
        {
            _health = new MockHealth();
        }

        public ITransform Transform { get; } = new MockTransform();
        public IStatOwner StatOwner { get; } = new MockStatOwner();
        public IWeaponOwner WeaponOwner => throw new NotImplementedException();

        public IHealth Health => _health;
        public IDamageable Damageable => _health;
        public IHealable Healable => _health;


        private readonly MockHealth _health;

        public void PickupItem(IItemConfig item, in PickupId pickupId)
        {
            throw new NotImplementedException();
        }
    }
}