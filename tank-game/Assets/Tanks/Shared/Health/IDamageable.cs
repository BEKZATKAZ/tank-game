using Tanks.Shared.Adapters;

namespace Tanks.Shared.Health
{
    public interface IDamageDealer
    {
        ITransform Transform { get; }
    }

    public interface IDamageable
    {
        void Damage(float amount, IDamageDealer from);
    }

    public interface IHealable
    {
        void Heal(float amount);
    }
}
