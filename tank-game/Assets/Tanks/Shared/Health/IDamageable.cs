using Tanks.Core.Helpers;

namespace Tanks.Shared.Health
{
    public interface IHealth
    {
        float Value { get; }

        void SetMaxHealth(PositiveFloat value);
    }

    public interface IDamageable
    {
        void Damage(PositiveFloat amount);
    }

    public interface IHealable
    {
        void Heal(PositiveFloat amount);
    }
}
