using Tanks.Core.Helpers;

namespace Tanks.Shared.Entities.Health
{
    public interface IHealth
    {
        PositiveFloatOrZero Value { get; }
        PositiveFloat MaxHealth { get; }

        void SetCurrentHealth(PositiveFloatOrZero value);
        void SetMaxHealth(PositiveFloat value);
    }

    public interface IDamageable
    {
        void Damage(PositiveFloat amount, IEntity from = null);
    }

    public interface IHealable
    {
        void Heal(PositiveFloat amount);
    }
}
